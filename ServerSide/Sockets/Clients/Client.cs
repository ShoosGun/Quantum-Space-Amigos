using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using DIMOWAModLoader;

namespace ServerSide.Sockets.Clients
{
    public class Client
    {
        private ClientDebuggerSide debugger;

        public string ID
        {
            get;
            private set;
        }
        public IPEndPoint EndPoint
        {
            get;
            private set;
        }
        private Socket sck;

        private DateTime startedReceivingTime;
        private int receivingLimit;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accepted"></param>
        /// <param name="debugger"></param>
        /// <param name="receivingLimit"> In packets/s </param>
        public Client(Socket accepted, ClientDebuggerSide debugger, int receivingLimit =  100)
        {
            this.debugger = debugger;
            this.receivingLimit = receivingLimit;

            ID = Guid.NewGuid().ToString();
            sck = accepted;
            EndPoint = (IPEndPoint)sck.RemoteEndPoint;

            startedReceivingTime = DateTime.UtcNow;
            sck.BeginReceive(new byte[] { 0 }, 0, 0, 0, callback, null);
        }

        private int amountOfReceivedPackets = 0;
        private void callback(IAsyncResult ar)
        {
            try
            {
                sck.EndReceive(ar);
                byte[] buffer = new byte[4];
                sck.Receive(buffer, 0, 4, 0);
                int dataSize = BitConverter.ToInt32(buffer, 0);
                if (dataSize <= 0)
                    throw new SocketException();

                buffer = new byte[dataSize];
                int received = sck.Receive(buffer, 0, buffer.Length, 0);
                while (received < dataSize)
                {
                    received += sck.Receive(buffer, received, dataSize - received, 0);
                }
                if (Received != null)
                {
                    Received(this, buffer);
                }

                if( (DateTime.UtcNow - startedReceivingTime).Milliseconds >= 1000)
                    amountOfReceivedPackets = 0;

                else if(amountOfReceivedPackets > receivingLimit)
                {
                    Thread.Sleep(1000 - (DateTime.UtcNow - startedReceivingTime).Milliseconds); // Esperar até dar um segundo
                }
                sck.BeginReceive(new byte[] { 0 }, 0, 0, 0,callback, null);
            }
            catch (Exception ex)
            {
                debugger.SendLogMultiThread(ex.Message, DebugType.ERROR);
                if (Disconnected != null)
                {
                    Disconnected(this);
                }
            }
        }
        public void Send(byte[] buffer) 
        {
            lock (this) //Será que isso ajuda? Sla, mas não quero que crashe de novo, se quiser tire esse lock e teste ai
            {
                try
                {   //Bruh momiento (pf não inverter na próxima vez, más lembranças)
                    byte[] sizeBuffer = BitConverter.GetBytes(buffer.Length);
                    sck.Send(sizeBuffer, 0, sizeBuffer.Length, 0);
                    sck.Send(buffer, 0, buffer.Length, 0);
                }
                catch (Exception ex)
                {
                    debugger.SendLogMultiThread("Erro enquanto ao enviar dados >> " + ex.Message, DebugType.ERROR);
                }
            }
        }
        public void Close()
        {
            sck.Close();
        }

        public event ClientReceivedHandler Received;
        public event ClientDisconnectedHandler Disconnected;
        public delegate void ClientReceivedHandler(Client sender, byte[] data);
        public delegate void ClientDisconnectedHandler(Client sender);
    }
}

