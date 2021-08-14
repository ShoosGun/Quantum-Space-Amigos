using System;
using System.Collections.Generic;
using System.Text;
using ClientSide.Sockets;

namespace ClientSide.PacketCouriers.Essentials
{
    //Hibrido de PacketCourier com DynamicPacketIO, ele TEM que ter o HeaderValue IGUAL a 0(ZERO). Assim teremos um canal confiavel para comunicarmos entre os computadores
    public class Client_DynamicPacketCourierHandler
    {
        public Client_DynamicPacketIO DynamicPacketIO { get; private set; }
        public int HeaderValue { get; private set; }
        private Dictionary<int, int> HeaderOfTheCouriersFromHash;
        private Dictionary<int, OnReceiveHeaderValue> WaitingForUpdatePacketCouriers;

        public delegate ReadPacketHolder.ReadPacket OnReceiveHeaderValue(int HeaderValue);

        public Client_DynamicPacketCourierHandler(ref Client_DynamicPacketIO dynamicPacketIO, int HeaderValue)
        {
            this.HeaderValue = HeaderValue;
            DynamicPacketIO = dynamicPacketIO;
            DynamicPacketIO.SetPacketCourier(HeaderValue, ReadPacket);
            HeaderOfTheCouriersFromHash = new Dictionary<int, int>();
            WaitingForUpdatePacketCouriers = new Dictionary<int, OnReceiveHeaderValue>();
        }

        public void RequestHeaders()
        {
        }
        public void SetPacketCourier(string localizationString, OnReceiveHeaderValue receiveEvent)
        {
            int hash = localizationString.GetHashCode();
            if (WaitingForUpdatePacketCouriers.ContainsKey(hash))
                throw new OperationCanceledException(string.Format("O hash de {0} ja esperando, use outra string que apresente um hash diferente de {1}", localizationString, hash));
            if(!HeaderOfTheCouriersFromHash.TryGetValue(hash, out int foundHeader))
            {
                DynamicPacketIO.SetPacketCourier(foundHeader, receiveEvent(foundHeader));
                return;
            }
            WaitingForUpdatePacketCouriers.Add(hash, receiveEvent);
        }
        public int GetHeaderValue(string localizationString)
        {
            int hash = localizationString.GetHashCode();
            if (!HeaderOfTheCouriersFromHash.TryGetValue(hash, out int foundHeader))
                throw new OperationCanceledException(string.Format("O hash de {0} ainda nao foi recebido", localizationString));
            return foundHeader;
        }
        public void ReadPacket(byte[] data)
        {
            PacketReader reader = new PacketReader(data);
            DPCHHeaders DPCHHeader = (DPCHHeaders)reader.ReadByte();
            switch (DPCHHeader)
            {
                case DPCHHeaders.UPDATE_HEADERS:
                    ReadUpdateHeaders(ref reader);
                    return;
                case DPCHHeaders.SEND_ALL_HEADERS:
                    ReadReturnAllHeaders(ref reader);
                    return;
                default:
                    return;
            }
        }
        private void ReadUpdateHeaders(ref PacketReader reader)
        {
            int amountOfHeaders = reader.ReadInt32();
            List<int> hashesWithConflictingData = new List<int>();
            for (int i = 0; i < amountOfHeaders; i++)
            {
                int hash = reader.ReadInt32();
                int headerValue = reader.ReadInt32();

                if (HeaderOfTheCouriersFromHash.TryGetValue(hash, out int foundHeader))
                {
                    if (foundHeader != headerValue)
                        hashesWithConflictingData.Add(foundHeader);
                }
                else
                {
                    HeaderOfTheCouriersFromHash[hash] = headerValue;
                    if (WaitingForUpdatePacketCouriers.TryGetValue(hash, out OnReceiveHeaderValue onReceive))
                    {
                        DynamicPacketIO.SetPacketCourier(headerValue, onReceive(headerValue));
                        WaitingForUpdatePacketCouriers.Remove(hash);
                    }
                }
            }

            if (hashesWithConflictingData.Count > 0)
            {
                string s = "Conficting information received in the past and now from the server: ";
                foreach (int i in hashesWithConflictingData)
                {
                    s += $"{i} ";
                }
                throw new Exception(s);
            }
        }
        private void ReadReturnAllHeaders(ref PacketReader reader)
        {
            int amountOfHeaders = reader.ReadInt32();
            for (int i = 0; i < amountOfHeaders; i++)
            {
                int hash = reader.ReadInt32();
                int headerValue = reader.ReadInt32();

                HeaderOfTheCouriersFromHash[hash] = headerValue;
                if (WaitingForUpdatePacketCouriers.TryGetValue(hash, out OnReceiveHeaderValue onReceive))
                {
                    DynamicPacketIO.SetPacketCourier(headerValue, onReceive(headerValue));
                    WaitingForUpdatePacketCouriers.Remove(hash);
                }
            }
        }
    }
    public enum DPCHHeaders : byte
    {
        UPDATE_HEADERS,
        SEND_ALL_HEADERS,
    }
}
