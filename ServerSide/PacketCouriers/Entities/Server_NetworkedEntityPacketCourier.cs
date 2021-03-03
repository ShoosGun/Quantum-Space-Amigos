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

        private const short MAX_AMOUNT_OF_ENTITIES = 2048;
        protected NetworkedEntity[] entities = new NetworkedEntity[MAX_AMOUNT_OF_ENTITIES];


        private List<short> entitiesIDs = new List<short>();
        private List<short> availlablePositions = new List<short>();
        public static short LastID { get; private set; }

        static private List<EntityGameSnapshot> gameSnapshots = new List<EntityGameSnapshot>();
        const int MAX_SNAPSHOTS = 10; //Número máxmo fotos que se pode ter do jogo
        const int TIME_BETWEEN_SNAPSHOTS = 10; //(em fotos/loops) A cada 10 loops, tira-se uma foto

        private void Awake()
        {
            ReferenceFrameTransform = GameObject.Find("HomePlanet_graybox").transform;
            Server = GameObject.Find("QSAServer").GetComponent<ServerMod>()._serverSide;
        }

        private byte numberOfLoops = 0;
        private void FixedUpdate()
        {
            //Enviar o delta para cada shade, dependendo dos seus casos(ping)
            if (gameSnapshots.Count > 0)
            {
                PacketWriter packet = new PacketWriter();
                packet.Write((byte)Header.NET_ENTITY_PC);                      //
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
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns> Returns the ID of the synced entity</returns>
        public short AddEntitySync(NetworkedEntity entity)
        {
            short ID = MAX_AMOUNT_OF_ENTITIES;
            if (availlablePositions.Count > 0)
            {
                ID = availlablePositions[0];
                availlablePositions.RemoveAt(0);
            }
            else if(LastID < MAX_AMOUNT_OF_ENTITIES)
            {
                ID = LastID;
                LastID++;
            }

            if (ID != MAX_AMOUNT_OF_ENTITIES)
                entitiesIDs.Add(ID);

            entities[ID] = entity;
            entity.ID = ID;
            return ID;
        }

        public void RemoveEntitySync(short ID)
        {
            if (entities[ID] == null)
                return;

            entities[ID] = null;
            entitiesIDs.Remove(ID);
            availlablePositions.Add(ID);
        }


        public void Receive(ref PacketReader packet, string ClientID)
        {
        }
        

        public struct EntityGameSnapshot
        {
            public KeyValuePair<EntityTransform, short>[] TransformsWithIds;
            public SnapshotTick SnapshotTick;

            public EntityGameSnapshot(ref List<short> entitiesIds, Transform InertialReference, ref NetworkedEntity[] entities, SnapshotTick snapshotTick)
            {
                SnapshotTick = snapshotTick;

                TransformsWithIds = new KeyValuePair<EntityTransform, short>[entitiesIds.Count];
                for (int i = 0; i < entitiesIds.Count; i++)
                    TransformsWithIds[i] = new KeyValuePair<EntityTransform, short>(new EntityTransform(InertialReference.InverseTransformPoint(entities[entitiesIds[i]].rigidbody.position), entities[entitiesIds[i]].rigidbody.rotation), entitiesIds[i]);
            }
        }
    }
    public enum EntityHeader : byte
    {
        DELTA_SYNC,
        TRANSFORM_SYNC,
        //ENTITY_SYNC, //Para que a quantidade de shades nos dois lados seja igual (Para novos clientes)
        //ENTITY_DELTA_PLUS_SYNC, //  /\ (Para já conectados)
        //ENTITY_DELTA_MINUS_SYNC,// /  \ 
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
