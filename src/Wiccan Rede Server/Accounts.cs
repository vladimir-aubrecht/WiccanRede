using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Net;
using System.Net.Sockets;

namespace WiccanRedeServer
{
    class Accounts : IAccountsActionExecutor
    {
        public enum Actions
        {
            Login = 12384,              //00
            Logout = 12385,             //01
            SendChatMessage = 12386,    //02
            GetContactList = 12387,     //03
            GetContactStatus = 12388,   //04
            GetMessages = 12389,        //05
            AddContact = 12390,         //06
            DeleteContact = 12391,      //07
            SendMessage = 12392,        //08
            GetAllChat = 12393,         //09
            GetLoggedUsers = 12641      //10
        }

        private struct ReceivedMessage
        {
            public String time;
            public String from;
            public String text;
        }

        private struct LoggedAccounts
        {
            public Socket socket;
            public String username;
            public World wold;
            public List<ReceivedMessage> receivedMessages;
        }

        int counter = 0;
        DataSet data;
        private static Accounts instance = null;
        private List<LoggedAccounts> loggedBuffer = new List<LoggedAccounts>();

        private Accounts()
        {
            data = new DataSet("Data");
            data.ReadXml(@"Config/accounts.xml");

            DataTable chatTable = new DataTable("Chat");
            DataColumn time = new DataColumn("Time");
            DataColumn from = new DataColumn("From");
            DataColumn text = new DataColumn("Text");

            chatTable.Columns.Add(time);
            chatTable.Columns.Add(from);
            chatTable.Columns.Add(text);
            data.Tables.Add(chatTable);

        }
        public static Accounts GetInstance()
        {
            if (instance == null)
                instance = new Accounts();

            return instance;
        }
        public bool ExecuteAction(ref ServerCore.Data inputdata)
        {
            Actions action = (Actions)(inputdata.action[1] * 257 + inputdata.action[2]);

            #region GetLoggedUsers
            if (action == Actions.GetLoggedUsers)
            {
                inputdata.parameters = GetLoggedUsers(SocketToUsername(inputdata.clientSocket));
            }
            #endregion

            #region GetAllChat
            else if (action == Actions.GetAllChat)
            {
                inputdata.parameters = GetAllChat(GetIndexOfUser(inputdata.clientSocket));
            }
            #endregion

            #region SendChatMessage
            else if (action == Actions.SendChatMessage)
            {
                if (inputdata.parameters.Length >= 1)
                {
                    SendChatMessage(SocketToUsername(inputdata.clientSocket), inputdata.parameters[0]);
                    return false;
                }
                else
                {
                    inputdata.parameters = ServerCore.Data.NOT_SUPPORTED;
                }
            }
            #endregion

            #region Logout
            else if (action == Actions.Logout)
            {
                inputdata.parameters = Logout(inputdata.clientSocket);
                inputdata.flags = 'T';
            }
            #endregion

            #region Login
            else if (action == Actions.Login)
            {
                if (inputdata.parameters.Length >= 2)
                {
                    inputdata.parameters = Login(inputdata.clientSocket, inputdata.parameters[0], inputdata.parameters[1]);
                }
                else
                {
                    inputdata.parameters = ServerCore.Data.NOT_SUPPORTED;
                }
            }
            #endregion

            #region SendMessage
            else if (action == Actions.SendMessage)
            {
                if (inputdata.parameters.Length >= 2)
                {
                    if (SendMessage(SocketToUsername(inputdata.clientSocket), inputdata.parameters[0], inputdata.parameters[1]))
                    {
                        return false;
                    }
                    else
                    {
                        inputdata.parameters = new string[] { "NotLogged" };
                    }
                }

            }
            #endregion

            return true;
        }


        private String[] GetAllChat(int userid)
        {
            int chatmessagecount = data.Tables["Chat"].Rows.Count * 3;
            int pmmessagecount = 0;
            int messagecount = chatmessagecount;

            if (userid != -1)
            {
                pmmessagecount = loggedBuffer[userid].receivedMessages.Count;
                messagecount += pmmessagecount * 3;
            }


            String[] chathistory = new String[messagecount];
            for (int t = 0, i = 0; t < chatmessagecount; t += 3, i++)
            {
                chathistory[t + 0] = (String)data.Tables["Chat"].Rows[i]["Time"];
                chathistory[t + 1] = (String)data.Tables["Chat"].Rows[i]["From"];
                chathistory[t + 2] = (String)data.Tables["Chat"].Rows[i]["Text"];
            }

            if (userid != -1)
            {
                for (int a = chatmessagecount, b = 0; b < pmmessagecount; a += 3, b++)
                {
                    chathistory[a + 0] = loggedBuffer[userid].receivedMessages[b].time;
                    chathistory[a + 1] = loggedBuffer[userid].receivedMessages[b].from;
                    chathistory[a + 2] = loggedBuffer[userid].receivedMessages[b].text;
                }
            }

            return chathistory;
        }
        private void SendChatMessage(String from, String text)
        {
            if (!IsUserLogged(from))
                return;

            if (data.Tables["Chat"].Rows.Count > 500)
                data.Tables["Chat"].Rows.RemoveAt(0);

            DataRow dr = data.Tables["Chat"].NewRow();
            dr["Time"] = DateTime.Now.ToString();
            dr["From"] = from;
            dr["Text"] = text;

            data.Tables["Chat"].Rows.Add(dr);
        }
        private bool SendMessage(String from, String to, String text)
        {
            int index = -1;
            if (IsUserLogged(to, out index))
            {
                ReceivedMessage rm = new ReceivedMessage();
                rm.time = DateTime.Now.ToString();
                rm.from = from;
                rm.text = text;

                loggedBuffer[index].receivedMessages.Add(rm);
                return true;
            }

            return false;
        }
        
        
        private String[] Logout(Socket socket)
        {
            String[] output;

            int index = -1;
            String outusername = SocketToUsername(socket, out index);

            if (index >= 0)
                loggedBuffer.RemoveAt(index);

            if (outusername != null)
            {
                String query = String.Format("Username LIKE '{0}'", outusername);
                DataRow[] accmatch = data.Tables["Account"].Select(query);
                accmatch[0]["Logged"] = "false";
                output = ServerCore.Data.TRUE;
            }
            else
                output = ServerCore.Data.FALSE;

            return output;
        }
        private String[] Login(Socket socket, String username, String password)
        {
            if (!Authenticate(username, password))
                return ServerCore.Data.FALSE;

                counter++;

                LoggedAccounts la = new LoggedAccounts();
                la.username = username;
                la.socket = socket;
                la.wold = new World(counter);
                la.receivedMessages = new List<ReceivedMessage>();
                loggedBuffer.Add(la);

                return ServerCore.Data.TRUE;
        }
        private bool Authenticate(String username, String password)
        {
            String query = String.Format("Username LIKE '{0}' AND Password LIKE '{1}'", username, password);
            DataRow[] accmatch = data.Tables["Account"].Select(query);

            if (accmatch.Length > 0)
            {
                accmatch[0]["Logged"] = "true";
                return true;
            }

            return false;
        }


        public IWorldActionExecutor GetWorld(ServerCore.Data data)
        {
            int index = -1;

            if (!IsUserLogged(data.clientSocket, out index))
                return null;

            return loggedBuffer[index].wold;
        }
        public IWorldActionExecutor GetWorld(String username)
        {
            int index = -1;

            if (!IsUserLogged(username, out index))
                return null;

            return loggedBuffer[index].wold;
        }
        public String[] GetLoggedUsers()
        {
            return GetLoggedUsers(null);
        }
        public String[] GetLoggedUsers(String without)
        {
            List<String> loggedusers = loggedBuffer.ConvertAll<String>(new Converter<LoggedAccounts, string>(GetUsername));

            if (without != null)
            {
                int index = loggedusers.IndexOf(without);

                if (index != -1)
                    loggedusers.RemoveAt(index);
            }

            return loggedusers.ToArray();
        }
        public String[] GetLoggedUsers(String without, out int index)
        {
            List<String> loggedusers = loggedBuffer.ConvertAll<String>(new Converter<LoggedAccounts, string>(GetUsername));

            index = -1;
            if (without != null)
            {
                index = loggedusers.IndexOf(without);
            }

            return loggedusers.ToArray();
        }
        private String GetUsername(LoggedAccounts la)
        {
            return la.username;
        }



        private bool IsUserLogged(String username)
        {
            int index = GetIndexOfUser(username);

            if (index == -1)
                return false;

            return true;
        }
        private bool IsUserLogged(Socket socket)
        {
            int index = GetIndexOfUser(socket);

            if (index == -1)
                return false;

            return true;
        }
        private bool IsUserLogged(String username, out int index)
        {
            index = GetIndexOfUser(username);

            if (index == -1)
                return false;

            return true;
        }
        private bool IsUserLogged(Socket socket, out int index)
        {
            index = GetIndexOfUser(socket);

            if (index == -1)
                return false;

            return true;
        }

        public String SocketToUsername(Socket socket)
        {
            int index = -1;
            return SocketToUsername(socket, out index);
        }
        public String SocketToUsername(Socket socket, out int index)
        {
            index = GetIndexOfUser(socket);

            if (index == -1)
                return null;

            return loggedBuffer[index].username;
        }
        private Socket UsernameToSocket(String username)
        {
            int index = GetIndexOfUser(username);

            if (index == -1)
                return null;

            return loggedBuffer[index].socket;
        }

        private int GetIndexOfUser(String username)
        {
            for (int t = 0; t < loggedBuffer.Count; t++)
            {
                if (loggedBuffer[t].username == username)
                {
                    if (loggedBuffer[t].socket.Connected)
                        return t;
                    else
                    {
                        loggedBuffer.RemoveAt(t);
                        return GetIndexOfUser(username);
                    }
                }
            }

            return -1;
        }
        private int GetIndexOfUser(Socket socket)
        {
            for (int t = 0; t < loggedBuffer.Count; t++)
            {
                if (loggedBuffer[t].socket == socket)
                {
                    if (loggedBuffer[t].socket.Connected)
                        return t;
                    else
                    {
                        loggedBuffer.RemoveAt(t);
                        return GetIndexOfUser(socket);
                    }
                }
            }

            return -1;
        }

    }
}