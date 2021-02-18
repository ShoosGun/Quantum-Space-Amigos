using System;
using System.Collections.Generic;
using System.Text;

namespace ServerSide.Sockets.Servers
{
    public enum UpdatesTypes : byte
    {
        NEW_CONNECTION,
        RECEIVED_DATA,
        //...
        DISCONNECTION,
        UpdatesTypes_Size
    }
}
