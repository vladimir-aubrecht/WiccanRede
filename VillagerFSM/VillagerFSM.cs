using System;
using System.Data;
using System.Drawing;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using Logging;
using WiccanRede.AI;

namespace WiccanRede.AI
{
    public struct FSM_State : WiccanRede.AI.IFsmState
    {
        public string name;
        public List<string> nextPossibleStates;

        //public Priorities priors;

        #region IFsmState Members

        public string GetName()
        {
            return name;
        }

        public List<string> GetNextPossibleStates()
        {
            return nextPossibleStates;
        }

        public bool IsEqual(string stateName)
        {
            return this.name == stateName;
        }

        //#region comparing operators
        //public static bool operator ==(FSM_State state1, FSM_State state2)
        //{
        //    return state1.name == state2.name;
        //}
        //public static bool operator !=(FSM_State state1, FSM_State state2)
        //{
        //    return state1.name != state2.name;
        //}
        //public static bool operator ==(FSM_State state1, string state2)
        //{
        //    return state1.name == state2;
        //}
        //public static bool operator !=(FSM_State state1, string state2)
        //{
        //    return state1.name != state2;
        //}
        //#endregion

        #endregion
    }

    public class VillagerFSM : WiccanRede.AI.FSM
    {
        static List<IFsmState> statesList;
        public static List<IFsmState> StatesList
        {
            get { return statesList; }
        }

        NPC npc;
        string config;
        static Random rand = new Random();

        public IFsmState this[string name]
        {
            get
            {
                foreach (IFsmState state in statesList)
	            {
                    if (state.GetName() == name)
                    {
                        return state;
                    }
	            }
                throw new Exception("Takovy stav neexistuje, " + name);
            }
        }

        public VillagerFSM(NPC npc)
        {
            statesList = new List<IFsmState>();
            config = "Settings\\VillagerFSM.xml";
            LoadFSM(config);
            //Logging.Logger.AddInfo("Nacten FSM:\n" + this.ToString());
            this.ActualState = this["Stand"];
            this.npc = npc;
        }

        private void LoadFSM(string config)
        {
            System.Xml.XmlDataDocument doc = new XmlDataDocument();
            doc.Load(config);

            FSM_State state = new FSM_State();
            foreach (XmlNode node in doc.GetElementsByTagName("State"))
            {
                state.nextPossibleStates = new List<String>();
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name == "StateName")
                    {
                        state.name = child.InnerText;
                    }
                    else if (child.Name == "NextPossibleState")
                    {
                        state.nextPossibleStates.Add(child.InnerText);
                    }
                }
                statesList.Add(state);
            }
            //states = new AllPosibleStates(this.statesList.ToArray());
        }

        public override void Update()
        {
            Status npcStatus = this.npc.GetStatus();

            //end of fight
            if (npcStatus.enemySeen == 0 && (this.actualState.IsEqual("Conflict") || this.actualState.IsEqual("Defense")))
            {
                Logging.Logger.AddInfo("Villager: Nepritel ztracen");
                this.npc.TargetedEnemy = null;
                this.React(AiEvents.enemyLost);
            }
            //switch to fight
            else if (npcStatus.enemySeen > 0 && !this.actualState.IsEqual("Defense"))
            {

            }

            if (this.npc.TargetedEnemy != null && !this.npc.TargetedEnemy.GetStatus().alive)
            {
                Logging.Logger.AddInfo("Villager: Nepritel zabit");
                this.npc.TargetedEnemy = null;
                this.React(AiEvents.enemyKilled);
            }


            //Do action
            this.CheckState(this.npc.ActionSelection);
        }

        private void React(WiccanRede.AI.AiEvents action)
        {
            switch (action)
            {
                case AiEvents.enemyLost:
                    ChangeState(this["Stand"]);
                    break;
                case AiEvents.ready:
                    ChangeState(this["Stand"]);
                    break;
                case AiEvents.lowHP:
                    ChangeState(this["Defense"]);
                    break;
                case AiEvents.disadvantage:
                    ChangeState(this["Defense"]);
                    break;
                case AiEvents.talk:
                    ChangeState(this["Talk"]);
                    break;
                default:
                    Logging.Logger.AddWarning("FSM: Villager, Neznama akce " + action.ToString());
                    break;
            }
        }

        private bool ChangeState(IFsmState state)
        {
            if (this.actualState.GetNextPossibleStates().Contains(state.GetName()))
            {
                if (this.npc.TargetedEnemy != null)
                {
                    Logging.Logger.AddInfo("Straz,NPC: " + this.npc.GetCharacter().name + " Menim stav na " + state.GetName() + " cil je " + this.npc.TargetedEnemy.GetCharacter().name); 
                }
                else
                {
                    Logging.Logger.AddInfo("Straz,NPC: " + this.npc.GetCharacter().name + " Menim stav na " + state.GetName()); 
                }
                this.actualState = state;
                return true;
            }
            else
	        {
                return false;
	        }
        }

        public List<string> GetFsmStatesName()
        {
            List<string> states = new List<string>();

            foreach (IFsmState state in statesList)
            {
                states.Add(state.GetName());
            }
            return states;
        }

        private void CheckState(IActionSelection actionSelection)
        {
            if (this.actualState.IsEqual("Defense"))
            {
                if (npc.TargetedEnemy == null)
                {
                    Logger.AddWarning("Villager- " + npc.GetCharacter().name + ": conflict, ale target je null");
                    return;
                }

                try
                {
                    Status npcStatus = npc.GetStatus();
                    //Status enemyStatus = npc.TargetedEnemy.GetStatus();
                    Logger.AddInfo(this.npc.character.name + ": " + npcStatus.ToString());
                    //ActionType type = ActionType.Defense; //(this.actualState.IsEqual("Confict") ? ActionType.Attack : ActionType.Defense);
                    Action selectedAction;

                    ConflictState state = actionSelection.GetNextAction(npc, npc.TargetedEnemy, npc.character.priors, out selectedAction);

                    this.npc.DoAction(selectedAction, state);
                }
                catch (Exception ex)
                {
                    Logger.AddWarning("Chyba pri vybirani akce: " + ex.ToString());
                }
            }
            else if (this.actualState.IsEqual("Stand"))
            {
                Point goal = this.npc.GetStatus().position;

                double random = rand.NextDouble();
                double random2 = rand.NextDouble();
                int offset1 = 0;
                int offset2 = 0;

                if (random > 0.66)
                    offset1 = 1;
                else if (random > 0.33)
                    offset1 = -1;

                if (random2 > 0.66)
                    offset2 = 1;
                else if (random2 > 0.33)
                    offset2 = -1;

                //move randomly with NPC
                goal.Offset(offset1, offset2);

                goal = LocationCorrection(goal);
                if (!Map.GetInstance().CellMap[goal.X, goal.Y].Block)
                {
                    this.npc.DoFightMove(goal);
                }
            }
            else if (this.actualState.IsEqual("Talk"))
            {
                //TODO let's talk :)
            }
        }

        private static Point LocationCorrection(Point goal)
        {
            //correction
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

        public override string ToString()
        {
            string str = "FSM: ";
            foreach (IFsmState state in statesList)
            {
                str += state.GetName();

                Console.WriteLine();
                foreach (string nextState in state.GetNextPossibleStates())
                {
                    str += ("-" + nextState);
                }
                str += "<br />";
                Console.WriteLine();
            }
            return str;
        }
    }
}
