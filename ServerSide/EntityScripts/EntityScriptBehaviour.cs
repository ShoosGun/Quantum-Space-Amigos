using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using UnityEngine;

using ServerSide.PacketCouriers.GameRelated.Entities;
using ServerSide.Sockets;
using ServerSide.Sockets.Servers;


namespace ServerSide.EntityScripts
{
    public class EntityScriptBehaviour : MonoBehaviour
    {        
        protected NetworkedEntity networkedEntity;

        [SerializeField]
        protected bool Serialize = false;

        public bool IsToSerialize()
        {
            return Serialize;
        }

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
        //TODO add an networked event system per EntitySciptBehavior, prob. inside NetworkedEntity
       
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
            if (networkedEntity != null)
                networkedEntity.RemoveEntityScript(this);
        }
    }
}
