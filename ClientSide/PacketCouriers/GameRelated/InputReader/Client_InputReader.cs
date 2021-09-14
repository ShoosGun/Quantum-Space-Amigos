using System;
using UnityEngine;

using ClientSide.Sockets;
using ClientSide.PacketCouriers.Essentials;


namespace ClientSide.PacketCouriers.GameRelated.InputReader
{
    public class Client_InputReader : MonoBehaviour
    {
        public Client_DynamicPacketIO DynamicPacketIO { get; private set; }
        const string IR_LOCALIZATION_STRING = "InputReader";
        public int HeaderValue { get; private set; }

        public void Awake()
        {
            Client_DynamicPacketCourierHandler handler = Client.GetClient().dynamicPacketCourierHandler;
            handler.SetPacketCourier(IR_LOCALIZATION_STRING, OnReceiveHeaderValue);
            DynamicPacketIO = handler.DynamicPacketIO;
        }
        private ReadPacketHolder.ReadPacket OnReceiveHeaderValue(int HeaderValue)
        {
            this.HeaderValue = HeaderValue;
            return ReadPacket;
        }
        public void FixedUpdate()
        {
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
        public void ReadPacket(int latency, DateTime sentPacketTime, byte[] data)
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
