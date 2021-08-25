using UnityEngine;
using System;

namespace ServerSide.PacketCouriers.GameRelated.Entities
{
    public class NetworkedEntity : MonoBehaviour
    {
        public string prefabName;
        public byte[][] intantiateData;
        public Vector3 InitialPosition;
        public Quaternion InitialRotation;
        public InstantiateType instantiateType;

        public int id;

        public void SetInstantiateVariables(string prefabName, InstantiateType instantiateType, params byte[][] intantiateData)
        {
            this.prefabName = prefabName;
            this.instantiateType = instantiateType;
            this.intantiateData = intantiateData;

            InitialPosition = transform.position;
            InitialRotation = transform.rotation;
        }
        public byte[] GetDataFromInstantiate(int index)
        {
            if (intantiateData.Length - 1 < index)
                throw new IndexOutOfRangeException();

            return intantiateData[index];
        }
		
		private void OnDestroy()
		{
            InstantiadableGameObjectsPrefabHub.RemoveGameObject(id);		
		}
    }
}