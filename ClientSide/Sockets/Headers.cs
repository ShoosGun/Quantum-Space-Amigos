using System;
using System.Collections.Generic;
using System.Text;

namespace ClientSide.Sockets
{
    public enum Header : byte
    {
        DISCONECT,
        SHADE_PC,
        NET_ENTITY_PC,
        REFRESH,
        OTHER //Quando se receber algo advindo de um plugin (por exemplo) ele irá primeiro enviar esse Header, ai (no estilo GlobalEvent do Outer Wilds) 
              //enviar o resto do pacote para o plugin fazer o que quiser com ele
    }
}
