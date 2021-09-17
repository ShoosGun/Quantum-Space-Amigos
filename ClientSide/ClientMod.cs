using UnityEngine;
using CAMOWA;
using DIMOWAModLoader;

using ClientSide.Sockets;
using ClientSide.SettingsMenu;
using ClientSide.Utils;

using ClientSide.PacketCouriers.Experiments;
using ClientSide.PacketCouriers.GameRelated.Entities;
using ClientSide.PacketCouriers.GameRelated.InputReader;

namespace ClientSide
{
    public class ClientMod : MonoBehaviour
    {

        public Client _clientSide;
        private ClientDebuggerSide _debugger;

        [IMOWAModInnit("Client Test", 2, 2)]
        public static void ModInnit(string porOndeTaInicializando)
        {
            if (!Application.runInBackground)
                Application.runInBackground = true; // Thanks _nebula ;)

            if (Application.loadedLevel == 0)
                new GameObject("QSAClientMainMenu").AddComponent<ClientModMenu>();

            else if (Application.loadedLevel == 1)
            {
                if (ClientModMenu.UseClient)
                {
                    new GameObject("QSAClient").AddComponent<ClientMod>();
                    var server = GameObject.Find("QSAServer");
                    if (server != null)
                        server.SetActive(false);
                }
            }
            Debug.Log("Client Test foi iniciado em " + porOndeTaInicializando);
        }

        private void Start()
        {
            gameObject.AddComponent<MajorSectorLocator>();

            _debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();
            _clientSide = new Client(_debugger);
            
            gameObject.AddComponent<Client_MarcoPoloExperiment>();
            gameObject.AddComponent<Client_EntityInitializer>();
            gameObject.AddComponent<Client_InputReader>();

            gameObject.AddComponent<ClientModSettingsMenu>();           
        } 
        private void FixedUpdate()
        {
            _clientSide.Update();
        }
        private void OnDestroy()
        {
            _clientSide.Close();
        }

    }
}
