using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WiccanRede.AI.Scripts
{
    class guardScript1
    {
        public static WiccanRede.AI.NpcTask Update(string state, WiccanRede.AI.NPC npc)
        {
            WiccanRede.AI.NpcTask tar = new WiccanRede.AI.NpcTask();
            tar.checkpoints = new List<System.Drawing.Point>();
            System.Drawing.Point pos = npc.GetPosition2D();
            tar.result = Result.none;
            switch (state)
            {
                case "Start":
                    break;
                case "Pub":
                    break;
                case "Obelisc":
                    //tar.checkpoints.Add(pos);
                    break;
                case "Forest":
                    //tar.checkpoints.Add(new Point(105, 115));
                    break;
                case "Final":
                    Logging.Logger.AddInfo("skript: Novy stav npc: " + state);
                    tar.checkpoints.Add(new Point(110, 110));
                    break;
                default:
                    tar.checkpoints.Add(pos);
                    break;
            }
            return tar;
        }
    }
}
