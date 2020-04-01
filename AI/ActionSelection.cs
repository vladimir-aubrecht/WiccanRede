using System;
using System.Xml;
using System.Data;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using Logging;

namespace WiccanRede.AI
{
  [System.Diagnostics.DebuggerDisplay("ConflictState: myHP={myHP}, enemyHP={enemyHP}, cost={cost}")]
  public struct ConflictState
  {
    public int myHP, myMana, myEnergy;
    public int enemyHP, enemyMana, enemyEnergy;
    public int mySpeed, enemySpeed;
    public Point myPosition, enemyPosition;

    public int cost, time, probability;

    public ConflictState(Status myStatus, Status enemyStatus)
    {
      this.myEnergy = (int)myStatus.energy;
      this.myHP = (int)myStatus.hp;
      this.myMana = (int)myStatus.mana;
      this.myPosition = myStatus.position;
      this.mySpeed = 1;
      this.enemySpeed = 1;
      this.enemyEnergy = (int)enemyStatus.energy;
      this.enemyHP = (int)enemyStatus.hp;
      this.enemyMana = (int)enemyStatus.mana;
      this.enemyPosition = enemyStatus.position;

      this.cost = 10;
      this.time = 1000;
      this.probability = 100;
    }

    public override int GetHashCode()
    {
      int hash = myHP * myMana * myEnergy;
      hash *= (enemyHP + enemyMana + enemyEnergy);
      return hash;
      //return base.GetHashCode();
    }

    public static ConflictState operator +(ConflictState state, Action action)
    {
      ConflictState newState = new ConflictState();
      newState.cost = state.cost;
      newState.enemyEnergy = state.enemyEnergy;
      newState.enemyHP = state.enemyHP - action.enemyHpTaken;
      newState.enemyMana = state.enemyMana - action.enemyManaDrain;
      newState.enemyPosition = state.enemyPosition; //TODO dodelat posouvani npccek pri zasahu
      newState.enemySpeed = state.enemySpeed - 1 * (int)action.enemySpeedReduce;
      newState.myEnergy = state.myEnergy - action.myEnergyDrain;
      newState.myHP = state.myHP + action.hpGot;
      newState.myMana = state.myMana - action.manaDrain;
      newState.myPosition = state.myPosition;
      newState.mySpeed = state.mySpeed;

      return newState;
    }

    public static ConflictState operator -(ConflictState state, Action action)
    {
      //enemy is casting
      ConflictState newState = new ConflictState();
      newState.cost = state.cost;
      newState.enemyEnergy = state.enemyEnergy;
      newState.enemyHP = state.enemyHP + action.hpGot;
      newState.enemyMana = state.enemyMana - action.manaDrain;
      newState.enemyPosition = state.enemyPosition; //TODO dodelat posouvani npccek pri zasahu
      newState.enemySpeed = state.enemySpeed;
      newState.myEnergy = state.myEnergy;
      newState.myHP = state.myHP - action.enemyHpTaken;
      newState.myMana = state.myMana - action.enemyManaDrain;
      newState.myPosition = state.myPosition;
      newState.mySpeed = state.mySpeed;

      return newState;
    }


    //spell failed and its effects fail on caster
    //public static ConflictState operator -(ConflictState state, Action action)
    //{
    //    ConflictState newState = new ConflictState();
    //    newState.cost = state.cost;
    //    newState.enemyEnergy = state.enemyEnergy;
    //    newState.enemyHP = state.enemyHP;
    //    newState.enemyMana = state.enemyMana;
    //    newState.enemyPosition = state.enemyPosition; //TODO dodelat posouvani npccek pri zasahu
    //    newState.enemySpeed = state.enemySpeed;
    //    newState.myEnergy = state.myEnergy - action.myEnergyDrain;
    //    newState.myHP = state.myHP - action.hpGot;
    //    newState.myHP = state.myHP - action.enemyHpTaken;
    //    newState.myMana = state.myMana - action.manaDrain;
    //    newState.myPosition = state.myPosition;
    //    newState.mySpeed = state.mySpeed;

    //    return newState;
    //}

    public override string ToString()
    {
      return "ConflictState: NPC: hp=" + myHP + " mana=" + myMana +
          " Enemy: hp=" + enemyHP + " mana=" + enemyMana;
    }
  }
  [System.Diagnostics.DebuggerDisplay("Akcion {name}")]
  public struct Action
  {
    /// <summary>
    /// action name
    /// </summary>
    public string name;
    public int enemyHpTaken, enemyManaDrain;
    public int manaDrain, hpGot;
    public int myShift, enemyShift;
    public float enemySpeedReduce;
    public int myEnergyDrain, enemyEnergyDrain;

    /// <summary>
    /// succes propability
    /// </summary>
    public int probability;
    public int range;

    /// <summary>
    /// time spent in action in miliseconds
    /// </summary>
    public int time;

    public override string ToString()
    {
      return "Action: " + name + " - prob = " + probability.ToString();
    }
  }


  [System.Diagnostics.DebuggerDisplay("StateProbPair: states:{states.Count} action={action.ToString()}")]
  struct StateProbPairs
  {
    public List<ConflictState> states;
    public List<int> probabilities;
    public Action action;

    public override string ToString()
    {
      string str = "StateProbPairs: " + action.ToString() + " stavy: " + states.Count;
      for (int i = 0; i < states.Count; i++)
      {
        str += "stav: " + states[i].ToString();
        str += "\n <br />";
      }
      return str;
    }
  }

  [System.Diagnostics.DebuggerDisplay("StateProbPair: states:{states.Count} action={action.ToString()}")]
  struct CopyOfStateProbPairs
  {
    public List<ConflictState> states;
    public List<int> probabilities;
    public Action action;

    public override string ToString()
    {
      string str = "CopyOfStateProbPairs: " + action.ToString() + " stavy: " + states.Count;
      for (int i = 0; i < states.Count; i++)
      {
        str += "stav: " + states[i].ToString();
        str += "\n <br />";
      }
      return str;
    }
  }

  /// <summary>
  /// class for selecting next action in combat situations
  /// </summary>
  public class ActionSelection : IActionSelection
  {

    //List<Action> attackActions;
    //List<Action> defenseActions;
    List<Action> actions;
    const int failProb = 10;
    Random rand;

    Action lastAction;

    /// <summary>
    /// ctor, loads actions from xml files
    /// </summary>
    public ActionSelection(string actionDefinitionPath)
    {
      rand = new Random();
      //attackActions = new List<Action>();
      //defenseActions = new List<Action>();
      actions = new List<Action>();

      try
      {
        //this.attackActions = LoadActions("Settings/AttackSpells.xml");
        //this.defenseActions = LoadActions("Settings/DefenseSpells.xml");
        this.actions = LoadActions(actionDefinitionPath);
      }
      catch (Exception ex)
      {
        Logging.Logger.AddWarning("Chyba pri nacitani konfigurace kouzel: " + ex.ToString());
        throw;
      }
    }

    /// <summary>
    /// loads action from xml file
    /// </summary>
    /// <param name="config">path to xml file</param>
    /// <returns>List of actions loaded from xml file</returns>
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
            case "Range":
              action.range = Convert.ToInt32(child.InnerText);
              break;
            default:
              Logging.Logger.AddWarning(child.Name + "- Neznamy paramter pri nacitani xml kongigurace kouzel");
              break;
          }
        }
        loadedActions.Add(action);
      }
      return loadedActions;
    }

    public List<Action> GetActions()
    {
      return this.actions;
    }

    /// <summary>
    /// apply actions on conflict state and by this generate followers of current confict state
    /// </summary>
    /// <param name="state"> current conflict state </param>
    /// <param name="character">character of npc, used for checking stats</param>
    /// <param name="actionType">action type, which says, what actions are available</param>
    /// <returns>List of state-probability pairs</returns>
    private List<StateProbPairs> Expand(ConflictState state, CharacterNPC character)
    {
      List<StateProbPairs> expanders = new List<StateProbPairs>();
      //List<Action> actions = null;
      //switch (actionType)
      //{
      //    case ActionType.Attack:
      //        actions = this.attackActions;
      //        break;
      //    case ActionType.Defense:
      //        actions = this.defenseActions;
      //        break;
      //    default:
      //        Logging.Logger.AddWarning("Spatny typ akce");
      //        return expanders;
      //    //break;
      //}

      foreach (Action action in this.actions)
      {
        if (action.name == lastAction.name && action.name.StartsWith("Heal"))
          break;
        //TODO - spravit cooldownama

        StateProbPairs pairs = new StateProbPairs();
        pairs.states = new List<ConflictState>();
        pairs.probabilities = new List<int>();

        ConflictState succesState;// = new ConflictState();
        ConflictState noEffectState;// = new ConflictState();
        ConflictState failState;// = new ConflictState();

        succesState = state + action;               //spell casted succesfull
        noEffectState = state;                      //casting has no effect,
        failState = state;// -action;                 //casting fail and harm caster
        failState.myHP -= action.enemyHpTaken;
        failState.myHP -= action.hpGot;


        int prob = action.probability;
        int noneProb = 100 - prob - failProb;
        int fail = failProb;

        //check and add to expanders
        if (CheckStats(character, ref succesState))
        {
          pairs.states.Add(succesState);
          pairs.probabilities.Add(prob);

          if (action.probability != 100)
          {
            if (CheckStats(character, ref failState))
            {
              pairs.states.Add(failState);
              pairs.probabilities.Add(fail);
            }
            //pairs.states.Add(noEffectState); 
            //pairs.probabilities.Add(noneProb);
          }
        }


        pairs.action = action;
        if (pairs.states.Count > 0)
        {
          expanders.Add(pairs);
        }
      }

      return expanders;
    }

    /// <summary>
    /// check stats of confict state, for example max values of mana, etc.
    /// </summary>
    /// <param name="character">character, which says all attributes to compare</param>
    /// <param name="exploringState">confict state to check</param>
    /// <returns>true if given state is valid one, else false</returns>
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

    /// <summary>
    /// from generated state-prob pairs choose the best one and returns new Confict state
    /// </summary>
    /// <param name="myself">npc, which is choosing from actions</param>
    /// <param name="enemy">enemy npc on which is the target</param>
    /// <param name="priors">priors from npc character, useless for now</param>
    /// <param name="actionType">type of action</param>
    /// <returns>new confict state</returns>
    public ConflictState GetNextAction(IActing myself, IActing enemy, Priorities priors, out Action selectedAction)
    {
      ConflictState currentState = new ConflictState(myself.GetStatus(), enemy.GetStatus());
      Priorities priorities = priors;
      CharacterNPC myChar = myself.GetCharacter();

      //if (this.stateSpace.Count == 0)
      //{
      //    CreateStateSpace(currentState, myChar); 
      //}

      List<StateProbPairs> expanders = Expand(currentState, myChar);

      StateProbPairs result = new StateProbPairs();

      int lowestCost = int.MaxValue;
      int actionCost = int.MaxValue;

      foreach (StateProbPairs pairs in expanders)
      {
        //if (this.stateSpace.Contains(pairs))
        //{
        //    Logging.Logger.AddInfo(pairs.ToString() + " je obsazeno");
        //}
        actionCost = ComputeCost(currentState, pairs, myChar);

        if (actionCost < lowestCost && actionCost >= 0)
        {
          result = pairs;
          lowestCost = actionCost;
        }
      }
      if (result.states == null)
      {
        result.states.Add(currentState);
        Action empty = new Action();
        empty.name = "empty";
        empty.time = 5000;
        result.action = empty;
        result.probabilities.Add(100);
      }

      int index = 0;
      if (result.probabilities.Count > 1)
      {
        int randChoose = rand.Next(100);
        if (randChoose < result.probabilities[0])
        {
          index = 0;
        }
        else if (randChoose < result.probabilities[0] + result.probabilities[1])
        {
          index = 1;
        }
        else
        {
          if (result.states.Count > 2)
          {
            index = 2;
          }
        }
      }

      if (index >= result.states.Count)
        index = 0;

      Logging.Logger.AddInfo(myChar.name + ": " + result.action.ToString() + " proti " + enemy.GetCharacter().name + ", cost=" + lowestCost + ", efekt: " + index);
      myself.SetCoolDown(result.action.time);

      selectedAction = result.action;
      this.lastAction = selectedAction;
      //return result.states[index];
      return result.states[0];
    }

    /// <summary>
    /// evaluate given state-prob pair 
    /// </summary>
    /// <param name="current">current confict state, without any applied actions</param>
    /// <param name="pairs">pairs to evaluate</param>
    /// <param name="character">npc character</param>
    /// <returns>calculated cost of given pairs</returns>
    private int ComputeCost(ConflictState current, StateProbPairs pairs, CharacterNPC character)
    {
      int cost = 0;
      ConflictState state;
      for (int i = 0; i < pairs.states.Count; i++)
      {
        state = pairs.states[i];
        if (state.myHP > character.hp)
        {
          state.myHP = character.hp;
        }
        if (state.myMana > character.mana)
        {
          state.myMana = character.mana;
        }

        int myHpRating = 0, enemyHpRating = 0, boostRating = 0, manaRating = 0;

        if (state.myHP > current.myHP)
        {
          myHpRating = current.myHP;
        }
        else if (state.myHP < current.myHP)
        {
          myHpRating = current.myHP - state.myHP;  //self harm
        }

        if (state.myMana - current.myMana >= 80)
        {
          manaRating = current.myMana;
        }
        else if (state.myMana < current.myMana)
        {
          manaRating = state.myMana - current.myMana;
        }

        if (state.enemyHP < current.enemyHP)
        {
          enemyHpRating = state.enemyHP;
        }
        boostRating = 0;// character.mana - state.myMana;

        int energyRating = (current.myEnergy - state.myEnergy) * 10;
        int timeRating = pairs.action.time / 10;

        int sum = myHpRating + enemyHpRating + boostRating + energyRating + manaRating + timeRating;
        sum *= (pairs.probabilities[i]);
        sum /= 100;

        cost += sum;
      }
      cost /= pairs.states.Count;
      return cost;
    }

  }
}
