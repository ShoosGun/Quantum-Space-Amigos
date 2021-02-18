using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CAMOWA;
using DIMOWAModLoader;
using ClientSide.Sockets;
using ClientSide.PacketCouriers.Shades;

namespace ClientSide
{
    public class ClientMod : MonoBehaviour
    {
        public Client _clientSide;
        private ClientDebuggerSide _debugger;

        [IMOWAModInnit("Client Test", 1, 2)]
        public static void ModInnit(string porOndeTaInicializando)
        {
            if (!Application.runInBackground)
                Application.runInBackground = true; // Thanks _nebula ;)

            Debug.Log("Client Test foi iniciado em " + porOndeTaInicializando);
            new GameObject("QSAClient").AddComponent<ClientMod>();
        }

        private void Start()
        {
            _debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();
            _clientSide = new Client(_debugger, gameObject.AddComponent<Client_ShadePacketCourier>());
            //_clientSide.TryConnect(); descobrir maneira do usuario escrever o ip do servidor com GUI
        }

    }
}
