using System;
using System.Collections.Generic;
using System.Text;

namespace DumbClient
{
    public enum Header : byte
    {
        DISCONECT,
        MOVEMENT,
        REFRESH
    }
    public enum SubMovementHeader : byte
    {
        HORIZONTAL_MOVEMENT,
        JUMP,
        SPIN
    }
}
