using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Logging;

namespace WiccanRede.AI
{
    [System.Diagnostics.DebuggerDisplay("DPItem: cost={state.cost}, {state.ToString()}")]
    class DPItem
    {
        private ConflictState state;

        public ConflictState State
        {
            get { return state; }
            set { state = value; }
        }

        private int probability;

        public int Probability
        {
            get { return probability; }
            set { probability = value; }
        }

        private Action action;

        public Action Action
        {
            get { return action; }
            set { action = value; }
        }

        private List<ConflictState> children;

        public List<ConflictState> Children
        {
            get { return children; }
            set { children = value; }
        }

        public override int GetHashCode()
        {
            return state.GetHashCode();// +action.GetHashCode() + probability.GetHashCode();
        }
    }

    [System.Diagnostics.DebuggerDisplay("CopyOfDPItem: cost={state.cost}, {state.ToString()}")]
    class CopyOfDPItem
    {
        private ConflictState state;

        public ConflictState State
        {
            get { return state; }
            set { state = value; }
        }

        private int probability;

        public int Probability
        {
            get { return probability; }
            set { probability = value; }
        }

        private Action action;

        public Action Action
        {
            get { return action; }
            set { action = value; }
        }

        private List<ConflictState> children;

        public List<ConflictState> Children
        {
            get { return children; }
            set { children = value; }
        }

        public override int GetHashCode()
        {
            return state.GetHashCode();// +action.GetHashCode() + probability.GetHashCode();
        }
    }

    public enum ActionType
    {
        Attack,
        Defense
    }

    public class CopyOfDynamicProgramming : WiccanRede.AI.IActionSelection
    {
        List<Action> attackActions;
        List<Action> defenseActions;
        List<ConflictState> stateSpace;
        List<Action> bestActions;

        Dictionary<int, DPItem> space;

        const int iterationCount = 10;
        const int failProb = 10;
        Random rand;

        int stateSpaceDeep = 4;

        public CopyOfDynamicProgramming()
        {
            attackActions = new List<Action>();
            defenseActions = new List<Action>();
            stateSpace = new List<ConflictState>();
            bestActions = new List<Action>();

            space = new Dictionary<int, DPItem>();

            rand = new Random();
            try
            {
                this.attackActions = LoadActions("Settings/AttackSpells.xml");
                this.defenseActions = LoadActions("Settings/DefenseSpells.xml");
            }
            catch (Exception ex)
            {
                Logging.Logger.AddWarning("Chyba pri nacitani konfigurace kouzel: " + ex.ToString());
                throw;
            }
        }

        private List<Action> LoadActions(string config)
        {
            List<Action> loadedActions = new List<Action>();
            XmlDataDocument doc = new XmlDataDocument();
            doc.Load(config);

            foreach (XmlNode node in doc.GetElementsByTagName("Action"))
            {
                Action action = new Action();
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "Name":
                            action.name = child.InnerText;
                            break;
                        case "EnemyHpTaken":
                            action.enemyHpTaken = Convert.ToInt32(child.InnerText);
                            break;
                        case "EnemyManaDrain":
                            action.enemyManaDrain = Convert.ToInt32(child.InnerText);
                            break;
                        case "ManaDrain":
                            action.manaDrain = Convert.ToInt32(child.InnerText);
                            break;
                        case "HpGot":
                            action.hpGot = Convert.ToInt32(child.InnerText);
                            break;
                        case "MyShift":
                            action.myShift = Convert.ToInt32(child.InnerText);
                            break;
                        case "EnemyShift":
                            action.enemyShift = Convert.ToInt32(child.InnerText);
                            break;
                        case "EnemySpeedReduce":
                            action.enemySpeedReduce = Convert.ToSingle(child.InnerText);
                            break;
                        case "EnemyEnergyDrain":
                            action.enemyEnergyDrain = Convert.ToInt32(child.InnerText);
                            break;
                        case "MyEnergyDrain":
                            action.myEnergyDrain = Convert.ToInt32(child.InnerText);
                            break;
                        case "Probability":
                            action.probability = Convert.ToInt32(child.InnerText);
                            break;
                        case "Time":
                            action.time = Convert.ToInt32(child.InnerText);
                            break;
                        default:
                            Logging.Logger.AddWarning("Neznamy paramter pri nacitani xml kongigurace kouzel");
                            break;
                    }
                }
                loadedActions.Add(action);
            }
            return loadedActions;
        }

        public void CreateStateSpace(ConflictState currentState, CharacterNPC character, CharacterNPC enemyChar, ActionType actinType)
        {
            Logging.Logger.StartTimer("Vytvareni statespace");
            int step = 0;
            Recurse(currentState, character, enemyChar, actinType, step);
            Logging.Logger.StopTimer("Vytvareni statespace");
            Logger.AddInfo("Velikost State space: " + this.stateSpace.Count.ToString());
            Logger.AddInfo("Velikost space: " + this.space.Count.ToString());
            this.bestActions.Capacity = this.stateSpace.Count;
        }

        private void Recurse(ConflictState state, CharacterNPC character, CharacterNPC enemyChar, ActionType actinType, int step)
        {
            List<ConflictState> nextStates = GetSuccessors(state, character, enemyChar, actinType);
            DPItem item = new DPItem();
            item.Probability = state.probability;
            item.State = state;
            item.Children = nextStates;
            if (!space.ContainsKey(item.GetHashCode()))
            {
                this.space.Add(item.GetHashCode(), item);
            }

            step++;
            if (step < this.stateSpaceDeep)
            {
                ConflictState successor;
                for (int i = 0; i < nextStates.Count; i++)
                {
                    successor = nextStates[i];

                    if (this.stateSpace.Contains(successor))
                    {
                        //Logger.AddInfo("DP: nalezena duplicita. " + successor.ToString());
                        continue;
                    }
                    if (successor.enemyHP <= 0)
                    {
                        Logger.AddInfo("DP:Nalezen cilovy stav " + successor.ToString());
                        successor.cost = 0;
                        //step = this.stateSpaceDeep;
                        nextStates[i] = successor;
                        break;
                    }
                    else if (successor.myHP < character.hp / 4)
                    {
                        successor.cost = 100;
                        nextStates[i] = successor;
                    }
                    this.stateSpace.Add(successor);
                    //try
                    //{
                    //    if (!space.ContainsKey(successor.GetHashCode()))
                    //    {
                    //        space.Add(successor.GetHashCode(), successor); 
                    //    }
                    //    else
                    //    {
                    //        //Logger.AddWarning("DP: chyba pri pridavani do hash tabulky..." + successor.ToString());
                    //    }
                    //}
                    //catch (Exception)
                    //{
                    //    Logger.AddWarning("DP: chyba pri pridavani do hash tabulky..." + successor.ToString());
                    //}
                    Recurse(successor, character, enemyChar, actinType, step);
                }
                //this.stateSpace.AddRange(nextStates);
            }
        }

        public void Iterate(ConflictState state)
        {
            int h = 10;
            DPItem item = new DPItem();

            if (!space.TryGetValue(state.GetHashCode(), out item))
            {
                return;
            }

            for (int i = 0; i < iterationCount; i++)
            {
                foreach (KeyValuePair<int, DPItem> pair in space)
                {
                    //int min = int.MaxValue;
                    h = pair.Value.State.cost;
                    ConflictState current = pair.Value.State;
                    foreach (ConflictState s in pair.Value.Children)
                    {
                        h += s.probability * s.cost;
                        //if (state.cost < min)
                        //{
                        //    min = state.cost;
                        //}
                    }
                    pair.Value.State = current;
                }
            }
        }

        private List<ConflictState> GetSuccessors(ConflictState state, CharacterNPC character, CharacterNPC enemyChar, ActionType actionType)
        {
            List<ConflictState> successors = new List<ConflictState>();
            List<Action> actions = null;
            switch (actionType)
            {
                case ActionType.Attack:
                    actions = this.attackActions;
                    break;
                case ActionType.Defense:
                    actions = this.defenseActions;
                    break;
                default:
                    Logging.Logger.AddWarning("DP: Spatny typ akce");
                    return successors;
                //break;
            }

            ConflictState succesState = new ConflictState();
            //ConflictState noEffectState = new ConflictState();
            ConflictState failState = new ConflictState();

            ConflictState enemySuccesState = new ConflictState();
            ConflictState enemyFailState = new ConflictState();

            foreach (Action action in actions)
            {
                if (action.probability == 100)
                {
                    succesState = state + action;
                    successors.Add(succesState);
                }
                else
                {
                    //spell is casted by "this" npc
                    succesState = state + action;               //spell casted succesfull
                    succesState.probability = action.probability;
                    //noEffectState = state;                      //casting has no effect,
                    failState = state;                           //casting fail and harm caster
                    failState.myHP -= action.enemyHpTaken;
                    failState.myHP -= action.hpGot;
                    failState.probability = failProb;

                    //spell is casted by this NPCs enemy
                    enemySuccesState = state - action;
                    enemySuccesState.probability = action.probability;
                    enemyFailState = state;
                    enemyFailState.enemyHP -= action.enemyHpTaken;
                    enemyFailState.enemyHP -= action.hpGot;
                    enemyFailState.probability = failProb;

                    //check if states are valid
                    if (CheckStats(character, enemyChar, ref enemyFailState))
                    {
                        successors.Add(enemyFailState);
                    }
                    if (CheckStats(character, enemyChar, ref enemySuccesState))
                    {
                        successors.Add(enemySuccesState);
                    }
                    if (CheckStats(character, enemyChar, ref succesState))
                    {
                        successors.Add(succesState);
                    }
                    if (CheckStats(character, enemyChar, ref failState))
                    {
                        successors.Add(failState);
                    }
                }
            }

            return successors;
        }

        private bool CheckStats(CharacterNPC character, CharacterNPC enemyChar, ref ConflictState exploringState)
        {
            if (exploringState.enemyHP > enemyChar.hp)
            {
                return false;
            }
            else if (exploringState.enemyHP < 0)
            {
                exploringState.enemyHP = 0;
            }

            return CheckStats(character, ref exploringState);
        }

        private bool CheckStats(CharacterNPC character, ref ConflictState exploringState)
        {
            if (exploringState.myMana < 0)
            {
                //not enaugh mana
                return false;
            }
            else if (exploringState.myMana > character.mana)
            {
                return false;
            }
            if (exploringState.enemyMana < 0)
            {
                exploringState.enemyMana = 0;
            }
            if (exploringState.enemyHP < 0)
            {
                exploringState.enemyHP = 0;
            }
            if (exploringState.myHP < 0)
            {
                exploringState.myHP = 0;
            }
            else if (exploringState.myHP > character.hp)
            {
                //heal over max hp
                return false;
            }
            //else if (exploringState.myHP - character.hp < 50
            //    && exploringState.myHP - character.hp > 0)
            //{
            //    exploringState.myHP = character.hp;
            //}
            return true;
        }

        #region IActionSelection Members

        public ConflictState GetNextAction(IActing myself, IActing enemy, Priorities priors, out Action selectedAction)
        {
            //TODO dodelat
            selectedAction = this.attackActions[0];
            return new ConflictState(myself.GetStatus(), enemy.GetStatus());
        }

        #endregion
    }
    public class DynamicProgramming : WiccanRede.AI.IActionSelection
    {
        List<Action> attackActions;
        List<Action> defenseActions;
        List<ConflictState> stateSpace;
        List<Action> bestActions;

        Dictionary<int, DPItem> space;

        const int iterationCount = 10;
        const int failProb = 10;
        Random rand;

        int stateSpaceDeep = 4;

        public DynamicProgramming()
        {
            attackActions = new List<Action>();
            defenseActions = new List<Action>();
            stateSpace = new List<ConflictState>();
            bestActions = new List<Action>();

            space = new Dictionary<int, DPItem>();

            rand = new Random();
            try
            {
                this.attackActions = LoadActions("Settings/AttackSpells.xml");
                this.defenseActions = LoadActions("Settings/DefenseSpells.xml");
            }
            catch (Exception ex)
            {
                Logging.Logger.AddWarning("Chyba pri nacitani konfigurace kouzel: " + ex.ToString());
                throw;
            }
        }

        private List<Action> LoadActions(string config)
        {
            List<Action> loadedActions = new List<Action>();
            XmlDataDocument doc = new XmlDataDocument();
            doc.Load(config);

            foreach (XmlNode node in doc.GetElementsByTagName("Action"))
            {
                Action action = new Action();
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name)
                    {
                        case "Name":
                            action.name = child.InnerText;
                            break;
                        case "EnemyHpTaken":
                            action.enemyHpTaken = Convert.ToInt32(child.InnerText);
                            break;
                        case "EnemyManaDrain":
                            action.enemyManaDrain = Convert.ToInt32(child.InnerText);
                            break;
                        case "ManaDrain":
                            action.manaDrain = Convert.ToInt32(child.InnerText);
                            break;
                        case "HpGot":
                            action.hpGot = Convert.ToInt32(child.InnerText);
                            break;
                        case "MyShift":
                            action.myShift = Convert.ToInt32(child.InnerText);
                            break;
                        case "EnemyShift":
                            action.enemyShift = Convert.ToInt32(child.InnerText);
                            break;
                        case "EnemySpeedReduce":
                            action.enemySpeedReduce = Convert.ToSingle(child.InnerText);
                            break;
                        case "EnemyEnergyDrain":
                            action.enemyEnergyDrain = Convert.ToInt32(child.InnerText);
                            break;
                        case "MyEnergyDrain":
                            action.myEnergyDrain = Convert.ToInt32(child.InnerText);
                            break;
                        case "Probability":
                            action.probability = Convert.ToInt32(child.InnerText);
                            break;
                        case "Time":
                            action.time = Convert.ToInt32(child.InnerText);
                            break;
                        default:
                            Logging.Logger.AddWarning("Neznamy paramter pri nacitani xml kongigurace kouzel");
                            break;
                    }
                }
                loadedActions.Add(action);
            }
            return loadedActions;
        }

        public void CreateStateSpace(ConflictState currentState, CharacterNPC character, CharacterNPC enemyChar, ActionType actinType)
        {
            Logging.Logger.StartTimer("Vytvareni statespace");
            int step = 0;
            Recurse(currentState, character, enemyChar, actinType, step);
            Logging.Logger.StopTimer("Vytvareni statespace");
            Logger.AddInfo("Velikost State space: " + this.stateSpace.Count.ToString());
            Logger.AddInfo("Velikost space: " + this.space.Count.ToString());
            this.bestActions.Capacity = this.stateSpace.Count;
        }

        private void Recurse(ConflictState state, CharacterNPC character, CharacterNPC enemyChar, ActionType actinType, int step)
        {
            List<ConflictState> nextStates = GetSuccessors(state, character, enemyChar, actinType);
            DPItem item = new DPItem();
            item.Probability = state.probability;
            item.State = state;
            item.Children = nextStates;
            if (!space.ContainsKey(item.GetHashCode()))
            {
                this.space.Add(item.GetHashCode(), item);
            }

            step++;
            if (step < this.stateSpaceDeep)
            {
                ConflictState successor;
                for (int i = 0; i < nextStates.Count; i++)
                {
                    successor = nextStates[i];

                    if (this.stateSpace.Contains(successor))
                    {
                        //Logger.AddInfo("DP: nalezena duplicita. " + successor.ToString());
                        continue;
                    }
                    if (successor.enemyHP <= 0)
                    {
                        Logger.AddInfo("DP:Nalezen cilovy stav " + successor.ToString());
                        successor.cost = 0;
                        //step = this.stateSpaceDeep;
                        nextStates[i] = successor;
                        break;
                    }
                    else if (successor.myHP < character.hp / 4)
                    {
                        successor.cost = 100;
                        nextStates[i] = successor;
                    }
                    this.stateSpace.Add(successor);
                    //try
                    //{
                    //    if (!space.ContainsKey(successor.GetHashCode()))
                    //    {
                    //        space.Add(successor.GetHashCode(), successor); 
                    //    }
                    //    else
                    //    {
                    //        //Logger.AddWarning("DP: chyba pri pridavani do hash tabulky..." + successor.ToString());
                    //    }
                    //}
                    //catch (Exception)
                    //{
                    //    Logger.AddWarning("DP: chyba pri pridavani do hash tabulky..." + successor.ToString());
                    //}
                    Recurse(successor, character, enemyChar, actinType, step);
                }
                //this.stateSpace.AddRange(nextStates);
            }
        }

        public void Iterate(ConflictState state)
        {
            int h = 10;
            DPItem item = new DPItem();

            if (!space.TryGetValue(state.GetHashCode(), out item))
            {
                return;
            }

            for (int i = 0; i < iterationCount; i++)
            {
                foreach (KeyValuePair<int, DPItem> pair in space)
                {
                    //int min = int.MaxValue;
                    h = pair.Value.State.cost;
                    ConflictState current = pair.Value.State;
                    foreach (ConflictState s in pair.Value.Children)
                    {
                        h += s.probability * s.cost;
                        //if (state.cost < min)
                        //{
                        //    min = state.cost;
                        //}
                    }
                    pair.Value.State = current;
                }
            }
        }

        private List<ConflictState> GetSuccessors(ConflictState state, CharacterNPC character, CharacterNPC enemyChar, ActionType actionType)
        {
            List<ConflictState> successors = new List<ConflictState>();
            List<Action> actions = null;
            switch (actionType)
            {
                case ActionType.Attack:
                    actions = this.attackActions;
                    break;
                case ActionType.Defense:
                    actions = this.defenseActions;
                    break;
                default:
                    Logging.Logger.AddWarning("DP: Spatny typ akce");
                    return successors;
                //break;
            }

            ConflictState succesState = new ConflictState();
            //ConflictState noEffectState = new ConflictState();
            ConflictState failState = new ConflictState();

            ConflictState enemySuccesState = new ConflictState();
            ConflictState enemyFailState = new ConflictState();

            foreach (Action action in actions)
            {
                if (action.probability == 100)
                {
                    succesState = state + action;
                    successors.Add(succesState);
                }
                else
                {
                    //spell is casted by "this" npc
                    succesState = state + action;               //spell casted succesfull
                    succesState.probability = action.probability;
                    //noEffectState = state;                      //casting has no effect,
                    failState = state;                           //casting fail and harm caster
                    failState.myHP -= action.enemyHpTaken;
                    failState.myHP -= action.hpGot;
                    failState.probability = failProb;

                    //spell is casted by this NPCs enemy
                    enemySuccesState = state - action;
                    enemySuccesState.probability = action.probability;
                    enemyFailState = state;
                    enemyFailState.enemyHP -= action.enemyHpTaken;
                    enemyFailState.enemyHP -= action.hpGot;
                    enemyFailState.probability = failProb;

                    //check if states are valid
                    if (CheckStats(character, enemyChar, ref enemyFailState))
                    {
                        successors.Add(enemyFailState); 
                    }
                    if (CheckStats(character, enemyChar, ref enemySuccesState))
                    {
                        successors.Add(enemySuccesState); 
                    }
                    if (CheckStats(character, enemyChar, ref succesState))
                    {
                        successors.Add(succesState);
                    }
                    if (CheckStats(character, enemyChar, ref failState))
                    {
                        successors.Add(failState);
                    }
                }                
            }
            
            return successors;
        }

        private bool CheckStats(CharacterNPC character, CharacterNPC enemyChar, ref ConflictState exploringState)
        {
            if (exploringState.enemyHP > enemyChar.hp)
            {
                return false;
            }
            else if (exploringState.enemyHP < 0)
            {
                exploringState.enemyHP = 0;
            }

            return CheckStats(character, ref exploringState);
        }

        private bool CheckStats(CharacterNPC character, ref ConflictState exploringState)
        {
            if (exploringState.myMana < 0)
            {
                //not enaugh mana
                return false;
            }
            else if (exploringState.myMana > character.mana)
            {
                return false;
            }
            if (exploringState.enemyMana < 0)
            {
                exploringState.enemyMana = 0;
            }
            if (exploringState.enemyHP < 0)
            {
                exploringState.enemyHP = 0;
            }
            if (exploringState.myHP < 0)
            {
                exploringState.myHP = 0;
            }
            else if (exploringState.myHP > character.hp)
            {
                //heal over max hp
                return false;
            }
            //else if (exploringState.myHP - character.hp < 50
            //    && exploringState.myHP - character.hp > 0)
            //{
            //    exploringState.myHP = character.hp;
            //}
            return true;
        }

        #region IActionSelection Members

        public ConflictState GetNextAction(IActing myself, IActing enemy, Priorities priors, out Action selectedAction)
        {
            //TODO dodelat
            selectedAction = this.attackActions[0];
            return new ConflictState(myself.GetStatus(), enemy.GetStatus());
        }

        #endregion
    }
}
