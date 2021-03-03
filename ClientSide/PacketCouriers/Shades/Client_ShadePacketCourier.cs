using System;
using System.Collections.Generic;
using UnityEngine;
using ClientSide.Sockets;
using ClientSide.PacketCouriers.Entities;

namespace ClientSide.PacketCouriers.Shades
{
    public class Client_ShadePacketCourier : MonoBehaviour, IPacketCourier
    {
        private Client client;
        private Client_NetworkedEntityPacketCourier entityPacketCourier;
        
        private Shade playerShade;
        private bool hasReceivedId = false;
        private List<short> shadesIDs = new List<short>();
        private Dictionary<short, Shade> shadesLookUpTable = new Dictionary<short, Shade>();

        void Start()
        {
            GameObject go = GameObject.Find("QSAClient");
            client = go.GetComponent<ClientMod>()._clientSide;
            entityPacketCourier = go.GetComponent<Client_NetworkedEntityPacketCourier>();

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

            foreach(short ID in shadesIDs)
                shadesLookUpTable[ID].DestroyShade();

            shadesIDs.Clear();
            shadesLookUpTable.Clear();

            hasReceivedId = false;
            Debug.Log("Desconectados do servidor");
        }
        
        void FixedUpdate()
        {
            if (hasReceivedId)
            {

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
            short shadeID;
            switch ((ShadeHeader)packet.ReadByte())
            {
                case ShadeHeader.SHADE_SYNC:
                    int amountOfShades = packet.ReadByte();
                    shadeID = packet.ReadInt16();
                    shadesIDs.Add(shadeID);
                    if (shadeID != playerShade.ID || !hasReceivedId)
                    {
                        hasReceivedId = true;
                        shadesLookUpTable.Add(shadeID, playerShade);
                        entityPacketCourier.SetEntitySync(playerShade, shadeID);
                    }

                    for (int i = 0; i < amountOfShades; i++)
                        ReceiveNewShade(ref packet);

                    Debug.Log($"Recebendo as novas shades que existem! Temos no total agora: {shadesLookUpTable.Keys.Count}; sendo que nosso ID é {playerShade.ID}!");
                    break;

                case ShadeHeader.SHADE_DELTA_MINUS_SYNC:
                    shadeID = packet.ReadInt16();
                    shadesIDs.Remove(shadeID);
                    shadesLookUpTable[shadeID].DestroyShade();
                    shadesLookUpTable.Remove(shadeID);
                    entityPacketCourier.RemoveEntitySync(shadeID);
                    break;

                case ShadeHeader.SHADE_DELTA_PLUS_SYNC:
                    ReceiveNewShade(ref packet);
                    break;

                default:
                    break;
            }
        }

        private void ReceiveNewShade(ref PacketReader packet)
        {
            short shadeID = packet.ReadInt16();
            shadesIDs.Add(shadeID);
            Shade addedShade;
            if (!shadesLookUpTable.ContainsKey(shadeID))
            {
                addedShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
                shadesLookUpTable.Add(shadeID, addedShade);
                entityPacketCourier.SetEntitySync(addedShade, shadeID);
            }
        }
    }

    public enum ShadeHeader : byte
    {
        MOVEMENT,
        SET_NAME,
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
