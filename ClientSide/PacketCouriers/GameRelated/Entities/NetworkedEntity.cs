using System;
using UnityEngine;

namespace ClientSide.PacketCouriers.GameRelated.Entities
{
    public class NetworkedEntity : MonoBehaviour
    {
        public string prefabName;
        public byte[][] intantiateData;

        public int id;

        public void SetInstantiateVariables(string prefabName,int id,  params byte[][] intantiateData)
        {
            this.prefabName = prefabName;
            this.intantiateData = intantiateData;
            this.id = id;
        }
        public byte[] GetDataFromInstantiate(int index)
        {
            if (intantiateData.Length - 1 < index)
                throw new IndexOutOfRangeException();

            return intantiateData[index];
        }        
    }
}