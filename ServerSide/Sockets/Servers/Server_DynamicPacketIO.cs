using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServerSide.Sockets.Servers
{
    public class ReadPacketHolder
    {
        public const byte MAX_AMOUNT_OF_HEADER_VALUES = byte.MaxValue;
        public delegate void ReadPacket(byte[] data, string ClientID);

        public byte HeaderValue { get; private set; }
        public ReadPacket PacketRead { get; private set; }

        public ReadPacketHolder(ReadPacket readPacket)
        {
            HeaderValue = GetUniqueHeaderValue();
            PacketRead = readPacket;
        }

        private static byte lastHeaderValue = 0;
        private byte GetUniqueHeaderValue()
        {
            if (lastHeaderValue < MAX_AMOUNT_OF_HEADER_VALUES)
            {
                byte thisHeaderValue = lastHeaderValue;
                lastHeaderValue++;
                return thisHeaderValue;
            }
            else
                return MAX_AMOUNT_OF_HEADER_VALUES;
        }

        public static void ResetReadPacketHolderHeaderValues()
        {
            lastHeaderValue = 0;
        }
    }
    //Teremos a classe ReadPacketHolder que guardara todos os dados para que possamos ter um IO dos pacotes
    //E teremos a classe DynamicPacketIO que cuidara dos valores que HeaderValue terão
    public class Server_DynamicPacketIO
    {
        private ReadPacketHolder[] readPacketHolders;
        private PacketWriter globalPacketWriter;
        private Dictionary<string, PacketWriter> clientSpecificPacketWriters;

        public Server_DynamicPacketIO()
        {
            ReadPacketHolder.ResetReadPacketHolderHeaderValues();
            clientSpecificPacketWriters = new Dictionary<string, PacketWriter>();
            readPacketHolders = new ReadPacketHolder[ReadPacketHolder.MAX_AMOUNT_OF_HEADER_VALUES];
        }

        private byte[] GetAllData(ref PacketWriter packetWriter)
        {
            if (packetWriter != null)
            {
                byte[] data = packetWriter.GetBytes();
                packetWriter = null;
                return data;
            }
            return new byte[]{ };
        }
        public byte[] GetGlobalPacketWriterData()
        {
            return GetAllData(ref globalPacketWriter);
        }
        public byte[] GetClientSpecificPacketWriterData(string ClientID)
        {
            if (clientSpecificPacketWriters.TryGetValue(ClientID, out PacketWriter packet))
            {
                clientSpecificPacketWriters.Remove(ClientID);
                return GetAllData(ref packet);
            }
            return new byte[] { };
        }

        public void ResetClientSpecificDataHolder()
        {
            clientSpecificPacketWriters.Clear();
        }

        private void WritePackedData(byte HeaderValue, byte[] data, ref PacketWriter writer)
        {
            if (writer == null)
                writer = new PacketWriter();

            if (readPacketHolders[HeaderValue] != null)
                writer.Write(HeaderValue);

            writer.Write(data.Length);
            writer.Write(data);
        }
        public void SendPackedData(byte HeaderValue, byte[] data, params string[] ClientIDs)
        {
            if (ClientIDs.Length == 0)
            {
                WritePackedData(HeaderValue, data, ref globalPacketWriter);
                return;
            }
            for (int i = 0; i < ClientIDs.Length; i++)
            {
                if (!clientSpecificPacketWriters.ContainsKey(ClientIDs[i]))
                    clientSpecificPacketWriters.Add(ClientIDs[i], new PacketWriter());

                if (clientSpecificPacketWriters[ClientIDs[i]] == null)
                    clientSpecificPacketWriters[ClientIDs[i]] = new PacketWriter();

                if (readPacketHolders[HeaderValue] != null)
                    clientSpecificPacketWriters[ClientIDs[i]].Write(HeaderValue);

                clientSpecificPacketWriters[ClientIDs[i]].Write(data.Length);
                clientSpecificPacketWriters[ClientIDs[i]].Write(data);
            }
        }

        public void ReadReceivedPacket(ref PacketReader packetReader, string ClientID)
        {
            bool continueLoop = true;
            List<int> ReceivedDataFromNonExistingHeaders = new List<int>();
            while (continueLoop)
            {
                try
                {
                    byte HeaderValue = packetReader.ReadByte();
                    int PackedDataSize = packetReader.ReadInt32();
                    byte[] PackedData = packetReader.ReadBytes(PackedDataSize);
                    if (readPacketHolders[HeaderValue] == null)
                    {
                        ReceivedDataFromNonExistingHeaders.Add(HeaderValue);
                    }
                    else
                    {
                        try
                        {
                            readPacketHolders[HeaderValue].PacketRead(PackedData, ClientID);
                        }
                        catch (Exception ex) { UnityEngine.Debug.Log(string.Format("{0} - {1} {2}", ex.Message, ex.Source, ex.StackTrace)); }
                    }
                }
                catch (Exception ex)
                {
                    packetReader.Close();
                    continueLoop = false;
                    if (ex.GetType() != typeof(EndOfStreamException)) //Quando chegar no final da Stream, ele joga um EndOfStreamException, então sabemos que ela acabou sem outros erros
                        throw ex;
                }
            }
            if (ReceivedDataFromNonExistingHeaders.Count > 0)
            {
                string s = "Received data from these non existing headers: ";
                foreach (int i in ReceivedDataFromNonExistingHeaders)
                {
                    s += $"{i} ";
                }
                throw new Exception(s);
            }
        }

        public int AddPacketCourier(ReadPacketHolder.ReadPacket readPacket)
        {
            ReadPacketHolder newReadPacketHolder = new ReadPacketHolder(readPacket);

            if (newReadPacketHolder.HeaderValue == ReadPacketHolder.MAX_AMOUNT_OF_HEADER_VALUES)
                return ReadPacketHolder.MAX_AMOUNT_OF_HEADER_VALUES;

            if (readPacketHolders[newReadPacketHolder.HeaderValue] != null)
                throw new OperationCanceledException(string.Format("This HeaderValue is already being used {0}", newReadPacketHolder.HeaderValue));

            readPacketHolders[newReadPacketHolder.HeaderValue] = newReadPacketHolder;
            return newReadPacketHolder.HeaderValue;
        }
    }
}
