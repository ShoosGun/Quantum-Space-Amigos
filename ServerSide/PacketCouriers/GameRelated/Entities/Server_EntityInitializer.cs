using System;
using System.Collections;
using System.Collections.Generic;
using ServerSide.PacketCouriers.Essentials;
using ServerSide.Sockets;
using ServerSide.Sockets.Servers;
using ServerSide.Utils;
using UnityEngine;


namespace ServerSide.PacketCouriers.GameRelated.Entities
{
    public enum InstantiateType : int
    {
        NotBuffered,
        Buffered,
        Buffered_Plus_Position_And_Rotation
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

        public static int AddGameObject(NetworkedEntity networkedEntity)//InstantiateType instantiateType, params byte[][] data
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
    }

    class Server_EntityInitializer : MonoBehaviour
    {
        public static Server_EntityInitializer server_EntityInitializer;

        public Server_DynamicPacketIO DynamicPacketIO { get; private set; }
        const string EI_LOCALIZATION_STRING = "EntityInitializer";
        public int HeaderValue { get; private set; }

        public void Start()
        {
            if (server_EntityInitializer != null)
            {
                Destroy(this);
                return;
            }
            server_EntityInitializer = this;

            Server_DynamicPacketCourierHandler handler = Server.GetServer().dynamicPacketCourierHandler;
            HeaderValue = handler.AddPacketCourier(EI_LOCALIZATION_STRING, ReadPacket);
            DynamicPacketIO = handler.DynamicPacketIO;
            StartCoroutine("SendMarcoPeriodically");
        }

        public void AddGameObjectPrefab(string gameObjectName, GameObject gameObject)
        {
            InstantiadableGameObjectsPrefabHub.AddPrefab(gameObject, gameObjectName);
            gameObject.SetActive(false);
        }

        public GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, InstantiateType instantiateType, params byte[][] data)
        {
            if (!InstantiadableGameObjectsPrefabHub.instantiadableGOPrefabs.TryGetValue(prefabName, out GameObject prefab))
                throw new OperationCanceledException(string.Format("There is no GameObject in {0}", prefabName));
            //Send:
            //gameObjectName string
            //object id short
            //position Vector3
            //rotation Quaternion
            //data byte[][]

            //TODO criar esse envio de dados

            GameObject gameObject = (GameObject)Instantiate(prefab, position, rotation);
            NetworkedEntity networkedEntity = gameObject.GetAttachedNetworkedEntity();
            networkedEntity.SetInstantiateVariables(prefabName, instantiateType, data);

            int ID = InstantiadableGameObjectsPrefabHub.AddGameObject(networkedEntity);

            return gameObject;
        }

        //TODO enviar dados buffered guardados em InstantiadableGameObjectsPrefabHub.bufferedInstantiationEntities
        // para clientes que acabaram de se conectar, seguindo a "regra" de Server_DynamicPacketCourierHandler com uma courotine
        private void Server_NewConnectionID(string clientID)
        {
            StartCoroutine("SendToNewConnection", clientID);
        }

        IEnumerator SendToNewConnection(string clientID)
        {
            yield return new WaitForSeconds(Time.deltaTime * 2f);
            SendBufferedEntities(clientID);
        }

        private void SendBufferedEntities(string clientID)
        {
            PacketWriter buffer = new PacketWriter();
            var bufferedEntitiesIDs = InstantiadableGameObjectsPrefabHub.GetAllUsedIds();
            for(int i = 0; i< bufferedEntitiesIDs.Count; i++)
            {
                NetworkedEntity entity = InstantiadableGameObjectsPrefabHub.networkedEntities[bufferedEntitiesIDs[i]];
                // Send:
                //gameObjectName string
                //object id int
                //data byte[][]

                //E se InstantiateType == Buffered_Plus_Position_And_Rotation
                //position Vector3
                //rotation Quaternion
            }
            DynamicPacketIO.SendPackedData((byte)HeaderValue, buffer.GetBytes());
        }

        public void SendMarco(int i)
        {
            PacketWriter marco = new PacketWriter();
            marco.Write("Marco " + i);
            DynamicPacketIO.SendPackedData((byte)HeaderValue, marco.GetBytes());
        }
        public void ReadPacket(byte[] data, string ClientID)
        {
            PacketReader reader = new PacketReader(data);
            Debug.Log($"Recebemos de {ClientID}: {reader.ReadString()}");
        }
    }
}

