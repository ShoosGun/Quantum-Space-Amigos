using System;
using System.Collections.Generic;
using System.Net.Sockets;
using DIMOWAModLoader;
using ServerSide.Sockets.Clients;
using System.Threading;
using System.IO;


using ServerSide.PacketCouriers.Essentials;

namespace ServerSide.Sockets.Servers
{
    public class Server
    {
        //Vocês não iriam acreditar que todo esse código (e mais) foi deletado por eu ter feito merge enquanto tentava colocar ele em um repo
        // É, nem eu acreditaria
        // que aventura desgranhenta, nunca mais não terei um backup de algum tipo (ao menos que eu esqueça :P)
        private ClientDebuggerSide debugger;
        private Listener l;
        private List<Client> clients;
        private Dictionary<string, Client> clientsLookUpTable;

        private List<Client> NewClientsCache = new List<Client>();
        private readonly object NCC_lock = new object();

        private const int MAX_DATA_PER_CLIENT_LOOP = 3;
        private Dictionary<string, byte[][]> ReceivedDataCache = new Dictionary<string, byte[][]>();
        private readonly object RDC_lock = new object();

        private List<string> DisconnecedClientsCache = new List<string>();
        private readonly object DCC_lock = new object();


        //Parte legal
        //private IPacketCourier[] PacketCouriers;
        private Server_DynamicPacketIO dynamicPacketIO;
        public Server_DynamicPacketCourierHandler dynamicPacketCourierHandler { get; private set; }
        private const int OBLIGATORY_HEADER_VALUE_OF_DPCH = 0;

        public Server(ClientDebuggerSide debugger)
        {
            this.debugger = debugger;

            dynamicPacketIO = new Server_DynamicPacketIO();
            dynamicPacketCourierHandler = new Server_DynamicPacketCourierHandler(ref dynamicPacketIO, this);

            if(dynamicPacketCourierHandler.HeaderValue != OBLIGATORY_HEADER_VALUE_OF_DPCH)
                throw new OperationCanceledException(string.Format("dynamicPacketCourierHandler tem que ter como HeaderValue o valor de {0}, mas no lugar tem {1}"
                    , OBLIGATORY_HEADER_VALUE_OF_DPCH, dynamicPacketCourierHandler.HeaderValue));

            clients = new List<Client>();
            clientsLookUpTable = new Dictionary<string, Client>();
            l = new Listener(2121, AllowedConnections.ANY, this.debugger);
            l.SocketAccepted += L_SocketAccepted;
            l.Start();

        }

        /// <summary>
        /// Handles new connections on the in game loop and creates the in-game representation of the client
        /// </summary>
        /// <returns></returns>
        private void NewConnections(Client client)
        {
            clientsLookUpTable.Add(client.ID, client);
            clients.Add(client);
            lock (RDC_lock)
            {
                var maxDataPerLoop = new byte[MAX_DATA_PER_CLIENT_LOOP][];
                for (int i = 0; i < MAX_DATA_PER_CLIENT_LOOP; i++)
                {
                    maxDataPerLoop[i] = new byte[] { };
                }
                ReceivedDataCache.Add(client.ID, maxDataPerLoop);
            }

            NewConnection?.Invoke();
            NewConnectionID?.Invoke(client.ID);

            string clientsString = "";
            foreach (Client c in clients)
            {
                clientsString += string.Format("\n{0}\n===================", c.ID);
            }

            debugger.SendLog("Lista dos clientes conectados ate agora:" + clientsString, DebugType.LOG);
        }
        private void NewConnections(List<Client> newClients)
        {
            foreach (var c in newClients)
            {
                NewConnections(c);
            }
        }
        /// <summary>
        /// Handles disconnections on the in game loop and delets the in-game representation of the client
        /// </summary>
        /// <returns></returns>
        private void Disconnections(string clientID)
        {
            Client c = clientsLookUpTable[clientID];
            debugger.SendLogMultiThread(c.ID + " se desconectou!", DebugType.LOG);

            Disconnection?.Invoke();
            DisconnectionID?.Invoke(clientID);

            c.Close();
            clientsLookUpTable.Remove(c.ID);
            clients.Remove(c);

            string clientsString = "";
            foreach (Client temp in clients)
            {
                clientsString = clientsString + "\n" + temp.ID + "\n===================";
            }
            debugger.SendLogMultiThread("Lista dos clientes conectados ate agora:\n" + clientsString, DebugType.LOG);
        }
        private void Disconnections(List<string> clientsIDs)
        {
            foreach (var c in clientsIDs)
            {
                Disconnections(c);
            }
        }
        /// <summary>
        /// Handles new data sent by the clients on the in-game loop
        /// </summary>
        /// <returns></returns>
        private void ReceivedData(string clientID, byte[] data)
        {
            if (data.Length > 0)
            {
                Client c = clientsLookUpTable[clientID];
                PacketReader packet = new PacketReader(data);
                try
                {
                    dynamicPacketIO.ReadReceivedPacket(packet, clientID);
                }
                catch (Exception ex)
                {
                    debugger.SendLog($"Erro ao ler dados de {c.ID}: {ex.Source} | {ex.Message}", DebugType.ERROR);
                }
            }
        }

        /// <summary>
        ///  Handles the new packets sent by all the clients on the in-game loop
        /// </summary>
        /// <param name="receivedData">Holds the packets in an array separated by the clients ids</param>
        /// <param name="resetDataArrays"></param>
        private void ReceivedData(Dictionary<string, byte[][]> receivedData, bool resetDataArrays = true)
        {
            //Ideias: 
            // 1 - Fazer a leitura de dados no async (X)
            // 2 - Fazer a leitura de dados em uma Coroutine da unity (X)
            // 3 - No lugar de enviar byte[]'s enviamos PacketReaders um pouco tratadas (X)
            for (int i = 0; i < clients.Count; i++)
            {
                string clientID = clients[i].ID;
                for (int j = 0; j < receivedData[clientID].Length; j++)
                {
                    byte[] data = receivedData[clientID][j];
                    if (data.Length > 0)
                    {
                        ReceivedData(clientID, data);
                        if (resetDataArrays)
                            receivedData[clientID][j] = new byte[] { };
                    }
                }
            }
        }

        public void FixedUpdate()
        {
            //Send data
            dynamicPacketCourierHandler.SendUpdateHeaders();

            //Global Data
            byte[] globalDataBuffer = dynamicPacketIO.GetGlobalPacketWriterData();
            if (globalDataBuffer.Length > 0)
                SendAll(globalDataBuffer);

            //Client Specific Data
            for (int i =0; i< clients.Count; i++)
            {
                byte[] clientSpecificBuffer = dynamicPacketIO.GetClientSpecificPacketWriterData(clients[i].ID);
                if (clientSpecificBuffer.Length > 0)
                    Send(clientSpecificBuffer, clients[i].ID);
            }
            dynamicPacketIO.ResetClientSpecificDataHolder();
            //

            bool NCC_NotLoked = Monitor.TryEnter(NCC_lock, 10);
            try
            {
                if (NCC_NotLoked && NewClientsCache.Count > 0)
                {
                    NewConnections(NewClientsCache);
                    NewClientsCache.Clear();
                }
            }
            finally
            {
                Monitor.Exit(NCC_lock);
            }

            bool RDC_NotLoked = Monitor.TryEnter(RDC_lock, 10);
            try
            {
                if (RDC_NotLoked)
                {
                    ReceivedData(ReceivedDataCache); // Dá clear dentro do método
                }
            }
            finally
            {
                Monitor.Exit(RDC_lock);
            }

            bool DCC_NotLoked = Monitor.TryEnter(DCC_lock, 10);
            try
            {
                if (DCC_NotLoked && DisconnecedClientsCache.Count > 0)
                {
                    Disconnections(DisconnecedClientsCache);
                    DisconnecedClientsCache.Clear();
                }
            }
            finally
            {
                Monitor.Exit(DCC_lock);
            }
        }

        private void L_SocketAccepted(Socket e)
        {
            //debugger.SendLogMultiThread(string.Format("Nova Coneccao: {0}\n===================", e.RemoteEndPoint), DebugType.LOG);
            Client client = new Client(e, debugger);
            client.Received += Client_Received;
            client.Disconnected += Client_Disconnected;
            lock (NCC_lock)
            {
                NewClientsCache.Add(client);
            }
        }

        private void Client_Disconnected(Client sender)
        {
            lock (DCC_lock)
            {
                DisconnecedClientsCache.Add(sender.ID);
            }
        }

        private void Client_Received(Client sender, byte[] data)
        {
            bool RDC_NotLoked = Monitor.TryEnter(RDC_lock);
            try
            {
                if (RDC_NotLoked)
                {
                    if (!ReceivedDataCache.ContainsKey(sender.ID))
                        return;

                    var senderCache = ReceivedDataCache[sender.ID];

                    for (int i = 0; i < senderCache.Length; i++)
                        if (senderCache[i].Length <= 0)
                        {
                            senderCache[i] = data;
                            break;
                        }
                }
            }
            finally
            {
                Monitor.Exit(RDC_lock);
            }
        }

        /// <summary>
        /// Sends a buffer of information to an especified Client
        /// </summary>
        /// <param name="shade">The client's in-game representation</param>
        /// <param name="buffer"></param>
        public void Send(byte[] buffer, string clientID)
        {
            if (clientsLookUpTable.ContainsKey(clientID))
                clientsLookUpTable[clientID].Send(buffer);
        }
        /// <summary>
        /// Sends a buffer of information to an array of especified Clients. It is faster, to use SendAll if you want to send some information to all connected clients.
        /// </summary>
        /// <param name="shades"></param>
        /// <param name="buffer"></param>
        public void Send(byte[] buffer, params string[] clientIDs)
        {
            foreach (string id in clientIDs)
            {
                Send(buffer, id);
            }
        }
        /// <summary>
        /// Sends a buffer of information to all Clients. By iterating through all the Clients and sending with their sockets, it can be faster compared to searching for all ClientID's and then using Send().
        /// </summary>
        /// <param name="Exceptions"> The ids of the ones you don't want to send to</param>
        /// <param name="buffer"></param>
        public void SendAll(byte[] buffer, params string[] Exceptions)
        {
            foreach (Client c in clients)
            {
                bool isInExceptions = false;
                foreach (string s in Exceptions)
                {
                    if (c.ID == s)
                    {
                        isInExceptions = true;
                        break;
                    }
                }
                if (!isInExceptions)
                    c.Send(buffer);
            }
        }

        public void Stop()
        {
            debugger.SendLog("Fechando o servidor . . .", DebugType.LOG);
            l.Stop();
        }

        public event NewConnectionHandler NewConnection;
        public delegate void NewConnectionHandler();

        public event NewConnectionIDHandler NewConnectionID;
        public delegate void NewConnectionIDHandler(string clientID);

        public event DisconnectionHandler Disconnection;
        public delegate void DisconnectionHandler();

        public event DisconnectionHandlerID DisconnectionID;
        public delegate void DisconnectionHandlerID(string clientID);
    }

    public struct ClientEssentials
    {
        public string ClientID;
        public byte[] Data;

        public ClientEssentials(string clientID, byte[] data)
        {
            ClientID = clientID;
            Data = data;
        }
    }
}
