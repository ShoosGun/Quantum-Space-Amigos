using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

using ServerSide.Sockets.Servers;
using ServerSide.Sockets;
using ServerSide.Utils;

using ServerSide.PacketCouriers.GameRelated.Entities;

namespace ServerSide.PacketCouriers.Experiments
{
    public class Server_MarcoPoloExperiment : MonoBehaviour
    {
        public Server_DynamicPacketIO DynamicPacketIO { get; private set; }
        
        const string MP_LOCALIZATION_STRING = "MarcoPoloExperiment";
        public int HeaderValue { get; private set; }

        public void Start()
        {
            DynamicPacketIO = Server.GetServer().DynamicPacketIO;
            HeaderValue = DynamicPacketIO.AddPacketReader(MP_LOCALIZATION_STRING, ReadPacket);
            
            Server_EntityInitializer.server_EntityInitializer.AddGameObjectPrefab("CuB0", CreateNetworkedCube());

            StartCoroutine("SendMarcoPeriodically");
            StartCoroutine("CreateAndDestroyCubePeriodically");
        }
        public void Update()
        {
            
        }
        IEnumerator CreateAndDestroyCubePeriodically()
        {
            while (true)
            {
                NetworkedEntity networkedEntity = Server_EntityInitializer.server_EntityInitializer.Instantiate("CuB0", Vector3.forward, Quaternion.identity, InstantiateType.Buffered).GetAttachedNetworkedEntity();
                yield return new WaitForSeconds(5f);
                Server_EntityInitializer.server_EntityInitializer.DestroyEntity(networkedEntity);
                yield return new WaitForSeconds(2.5f);
            }
        }
        IEnumerator SendMarcoPeriodically()
        {
            int i = 0;
            while (true)
            {
                SendMarco(i);
                i++;
                yield return new WaitForSeconds(5f);
            }
        } 
        public void SendMarco(int i)
        {
            PacketWriter marco = new PacketWriter();
            marco.Write("Marco " + i);
            DynamicPacketIO.SendPackedData((byte)HeaderValue, marco.GetBytes());
        }
        public void ReadPacket(int latency, DateTime packetSentTime, byte[] data, string ClientID)
        {
            PacketReader reader = new PacketReader(data);
            Debug.Log($"Recebemos de {ClientID}: {reader.ReadString()} | {latency} ms - {packetSentTime.Ticks} ticks");
        }

        public GameObject CreateNetworkedCube()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.GetComponent<Collider>().enabled = false;
            go.AddComponent<NetworkedEntity>();
            return go;
        }
    }
}
