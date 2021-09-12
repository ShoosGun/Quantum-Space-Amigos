using ServerSide.Sockets;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerSide.PacketCouriers.GameRelated.InputReader
{
    public class ClientInputChannels
    {
        public NetworkedInputChannel moveX;
        public NetworkedInputChannel moveZ;
        public NetworkedInputChannel moveUp;
        public NetworkedInputChannel moveDown;

        public NetworkedInputChannel pitch;
        public NetworkedInputChannel yaw;

        public NetworkedInputChannel zoomIn;
        public NetworkedInputChannel zoomOut;

        public NetworkedInputChannel interact;
        public NetworkedInputChannel cancel;

        public NetworkedInputChannel jump;

        public NetworkedInputChannel lockOn;
        public NetworkedInputChannel probe;
        public NetworkedInputChannel altProbe;

        public NetworkedInputChannel matchVelocity;
        public NetworkedInputChannel autopilot;
        public NetworkedInputChannel landingCam;
        public NetworkedInputChannel swapRollAndYaw;

        public NetworkedInputChannel map;
        public NetworkedInputChannel pause;
        //We could make a more optimized code, or we could follow the alpha code style ;
        public ClientInputChannels()
        {
            moveX = new NetworkedInputChannel();
            moveZ = new NetworkedInputChannel();
            moveUp = new NetworkedInputChannel();
            moveDown = new NetworkedInputChannel();

            pitch = new NetworkedInputChannel();
            yaw = new NetworkedInputChannel();
            zoomIn = new NetworkedInputChannel();

            zoomOut = new NetworkedInputChannel();

            interact = new NetworkedInputChannel();
            cancel = new NetworkedInputChannel();

            jump = new NetworkedInputChannel();

            lockOn = new NetworkedInputChannel();
            probe = new NetworkedInputChannel();
            altProbe = new NetworkedInputChannel();

            matchVelocity = new NetworkedInputChannel();
            autopilot = new NetworkedInputChannel();
            landingCam = new NetworkedInputChannel();
            swapRollAndYaw = new NetworkedInputChannel();

            map = new NetworkedInputChannel();
            pause = new NetworkedInputChannel();
        }
        public void ReadClienetInputChannelsData(ref PacketReader reader)
        {
            moveX.ReadInputChannelData(ref reader);
            moveZ.ReadInputChannelData(ref reader);
            moveUp.ReadInputChannelData(ref reader);
            moveDown.ReadInputChannelData(ref reader);

            pitch.ReadInputChannelData(ref reader);
            yaw.ReadInputChannelData(ref reader);
            zoomIn.ReadInputChannelData(ref reader);

            zoomOut.ReadInputChannelData(ref reader);

            interact.ReadInputChannelData(ref reader);
            cancel.ReadInputChannelData(ref reader);

            jump.ReadInputChannelData(ref reader);

            lockOn.ReadInputChannelData(ref reader);
            probe.ReadInputChannelData(ref reader);
            altProbe.ReadInputChannelData(ref reader);

            matchVelocity.ReadInputChannelData(ref reader);
            autopilot.ReadInputChannelData(ref reader);
            landingCam.ReadInputChannelData(ref reader);
            swapRollAndYaw.ReadInputChannelData(ref reader);

            map.ReadInputChannelData(ref reader);
            pause.ReadInputChannelData(ref reader);
        }
    }
}
