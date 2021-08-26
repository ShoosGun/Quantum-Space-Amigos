//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using ClientSide.Sockets;
//using ClientSide.PacketCouriers.Essentials;

//namespace ClientSide.PacketCouriers.Entities
//{
//    public struct SnapshotTick //Até descobir uma maneira de "renomear" int/DateTime/... vai ser assim
//    {
//        public DateTime Tick; // Se formos ter que mudar a forma que contamos os ticks, só teremos que mudar aki
//    }

//    public class Client_NetworkedEntityPacketCourier : MonoBehaviour, IPacketCourier
//    {
//        private Client client;
//        const string NE_LOCALIZATION_STRING = "NetworkedEntityPacketCourier";
//        public int HeaderValue { get; private set; }

//        private Transform ReferenceFrameTransform;

//        public const ushort MAX_AMOUNT_OF_ENTITIES = 2048;
//        protected NetworkedEntity[] entities = new NetworkedEntity[MAX_AMOUNT_OF_ENTITIES];
//        private List<ushort> entitiesIDs = new List<ushort>();

//        private IEntityOwner[] entityOwners = new IEntityOwner[254];

//        private List<SnapshotTick> snapshotsTicks = new List<SnapshotTick>();
//        private List<EntityGameSnapshot> serverSnapshots = new List<EntityGameSnapshot>();
//        private const int MAX_SNAPSHOTS = 3; //Número máxmo fotos que se pode ter do jogo

//        private void Start()
//        {
//            ReferenceFrameTransform = GameObject.Find("TimberHearth_Body").transform;
            
//            client = GameObject.Find("QSAClient").GetComponent<ClientMod>()._clientSide;
//            Client_DynamicPacketCourierHandler dynamicPacketCourierHandler = client.dynamicPacketCourierHandler;
//            dynamicPacketCourierHandler.SetPacketCourier(NE_LOCALIZATION_STRING, OnReceiveHeaderValue);

//            client.Disconnection += Client_Disconnection;
//        }
//        public ReadPacketHolder.ReadPacket OnReceiveHeaderValue(int HeaderValue)
//        {
//            this.HeaderValue = HeaderValue;
//            return Receive;
//        }

//        private void OnDestroy()
//        {
//            client.Disconnection -= Client_Disconnection;
//        }

//        private void Client_Disconnection()
//        {
//            foreach (ushort ID in entitiesIDs)
//            {
//                entities[ID].ID = MAX_AMOUNT_OF_ENTITIES;
//                entities[ID] = null;
//            }
//            entitiesIDs.Clear();
//            serverSnapshots.Clear();
//            snapshotsTicks.Clear();
//        }

//        //private SnapshotTick ourCurrentSnapshot; //O cliente pode estar em uma snapshot diferente da do servidor, ele guardara qual aki
//        private void FixedUpdate()
//        {
//            //Ler as snapshots vindas do servidor
//            foreach (EntityGameSnapshot gameSnapshot in serverSnapshots)
//            {
//                foreach(EntityTransformWithId transformWithId in gameSnapshot.EntityTransformWithIds)
//                {
//                    if (entitiesIDs.Contains(transformWithId.ID))
//                    {
//                        NetworkedEntity networkedEntity = entities[transformWithId.ID];
//                        //Delta Syncs
//                        if (transformWithId.IsDeltaSync)
//                        {
//                            networkedEntity.transform.position += ReferenceFrameTransform.TransformDirection(transformWithId.EntityTransform.Position);
//                            networkedEntity.transform.rotation = transformWithId.EntityTransform.Rotation * networkedEntity.transform.rotation;
//                        }
//                        else
//                        {
//                            networkedEntity.transform.position = ReferenceFrameTransform.TransformPoint(transformWithId.EntityTransform.Position);
//                            networkedEntity.transform.rotation = ReferenceFrameTransform.rotation * transformWithId.EntityTransform.Rotation ;
//                        }
//                    }
//                }
//            }
//            if (serverSnapshots.Count > 0)
//                serverSnapshots.Clear();
//        }

//        /// <summary>
//        /// When you receive the id of the entityOwner, place it here with a callback for receiving the ids of entities.
//        /// </summary>
//        /// <param name="ID">The ID received for this entityOwner</param>
//        /// <returns></returns>
//        public void SetEntityOwner(byte ID, IEntityOwner entityOwner)
//        {
//            if (ID < 255)
//                entityOwners[ID] = entityOwner;
//        }
        
//        public void Receive(byte[] data)
//        {
//            PacketReader packet = new PacketReader(data);
//            switch ((EntityHeader)packet.ReadByte())
//            {
//                case EntityHeader.DELTA_SYNC:
//                    if (serverSnapshots.Count > MAX_SNAPSHOTS)
//                        serverSnapshots.RemoveAt(0);
//                    serverSnapshots.Add(ReadEntityGameSnapshots(ref packet, true));
//                    break;

//                case EntityHeader.TRANSFORM_SYNC:
//                    if (serverSnapshots.Count > MAX_SNAPSHOTS)
//                        serverSnapshots.RemoveAt(0);
//                    serverSnapshots.Add(ReadEntityGameSnapshots(ref packet,false));
//                    break;

//                case EntityHeader.ENTITY_SYNC:
//                    short amountOfEntities = packet.ReadInt16();
//                    for(int i = 0; i < amountOfEntities; i++)
//                        ReadAddEntity(ref packet);
//                    break;

//                case EntityHeader.ENTITY_DELTA_PLUS_SYNC:
//                    ReadAddEntity(ref packet);
//                    break;

//                case EntityHeader.ENTITY_DELTA_MINUS_SYNC:
//                    ushort entityId = packet.ReadUInt16();
//                    if (entitiesIDs.Contains(entityId))
//                    {
//                        entityOwners[entities[entityId].PCOwner].OnRemoveEntity(entityId);
//                        entitiesIDs.Remove(entityId);
//                        entities[entityId] = null;
//                    }
//                    break;

//                default:
//                    break;
//            }
//        }

//        private void ReadAddEntity(ref PacketReader packet)
//        {
//            ushort entityId = packet.ReadUInt16();
//            byte ownerId = packet.ReadByte();
//            if (entityOwners[ownerId] != null && !entitiesIDs.Contains(entityId))
//            {
//                entities[entityId] = entityOwners[ownerId].OnAddEntity(entityId);
//                entitiesIDs.Add(entityId);
//            }
//            Debug.Log($"Nova entidade de {ownerId}! Id = {entityId}");
//        }

//        private EntityGameSnapshot ReadEntityGameSnapshots(ref PacketReader packet, bool isDeltaSync)
//        {
//            SnapshotTick arriveTime;
//            arriveTime.Tick = packet.ReadDateTime();
//            short amountOfEntities = packet.ReadInt16();
//            EntityTransformWithId[] entityTransforms = new EntityTransformWithId[amountOfEntities];
//            for (int i = 0; i < amountOfEntities; i++)
//            {
//                ushort ID = packet.ReadUInt16();
//                entityTransforms[i] = new EntityTransformWithId(new EntityTransform(packet.ReadVector3(), packet.ReadQuaternion()), ID, isDeltaSync);
//            }
//            return new EntityGameSnapshot(entityTransforms, arriveTime);
//        }

//        public struct EntityGameSnapshot
//        {
//            public EntityTransformWithId[] EntityTransformWithIds;
//            public SnapshotTick SnapshotTick;

//            public EntityGameSnapshot(EntityTransformWithId[] entityTransformWithIds, SnapshotTick snapshotTick)
//            {
//                SnapshotTick = snapshotTick;
//                EntityTransformWithIds = entityTransformWithIds;
//            }
//        }
        
//    }
//    public enum EntityHeader : byte
//    {
//        DELTA_SYNC,
//        TRANSFORM_SYNC,
//        ENTITY_SYNC, //Para que a quantidade de shades nos dois lados seja igual (Para novos clientes)
//        ENTITY_DELTA_PLUS_SYNC, //  /\ (Para já conectados)
//        ENTITY_DELTA_MINUS_SYNC,// /  \ 
//    }
//    public struct EntityTransformWithId
//    {
//        public EntityTransform EntityTransform;
//        public ushort ID;
//        public bool IsDeltaSync;

//        public EntityTransformWithId(EntityTransform entityTransform, ushort id, bool isDeltaSync)
//        {
//            EntityTransform = entityTransform;
//            ID = id;
//            IsDeltaSync = isDeltaSync;
//        }
//    }

//    public struct EntityTransform
//    {
//        public Vector3 Position;
//        public Quaternion Rotation;

//        public EntityTransform(Vector3 position, Quaternion rotation)
//        {
//            Position = position;
//            Rotation = rotation;
//        }
//        public EntityTransform(Rigidbody rigidbody)
//        {
//            Position = rigidbody.position;
//            Rotation = rigidbody.rotation;
//        }
//        public EntityTransform(Transform transform)
//        {
//            Position = transform.position;
//            Rotation = transform.rotation;
//        }
//    }
//}
