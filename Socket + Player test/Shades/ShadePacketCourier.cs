using System;
using System.Collections.Generic;
using UnityEngine;
using ServerSide.Sync;
using ServerSide.Sockets;
using ServerSide.Sockets.Servers;

namespace ServerSide.Shades
{
    public class ShadePacketCourier : MonoBehaviour, IPacketCourier
    {
        private List<ShadePacketPair> clientShades = new List<ShadePacketPair>();

        private Dictionary<string, ShadePacketPair> clientsShadesLookUpTable = new Dictionary<string, ShadePacketPair>();

        private Server server;

        void Start()
        {
            server = GameObject.Find("ShadeTest").GetComponent<ServerMod>()._serverSide;

            server.NewConnectionID += Server_NewConnectionID;
            server.DisconnectionID += Server_DisconnectionID;
        }

        void Destroy()
        {
            server.NewConnectionID -= Server_NewConnectionID;
            server.DisconnectionID -= Server_DisconnectionID;
        }

        private void Server_NewConnectionID(string clientID)
        {
            Shade newShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
            newShade.ClientID = clientID;

            ShadePacketPair shadePacketPair = new ShadePacketPair(newShade);
            clientShades.Add(shadePacketPair);
            clientsShadesLookUpTable.Add(clientID, shadePacketPair);
            Debug.Log("Nova Shade!\n" + $" Quantidade de Shades no momento: {clientShades.Count}");
        }


        private void Server_DisconnectionID(string clientID)
        {
            if (clientsShadesLookUpTable.ContainsKey(clientID))
            {
                clientsShadesLookUpTable[clientID].Shade.DestroyShade();
                clientsShadesLookUpTable[clientID].MovementPacketsCache.Clear();

                //Remover da lista primeiro para não usarmos algo nulo para apagar essa cópia
                clientShades.Remove(clientsShadesLookUpTable[clientID]);
                clientsShadesLookUpTable.Remove(clientID);
            }
        }


        void FixedUpdate()
        {
            foreach (ShadePacketPair pair in clientShades)
            {
                if (pair.MovementPacketsCache.Count > 0)
                {
                    if(pair.Shade.MovementModel != null)
                        pair.Shade.MovementModel.SetNewPacket(pair.MovementPacketsCache[0]);
                    pair.MovementPacketsCache.RemoveAt(0);
                }
                else if(pair.Shade.MovementModel != null)
                    pair.Shade.MovementModel.SetNewPacket(new MovementPacket(Vector3.zero, 0f, false, DateTime.UtcNow));
            }  
            
        }

        public void Receive(ref PacketReader packet, string clientID)
        {
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
                switch ((MovementHeader)packet.ReadByte())
                {
                    case MovementHeader.HORIZONTAL_MOVEMENT:
                        moveInput = packet.ReadVector3();
                        break;

                    case MovementHeader.SPIN:
                        turnInput = packet.ReadSingle();
                        break;

                    case MovementHeader.JUMP:
                        jumpInput = packet.ReadBoolean();
                        break;

                    default:
                        break;
                }
            }
            if (clientsShadesLookUpTable.ContainsKey(clientID))
            {
                if (clientsShadesLookUpTable[clientID].MovementPacketsCache.Count == 10)
                    clientsShadesLookUpTable[clientID].MovementPacketsCache.RemoveAt(0); // Caso, CASO, acumulem

                clientsShadesLookUpTable[clientID].MovementPacketsCache.Add(new MovementPacket(moveInput, turnInput, jumpInput, sendTime));
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
    
    public enum MovementHeader : byte
    {
        HORIZONTAL_MOVEMENT,
        JUMP,
        SPIN
    }
}
