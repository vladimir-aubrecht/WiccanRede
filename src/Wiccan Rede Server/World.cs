using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace WiccanRedeServer
{
    class World : IWorldActionExecutor, IDisposable
    {
        public enum Actions
        {
            SetPosition = 12384,        //00
            GetPosition = 12385,        //01
            GetPlayersPosition = 12386  //02
        }
        public struct Vector3
        {
            public float x;
            public float y;
            public float z;

            public static Vector3 operator +(Vector3 op1, Vector3 op2)
            {
                Vector3 v = new Vector3();
                v.x = op1.x + op2.x;
                v.y = op1.y + op2.y;
                v.z = op1.z + op2.z;
                return v;
            }
            public static Vector3 operator -(Vector3 op1, Vector3 op2)
            {
                Vector3 v = new Vector3();
                v.x = op1.x - op2.x;
                v.y = op1.y - op2.y;
                v.z = op1.z - op2.z;
                return v;
            }
            public static Vector3 operator *(float k, Vector3 op2)
            {
                Vector3 v = new Vector3();
                v.x = k*op2.x;
                v.y = k*op2.y;
                v.z = k*op2.z;
                return v;
            }
            public static Vector3 operator *(Vector3 op1, float k)
            {
                Vector3 v = new Vector3();
                v.x = op1.x * k;
                v.y = op1.y * k;
                v.z = op1.z * k;
                return v;
            }
            public float GetLength()
            {
                return (float)Math.Sqrt(x * x + y * y + z * z);
            }
            public new String[] ToString()
            {
                return new string[] { x.ToString(), y.ToString(), z.ToString() };
            }
        }

        int id = -1;
        Vector3 position = new Vector3();

        public void Dispose()
        {
        }

        public World(int id)
        {
            this.id = id;
        }

        public bool ExecuteAction(ref ServerCore.Data inputdata)
        {
            int action = inputdata.action[1] * 257 + inputdata.action[2];

            Actions act = (Actions)action;

            #region SetPosition
            if (act == Actions.SetPosition)
            {
                Vector3 pos = new Vector3();
                if (AreCoordinates(inputdata.parameters, out pos))
                {
                    this.position = pos;
                }
            }
            #endregion

            #region GetPosition
            else if (act == Actions.GetPosition)
            {
                inputdata.parameters = position.ToString();
                return true;
            }
            #endregion

            #region GetPlayersPosition
            else if (act == Actions.GetPlayersPosition)
            {
                int index = -1;
                String[] users = Accounts.GetInstance().GetLoggedUsers(Accounts.GetInstance().SocketToUsername(inputdata.clientSocket), out index);
                List<String> players = new List<String>();

                for (int t = 0; t < users.Length; t++)
                {
                    IWorldActionExecutor world = Accounts.GetInstance().GetWorld(users[t]);
                    
                    if (world != null && t != index)
                        players.AddRange(world.GetPosition().ToString());
                }

                inputdata.parameters = players.ToArray();

                return true;
            }
            #endregion

            return false;
        }

        private bool AreCoordinates(String[] parameters)
        {
            Vector3 v = new Vector3();
            return AreCoordinates(parameters, out v);
        }
        private bool AreCoordinates(String[] parameters, out Vector3 coordinates)
        {

            try
            {
                coordinates.x = Convert.ToSingle(parameters[0]);
                coordinates.y = Convert.ToSingle(parameters[1]);
                coordinates.z = Convert.ToSingle(parameters[2]);
            }
            catch
            {
                coordinates = new Vector3();
                return false;
            }


            return true;

        }

        public Vector3 GetPosition()
        {
            return position;
        }

        public int GetID()
        {
            return id;
        }
    }
}
