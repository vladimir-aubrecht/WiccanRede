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

    public class GuardFSM : WiccanRede.AI.FSM
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

        public GuardFSM(NPC npc)
        {
            statesList = new List<IFsmState>();
            config = "Settings\\GuardFSM.xml";
            LoadFSM(config);
            //Logging.Logger.AddInfo("Nacten FSM:\n" + this.ToString());
            this.ActualState = this["Stand"];
            this.npc = npc;
            //rand = new Random();
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

        /// <summary>
        /// according to status npc change state and call action
        /// </summary>
        public override void Update()
        {
            Status npcStatus = this.npc.GetStatus();

            //end of fight
            if (npcStatus.enemySeen == 0 && (this.actualState.IsEqual("Conflict") || this.actualState.IsEqual("Defense")))
            {
                Logging.Logger.AddInfo("Straz: Nepritel ztracen");
                this.npc.TargetedEnemy = null;
                this.React(AiEvents.enemyLost);
            }
            //switch to fight
            else if (npcStatus.enemySeen > 0 && !(this.actualState.IsEqual("Defense") && !(this.actualState.IsEqual("Conflict"))))
            {
                int powerDifference = this.npc.character.level * (int)this.npc.GetStatus().hp - npcStatus.enemyPower;
                int fight = this.npc.character.priors.killPrior;
                int def = this.npc.character.priors.livePrior;

                if (powerDifference < (def - fight))
                {
                    this.React(AiEvents.disadvantage);
                }
                else 
                {
                    this.React(AiEvents.enemySeen);
                }
            }
            //isn't fighting, but has got enemy
            else if (this.npc.TargetedEnemy != null && !this.actualState.IsEqual("Defense") && !this.actualState.IsEqual("Conflict"))
            {
                this.React(AiEvents.seek);
            }
            else if (this.actualState.IsEqual("Conflict") || this.actualState.IsEqual("Defense"))
            {
                int powerDifference = this.npc.character.level * (int)this.npc.GetStatus().hp - npcStatus.enemyPower;
                int fight = this.npc.character.priors.killPrior;
                int def = this.npc.character.priors.livePrior;

                if (powerDifference < (def - fight) && this.npc.CurrentTask.time > 960)
                {
                    this.React(AiEvents.disadvantage);
                }
                else if (powerDifference < (def - fight) && this.npc.GetStatus().hp < this.npc.character.hp / 5)
                {
                    this.React(AiEvents.run);
                }
                else
                {
                    this.React(AiEvents.enemySeen);
                }
            }

            if (this.npc.TargetedEnemy != null && !this.npc.TargetedEnemy.GetStatus().alive)
            {
                Logging.Logger.AddInfo("Straz: Nepritel zabit");
                this.npc.TargetedEnemy = null;
                this.React(AiEvents.enemyKilled);
            }

            if (this.actualState.IsEqual("Start") 
                && this.npc.CurrentTask.checkpoints != null && this.npc.CurrentTask.checkpoints.Count > 0)
            {
                this.React(AiEvents.ready);
            }

            //Do action 
            this.CheckState(this.npc.ActionSelection);
        }

        private void React(WiccanRede.AI.AiEvents action)
        {
            switch (action)
            {
                case AiEvents.enemySeen:
                    ChangeState(this["Conflict"]);
                    break;
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
                    //ChangeState(this["Talk"]);
                    break;
                case AiEvents.newTask:
                    ChangeState(this["Stand"]);
                    break;
                case AiEvents.seek:
                    ChangeState(this["Seek"]);
                    break;
                default:
                    Logging.Logger.AddWarning("FSM: Straz, Neznama akce " + action.ToString());
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

        /// <summary>
        /// get names of FSM states
        /// </summary>
        /// <returns></returns>
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
            if (this.actualState.IsEqual("Conflict") || this.actualState.IsEqual("Defense"))
            {
                if (npc.TargetedEnemy == null)
                {
                    Logger.AddWarning("Straz- " + npc.GetCharacter().name + ": conflict, ale target je null");
                    return;
                }

                try
                {
                    Status npcStatus = npc.GetStatus();
                    Status enemyStatus = npc.TargetedEnemy.GetStatus();
                    //Logger.AddInfo(this.npc.character.name + ": " + npcStatus.ToString());
                    //ActionType type = (this.actualState.IsEqual("Confict") ? ActionType.Attack : ActionType.Defense);
                    Action selectedAction;

                    ConflictState state = actionSelection.GetNextAction(npc, npc.TargetedEnemy, npc.character.priors, out selectedAction);

                    //do moving
                    //if (rand.Next(100) < 95)
                    {
                        Point npcPos = this.npc.GetStatus().position;
                        Point enemyPos = this.npc.GetTargetedEnemy().GetStatus().position;

                        if (this.npc.CalculateDistance(npcPos, enemyPos) > 4)
                        {
                            Point diff = new Point();
                            diff.X = enemyPos.X - npcPos.X;
                            diff.Y = enemyPos.Y - npcPos.Y;

                            Point goal = this.npc.GetStatus().position;

                            //move with npc, try to stay at the "ideal" distance
                            if (diff.X != 0 && diff.Y != 0)
                            {
                                goal.Offset(new Point(diff.X / Math.Abs(diff.X), diff.Y / Math.Abs(diff.Y))); 
                            }
                                //don't want to divide by zero
                            else if (diff.X == 0)
                            {
                                goal.Offset(new Point(0, diff.Y / Math.Abs(diff.Y))); 
                            }
                            else if (diff.Y == 0)
                            {
                                goal.Offset(new Point(diff.X / Math.Abs(diff.X), 0)); 
                            }

                            goal = LocationCorrection(goal);
                            if (!Map.GetInstance().CellMap[goal.X, goal.Y].Block)
                            {
                                this.npc.DoFightMove(goal);
                            }
                        }
                        else
                        {
                            Point goal = this.npc.GetStatus().position;
                            goal = RandomMove(goal);
                            if (!Map.GetInstance().CellMap[goal.X, goal.Y].Block)
                            {
                                this.npc.DoFightMove(goal);
                            }
                        }
                    }

                    this.npc.DoAction(selectedAction, state);
                }
                catch (Exception ex)
                {
                    Logger.AddWarning("Chyba pri vybirani akce: " + ex.ToString());
                }
            }
            else if (this.actualState.IsEqual("Seek"))
            {
                Point goal = this.npc.GetStatus().position;
                goal = this.npc.TargetedEnemy.GetStatus().position;
                //goal = LocationCorrection(goal);

                //if (!Map.GetInstance().CellMap[goal.X, goal.Y].Block)
                {
                    this.npc.DoFightMove(goal);
                }
            }
            else if (this.actualState.IsEqual("Stand"))
            {
                //Point goal = this.npc.GetStatus().position;
                //goal = RandomMove(goal);

                //if (!Map.GetInstance().CellMap[goal.X, goal.Y].Block)
                //{
                //    this.npc.DoFightMove(goal);
                //}
            }
            else if (this.actualState.IsEqual("Talk"))
            {
                //TODO let's talk :)
            }
            else if (this.actualState.IsEqual("Start"))
            {
                if (this.npc.CurrentTask.checkpoints != null)
                    this.React(AiEvents.ready);
            }
        }

        private Point RandomMove(Point goal)
        {
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
            return goal;
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
