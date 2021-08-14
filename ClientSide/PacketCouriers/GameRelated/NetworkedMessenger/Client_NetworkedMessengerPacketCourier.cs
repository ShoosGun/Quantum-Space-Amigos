using System;
using System.Collections.Generic;
using System.Text;
using ClientSide.Sockets;
using UnityEngine;

namespace ClientSide.PacketCouriers.NetworkedMessenger
{
    //Será Equivalente ao GlobalMessenger normal (só que não)[porém que sim]
    public class Server_NetworkedMessengerPacketCourier : MonoBehaviour, IPacketCourier
    {
        private Client Client;
        
        private Dictionary<long, Delegate> eventTable = new Dictionary<long, Delegate>();

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
            Client = GameObject.Find("QSAClient").GetComponent<ClientMod>()._clientSide;
        }
        
        public void AddListener(string eventType, Callback handler)
        {
            long hash = GerarHash(eventType);

            if (!eventTable.ContainsKey(hash))
            {
                eventTable.Add(hash, null);
                PacketWriter packet = new PacketWriter();
                packet.Write((byte)3);
                packet.Write((byte)0);
                packet.Write(hash);
                Client.Send(packet.GetBytes());
            }

            eventTable[hash] = (Callback)Delegate.Combine((Callback)eventTable[hash], handler);
        }

        public void RemoveListener(string eventType, Callback handler)
        {
            long hash = GerarHash(eventType);

            if (eventTable.ContainsKey(hash))
            {
                eventTable[hash] = (Callback)Delegate.Remove((Callback)eventTable[hash], handler);

                PacketWriter packet = new PacketWriter();
                packet.Write((byte)3);
                packet.Write((byte)1);
                packet.Write(hash);
                Client.Send(packet.GetBytes());

                if (eventTable[hash] == null)
                    eventTable.Remove(hash);
            }
        }
        
        public void FireEvent(long hash)
        {
            Delegate @delegate;
            if (eventTable.TryGetValue(hash, out @delegate))
                ((Callback)@delegate)?.Invoke();
        }

        public void Receive(ref PacketReader packet)
        {
            FireEvent(packet.ReadInt64());
        }

    }
}
