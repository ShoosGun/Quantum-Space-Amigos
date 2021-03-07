using System;
using System.Collections.Generic;
using System.Text;
using ServerSide.Sockets;
using ServerSide.PacketCouriers.Entities;
using UnityEngine;

namespace ServerSide.PacketCouriers.PersistentOWRigdSync
{

    /// <summary>
    /// For OWRigidbodies that will be synced and are always active, like moons, planets, anglerfishes, the balls in the observatory, the model ship, ...
    /// </summary>
    public class Server_PersistentOWRigdPacketCourier : MonoBehaviour, IPacketCourier
    {
        //Nomes dos OWRigidbodies que serão syncados
        private readonly string[] OWRigidbodiesGONames = new string[]
        { "",

        };

        //Nomes do grupo em que eles estão, e ai automaticamente procurar por eles, quando descobrirmos uma maneira de saber quantos virão, 
        //e ai colocar essa quantidade  no SyncedOWRigidbodies, deixarei isso comentado
        //private readonly string[] OWRigidbodiesGroupNames = new string[]
        //{ "",

        //};

        private NetworkedEntity[] SyncedOWRigidbodies;


        public void Awake()
        {
            SyncedOWRigidbodies = new NetworkedEntity[OWRigidbodiesGONames.Length];
            //Pegar referencias de todos os OWRigid que serão sincronizados

            for (int i = 0; i < SyncedOWRigidbodies.Length; i++)
                SyncedOWRigidbodies[i] = GameObject.Find(OWRigidbodiesGONames[i]).AddComponent<NetworkedEntity>();

        }
        public void Receive(ref PacketReader packet, string ClientID)
        {

        }
    }
}
