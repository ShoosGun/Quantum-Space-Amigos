using UnityEngine;
using CAMOWA;
using DIMOWAModLoader;

using ServerSide.Sockets.Servers;

using ServerSide.PacketCouriers.Experiments;
using ServerSide.PacketCouriers.Essentials;
using ServerSide.PacketCouriers.GameRelated.Entities;

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

            _serverSide = new Server(_debugger, gameObject.AddComponent<Server_DynamicPacketCourierHandler>());
            
            gameObject.AddComponent<Server_MarcoPoloExperiment>();
            gameObject.AddComponent<Server_EntityInitializer>();
            //gameObject.AddComponent<Server_NetworkedEntityPacketCourier>();
            //gameObject.AddComponent<Server_ShadePacketCourier>();
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
