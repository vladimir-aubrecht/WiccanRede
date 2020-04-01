using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WiccanRede.AI.Scripts
{
    public class ScriptClass: AI.IScripting
    {
        public ScriptClass()
        {
            Logging.Logger.AddInfo("inicializace skriptu...");
        }

        public object[] Update(params object[] pars)
        {
            string state = "";
            WiccanRede.AI.NPC npc;
            if (pars[0] is string)
            {
                state = (string) pars[0];
            }
            else
            {
                Logging.Logger.AddInfo("Chybne parametry pro skript, ocekavan string, predan : " + pars[0].GetType().ToString());
            }
            if (pars[1] is WiccanRede.AI.NPC)
            {
                npc = (WiccanRede.AI.NPC)pars[1];
            }
            else
            {
                Logging.Logger.AddInfo("Chybne parametry pro skript, ocekavan NPC, predan : " + pars[0].GetType().ToString());
            }
            WiccanRede.AI.NpcTask tar = new WiccanRede.AI.NpcTask();
            tar.checkpoints = new List<Point>();
            tar.result = Result.none;

            switch (state)
            {
                case "Start":
                    //Logging.Logger.AddInfo("skript: Novy stav npc: " + state);
                    break;
                case "Pub":
                    Logging.Logger.AddInfo("skript: Novy stav npc: " + state);
                    //tar.checkpoints.Add(new Point(20, 15));
                    //tar.checkpoints.Add(new Point(20, 5));
                    //tar.checkpoints.Add(new Point(5, 25));
                    //tar.time = float.PositiveInfinity;
                    break;
                case "Obelisc":
                    //Logging.Logger.AddInfo("skript: Novy stav npc: " + state);
                    tar.order = false;
                    tar.checkpoints.Add(new Point(27, 74));
                    tar.checkpoints.Add(new Point(11, 116));
                    tar.checkpoints.Add(new Point(50, 10));
                    tar.checkpoints.Add(new Point(88, 39));
                    tar.checkpoints.Add(new Point(90, 100));
                    tar.time = 20 * 60;
                    break;
                case "Forest":
                    //Logging.Logger.AddInfo("skript: Novy stav npc: " + state);
                    tar.checkpoints.Add(new Point(100, 120));
                    tar.time = 5 * 60;
                    break;
                case "Final":
                    //Logging.Logger.AddInfo("skript: Novy stav npc: " + state);
                    tar.checkpoints.Add(new Point(110, 110));
                    tar.time = float.PositiveInfinity;
                    break;
                default:
                    Logging.Logger.AddWarning("skript: Neznamy stav: " + state);
                    break;
            }
            return new object[] { tar };
        }

    }
}
