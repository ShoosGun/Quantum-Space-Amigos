using System;
using System.Collections.Generic;
using System.Text;
using ServerSide.Utils;
using ServerSide.Sockets.Servers;
using ServerSide.Sockets;
using UnityEngine;
using ServerSide.PacketCouriers.Essentials;

namespace ServerSide.PacketCouriers.NetworkedMessenger
{
    //Será Equivalente ao GlobalMessenger normal (só que não)
    public class Server_NetworkedMessengerPacketCourier : MonoBehaviour, IPacketCourier
    {
        private Server_DynamicPacketCourierHandler dynamicPacketCourierHandler;
        const string NM_LOCALIZATION_STRING = "NetworkedMessengerPacketCourier";
        private int HeaderValue;

        private Dictionary<long, List<string>> eventTable = new Dictionary<long, List<string>>();

        public void Start()
        {
            dynamicPacketCourierHandler = GameObject.Find("QSAServer").GetComponent<ServerMod>()._serverSide.dynamicPacketCourierHandler;
            HeaderValue = dynamicPacketCourierHandler.AddPacketCourier(NM_LOCALIZATION_STRING, Receive);
        }

        //Se receber info que o cliente quer participar desse evento, colocalo na lista do evento
        public void AddListener(long hash, string ClientID)
        {
            if (eventTable.ContainsKey(hash))
                if (!eventTable[hash].Contains(ClientID))
                    eventTable[hash].Add(ClientID);

            eventTable.Add(hash, new List<string>() { ClientID });
        }

        public void RemoveListener(long hash, string ClientID)
        {
            if (!eventTable.ContainsKey(hash))
                return;

            if (eventTable[hash].Contains(ClientID))
                eventTable[hash].Remove(ClientID);
        }

        //Falar que o evento ocorreu
        public void FireEvent(string eventType)
        {
            long hash = Util.GerarHash(eventType);
            if (!eventTable.ContainsKey(hash))
                return;
            PacketWriter packet = new PacketWriter();
            packet.Write(hash); //Hash do Evento

            dynamicPacketCourierHandler.DynamicPacketIO.SendPackedData((byte)HeaderValue, packet.GetBytes(), eventTable[hash].ToArray());
        }

        public void Receive(int latency,DateTime packetSentTime, byte[] data, string ClientID)
        {
            PacketReader packet = new PacketReader(data);
            switch(packet.ReadByte())
            {
                case 0: // Entrar
                    AddListener(packet.ReadInt64(), ClientID);
                    return;

                case 1: // Sair
                    RemoveListener(packet.ReadInt64(), ClientID);
                    return;
            }
        }

    }
}
