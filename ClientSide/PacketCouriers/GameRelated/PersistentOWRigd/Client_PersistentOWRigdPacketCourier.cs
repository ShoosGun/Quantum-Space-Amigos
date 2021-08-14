﻿using System;
using System.Collections.Generic;
using System.Text;
using ClientSide.PacketCouriers.Entities;
using ClientSide.PacketCouriers.Shades.MovementConstraints;
using ClientSide.Sockets;
using UnityEngine;

namespace ClientSide.PacketCouriers.PersistentOWRigd
{

    /// <summary>
    /// For OWRigidbodies that will be synced and are always active, like moons, planets, anglerfishes, the balls in the observatory, the model ship, ...
    /// </summary>
    public class Client_PersistentOWRigdPacketCourier : MonoBehaviour, IPacketCourier, IEntityOwner //Usar o OWRigidFollowsAnother para consetar isso, transforma-los em kinematic não é suficiente
    {
        private Client client;
        private Client_NetworkedEntityPacketCourier entityPacketCourier;

        //Nomes dos OWRigidbodies que serão syncados
        private readonly PersistentOWRigdStuff[] OWRigidbodiesGONames = new PersistentOWRigdStuff[]
        {
            new PersistentOWRigdStuff("ModelShip_Body", "Detector", "Colliders/body", "Colliders/landingGear"),
            //new PersistentOWRigdStuff("Moon_Body"),
        };

        ////Nomes do grupo em que eles estão, e ai automaticamente procura por eles (para quando os nomes serem iguais. Ex.: as bolas do observatório)
        //private readonly string[] OWRigidbodiesGroupNames = new string[]
        //{ "GravityBallRoot", //Origem das pelotas do observatório, são 3 no total todas chamadas de Ball_Body
        //};

        private PersistentOWRigdNE[] SyncedOWRigidbodies;
        private Dictionary<ushort,int> SyncedOWRigidbodiesIDsWithPositions;

        private byte THIS_PC_ID = 255;
        private bool hasReceivedId = false;

        bool connectedToServer = false;

        public void Awake()
        {
            //Pegar referencias de todos os OWRigid que serão sincronizados
            List<PersistentOWRigdNE> SyncedOWRigidbodiesList = new List<PersistentOWRigdNE>();

            for (int i = 0; i < OWRigidbodiesGONames.Length; i++)
            {
                PersistentOWRigdNE oW = GameObject.Find(OWRigidbodiesGONames[i].GOName).AddComponent<PersistentOWRigdNE>();
                //FakePersistentOWRigdNE oW = CreateFakePersRigd();
                //oW.SetToConstrain(t.GetComponent<OWRigidbody>());

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
                pk.Write((byte)Header.Header_Size + 2);
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
                    pk.Write((byte)Header.Header_Size + 1);
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
            Debug.Log("GO preparado para sincronizar");
            //Debug.Log("GO impedindo movimentos do objeto alvo");

            return SyncedOWRigidbodies[positionInArray];
        }

        public void OnRemoveEntity(ushort id)
        {
            int positionInArray = SyncedOWRigidbodiesIDsWithPositions[id];

            SyncedOWRigidbodies[positionInArray].GoToSimulationMode();

            SyncedOWRigidbodiesIDsWithPositions.Remove(id);
        }


        //FakePersistentOWRigdNE CreateFakePersRigd()
        //{
        //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    go.AddComponent<Rigidbody>().isKinematic = true;
        //    go.AddComponent<OWRigidbody>();

        //    go.collider.enabled = false;
        //    go.layer = LayerMask.NameToLayer("Primitive");
        //    go.transform.parent = GameObject.Find("TimberHearth_Body").transform;

        //    return go.AddComponent<FakePersistentOWRigdNE>();
        //}
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
