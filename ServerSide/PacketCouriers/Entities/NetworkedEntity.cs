using UnityEngine;

namespace ServerSide.PacketCouriers.Entities
{
    /// <summary>
    /// Give this script for the OWRigidbody that you want to be synced
    /// </summary>
    /// 

    [RequireComponent(typeof(OWRigidbody))]
    public abstract class NetworkedEntity : MonoBehaviour
    {
        public short ID;
    }
}