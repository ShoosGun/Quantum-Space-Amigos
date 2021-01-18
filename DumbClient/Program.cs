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
            //float turnInput = 0f;
            //bool jumpInput = false;
            Thread.Sleep(100);          
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

                //if ((DateTime.UtcNow - start).Milliseconds >= 33 && (moveInput != Vector3.zero || jumpInput || turnInput != 0f) )
                //{
                    //Send packet to server
                    //Console.WriteLine("Mandando pacote. . .");
                    PacketWriter packet = new PacketWriter();
                    packet.Write((byte)Header.MOVEMENT);

                    packet.Write(DateTime.UtcNow);

                    packet.Write((byte)1);//Quantos sub pacotes vamos mandar 
                    
                    //Não importa a ordem e a quantidade (mas e se mandarmos mais de um :heartian_thinking_emojo:)
                    
                    packet.Write((byte)SubMovementHeader.HORIZONTAL_MOVEMENT);
                    packet.Write(new Vector3(0f,0f,1f));

                    //packet.Write((byte)SubMovementHeader.SPIN);
                    //packet.Write(turnInput);

                    //packet.Write((byte)SubMovementHeader.JUMP);
                    //packet.Write(jumpInput);
                    byte[] buffer = packet.GetBytes();

                    clientSck.Send(BitConverter.GetBytes(buffer.Length));
                    clientSck.Send(buffer);

                    //moveInput = Vector3.zero;
                    //turnInput = 0f;
                    //jumpInput = false;

                    start = DateTime.UtcNow;
                //}
            }
            Console.WriteLine("Pressione ENTER para fechar o programa");
            clientSck.Close();
            Console.Read();

            
        }

    }
}
