using System;
using System.Collections.Generic;
using System.Net.Sockets;
using DIMOWAModLoader;
using ServerSide.Sockets.Clients;
using ServerSide.Shades;
using UnityEngine;
using System.Threading;

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


        //private List<ClientUpdate>[] UpdatesCache;

        //Parte legal
        private Dictionary<string, Shade> clientsShades = new Dictionary<string, Shade>();
        //private object cShades_lock = new object();

        public Server(ClientDebuggerSide debugger)
        {
            this.debugger = debugger;
            clients = new List<Client>();
            clientsLookUpTable = new Dictionary<string, Client>();
            l = new Listener(2121, this.debugger);
            l.SocketAccepted += L_SocketAccepted;
            l.Start();

            ////MDS HAHAHAHAH
            //UpdatesCache = new List<ClientUpdate>[(int)UpdatesTypes.UpdatesTypes_Size]; // ai não precisamos mudar o valor quando adicionarmos mais no UpdatesTypes
            //for (int i = 0; i < UpdatesCache.Length; i++)
            //{
            //    UpdatesCache[i] = new List<ClientUpdate>();
            //}
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
                for(int i = 0; i< MAX_DATA_PER_CLIENT_LOOP; i++)
                {
                    maxDataPerLoop[i] = new byte[] { };
                }

                ReceivedDataCache.Add(client.ID, maxDataPerLoop);
            }

            Shade newShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
            clientsShades.Add(client.ID, newShade);

            string clientsString = "";
            foreach (Client c in clients)
            {
                clientsString = clientsString + "\n" + c.ID + "\n===================";
            }
            debugger.SendLogMultiThread("Lista dos clientes conectados ate agora:" + clientsString, DebugType.LOG);
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

            //Destruir e remover a representação do cliente no jogo
            clientsShades[c.ID].DestroyShade();
            clientsShades.Remove(c.ID);

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
            try
            {
                PacketReader packet = new PacketReader(data);
				while(data.Length > 0)
                {
                    switch ((Header)packet.ReadByte())
                    {
                        case Header.MOVEMENT:
                            DateTime sendTime = packet.ReadDateTime();

                            Vector3 moveInput = Vector3.zero;
                            float turnInput = 0f;
                            bool jumpInput = false;

                            //Tipos de imput:
                            // Falar quantos deles [1,3] vão vir
                            // 1 - MoveInput - > Vector3
                            // 2 - TurnInput - > float
                            // 3 - JumpInput - > bool

                            for (byte amountOfMovement = packet.ReadByte(); amountOfMovement > 0; amountOfMovement--)
                            {
                                switch ((SubMovementHeader)packet.ReadByte())
                                {
                                    case SubMovementHeader.HORIZONTAL_MOVEMENT:
                                        moveInput = packet.ReadVector3();
                                        break;

                                    case SubMovementHeader.SPIN:
                                        turnInput = packet.ReadSingle();
                                        break;

                                    case SubMovementHeader.JUMP:
                                        jumpInput = packet.ReadBoolean();
                                        break;

                                    default:
                                        break;
                                }
                            }
                            //Send inputs to the specified shade
                            clientsShades[c.ID].PacketCourrier.AddMovementPacket(new MovementPacket(moveInput, turnInput, jumpInput, sendTime));

                            break;

                        case Header.REFRESH:
                            break;

                        case Header.NAME:
                            clientsShades[c.ID].Name = packet.ReadString();
                            break;

                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                debugger.SendLogMultiThread("Erro ao ler dados de " + c.ID + " :" + ex.Message, DebugType.ERROR);
            }
        }
        private void ReceivedData(Dictionary<string,byte[][]> receivedData, bool resetDataArrays = true)
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
                        if(resetDataArrays)
                            receivedData[clientID][j] = new byte[] { };
                    }
                }
                
            }
        }

        public void Update()
        {
            //lock (UpdatesCache.SyncRoot)
            //{
            //    for (int i = 0; i < UpdatesCache.Length; i++)
            //    {
            //        //pegar o par e dependendo do valor do i fazer coisas de acordo com UpdateTypes
            //        foreach (ClientUpdate clientUpdate in UpdatesCache[i])
            //        {
            //            switch ((UpdatesTypes)i)
            //            {
            //                case UpdatesTypes.NEW_CONNECTION:

            //                    clients.Add(clientUpdate.Client);

            //                    Shade newShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
            //                    clientsShades.Add(clientUpdate.Client.ID, newShade);

            //                    string clientsString = "";
            //                    foreach (Client c in clients)
            //                    {
            //                        clientsString = clientsString + "\n" + c.ID + "\n===================";
            //                    }
            //                    debugger.SendLogMultiThread("Lista dos clientes conectados ate agora:" + clientsString, DebugType.LOG);
            //                    break;

            //                case UpdatesTypes.RECEIVED_DATA:
            //                    foreach (Client c in clients)
            //                    {
            //                        if (c.ID == clientUpdate.Client.ID)
            //                        {
            //                            try
            //                            {
            //                                PacketReader packet = new PacketReader(clientUpdate.Data);
            //                                //debugger.SendLogMultiThread(c.ID + " mandou dados!", DebugType.LOG);
            //                                //Fazer o que precisa fazer com os dados
            //                                switch ((Header)packet.ReadByte())
            //                                {
            //                                    case Header.MOVEMENT:
            //                                        DateTime sendTime = packet.ReadDateTime();

            //                                        Vector3 moveInput = Vector3.zero;
            //                                        float turnInput = 0f;
            //                                        bool jumpInput = false;

            //                                        //Tipos de imput:
            //                                        // Falar quantos deles [1,3] vão vir
            //                                        // 1 - MoveInput - > Vector3
            //                                        // 2 - TurnInput - > float
            //                                        // 3 - JumpInput - > bool

            //                                        for (byte amountOfMovement = packet.ReadByte(); amountOfMovement > 0; amountOfMovement--)
            //                                        {
            //                                            switch ((SubMovementHeader)packet.ReadByte())
            //                                            {
            //                                                case SubMovementHeader.HORIZONTAL_MOVEMENT:
            //                                                    moveInput = packet.ReadVector3();
            //                                                    //debugger.SendLogMultiThread($"Movimento Horizontal: {moveInput}");
            //                                                    break;

            //                                                case SubMovementHeader.SPIN:
            //                                                    turnInput = packet.ReadSingle();
            //                                                    //debugger.SendLogMultiThread($"Giro: {turnInput}");
            //                                                    break;

            //                                                case SubMovementHeader.JUMP:
            //                                                    jumpInput = packet.ReadBoolean();
            //                                                    //debugger.SendLogMultiThread($"Pulo: {jumpInput}");
            //                                                    break;

            //                                                default:
            //                                                    break;
            //                                            }
            //                                        }
            //                                        //Send inputs to the specified shade
            //                                        //debugger.SendLogMultiThread($"Dados recebidos de {c.ID}");
            //                                        clientsShades[c.ID].PacketCourrier.AddMovementPacket(new MovementPacket(moveInput, turnInput, jumpInput, sendTime));
            //                                        break;

            //                                    case Header.REFRESH:
            //                                        break;

            //                                    default:
            //                                        break;
            //                                }

            //                            }
            //                            catch (Exception ex)
            //                            {
            //                                debugger.SendLogMultiThread("Erro ao ler dados de " + c.ID + " :" + ex.Message, DebugType.ERROR);
            //                            }
            //                            break;
            //                        }
            //                    }
            //                    break;


            //                case UpdatesTypes.DISCONNECTION:
            //                    foreach (Client c in clients)
            //                    {
            //                        if (c.ID == clientUpdate.Client.ID)
            //                        {
            //                            debugger.SendLogMultiThread(c.ID + " se desconectou!", DebugType.LOG);

            //                            //Destruir e remover a representação do cliente no jogo
            //                            clientsShades[c.ID].DestroyShade();
            //                            clientsShades.Remove(c.ID);

            //                            c.Close();
            //                            clients.Remove(c);


            //                            string stringLoka = "";
            //                            foreach (Client temp in clients)
            //                            {
            //                                stringLoka = stringLoka + "\n" + temp.ID + "\n===================";
            //                            }
            //                            debugger.SendLogMultiThread("Lista dos clientes conectados ate agora:\n" + stringLoka, DebugType.LOG);
            //                            break;
            //                        }
            //                    }
            //                    break;

            //                case UpdatesTypes.UpdatesTypes_Size:
            //                default:
            //                    break;
            //            }
            //        }
            //        //Quando fazer o que tem que fazer, remover o que ja foi
            //        UpdatesCache[i].Clear();
            //    }
            //}
            //Agora estão todos separados, para melhor manuntenção do código
            bool NCC_NotLoked = Monitor.TryEnter(NCC_lock,10);
            try
            {
                if (NCC_NotLoked)
                {
                    if (NewClientsCache.Count > 0)
                    {
                        NewConnections(NewClientsCache);
                        NewClientsCache.Clear();
                    }
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
                    if (DisconnecedClientsCache.Count > 0)
                    {
                        Disconnections(DisconnecedClientsCache);
                        DisconnecedClientsCache.Clear();
                    }
                }
            }
            finally
            {
                Monitor.Exit(DCC_lock);
            }
        }

        private void L_SocketAccepted(Socket e)
        {
            debugger.SendLogMultiThread(string.Format("Nova Coneccao: {0}\n===================", e.RemoteEndPoint), DebugType.LOG);
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

        public void Stop()
        {
            debugger.SendLogMultiThread("Fechando o servidor . . .", DebugType.LOG);
            Update();// ultimo Upddate para limpar o cache de updates
            l.Stop();
        }


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
