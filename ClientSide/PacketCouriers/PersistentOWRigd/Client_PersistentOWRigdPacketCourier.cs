using System;
using System.Collections.Generic;
using System.Text;
using ClientSide.PacketCouriers.Entities;
using ClientSide.Sockets;
using UnityEngine;

namespace ClientSide.PacketCouriers.PersistentOWRigd
{

    /// <summary>
    /// For OWRigidbodies that will be synced and are always active, like moons, planets, anglerfishes, the balls in the observatory, the model ship, ...
    /// </summary>
    public class Client_PersistentOWRigdPacketCourier : MonoBehaviour, IPacketCourier, IEntityOwner
    {
        private Client client;
        private Client_NetworkedEntityPacketCourier entityPacketCourier;

        //Nomes dos OWRigidbodies que serão syncados
        private readonly PersistentOWRigdStuff[] OWRigidbodiesGONames = new PersistentOWRigdStuff[]
        {
            new PersistentOWRigdStuff("ModelShip_Body","Detector","Colliders/body","Colliders/landingGear"),
            //new PersistentOWRigdStuff("Satellite_Body","Detector","Collider"),
        };

        ////Nomes do grupo em que eles estão, e ai automaticamente procura por eles (para quando os nomes serem iguais. Ex.: as bolas do observatório)
        //private readonly string[] OWRigidbodiesGroupNames = new string[]
        //{ "GravityBallRoot", //Origem das pelotas do observatório, são 3 no total todas chamadas de Ball_Body
        //};

        private OWRigidbodyNetworker[] SyncedOWRigidbodies;
        private Dictionary<ushort,int> SyncedOWRigidbodiesIDsWithPositions;

        private byte THIS_PC_ID = 255;
        private bool hasReceivedId = false;

        bool connectedToServer = false;

        public void Awake()
        {
            //Pegar referencias de todos os OWRigid que serão sincronizados
            List<OWRigidbodyNetworker> SyncedOWRigidbodiesList = new List<OWRigidbodyNetworker>();

            for (int i = 0; i < OWRigidbodiesGONames.Length; i++)
            {
                OWRigidbodyNetworker oW = GameObject.Find(OWRigidbodiesGONames[i].GOName).AddComponent<OWRigidbodyNetworker>();
                oW.GetCollidersFromPaths(OWRigidbodiesGONames[i].Colliders);
                SyncedOWRigidbodiesList.Add(oW);
            }

            //for (int j = 0; j < OWRigidbodiesGroupNames.Length; j++)
            //{
            //    Transform groupParent = GameObject.Find(OWRigidbodiesGroupNames[j]).transform;
            //    OWRigidbody[] OWRigidbodies = groupParent.GetComponentsInChildren<OWRigidbody>();
            //    foreach (var OWRig in OWRigidbodies)
            //    {
            //        SyncedOWRigidbodiesList.Add(OWRig.gameObject.AddComponent<OWRigidbodyNetworker>());
            //        OWRig.gameObject.GetComponent<OWRigidbodyNetworker>().GetDetectorParam();
            //    }
            //}

            SyncedOWRigidbodies = SyncedOWRigidbodiesList.ToArray();
            SyncedOWRigidbodiesIDsWithPositions = new Dictionary<ushort, int>();
        }
        public void Start()
        {
            GameObject serverGO = GameObject.Find("QSAClient");
            client = serverGO.GetComponent<ClientMod>()._clientSide;
            entityPacketCourier = serverGO.GetComponent<Client_NetworkedEntityPacketCourier>();
            client.Connection += Client_Connection;
        }

        private void Client_Connection()
        {
            connectedToServer = true;
        }

        private void FixedUpdate()
        {
            if (connectedToServer && !hasReceivedId && (int)(Time.realtimeSinceStartup * 10) % 10 == 0)
            {
                PacketWriter pk = new PacketWriter();
                pk.Write((byte)Header.PERSISTENT_RIGIDB_PC);
                pk.Write((byte)PersistentOWRigd_Header.ENTITY_OWNER_ID);
                client.Send(pk.GetBytes());
            }
        }

        public void Receive(ref PacketReader packet)
        {
            switch ((PersistentOWRigd_Header)packet.ReadByte())
            {
                case PersistentOWRigd_Header.ENTITY_OWNER_ID:
                    byte ownerId = packet.ReadByte();
                    Debug.Log($"ID de dono = {ownerId}");
                    if (!hasReceivedId)
                    {
                        hasReceivedId = true;
                        THIS_PC_ID = ownerId;
                        entityPacketCourier.SetEntityOwner(THIS_PC_ID, this);
                    }
                    int amountOfIds = packet.ReadInt32();
                    for (int i = 0; i < amountOfIds; i++)
                    {
                        ushort entityID = packet.ReadUInt16();
                        Debug.Log($"({i}) ID esperado = {entityID}");
                        if (!SyncedOWRigidbodiesIDsWithPositions.ContainsKey(entityID))
                            SyncedOWRigidbodiesIDsWithPositions.Add(entityID, i);
                    }
                    PacketWriter pk = new PacketWriter();
                    pk.Write((byte)Header.NET_ENTITY_PC);
                    pk.Write((byte)EntityHeader.ENTITY_SYNC);
                    client.Send(pk.GetBytes());

                    break;
                default:
                    break;
            }
        }

        public NetworkedEntity OnAddEntity(ushort id)
        {
            Debug.Log($"Recebendo a id para sincronizar : {id}");

            int positionInArray = SyncedOWRigidbodiesIDsWithPositions[id];


            Debug.Log($"Posicao na fila : {positionInArray}");

            SyncedOWRigidbodies[positionInArray].GoToNetworkedMode();

            ////Teste para ver se o problema com a nave é virar kinematic
            //if (SyncedOWRigidbodies[positionInArray].name == "ModelShip_Body")
            //    SyncedOWRigidbodies[positionInArray].rigidbody.isKinematic = false;

            Debug.Log("GO preparado para sincronizar");

            return SyncedOWRigidbodies[positionInArray];
        }

        public void OnRemoveEntity(ushort id)
        {
            int positionInArray = SyncedOWRigidbodiesIDsWithPositions[id];

            SyncedOWRigidbodies[positionInArray].GoToSimulationMode();
            SyncedOWRigidbodiesIDsWithPositions.Remove(id);
        }
    }
    enum PersistentOWRigd_Header : byte
    {
        ENTITY_OWNER_ID,
    }

    struct PersistentOWRigdStuff
    {
        public string GOName;
        public string[] Colliders;

        public PersistentOWRigdStuff(string GOName, params string[] Colliders)
        {
            this.GOName = GOName;
            this.Colliders = Colliders;
        }
    }
}
