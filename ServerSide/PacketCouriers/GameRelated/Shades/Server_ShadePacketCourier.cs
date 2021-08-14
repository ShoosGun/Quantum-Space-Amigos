using System;
using System.Collections.Generic;
using UnityEngine;
using ServerSide.Sockets;
using ServerSide.Sockets.Servers;
using ServerSide.PacketCouriers.Entities;
using ServerSide.PacketCouriers.Essentials;

namespace ServerSide.PacketCouriers.Shades
{
    public class Server_ShadePacketCourier : MonoBehaviour, IPacketCourier
    {
        private Server_DynamicPacketCourierHandler dynamicPacketCourierHandler;
        const string SHADE_LOCALIZATION_STRING = "ShadePacketCourier";
        private int HeaderValue;

        private Server_NetworkedEntityPacketCourier entityPacketCourier;
        private Server server;

        private Shade serverShade;

        private List<ushort> shadesIDs = new List<ushort>();
        private Dictionary<ushort, ShadePacketPair> shadesLookUpTable = new Dictionary<ushort, ShadePacketPair>();

        private Dictionary<string, ushort> clientsIDsConversionTable = new Dictionary<string, ushort>(); //Para converter Id do cliente -> id da entidade (shade)
        
        /// <summary>
        /// The ID for using the NetworkedEntityPC
        /// </summary>
        private byte SHADEPC_ID;

        void Start()
        {
            GameObject serverGO = GameObject.Find("QSAServer");

            server = serverGO.GetComponent<ServerMod>()._serverSide;

            dynamicPacketCourierHandler = server.dynamicPacketCourierHandler;
            HeaderValue = dynamicPacketCourierHandler.AddPacketCourier(SHADE_LOCALIZATION_STRING, Receive);
            entityPacketCourier = serverGO.GetComponent<Server_NetworkedEntityPacketCourier>();

            server.NewConnectionID += Server_NewConnectionID;
            server.DisconnectionID += Server_DisconnectionID;
        }

        void OnDestroy()
        {
            server.NewConnectionID -= Server_NewConnectionID;
            server.DisconnectionID -= Server_DisconnectionID;
        }

        private void Server_NewConnectionID(string clientID)
        {
            Shade newShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
            ushort shadeID = entityPacketCourier.AddEntitySync(newShade, SHADEPC_ID);

            //Criando e guardando a shade do cliente que acabou de conectar
            clientsIDsConversionTable.Add(clientID, shadeID);

            ShadePacketPair shadePacketPair = new ShadePacketPair(newShade);

            shadesLookUpTable.Add(shadeID, shadePacketPair);
            shadesIDs.Add(shadeID);

            //Mandar para quem chegou o ID do grupo de shades que receberá e o id da sua shade
            PacketWriter packetForClient = new PacketWriter();
            packetForClient.Write((byte)ShadeHeader.ENTITY_OWNER_ID);
            packetForClient.Write(SHADEPC_ID);
            packetForClient.Write(shadeID);

            dynamicPacketCourierHandler.DynamicPacketIO.SendPackedData((byte)HeaderValue, packetForClient.GetBytes(), clientID);
            
            Debug.Log($"Nova Shade! ID = {shadeID}\n Quantidade de Shades no momento: {shadesIDs.Count}");
        }


        private void Server_DisconnectionID(string clientID)
        {
            if (clientsIDsConversionTable.ContainsKey(clientID))
            {
                ushort shadeID = clientsIDsConversionTable[clientID];

                shadesLookUpTable[shadeID].Shade.DestroyShade();
                shadesLookUpTable[shadeID].MovementPacketsCache.Clear();
                
                entityPacketCourier.RemoveEntitySync(shadeID); //Falar para ele parar de fazer sync com esse ID

                shadesIDs.Remove(shadeID);
                shadesLookUpTable.Remove(shadeID);
                clientsIDsConversionTable.Remove(clientID);
            }
        }

        private bool wasThereOWRigdb = false;
        void Update()
        {
            if(serverShade == null)
            {
                serverShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();

                SHADEPC_ID = entityPacketCourier.AddEntityOwner();
                ushort serverShadeID = entityPacketCourier.AddEntitySync(serverShade, SHADEPC_ID);

                serverShade.gameObject.collider.enabled = false;
                serverShade.gameObject.AddComponent<MovementConstraints.OWRigidbodyFollowsAnother>();

                ShadePacketPair shadePacketPair = new ShadePacketPair(serverShade);
                shadesIDs.Add(serverShadeID);
                shadesLookUpTable.Add(serverShadeID, shadePacketPair);
            }
            else if (serverShade.GetAttachedOWRigidbody() != null && !wasThereOWRigdb)
            {
                serverShade.GetComponent<MovementConstraints.OWRigidbodyFollowsAnother>().SetConstrain(GameObject.FindGameObjectWithTag("Player").GetAttachedOWRigidbody());
                wasThereOWRigdb = true;
            }
        }
        
        void FixedUpdate()
        {
            //Dá a movimentação recebida dos clientes para suas shades
            foreach (ushort shadeID in shadesIDs)
            {
                ShadePacketPair shadePacketPair = shadesLookUpTable[shadeID];
                if (shadePacketPair.MovementPacketsCache.Count > 0)
                {
                    if (shadePacketPair.Shade.MovementModel != null)
                        shadePacketPair.Shade.MovementModel.SetNewPacket(shadePacketPair.MovementPacketsCache[0]);

                    shadePacketPair.MovementPacketsCache.RemoveAt(0);
                }
                else if (shadePacketPair.Shade.MovementModel != null)
                    shadePacketPair.Shade.MovementModel.SetNewPacket(new MovementPacket(Vector3.zero, 0f, false, DateTime.UtcNow));
            }
        }

        public void Receive(byte[] data, string clientID)
        {
            PacketReader packet = new PacketReader(data);
            switch ((ShadeHeader)packet.ReadByte())
            {
                case ShadeHeader.MOVEMENT:
                    ReceiveMovementPacket(ref packet, clientID);
                    break;

                case ShadeHeader.SET_NAME:
                    if (shadesLookUpTable.ContainsKey(clientsIDsConversionTable[clientID]))
                        shadesLookUpTable[clientsIDsConversionTable[clientID]].Shade.Name = packet.ReadString();
                    break;

                case ShadeHeader.ENTITY_OWNER_ID:
                    PacketWriter packetForClient = new PacketWriter();
                    Debug.Log($"Enviando o ID da nossa PC = {SHADEPC_ID} e do cliente {clientsIDsConversionTable[clientID]}");
                    packetForClient.Write((byte)ShadeHeader.ENTITY_OWNER_ID);
                    packetForClient.Write(SHADEPC_ID);//byte
                    packetForClient.Write(clientsIDsConversionTable[clientID]);//short

                    dynamicPacketCourierHandler.DynamicPacketIO.SendPackedData((byte)HeaderValue, packetForClient.GetBytes(), clientID);
                    break;
                default:
                    break;
            }
        }

        private void ReceiveMovementPacket(ref PacketReader packet, string clientID)
        {
            DateTime sendTime = packet.ReadDateTime();

            Vector3 moveInput = Vector3.zero;
            float turnInput = 0f;
            bool jumpInput = false;

            ShadeMovementHeader Movements = (ShadeMovementHeader)packet.ReadByte();

            //Tipos de input:
            // 1 - MoveInput - > Vector3  001
            // 2 - TurnInput - > float    010
            // 3 - JumpInput - > bool     100

            //xx1    &     001 -> 001 == 001 (True)
            if ((Movements & ShadeMovementHeader.HORIZONTAL_MOVEMENT) == ShadeMovementHeader.HORIZONTAL_MOVEMENT)
                moveInput = packet.ReadVector3();
            //x1x    &     010 -> 010 == 010 (True)
            if ((Movements & ShadeMovementHeader.SPIN) == ShadeMovementHeader.SPIN)
                turnInput = packet.ReadSingle();
            //1xx    &     100 -> 100 == 100 (True)
            if ((Movements & ShadeMovementHeader.JUMP) == ShadeMovementHeader.JUMP)
                jumpInput = packet.ReadBoolean();

            if (clientsIDsConversionTable.ContainsKey(clientID))
            {
                ushort shadeID = clientsIDsConversionTable[clientID];
                if (shadesLookUpTable[shadeID].MovementPacketsCache.Count == 10)
                    shadesLookUpTable[shadeID].MovementPacketsCache.RemoveAt(0); // Caso, CASO, acumulem

                shadesLookUpTable[shadeID].MovementPacketsCache.Add(new MovementPacket(moveInput, turnInput, jumpInput, sendTime));
            }
        }
    }

    public struct ShadePacketPair
    {
        public Shade Shade;
        public List<MovementPacket> MovementPacketsCache;
        public ShadePacketPair(Shade shade)
        {
            Shade = shade;
            MovementPacketsCache = new List<MovementPacket>();
        }
    }
    public enum ShadeHeader : byte
    {
        ENTITY_OWNER_ID,
        MOVEMENT,
        SET_NAME
    }

    public enum ShadeMovementHeader : byte
    {
        HORIZONTAL_MOVEMENT = 1,//001
        JUMP = 2,//010
        SPIN = 4//100
    }
}
