using System;
using System.Collections.Generic;
using System.Text;
using ClientSide.Sockets;
using UnityEngine;

namespace ClientSide.PacketCouriers.PersistentOWRigdSync
{

    /// <summary>
    /// For OWRigidbodies that will be synced and are always active, like moons, planets, anglerfishes, the balls in the observatory, the model ship, ...
    /// </summary>
    public class Client_PersistentOWRigdPacketCourier : MonoBehaviour, IPacketCourier
    {
        

        public void Receive(ref PacketReader packet)
        {
            //
        }
    }
}
