using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ClientSide.SettingsMenu
{
    public class ClientModSettingsMenuTrigger : MonoBehaviour
    {
        ClientModSettingsMenu settingsMenu;
        public void Awake()
        {
            settingsMenu = gameObject.AddComponent<ClientModSettingsMenu>();
        }
        //public void Update()
        //{
        //    bool buttonReleased = Input.GetKeyUp(KeyCode.Backspace);
        //    if (settingsMenu.enabled && buttonReleased)
        //        settingsMenu.Close();

        //    else if (buttonReleased)
        //        settingsMenu.Open();
        //}
    }
}
