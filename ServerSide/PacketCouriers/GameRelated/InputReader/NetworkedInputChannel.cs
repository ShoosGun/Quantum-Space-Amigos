using System.Collections.Generic;

using ServerSide.Sockets;
using UnityEngine;

namespace ServerSide.PacketCouriers.GameRelated.InputReader
{
    public class NetworkedInputChannel
    {
        private Queue<NetworkedInputs> networkedInputs = new Queue<NetworkedInputs>();
        private NetworkedInputs currentInput;
        private NetworkedInputs lastInput;
        public NetworkedInputChannel()
        {
            lastInput = new NetworkedInputs(0f, 0f, false, false, false);
            currentInput = new NetworkedInputs(0f, 0f, false, false, false);
        }
        public void GoToNextInput()
        {
            lastInput = currentInput;

            if (networkedInputs.Count > 0)
                currentInput = networkedInputs.Dequeue();
            else
                currentInput = new NetworkedInputs(0f, 0f, false, false, false);
        }
        public void ReadInputChannelData(ref PacketReader reader)
        {
            networkedInputs.Enqueue(new NetworkedInputs(reader.ReadSingle(), reader.ReadSingle(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean()));
        }
        public float GetAxis()
        {
            return currentInput.Axis;
        }    
        public float GetAxisRaw()
        {
            return currentInput.AxisRaw;
        }        
        public bool GetButton()
        {
            return currentInput.Button;
        }        
        public bool GetButtonDown()
        {
            return currentInput.ButtonDown;
        }
        public bool GetButtonUp()
        {
            return currentInput.ButtonUp;
        }
        //Axis only
        private const float AxisToBoolClamp = 0.2f;
        public bool AxisIsNoLongerPositive()
        {
            return lastInput.Axis > AxisToBoolClamp && currentInput.Axis < AxisToBoolClamp;
        }
        public bool AxisIsNewlyNegative()
        {
            return lastInput.Axis > -AxisToBoolClamp && currentInput.Axis < -AxisToBoolClamp;
        }
        public bool AxisIsNoLongerNegative()
        {
            return lastInput.Axis < -AxisToBoolClamp && currentInput.Axis > -AxisToBoolClamp;
        }
        public bool AxisIsNewlyNull()
        {
            return (lastInput.Axis < -AxisToBoolClamp || lastInput.Axis > AxisToBoolClamp) && currentInput.Axis > -AxisToBoolClamp && currentInput.Axis < AxisToBoolClamp;
        }
        public bool AxisIsNoLongerNull()
        {
            return lastInput.Axis > -0.2f && lastInput.Axis < 0.2f && (currentInput.Axis < -0.2f || currentInput.Axis > 0.2f);
        }
    }
    public struct NetworkedInputs
    {
        public float Axis;
        public float AxisRaw;
        public bool Button;
        public bool ButtonDown;
        public bool ButtonUp;

        public NetworkedInputs(float Axis, float AxisRaw, bool Button, bool ButtonDown, bool ButtonUp)
        {
            this.Axis = Axis;
            this.AxisRaw = AxisRaw;
            this.Button = Button;
            this.ButtonDown = ButtonDown;
            this.ButtonUp = ButtonUp;
        }
    }
}