using System;
using System.Collections.Generic;
using System.Text;
using ServerSide.Utils;
using ServerSide.Sockets.Servers;
using ServerSide.Sockets;

namespace ServerSide.PacketCouriers.Essentials
{
    //Hibrido de PacketCourier com DynamicPacketIO, ele TEM que ter o HeaderValue IGUAL a 0(ZERO). Assim teremos um canal confiavel para comunicarmos entre os computadores
    public class DynamicPacketCourierHandler
    {
        public DynamicPacketIO DynamicPacketIO { get; private set; }
        public int HeaderValue { get; private set; }
        private List<int> UsedHashes;
        private List<KeyValuePair<int, int>> HashesToUpdate;
        public DynamicPacketCourierHandler(ref DynamicPacketIO dynamicPacketIO)
        {
            DynamicPacketIO = dynamicPacketIO;
            HeaderValue = DynamicPacketIO.AddPacketCourier(ReadPacket);
            UsedHashes = new List<int> { HeaderValue };
            HashesToUpdate = new List<KeyValuePair<int, int>>();
        }

        public void UpdateHeaders()
        {
            if (HashesToUpdate.Count > 0)
            {
                PacketWriter writer = DynamicPacketIO.GetPacketWriter((byte)HeaderValue);
                writer.Write(HashesToUpdate.Count);
                for(int i =0; i< HashesToUpdate.Count; i++)
                {
                    writer.Write(HashesToUpdate[i].Key);//O hash
                    writer.Write(HashesToUpdate[i].Value);//O valor do HeaderValue
                }
                HashesToUpdate.Clear();
            }
        }
        public int AddPacketCourier(string localizationString, ReadPacketHolder.ReadPacket readPacket)
        {
            int hash = localizationString.GetHashCode();
            if (UsedHashes.Exists(i => i == hash))
                throw new OperationCanceledException(string.Format("O hash de {0} ja esta gravado, use outra string que apresente um hash diferente de {1}", localizationString, hash));

            int courierHeaderValue = DynamicPacketIO.AddPacketCourier(readPacket);
            UsedHashes.Add(hash);
            HashesToUpdate.Add(new KeyValuePair<int, int>(hash, courierHeaderValue));
            return courierHeaderValue;
        }
        public void ReadPacket(PacketReader reader)
        {
        }
    }
}
