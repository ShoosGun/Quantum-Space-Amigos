using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ServerSide.Sockets.Servers;
using DIMOWAModLoader;
using CAMOWA;

namespace ServerSide
{
    public class ServerMod : MonoBehaviour
    {

        private Server _serverSide;
        private ClientDebuggerSide _debugger;

        [IMOWAModInnit("Server Test", 1, 2)]
        public static void ModInnit(string porOndeTaInicializando)
        {
            Debug.Log("Server Test foi iniciado em " + porOndeTaInicializando);
            new GameObject("ShadeTest").AddComponent<ServerMod>();
        }
        
        private void Start()
        {
            _debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();
            _serverSide = new Server(_debugger);
        }
        
        //A parte que cria as shades foi perdida, teremos que aprender elas de novo ;-;
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
