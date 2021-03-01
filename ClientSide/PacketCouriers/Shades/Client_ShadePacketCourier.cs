using System;
using System.Collections.Generic;
using UnityEngine;
using ClientSide.Sync;
using ClientSide.Sockets;

namespace ClientSide.PacketCouriers.Shades
{
    public class Client_ShadePacketCourier : MonoBehaviour, IPacketCourier
    {
        private Client client;

        private Transform solarSystemTransform;

        private List<KeyValuePair<ShadeTransformIDPair, DateTime>> serverSnapshots = new List<KeyValuePair<ShadeTransformIDPair, DateTime>>();

        const int MAX_SNAPSHOTS = 10; //Número máxmo fotos que se pode ter do jogo

        private Shade playerShade;
        private string playerID = "";
        private Dictionary<string, Shade> serverShadesLookUpTable = new Dictionary<string, Shade>();

        void Start()
        {
            solarSystemTransform = GameObject.Find("HomePlanet_graybox").transform;
            client = GameObject.Find("QSAClient").GetComponent<ClientMod>()._clientSide;

            client.Connection += Client_Connection;
            client.Disconnection += Client_Disconnection;
        }

        void OnDestroy()
        {
            client.Connection -= Client_Connection;
            client.Disconnection -= Client_Disconnection;
        }

        private void Client_Connection()
        {
            playerShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();

            Debug.Log("Conectados no Servidor!");
        }


        private void Client_Disconnection()
        {
            playerShade.DestroyShade();
            serverShadesLookUpTable.Clear();
            Debug.Log("Desconectados do servidor");
        }
        
        void FixedUpdate()
        {
            //Lendo as novas posições dadas pelo servidor e dando-as para as respectivas shades
            if (playerID != "")
            {
                foreach (var shadeTransf in serverSnapshots)
                {
                    if (serverShadesLookUpTable.ContainsKey(shadeTransf.Key.ID))
                    {

                        //Delta Syncs
                        if (serverShadesLookUpTable[shadeTransf.Key.ID].rigidbody != null)
                        {
                            if (shadeTransf.Key.ShadeTransform.isDeltaSync)
                            {
                                serverShadesLookUpTable[shadeTransf.Key.ID].rigidbody.position = solarSystemTransform.TransformDirection(shadeTransf.Key.ShadeTransform.Position) + shadeTransf.Key.ShadeTransform.Position + playerShade.rigidbody.position;
                                serverShadesLookUpTable[shadeTransf.Key.ID].rigidbody.MoveRotation(shadeTransf.Key.ShadeTransform.Rotation);
                            }
                            else
                            {
                                serverShadesLookUpTable[shadeTransf.Key.ID].rigidbody.position = solarSystemTransform.TransformPoint(shadeTransf.Key.ShadeTransform.Position);
                                serverShadesLookUpTable[shadeTransf.Key.ID].rigidbody.rotation = shadeTransf.Key.ShadeTransform.Rotation;
                            }
                        }
                    }
                }
                if (serverSnapshots.Count > 0)
                    serverSnapshots.Clear();
                //Enviando os botões pressionados pelo cliente ao servidor

                PacketWriter pk = new PacketWriter(); 
                pk.Write((byte)Header.SHADE_PC);
                pk.Write((byte)ShadeHeader.MOVEMENT);
                pk.Write(DateTime.UtcNow);

                pk.Write((byte)(ShadeMovementHeader.HORIZONTAL_MOVEMENT | ShadeMovementHeader.SPIN | ShadeMovementHeader.JUMP));

                pk.Write(new Vector3(OWInput.GetAxis(GroundInput.moveX), 0f, OWInput.GetAxis(GroundInput.moveZ)));
                pk.Write(OWInput.GetAxis(GroundInput.turn));
                pk.Write(OWInput.GetButtonDown(GroundInput.jump));

                client.Send(pk.GetBytes());
            }
            else if (playerShade != null && (int)(Time.realtimeSinceStartup * 10) % 10 == 0)
            {
                PacketWriter pk = new PacketWriter(); 
                pk.Write((byte)Header.SHADE_PC);
                pk.Write((byte)ShadeHeader.SHADE_SYNC);
                client.Send(pk.GetBytes());
            }
        }
        public void Receive(ref PacketReader packet)
        {
            DateTime arriveTime;
            ShadeTransform shadeTransform;
            string shadeID;

            switch ((ShadeHeader)packet.ReadByte())
            {
                case ShadeHeader.DELTA_SYNC:
                    arriveTime = packet.ReadDateTime();
                    shadeID = packet.ReadString();

                    shadeTransform = new ShadeTransform(packet.ReadVector3(), packet.ReadQuaternion());
                    serverSnapshots.Add(new KeyValuePair<ShadeTransformIDPair, DateTime>(new ShadeTransformIDPair(shadeTransform,shadeID), arriveTime));
                    break;

                case ShadeHeader.TRANSFORM_SYNC:
                    arriveTime = packet.ReadDateTime();
                    int amountOfShades = packet.ReadByte();
                    for (int i = 0; i < amountOfShades; i++)
                    {
                        shadeID = packet.ReadString();
                        shadeTransform = new ShadeTransform(packet.ReadVector3(), packet.ReadQuaternion(), false);
                        serverSnapshots.Add(new KeyValuePair<ShadeTransformIDPair, DateTime>(new ShadeTransformIDPair(shadeTransform, shadeID), arriveTime));
                    }
                    break;

                case ShadeHeader.SHADE_SYNC:
                    int amountOfNewShades = packet.ReadByte();
                    shadeID = packet.ReadString(); 
                    if (shadeID != playerID)
                    {
                        playerID = shadeID;
                        serverShadesLookUpTable.Add(shadeID, playerShade);
                    }

                    for (int i = 0; i < amountOfNewShades; i++)
                    {
                        shadeID = packet.ReadString();
                        
                        if (!serverShadesLookUpTable.ContainsKey(shadeID))
                            serverShadesLookUpTable.Add(shadeID, GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>());
                    }

                    Debug.Log($"Recebendo as novas shades que existem! Temos no total agora: {serverShadesLookUpTable.Keys.Count}; sendo que nós nos chamamos {playerID}!");
                    break;

                case ShadeHeader.SHADE_DELTA_MINUS_SYNC:
                    shadeID = packet.ReadString();
                    serverShadesLookUpTable.Remove(shadeID);
                    break;

                case ShadeHeader.SHADE_DELTA_PLUS_SYNC:
                    shadeID = packet.ReadString();
                    if (!serverShadesLookUpTable.ContainsKey(shadeID))
                        serverShadesLookUpTable.Add(shadeID, GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>());
                    break;

                default:
                    break;
            }
        }
    }
    
    public struct ShadeTransformIDPair
    {
        public ShadeTransform ShadeTransform;
        public string ID;

        public ShadeTransformIDPair(ShadeTransform ShadeTransform, string ID)
        {
            this.ShadeTransform = ShadeTransform;
            this.ID = ID;
        }
    }

    public struct ShadeTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool isDeltaSync;

        public ShadeTransform(Vector3 position, Quaternion rotation, bool isDeltaSync = true)
        {
            Position = position;
            Rotation = rotation;
            this.isDeltaSync = isDeltaSync;
        }
        public ShadeTransform(Rigidbody rigidbody, bool isDeltaSync = true)
        {
            Position = rigidbody.position;
            Rotation = rigidbody.rotation;
            this.isDeltaSync = isDeltaSync;
        }
    }

    public enum ShadeHeader : byte
    {
        MOVEMENT,
        SET_NAME,
        DELTA_SYNC,
        TRANSFORM_SYNC,
        SHADE_SYNC, //Para que a quantidade de shades nos dois lados seja igual (Para novos clientes)
        SHADE_DELTA_PLUS_SYNC, //  /\ (Para já conectados)
        SHADE_DELTA_MINUS_SYNC,
    }

    public enum ShadeMovementHeader : byte
    {
        HORIZONTAL_MOVEMENT = 1,//001
        JUMP = 2,//010
        SPIN = 4//100
    }
}
