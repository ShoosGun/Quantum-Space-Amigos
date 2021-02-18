using System;
using System.Collections.Generic;
using System.Text;

namespace DumbClient
{
    public enum Header : byte
    {
        DISCONECT,
        SHADE_PC,
        REFRESH,
        OTHER
    }

    //Parte do Shades no cliente

    public enum ShadeHeader : byte
    {
        MOVEMENT,
        SET_NAME
    }

    public enum ShadeMovementSubHeader : byte
    {
        HORIZONTAL_MOVEMENT,
        JUMP,
        SPIN
    }
}
