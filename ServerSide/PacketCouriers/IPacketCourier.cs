using ServerSide.Sockets;

namespace ServerSide.PacketCouriers
{
    public interface IPacketCourier
    {
       void Receive(ref PacketReader packet, string ClientID);

    }
}
