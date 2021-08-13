using System;
using System.Collections.Generic;
using System.Text;
using ServerSide.Sockets.Servers;
using ServerSide.Sockets;
using UnityEngine;

namespace ServerSide.PacketCouriers.NetworkedMessenger
{
    //Será Equivalente ao GlobalMessenger normal (só que não)
    public class Server_NetworkedMessengerPacketCourier : MonoBehaviour, IPacketCourier
    {
        private Server Server;
        
        private Dictionary<long, List<string>> eventTable = new Dictionary<long, List<string>>();

        // Código de Cpp em C# 0_0
        public static long GerarHash(string s) //Gerar o Hash code de strings
        {
            const int p = 53;
            const int m = 1000000000 + 9; //10e9 + 9
            long hash_value = 0;
            long p_pow = 1;
            foreach (char c in s)
            {
                hash_value = (hash_value + (c - 'a' + 1) * p_pow) % m;
                p_pow = p_pow * p % m;
            }
            return hash_value;
        }

        public void Start()
        {
            Server = GameObject.Find("QSAServer").GetComponent<ServerMod>()._serverSide;
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
            long hash = GerarHash(eventType);
            if (!eventTable.ContainsKey(hash))
                return;
            PacketWriter packet = new PacketWriter();
            packet.Write((byte)3);    //Header
            packet.Write(hash); //Hash do Evento

            Server.Send(eventTable[hash].ToArray(), packet.GetBytes());
        }

        public void Receive(ref PacketReader packet, string ClientID)
        {
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
