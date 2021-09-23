using System;
using System.Collections;
using UnityEngine;

using ClientSide.Sockets;

using ClientSide.PacketCouriers.GameRelated.Entities;
using ClientSide.EntityScripts.TransfromSync;

namespace ClientSide.PacketCouriers.Experiments
{
    public class Client_MarcoPoloExperiment : MonoBehaviour
    {
        private Client client;
        private Client_DynamicPacketIO DynamicPacketIO;

        const string MP_LOCALIZATION_STRING = "MarcoPoloExperiment";
        public int HeaderValue { get; private set; }
        

        public void Start()
        {
            client = Client.GetClient();

            DynamicPacketIO = client.DynamicPacketIO;
            HeaderValue = DynamicPacketIO.AddPacketReader(MP_LOCALIZATION_STRING, ReadPacket);

            client.Connection += Client_Connection;

            Client_EntityInitializer.client_EntityInitializer.AddGameObjectPrefab("CuB0", CreateNetworkedCube());
        }

        private void Client_Connection()
        {
            StartCoroutine("SendPoloPeriodically");
        }
        
        IEnumerator SendPoloPeriodically()
        {
            while (true)
            {
                SendPolo();
                yield return new WaitForSeconds(1f);
            }
        }
        public void SendPolo()
        {
            PacketWriter polo = new PacketWriter();
            polo.Write("Polo");
            DynamicPacketIO.SendPackedData(HeaderValue, polo.GetBytes());
        }
        public void ReadPacket(byte[] data, ReceivedPacketData receivedPacketData)
        {
            PacketReader reader = new PacketReader(data);
            Debug.Log($"Recebemos do servidor: {reader.ReadString()} | {receivedPacketData.Latency} ms");
        }

        public GameObject CreateNetworkedCube()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.GetComponent<Collider>().enabled = false;
            go.AddComponent<Rigidbody>();
            go.AddComponent<OWRigidbody>();
            NetworkedEntity networkedEntity = go.AddComponent<NetworkedEntity>();
            networkedEntity.AddEntityScript<TransformEntitySync>();
            networkedEntity.AddEntityScript<RigidbodyEntitySync>();
            return go;
        }
    }
}
