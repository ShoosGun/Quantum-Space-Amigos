using System;
using System.Collections.Generic;
using System.Text;

namespace ClientSide.PacketCouriers.Entities
{
    public interface IEntityOwner
    {
        NetworkedEntity OnAddEntity(ushort id);
        void OnRemoveEntity(ushort id);
    }
}
