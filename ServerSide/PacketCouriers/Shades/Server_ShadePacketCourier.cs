using System;
using System.Collections.Generic;
using UnityEngine;
using ServerSide.Sync;
using ServerSide.Sockets;
using ServerSide.Sockets.Servers;

namespace ServerSide.PacketCouriers.Shades
{
    public class Server_ShadePacketCourier : MonoBehaviour, IPacketCourier
    {
        private Server server;

        private Transform thTransform;

        private List<ShadePacketPair> clientShades = new List<ShadePacketPair>();
        private Dictionary<string, ShadePacketPair> clientsShadesLookUpTable = new Dictionary<string, ShadePacketPair>();

        private List<ShadeGameSnapshot> gameSnapshots = new List<ShadeGameSnapshot>();
        const int MAX_SNAPSHOTS = 10; //Número máxmo fotos que se pode ter do jogo
        const int TIME_BETWEEN_SNAPSHOTS = 10; //(em fotos/loops) A cada 10 loops, tira-se uma foto
        void Start()
        {
            //thTransform = GameObject.Find("TimberHearth_Pivot").GetComponent<Transform>();
            server = GameObject.Find("QSAServer").GetComponent<ServerMod>()._serverSide;

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

        int numberOfLoops = 0;
        ShadeGameSnapshot latestSnapshot;
        void FixedUpdate()
        {
            foreach (ShadePacketPair pair in clientShades)
            {
                if (pair.MovementPacketsCache.Count > 0)
                {
                    if (pair.Shade.MovementModel != null)
                        pair.Shade.MovementModel.SetNewPacket(pair.MovementPacketsCache[0]);
                    pair.MovementPacketsCache.RemoveAt(0);
                }
                else if (pair.Shade.MovementModel != null)
                    pair.Shade.MovementModel.SetNewPacket(new MovementPacket(Vector3.zero, 0f, false, DateTime.UtcNow));
            }

            //Foto do jogo nesse momento
            if (numberOfLoops % TIME_BETWEEN_SNAPSHOTS == 0)
            {
                if (gameSnapshots.Count > MAX_SNAPSHOTS)
                    gameSnapshots.RemoveAt(0);
                gameSnapshots.Add(new ShadeGameSnapshot(clientShades));
            }
            numberOfLoops++;

            //Enviar o delta para cada shade, dependendo dos seus casos(ping)
            if(latestSnapshot.SnapshotTime!= null)
            foreach(var cl in latestSnapshot.TransformsWithIds)
            {
                    ShadeTransform deltaTransform = new ShadeTransform(clientsShadesLookUpTable[cl.Value].Shade.transform) - cl.Key;

                    PacketWriter packet = new PacketWriter();
                    packet.Write((byte)Header.SHADE_PC);
                    packet.Write((byte)ShadeHeader.TRANSFORM);

                    packet.Write(DateTime.UtcNow);
                    packet.Write(deltaTransform.Position);
                    packet.Write(deltaTransform.Rotation);
                    server.Send(cl.Value, packet.GetBytes());
            }
            latestSnapshot = new ShadeGameSnapshot(clientShades);
        }

        public void Receive(ref PacketReader packet, string clientID)
        {
            switch ((ShadeHeader)packet.ReadByte())
            {
                case ShadeHeader.MOVEMENT:
                    ReceiveMovementPacket(ref packet, clientID);
                    break;

                case ShadeHeader.SET_NAME:
                    if (clientsShadesLookUpTable.ContainsKey(clientID))
                    {
                        clientsShadesLookUpTable[clientID].Shade.Name = packet.ReadString();
                    }
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

            //Tipos de imput:
            // Falar quantos deles [1,3] vão vir
            // 1 - MoveInput - > Vector3
            // 2 - TurnInput - > float
            // 3 - JumpInput - > bool
            for (byte amountOfMovement = packet.ReadByte(); amountOfMovement > 0; amountOfMovement--)
            {
                switch ((ShadeMovementHeader)packet.ReadByte())
                {
                    case ShadeMovementHeader.HORIZONTAL_MOVEMENT:
                        moveInput = packet.ReadVector3();
                        break;

                    case ShadeMovementHeader.SPIN:
                        turnInput = packet.ReadSingle();
                        break;

                    case ShadeMovementHeader.JUMP:
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


    public struct ShadeGameSnapshot
    {
        public KeyValuePair<ShadeTransform, string>[] TransformsWithIds;
        public DateTime SnapshotTime;

        public ShadeGameSnapshot(List<Shade> shades)
        {
            SnapshotTime = DateTime.UtcNow;

            TransformsWithIds = new KeyValuePair<ShadeTransform, string>[shades.Count];
            for (int i = 0; i < shades.Count; i++)
                TransformsWithIds[i] = new KeyValuePair<ShadeTransform, string>(new ShadeTransform(shades[i].transform), shades[i].ClientID);
        }
        public ShadeGameSnapshot(List<ShadePacketPair> shades)
        {
            SnapshotTime = DateTime.UtcNow;

            TransformsWithIds = new KeyValuePair<ShadeTransform, string>[shades.Count];
            for (int i = 0; i < shades.Count; i++)
                TransformsWithIds[i] = new KeyValuePair<ShadeTransform, string>(new ShadeTransform(shades[i].Shade.transform), shades[i].Shade.ClientID);
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
    public struct ShadeTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public ShadeTransform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
        public ShadeTransform(Transform transform)
        {
            Position = transform.position;
            Rotation = transform.rotation;
        }
        

        public static ShadeTransform operator -(ShadeTransform right, ShadeTransform left)
        {
            return new ShadeTransform(right.Position - left.Position,right.Rotation*Quaternion.Inverse(left.Rotation));
        }
    }
    public enum ShadeHeader : byte
    {
        MOVEMENT,
        SET_NAME,
        TRANSFORM
    }

    public enum ShadeMovementHeader : byte
    {
        HORIZONTAL_MOVEMENT,
        JUMP,
        SPIN
    }
}
