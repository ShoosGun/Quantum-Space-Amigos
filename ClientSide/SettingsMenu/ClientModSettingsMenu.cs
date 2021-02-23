using ClientSide.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ClientSide.SettingsMenu
{
    public class ClientModSettingsMenu /*: Menu*/ : MonoBehaviour
    {
        private bool conectado = false;
        private Client cliente;
        private string IP = "127.0.0.1";
        private bool conectar = false;

        /*new*/ public void Awake()
        {
            //_menuOptions = new GUIText[] { new GUIText(), new GUIText() };
            //base.Awake();
            cliente = gameObject.GetComponent<ClientMod>()._clientSide;
        }
        
        public /*override */void Close()
        {
            
            conectar = false;

            //base.Close();
        }

        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.Backspace))
            {
                conectar = !conectar;
                if (Time.timeScale != 0)
                {
                    if (conectar)
                    {
                        Screen.showCursor = true;
                        Screen.lockCursor = false;
                    }
                    else
                    {
                        Screen.showCursor = false;
                        Screen.lockCursor = true;
                    }
                }
            }

            if(cliente.Connected != conectado)
                conectado = cliente.Connected;
        }

        public void OnGUI()
        {
            if (conectar)
            {
                IP = GUI.PasswordField(new Rect(10, 10, 150, 25), IP, "*"[0]);
                if (GUI.Button(new Rect(10, 35, 150, 25), "Conectar para esse IP"))
                {
                    cliente.TryConnect(IP,1000);
                    conectar = false;
                }
            }
        }
        
        //protected override void UpdateOptionText()
        //{
        //    for (int i = 0; i < _menuOptions.Length; i++)
        //    {
        //        switch (i)
        //        {
        //            case 0:
        //                _menuOptions[i].text = "Back";
        //                break;
        //            case 1:
        //                if (conectado)
        //                    _menuOptions[i].text = "Desconectar";
        //                else
        //                    _menuOptions[i].text = "Conectar";
        //                break;
        //        }
        //    }
        //}
        
        //protected override void ToggleOption(int direction = 0)
        //{
        //    switch (_optionIndex)
        //    {
        //        case 0:
        //            Close();
        //            break;
        //        case 1:
        //            if (conectado)
        //            {//Desconectar
        //            }
        //            else
        //                conectar = true;

        //            break;
        //    }
        //}
    }
}
