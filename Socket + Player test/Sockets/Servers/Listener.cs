using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using DIMOWAModLoader;

namespace ServerSide.Sockets.Servers
{
    public class Listener
    {
        private ClientDebuggerSide debugger;
        public bool Listening
        {
            get;
            private set;
        }

        public int Port
        {
            get;
            private set;
        }
        private Socket s;

        public Listener(int port, ClientDebuggerSide debugger)
        {
            Port = port;
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.debugger = debugger;
        }
        public void Start()
        {
            if (Listening)
                return;

            s.Bind(new IPEndPoint(IPAddress.Parse("127.1.0.0"), Port)); // Depois trocar por 0
            s.Listen(0);

            s.BeginAccept(callback, null);
            Listening = true;

        }

        public void Stop()
        {
            if (!Listening)
                return;
            
            s.Close();
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        }

        private void callback(IAsyncResult ar)
        {
            try
            {
                Socket s = this.s.EndAccept(ar);

                if (SocketAccepted != null)
                {
                    SocketAccepted(s);
                }

                this.s.BeginAccept(callback, null);
            }
            catch (Exception ex)
            {
                debugger.SendLogMultiThread(ex.Message, DebugType.ERROR);
            }
        }

        public event SocketAcceptedHandler SocketAccepted;
        public delegate void SocketAcceptedHandler(Socket e);
    }
} 