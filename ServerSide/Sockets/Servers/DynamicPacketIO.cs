using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ServerSide.Sockets.Servers
{
    public class ReadPacketHolder
    {
        public delegate void ReadPacket(PacketReader packet);

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
            if (lastHeaderValue < byte.MaxValue -1)
            {
                byte thisHeaderValue = lastHeaderValue;
                lastHeaderValue++;
                return thisHeaderValue;
            }
            else
                return byte.MaxValue;
        }
    }
    //Teremos a classe ReadPacketHolder que guardara todos os dados para que possamos ter um IO dos pacotes
    //E teremos a classe DynamicPacketIO que cuidara dos valores que HeaderValue terão
    public class DynamicPacketIO
    {
        private ReadPacketHolder[] readPacketHolders = new ReadPacketHolder[byte.MaxValue];
        private PacketWriter packetWriter;
        
        public byte[] GetAllData()
        {
            byte[] data = packetWriter.GetBytes();
            packetWriter = null;
            return data;
        }

        public ref PacketWriter GetPacketWriter(byte HeaderValue)
        {
            if(packetWriter == null)
                packetWriter = new PacketWriter();

            if (readPacketHolders[HeaderValue] != null)
                packetWriter.Write(HeaderValue);

            return ref packetWriter;
        }

        public void ReadReceivedPacket(PacketReader packetReader)
        {
            bool continueLoop = true;
            while (continueLoop)
            {
                try
                {
                    byte HeaderValue = packetReader.ReadByte();
                    if (readPacketHolders[HeaderValue] == null)
                        throw new Exception(string.Format("This HeaderValue doesn't exist {0}", HeaderValue));

                    readPacketHolders[HeaderValue].PacketRead(packetReader);
                }
                catch (Exception ex)
                {
                    packetReader.Close();
                    continueLoop = false;
                    if (ex.GetType() != typeof(EndOfStreamException)) //Quando chegar no final da Stream, ele joga um EndOfStreamException, então sabemos que ela acabou
                        throw ex;
                }
            }
        }

        public int AddPacketCourier(ReadPacketHolder.ReadPacket readPacket)
        {
            ReadPacketHolder newReadPacketHolder = new ReadPacketHolder(readPacket);

            if (newReadPacketHolder.HeaderValue == byte.MaxValue)
                return byte.MaxValue;

            if (readPacketHolders[newReadPacketHolder.HeaderValue] != null)
                throw new Exception(string.Format("This HeaderValue is already being used {0}", newReadPacketHolder.HeaderValue));

            readPacketHolders[newReadPacketHolder.HeaderValue] = newReadPacketHolder;
            return newReadPacketHolder.HeaderValue;
        }
    }
}
