using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WiccanRede.AI.Scripting
{
    public class ScriptFunctions
    {
        NPC npc;
        List<Action> actions;
        Dictionary<string, Action> spells;

        public ScriptFunctions(NPC npc)
        {
            this.npc = npc;
            actions = npc.ActionSelection.GetActions();
            spells = new Dictionary<string, Action>();
            foreach (Action a in actions)
            {
                spells.Add(a.name, a);
            }
        }

        [AI.Scripting.AttributScriptMethod("GetStatus", "Gets npc status")]
        public Status GetStatus()
        {
            return this.npc.GetStatus();
        }

        [AI.Scripting.AttributScriptMethod("GetEnemyStatus", "Gets npc's target status")]
        public Status GetEnemyStatus()
        {
            return this.npc.GetTargetedEnemy().GetStatus();
        }

        [AI.Scripting.AttributScriptMethod("GetNpc", "Gets current npc")]
        public NPC GetNpc()
        {
            return this.npc;
        }

        [AI.Scripting.AttributScriptMethod("Spell", "cast spell")]
        public void Spell(string spellName)
        {
            Action action = new Action();
            Logging.Logger.AddInfo(this.npc.character.name + " kouzli " + spellName);
            this.spells.TryGetValue(spellName, out action);

            if (action.name != null)
            {
                this.npc.SetCoolDown(action.time);
                this.npc.Heal(action.hpGot);
                this.npc.DrainMana(action.manaDrain);

                ActionInfo info = new ActionInfo();
                info.action = action;
                info.npcName = this.npc.character.name;
                info.startPosition = this.npc.GetPosition3D();
                info.targetName = this.npc.GetTargetedEnemy().GetCharacter().name;
                info.targetPosition = this.npc.GetTargetedEnemy().GetPosition3D();
                this.npc.GetEntity().Spell(info); 
            }
        }

        [AI.Scripting.AttributScriptMethod("GetAction", "get info about action")]
        public Action GetAction(string actionName)
        {
            Action action = new Action();
            this.spells.TryGetValue(actionName, out action);
            return action;
        }

        [AI.Scripting.AttributScriptMethod("Talk", "send text to talk")]
        public void Talk(string text)
        {
            this.npc.GetEntity().Talk(this.npc.character.name + ": " + text);
        }

        [AI.Scripting.AttributScriptMethod("Stun", "stun current enemy")]
        public void Stun(int stunTime)
        {
            this.npc.GetTargetedEnemy().Stun(stunTime);
            this.npc.SetCoolDown(1500);
        }

        [AI.Scripting.AttributScriptMethod("Log", "add logging information")]
        public void AddLogMessage(string text)
        {
            Logging.Logger.AddInfo("LUA: " + this.npc.character.name + ": " + text);
        }

        [AI.Scripting.AttributScriptMethod("SummonGuards", "will add new npc into the game")]
        public void SummonGuards()
        {
            Logging.Logger.AddInfo("Summon guardu pro bose");
        }

        [AI.Scripting.AttributScriptMethod("GoAt", "will go on the given position")]
        public void GoAt(int x, int y)
        {
            if (Map.GetInstance().CellMap[x, y].Block)
                return;

            System.Drawing.Point goal = new System.Drawing.Point(x, y);
            goal = LocationCorrection(goal);
            this.npc.DoFightMove(goal);
        }

        [AI.Scripting.AttributScriptMethod("IsMoving", "return if the npc is on way")]
        public bool IsMoving()
        {
            return this.npc.FightMove.isOnWay;
        }

        [AI.Scripting.AttributScriptMethod("Hide", "hide npc")]
        public void Hide()
        {

        }

        [AI.Scripting.AttributScriptMethod("Rest", "recharge hp, mana, energy")]
        public void Rest()
        {
            this.npc.Rest();
        }

        [AI.Scripting.AttributScriptMethod("HasTask", "indicates if npc has task to go on")]
        public bool HasTask()
        {
            if (this.npc.CurrentTask.checkpoints == null)
                return false;
            return (this.npc.CurrentTask.checkpoints.Count > 0 );
        }

        [AI.Scripting.AttributScriptMethod("Go", "send npc to his task's checkpoints")]
        public void Go()
        {
            this.npc.Go();
        }

        private static Point LocationCorrection(Point goal)
        {
            Point size = Map.GetInstance().MapSize;
            if (goal.X < 0)
                goal.X = 0;
            else if (goal.X >= size.X)
                goal.X = size.X - 2;

            if (goal.Y < 0)
                goal.Y = 0;
            else if (goal.Y >= size.Y)
                goal.Y = size.Y - 2;
            return goal;
        }
    }
}

