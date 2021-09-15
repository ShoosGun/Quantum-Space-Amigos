using ClientSide.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClientSide.Sockets
{    
    public class Client_DynamicPacketIO
    {
        public delegate void ReadPacket(int latency, DateTime packetSentTime, byte[] data);
        private Dictionary<int, ReadPacket> ReadPacketHolders = new Dictionary<int, ReadPacket>();
        private PacketWriter packetWriter;
        
        public byte[] GetAllData()
        {
            if (packetWriter != null)
            {
                byte[] data = packetWriter.GetBytes();
                packetWriter = null;
                return data;
            }
            return new byte[] { };
        }

        public void SendPackedData(byte HeaderValue, byte[] data)
        {
            if (packetWriter == null)
            {
                packetWriter = new PacketWriter();
                packetWriter.Write(DateTime.UtcNow);//Send packet time
            }

            packetWriter.Write(HeaderValue);

            packetWriter.Write(data.Length);
            packetWriter.Write(data);
        }

        public void ReadReceivedPacket(ref PacketReader packetReader)
        {
            bool continueLoop = true;
            List<int> ReceivedDataFromNonExistingHeaders = new List<int>();
            DateTime sentTime = packetReader.ReadDateTime();
            int latency = (DateTime.UtcNow - sentTime).Milliseconds;
            while (continueLoop)
            {
                try
                {
                    byte HeaderValue = packetReader.ReadByte();
                    int PackedDataSize = packetReader.ReadInt32();
                    byte[] PackedData = packetReader.ReadBytes(PackedDataSize);

                    if (ReadPacketHolders.TryGetValue(HeaderValue, out ReadPacket readPacket))
                    {
                        try
                        {
                            readPacket(latency, sentTime, PackedData);
                        }
                        catch (Exception ex) { UnityEngine.Debug.Log(string.Format("{0} - {1} {2}", ex.Message, ex.Source, ex.StackTrace)); }
                    }
                    else
                    {
                        ReceivedDataFromNonExistingHeaders.Add(HeaderValue);
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

        public int AddPacketReader(string LocalizationString, ReadPacket readPacket)
        {
            int hash = Util.GerarHashInt(LocalizationString);

            if (ReadPacketHolders.ContainsKey(hash))
                throw new OperationCanceledException(string.Format("This string has a hash thay is already being used {0}", hash));

            ReadPacketHolders.Add(hash, readPacket);
            return hash;
        }
    }
}
