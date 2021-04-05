using UnityEngine;

namespace ClientSide.PacketCouriers.Entities
{
    /// <summary>
    /// Give this script for the OWRigidbody that you want to be synced. Must have an Rigidbody
    /// </summary>
    /// 
    public abstract class NetworkedEntity : MonoBehaviour
    {
        public ushort ID;

        /// <summary>
        /// The PacketCourier that owns this ID/Entity
        /// </summary>
        public byte PCOwner;

    }
}