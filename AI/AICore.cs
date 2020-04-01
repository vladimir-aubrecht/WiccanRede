using System;
using System.Threading;
using Microsoft.DirectX;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;

namespace WiccanRede.AI
{
  public class AICore : IDisposable
  {
    List<NPC> npcs;
    List<Type> fsms;
    ControlNPC control;
    IWalkable terrain;
    GamePlayer player;
    Map map;

    float updateTime;
    DateTime last;

    private float step = 0.1f;

    Thread aiThread;
    public AICore(IWalkable terrain)
    {
      npcs = new List<NPC>();
      fsms = new List<Type>();
      this.terrain = terrain;
      control = new ControlNPC();

      this.last = DateTime.Now;

      map = Map.GetInstance(terrain);

      LoadFsmPlugins("Plug-ins\\");
    }

    private void LoadFsmPlugins(string path)
    {
      string wholePath = Directory.GetCurrentDirectory() + "\\" + path;
      string[] files;
      try
      {
        files = Directory.GetFiles(wholePath);
      }
      catch (Exception ex)
      {
        Logging.Logger.AddError("Nenalezen adresar s plug-inama! " + wholePath + "-" + ex.ToString());
        return;
      }

      foreach (string file in files)
      {
        if (!file.EndsWith(".dll"))
          continue;
        Assembly assembly = Assembly.LoadFrom(file);
        Type[] types = assembly.GetTypes();
        foreach (Type type in types)
        {
          try
          {
            if (type.BaseType.Equals(typeof(FSM)))
            {
              Logging.Logger.AddInfo("Nacten plug-in pro FSM: " + type.Name);
              this.fsms.Add(type);
            }
          }
          catch
          {
            //type has not a BaseType
          }
        }
      }
    }

    /// <summary>
    /// Gets the NPC map.
    /// </summary>
    /// <returns></returns>
    public static System.Drawing.Bitmap GetNpcMap()
    {
      return Map.GetInstance().GetBitmap();
    }

    #region threading
    public void StartOwnThread()
    {
      aiThread = new Thread(Start);
      aiThread.Name = "ai_thread";
      aiThread.Priority = ThreadPriority.Normal;
      aiThread.Start();
    }

    private void Start()
    {
      while (true)
      {
        Update();
        this.updateTime = (float)(DateTime.Now - this.last).TotalMilliseconds;
        this.last = DateTime.Now;
        //Logging.Logger.AddInfo("ai update, " + this.updateTime.ToString());
      }
    }


    public void StopAI()
    {
      if (this.aiThread != null)
      {
        this.aiThread.Abort();
      }
    }
    public void JoinAI()
    {
      this.aiThread.Join();
    }
    public void InterruptAI()
    {
      this.aiThread.Interrupt();
    }
    #endregion

    /// <summary>
    /// main update function, call other updates
    /// </summary>
    public void Update()
    {
      //if (player.Cooldown > 0)
      {
        player.Update(step);
      }
      control.Update(npcs, step);
    }

    /// <summary>
    /// update for story line
    /// </summary>
    /// <param name="gameState">name of new game state</param>
    public void Update(string gameState)
    {
      control.Update(npcs, step, gameState);

      CharacterNPC playerChar = this.player.GetCharacter();
      playerChar.level++;
      this.player.UpdateCharacter(playerChar);
    }

    /// <summary>
    /// Talks the request.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="gameState">State of the game.</param>
    public void TalkRequest(string sender, string gameState)
    {
      control.Update(this.npcs, sender, gameState);
    }

    /// <summary>
    /// register human player
    /// </summary>
    /// <param name="entity">object for graphical representation</param>
    /// <param name="character">player's character</param>
    public void AddPlayer(IControlable entity, CharacterNPC character)
    {
      this.player = new GamePlayer(entity, character);
    }

    /// <summary>
    /// register new ai controled player
    /// </summary>
    /// <param name="charakter">character of added npc</param>
    /// <param name="position">position where npc starts</param>
    /// <param name="entity">pointer to the graphical representation</param>
    /// <param name="fsmName">Name of finite state machine</param>
    /// <see cref="FSM"/>
    public void AddPlayer(CharacterNPC charakter, Vector3 position, IControlable entity, string controlMechanism, string actionDefinition)
    {
      NPC npc = new NPC(charakter, position, this.terrain, entity);
      npc.AttachActions(actionDefinition);

      if (controlMechanism.EndsWith(".lua"))
      {
        npc.AttachScript(controlMechanism);
        npcs.Add(npc);
      }
      else
      {
        AddNpcWithFsm(charakter, entity, controlMechanism, npc);
      }
    }

    /// <summary>
    /// Adds the NPC with FSM.
    /// </summary>
    /// <param name="charakter">The charakter.</param>
    /// <param name="entity">The entity.</param>
    /// <param name="fsmName">Name of the FSM.</param>
    /// <param name="npc">The NPC.</param>
    private void AddNpcWithFsm(CharacterNPC charakter, IControlable entity, string fsmName, NPC npc)
    {
      FSM fsm = null;
      try
      {
        if (this.fsms != null && fsms.Count > 0)
        {
          Type type = fsms[0];
          foreach (Type t in fsms)
          {
            if (t.Name == fsmName)
            {
              type = t;
              break;
            }
          }
          //ConstructorInfo ci = type.GetConstructor(new Type[] { type });
          ConstructorInfo ci = type.GetConstructors()[0];
          List<Object> constructorPars = new List<object>();
          foreach (ParameterInfo pi in ci.GetParameters())
          {
            if (pi.ParameterType.Name == "NPC")
            {
              constructorPars.Add(npc);
            }
            else if (pi.ParameterType.Name == "CharacterNPC")
            {
              constructorPars.Add(charakter);
            }
            else if (pi.ParameterType.Name == "IControlable")
            {
              constructorPars.Add(entity);
            }
            else if (pi.ParameterType.Name == "Map")
            {
              constructorPars.Add(this.map);
            }
          }
          fsm = (FSM)Activator.CreateInstance(type, constructorPars.ToArray());
          //FSM fsm = (FSM)ci.Invoke(constructorPars.ToArray());
        }

      }
      catch (Exception ex)
      {
        Logging.Logger.AddError("Chyba pri nacitani FSM: " + ex.ToString());
      }

      if (fsm != null)
      {
        //Logging.Logger.AddInfo("pridavam npc na pozici: " + position.ToString());
        npc.AttachFSM(fsm);
        //lock (this.npcs)
        {
          npcs.Add(npc);
        }
      }
      else
      {
        Logging.Logger.AddWarning("Nemohu pridat NPC " + charakter.name + ". Nenalezen zadny fsm");
      }
    }

    /// <summary>
    /// register new ai controled player with script
    /// </summary>
    /// <param name="charakter">character of NPC</param>
    /// <param name="position">starting position</param>
    /// <param name="entity">pointer to the graphical representation</param>
    /// <param name="scriptPath">path to the script</param>
    public void AddPlayer(CharacterNPC charakter, Vector3 position, IControlable entity, string scriptPath)
    {
      NPC npc = new NPC(charakter, position, this.terrain, entity);
      npc.AttachScript(scriptPath);
    }

    /// <summary>
    /// take action from game - this mean for example damage for NPC from player
    /// </summary>
    /// <param name="npcName">NPC which is affected</param>
    /// <param name="info">info about action</param>
    public void AcceptAction(string npcName, ActionInfo info)
    {
      if (npcName == "Player")
      {
        this.player.SufferDamage(info.action.enemyHpTaken);
        this.player.DrainMana(info.action.enemyManaDrain);
        return;
      }
      foreach (NPC npc in this.npcs)
      {
        if (npc.character.name == npcName)
        {
          npc.SufferDamage(info.action.enemyHpTaken);
          npc.DrainMana(info.action.enemyManaDrain);
          if (npc.TargetedEnemy == null)
          {
            npc.RegisterAttack(this.player);
          }
          break;
        }
      }
    }

    /// <summary>
    /// method called after human player did some action, updates player's status
    /// </summary>
    /// <param name="info">info about player's action</param>
    /// <returns>whether player can do that</returns>
    public bool AcceptPlayerAction(ActionInfo info)
    {
      //if action takes mana then check if this is possible
      if (info.action.manaDrain > 0 && this.player.GetStatus().mana < info.action.manaDrain)
        return false;
      else if (this.player.Cooldown > 0)
        return false;

      this.player.DrainMana(info.action.manaDrain);
      this.player.Heal(info.action.hpGot);
      this.player.SetCoolDown(info.action.time);
      return true;
    }

    /// <summary>
    /// Determines whether [is player stuned].
    /// </summary>
    /// <returns>
    /// 	<c>true</c> if [is player stuned]; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPlayerStuned()
    {
      return this.player.IsStuned;
    }

    /// <summary>
    /// get character information about player
    /// </summary>
    /// <returns>character information about player such as level, power, etc</returns>
    public NpcInfo GetPlayerInfo()
    {
      return new NpcInfo(this.player);
    }

    /// <summary>
    /// gets all base info about all NPCs
    /// </summary>
    /// <remarks>can be used to monitor npc's status</remarks>
    /// <returns>list of infos</returns>
    public List<NpcInfo> GetNpcInfo()
    {
      List<NpcInfo> infos = new List<NpcInfo>();
      NpcInfo[] npcInfos = new NpcInfo[this.npcs.Count];

      for (int i = 0; i < npcs.Count; i++)
      {
        npcInfos[i] = new NpcInfo(npcs[i]);
      }
      infos.AddRange(npcInfos);

      //add player info
      NpcInfo playerInfo = new NpcInfo(this.player);
      infos.Add(playerInfo);

      return infos;
    }

    /// <summary>
    /// gets and sets the time step in seconds
    /// </summary>
    public float Step
    {
      get { return step; }
      set { step = value; }
    }

    /// <summary>
    /// gets the terrain
    /// </summary>
    public IWalkable Terrain
    {
      get
      {
        return this.terrain;
      }
    }

    #region IDisposable Members

    public void Dispose()
    {
      if (this.aiThread != null && this.aiThread.IsAlive)
      {
        aiThread.Abort();
        aiThread = null;
      }
      this.npcs = null;
    }

    #endregion
  }


  /// <summary>
  /// struct describing selected action, from who and at who. its given into to game logic
  /// </summary>
  [System.Diagnostics.DebuggerDisplay("{npcName} dela {action.name} proti {targetName}")]
  public struct ActionInfo
  {
    public Vector3 targetPosition;
    public Vector3 startPosition;
    public Action action;
    public string targetName;
    public string npcName;

    public override string ToString()
    {
      return action + " @ " + targetName + "pos=\n" + targetPosition.ToString();
    }

  }

  /// <summary>
  /// struct describing selected action, from who and at who. its given into to game logic
  /// </summary>
  [System.Diagnostics.DebuggerDisplay("{npcName} dela {action.name} proti {targetName}")]
  public struct CopyOfActionInfo
  {
    public Vector3 targetPosition;
    public Vector3 startPosition;
    public Action action;
    public string targetName;
    public string npcName;

    public override string ToString()
    {
      return action + " @ " + targetName + "pos=\n" + targetPosition.ToString();
    }

  }

  /// <summary>
  /// class for giving info outside the library
  /// </summary>
  [System.Diagnostics.DebuggerDisplay("{character.name} hp={status.hp} pos={status.position}")]
  public class NpcInfo
  {
    /// <summary>
    /// ctor, create info about NPC
    /// </summary>
    /// <param name="npc"></param>
    public NpcInfo(IActing npc)
    {
      this.status = npc.GetStatus();
      this.character = npc.GetCharacter();
    }
    Status status;
    /// <summary>
    /// status of NPC, base stats as hp, mana, enemies, etc
    /// </summary>
    public Status Status
    {
      get
      {
        return status;
      }
    }
    CharacterNPC character;
    /// <summary>
    /// character of npc, contains base starting info, such as max hp, mana, level, etc
    /// </summary>
    public CharacterNPC Character
    {
      get
      {
        return character;
      }
    }

    public override string ToString()
    {
      string str = this.character.name + " @ " + this.status.position + " ma " + this.status.hp;
      return str;
    }
  }
}
