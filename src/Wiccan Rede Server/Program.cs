using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;

namespace WiccanRedeServer
{
    class Program
    {
        private static Thread displayThread = null;
        private static Thread uinputThread = null;
        private static Thread coreThread = null;
        private static Thread ninputThread = null;
        private static Thread noutputThread = null;

        private static Display display = null;
        private static NetInput ninput = null;
        private static NetOutput noutput = null;
        private static ServerCore core = null;
        private static UserInput uinput = null;

        public static void Quit()
        {
            uinput.Stop();
            noutput.Stop();
            ninput.Stop();
            core.Stop();

            uinputThread.Abort();
            ninputThread.Abort();
            noutputThread.Abort();

            display.WriteLine("Server is stopped. Please press any key to close this console ...");
            display.Stop();
            displayThread.Abort();
            coreThread.Abort();
        }

        static void Main(string[] args)
        {
            IPAddress[] ipset = Dns.GetHostByName(Dns.GetHostName()).AddressList;

            IPAddress ip;
            if (Properties.Settings.Default.DetectIP)
                ip = ipset[0];
            else
                ip = Dns.GetHostAddresses(Properties.Settings.Default.IP)[0];

            int port = Properties.Settings.Default.Port;

            Accounts accounts = Accounts.GetInstance();

            display = new Display();
            ninput = new NetInput(ip, port);
            noutput = new NetOutput();
            core = new ServerCore(display, ninput, noutput, accounts);
            uinput = new UserInput(core);

            display.WriteLine("Server starting ...");

            displayThread = new Thread(display.Start);
            displayThread.Start();

            uinputThread = new Thread(uinput.Start);
            uinputThread.Start();

            coreThread = new Thread(core.Start);
            coreThread.Start();

            ninputThread = new Thread(ninput.Start);
            ninputThread.Start();

            noutputThread = new Thread(noutput.Start);
            noutputThread.Start();

            display.WriteLine("Server running ...");
        }
    }
}
