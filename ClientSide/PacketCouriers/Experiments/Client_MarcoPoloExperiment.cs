using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ClientSide.PacketCouriers.Essentials;
using ClientSide.Sockets;
using System.Collections;

namespace ClientSide.PacketCouriers.Experiments
{
    public class Client_MarcoPoloExperiment : MonoBehaviour
    {
        public Client_DynamicPacketIO DynamicPacketIO { get; private set; }
        const string MP_LOCALIZATION_STRING = "MarcoPoloExperiment";
        public int HeaderValue { get; private set; }
        private bool GotTheHeaderValue = false;

        public void Start()
        {
            Client_DynamicPacketCourierHandler handler = Client.GetClient().dynamicPacketCourierHandler;
            handler.SetPacketCourier(MP_LOCALIZATION_STRING,OnReceiveHeaderValue);
            DynamicPacketIO = handler.DynamicPacketIO;
        }

        private ReadPacketHolder.ReadPacket OnReceiveHeaderValue(int HeaderValue)
        {
            this.HeaderValue = HeaderValue;
            GotTheHeaderValue = true;
            return ReadPacket;
        }

        public void Update()
        {
            if(GotTheHeaderValue)
                StartCoroutine("SendMarcoPeriodically");
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
            DynamicPacketIO.SendPackedData((byte)HeaderValue, polo.GetBytes());
        }
        public void ReadPacket(byte[] data)
        {
            PacketReader reader = new PacketReader(data);
            Debug.Log($"Recebemos do servidor: {reader.ReadString()}");
        }
    }
}
