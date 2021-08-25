using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.IO;

namespace DumbClient
{
    class Program
    {
        private static Socket clientSck;
        private static readonly object packets_lock = new object();
        private static List<byte[]> packets = new List<byte[]>();
        private static bool desconectados = false;
        private static bool on = true;
        
        //TODO Trocar aonde pega o hash pela função do Utils!
        const string MP_LOCALIZATION_STRING = "MarcoPoloExperiment";
        private static int MarcoPoloHeader = -1;

        private static void Main(string[] args)
        {
            Console.Title = "Dumb Client";
            Console.WriteLine("Escreva o IP do servidor");
            string serverIP = Console.ReadLine();
            if (serverIP.Length == 0 || serverIP.ToLower() == "localhost") 
                serverIP = "127.1.0.0";
            Console.WriteLine("Pressione ENTER para tentar conectar no servidor, e de um nome para ele caso queira");
            string shadeName = Console.ReadLine();

            clientSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSck.Connect(new IPEndPoint(IPAddress.Parse(serverIP), 2121));

                clientSck.BeginReceive(new byte[] { 0 }, 0, 0, 0, callback, null);

                Thread.Sleep(100);

                //PacketWriter namePacket = new PacketWriter();
                //namePacket.Write((byte)Header.SHADE_PC);
                //namePacket.Write((byte)ShadeHeader.SET_NAME);
                //namePacket.Write(shadeName);
                //byte[] namePacketBuffer = namePacket.GetBytes();
                
                //clientSck.Send(BitConverter.GetBytes(namePacketBuffer.Length));
                //clientSck.Send(namePacketBuffer);

                //namePacket.Close();

                Console.WriteLine("Precione P para desconectar esse cliente");
                Console.CursorVisible = false;                
                while (on)
                {
                    bool packets_NotLoked = Monitor.TryEnter(packets_lock, 10);
                    if (packets_NotLoked)
                    {
                        try
                        {
                            for (int i =0;i<packets.Count;i++)
                            {
                                bool jaFoiPeloPacote = false;
                                PacketReader reader = new PacketReader(packets[i]);
                                int j = 0;
                                Console.WriteLine("Novo Pacote");
                                while (j<4)
                                {
                                    j++;
                                    try
                                    {
                                        byte headerValue = reader.ReadByte();
                                        int dataSize = reader.ReadInt32();
                                        byte[] data = reader.ReadBytes(dataSize);
                                        Console.WriteLine("Header " + headerValue);
                                        Console.WriteLine("Tamanho da mensage: " + dataSize);
                                        PacketReader packetReader = new PacketReader(data);
                                        //Console.WriteLine("Tamanho da mensage (2): " + data.Length);
                                        if (headerValue == 0)
                                        {
                                            Console.WriteLine("Informacao sobre os Headers!!!");
                                            packetReader.ReadByte();
                                            int amountOfHeaders = packetReader.ReadInt32();
                                            Console.WriteLine(amountOfHeaders);
                                            for (int k = 0; k < amountOfHeaders; k++)
                                            {
                                                long hash = packetReader.ReadInt64();
                                                int value = packetReader.ReadInt32();
                                                Console.WriteLine("Hash {0}", hash);
                                                Console.WriteLine("de {0} vem {1}", MP_LOCALIZATION_STRING, GerarHash(MP_LOCALIZATION_STRING));
                                                if (hash == GerarHash(MP_LOCALIZATION_STRING))
                                                {
                                                    MarcoPoloHeader = value;
                                                    Console.WriteLine("Header de {0} eh {1}", MP_LOCALIZATION_STRING, MarcoPoloHeader);
                                                }
                                            }
                                        }
                                        else if (headerValue == MarcoPoloHeader && !jaFoiPeloPacote)
                                        {
                                            jaFoiPeloPacote = true;
                                            Console.WriteLine("Mensagem: {0}", packetReader.ReadString());
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex.GetType() != typeof(EndOfStreamException)) //Quando chegar no final da Stream, ele joga um EndOfStreamException, então sabemos que ela acabou
                                            Console.WriteLine("Erro ao ler dados: {0} | {1}", ex.StackTrace, ex.Message);
                                        break;
                                    }
                                }
                            }
                            packets.Clear();
                        }
                        finally
                        {
                            Monitor.Exit(packets_lock);
                        }
                    }

                    if (Console.KeyAvailable == true)
                    {
                        ConsoleKeyInfo keyPressed = Console.ReadKey(true);
                        switch (keyPressed.Key)
                        {
                            //Desligar
                            case ConsoleKey.P:
                                on = false;
                                break;
                            default:
                                break;
                        }
                    }

                    if (desconectados)
                        on = false;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Não foi possivel conectar o cliente com o servidor: {0} >> {1}", ex.StackTrace , ex.Message);
            }
            Console.WriteLine("Pressione ENTER para fechar o programa");
            clientSck.Close();
            Console.Read();
        }
        private static void callback(IAsyncResult ar)
        {
            try
            {
                clientSck.EndReceive(ar);
                 byte[] receivedBuffer = new byte[4];
                clientSck.Receive(receivedBuffer, 0, 4, 0);
                int dataSize = BitConverter.ToInt32(receivedBuffer, 0);

                if (dataSize <= 0)
                    throw new SocketException();
                
                byte[] buffer = new byte[dataSize];
                int received = clientSck.Receive(buffer, 0, buffer.Length, 0);
                while (received < dataSize)
                {
                    received += clientSck.Receive(buffer, received, dataSize - received, 0);
                }
                lock (packets_lock)
                {
                    packets.Add(buffer);
                }


                clientSck.BeginReceive(new byte[] { 0 }, 0, 0, 0, callback, null);
            }
            catch
            {
                if(on)
                    Console.WriteLine("Fomos desconectados do servidor");
                desconectados = true;
            }
        }

        public static long GerarHash(string s) //Gerar o Hash code de strings
        {
            const int p = 53;
            const int m = 1000000000 + 9; //10e9 + 9
            long hash_value = 0;
            long p_pow = 1;
            foreach (char c in s)
            {
                hash_value = (hash_value + (c - 'a' + 1) * p_pow) % m;
                p_pow = p_pow * p % m;
            }
            return hash_value;
        }
    }
}
