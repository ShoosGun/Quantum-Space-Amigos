using System;
using System.Collections.Generic;
using UnityEngine;

using ServerSide.Sockets.Servers;
using ServerSide.Sockets;

namespace ServerSide.PacketCouriers.GameRelated.InputReader
{
    public class Server_InputReader : MonoBehaviour
    {
        public Server_DynamicPacketIO DynamicPacketIO { get; private set; }

        const string IR_LOCALIZATION_STRING = "InputReader";
        public int HeaderValue { get; private set; }

        private Dictionary<string, ClientInputChannels> ClientsInputChannels;

        public void Start()
        {
            DynamicPacketIO = Server.GetServer().DynamicPacketIO;
            HeaderValue = DynamicPacketIO.AddPacketReader(IR_LOCALIZATION_STRING, ReadPacket);

            ClientsInputChannels = new Dictionary<string, ClientInputChannels>();

            Server.GetServer().NewConnectionID += Server_InputReader_NewConnectionID;
            Server.GetServer().DisconnectionID += Server_InputReader_DisconnectionID;
        }
        public void OnDestroy()
        {
            if (Server.GetServer() == null)
                return;

            Server.GetServer().NewConnectionID -= Server_InputReader_NewConnectionID;
            Server.GetServer().DisconnectionID -= Server_InputReader_DisconnectionID;
        }

        private void Server_InputReader_NewConnectionID(string clientID)
        {
            ClientsInputChannels.Add(clientID, new ClientInputChannels());
        }
        private void Server_InputReader_DisconnectionID(string clientID)
        {
            ClientsInputChannels.Remove(clientID);
        }

        public void FixedUpdate()
        {
            foreach (var inputChannel in ClientsInputChannels)
                inputChannel.Value.GoToNextInputsInInputChannels();
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
            PacketReader reader = new PacketReader(data);
            if (ClientsInputChannels.TryGetValue(receivedPacketData.ClientID, out ClientInputChannels inputChannels))
                inputChannels.ReadClienetInputChannelsData(receivedPacketData.Latency, receivedPacketData.SentTime, ref reader);
        }
    }
}
