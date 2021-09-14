using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClientSide.Sockets
{
    public class ReadPacketHolder
    {
        public const byte MAX_AMOUNT_OF_HEADER_VALUES = byte.MaxValue;
        public delegate void ReadPacket(int latency,DateTime sentPacketTime, byte[] data);
        
        public ReadPacket PacketRead { get; private set; }

        public ReadPacketHolder(ReadPacket readPacket)
        {
            PacketRead = readPacket;
        }
    }
    //Teremos a classe ReadPacketHolder que guardara todos os dados para que possamos ter um IO dos pacotes
    //E teremos a classe DynamicPacketIO que cuidara dos valores que HeaderValue terão
    public class Client_DynamicPacketIO
    {
        private ReadPacketHolder[] readPacketHolders = new ReadPacketHolder[ReadPacketHolder.MAX_AMOUNT_OF_HEADER_VALUES];
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
            DateTime sentTime = packetReader.ReadDateTime();
            int latency = (DateTime.UtcNow - sentTime).Milliseconds;
            UnityEngine.Debug.Log(string.Format("Lendo info recebida com delay de {0}", latency));

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
                            UnityEngine.Debug.Log("Passando para " + HeaderValue);
                            readPacketHolders[HeaderValue].PacketRead(latency, sentTime, PackedData);
                        }
                        catch(Exception ex) { UnityEngine.Debug.Log(string.Format("{0} - {1} {2}", ex.Message, ex.Source, ex.StackTrace)); }
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

        public void SetPacketCourier(int headerValue, ReadPacketHolder.ReadPacket readPacket)
        {
            if (headerValue == ReadPacketHolder.MAX_AMOUNT_OF_HEADER_VALUES - 1)
                throw new IndexOutOfRangeException(string.Format("The HeaderValue cannot be equal or bigger then ", ReadPacketHolder.MAX_AMOUNT_OF_HEADER_VALUES - 1));

            if (readPacketHolders[headerValue] != null)
                throw new OperationCanceledException(string.Format("This HeaderValue is already being used {0}", headerValue));

            readPacketHolders[headerValue] = new ReadPacketHolder(readPacket);
        }
    }
}
