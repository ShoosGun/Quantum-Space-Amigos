using System;
using System.Collections.Generic;
using System.Text;

namespace ClientSide.PacketCouriers.Entities
{
    public interface IEntityOwner
    {
        NetworkedEntity OnAddEntity(short id);
        void OnRemoveEntity(short id);
    }
}
