using ClientSide.PacketCouriers.GameRelated.Entities;
using UnityEngine;

namespace ClientSide.EntityScripts
{
    public class EntityScriptBehaviour : MonoBehaviour
    {
        protected NetworkedEntity networkedEntity;
        protected virtual void Start()
        {
            networkedEntity = GetComponent<NetworkedEntity>();
        }
        public virtual byte[] OnSerialize()
        {
            return new byte[] { };
        }
        public virtual void OnDeserialize(byte[] serializationData)
        {
        }
    }
}
