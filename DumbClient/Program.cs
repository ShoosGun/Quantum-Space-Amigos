using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace DumbClient
{
    class Program
    {
        private static Socket clientSck;

        private static void Main(string[] args)
        {
            Console.Title = "Dumb Client";
            Console.WriteLine("Pressione ENTER para tentar conectar no servidor, e de um nome para ele caso queira");
            string shadeName = Console.ReadLine();

            clientSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSck.Connect(new IPEndPoint(IPAddress.Parse("127.1.0.0"), 2121));

            PacketWriter namePacket = new PacketWriter();
            namePacket.Write((byte)Header.NAME);
            namePacket.Write(shadeName);
            byte[] namePacketBuffer = namePacket.GetBytes();

            clientSck.Send(BitConverter.GetBytes(namePacketBuffer.Length));
            clientSck.Send(namePacketBuffer);
            
            namePacket.Close();


            Console.WriteLine("Precione P para desconectar esse cliente");
            Console.CursorVisible = false;
            bool on = true;
            DateTime start = DateTime.UtcNow;

			bool useRandomInput = false;
            Vector3 moveInput = Vector3.zero;
            float turnInput = 0f;
            bool jumpInput = false;
            Thread.Sleep(100);
            System.Random rnd = new System.Random(start.Millisecond);
            while (on)
            {

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
				if(useRandomInput)
				{
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


                    Thread.Sleep(10);
                    moveInput = Vector3.zero;
                    turnInput = 0f;
                    jumpInput = false;

                    start = DateTime.UtcNow;
                }
            }
            Console.WriteLine("Pressione ENTER para fechar o programa");
            clientSck.Close();
            Console.Read();


        }

    }
}
