using UnityEngine;
using System;

namespace ServerSide.PacketCouriers.GameRelated.Entities
{
    public class NetworkedEntity : MonoBehaviour
    {
        public string prefabName;
        public object[] intantiateData;
        public InstantiateType instantiateType;

        public int id;

        public void SetInstantiateVariables(string prefabName, InstantiateType instantiateType, params object[] intantiateData)
        {
            this.prefabName = prefabName;
            this.instantiateType = instantiateType;
            this.intantiateData = intantiateData;
        }
		
		private void OnDestroy()
		{
            Server_EntityInitializer.server_EntityInitializer.DestroyEntity(this);
        }
    }
}