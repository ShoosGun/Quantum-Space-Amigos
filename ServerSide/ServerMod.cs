using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ServerSide.Sockets.Servers;
using CAMOWA;
using DIMOWAModLoader;

//using ServerSide.PacketCouriers;
//using ServerSide.PacketCouriers.Shades;
//using ServerSide.PacketCouriers.Entities;
//using ServerSide.PacketCouriers.PersistentOWRigdSync;

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
            IPacketCourier[] PacketCouriers = new IPacketCourier[]
            {
                gameObject.AddComponent<Server_ShadePacketCourier>(), //0
                gameObject.AddComponent<Server_NetworkedEntityPacketCourier>(), //1
                gameObject.AddComponent<Server_PersistentOWRigdPacketCourier>() //2
            };

            _debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();
            _serverSide = new Server(_debugger, PacketCouriers);
        }

        private void FixedUpdate()
        {
            _serverSide.FixedUpdate(); // Big Brain time
        }

        private void OnDestroy()
        {
            _serverSide.Stop();
        }
    }
}
