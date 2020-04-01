using System;
using Logging;
using System.Collections.Generic;
using System.Text;
using WiccanRede.AI.Scripting;

namespace WiccanRede.AI
{
  public class ControlNPC
  {
    Scripting.ScriptingCsharp taskScripting;
    Scripting.ScriptingCsharp talkScripting;
    Scripting.ScriptingCsharp guardScripting;

    //DynamicProgramming dynamic;
    //ActionSelection actionSelection;

    /// <summary>
    /// ctor, init scripting
    /// </summary>
    public ControlNPC()
    {
      taskScripting = new ScriptingCsharp();
      taskScripting.LoadScript("Scripting\\Scripts\\script1.cs");

      talkScripting = new ScriptingCsharp();
      talkScripting.LoadScript("Scripting\\Scripts\\talkScript.cs");

      guardScripting = new ScriptingCsharp();
      guardScripting.LoadScript("Scripting\\Scripts\\guardSCript1.cs");
    }

    /// <summary>
    /// update for all npcs, check their cooldown, life and continue to update for each npc
    /// </summary>
    /// <param name="npcs">list of all npcs</param>
    /// <param name="step">time step from last update in seconds</param>
    public void Update(List<NPC> npcs, float step)
    {
      //lock (npcs)
      {
        for (int i = 0; i < npcs.Count; i++)
        {
          if (!npcs[i].GetStatus().alive)
          {
            npcs.RemoveAt(i);        //npc is dead;
          }
        }
        Map.GetInstance().Update(npcs);
        foreach (NPC npc in npcs)
        {
          if (npc.Cooldown > 0)
          {
            npc.UpdateTime(step);
            continue;
          }
          npc.UpdateNPC(npcs, step);

          //IFsmState actualState = npc.State;
          //switch (npc.character.type)
          //{
          //    case NPCType.beast:
          //        break;
          //    case NPCType.guard:
          //        npc.Fsm.CheckState(npc.ActionSelection);
          //        break;
          //    case NPCType.villager:
          //        npc.Fsm.CheckState(npc.ActionSelection);
          //        break;
          //    case NPCType.enemy:
          //        npc.Fsm.CheckState(npc.ActionSelection);
          //        break;
          //    case NPCType.boss:
          //        break;
          //    default:
          //        break;
          //}
          npc.Entity.SetStatus(npc.GetStatus());
        }
      }
    }

    /// <summary>
    /// game update for all npcs, run script for the given gamestat and potencionally add tasks
    /// </summary>
    /// <param name="npcs">list of all npcs</param>
    /// <param name="step">time step from last update in seconds</param>
    /// <param name="gameState">new gamestate name</param>
    /// <seealso cref="Scripting"/>
    public void Update(List<NPC> npcs, float step, string gameState)
    {
      //lock (npcs)
      {
        Map.GetInstance().Update(npcs);
        foreach (NPC npc in npcs)
        {
          switch (npc.character.type)
          {
            case NPCType.beast:
              break;
            case NPCType.guard:
              try
              {
                object[] par = { gameState, npc };
                NpcTask newTarget;
                newTarget = (NpcTask)guardScripting.RunMethod("Update", par);
                npc.AddTask(newTarget);
                //npc.UpdateNPC(npcs, step);
              }
              catch (Exception ex)
              {
                Logger.AddWarning("Chyba pri zadavani cilu pro NPC " + npc.character.name + ex.ToString());
              }
              break;
            case NPCType.enemy:
              try
              {
                object[] par = { gameState, npc };
                NpcTask newTarget;
                //newTarget = (NpcTask)taskScripting.RunMethod("Update", par);
                newTarget = (NpcTask)taskScripting.RunMethodFromType("Update", par)[0];
                npc.AddTask(newTarget);
                //npc.UpdateNPC(npcs, step);
              }
              catch (Exception ex)
              {
                Logger.AddWarning("Chyba pri zadavani cilu pro NPC " + npc.character.name + ex.ToString());
              }
              break;
            case NPCType.villager:
              break;
            case NPCType.boss:
              break;
            default:
              break;
          }
        }
      }
    }

    public void Update(List<NPC> npcs, string npcName, string gameState)
    {
      if (npcName == "Story")
      {
        try
        {
          object[] par = { gameState, npcName };
          List<string> talks = (List<string>)this.talkScripting.RunMethod("Update", par);
          if (talks.Count > 0)
          {
            Random rand = new Random();
            int i = rand.Next(talks.Count - 1);
            GamePlayer.GetPlayer().GetEntity().Talk(talks[i]);
          }
        }
        catch (Exception ex)
        {
          Logger.AddWarning("Chyba pri spousteni talk scriptu " + ex.ToString());
        }
      }
      foreach (NPC npc in npcs)
      {
        if (npc.character.name == npcName)
        {
          if (npc.character.type == NPCType.villager)
          {
            try
            {
              object[] par = { gameState, npcName };
              List<string> talks = (List<string>)this.talkScripting.RunMethod("Update", par);
              if (talks.Count > 0)
              {
                Random rand = new Random();
                int i = rand.Next(talks.Count - 1);
                npc.GetEntity().Talk(talks[i]);
              }
            }
            catch (Exception ex)
            {
              Logger.AddWarning("Chyba pri spousteni talk scriptu " + ex.ToString());
            }
          }
          break;
        }
      }
    }

  }
}
