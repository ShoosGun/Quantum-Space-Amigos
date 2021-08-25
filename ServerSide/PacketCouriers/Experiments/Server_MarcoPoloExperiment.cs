using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ServerSide.Sockets.Servers;
using ServerSide.PacketCouriers.Essentials;
using ServerSide.Sockets;
using System.Collections;

namespace ServerSide.PacketCouriers.Experiments
{
    public class Server_MarcoPoloExperiment : MonoBehaviour
    {
        public Server_DynamicPacketIO DynamicPacketIO { get; private set; }
        
        const string MP_LOCALIZATION_STRING = "MarcoPoloExperiment";
        public int HeaderValue { get; private set; }

        public void Start()
        {
            Server_DynamicPacketCourierHandler handler = Server.GetServer().dynamicPacketCourierHandler;
            HeaderValue = handler.AddPacketCourier(MP_LOCALIZATION_STRING, ReadPacket);
            DynamicPacketIO = handler.DynamicPacketIO;

            StartCoroutine("SendMarcoPeriodically");
        }
        public void Update()
        {
            
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
        public void ReadPacket(byte[] data, string ClientID)
        {
            PacketReader reader = new PacketReader(data);
            Debug.Log($"Recebemos de {ClientID}: {reader.ReadString()}");
        }
    }
}
