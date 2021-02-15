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

                PacketWriter namePacket = new PacketWriter();
                namePacket.Write((byte)Header.NAME);
                namePacket.Write(shadeName);
                byte[] namePacketBuffer = namePacket.GetBytes();
                
                clientSck.Send(BitConverter.GetBytes(namePacketBuffer.Length));
                clientSck.Send(namePacketBuffer);

                namePacket.Close();

                Console.WriteLine("Precione P para desconectar esse cliente");
                Console.CursorVisible = false;
                DateTime start = DateTime.UtcNow;

                bool useRandomInput = true;
                Vector3 moveInput = Vector3.zero;
                float turnInput = 0f;
                bool jumpInput = false;
                System.Random rnd = new System.Random(start.Millisecond);
                while (on)
                {
                    bool packets_NotLoked = Monitor.TryEnter(packets_lock, 10);
                    if (packets_NotLoked)
                    {
                        try
                        {
                            foreach (var p in packets)
                            {
                                while (true)
                                {
                                    try
                                    {
                                        PacketReader pac = new PacketReader(p);
                                        switch ((Header)pac.ReadByte())
                                        {
                                            case Header.NAME:
                                                Console.WriteLine("Nome recebido: {0}", pac.ReadString());
                                                break;
                                            default:
                                                break;
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

                            case ConsoleKey.R:
                                useRandomInput = !useRandomInput;
                                break;

                            ////Mover
                            //case ConsoleKey.W:
                            //    moveInput.z++;
                            //    break;

                            //case ConsoleKey.S:
                            //    moveInput.z--;
                            //    break;

                            //case ConsoleKey.A:
                            //    moveInput.x--;
                            //    break;

                            //case ConsoleKey.D:
                            //    moveInput.x++;
                            //    break;

                            ////Virar
                            //case ConsoleKey.Q:
                            //    turnInput++;
                            //    break;

                            //case ConsoleKey.E:
                            //    turnInput--;
                            //    break;

                            ////Pular
                            //case ConsoleKey.Z:
                            //    jumpInput = true;
                            //    break;

                            default:
                                break;
                        }
                    }

                    if (desconectados)
                        on = false;

                    if (useRandomInput && !desconectados)
                    {
                        Thread.Sleep(10);
                        moveInput = new Vector3(rnd.Next(0, 2), 0f, rnd.Next(0, 2));
                        turnInput = rnd.Next(-1, 2);
                        jumpInput = rnd.Next(0, 101) < 11;



                        //Send packet to server
                        PacketWriter packet = new PacketWriter();
                        packet.Write((byte)Header.MOVEMENT);

                        packet.Write(DateTime.UtcNow);

                        packet.Write((byte)3);//Quantos sub pacotes vamos mandar 

                        packet.Write((byte)SubMovementHeader.HORIZONTAL_MOVEMENT);
                        packet.Write(moveInput);

                        packet.Write((byte)SubMovementHeader.SPIN);
                        packet.Write(turnInput);

                        packet.Write((byte)SubMovementHeader.JUMP);
                        packet.Write(jumpInput);
                        byte[] buffer = packet.GetBytes();

                        clientSck.Send(BitConverter.GetBytes(buffer.Length));
                        clientSck.Send(buffer);



                        moveInput = Vector3.zero;
                        turnInput = 0f;
                        jumpInput = false;
                    }
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
                Console.Write("Dados foram recebidos, tamanho: ");
                 byte[] receivedBuffer = new byte[4];
                clientSck.Receive(receivedBuffer, 0, 4, 0);
                int dataSize = BitConverter.ToInt32(receivedBuffer, 0);
                Console.WriteLine(dataSize);

                if (dataSize <= 0)
                    throw new SocketException();

                Console.WriteLine("Problema e o loop?");
                byte[] buffer = new byte[dataSize];
                int received = clientSck.Receive(buffer, 0, buffer.Length, 0);
                Console.WriteLine(received);
                while (received < dataSize)
                {
                    received += clientSck.Receive(buffer, received, dataSize - received, 0);
                }
                Console.WriteLine("Nao, problema e o lock");
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

    }
}
