using System;
using System.Collections.Generic;
using UnityEngine;
using ClientSide.Sockets;


namespace ClientSide.PacketCouriers.Entities
{
    public struct SnapshotTick //Até descobir uma maneira de "renomear" int/DateTime/... vai ser assim
    {
        public DateTime Tick; // Se formos ter que mudar a forma que contamos os ticks, só teremos que mudar aki
    }

    public class Client_NetworkedEntityPacketCourier : MonoBehaviour, IPacketCourier
    {
        private Transform ReferenceFrameTransform;
        private Client Client;

        public const short MAX_AMOUNT_OF_ENTITIES = 2048;
        protected NetworkedEntity[] entities = new NetworkedEntity[MAX_AMOUNT_OF_ENTITIES];
        private List<short> entitiesIDs = new List<short>();


        private List<SnapshotTick> snapshotsTicks = new List<SnapshotTick>();
        private List<EntityGameSnapshot> serverSnapshots = new List<EntityGameSnapshot>();
        private const int MAX_SNAPSHOTS = 3; //Número máxmo fotos que se pode ter do jogo

        private void Awake()
        {
            ReferenceFrameTransform = GameObject.Find("HomePlanet_graybox").transform;
            Client = GameObject.Find("QSAClient").GetComponent<ClientMod>()._clientSide;
            Client.Disconnection += Client_Disconnection;
        }
        private void OnDestroy()
        {
            Client.Disconnection -= Client_Disconnection;
        }

        private void Client_Disconnection()
        {
            foreach (short ID in entitiesIDs)
            {
                entities[ID].ID = MAX_AMOUNT_OF_ENTITIES;
                entities[ID] = null;
            }
            entitiesIDs.Clear();
            serverSnapshots.Clear();
            snapshotsTicks.Clear();
        }

        //private SnapshotTick ourCurrentSnapshot;
        private void FixedUpdate()
        {
            //Ler as snapshots vindas do servidor
            foreach (EntityGameSnapshot gameSnapshot in serverSnapshots)
            {
                foreach(EntityTransformWithId transformWithId in gameSnapshot.EntityTransformWithIds)
                {
                    if (entitiesIDs.Contains(transformWithId.ID))
                    {
                        NetworkedEntity networkedEntity = entities[transformWithId.ID];
                        //Delta Syncs
                        if (transformWithId.IsDeltaSync)
                        {
                            networkedEntity.rigidbody.position = ReferenceFrameTransform.TransformDirection(transformWithId.EntityTransform.Position) + networkedEntity.rigidbody.position;
                            networkedEntity.rigidbody.rotation = transformWithId.EntityTransform.Rotation * networkedEntity.rigidbody.rotation;
                        }
                        else
                        {
                            networkedEntity.rigidbody.position = ReferenceFrameTransform.TransformPoint(transformWithId.EntityTransform.Position);
                            networkedEntity.rigidbody.rotation = transformWithId.EntityTransform.Rotation * ReferenceFrameTransform.rotation;
                        }
                    }
                }
            }
            if (serverSnapshots.Count > 0)
                serverSnapshots.Clear();
        }

        /// <summary>
        /// When you receive the id of the synced entity, place it here with its ID for syncing
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ID">The ID received for this entity</param>
        /// <returns></returns>
        public void SetEntitySync(NetworkedEntity entity, short ID)
        {
            if (ID < MAX_AMOUNT_OF_ENTITIES)
            {
                entitiesIDs.Add(ID);
                entities[ID] = entity;
                entity.ID = ID;
            }
        }

        /// <summary>
        /// When you receive the id of a no longer synced entity, place it here with its ID for stop syncing
        /// </summary>
        /// <param name="ID">The ID of this entity</param>
        /// <returns></returns>
        public void RemoveEntitySync(short ID)
        {
            if (entities[ID] == null)
                return;
            
            entities[ID].ID = MAX_AMOUNT_OF_ENTITIES;
            entities[ID] = null;
            entitiesIDs.Remove(ID);
        }


        public void Receive(ref PacketReader packet)
        {
            switch ((EntityHeader)packet.ReadByte())
            {
                case EntityHeader.DELTA_SYNC:
                    if (serverSnapshots.Count > MAX_SNAPSHOTS)
                        serverSnapshots.RemoveAt(0);
                    serverSnapshots.Add(ReadEntityGameSnapshots(ref packet, true));
                    break;

                case EntityHeader.TRANSFORM_SYNC:
                    if (serverSnapshots.Count > MAX_SNAPSHOTS)
                        serverSnapshots.RemoveAt(0);
                    serverSnapshots.Add(ReadEntityGameSnapshots(ref packet,false));
                    break;
            }
        }

        private EntityGameSnapshot ReadEntityGameSnapshots(ref PacketReader packet, bool isDeltaSync)
        {
            SnapshotTick arriveTime;
            arriveTime.Tick = packet.ReadDateTime();
            short amountOfEntities = packet.ReadInt16();
            EntityTransformWithId[] entityTransforms = new EntityTransformWithId[amountOfEntities];
            for (int i = 0; i < amountOfEntities; i++)
            {
                short ID = packet.ReadInt16();
                entityTransforms[i] = new EntityTransformWithId(new EntityTransform(packet.ReadVector3(), packet.ReadQuaternion()), ID, isDeltaSync);
            }
            return new EntityGameSnapshot(entityTransforms, arriveTime);
        }

        public struct EntityGameSnapshot
        {
            public EntityTransformWithId[] EntityTransformWithIds;
            public SnapshotTick SnapshotTick;

            public EntityGameSnapshot(EntityTransformWithId[] entityTransformWithIds, SnapshotTick snapshotTick)
            {
                SnapshotTick = snapshotTick;
                EntityTransformWithIds = entityTransformWithIds;
            }
        }
    }
    public enum EntityHeader : byte
    {
        DELTA_SYNC,
        TRANSFORM_SYNC,
    }
    public struct EntityTransformWithId
    {
        public EntityTransform EntityTransform;
        public short ID;
        public bool IsDeltaSync;

        public EntityTransformWithId(EntityTransform entityTransform, short id, bool isDeltaSync)
        {
            EntityTransform = entityTransform;
            ID = id;
            IsDeltaSync = isDeltaSync;
        }
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
    }
}
