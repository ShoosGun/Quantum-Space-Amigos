using UnityEngine;

namespace ServerSide.PacketCouriers.Entities
{
    /// <summary>
    /// Give this script for the OWRigidbody that you want to be synced
    /// </summary>
    /// 
    
    public abstract class NetworkedEntity : MonoBehaviour
    {
        public short ID;

        /// <summary>
        /// The PacketCourier that owns this ID/Entity
        /// </summary>
        public byte PCOwner;

        /// <summary>
        /// Checks to see if there is a Rigidbody and places one
        /// </summary>
        protected virtual void Start()
        {
            if (gameObject.GetComponent<Rigidbody>() == null)
                gameObject.AddComponent<Rigidbody>();
        }
    }
}