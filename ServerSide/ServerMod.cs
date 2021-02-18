using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ServerSide.Sockets.Servers;
using ServerSide.Shades;
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

            Debug.Log("Server Test foi iniciado em " + porOndeTaInicializando);
            new GameObject("ShadeTest").AddComponent<ServerMod>();
        }

        private void Start()
        {
            _debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();
            _serverSide = new Server(_debugger, gameObject.AddComponent<ShadePacketCourier>());
        }
        
        private void FixedUpdate()
        {
            _serverSide.FixedUpdate();
            //Ver o estado do jogo e tirar uma "foto" dele
        }
        private void Update()
        {
            _serverSide.Update();
        }

        private void OnDestroy()
        {
            _serverSide.Stop();
        }
    }
}
