using ServerSide.Sockets;

namespace ServerSide.PacketCouriers
{
    public interface IPacketCourier
    {
       void Receive(byte[] data, string ClientID);
    }
}
