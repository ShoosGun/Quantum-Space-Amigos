using System;
using System.Collections.Generic;
using System.Net.Sockets;
using DIMOWAModLoader;
using ServerSide.Sockets.Clients;
using ServerSide.Shades;
using UnityEngine;

namespace ServerSide.Sockets.Servers
{
    public class Server
    {
        //Vocês não iriam acreitar que todo esse código (e mais) foi deletado por eu ter feito merge enquanto tentava colocar ele em um repo
        // É, nem eu acreditaria
        // que aventura desgranhenta, nunca mais não terei um backup de algum tipo (ao menos que eu esqueça :P)
        private ClientDebuggerSide debugger;
        private Listener l;
        private List<Client> clients;
        private List<ClientUpdate>[] UpdatesCache;

        //Parte legal
        private Dictionary<string, Shade> clientsShades = new Dictionary<string, Shade>();
        
        public Server(ClientDebuggerSide debugger)
        {
            this.debugger = debugger;
            clients = new List<Client>();
            l = new Listener(2121, this.debugger);
            l.SocketAccepted += L_SocketAccepted;
            l.Start();

            //MDS HAHAHAHAH
            UpdatesCache = new List<ClientUpdate>[(int)UpdatesTypes.UpdatesTypes_Size]; // ai não precisamos mudar o valor quando adicionarmos mais no UpdatesTypes
            for (int i = 0; i < UpdatesCache.Length; i++)
            {
                UpdatesCache[i] = new List<ClientUpdate>();
            }
        }

        //AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA *respira* AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA . . .
        public void Update()
        {
            lock (UpdatesCache.SyncRoot)
            {
                for (int i = 0; i < UpdatesCache.Length; i++)
                {
                    //pegar o par e dependendo do valor do i fazer coisas de acordo com UpdateTypes
                    //(momentos em que você adora ter aprendido phyton por lembrar desse tipo de estrutura mas o odeia pelo mesmo motivo)
                    foreach (ClientUpdate clientUpdate in UpdatesCache[i])
                    {
                        switch ((UpdatesTypes)i)
                        {
                            case UpdatesTypes.NEW_CONNECTION:

                                clients.Add(clientUpdate.Client);

                                Shade newShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
                                clientsShades.Add(clientUpdate.Client.ID, newShade);

                                string clientsString = "";
                                foreach (Client c in clients)
                                {
                                    clientsString = clientsString + "\n" + c.ID + "\n===================";
                                }
                                debugger.SendLogMultiThread("Lista dos clientes conectados ate agora:" + clientsString, DebugType.LOG);
                                break;

                            case UpdatesTypes.RECEIVED_DATA:
                                foreach (Client c in clients)
                                {
                                    if (c.ID == clientUpdate.Client.ID)
                                    {
                                        try
                                        {
                                            PacketReader packet = new PacketReader(clientUpdate.Data);
                                            //debugger.SendLogMultiThread(c.ID + " mandou dados!", DebugType.LOG);
                                            //Fazer o que precisa fazer com os dados
                                            switch ((Header)packet.ReadByte())
                                            {
                                                case Header.MOVEMENT:

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
                                                                //debugger.SendLogMultiThread($"Movimento Horizontal: {moveInput}");
                                                                break;

                                                            case SubMovementHeader.SPIN:
                                                                turnInput = packet.ReadSingle();
                                                                //debugger.SendLogMultiThread($"Giro: {turnInput}");
                                                                break;

                                                            case SubMovementHeader.JUMP:
                                                                jumpInput = packet.ReadBoolean();
                                                                //debugger.SendLogMultiThread($"Pulo: {jumpInput}");
                                                                break;

                                                            default:
                                                                break;
                                                        }
                                                    }
                                                    //Send inputs to the specified shade
                                                    clientsShades[c.ID].MovementModel.AddMovementPacket(new MovementPacket(moveInput, turnInput, jumpInput));
                                                    break;

                                                case Header.REFRESH:
                                                    break;

                                                default:
                                                    break;
                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            debugger.SendLogMultiThread("Erro ao ler dados de " + c.ID + " :" + ex.Message, DebugType.ERROR);
                                        }
                                        break;
                                    }
                                }
                                break;


                            case UpdatesTypes.DISCONNECTION:
                                foreach (Client c in clients)
                                {
                                    if (c.ID == clientUpdate.Client.ID)
                                    {
                                        debugger.SendLogMultiThread(c.ID + " se desconectou!", DebugType.LOG);

                                        //Destruir e remover a representação do cliente no jogo
                                        clientsShades[c.ID].DestroyShade();
                                        clientsShades.Remove(c.ID);

                                        c.Close();
                                        clients.Remove(c);


                                        string stringLoka = "";
                                        foreach (Client temp in clients)
                                        {
                                            stringLoka = stringLoka + "\n" + temp.ID + "\n===================";
                                        }
                                        debugger.SendLogMultiThread("Lista dos clientes conectados ate agora:\n" + stringLoka, DebugType.LOG);
                                        break;
                                    }
                                }
                                break;

                            case UpdatesTypes.UpdatesTypes_Size:
                            default:
                                break;
                        }
                    }
                    //Quando fazer o que tem que fazer, remover o que ja foi
                    UpdatesCache[i].Clear();
                }
            }
        }

        private void L_SocketAccepted(Socket e)
        {
            debugger.SendLogMultiThread(string.Format("Nova Coneccao: {0}\n===================", e.RemoteEndPoint), DebugType.LOG);
            Client client = new Client(e, debugger);
            client.Received += Client_Received;
            client.Disconnected += Client_Disconnected;
            lock (UpdatesCache.SyncRoot)
            {
                UpdatesCache[(int)UpdatesTypes.NEW_CONNECTION].Add(new ClientUpdate(client, null));
            }
        }

        private void Client_Disconnected(Client sender)
        {
            lock (UpdatesCache.SyncRoot)
            {
                UpdatesCache[(int)UpdatesTypes.DISCONNECTION].Add(new ClientUpdate(sender, null));
            }
        }

        private void Client_Received(Client sender, byte[] data)
        {
            lock (UpdatesCache.SyncRoot)
            {
                UpdatesCache[(int)UpdatesTypes.RECEIVED_DATA].Add(new ClientUpdate(sender, data));
            }
        }

        public void Stop()
        {
            debugger.SendLogMultiThread("Fechando o servidor . . .", DebugType.LOG);
            Update();// ultimo Upddate para limpar o cache de updates
            l.Stop();
        }

        //private GameObject ShadeMaker()
        //{
        //    debugger.SendLogMultiThread("Criando um shade!");
        //    return ShadeSpawner.SpawnPlayerShade(GameObject.Find("Player").transform);
        //}

    }

    public struct ClientUpdate
    {
        public Client Client;
        public byte[] Data;

        public ClientUpdate(Client client, byte[] data)
        {
            Client = client;
            Data = data;
        }
    }
}
