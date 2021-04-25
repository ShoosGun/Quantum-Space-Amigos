using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CAMOWA;
using DIMOWAModLoader;
using ClientSide.Sockets;
using ClientSide.SettingsMenu;

using ClientSide.PacketCouriers;
using ClientSide.PacketCouriers.Shades;
using ClientSide.PacketCouriers.Entities;
using ClientSide.PacketCouriers.PersistentOWRigd;

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

            IPacketCourier[] PacketCouriers = new IPacketCourier[]
            {
                gameObject.AddComponent<Client_ShadePacketCourier>(),
                gameObject.AddComponent<Client_NetworkedEntityPacketCourier>(),
                gameObject.AddComponent<Client_PersistentOWRigdPacketCourier>()
            };
            

            _debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();
            _clientSide = new Client(_debugger, PacketCouriers);

            gameObject.AddComponent<ClientModSettingsMenu>();

            //balão falando que não se está conectado
            //no menu de settings ter uma opção chamada "conectar", e com ela aparecer uma caia de texto de um botão para se conectar ao servidor
            //balão falando que se está tentando conectar e depois se a conecção deu certo
            //no lugar de "conectar" agora é "desconectar"
            //se o cliente foi desconectado do servido voltar ao inicio

            //_clientSide.TryConnect(); descobrir maneira do usuario escrever o ip do servidor com GUI
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
