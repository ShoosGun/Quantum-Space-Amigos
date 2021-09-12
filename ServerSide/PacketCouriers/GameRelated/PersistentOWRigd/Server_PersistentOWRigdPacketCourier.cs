//using System;
//using System.Collections.Generic;
//using System.Text;
//using ServerSide.Sockets;
//using ServerSide.PacketCouriers.Entities;
//using UnityEngine;
//using ServerSide.Sockets.Servers;
//using ServerSide.PacketCouriers.Essentials;

//namespace ServerSide.PacketCouriers.PersistentOWRigdSync
//{

//    /// <summary>
//    /// For OWRigidbodies that will be synced and are always active, like moons, planets, anglerfishes, the balls in the observatory, the model ship, ...
//    /// </summary>
//    public class Server_PersistentOWRigdPacketCourier : MonoBehaviour, IPacketCourier 
//    {
//        private Server_DynamicPacketCourierHandler dynamicPacketCourierHandler;
//        const string POW_LOCALIZATION_STRING = "PersistentOWRigdPacketCourier";
//        private int HeaderValue;

//        private Server_NetworkedEntityPacketCourier entityPacketCourier;

//        //Nomes dos OWRigidbodies que serão syncados
//        private readonly string[] OWRigidbodiesGONames = new string[]
//        {
//            "ModelShip_Body",
//            //"Moon_Body",
//        };

//        ////Nomes do grupo em que eles estão, e ai automaticamente procura por eles (para quando os nomes serem iguais. Ex.: as bolas do observatório)
//        //private readonly string[] OWRigidbodiesGroupNames = new string[]
//        //{ "GravityBallRoot", //Origem das pelotas do observatório, são 3 no total todas chamadas de Ball_Body
//        //};

//        private NetworkedEntity[] SyncedOWRigidbodies;

//        private bool hasSyncedTheEntities = false;

//        /// <summary>
//        /// The ID for using the NetworkedEntityPC
//        /// </summary>
//        private byte THIS_PC_ID;

//        public void Awake()
//        {
//            //Pegar referencias de todos os OWRigid que serão sincronizados
//            List<NetworkedEntity> SyncedOWRigidbodiesList = new List<NetworkedEntity>();

//            for (int i = 0; i < OWRigidbodiesGONames.Length; i++)
//                SyncedOWRigidbodiesList.Add(GameObject.Find(OWRigidbodiesGONames[i]).AddComponent<NetworkedEntity>());

//            //for (int j = 0; j < OWRigidbodiesGroupNames.Length; j++)
//            //{
//            //    Transform groupParent = GameObject.Find(OWRigidbodiesGroupNames[j]).transform;
//            //    OWRigidbody[] OWRigidbodies = groupParent.GetComponentsInChildren<OWRigidbody>();
//            //    foreach (var OWRig in OWRigidbodies)
//            //        SyncedOWRigidbodiesList.Add(OWRig.gameObject.AddComponent<NetworkedEntity>());
//            //}

//            SyncedOWRigidbodies = SyncedOWRigidbodiesList.ToArray();
//        }
//        public void Start()
//        {
//            GameObject serverGO = GameObject.Find("QSAServer");

//            dynamicPacketCourierHandler = serverGO.GetComponent<ServerMod>()._serverSide.dynamicPacketCourierHandler;
//            HeaderValue = dynamicPacketCourierHandler.AddPacketCourier(POW_LOCALIZATION_STRING, Receive);

//            entityPacketCourier = serverGO.GetComponent<Server_NetworkedEntityPacketCourier>();
//        }

//        public void Update()
//        {
//            if (!hasSyncedTheEntities)
//            {
//                Debug.Log("Sincronizando objetos permanentes. . .");
//                THIS_PC_ID = entityPacketCourier.AddEntityOwner();
//                foreach (NetworkedEntity entity in SyncedOWRigidbodies)
//                {
//                    entityPacketCourier.AddEntitySync(entity, THIS_PC_ID);
//                    Debug.Log($"{entity.name} com ID = {entity.ID}");
//                }

//                hasSyncedTheEntities = true;
//            }
//        }

//        public void Receive(byte[] data, string ClientID)
//        {
//            PacketReader packet = new PacketReader(data);
//            switch ((PersistentOWRigd_Header)packet.ReadByte())
//            {
//                case PersistentOWRigd_Header.ENTITY_OWNER_ID:
//                    PacketWriter packetForClient = new PacketWriter();
//                    packetForClient.Write((byte)PersistentOWRigd_Header.ENTITY_OWNER_ID);
//                    packetForClient.Write(THIS_PC_ID);//byte
//                    packetForClient.Write(SyncedOWRigidbodies.Length);//int
//                    foreach (NetworkedEntity entity in SyncedOWRigidbodies)
//                        packetForClient.Write(entity.ID);//short

//                    dynamicPacketCourierHandler.DynamicPacketIO.SendPackedData((byte)HeaderValue, packetForClient.GetBytes(), ClientID);
//                    break;
//                default:
//                    break;
//            }
//        }
//    }

//    enum PersistentOWRigd_Header : byte
//    {
//        ENTITY_OWNER_ID,
//    }
    
//}
