using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

namespace WiccanRedeServer
{
    class Display : IDisplay, IStartable
    {

        private bool stop = false;
        private StreamWriter log = null;

        private List<String> messages = new List<string>();
        private List<String> logmessages = new List<string>();
        private ManualResetEvent dataForDisplaying = new ManualResetEvent(false);
        private ManualResetEvent dataForSaving = new ManualResetEvent(false);

        public void WriteLine(string message)
        {
            //Nastaveni limitu na 100k zprav, aby nemohlo dojit k preplneni pameti
            if (messages.Count < 100000)
            {
                lock (messages)
                {
                    messages.Add(message);
                    dataForDisplaying.Set();
                }
                    
            }
        
        }

        public void LogLine(String message)
        {
            //nastaveni limitu na 10k zprav
            if (logmessages.Count < 10000)
            {
                lock (logmessages)
                {
                    logmessages.Add(message);
                    dataForDisplaying.Set();
                }
            }
        }


        public void Start()
        {
            String file = "log.txt";
            log = File.AppendText(file);

            while (!stop)
            {
                if (messages.Count > 0)
                {
                    lock (messages)
                    {
                        //zde probiha vystup na obrazovku
                        Console.WriteLine(messages[0]);
                        //odstraneni vypisu z bufferu a uvolneni mista pro dalsi zpravy
                        messages.RemoveAt(0);
                    }
                }
                else if (logmessages.Count > 0)
                {
                    SaveLog();
                }
                else
                {
                    dataForDisplaying.Reset();
                    dataForDisplaying.WaitOne();
                }
            }
        }

        public void Stop()
        {
            SaveLog();

            stop = true;
            dataForDisplaying.Set();
        }

        public bool IsStoped()
        {
            return stop;
        }

        private void SaveLog()
        {
            if (log != null)
            {
                lock (log)
                {
                    lock (logmessages)
                    {
                        for (int t = 0; t < logmessages.Count; t++)
                        {
                            log.WriteLine(logmessages[t]);
                        }

                        log.Flush();
                        logmessages.Clear();
                    }
                }
            }

        }

    }
}
