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
            Console.WriteLine("Pressione ENTER para tentar conectar no servidor");
            string s = Console.ReadLine();

            clientSck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSck.Connect(new IPEndPoint(IPAddress.Parse("127.1.0.0"), 2121));

            Console.WriteLine("Precione ESC para desconectar esse cliente");
            Console.CursorVisible = false;
            bool on = true;
            DateTime start = DateTime.UtcNow;

            Vector3 moveInput = Vector3.zero;
            float turnInput = 0f;
            bool jumpInput = false;
            Thread.Sleep(100);
            System.Random rnd = new System.Random();
            while (on)
            {

                if (Console.KeyAvailable == true)
                {
                    ConsoleKeyInfo keyPressed = Console.ReadKey(true);
                    switch (keyPressed.Key)
                    {
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

                        //Desligar
                        case ConsoleKey.P:
                            on = false;
                            break;

                        default:
                            break;
                    }
                }
                moveInput = new Vector3(rnd.Next(0, 2), 0f, rnd.Next(0, 2));
                turnInput = rnd.Next(-1, 2);
                jumpInput = rnd.Next(0, 101) < 26;


                //Send packet to server
                PacketWriter packet = new PacketWriter();
                packet.Write((byte)Header.MOVEMENT);

                packet.Write(DateTime.UtcNow);

                packet.Write((byte)3);//Quantos sub pacotes vamos mandar 
                
                packet.Write((byte)SubMovementHeader.HORIZONTAL_MOVEMENT);
                packet.Write(new Vector3(0f, 0f, 1f));

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
                Thread.Sleep(10);

                start = DateTime.UtcNow;
            }
            Console.WriteLine("Pressione ENTER para fechar o programa");
            clientSck.Close();
            Console.Read();


        }

    }
}
