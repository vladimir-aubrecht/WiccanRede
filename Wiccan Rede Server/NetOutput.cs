using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace WiccanRedeServer
{
    class NetOutput : IStartable, INetOutput
    {
        private bool stop = false;

        private ManualResetEvent dataForSending = new ManualResetEvent(false);
        private List<ServerCore.Data> outputBuffer = new List<ServerCore.Data>(); 

        public void Start()
        {
            while (!stop)
            {
                if (outputBuffer.Count > 0)
                {
                    lock (outputBuffer)
                    {
                        SendDataToClient(outputBuffer[0]);
                        outputBuffer.RemoveAt(0);
                    }
                }
                else
                {
                    dataForSending.Reset();
                    dataForSending.WaitOne();
                }
                    
            }
        }

        public void Stop()
        {
            stop = true;
            dataForSending.Set();
        }

        public bool IsStoped()
        {
            return stop;
        }

        private void SendDataToClient(ServerCore.Data od)
        {
            if (od.clientSocket.Connected)
            {
                NetworkStream networkStream = new NetworkStream(od.clientSocket, false);
                StreamWriter streamWriter = new StreamWriter(networkStream);

                List<char> packet = new List<char>();
                
                packet.Add(od.priority);
                packet.Add(od.action[0]);
                packet.Add(od.action[1]);
                packet.Add(od.action[2]);
                packet.Add(od.flags);

                for (int t = 0; t < od.parameters.Length; t++)
                {
                    char[] par = od.parameters[t].ToCharArray();

                    for (int u = 0; u < par.Length; u++)
                    {
                        packet.Add(par[u]);
                    }

                    packet.Add('$');
                }

                try
                {
                    streamWriter.WriteLine(packet.ToArray());
                    streamWriter.Flush();

                    streamWriter.Close();
                }
                catch
                {
                    //uzivatel se odpojil, data se zahodi ...
                }
            }

        }

        public void AddOutputData(ServerCore.Data outputData)
        {
            lock (outputBuffer)
            {
                outputBuffer.Add(outputData);
                dataForSending.Set();
            }
        }
    }
}
