using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace WiccanRedeClient
{
    public class Connector
    {
        private TcpClient tcpclient = null;
        private Socket socket = null;
        private StreamWriter streamwriter = null;
        private StreamReader streamreader = null;
        private bool logged = false;

        public bool Logged
        {
            get
            {
                return logged;
            }
        }

        public Connector(String hostname, int port, String username, String password)
        {
            tcpclient = new TcpClient(hostname, port);

            if (tcpclient.Connected)
            {
                socket = tcpclient.Client;
                Log(username, password);
            }
        }

        private void Log(String username, String password)
        {
            if (socket.Connected)
            {
                WriteToStream(PacketBuilder.LoginPacket(username, password));
                String respond = ReadFromStream();
                Packet p = Packet.CreateFromString(respond);

                if (p.EqualFirstParameter(Packet.TRUE))
                    logged = true;
                else
                {
                    logged = false;
                    Disconnect();
                }

            }
        }

        public void Disconnect()
        {
            WriteToStream(PacketBuilder.LogoutPacket());
            tcpclient.Close();
        }

        public String ReadFromStream()
        {
            if (socket == null || !socket.Connected)
                return null;

                NetworkStream networkstream = new NetworkStream(socket, false);
                streamreader = new StreamReader(networkstream);
                String respond = streamreader.ReadLine();
                streamreader.Close();
                return respond;
        }

        public void WriteToStream(Packet unlogpacket)
        {
            if (socket == null || !socket.Connected)
                return;

                NetworkStream networkstream = new NetworkStream(socket, false);
                streamwriter = new StreamWriter(networkstream);
                streamwriter.WriteLine(unlogpacket.ToString());
                streamwriter.Flush();
                streamwriter.Close();                
        }
    }
}
