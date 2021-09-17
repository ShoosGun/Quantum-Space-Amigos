using ServerSide.PacketCouriers.GameRelated.Entities;
using ServerSide.Sockets;
using ServerSide.Sockets.Servers;
using UnityEngine;

namespace ServerSide.EntityScripts
{
    public class EntityScriptBehaviour : MonoBehaviour
    {        
        protected NetworkedEntity networkedEntity;

        [SerializeField]
        protected string UniqueScriptIdentifingString;

        [SerializeField]
        private bool HasGeneratedScriptId = false;
        [SerializeField]
        private int ScriptID = 0;
        public int GetScriptID()
        {
            if (!HasGeneratedScriptId)
            {
                ScriptID = Utils.Util.GerarHashInt(UniqueScriptIdentifingString);
                HasGeneratedScriptId = true;
            }
            return ScriptID;
        }

        protected virtual void Start()
        {
            networkedEntity = GetComponent<NetworkedEntity>();
        }
        public virtual void OnSerialize(ref PacketWriter writer)
        {
        }
        public virtual void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
        }
        protected virtual void OnDestroy()
        {
            if(networkedEntity != null)
                networkedEntity.RemoveEntityScript(this);
        }
    }
}
