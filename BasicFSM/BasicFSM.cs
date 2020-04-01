using System;
using System.Data;
using System.Drawing;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Logging;
using WiccanRede.AI;

namespace WiccanRede.AI
{
    [System.Diagnostics.DebuggerDisplay("FSM_State: {name}")]
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

    public class BasicFSM : WiccanRede.AI.FSM
    {
        static List<IFsmState> statesList;
        public static List<IFsmState> StatesList
        {
            get { return statesList; }
        }

        NPC npc;
        string config;
        Random rand = new Random(DateTime.Now.Millisecond);

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

        public BasicFSM(NPC npc)
        {
            statesList = new List<IFsmState>();
            config = "Settings\\FSM.xml";
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

            //no more run away
            if (this.actualState.IsEqual("RunAway") && !this.npc.FightMove.isOnWay)
            {
                this.React(AiEvents.weakness);
            }
            //no energy or health
            else if ( (npcStatus.energy < 10 || this.npc.GetCharacter().hp / npcStatus.hp >= 4 )
                && !this.actualState.IsEqual("Conflict") && !this.actualState.IsEqual("Defense"))
            {
                this.React(AiEvents.weakness);
            }
            //energy is back
            else if (npcStatus.energy > 80 && this.npc.GetCharacter().hp - npcStatus.hp < (this.npc.GetCharacter().hp / 10)
                && this.actualState == this["Weakness"])
            {
                this.React(AiEvents.ready);
            }

            //end of fight
            if (npcStatus.enemySeen == 0 && (this.actualState.IsEqual("Conflict") || this.actualState.IsEqual("Defense")))
            {
                Logging.Logger.AddInfo("Nepritel ztracen");
                this.npc.TargetedEnemy = null;
                this.React(AiEvents.enemyLost);
            }
            //isn't fighting, but has got enemy
            //else if (this.npc.TargetedEnemy != null && !this.actualState.IsEqual("Defense") && !this.actualState.IsEqual("Conflict"))
            //{
            //    this.React(AiEvents.seek);
            //}
            //switch to fight
            else if (npcStatus.enemySeen > 0 && !this.actualState.IsEqual("Defense") && !(this.actualState.IsEqual("Conflict")))
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
            else if (this.actualState.IsEqual("Conflict") || this.actualState.IsEqual("Defense"))
            {
                int offense = (int)npcStatus.hp * this.npc.GetCharacter().priors.killPrior * this.npc.GetCharacter().level;
                int defense = (int)this.npc.GetTargetedEnemy().GetStatus().hp * this.npc.GetTargetedEnemy().GetCharacter().level * this.npc.GetCharacter().priors.livePrior;


                //if (this.npc.CurrentTask.time > 40)
                {
                    if (defense - offense > 0 && (int)npcStatus.hp < this.npc.GetCharacter().hp / 5)
                    {
                        this.React(AiEvents.run);
                    }
                    else if (defense - offense > 0 && (int)npcStatus.hp < this.npc.GetCharacter().hp / 4)
                    {
                        this.React(AiEvents.disadvantage);
                    }
                }
            }
            else if (this.actualState.IsEqual("Stand") && !this.npc.CurrentTask.finished)
            {
                this.React(AiEvents.ready);
            }

            if (this.npc.TargetedEnemy != null && !this.npc.TargetedEnemy.GetStatus().alive)
            {
                Logging.Logger.AddInfo("Nepritel zabit");
                this.npc.TargetedEnemy = null;
                this.React(AiEvents.enemyKilled);
            }

            CheckState(this.npc.ActionSelection);
        }

        internal void React(WiccanRede.AI.AiEvents action)
        {
            switch (action)
            {
                case AiEvents.enemyKilled:
                    ChangeState(this["Go"]);
                    break;
                case AiEvents.enemySeen:
                    ChangeState(this["Conflict"]);
                    break;
                case AiEvents.enemyLost:
                    ChangeState(this["Go"]);
                    break;
                case AiEvents.ready:
                    ChangeState(this["Go"]);
                    break;
                case AiEvents.weakness:
                    ChangeState(this["Weakness"]);
                    break;
                case AiEvents.lowHP:
                    ChangeState(this["Defense"]);
                    break;
                case AiEvents.disadvantage:
                    ChangeState(this["Defense"]);
                    break;
                case AiEvents.newTask:
                    ChangeState(this["Go"]);
                    break;
                case AiEvents.run:
                    ChangeState(this["RunAway"]);
                    break;
                case AiEvents.seek:
                    ChangeState(this["Seek"]);
                    break;
                default:
                    Logging.Logger.AddWarning("FSM: Neznama akce " + action.ToString());
                    break;
            }
        }

        private bool ChangeState(IFsmState state)
        {
            if (this.actualState.GetNextPossibleStates().Contains(state.GetName()))
            {
                if (this.npc.TargetedEnemy != null)
                {
                    Logging.Logger.AddInfo("NPC: " + this.npc.GetCharacter().name + " Menim stav na " + state.GetName() + " cil je " + this.npc.TargetedEnemy.GetCharacter().name); 
                }
                else
                {
                    Logging.Logger.AddInfo("NPC: " + this.npc.GetCharacter().name + " Menim stav na " + state.GetName()); 
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

        internal void CheckState(IActionSelection actionSelection)
        {
            if (this.actualState.IsEqual("Go"))
            {
                npc.Go();
            }
            else if (this.actualState.IsEqual("Conflict") || this.actualState.IsEqual("Defense"))
            {
                if (npc.TargetedEnemy == null)
                {
                    Logger.AddWarning(npc.GetCharacter().name + ": conflict, ale target je null");
                    return;
                }

                try
                {
                    Status npcStatus = npc.GetStatus();
                    //Status enemyStatus = npc.TargetedEnemy.GetStatus();
                    //Logger.AddInfo(this.npc.character.name + ": " + npcStatus.ToString());
                    //ActionType type = (this.actualState.IsEqual("Confict") ? ActionType.Attack : ActionType.Defense);
                    Action selectedAction;

                    ConflictState state = actionSelection.GetNextAction(npc, npc.TargetedEnemy, npc.character.priors, out selectedAction);

                    //do moving
                    #region moving
                    //if (rand.Next(100) < 80)
                    {
                        Point npcPos = this.npc.GetStatus().position;
                        Point enemyPos = this.npc.GetTargetedEnemy().GetStatus().position;
                        int dist = this.npc.CalculateDistance(npcPos, enemyPos);

                        if (dist > 10 && this.npc.TargetedEnemy.GetCharacter().name == "Player")
                        {
                            this.npc.DoFightMove(enemyPos);
                        }
                        else if (dist > 4)
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
                                //diff.X++;
                                goal.Offset(0, diff.Y / Math.Abs(diff.Y));
                            }
                            else if (diff.Y == 0)
                            {
                                diff.Y++;
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
                            double random = this.rand.NextDouble();
                            double random2 = this.rand.NextDouble();
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
                    #endregion
                    }

                    this.npc.DoAction(selectedAction, state, this.actualState.GetName());
                }
                catch (Exception ex)
                {
                    Logger.AddWarning("Chyba pri vybirani akce: " + ex.ToString());
                }
            }
            else if (this.actualState.IsEqual("RunAway"))
            {
                Point goal = new Point();
                //Point p = new Point();
                //p.X = this.npc.GetPosition2D().X - this.npc.GetTargetedEnemy().GetStatus().position.X;
                //p.Y = this.npc.GetPosition2D().Y - this.npc.GetTargetedEnemy().GetStatus().position.Y;
                //goal = this.npc.GetPosition2D();
                //goal.Offset(p);
                int offset = 25;
                Point current = this.npc.GetPosition2D();
                List<Point> locations = new List<Point>();

                Point up = current;
                up.Offset(0, offset);
                up = LocationCorrection(up);
                locations.Add(up);
                Point down = current;
                down.Offset(0, -offset);
                down = LocationCorrection(down);
                locations.Add(down);
                Point left = current;
                left.Offset(-offset, 0);
                left = LocationCorrection(left);
                locations.Add(left);
                Point right = current;
                right.Offset(offset, 0);
                right = LocationCorrection(right);
                locations.Add(right);

                Point rightUp = current;
                rightUp.Offset(offset, offset);
                rightUp = LocationCorrection(rightUp);
                locations.Add(rightUp);
                Point leftUp = current;
                leftUp.Offset(-offset, offset);
                leftUp = LocationCorrection(leftUp);
                locations.Add(leftUp);
                Point leftDown = current;
                leftDown.Offset(-offset, -offset);
                leftDown = LocationCorrection(leftDown);
                locations.Add(leftDown);
                Point rightDown = current;
                rightDown.Offset(offset, -offset);
                rightDown = LocationCorrection(rightDown);
                locations.Add(rightDown);

                //find safest new location to run to
                int eval = 0;
                int bigest = 0;
                foreach (Point loc in locations)
                {
                    foreach (IActing enemy in this.npc.GetStatus().nearEnemies)
                    {
                        eval += this.npc.CalculateDistance(enemy.GetStatus().position, loc);
                    }
                    if (eval > bigest)
                    {
                        bigest = eval;
                        goal = loc;
                    }
                    eval = 0;
                }

                goal = LocationCorrection(goal);

                npc.DoFightMove(goal);
            }
            else if (this.actualState.IsEqual("Stand"))
            {
                //TODO - what to do in stand state?
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
            else if (this.actualState.IsEqual("Weakness"))
            {
                this.npc.Rest();
            }
            else
            {
                Logger.AddWarning("BasicFSM: neznamy stav automatu - " + this.actualState);
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
