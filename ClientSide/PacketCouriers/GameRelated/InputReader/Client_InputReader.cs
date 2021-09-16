using System;
using UnityEngine;

using ClientSide.Sockets;


namespace ClientSide.PacketCouriers.GameRelated.InputReader
{
    public class Client_InputReader : MonoBehaviour
    {
        private Client client;
        private Client_DynamicPacketIO DynamicPacketIO;
        const string IR_LOCALIZATION_STRING = "InputReader";
        public int HeaderValue { get; private set; }

        public void Awake()
        {
            client = Client.GetClient();
            DynamicPacketIO = client.DynamicPacketIO;
            HeaderValue = DynamicPacketIO.AddPacketReader(IR_LOCALIZATION_STRING, ReadPacket);
        }
        public void FixedUpdate()
        {
            if(client.Connected)
                SendInputUpdate();
        }
        public void SendInputUpdate()
        {
            PacketWriter writer = new PacketWriter();
            for (int i = 0; i < Channels.Length; i++)
                WriteInputChannelData(ref writer, Channels[i]);

            DynamicPacketIO.SendPackedData((byte)HeaderValue, writer.GetBytes());
        }
        public void WriteInputChannelData(ref PacketWriter writer, InputChannel inputChannel)
        {
            writer.Write(inputChannel.GetAxis());
            writer.Write(inputChannel.GetAxisRaw());
            writer.Write(inputChannel.GetButton());
            writer.Write(inputChannel.GetButtonDown());
            writer.Write(inputChannel.GetButtonUp());
        }        
        public void ReadPacket(byte[] data, ReceivedPacketData receivedPacketData)
        {
        }

        private readonly InputChannel[] Channels =
        {
            InputChannels.moveX,
            InputChannels.moveZ,
            InputChannels.moveUp,
            InputChannels.moveDown,

            InputChannels.pitch,
            InputChannels.yaw,

            InputChannels.zoomIn,
            InputChannels.zoomOut,

            InputChannels.interact,
            InputChannels.cancel,

            InputChannels.jump,

            InputChannels.lockOn,
            InputChannels.probe,
            InputChannels.altProbe,

            InputChannels.matchVelocity,
            InputChannels.autopilot,
            InputChannels.landingCam,
            InputChannels.swapRollAndYaw,

            InputChannels.map,
            InputChannels.pause
        };
    }
}
