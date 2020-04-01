using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WiccanRedeServer
{
    class NetInput : IStartable, INetInput
    {
        private bool stop = false;
        private int users = 0;
        private int port = Properties.Settings.Default.Port;
        private IPAddress ip = new IPAddress(new byte[4]{127,0,0,1});
        
        private ManualResetEvent allDone = new ManualResetEvent(false);

        private List<ServerCore.Data> dataBuffer = new List<ServerCore.Data>();

        public NetInput(IPAddress ip, int port)
        {
            if (ip != null)
            {
                this.ip = ip;
                this.port = port;
            }
        }
        
        private void AcceptClient(IAsyncResult asyn)
        {

            allDone.Set();

            Socket listener = (Socket)asyn.AsyncState;
            Socket clientsocket = listener.EndAccept(asyn);

            ServerCore.Data data = new ServerCore.Data();
            data.clientSocket = clientsocket;

            while (clientsocket.Connected)
            {

                NetworkStream networkStream = new NetworkStream(clientsocket, false);
                StreamReader streamReader = new StreamReader(networkStream);

                try
                {
                    if (streamReader.EndOfStream)
                        break;
                }
                catch
                {
                    break;
                }

                string line = streamReader.ReadLine();

                #region parsing packet
                if (line.Length > 4)
                {
                    data.priority = line[0];
                    data.action = new char[3] { line[1], line[2], line[3] };
                    data.flags = line[4];
                    
                    line = line.Substring(5);
                    
                    List<String> parameters = new List<string>();
                    while (true)
                    {
                        int parindex = line.IndexOf("$");

                        if (parindex == -1)
                            break;

                        parameters.Add(line.Substring(0, parindex));
                        line = line.Substring(parindex + 1);
                    }
                    
                    data.parameters = parameters.ToArray();
                    lock (dataBuffer)
                    {
                        dataBuffer.Add(data);
                    }
                }
                #endregion
            }

            users--;
            clientsocket.Close();
        }

        public void Start()
        {
            TcpListener tcpListener = new TcpListener(ip, port);
            tcpListener.Start();

            while (!stop)
            {
                allDone.Reset();

                tcpListener.BeginAcceptSocket(AcceptClient, tcpListener.Server);

                allDone.WaitOne();
                users++;
            }
        }

        public void Stop()
        {
            stop = true;
            allDone.Set();
        }

        public bool IsStoped()
        {
            return stop;
        }

        public int ConnectedUsersCount()
        {
            return users;
        }

        public bool AreInputData()
        {
            if (dataBuffer.Count > 0)
                return true;

            return false;
        }

        public ServerCore.Data GetInputData()
        {
            ServerCore.Data id;
            
            lock (dataBuffer)
            {
                id = dataBuffer[0];
                dataBuffer.RemoveAt(0);
            }
            
            return id;
        }
    }
}
