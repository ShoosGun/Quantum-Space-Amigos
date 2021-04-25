using System;
using System.Collections.Generic;
using UnityEngine;
using ServerSide.Sockets.Servers;
using ServerSide.Sockets;

namespace ServerSide.PacketCouriers.Entities
{
    public struct SnapshotTick //Até descobir uma maneira de "renomear" int/DateTime/... vai ser assim (lembro que tinha isso em C++, mas n sei como fazer)
    {
        public DateTime Tick; // Se formos ter que mudar a forma que contamos os ticks, só teremos que mudar aki
    }
    public class Server_NetworkedEntityPacketCourier : MonoBehaviour , IPacketCourier
    {
        private Transform ReferenceFrameTransform;
        private Server Server;

        private const ushort MAX_AMOUNT_OF_ENTITIES = 2048;
        protected NetworkedEntity[] entities = new NetworkedEntity[MAX_AMOUNT_OF_ENTITIES];

        static byte lastEntityOwnerID = 0;

        private List<ushort> entitiesIDs = new List<ushort>();
        private List<ushort> availlablePositions = new List<ushort>();
        public static ushort LastID { get; private set; }

        static private List<EntityGameSnapshot> gameSnapshots = new List<EntityGameSnapshot>();
        const int MAX_SNAPSHOTS = 10; //Número máxmo fotos que se pode ter do jogo
        const int TIME_BETWEEN_SNAPSHOTS = 10; //(em fotos/loops) A cada 10 loops, tira-se uma foto

        private void Start()
        {
            ReferenceFrameTransform = GameObject.Find("TimberHearth_Body").transform;
            Server = GameObject.Find("QSAServer").GetComponent<ServerMod>()._serverSide;
        }

        private byte numberOfLoops = 0;
        private void FixedUpdate()
        {
            //Enviar o delta para cada shade, dependendo dos seus casos(ping)
            if (gameSnapshots.Count > 0)
            {
                PacketWriter packet = new PacketWriter();
                packet.Write((byte)Header.Header_Size + 1);                      //
                packet.Write((byte)EntityHeader.TRANSFORM_SYNC); //Só position sync por enquanto
                packet.Write(gameSnapshots[0].SnapshotTick.Tick);     //Referencial no passado(do cl.Key)       Referencial do presente (do lookUpTable)
                packet.Write((short)gameSnapshots[0].TransformsWithIds.Length);

                foreach (var cl in gameSnapshots[0].TransformsWithIds)
                {
                    packet.Write(cl.Value); //ID da shade
                    packet.Write(cl.Key.Position);     //InertialReference.InverseTransformPoint(shades[i].Shade.transform.position)
                    packet.Write(cl.Key.Rotation);
                }

                Server.SendAll(packet.GetBytes()); //Mandar para todos
                gameSnapshots.RemoveAt(0);
            }

            //Foto do jogo nesse momento
            if (numberOfLoops % TIME_BETWEEN_SNAPSHOTS == 0)
            {
                numberOfLoops = 0;
                if (gameSnapshots.Count > MAX_SNAPSHOTS)
                    gameSnapshots.RemoveAt(0);
                SnapshotTick snapshotTick;
                snapshotTick.Tick = DateTime.UtcNow;
                gameSnapshots.Add(new EntityGameSnapshot(ref entitiesIDs, ReferenceFrameTransform, ref entities, snapshotTick));
            }
            numberOfLoops++;
        }


        /// <summary>
        /// Gives a unique id for owning entities, if it gives 255 it means that it is out of ids
        /// </summary>
        /// <returns></returns>
        public byte AddEntityOwner()
        {
            if (lastEntityOwnerID < 254)
            {
                byte thisOwnerID = lastEntityOwnerID;
                lastEntityOwnerID++;
                Debug.Log($"Novo dono de entidades ID = {thisOwnerID}");
                return thisOwnerID;
            }
            else
                return 255;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns> Returns the ID of the synced entity</returns>
        public ushort AddEntitySync(NetworkedEntity entity, byte ownerID)
        {
            Debug.Log($"Nova entidade de dono com ID = {ownerID}");

            if (ownerID > 255)
                return MAX_AMOUNT_OF_ENTITIES;

            ushort ID = MAX_AMOUNT_OF_ENTITIES;

            if (availlablePositions.Count > 0)
            {
                ID = availlablePositions[0];
                availlablePositions.RemoveAt(0);
            }
            else if (LastID < MAX_AMOUNT_OF_ENTITIES)
            {
                ID = LastID;
                LastID++;
            }

            if (ID != MAX_AMOUNT_OF_ENTITIES)
            {
                entitiesIDs.Add(ID);

                entities[ID] = entity;
                entity.ID = ID;
                entity.PCOwner = ownerID;

                //Mandando o surgimento uma entidade para os clientes
                PacketWriter packet = new PacketWriter();
                packet.Write((byte)Header.Header_Size + 1);
                packet.Write((byte)EntityHeader.ENTITY_DELTA_PLUS_SYNC);
                packet.Write(ID);
                packet.Write(ownerID);
                Server.SendAll(packet.GetBytes());
            }

            return ID;
        }

        public void RemoveEntitySync(ushort ID)
        {
            Debug.Log($"Removendo a entidade de ID = {ID}");

            if (entities[ID] == null)
                return;
            entities[ID] = null;
            entitiesIDs.Remove(ID);
            availlablePositions.Add(ID);

            //Mandando o surgimento uma entidade para os clientes
            PacketWriter packet = new PacketWriter();
            packet.Write((byte)Header.Header_Size + 1);
            packet.Write((byte)EntityHeader.ENTITY_DELTA_MINUS_SYNC);
            packet.Write(ID);
            Server.SendAll(packet.GetBytes());
        }

        public void WriteEntitySync(ref PacketWriter packet)
        {
            Debug.Log($"Fazendo o sync das entidades");
            packet.Write((byte)Header.Header_Size + 1);
            packet.Write((byte)EntityHeader.ENTITY_SYNC);
            packet.Write((ushort)entitiesIDs.Count);
            foreach(ushort id in entitiesIDs)
            {
                packet.Write(id); //O id da entidade
                packet.Write(entities[id].PCOwner); //O id de quem pediu para sincroniza-la
            }
        }


        public void Receive(ref PacketReader packet, string ClientID)
        {
            switch ((EntityHeader)packet.ReadByte())
            {
                case EntityHeader.ENTITY_SYNC:
                    PacketWriter packetToSend = new PacketWriter();
                    WriteEntitySync(ref packetToSend);
                    Server.Send(ClientID, packetToSend.GetBytes());
                    break;
                default:
                    break;
            }
        }
        

        public struct EntityGameSnapshot
        {
            public KeyValuePair<EntityTransform, ushort>[] TransformsWithIds;
            public SnapshotTick SnapshotTick;

            public EntityGameSnapshot(ref List<ushort> entitiesIds, Transform InertialReference, ref NetworkedEntity[] entities, SnapshotTick snapshotTick)
            {
                SnapshotTick = snapshotTick;

                TransformsWithIds = new KeyValuePair<EntityTransform, ushort>[entitiesIds.Count];
                for (int i = 0; i < entitiesIds.Count; i++)
                    TransformsWithIds[i] = new KeyValuePair<EntityTransform, ushort>(new EntityTransform(InertialReference.InverseTransformPoint(entities[entitiesIds[i]].transform.position), Quaternion.Inverse(InertialReference.transform.rotation) * entities[entitiesIds[i]].transform.rotation), entitiesIds[i]);
            }
        }
    }
    public enum EntityHeader : byte
    {
        DELTA_SYNC,
        TRANSFORM_SYNC,
        ENTITY_SYNC, //Para que a quantidade de shades nos dois lados seja igual (Para novos clientes)
        ENTITY_DELTA_PLUS_SYNC, //  /\ (Para já conectados)
        ENTITY_DELTA_MINUS_SYNC,// /  \ 
    }


    public struct EntityTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public EntityTransform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
        public EntityTransform(Rigidbody rigidbody)
        {
            Position = rigidbody.position;
            Rotation = rigidbody.rotation;
        }
        public EntityTransform(Transform transform)
        {
            Position = transform.position;
            Rotation = transform.rotation;
        }

        public static EntityTransform operator -(EntityTransform right, EntityTransform left)
        {
            return new EntityTransform(right.Position - left.Position, right.Rotation * Quaternion.Inverse(left.Rotation));
        }
    }
}
