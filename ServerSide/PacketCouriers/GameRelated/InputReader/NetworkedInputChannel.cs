using ServerSide.Sockets;

namespace ServerSide.PacketCouriers.GameRelated.InputReader
{
    public class NetworkedInputChannel
    {
        private float Axis;
        private float AxisRaw;
        private bool Button;
        private bool ButtonDown;
        private bool ButtonUp;

        public void UpdateInputs(float Axis, float AxisRaw, bool Button, bool ButtonDown, bool ButtonUp)
        {
            this.Axis = Axis;
            this.AxisRaw = AxisRaw;
            this.Button = Button;
            this.ButtonDown = ButtonDown;
            this.ButtonUp = ButtonUp;
        }
        public void ResetInputs()
        {
            UpdateInputs(0f, 0f, false, false, false);
        }
        public void ReadInputChannelData(ref PacketReader reader)
        {
            UpdateInputs(reader.ReadSingle(), reader.ReadSingle(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean());
        }
        public float GetAxis()
        {
            return Axis;
        }    
        public float GetAxisRaw()
        {
            return AxisRaw;
        }        
        public bool GetButton()
        {
            return Button;
        }        
        public bool GetButtonDown()
        {
            return ButtonDown;
        }        
        public bool GetButtonUp()
        {
            return ButtonUp;
        }
    }
}