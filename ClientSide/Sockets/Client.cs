using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using DIMOWAModLoader;


namespace ClientSide.Sockets
{
    public class Client
    {
        private const int serverPort = 2121;
        private Socket serverSocket; //Port do servidor: 2121
        private const ProtocolType serverProtocolType = ProtocolType.Tcp;

        private string ConnectedServerIP = ""; //se a conecção der certo, gravar para tentar reconectar no futuro caso haja uma desconecção
        private ClientDebuggerSide debugger;
        public bool Connected { private set; get; }
        
        private readonly object packetBuffers_lock = new object();
        private Queue<byte[]> packetBuffers = new Queue<byte[]>();

        //private int receivingLimit;
        private bool wasConnected = false;
        
        public Client_DynamicPacketIO DynamicPacketIO { get; private set; }

        private static Client CurrentClient = null;
        public static Client GetClient()
        {
            return CurrentClient;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="debugger"></param>
        /// <param name="receivingLimit"> In packets/s </param>
        public Client(ClientDebuggerSide debugger, int receivingLimit = 30)//TODO reinplement the receiveLimit param
        {
            if (CurrentClient != null)
                return;

            Connected = false;
            //this.receivingLimit = receivingLimit;
            this.debugger = debugger;

            DynamicPacketIO = new Client_DynamicPacketIO();

            CurrentClient = this;
        }

        /// <summary>
        /// Disconected any prior connection before attempting to connect, the attempts happen in another thread
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="timeOut"> In milliseconds. It will wait indefinitely if set to a negative number</param>
        public void TryConnect(string IP, int timeOut = -1)
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, serverProtocolType);
            ConnectedServerIP = IP;
            //Tentar conectar, e se conectar gravar o IP na string
            debugger.SendLog("Tentando conectar...");
            //Se for negativo ira esperar para sempre por uma conecção

            if (timeOut >= 0)
                new Thread(() =>
                {
                    serverSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), serverPort), ConnectCallback, null).AsyncWaitHandle.WaitOne(timeOut, true);
                    if (!serverSocket.Connected)
                    {
                        serverSocket.Close();
                        ConnectedServerIP = "";
                        debugger.SendLogMultiThread($"A tentativa de coneccao superou o limite de {timeOut} ms, tentativa abortada", DebugType.WARNING);
                    }
                }).Start();
            else
                serverSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), serverPort), ConnectCallback, null);
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
                    packetBuffers.Enqueue(buffer);

                //TODO refazer isso
                ////Proteção anti-spam de pacotes
                //if ((DateTime.UtcNow - startedReceivingTime).Milliseconds >= 1000)
                //    amountOfReceivedPackets = 0;

                //else if (amountOfReceivedPackets > receivingLimit)
                //{
                //    Thread.Sleep(1000 - (DateTime.UtcNow - startedReceivingTime).Milliseconds); // Esperar até dar um segundo
                //}
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
                    debugger.SendLog("Conectados!");
                    wasConnected = true;
                    Connection?.Invoke();
                }
                byte[] buffer = DynamicPacketIO.GetAllData();
                if (buffer.Length > 0)
                    Send(buffer);
                //Ler dados
                bool packetBuffers_NotLocked = Monitor.TryEnter(packetBuffers_lock, 10);
                try
                {
                    if (packetBuffers_NotLocked && packetBuffers.Count > 0)
                    {
                        ReceiveData(packetBuffers); //We could use Dequeue inside ReceiveData
                        packetBuffers.Clear();
                    }
                }
                finally
                {
                    Monitor.Exit(packetBuffers_lock);
                }
            }
            else if (wasConnected)
            {
                debugger.SendLog("Desconectados!");
                wasConnected = false;
                Disconnection?.Invoke();
            }
        }
        private void ReceiveData(Queue<byte[]> packets)
        {
            foreach(byte[] data in packets)
            {
                if (data.Length > 0)
                {
                    PacketReader packet = new PacketReader(data);
                    try
                    {
                        DynamicPacketIO.ReadReceivedPacket(ref packet);
                    }
                    catch (Exception ex)
                    {
                        debugger.SendLog($"Erro ao ler dados do servidor: {ex.Source} | {ex.Message}", DebugType.ERROR);
                    }
                }
            }
        }
        public void Send(byte[] data)
        {
            lock (this) //Será que isso ajuda? Sla, mas não quero que crashe de novo, se quiser tire esse lock e teste ai
            {
                try
                {   //Bruh momiento (pf não inverter na próxima vez, más lembranças)
                    byte[] sizeBuffer = BitConverter.GetBytes(data.Length);
                    serverSocket.Send(sizeBuffer, 0, sizeBuffer.Length, 0);
                    serverSocket.Send(data, 0, data.Length, 0);
                }
                catch (Exception ex)
                {
                    debugger.SendLogMultiThread("Erro enquanto ao enviar dados >> " + ex.Message, DebugType.ERROR);
                }
            }
        }
        public void Close()
        {
            if(serverSocket != null)
                serverSocket.Close();

            CurrentClient = null;
        }

        public event ConnectionHandler Connection;
        public delegate void ConnectionHandler();

        public event DisconnectionHandler Disconnection;
        public delegate void DisconnectionHandler();
    }
}
