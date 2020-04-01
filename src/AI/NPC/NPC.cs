using System;
using Microsoft.DirectX;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Logging;
using System.Diagnostics;
using WiccanRede.AI.Scripting;

namespace WiccanRede.AI
{
  /// <summary>
  /// class representing NPC
  /// </summary>
  [DebuggerDisplay("NPC {character.name}, type {character.type}")]
  public class NPC : IActing, IDisposable
  {
    #region Positioning
    private Vector3 position3D;
    public Vector3 Position3D
    {
      get { return position3D; }
      set { position3D = value; }
    }

    private int x, y;
    public System.Drawing.Point targetCell;

    private Vector3 direction;
    public Vector3 Direction
    {
      get { return direction; }
      set { direction = value; }
    }
    #endregion

    #region AI structures
    public CharacterNPC character;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Map aiMap;
    private MapCellInfo[,] npcMap;

    //private FSM fsm;
    //public FSM Fsm
    //{
    //    get { return fsm; }
    //}

    private IControlMechanism controlMechanism;

    internal IControlMechanism ControlMechanism
    {
      get { return controlMechanism; }
    }

    //private IFsmState state;
    //public IFsmState State
    //{
    //    get { return this.fsm.ActualState; }
    //}

    private Astar pathFinding;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal Astar PathFinding
    {
      get { return pathFinding; }
    }

    NpcTask currentTask;
    public NpcTask CurrentTask
    {
      get { return currentTask; }
    }

    //private ActionSelection defenseActionSelection;

    //internal ActionSelection DefenseActionSelection
    //{
    //    get { return defenseActionSelection; }
    //}

    //private ActionSelection offenseActionSelection;

    //internal ActionSelection OffenseActionSelection
    //{
    //    get { return offenseActionSelection; }
    //}

    private ActionSelection actionSelection;

    public ActionSelection ActionSelection
    {
      get { return actionSelection; }
    }
    #endregion

    #region World input
    private IWalkable terrain;
    public IWalkable Terrain
    {
      get { return terrain; }
    }

    private IControlable entity;
    public IControlable Entity
    {
      get { return entity; }
    }
    #endregion

    private int visualRange;

    private Traveling taskMove;

    public Traveling TaskMove
    {
      get { return taskMove; }
    }
    private Traveling fightMove;

    public Traveling FightMove
    {
      get { return fightMove; }
    }

    //for 3D moving
    private float stepProgress;
    private float step;

    Stack<NpcTask> taskStack;

    IActing targetedEnemy;
    Status currentStatus;
    int cooldown;
    int healCD;
    Point currentCheckpoint;

    const bool CanAttackPlayer = true;

    bool script;
    //Scripting.ScriptingLua luaScript;
    //Scripting.ScriptFunctions scriptFunctions;

    /// <summary>
    /// cooldown on which npc can't do anything
    /// </summary>
    public int Cooldown
    {
      get { return cooldown; }
    }

    /// <summary>
    /// enemy on which is this npc targeted, may be null, if there isn't any npc in sight
    /// </summary>
    public IActing TargetedEnemy
    {
      get { return targetedEnemy; }
      set { targetedEnemy = value; }
    }

    /// <summary>
    /// ctor, initialize ai structures
    /// </summary>
    /// <param name="character">character of npc</param>
    /// <param name="position">position on which npc stands</param>
    /// <param name="terrain">terrain, where npc moves</param>
    /// <param name="entity">pointer on graphical representation of npc, used for changing position, making effects, ...</param>
    public NPC(CharacterNPC character, Vector3 position, IWalkable terrain, IControlable entity)
    {
      this.position3D = position;
      System.Drawing.Point xy = terrain.Get2DMapPosition(position);
      this.x = xy.X;
      this.y = xy.Y;
      this.character = character;

      this.visualRange = this.character.visualRange;

      this.currentStatus = new Status();
      this.currentStatus.hp = character.hp;
      this.currentStatus.mana = character.mana;
      this.currentStatus.energy = 100;
      this.currentStatus.position = this.GetPosition2D();
      this.currentStatus.enemySeen = 0;
      this.currentStatus.alive = true;
      this.currentStatus.nearEnemies = new List<IActing>();

      this.entity = entity;
      this.terrain = terrain;
      this.taskStack = new Stack<NpcTask>();

      this.taskMove = new Traveling(new Astar());
      this.fightMove = new Traveling(new Astar());

      this.targetCell = new System.Drawing.Point(x, y);
      this.aiMap = Map.GetInstance();

      pathFinding = new Astar();

      this.entity.ChangePosition(position);
    }


    /// <summary>
    /// will attach finite state machine loaded as plug-in
    /// </summary>
    /// <remarks>this should be called after ctor, and has to be called before first Update function</remarks>
    /// <param name="fsm">Finite state machine to attach</param>
    public void AttachFSM(FSM fsm)
    {
      //this.fsm = fsm;
      //this.state = fsm.ActualState;
      this.controlMechanism = fsm;
    }

    public void AttachActions(string actionDefinition)
    {
      this.actionSelection = new ActionSelection(actionDefinition);
    }

    public void AttachScript(string scriptPath)
    {
      this.script = true;

      Scripting.ScriptingLua luaScript;
      Scripting.ScriptFunctions scriptFunctions;

      luaScript = new WiccanRede.AI.Scripting.ScriptingLua();
      scriptFunctions = new ScriptFunctions(this);
      luaScript.RegisterFunctions(scriptFunctions);
      Logger.AddImportant("Nacitani a spousteni lua skriptu " + scriptPath);
      luaScript.LoadScript(scriptPath);

      luaScript.RunFunction("Init", new object[] { });
      this.controlMechanism = luaScript;
    }

    /// <summary>
    /// Updates NPCs cooldown and restore stats
    /// </summary>
    /// <param name="step">duration from last update in miliseconds, typicaly little part of second</param>
    public void UpdateTime(float step)
    {
      if (this.cooldown > 0)
      {
        this.cooldown -= (int)(step * 1000);
      }
      if (cooldown <= 0)
      {
        cooldown = 0;
      }

      if (this.currentTask.time > 0)
      {
        this.currentTask.time -= step;
      }
      if (this.currentTask.time < 0 && this.currentTask.checkpoints.Count > 0)
      {
        this.currentTask.time = 0;
        this.currentTask.result = Result.fail;
        Logger.AddImportant(character.name + " nestihl splnit ukol");
      }

      if (this.fightMove.isOnWay)
      {
        if (this.fightMove.progress < this.fightMove.path.Count)
        {
          this.targetCell.X = this.fightMove.path[this.fightMove.progress].X;
          this.targetCell.Y = this.fightMove.path[this.fightMove.progress].Y;

        }
        else
        {
          this.fightMove.isOnWay = false;
          this.entity.StopMove();
        }
        Move(this.step);
      }

    }

    /// <summary>
    /// update NPC's map, check if is anybody in visual range
    /// </summary>
    /// <param name="npcs">list of all npcs</param>
    /// <param name="step">duration from last update in miliseconds, typicaly little part of second</param>
    public void UpdateNPC(List<NPC> npcs, float step)
    {
      if (!this.currentStatus.alive || this.currentStatus.hp <= 0)
      {
        return;
      }

      UpdateTime(step);
      UpdateTask();

      #region check npcs and player

      int seenEnemyCount = 0;
      int nearest = this.visualRange + 1000;
      int distance;
      this.currentStatus.nearEnemies.Clear();
      //check npcs
      if (npcs.Count > 1)     //if only one NPC -> this one
      {
        foreach (NPC npc in npcs)
        {
          if (this.character.type == NPCType.villager)
          {
            this.controlMechanism.Update();
            return;
          }
          else if (this.character.type == NPCType.guard)
          {
            //guards will atack only player
            break;
          }
          else if (this.Equals(npc))
          {
            continue;   //this npc
          }
          else if (npc.character.type == NPCType.villager || npc.character.type == NPCType.boss
              || npc.character.type == NPCType.guard)
          {
            //don't attack villagers and guards and boss
            continue;
          }
          else if (this.character.type == npc.character.type && npc.character.type != NPCType.enemy)
          {
            //not enemy
            continue;
          }
          distance = CalculateDistance(npc.GetPosition2D());

          if (distance < this.visualRange && CheckVisibility(npc))
          {
            seenEnemyCount++;
            this.currentStatus.nearEnemies.Add(npc);

            if (distance < nearest)     //TODO lip vybirat target
            {
              this.targetedEnemy = npc;
              nearest = distance;
            }
          }
        }
      }

      //check player
      if (CanAttackPlayer)
      {
        distance = CalculateDistance(this.terrain.GetPlayerPosition());
        if (distance < this.visualRange && CheckVisibility(GamePlayer.GetPlayer()))
        {
          IActing player = GamePlayer.GetPlayer();
          seenEnemyCount++;
          this.currentStatus.nearEnemies.Add(GamePlayer.GetPlayer());
          if (distance < nearest)
          {
            this.targetedEnemy = GamePlayer.GetPlayer();
            nearest = distance;
          }
        }
      }
      #endregion

      int targetingEnemyPower = 0;
      foreach (IActing npc in this.currentStatus.nearEnemies)
      {
        if (npc.GetTargetedEnemy() == (this as IActing))
        {
          targetingEnemyPower += ((int)npc.GetStatus().hp * npc.GetCharacter().level);
        }
      }

      //update status
      this.currentStatus.enemySeen = seenEnemyCount;
      this.currentStatus.enemyPower = targetingEnemyPower;
      this.currentStatus.position = this.GetPosition2D();
      this.currentStatus.cooldown = this.cooldown;


      this.controlMechanism.Update();

      this.step = step;
    }

    /// <summary>
    /// update taks of npc, if is done, if is new one available,...
    /// </summary>
    private void UpdateTask()
    {
      if ((this.currentTask.checkpoints == null || this.currentTask.checkpoints.Count == 0)
          && this.taskStack.Count > 0)     //no current task
      {
        this.currentTask = this.taskStack.Pop();
        Logging.Logger.AddImportant("menim currentTask pro npc: " + this.character.name +
           " na " + this.currentTask.ToString());
        //if (!this.script)
        //{
        //    this.fsm.React(AiEvents.newTask); 
        //}
        //TODO vyresti pridavani tasku
      }
      else if (this.currentTask.result == Result.fail)
      {
        Logger.AddImportant(character.name + " nesplnil cil");
        //this.Die();
      }
      else if (this.currentTask.result == Result.succes && this.taskStack.Count > 0)
      {
        Logger.AddInfo(character.name + " jde na dalsi cil");
        this.currentTask = this.taskStack.Pop();
        //if (!this.script)
        //{
        //    this.fsm.React(AiEvents.newTask); 
        //}
      }
      //else if (this.currentTask.checkpoints != null && this.currentTask.checkpoints.Contains(this.GetPosition2D()))
      //{
      //    this.currentTask.checkpoints.Remove(this.GetPosition2D());
      //}
      else if (this.currentTask.checkpoints != null && this.currentTask.checkpoints.Count == 0 && this.currentTask.result != Result.succes)
      {
        this.currentTask.result = Result.succes;
        Logger.AddImportant(this.character.name + " splnil cil");
      }
    }

    /// <summary>
    /// Check visibility from this npc to the given npc
    /// </summary>
    /// <param name="npc">other npc on which is testing visibility</param>
    /// <returns>true if npc is visible, otherwise false</returns>
    /// <seealso cref="Bresenham"/>
    private bool CheckVisibility(IActing npc)
    {
      List<Point> line = Helper.Bresenham(this.GetPosition2D(), npc.GetStatus().position);
      List<int> heights = new List<int>();

      int thisHeight = (int)Map.GetInstance().GetTerrain().Get3Dposition(this.GetPosition2D()).Y + 80;
      int npcHeight = (int)Map.GetInstance().GetTerrain().Get3Dposition(npc.GetStatus().position).Y + 80;

      foreach (Point p in line)
      {
        int height = (int)this.terrain.Get3Dposition(p).Y;
        if (Map.GetInstance().CellMap[p.X, p.Y].Block && !Map.GetInstance().CellMap[p.X, p.Y].Danger && !Map.GetInstance().CellMap[p.X, p.Y].Player)
        {
          height += 200;
        }
        heights.Add(height);
      }

      Vector3 npc1, npc2;
      npc2 = new Vector3(this.x, thisHeight, this.y);
      npc1 = new Vector3(npc.GetStatus().position.X, npcHeight, npc.GetStatus().position.Y);

      //Vector3 u = npc2 - npc1;
      //Vector3 n = Vector3.Cross(u, new Vector3(0,1,0));
      float localHeight;
      for (int i = 0; i < line.Count; i++)
      {
        localHeight = npc1.Y + ((line[i].X - npc1.X) * (npc2.Y - npc1.Y)) / (npc2.X - npc1.X);
        //localHeight2 = npc1.Y + ((line[i].Y - npc1.Z) * (npc2.Y - npc1.Y)) / (npc2.Z - npc1.Z);

        if (localHeight < heights[i])
        {
          //Logger.AddInfo(this.character.name +  ": Nepritel " + npc.character.name + " neni videt");
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// calculate distance from this npc location to the given point using Manhatan distance
    /// </summary>
    /// <param name="position">position of point where to calculate distance</param>
    /// <returns>distance</returns>
    private int CalculateDistance(Point position)
    {
      int xd = position.X - this.x;
      int yd = position.Y - this.y;

      int distance = Math.Max(Math.Abs(xd), Math.Abs(yd));
      return distance;
    }

    public int CalculateDistance(Point p0, Point p1)
    {
      int xd = p0.X - p1.X;
      int yd = p0.Y - p1.Y;

      int distance = Math.Max(Math.Abs(xd), Math.Abs(yd));
      return distance;
    }

    /// <summary>
    /// NPC is attacked by unseen attacker
    /// </summary>
    /// <param name="enemy"></param>
    public void RegisterAttack(IActing enemy)
    {
      this.TargetedEnemy = enemy;
    }

    /// <summary>
    /// check borders of map and calculate 3D position where to move and call Move3D(...)
    /// </summary>
    /// <param name="step">time step in seconds</param>
    private void Move(float step)
    {
      //check borders
      if (this.targetCell.X < 0)
      {
        this.targetCell.X = 0;
      }
      else if (this.targetCell.X >= this.aiMap.MapSize.X)
      {
        this.targetCell.X = this.aiMap.MapSize.X - 1;
      };
      if (this.targetCell.Y < 0)
      {
        this.targetCell.Y = 0;
      }
      else if (this.targetCell.Y >= this.aiMap.MapSize.Y)
      {
        this.targetCell.Y = this.aiMap.MapSize.Y - 1;
      }

      //calculate 3D position
      if (this.targetCell != new System.Drawing.Point(this.x, this.y))
      {
        Vector3 v = terrain.Get3Dposition(this.targetCell);
        Move3D(v, step);
      }
      else
      {
        if (this.fightMove.isOnWay)
        {
          this.fightMove.isOnWay = false;
          this.fightMove.progress = 0;
        }
      }

    }

    /// <summary>
    /// Move with npc in 3D, do linear interpolation, calls entitity.ChangePosition(...)
    /// </summary>
    /// <param name="targettarged 3D position</param>
    /// <param name="step">time step from last update from seconds</param>
    private void Move3D(Vector3 target, float step)
    {
      Vector3 startPosition = terrain.Get3Dposition(new System.Drawing.Point(this.x, this.y));
      //Vector3 startPosition = this.position3D;
      this.position3D = Helper.Interpolate3D(startPosition, target, this.stepProgress);
      //this.position3D.Y = 0;

      //call graphic shange
      entity.ChangePosition(this.position3D);
      this.direction = target - this.terrain.Get3Dposition(new System.Drawing.Point(this.x, this.y));

      entity.ChangeDirection(this.direction);
      //Logging.Logger.AddInfo(stepProgress.ToString());

      this.stepProgress += (step / 1f);
      //if (this.stepProgress > 1)
      //{
      //    this.stepProgress = 1;  //pripadne zaokrouhleni
      //}
      if (this.stepProgress >= 1)
      {
        this.x = targetCell.X;
        this.y = targetCell.Y;
        //this.taskPathProgress++;
        if (this.fightMove.isOnWay)
        {
          this.fightMove.progress++;
        }
        else
        {
          this.taskMove.progress++;
        }
        this.stepProgress = 0f;
        //this.position3D = target;
      }
    }

    /// <summary>
    /// linear interpolation between two vectors3
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    //private Vector3 Interpolate3D(Vector3 start, Vector3 end, float delta)
    //{
    //  Microsoft.DirectX.Vector3 result = new Microsoft.DirectX.Vector3();
    //  result.X = (1 - delta) * start.X + delta * end.X;
    //  result.Y = (1 - delta) * start.Y + delta * end.Y;
    //  result.Z = (1 - delta) * start.Z + delta * end.Z;
    //  return result;
    //}

    /// <summary>
    /// x coordinate of npc in map
    /// </summary>
    /// <returns>x coordinate</returns>
    public int GetMapX()
    {
      return this.x;
    }
    /// <summary>
    /// y coordinate of npc in map
    /// </summary>
    /// <returns>y coordinate</returns>
    public int GetMapY()
    {
      return this.y;
    }
    /// <summary>
    /// location of npc in the map
    /// </summary>
    /// <returns>location</returns>
    public Point GetPosition2D()
    {
      return new Point(x, y);
    }


    #region IActing Members

    /// <summary>
    /// adds the task into the queqe if tasks
    /// </summary>
    /// <param name="task">task to add</param>
    public void AddTask(NpcTask task)
    {
      this.taskStack.Push(task);
      Logging.Logger.AddInfo("pridavam ukol na zasobnik: " + task.ToString());
    }

    /// <summary>
    /// returns this npc's character
    /// </summary>
    /// <returns>Character of npc</returns>
    public CharacterNPC GetCharacter()
    {
      return this.character;
    }

    /// <summary>
    /// npc go to his task's checkpoints
    /// </summary>
    public void Go()
    {
      if (!this.taskMove.isOnWay)
      {
        if (this.currentTask.checkpoints != null && this.currentTask.checkpoints.Count > 0)
        {
          System.Drawing.Point goal = new Point();// = this.currentTask.checkpoints[0];
          int nearest = int.MaxValue;
          foreach (Point p in this.currentTask.checkpoints)
          {
            int goalDistance = CalculateDistance(p);
            if (goalDistance < nearest)
            {
              goal = p;
              this.currentCheckpoint = p;
              nearest = goalDistance;
            }

          }
          System.Drawing.Point pos = this.GetPosition2D();
          int distance = 0;
          //distance = (int)Math.Sqrt(pos.X * pos.X + pos.Y * pos.Y) - (int)Math.Sqrt(goal.X * goal.X + goal.Y * goal.Y);
          //distance = (int)Math.Abs(distance);
          distance = this.CalculateDistance(goal);

          if (distance < 2)
          {
            return;
          }
          //this.isOnTaskWay = true;
          //this.taskPathProgress = 0;
          this.taskMove.isOnWay = true;
          this.taskMove.progress = 0;
          Logger.AddInfo("NPC: " + this.character.name + ", " + this.character.type.ToString()
              + ". Cil je: " + goal.ToString() + ", vzdalenost je: " + distance.ToString());

          this.taskMove.pathFinding.Search((distance > 25), ref pos, ref goal);
          this.fightMove.isOnWay = false;
          this.entity.StartMove();
        }
      }
      else
      {
        if (this.taskMove.pathFinding.done)
        {
          //this.taskPath = this.pathFinding.Path;
          this.taskMove.path = this.taskMove.pathFinding.Path;
          if (this.taskMove.progress < this.taskMove.path.Count)
          {
            this.targetCell.X = this.taskMove.path[this.taskMove.progress].X;
            this.targetCell.Y = this.taskMove.path[this.taskMove.progress].Y;
          }
          else
          {
            this.taskMove.isOnWay = false;
            this.entity.StopMove();
            this.currentTask.checkpoints.Remove(this.currentCheckpoint);
          }
        }
      }
      Move(this.step);
    }

    /// <summary>
    /// npc explores random area
    /// </summary>
    public void Explore()
    {
      Random rand, rand2;

      if (!this.taskMove.isOnWay)
      {
        System.Drawing.Point goal;
        System.Drawing.Point pos = this.GetPosition2D();
        if (this.character.type == NPCType.villager)
        {
          rand = new Random(DateTime.Now.Millisecond.GetHashCode());
          rand2 = new Random(DateTime.Now.Millisecond);
          goal = new System.Drawing.Point((int)(rand.NextDouble() * Map.GetInstance().MapSize.X), (int)(rand2.NextDouble() * Map.GetInstance().MapSize.Y));
        }
        else
        {
          goal = new System.Drawing.Point();
          goal.X = this.Terrain.GetPlayerPosition().X + 1;
          goal.Y = this.Terrain.GetPlayerPosition().Y + 1;
        }

        int distance = 0;
        distance = (int)Math.Sqrt(pos.X * pos.X + pos.Y * pos.Y) - (int)Math.Sqrt(goal.X * goal.X + goal.Y * goal.Y);
        distance = (int)Math.Abs(distance);

        if (distance < 2)
        {
          return;
        }
        this.taskMove.isOnWay = true;
        this.taskMove.progress = 0;
        Logger.AddInfo("this: " + this.character.name + ", " + this.character.type.ToString()
            + ". Cil je: " + goal.ToString() + ", vzdalenost je: " + distance.ToString());
        this.taskMove.pathFinding.Search((distance > 15), ref pos, ref goal);
        //this.PathFinding.Search(startPos, goal);
      }
      else
      {
        if (this.taskMove.pathFinding.done)
        {
          if (this.taskMove.progress < this.taskMove.pathFinding.Path.Count)
          {
            this.targetCell.X = this.taskMove.pathFinding.Path[this.taskMove.progress].X;
            this.targetCell.Y = this.taskMove.pathFinding.Path[this.taskMove.progress].Y;
          }
          else
          {
            this.taskMove.isOnWay = false;
          }
        }
      }

    }

    /// <summary>
    /// gets status of current npc
    /// </summary>
    /// <returns>current status</returns>
    public Status GetStatus()
    {
      //return new Status(this.currentHP, this.currentMana, this.currentEnergy, this.alive, this.GetPosition2D(),0);
      return this.currentStatus;
    }

    /// <summary>
    /// sets status of npc
    /// </summary>
    /// <param name="status">new status to set</param>
    public void SetStatus(Status status)
    {
      if (status.hp <= 0)
      {
        status.alive = false;
        this.Die();
      }
      this.currentStatus = status;
      //TODO zapojit level postav do ovlivnovani damage
    }

    /// <summary>
    /// subtract the damage from current hit points, check if npc is death
    /// </summary>
    /// <param name="damage">damage in hit points</param>
    public void SufferDamage(int damage)
    {
      this.entity.GetHarm();
      //this.currentHP -= damage;
      int takenHP = damage - this.character.defense * this.character.level;

      if (takenHP < 0)
        takenHP = 10;

      this.currentStatus.hp -= takenHP;
      if (this.currentStatus.hp <= 0)
      {
        this.Die();
      }
    }

    /// <summary>
    /// subtract mana from current mana
    /// </summary>
    /// <param name="mana">drained mana</param>
    public void DrainMana(int mana)
    {
      //this.currentMana -= mana;
      this.currentStatus.mana -= mana;
      if (this.currentStatus.mana <= 0)
      {
        this.currentStatus.mana = 0;
      }
    }

    /// <summary>
    /// add hit points to current status hit points
    /// </summary>
    /// <param name="hp">hit points to add</param>
    public void Heal(int hp)
    {
      this.currentStatus.hp += hp;
      if (this.currentStatus.hp > this.character.hp)
        this.currentStatus.hp = this.character.hp;
    }

    /// <summary>
    /// set cooldown (after spellcasting for example), when npc can't do anything
    /// </summary>
    /// <param name="miliseconds">time in ms to set cooldown</param>
    public void SetCoolDown(int miliseconds)
    {
      this.cooldown = miliseconds;
    }

    /// <summary>
    /// gets entity, can call signals etc.
    /// </summary>
    /// <returns>IControlable entity</returns>
    public IControlable GetEntity()
    {
      return this.entity;
    }

    /// <summary>
    /// real 3D position, not 3D position of 2D map field
    /// </summary>
    /// <returns>3D position</returns>
    public Vector3 GetPosition3D()
    {
      Vector3 pos = this.position3D;
      //position of head
      pos.Y += 40;
      return pos;
    }

    public void DoFightMove(Point goal)
    {
      Point start = this.GetPosition2D();
      if ((!this.fightMove.isOnWay /*|| this.fightMove.path.Count < 2*/) && !goal.Equals(this.GetPosition2D()))
      {
        this.fightMove.isOnWay = true;
        this.fightMove.progress = 0;
        int dist = CalculateDistance(goal);
        if (dist > 4)
        {
          this.fightMove.pathFinding.Search(false, ref start, ref goal);
          this.fightMove.path = this.fightMove.pathFinding.Path;
        }
        else if (dist == 1)
        {
          this.fightMove.path.Clear();
          this.fightMove.path.Add(goal);
        }
        else
        {
          this.fightMove.path = Helper.Bresenham(this.GetPosition2D(), goal);
          if (!this.fightMove.path.Contains(goal))
          {
            this.fightMove.path.Add(goal);
            //TODO lip vyresit, aby NPC meli cestu i s cilem
          }
        }

        this.entity.StartMove();

        this.taskMove.isOnWay = false;
      }
    }

    public IActing GetTargetedEnemy()
    {
      return this.targetedEnemy;
    }

    public void Rest()
    {
      this.currentStatus.energy += this.step * 10;
      this.currentStatus.hp += this.step * 10;
      this.currentStatus.mana += this.step * 10;

      if (this.currentStatus.energy > 100)
        this.currentStatus.energy = 100;
      if (this.currentStatus.hp > this.character.hp)
        this.currentStatus.hp = this.character.hp;
      if (this.currentStatus.mana > this.character.mana)
        this.currentStatus.mana = this.character.mana;
    }

    public void DoAction(Action selectedAction, ConflictState state)
    {
      //update my status with such as subtract my mana, add healed hp,..
      this.currentStatus.hp = state.myHP;
      this.currentStatus.mana = state.myMana;
      this.currentStatus.energy = state.myEnergy;

      selectedAction.enemyHpTaken += this.character.level * this.character.power;
      //send action info into the game logic
      ActionInfo info = new ActionInfo();
      info.action = selectedAction;
      info.targetPosition = this.targetedEnemy.GetPosition3D();
      //info.targetPosition.Y += 80;
      info.startPosition = this.GetPosition3D();
      info.npcName = this.character.name;

      info.targetName = this.TargetedEnemy.GetCharacter().name;
      this.GetEntity().Spell(info);
      this.targetedEnemy.GetEntity().GetHarm();
    }

    public void DoAction(Action selectedAction, ConflictState state, string type)
    {
      if (type == "Defense")
      {
        //stronger healing
        this.currentStatus.hp = state.myHP + selectedAction.hpGot / 4;
        this.currentStatus.mana = state.myMana;
        this.currentStatus.energy = state.myEnergy;

        //weaker attack
        selectedAction.enemyHpTaken -= selectedAction.enemyHpTaken / 4;
        selectedAction.enemyHpTaken += this.character.level * this.character.power;
        //send action info into the game logic
        ActionInfo info = new ActionInfo();
        info.action = selectedAction;
        info.targetPosition = this.targetedEnemy.GetPosition3D();
        //info.targetPosition.Y += 80;
        info.startPosition = this.GetPosition3D();
        info.npcName = this.character.name;

        info.targetName = this.TargetedEnemy.GetCharacter().name;
        this.GetEntity().Spell(info);
        this.targetedEnemy.GetEntity().GetHarm();
      }
      else
      {
        //update my status with such as subtract my mana, add healed hp,..
        this.currentStatus.hp = state.myHP;
        this.currentStatus.mana = state.myMana;
        this.currentStatus.energy = state.myEnergy;

        selectedAction.enemyHpTaken += this.character.level * this.character.power;
        //send action info into the game logic
        ActionInfo info = new ActionInfo();
        info.action = selectedAction;
        info.targetPosition = this.targetedEnemy.GetPosition3D();
        //info.targetPosition.Y += 80;
        info.startPosition = this.GetPosition3D();
        info.npcName = this.character.name;

        info.targetName = this.TargetedEnemy.GetCharacter().name;
        this.GetEntity().Spell(info);
        this.targetedEnemy.GetEntity().GetHarm();
      }
    }

    public void Stun(int stunTime)
    {
      this.SetCoolDown(stunTime);
    }

    #endregion


    /// <summary>
    /// death of this npc
    /// </summary>
    private void Die()
    {
      this.currentStatus.alive = false;
      this.entity.Die();
      Logger.AddImportant(this.character.name + " umrel!");
    }

    #region IDisposable Members

    public void Dispose()
    {
      this.targetedEnemy = null;
      this.taskStack = null;
      this.pathFinding = null;
    }

    #endregion
  }



}

