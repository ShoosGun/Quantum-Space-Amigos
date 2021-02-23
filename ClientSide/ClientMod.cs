using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CAMOWA;
using DIMOWAModLoader;
using ClientSide.Sockets;
using ClientSide.PacketCouriers.Shades;
using ClientSide.SettingsMenu;

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
            gameObject.AddComponent<ClientModSettingsMenu>();
            //balão falando que não se está conectado
            //no menu de settings ter uma opção chamada "conectar", e com ela aparecer uma caia de texto de um botão para se conectar ao servidor
            //balão falando que se está tentando conectar e depois se a conecção deu certo
            //no lugar de "conectar" agora é "desconectar"
            //se o cliente foi desconectado do servido voltar ao inicio

            //_clientSide.TryConnect(); descobrir maneira do usuario escrever o ip do servidor com GUI
        }
        private void OnDestroy()
        {
            _clientSide.Close();
        }

    }
}
