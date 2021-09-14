using System;

namespace ServerSide.PacketCouriers
{
    public interface IPacketCourier
    {
       void Receive(int latency,DateTime packetSentTime, byte[] data, string ClientID);
    }
}
