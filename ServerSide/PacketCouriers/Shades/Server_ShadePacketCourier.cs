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
        //Achar o que aki está null
        private Server server;

        private Shade serverShade;

        private Transform solarSystemTransform;

        private List<ShadePacketPair> clientShades = new List<ShadePacketPair>();
        private Dictionary<string, ShadePacketPair> clientsShadesLookUpTable = new Dictionary<string, ShadePacketPair>();

        private List<ShadeGameSnapshot> gameSnapshots = new List<ShadeGameSnapshot>();
        const int MAX_SNAPSHOTS = 10; //Número máxmo fotos que se pode ter do jogo
        const int TIME_BETWEEN_SNAPSHOTS = 10; //(em fotos/loops) A cada 10 loops, tira-se uma foto
        void Start()
        {
            solarSystemTransform = GameObject.Find("HomePlanet_graybox").transform; //Depois trocar por SolarSystem_Root ou algo assim
            server = GameObject.Find("QSAServer").GetComponent<ServerMod>()._serverSide;

            server.NewConnectionID += Server_NewConnectionID;
            server.DisconnectionID += Server_DisconnectionID;

            serverShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
            serverShade.ClientID = "server";
            serverShade.gameObject.collider.enabled = false;
            serverShade.gameObject.AddComponent<MovementConstraints.OWRigidbodyFollowsAnother>();

            ShadePacketPair shadePacketPair = new ShadePacketPair(serverShade);
            clientShades.Add(shadePacketPair);
            clientsShadesLookUpTable.Add("server", shadePacketPair);
        }

        void OnDestroy()
        {
            server.NewConnectionID -= Server_NewConnectionID;
            server.DisconnectionID -= Server_DisconnectionID;
        }

        private void Server_NewConnectionID(string clientID)
        {
            
            //Mandar para quem chegou quem são os conectados
            PacketWriter packetForClients = new PacketWriter();

            packetForClients.Write((byte)Header.SHADE_PC);
            packetForClients.Write((byte)ShadeHeader.SHADE_SYNC);
            packetForClients.Write((byte)clientShades.Count); 
            packetForClients.Write(clientID);
            foreach (var cl in clientShades)
                packetForClients.Write(cl.Shade.ClientID);

            server.Send(clientID, packetForClients.GetBytes()); //Tem PacketWriter.Close() incluso


            //Mandar para todos os conectados quem acabou de chegar (menos para quem chegou)
            packetForClients = new PacketWriter();
            packetForClients.Write((byte)Header.SHADE_PC);
            packetForClients.Write((byte)ShadeHeader.SHADE_DELTA_PLUS_SYNC);
            packetForClients.Write(clientID);
            server.SendAll(packetForClients.GetBytes(), clientID);
            //---

            //Criando e guardando a shade do cliente que acabou de conectar

            Shade newShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
            newShade.ClientID = clientID;

            ShadePacketPair shadePacketPair = new ShadePacketPair(newShade);

            clientShades.Add(shadePacketPair);
            clientsShadesLookUpTable.Add(newShade.ClientID, shadePacketPair);

            Debug.Log($"Nova Shade!\n Quantidade de Shades no momento: {clientShades.Count}");
        }


        private void Server_DisconnectionID(string clientID)
        {
            if (clientsShadesLookUpTable.ContainsKey(clientID))
            {
                clientsShadesLookUpTable[clientID].Shade.DestroyShade();
                clientsShadesLookUpTable[clientID].MovementPacketsCache.Clear();

                var packetForClients = new PacketWriter();
                packetForClients.Write((byte)Header.SHADE_PC);
                packetForClients.Write((byte)ShadeHeader.SHADE_DELTA_MINUS_SYNC);
                packetForClients.Write(clientID);
                server.SendAll(packetForClients.GetBytes());

                //Remover da lista primeiro para não usarmos algo nulo para apagar essa cópia
                clientShades.Remove(clientsShadesLookUpTable[clientID]);
                clientsShadesLookUpTable.Remove(clientID);
            }
        }

        private bool wasThereOWRigdb = false;
        void Update()
        {
            if (serverShade.GetAttachedOWRigidbody() != null && !wasThereOWRigdb)
            {
                serverShade.GetComponent<MovementConstraints.OWRigidbodyFollowsAnother>().SetConstrain(GameObject.FindGameObjectWithTag("Player").GetAttachedOWRigidbody());
                wasThereOWRigdb = true;
            }
        }

        int numberOfLoops = 0;
        void FixedUpdate()
        {
            //Dá a movimentação recebida dos clientes para suas shades
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

            //Enviar o delta para cada shade, dependendo dos seus casos(ping)
            if (gameSnapshots.Count > 0)
            {
                PacketWriter packet = new PacketWriter();
                packet.Write((byte)Header.SHADE_PC);
                packet.Write((byte)ShadeHeader.TRANSFORM_SYNC); //Só position sync por enquanto
                packet.Write(DateTime.UtcNow);     //Referencial no passado(do cl.Key)       Referencial do presente (do lookUpTable)
                packet.Write((byte)gameSnapshots[0].TransformsWithIds.Length);

                foreach (var cl in gameSnapshots[0].TransformsWithIds)
                {
                    packet.Write(cl.Value); //ID da shade
                    packet.Write(cl.Key.Position);     //InertialReference.InverseTransformPoint(shades[i].Shade.transform.position)
                    packet.Write(cl.Key.Rotation);
                }

                server.SendAll(packet.GetBytes()); //Mandar para todos
                gameSnapshots.RemoveAt(0);
            }

            //Foto do jogo nesse momento
            if (numberOfLoops % TIME_BETWEEN_SNAPSHOTS == 0)
            {
                if (gameSnapshots.Count > MAX_SNAPSHOTS)
                    gameSnapshots.RemoveAt(0);
                gameSnapshots.Add(new ShadeGameSnapshot(clientShades, solarSystemTransform));
            }
            numberOfLoops++;
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

                case ShadeHeader.SHADE_SYNC:
                    PacketWriter packetForClients = new PacketWriter();

                    packetForClients.Write((byte)Header.SHADE_PC);
                    packetForClients.Write((byte)ShadeHeader.SHADE_SYNC);
                    packetForClients.Write((byte)clientShades.Count);
                    packetForClients.Write(clientID);
                    foreach (var cl in clientShades)
                        packetForClients.Write(cl.Shade.ClientID);
                    server.Send(clientID, packetForClients.GetBytes());

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

        public ShadeGameSnapshot(List<Shade> shades, Transform InertialReference)
        {
            SnapshotTime = DateTime.UtcNow;
            
            TransformsWithIds = new KeyValuePair<ShadeTransform, string>[shades.Count];
            for (int i = 0; i < shades.Count; i++)
                TransformsWithIds[i] = new KeyValuePair<ShadeTransform, string>(new ShadeTransform(InertialReference.InverseTransformPoint(shades[i].rigidbody.position), shades[i].rigidbody.rotation), shades[i].ClientID);
        }
        public ShadeGameSnapshot(List<ShadePacketPair> shades, Transform InertialReference)
        {
            SnapshotTime = DateTime.UtcNow;

            TransformsWithIds = new KeyValuePair<ShadeTransform, string>[shades.Count];
            for (int i = 0; i < shades.Count; i++)
                TransformsWithIds[i] = new KeyValuePair<ShadeTransform, string>(new ShadeTransform(InertialReference.InverseTransformPoint(shades[i].Shade.rigidbody.position), shades[i].Shade.rigidbody.rotation * Quaternion.Inverse(InertialReference.rotation)), shades[i].Shade.ClientID);
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
        public ShadeTransform(Rigidbody rigidbody)
        {
            Position = rigidbody.position;
            Rotation = rigidbody.rotation;
        }
        public ShadeTransform(Transform transform)
        {
            Position = transform.position;
            Rotation = transform.rotation;
        }

        public static ShadeTransform operator -(ShadeTransform right, ShadeTransform left)
        {
            return new ShadeTransform(right.Position - left.Position, right.Rotation * Quaternion.Inverse(left.Rotation));
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
        HORIZONTAL_MOVEMENT=1,//001
        JUMP=2,//010
        SPIN=4//100
    }
}
