using System.Collections.Generic;

using ServerSide.Sockets;

namespace ServerSide.PacketCouriers.GameRelated.InputReader
{
    public class NetworkedInputChannel
    {
        private Queue<NetworkedInputs> networkedInputs = new Queue<NetworkedInputs>();
        public void GoToNextInput()
        {
            networkedInputs.Dequeue();

            if (networkedInputs.Count < 1)
                networkedInputs.Enqueue(new NetworkedInputs(0f, 0f, false, false, false));
        }
        public void ReadInputChannelData(ref PacketReader reader)
        {
            networkedInputs.Enqueue(new NetworkedInputs(reader.ReadSingle(), reader.ReadSingle(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean()));
        }
        public float GetAxis()
        {
            return networkedInputs.Peek().Axis;
        }    
        public float GetAxisRaw()
        {
            return networkedInputs.Peek().AxisRaw;
        }        
        public bool GetButton()
        {
            return networkedInputs.Peek().Button;
        }        
        public bool GetButtonDown()
        {
            return networkedInputs.Peek().ButtonDown;
        }        
        public bool GetButtonUp()
        {
            return networkedInputs.Peek().ButtonUp;
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