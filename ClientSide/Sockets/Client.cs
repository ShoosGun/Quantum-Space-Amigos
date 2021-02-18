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
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Connected = serverSocket.Connected;
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
            //Tentar conectar, e se conectar gravar o IP na string
            debugger.SendLogMultiThread("Tentando conectar...");
            serverSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), serverPort), ConnectCallback, null);
            //Se for negativo ira esperar para sempre por uma conecção
            if (timeOut > 0)
                new Thread(() =>
                {
                    DateTime time = DateTime.UtcNow;
                    while (!serverSocket.Connected)
                    {
                        if ((DateTime.UtcNow - time).Milliseconds > timeOut && !serverSocket.Connected)
                        {
                            try
                            {
                                serverSocket.Close();
                            }
                            catch (SocketException ex)
                            {
                                if (ex.ErrorCode == 10038) //Erro de timeout
                                {
                                    debugger.SendLogMultiThread($"Server demorou mais que {timeOut} milisegundos para responder, tentativa cancelada", DebugType.WARNING);
                                    serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    serverSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), serverPort), ConnectCallback, null);
                                }
                                else
                                {
                                    debugger.SendLogMultiThread($"Erro ao fechar com o timeout >> Error Code:{ex.ErrorCode}\n {ex.Message} -- {ex.Source}", DebugType.ERROR);
                                    break;
                                }
                            }
                        }
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

        public event ConnectionHandler Connection;
        public delegate void ConnectionHandler();

        public event DisconnectionHandler Disconnection;
        public delegate void DisconnectionHandler();
    }
}
