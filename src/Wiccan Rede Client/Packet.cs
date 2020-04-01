using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace WiccanRedeClient
{
    public struct Packet
    {
        public char priority;
        public char[] action;
        public char flags;
        public String[] parameters;

        public static String[] TRUE = new String[1] { "true" };
        public static String[] FALSE = new String[1] { "false" };
        public static String[] NOT_SUPPORTED = new String[1] { "notsupported" };

        public Packet(char priority, char group, char a1, char a2, char flag, params String[] parameters)
        {
            this.priority = priority;
            this.action = new char[] { group, a1, a2 };
            this.flags = flag;
            this.parameters = parameters;
        }

        public override String ToString()
        {
            String packet = new String(new char[] { priority, action[0], action[1], action[2], flags });

            if (parameters != null)
                for (int i = 0; i < parameters.Length; i++)
                    packet += parameters[i] + "$";

            return packet;
        }

        public bool EqualParameters(params String[] parameters)
        {
            bool isequal = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] != this.parameters[i])
                {
                    isequal = false;
                    break;
                }
            }

            return isequal;
        }

        public bool EqualFirstParameter(params String[] parameters)
        {
            if (parameters == null || parameters.Length <= 0)
                return false;

            if (this.parameters[0] == parameters[0])
                return true;

            return false;
        }

        public static Packet CreateFromString(String data)
        {
            Packet p = new Packet();
            if (data.Length < 5)
            {
                p.flags = 'E';
                return p;
            }

            p.priority = data[0];
            p.action = new char[] { data[1], data[2], data[3] };
            p.flags = data[4];

            data = data.Substring(5);

            List<String> parameters = new List<string>();
            while (true)
            {
                int parindex = data.IndexOf("$");

                if (parindex == -1)
                    break;

                parameters.Add(data.Substring(0, parindex));
                data = data.Substring(parindex + 1);
            }

            p.parameters = parameters.ToArray();

            return p;
        }
    }

    public static class PacketBuilder
    {
        public static Packet ServerTimePacket()
        {
            return new Packet('9', 'C', '0', '0', 'C');
        }

        public static Packet LoginPacket(String username, String password)
        {
            return new Packet('9', 'A', '0', '0', 'C', username, password);
        }

        public static Packet LogoutPacket()
        {
            return new Packet('9', 'A', '0', '1', 'C');
        }

        public static Packet SendChatMessagePacket(String text)
        {
            return new Packet('9', 'A', '0', '2', 'C', text);
        }

        public static Packet GetLoggedUsersPacket()
        {
            return new Packet('9', 'A', '1', '0', 'C');
        }

        public static Packet GetAllChatPacket()
        {
            return new Packet('9', 'A', '0', '9', 'C');
        }

        public static Packet SetPositionPacket(float x, float y, float z)
        {
            return new Packet('9', 'W', '0', '0', 'C', x.ToString(), y.ToString(), z.ToString());
        }

        public static Packet GetPositionPacket()
        {
            return new Packet('9', 'W', '0', '1', 'C');
        }

        public static Packet GetPlayersPositionPacket()
        {
            return new Packet('9', 'W', '0', '2', 'C');
        }

        public static Packet SendMessage(String to, String text)
        {
            return new Packet('9', 'A', '0', '8', 'C', to, text);
        }
    }
}