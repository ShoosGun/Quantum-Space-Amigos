using UnityEngine;
using System;

namespace ServerSide.PacketCouriers.GameRelated.Entities
{
    public class NetworkedEntity : MonoBehaviour
    {
        public string prefabName;
        public byte[][] intantiateData;
        public InstantiateType instantiateType;

        public int id;

        public void SetInstantiateVariables(string prefabName, InstantiateType instantiateType, params byte[][] intantiateData)
        {
            this.prefabName = prefabName;
            this.instantiateType = instantiateType;
            this.intantiateData = intantiateData;
        }
        public byte[] GetDataFromInstantiate(int index)
        {
            if (intantiateData.Length - 1 < index)
                throw new IndexOutOfRangeException();

            return intantiateData[index];
        }
		
		private void OnDestroy()
		{
            Server_EntityInitializer.server_EntityInitializer.DestroyEntity(this);
        }
    }
}