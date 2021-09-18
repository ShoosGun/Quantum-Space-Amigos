﻿using UnityEngine;
using System;
using ServerSide.EntityScripts;
using System.Collections.Generic;
using ServerSide.Sockets;
using ServerSide.Sockets.Servers;

namespace ServerSide.PacketCouriers.GameRelated.Entities
{
    public class NetworkedEntity : MonoBehaviour
    {
        public string prefabName;
        public object[] intantiateData;
        public InstantiateType instantiateType;

        public Dictionary<int,EntityScriptBehaviour> ComponentsToIO = new Dictionary<int, EntityScriptBehaviour>();

        public int id;

        private void Awake()
        {
            foreach (var script in GetComponents<EntityScriptBehaviour>())
                SetEntityScript(script);
        }

        public void SetInstantiateVariables(string prefabName, InstantiateType instantiateType, params object[] intantiateData)
        {
            this.prefabName = prefabName;
            this.instantiateType = instantiateType;
            this.intantiateData = intantiateData;
        }

        public T AddEntityScript<T>() where T: EntityScriptBehaviour
        {
            T script =  gameObject.AddComponent<T>();
            if (ComponentsToIO.ContainsKey(script.GetScriptID()))
                throw new OperationCanceledException(string.Format("The Script id from {0} ({1}) is already being used by another script", script.GetType(), script.GetScriptID()));

            ComponentsToIO.Add(script.GetScriptID(), script);
            return script;
        }
        public void SetEntityScript<T>(T script) where T: EntityScriptBehaviour
        {
            if (ComponentsToIO.ContainsKey(script.GetScriptID()))
                throw new OperationCanceledException(string.Format("The Script id from {0} ({1}) is already being used by another script", script.GetType(), script.GetScriptID()));

            if (gameObject.GetComponent<T>() == script)
                ComponentsToIO.Add(script.GetScriptID(), script);
        }
        public void RemoveEntityScript<T>(T script) where T : EntityScriptBehaviour
        {
            if (!ComponentsToIO.Remove(script.GetScriptID()))
                Debug.LogWarning(string.Format("The script {0}({1}) isn't being synced in {2}", script.GetType(), script.GetScriptID(), transform.name), this);
        }

        public void  OnSerializeEntity(ref PacketWriter writer)
        {
            if (ComponentsToIO.Count <= 0)
                return;

            int serializedScripts = 0;
            PacketWriter scriptSerializeBuffer = new PacketWriter();
            foreach (var networkedScript in ComponentsToIO)
            {
                if (networkedScript.Value.IsToSerialize())
                {
                    scriptSerializeBuffer.Write(networkedScript.Key);
                    networkedScript.Value.OnSerialize(ref scriptSerializeBuffer);
                }
            }

            writer.Write(serializedScripts);
            writer.Write(scriptSerializeBuffer.GetBytes());
        }
        public void OnDeserializeEntity(byte[] data, ReceivedPacketData receivedPacketData)
        {
            PacketReader reader = new PacketReader(data);
            int count = reader.ReadInt32();
            for(int i =0; i< count;i++)
            {
                int scriptId = reader.ReadInt32();
                if(ComponentsToIO.TryGetValue(scriptId,out var script))
                {
                    try
                    {
                        script.OnDeserialize(ref reader, receivedPacketData);
                    }
                    catch(Exception ex)
                    {
                        Debug.Log(string.Format("OnDeserialize failed in {0} : {1}", script.name, ex.Message));
                    }
                }
                else
                {
                    Debug.LogError(string.Format("The script with id {0} couldn't be found", scriptId));
                    break;
                }
            }
        }

        private void OnDestroy()
		{
            Server_EntityInitializer.server_EntityInitializer.DestroyEntity(this);
        }
    }
}