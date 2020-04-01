using System;
using System.Collections.Generic;
using System.Text;
using WiccanRedeClient;
using Microsoft.DirectX;

namespace WiccanRede
{
    class NetBridge : IWiccanRedeNet
    {
        private static NetBridge instance = null;
        private Connector connection = null;

        private NetBridge()
        {
        }

        public static IWiccanRedeNet GetInstance()
        {
            if (instance == null)
                instance = new NetBridge();

            return instance;
        }

        public void Connect(String url, int port, String username, String password)
        {
            if (connection == null)
                connection = new Connector(url, port, username, password);
        }

        public void Disconnect()
        {
            if (connection != null)
                connection.Disconnect();
        }

        public void SetPosition(Vector3 position)
        {
            if (connection == null || !connection.Logged)
                return;

            connection.WriteToStream(PacketBuilder.SetPositionPacket(position.X, position.Y, position.Z));
        }

        public Vector3 GetPosition()
        {
            if (connection == null || !connection.Logged)
                return new Vector3();

            connection.WriteToStream(PacketBuilder.GetPositionPacket());
            Packet respond = Packet.CreateFromString(connection.ReadFromStream());
            
            Vector3 pos = new Vector3();
            try
            {
                pos.X = Convert.ToSingle(respond.parameters[0]);
                pos.Y = Convert.ToSingle(respond.parameters[1]);
                pos.Z = Convert.ToSingle(respond.parameters[2]);
            }
            catch {/* Hrac se necha umistit do pocatku souradnic */}

            return pos;
        }

        public Vector3[] GetPlayersPosition()
        {
            if (connection == null || !connection.Logged)
                return null;

            connection.WriteToStream(PacketBuilder.GetPlayersPositionPacket());
            Packet respond = Packet.CreateFromString(connection.ReadFromStream());

            List<Vector3> positions = new List<Vector3>();
            for (int t = 0; t < respond.parameters.Length; t+=3)
            {
                Vector3 pos = new Vector3();
                try
                {
                    pos.X = Convert.ToSingle(respond.parameters[t + 0]);
                    pos.Y = Convert.ToSingle(respond.parameters[t + 1]);
                    pos.Z = Convert.ToSingle(respond.parameters[t + 2]);
                }
                catch { /* Hrac se necha umistit do pocatku souradnic */ }
                positions.Add(pos);
            }

            return positions.ToArray();
        }

        public String[] GetPlayersNames()
        {
            if (connection == null || !connection.Logged)
                return null;

            connection.WriteToStream(PacketBuilder.GetLoggedUsersPacket());

            Packet respond = Packet.CreateFromString(connection.ReadFromStream());
            return respond.parameters;
        }

        public String[] GetAllChatMessages()
        {
            if (connection == null || !connection.Logged)
                return null;

            connection.WriteToStream(PacketBuilder.GetAllChatPacket());

            Packet respond = Packet.CreateFromString(connection.ReadFromStream());

            return respond.parameters;
        }

        public void SendChatMessages(String text)
        {
            if (connection == null || !connection.Logged)
                return;

            connection.WriteToStream(PacketBuilder.SendChatMessagePacket(text));
        }

        public void SendMessage(String to, String text)
        {
            if (connection == null || !connection.Logged)
                return;


            connection.WriteToStream(PacketBuilder.SendMessage(to, text));
        }

    }
}
