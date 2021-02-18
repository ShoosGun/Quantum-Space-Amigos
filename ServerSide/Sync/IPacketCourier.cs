using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ServerSide.Sockets;

namespace ServerSide.Sync
{
    public interface IPacketCourier
    {
       void Receive(ref PacketReader packet, string ClientID);

    }
}
