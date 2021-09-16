using System;
using System.Collections;
using System.Collections.Generic;

using ServerSide.Sockets;
using ServerSide.Sockets.Servers;
using ServerSide.Utils;
using UnityEngine;


namespace ServerSide.PacketCouriers.GameRelated.Entities
{
    public enum InstantiateType : int
    {
        NotBuffered,
        Buffered
    }

    static class InstantiadableGameObjectsPrefabHub
    {
        public static readonly Dictionary<string, GameObject> instantiadableGOPrefabs = new Dictionary<string, GameObject>();

        public static readonly Dictionary<int, NetworkedEntity> networkedEntities = new Dictionary<int, NetworkedEntity>();
        public static readonly List<int> bufferedInstantiationEntities = new List<int>();

        private static List<int> avaliableIds = new List<int>();
        private static int nextID = 0;
        public const int MAX_ID_AMOUNT = 100;

        public static void AddPrefab(GameObject gameObject, string prefabName)
        {
            if (instantiadableGOPrefabs.ContainsKey(prefabName))
                throw new OperationCanceledException(string.Format("There is already a GameObject in {0}", prefabName));

            instantiadableGOPrefabs.Add(prefabName, gameObject);
        }

        public static List<int> GetAllUsedIds()
        {
            HashSet<int> allUsedIds = new HashSet<int>();
            for (int i = 0; i < nextID; i++)
                allUsedIds.Add(i);
            allUsedIds.ExceptWith(avaliableIds);

            return new List<int>(allUsedIds);
        }

        public static int AddGameObject(NetworkedEntity networkedEntity)
        {
            int ID;
            if (avaliableIds.Count > 0)
            {
                ID = avaliableIds[0];
                avaliableIds.RemoveAt(0);
                networkedEntities.Add(ID, networkedEntity);
                networkedEntity.id = ID;

                if (networkedEntity.instantiateType > InstantiateType.NotBuffered)
                    bufferedInstantiationEntities.Add(ID);

                return ID;
            }
            else if (nextID < MAX_ID_AMOUNT)
            {
                ID = nextID;
                nextID++;
                networkedEntities.Add(ID, networkedEntity);
                networkedEntity.id = ID;

                if (networkedEntity.instantiateType > InstantiateType.NotBuffered)
                    bufferedInstantiationEntities.Add(ID);

                return ID;
            }
            throw new OperationCanceledException(string.Format("The max amount of GameObjects ids ({0}) was exceded", MAX_ID_AMOUNT));
        }

        public static bool RemoveGameObject(int id)
        {
            if (!networkedEntities.ContainsKey(id))
                return false;
            networkedEntities.Remove(id);
            avaliableIds.Add(id);
            bufferedInstantiationEntities.Remove(id);
            return true;
        }

        /// <summary>
        /// Same as RemoveGameObject(int id), but if ForceValueRemove is set to true it will search for the key in the dictionary KeyValuesPairs if just using the id fails.
        /// </summary>
        /// <param name="networkedEntity"></param>
        /// <param name="ForceValueRemove"></param>
        /// <returns></returns>
        public static bool RemoveGameObject(NetworkedEntity networkedEntity, bool ForceValueRemove = false)
        {
            bool resultOfFirstAttempt = RemoveGameObject(networkedEntity.id);
            if (!ForceValueRemove || resultOfFirstAttempt)
                return resultOfFirstAttempt;

            if (!networkedEntities.ContainsValue(networkedEntity))
                return false;

            int key = -1;
            foreach (var keyPar in networkedEntities)
            {
                if (keyPar.Value == networkedEntity) { key = keyPar.Key; break; }
            }
            networkedEntities.Remove(key);
            avaliableIds.Add(key);
            return true;
        }

        public static void ResetInstantiadableGameObjectsPrefabHub()
        {
            instantiadableGOPrefabs.Clear();
            networkedEntities.Clear();
            bufferedInstantiationEntities.Clear();
            avaliableIds.Clear();
            nextID = 0;
        }
    }

    class Server_EntityInitializer : MonoBehaviour
    {
        public static Server_EntityInitializer server_EntityInitializer;

        public Server_DynamicPacketIO DynamicPacketIO { get; private set; }
        const string EI_LOCALIZATION_STRING = "EntityInitializer";
        public int HeaderValue { get; private set; }

        public void Awake()
        {
            if (server_EntityInitializer != null)
            {
                Destroy(this);
                return;
            }
            server_EntityInitializer = this;

            DynamicPacketIO = Server.GetServer().DynamicPacketIO;
            HeaderValue = DynamicPacketIO.AddPacketReader(EI_LOCALIZATION_STRING, ReadPacket);

            Server.GetServer().NewConnectionID += Server_NewConnectionID;
        }

        public void OnDestroy()
        {
            if (server_EntityInitializer == this)
            {
                InstantiadableGameObjectsPrefabHub.ResetInstantiadableGameObjectsPrefabHub();
            }
        }

        public void AddGameObjectPrefab(string gameObjectName, GameObject gameObject)
        {
            InstantiadableGameObjectsPrefabHub.AddPrefab(gameObject, gameObjectName);
            gameObject.SetActive(false);
        }

        public GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, InstantiateType instantiateType, params object[] data)
        {
            if (!InstantiadableGameObjectsPrefabHub.instantiadableGOPrefabs.TryGetValue(prefabName, out GameObject prefab))
                throw new OperationCanceledException(string.Format("There is no GameObject in {0}", prefabName));

            GameObject gameObject = (GameObject)Instantiate(prefab, position, rotation);
            NetworkedEntity networkedEntity = gameObject.GetAttachedNetworkedEntity();
            networkedEntity.SetInstantiateVariables(prefabName, instantiateType, data);
            int ID = InstantiadableGameObjectsPrefabHub.AddGameObject(networkedEntity);
            gameObject.SetActive(true);

            SendAddEntities(new string[] { }, ID);
            
            return gameObject;
        }

        public void DestroyEntity(NetworkedEntity networkedEntity)
        {
            InstantiadableGameObjectsPrefabHub.RemoveGameObject(networkedEntity, true);
            Destroy(networkedEntity.gameObject);
            SendRemoveEntities(new string[] { }, networkedEntity.id);
        }
        
        private void Server_NewConnectionID(string clientID)
        {
            StartCoroutine("SendToNewConnection", clientID);
        }

        IEnumerator SendToNewConnection(string clientID)
        {
            yield return new WaitForSeconds(Time.deltaTime * 2f);
            SendAddEntities(new string[] { clientID }, InstantiadableGameObjectsPrefabHub.bufferedInstantiationEntities.ToArray());
        }

        private void SendAddEntities(string[] clientIDs, params int[] entitiesIDs)
        {
            PacketWriter buffer = new PacketWriter();
            buffer.Write((byte)EntityInitializerHeaders.Instantiate);
            buffer.Write(entitiesIDs.Length);
            for (int i = 0; i< entitiesIDs.Length; i++)
            {
                NetworkedEntity networkedEntity = InstantiadableGameObjectsPrefabHub.networkedEntities[entitiesIDs[i]];
                WriteEntityInstantiateData(networkedEntity, ref buffer);
            }
            DynamicPacketIO.SendPackedData((byte)HeaderValue, buffer.GetBytes(), clientIDs);
        }

        private void SendRemoveEntities(string[] clientIDs, params int[] entitiesIDs)
        {
            PacketWriter buffer = new PacketWriter();
            buffer.Write((byte)EntityInitializerHeaders.Remove);
            buffer.Write(entitiesIDs);
            DynamicPacketIO.SendPackedData((byte)HeaderValue, buffer.GetBytes(), clientIDs);
        }

        private void WriteEntityInstantiateData(NetworkedEntity entity, ref PacketWriter writer)
        {
            writer.Write(entity.prefabName);
            writer.Write(entity.id);
            writer.Write(entity.intantiateData);
            writer.Write(entity.transform.position);
            writer.Write(entity.transform.rotation);
        }

        public void WriteEntityScriptsOnSerialization(ref PacketWriter writer)
        {
            writer.Write(InstantiadableGameObjectsPrefabHub.networkedEntities.Count);
            foreach(var entity in InstantiadableGameObjectsPrefabHub.networkedEntities)
            {
                PacketWriter entityWriter = new PacketWriter();
                entity.Value.OnSerializeEntity(ref entityWriter);
                byte[] data = entityWriter.GetBytes();
                if (data.Length > 0)
                {
                    writer.Write(entity.Key);
                    writer.WriteAsArray(data);
                }
            }
        }
        public void ReadEntityScriptsOnDeserialization(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            int count = reader.ReadInt32();
            for(int i =0; i < count; i++)
            {
                int entityId = reader.ReadInt32();
                byte[] data = reader.ReadByteArray();

                if (InstantiadableGameObjectsPrefabHub.networkedEntities.TryGetValue(entityId, out NetworkedEntity entity))
                    entity.OnDeserializeEntity(data, receivedPacketData);
            }
        }
        public void ReadPacket(byte[] data, ReceivedPacketData receivedPacketData)
        {
            PacketReader reader = new PacketReader(data);
            ReadEntityScriptsOnDeserialization(ref reader, receivedPacketData);
        }

        enum EntityInitializerHeaders : byte
        {
            Instantiate,
            Remove
        }
    }
}

