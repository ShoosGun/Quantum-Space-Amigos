using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

using ServerSide.Sockets.Servers;
using ServerSide.Sockets;
using ServerSide.Utils;

using ServerSide.PacketCouriers.GameRelated.Entities;
using ServerSide.EntityScripts.TransfromSync;
using ServerSide.PacketCouriers.GameRelated.InputReader;

namespace ServerSide.PacketCouriers.Experiments
{
    public class Server_MarcoPoloExperiment : MonoBehaviour
    {
        public Server_DynamicPacketIO DynamicPacketIO { get; private set; }
        
        const string MP_LOCALIZATION_STRING = "MarcoPoloExperiment";
        public int HeaderValue { get; private set; }
		
		public string clientToSee = "";

        public void Start()
        {
            DynamicPacketIO = Server.GetServer().DynamicPacketIO;
            HeaderValue = DynamicPacketIO.AddPacketReader(MP_LOCALIZATION_STRING, ReadPacket);
			
			Server.GetServer().NewConnectionID += Server_MarcoPoloExperiment_NewConnectionID;
			Server.GetServer().DisconnectionID += Server_MarcoPoloExperiment_DisconnectionID;
            
            Server_EntityInitializer.server_EntityInitializer.AddGameObjectPrefab("CuB0", CreateNetworkedCube());

            StartCoroutine("SendMarcoPeriodically");
            //StartCoroutine("CreateAndDestroyCubePeriodically");
        }
		public void OnDestroy()
		{
            if (Server.GetServer() == null)
                return;
			Server.GetServer().NewConnectionID -= Server_MarcoPoloExperiment_NewConnectionID;
			Server.GetServer().DisconnectionID -= Server_MarcoPoloExperiment_DisconnectionID;			
		}
		private void Server_MarcoPoloExperiment_NewConnectionID(string clientID)
        {
            clientToSee = clientID;
        }
        private void Server_MarcoPoloExperiment_DisconnectionID(string clientID)
        {
            if(clientToSee == clientID)
				clientToSee = "";
        }
		NetworkedEntity entityCreatedByClient;
        public void FixedUpdate()
        {
			ClientInputChannels channels = Server_InputReader.GetClientInputs(clientToSee);
			if(channels != null && !string.IsNullOrEmpty(clientToSee))
			{
				if(channels.flashlight.AxisIsNoLongerPositive())
				{
                    if (entityCreatedByClient == null)
                    {                        
                        entityCreatedByClient = Server_EntityInitializer.server_EntityInitializer.Instantiate("CuB0", Locator.GetPlayerTransform().transform.position + Vector3.forward*2, Quaternion.identity, InstantiateType.Buffered, (byte)SyncTransform.PositionOnly, (byte)SyncRigidbody.Both).GetAttachedNetworkedEntity();
                    }
                    else
                        Server_EntityInitializer.server_EntityInitializer.DestroyEntity(entityCreatedByClient);
                }
					
			}
        }
        IEnumerator CreateAndDestroyCubePeriodically()
        {
            while (true)
            {
                NetworkedEntity networkedEntity = Server_EntityInitializer.server_EntityInitializer.Instantiate("CuB0", Vector3.forward, Quaternion.identity, InstantiateType.Buffered,(byte)SyncTransform.PositionAndRotationOnly).GetAttachedNetworkedEntity();
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
            DynamicPacketIO.SendPackedData(HeaderValue, marco.GetBytes());
        }
        public void ReadPacket( byte[] data, ReceivedPacketData receivedPacketData)
        {
            PacketReader reader = new PacketReader(data);
            Debug.Log($"Recebemos de {receivedPacketData.ClientID}: {reader.ReadString()} | {receivedPacketData.Latency} ms - {receivedPacketData.SentTime.Ticks} ticks");
        }

        public GameObject CreateNetworkedCube()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);

            //Detector
            GameObject detector = new GameObject("cubo_detector");
            detector.transform.parent = go.transform;
            detector.transform.localPosition = Vector3.zero;

            detector.AddComponent<SphereCollider>();
            go.AddComponent<MultiFieldDetector>();
            go.AddComponent<SectorDetector>();
            //

            go.AddComponent<Rigidbody>();
            go.AddComponent<OWRigidbody>();

            NetworkedEntity networkedEntity = go.AddComponent<NetworkedEntity>();
            networkedEntity.AddEntityScript<TransformEntitySync>();
            networkedEntity.AddEntityScript<RigidbodyEntitySync>();
            return go;
        }
    }
}
