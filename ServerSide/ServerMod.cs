using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ServerSide.Sockets.Servers;
using ServerSide.PacketCouriers.Shades;
using DIMOWAModLoader;
using CAMOWA;

namespace ServerSide
{
    public class ServerMod : MonoBehaviour
    {
        public Server _serverSide;
        private ClientDebuggerSide _debugger;

        [IMOWAModInnit("Server Test", 1, 2)]
        public static void ModInnit(string porOndeTaInicializando)
        {
            if (!Application.runInBackground)
                Application.runInBackground = true; // Thanks _nebula ;)

            var client = GameObject.Find("QSAClient");
            if (client == null)
                new GameObject("QSAServer").AddComponent<ServerMod>();

            Debug.Log("Server Test foi iniciado em " + porOndeTaInicializando);
        }

        private void Start()
        {
            _debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();
            _serverSide = new Server(_debugger, gameObject.AddComponent<Server_ShadePacketCourier>());
        }

        private void FixedUpdate()
        {
            _serverSide.FixedUpdate();
            //Ver o estado do jogo e tirar uma "foto" dele
        }

        private void OnDestroy()
        {
            _serverSide.Stop();
        }
    }
}
