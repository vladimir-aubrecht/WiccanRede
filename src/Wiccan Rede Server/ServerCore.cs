using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRedeServer
{
    class ServerCore : ICommand, IStartable
    {
        public struct Data
        {
            public System.Net.Sockets.Socket clientSocket;
            public char priority;
            public char[] action;
            public char flags;
            public String[] parameters;

            public static String[] TRUE = new String[1] { "true" };
            public static String[] FALSE = new String[1] { "false" };
            public static String[] NOT_SUPPORTED = new String[1] { "notsupported" };

            public override String ToString()
            {
                String packet = new String(new char[] { priority, action[0], action[1], action[2], flags });

                if (parameters != null)
                    for (int i = 0; i < parameters.Length; i++)
                        packet += parameters[i] + "$";

                return packet;
            }
        }

        enum SyncServer
        {
            Time = 12384,               //00
        }

        private IDisplay display = null;
        private INetInput input = null;
        private INetOutput output = null;
        private IAccountsActionExecutor accounts = null;

        private bool stop = false;
        private DateTime startupTime;
        private char priority = (char)0;
        private bool packetstrafficshow = false;
        private bool packetstrafficlog = false;

        private List<String> commands = new List<String>();
        private List<Data> ServerData = new List<Data>();

        public ServerCore(IDisplay display, INetInput input, INetOutput output, IAccountsActionExecutor accounts)
        {
            this.startupTime = DateTime.Now;
            this.display = display;
            this.input = input;
            this.output = output;
            this.accounts = accounts;
        }

        public void Start()
        {

            while (!stop)
            {

                #region Zpracovani prikazu z konzole
                lock (commands)
                {
                    if (commands.Count > 0)
                    {
                        ParseCommand(commands[0]);
                        commands.RemoveAt(0);
                    }
                }
                #endregion


                //pokud existuji nejaka data na vstupu ze site, pak je zpracuj
                if (input.AreInputData())
                {
                    Data data = input.GetInputData();

                    if (data.clientSocket.Connected)
                    {
                        String ip = data.clientSocket.LocalEndPoint.ToString();

                        if (packetstrafficshow)
                            display.WriteLine("From: " + ip + " Packet: " + data.ToString());

                        if (packetstrafficlog)
                            display.LogLine("From: " + ip + " Packet: " + data.ToString());

                        ProcessData(ref data);

                        if (packetstrafficshow && data.flags != 'T')
                            display.WriteLine("To: " + ip + " Packet: " + data.ToString());

                        if (packetstrafficlog && data.flags != 'T')
                            display.LogLine("From: " + ip + " Packet: " + data.ToString());
                    }

                }

                if (ServerData.Count > 0)
                {
                    Data data = ServerData[0];
                    ProcessData(ref data);
                    ServerData.RemoveAt(0);
                }

            }

        }

        private void ProcessData(ref Data inputdata)
        {
            if (inputdata.priority >= priority)
            {

                if (ExecuteAction(ref inputdata))
                {
                    if (inputdata.flags == 'T')
                    {
                        if (inputdata.clientSocket.Connected)
                        {
                            inputdata.clientSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                            inputdata.clientSocket.Close();
                        }
                    }
                    else
                    {
                        inputdata.flags = 'S';
                        output.AddOutputData(inputdata);
                    }
                }

            }
        }

        public void Stop()
        {
            stop = true;
        }

        public bool IsStoped()
        {
            return stop;
        }

        public void ExecuteCommand(String command)
        {
            lock (commands)
            {
                commands.Add(command);    
            }
        }

        private void ParseCommand(String command)
        {
            switch (command)
            {
                case "users-count-nonlogged":
                    display.WriteLine("Connected users: " + input.ConnectedUsersCount().ToString());
                    break;

                case "users-count":
                    String[] cusers = accounts.GetLoggedUsers();
                    display.WriteLine("Users logged on the server are: " + cusers.Length);
                    break;

                case "users-name":
                    String[] nusers = accounts.GetLoggedUsers();

                    if (nusers.Length > 0)
                    {
                        foreach (String name in nusers)
                        {
                            display.WriteLine(name);
                        }
                    }
                    else
                    {
                        display.WriteLine("Users aren't on the server.");
                    }
                    break;

                case "priority-current":
                    display.WriteLine("Current priority: " + (int)priority);
                    break;

                case "quit":
                    display.WriteLine("Quiting server ...");
                    Program.Quit();
                    break;

                case "packets-traffic-show": packetstrafficshow = true;
                    break;

                case "packets-traffic-hide": packetstrafficshow = false;
                    break;

                case "packets-traffic-log": packetstrafficlog = true;
                    break;

                case "packets-traffic-stoplog": packetstrafficlog = false;
                    break;

                default: display.WriteLine("Command not found.");
                    break;
            }
        }

        private bool ExecuteAction(ref Data inputdata)
        {
            switch (inputdata.action[0])
            {
                //akce ve svete
                case 'W':
                    IWorldActionExecutor world = accounts.GetWorld(inputdata);
                    if (world != null)
                        return world.ExecuteAction(ref inputdata);
                    else
                        return false;

                //akce pro synchronizaci klientu
                case 'C':
                    return ExecuteServerAction(ref inputdata);

                //akce pro synchronizaci uzivatelskych uctu
                case 'A':
                    return accounts.ExecuteAction(ref inputdata);
                default:
                    return false;
            }
        }

        private bool ExecuteServerAction(ref Data inputdata)
        {
            int action = inputdata.action[1] * 257 + inputdata.action[2];

            switch ((SyncServer)action)
            {
                case SyncServer.Time:
                    inputdata.parameters = new String[1]{DateTime.Now.ToString()};
                    break;
                
                default:
                    return false;
            }

            return true;
        }

    }

}
