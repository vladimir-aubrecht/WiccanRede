using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRedeServer
{
    class UserInput : IStartable
    {
        private bool stop = false;

        ICommand commander = null;

        public UserInput(ICommand commander)
        {
            this.commander = commander;
        }

        public void Start()
        {
            while (!stop)
            {
                String command = Console.ReadLine();
                commander.ExecuteCommand(command);

                stop = ((IStartable)commander).IsStoped();
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

    }
}
