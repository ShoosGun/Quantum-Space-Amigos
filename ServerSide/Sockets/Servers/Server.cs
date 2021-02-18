using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using DIMOWAModLoader;
using ServerSide.Sockets.Clients;
using ServerSide.PacketCouriers.Shades;
using UnityEngine;
using System.Threading;
using System.IO;

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
        public Server_ShadePacketCourier shadePacketCourier;
        //Fotografias do jogo, uma a cada..., transformar tudo em uma entidade, a qual pode recebe dados de acordo e envia de maneira semelhante


        public Server(ClientDebuggerSide debugger, Server_ShadePacketCourier shadePacketCourier)
        {
            this.debugger = debugger;
            this.shadePacketCourier = shadePacketCourier;

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
                clientsString = clientsString + "\n" + c.ID + "\n===================";
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
        /// Handles new data sent by the clients on the in game loop and passes it to the respective in-game representation
        /// </summary>
        /// <returns></returns>
        private void ReceivedData(string clientID, byte[] data)
        {
            Client c = clientsLookUpTable[clientID];

            PacketReader packet = new PacketReader(data);
            while (true)
            {
                try
                {
                    switch ((Header)packet.ReadByte())
                    {
                        case Header.SHADE_PC:
                            shadePacketCourier.Receive(ref packet, c.ID);
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
                        debugger.SendLog($"Erro ao ler dados de {c.ID}: {ex.Source} | {ex.Message}", DebugType.ERROR);
                    packet.Close();
                    break;
                }
            }
        }
        private void ReceivedData(Dictionary<string, byte[][]> receivedData, bool resetDataArrays = true)
        {
            //Ideias: 
            // 1 - Fazer a leitura de dados no async
            // 2 - Fazer a leitura de dados em uma Coroutine da unity
            // 3 - No lugar de enviar byte[]'s enviamos PacketReaders um pouco tratadas
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
        public void Send(string clientID, byte[] buffer)
        {
            if(clientsLookUpTable.ContainsKey(clientID))
                clientsLookUpTable[clientID].Send(buffer);
        }
        /// <summary>
        /// Sends a buffer of information to an array of especified Clients. It is faster, and safer, to use SendAll if you want to send some information to all connected clients.
        /// </summary>
        /// <param name="shades"></param>
        /// <param name="buffer"></param>
        public void Send(string[] clientIDs, byte[] buffer)
        {
            foreach(string id in clientIDs)
            {
                Send(id, buffer);
            }
        }
        /// <summary>
        /// Sends a buffer of information to all Clients. By iterating through all the Clients and sending with their sockets, it can be faster compared to seraching for all Shades and then using Send().
        /// </summary>
        /// <param name="shades"></param>
        /// <param name="buffer"></param>
        public void SendAll(byte[] buffer)
        {
            foreach(Client c in clients)
            {
                c.Send(buffer);
            }
        }

        public void Stop()
        {
            debugger.SendLog("Fechando o servidor . . .", DebugType.LOG);
            FixedUpdate();// ultimo Upddate para limpar o cache de updates
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
