using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DIMOWAModLoader;
using System.IO;
using ClientSide.PacketCouriers.Shades;

namespace ClientSide.Sockets
{
    public class Client
    {
        private const int serverPort = 2121;
        private Socket serverSocket; //Port do servidor: 2121
        private string IP; //se a conecção der certo, gravar para tentar reconectar no futuro caso haja uma desconecção
        private ClientDebuggerSide debugger;
        public bool Connected { private set; get; }


        private readonly object packetBuffers_lock = new object();
        private List<byte[]> packetBuffers = new List<byte[]>();
        private int receivingLimit;
        private bool wasConnected = false;

        private Client_ShadePacketCourier shadePacketCourier;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="debugger"></param>
        /// <param name="receivingLimit"> In packets/s </param>
        public Client(ClientDebuggerSide debugger,Client_ShadePacketCourier shadePacketCourier, int receivingLimit = 100)
        {
            Connected = false;
            this.receivingLimit = receivingLimit;
            this.debugger = debugger;
            this.shadePacketCourier = shadePacketCourier;
        }

        /// <summary>
        /// Disconected any prior connection before attempting to connect, the attempts happen in another thread
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="timeOut"> In milliseconds. It will wait indefinitely if set to a negative number</param>
        public void TryConnect(string IP, int timeOut = -1)
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Tentar conectar, e se conectar gravar o IP na string
            debugger.SendLog("Tentando conectar...");
            //Se for negativo ira esperar para sempre por uma conecção

            IAsyncResult result = serverSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), serverPort), ConnectCallback, null);
            if (timeOut >= 0)
                new Thread(() =>
                {
                    bool success = result.AsyncWaitHandle.WaitOne(timeOut, true);
                    if (!serverSocket.Connected)
                    {
                        serverSocket.Close();
                        debugger.SendLogMultiThread($"A tentativa de coneccao superou o limite de {timeOut} ms, tentativa abortada", DebugType.WARNING);
                    }
                }).Start();
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            serverSocket.EndConnect(ar);
            Connected = true;
            startedReceivingTime = DateTime.UtcNow;
            serverSocket.BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, ReceiveCallback, null);
        }
        DateTime startedReceivingTime;
        int amountOfReceivedPackets = 0;
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                serverSocket.EndReceive(ar);
                //Tratamento de dados
                byte[] buffer = new byte[4];
                serverSocket.Receive(buffer, 0, 4, 0);
                int dataSize = BitConverter.ToInt32(buffer, 0);
                if (dataSize <= 0)
                    throw new SocketException();

                buffer = new byte[dataSize];
                int received = serverSocket.Receive(buffer, 0, buffer.Length, 0);
                while (received < dataSize)
                {
                    received += serverSocket.Receive(buffer, received, dataSize - received, 0);
                }

                lock (packetBuffers_lock)
                    packetBuffers.Add(buffer);

                //Proteção anti-spam de pacotes
                if ((DateTime.UtcNow - startedReceivingTime).Milliseconds >= 1000)
                    amountOfReceivedPackets = 0;

                else if (amountOfReceivedPackets > receivingLimit)
                {
                    Thread.Sleep(1000 - (DateTime.UtcNow - startedReceivingTime).Milliseconds); // Esperar até dar um segundo
                }
                serverSocket.BeginReceive(new byte[] { 0 }, 0, 0, 0, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                debugger.SendLogMultiThread(ex.Message, DebugType.ERROR);
                Connected = false;
            }


        }

        public void Update()
        {
            if (Connected)
            {
                if (!wasConnected)
                {
                    wasConnected = true;
                    Connection?.Invoke();
                }

                //Ler dados
                bool packetBuffers_NotLocked = Monitor.TryEnter(packetBuffers_lock, 10);
                try
                {
                    if (packetBuffers_NotLocked && packetBuffers.Count > 0)
                    {
                        ReceiveData(packetBuffers);
                        packetBuffers.Clear();
                    }
                }
                finally
                {
                    Monitor.Exit(packetBuffers_lock);
                }
            }

            if (wasConnected && !Connected)
            {
                wasConnected = false;
                Disconnection?.Invoke();
            }
        }

        private void ReceiveData(byte[] buffer)
        {
            PacketReader packet = new PacketReader(buffer);
            while (true)
            {
                try
                {
                    switch ((Header)packet.ReadByte())
                    {
                        case Header.SHADE_PC:
                            shadePacketCourier.Receive(ref packet); // Fazer a parte da Shades no cliente
                            break;

                        case Header.REFRESH:
                            break;

                        default:
                            throw new EndOfStreamException();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(EndOfStreamException)) //Quando chegar no final da Stream, ele joga um EndOfStreamException, então sabemos que ela acabou
                        debugger.SendLog($"Erro ao ler dados do servidor: {ex.Source} | {ex.Message}", DebugType.ERROR);
                    packet.Close();
                    break;
                }
            }
        }
        private void ReceiveData(List<byte[]> packets)
        {
            foreach (var pk in packets)
                ReceiveData(pk);
        }
        public void Close()
        {
            serverSocket.Close();
        }

        public event ConnectionHandler Connection;
        public delegate void ConnectionHandler();

        public event DisconnectionHandler Disconnection;
        public delegate void DisconnectionHandler();
    }
}
