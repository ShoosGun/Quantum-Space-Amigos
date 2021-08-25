using System;
using System.Collections.Generic;
using UnityEngine;
using ClientSide.Sockets;
using ClientSide.PacketCouriers.Entities;
using ClientSide.PacketCouriers.Shades.MovementConstraints;
using ClientSide.PacketCouriers.Essentials;

namespace ClientSide.PacketCouriers.Shades
{
    public class Client_ShadePacketCourier : MonoBehaviour, IPacketCourier , IEntityOwner
    {
        private Client client;
        //TODO trocar Client_DynamicPacketCourierHandler pelo "DynamicIO"
        private Client_DynamicPacketCourierHandler dynamicPacketCourierHandler;
        const string SHADE_LOCALIZATION_STRING = "ShadePacketCourier";
        public int HeaderValue { get; private set; }

        private Client_NetworkedEntityPacketCourier entityPacketCourier;
        private OWRigidbodyFollowsAnother playerConstrain;
        
        private Shade playerShade;
        private ushort playerShadeId;
        private bool hasReceivedId = false;
        private List<ushort> shadesIDs = new List<ushort>();
        private Dictionary<ushort, Shade> shadesLookUpTable = new Dictionary<ushort, Shade>();

        bool connectedToServer = false;

        /// <summary>
        /// The ID for using the NetworkedEntityPC
        /// </summary>
        private byte SHADEPC_ID = 255;

        void Start()
        {
            playerConstrain = GameObject.FindGameObjectWithTag("Player").AddComponent<OWRigidbodyFollowsAnother>();

            GameObject go = GameObject.Find("QSAClient");
            client = go.GetComponent<ClientMod>()._clientSide;
            dynamicPacketCourierHandler = client.dynamicPacketCourierHandler;
            dynamicPacketCourierHandler.SetPacketCourier(SHADE_LOCALIZATION_STRING, OnReceiveHeaderValue);

            entityPacketCourier = go.GetComponent<Client_NetworkedEntityPacketCourier>();

            client.Connection += Client_Connection;
            client.Disconnection += Client_Disconnection;
        }
        void OnDestroy()
        {
            client.Connection -= Client_Connection;
            client.Disconnection -= Client_Disconnection;
        }

        public ReadPacketHolder.ReadPacket OnReceiveHeaderValue(int HeaderValue)
        {
            this.HeaderValue = HeaderValue;
            return Receive;
        }

        private void Client_Connection()
        {
            connectedToServer = true;
            Debug.Log("Conectados no Servidor!");
        }


        private void Client_Disconnection()
        {
            playerShade = null;
            playerConstrain.Reset();

            foreach (ushort ID in shadesIDs)
                shadesLookUpTable[ID].DestroyShade();

            shadesIDs.Clear();
            shadesLookUpTable.Clear();

            hasReceivedId = false;
            connectedToServer = false;
            Debug.Log("Desconectados do servidor");
        }
        
        public NetworkedEntity OnAddEntity(ushort id)
        {
            shadesIDs.Add(id);
            if (!shadesLookUpTable.ContainsKey(id))
            {
                Shade addedShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>().GenerateShade();
                shadesLookUpTable.Add(id, addedShade);

                if (id == playerShadeId)
                {
                    playerShade = addedShade;
                    //playerConstrain.SetConstrain(playerShade.GetAttachedOWRigidbody());
                }

                Debug.Log($"Nova Shade! ID = {id}");
                addedShade.GoToNetworkedMode();
                Debug.Log("Shade em modo de sincronizacao");

                return addedShade;
            }
            else
                return shadesLookUpTable[id];
        }

        public void OnRemoveEntity(ushort id)
        {
            shadesIDs.Remove(id);
            shadesLookUpTable[id].GoToSimulationMode();
            shadesLookUpTable[id].DestroyShade();
            shadesLookUpTable.Remove(id);
        }

        void FixedUpdate()
        {
            //if (playerShade != null)
            //{
                //Enviando os botões pressionados pelo cliente ao servidor
                PacketWriter pk = new PacketWriter();
                pk.Write((byte)ShadeHeader.MOVEMENT);
                pk.Write(DateTime.UtcNow);

                pk.Write((byte)(ShadeMovementHeader.HORIZONTAL_MOVEMENT | ShadeMovementHeader.SPIN | ShadeMovementHeader.JUMP));

                pk.Write(new Vector3(OWInput.GetAxis(GroundInput.moveX), 0f, OWInput.GetAxis(GroundInput.moveZ)));
                pk.Write(OWInput.GetAxis(GroundInput.turn));
                pk.Write(OWInput.GetButtonDown(GroundInput.jump));

                dynamicPacketCourierHandler.DynamicPacketIO.SendPackedData((byte)HeaderValue, pk.GetBytes());
            //}
            if (connectedToServer && !hasReceivedId && (int)(Time.realtimeSinceStartup * 10) % 10 == 0)
            {
                PacketWriter pak = new PacketWriter();
                pak.Write((byte)ShadeHeader.ENTITY_OWNER_ID);
                dynamicPacketCourierHandler.DynamicPacketIO.SendPackedData((byte)HeaderValue, pak.GetBytes());
            }
        }

        public void Receive(byte[] data)
        {
            PacketReader packet = new PacketReader(data);
            switch ((ShadeHeader)packet.ReadByte())
            {
                case ShadeHeader.ENTITY_OWNER_ID:
                    byte ownerId = packet.ReadByte();
                    if (!hasReceivedId)
                    {
                        hasReceivedId = true;
                        SHADEPC_ID = ownerId;
                        entityPacketCourier.SetEntityOwner(SHADEPC_ID, this);
                    }

                    ushort shadeID = packet.ReadUInt16();
                    if (shadeID != playerShadeId)
                    {
                        playerShadeId = shadeID;
                        if(shadesLookUpTable.ContainsKey(playerShadeId))
                            playerShade = shadesLookUpTable[playerShadeId];
                    }
                    PacketWriter pk = new PacketWriter();
                    pk.Write((byte)EntityHeader.ENTITY_SYNC);
                    dynamicPacketCourierHandler.DynamicPacketIO.SendPackedData((byte)entityPacketCourier.HeaderValue, pk.GetBytes());
                    Debug.Log($"Recebendo o ID do PC = {SHADEPC_ID} e da nossa shade {shadeID}");

                    break;

                default:
                    break;
            }
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
