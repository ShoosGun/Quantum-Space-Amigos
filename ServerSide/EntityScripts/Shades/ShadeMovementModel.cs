using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using DIMOWAModLoader;
using ServerSide.PacketCouriers.GameRelated.InputReader;

namespace ServerSide.EntityScripts.Shades
{
    public class ShadeMovementModel : CharacterMovementModel
    {
        private string ClientControllerString = "";
        
        private void Start()
        {
            _jumpSpeed = 6f;
            _turnRate = 160f;
        }
        protected override Vector3 GetMoveInput()
        {
            ClientInputChannels channels = Server_InputReader.GetClientInputs(ClientControllerString);
            if(channels != null)
                return new Vector3(channels.moveX.GetAxis(), 0f, channels.moveZ.GetAxis());

            return Vector3.zero;
        }

        protected override float GetTurnInput()
        {
            ClientInputChannels channels = Server_InputReader.GetClientInputs(ClientControllerString);
            if (channels != null)
                return channels.yaw.GetAxis();

            return 0f;
        }

        protected override bool GetJumpInput()
        {
            ClientInputChannels channels = Server_InputReader.GetClientInputs(ClientControllerString);
            if (channels != null)
                return channels.jump.GetButtonDown();

            return false;
        }        
    }
}
