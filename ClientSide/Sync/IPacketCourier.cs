using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ClientSide.Sockets;

namespace ClientSide.Sync
{
    public interface IPacketCourier
    {
       void Receive(ref PacketReader packet);

    }
}
