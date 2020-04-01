using System;
using System.Collections.Generic;
using System.Text;
using Logging;
using System.Diagnostics;
using Microsoft.DirectX;
using System.Drawing;

namespace WiccanRede.AI
{
  public class GeneralNPC : IActing
  {
    #region Positioning
    protected Vector3 position3D;

    public Vector3 Position3D
    {
      get { return position3D; }
    }

    protected Point position2D;

    public Point Position2D
    {
      get { return position2D; }
    }
    protected int x, y;
    protected System.Drawing.Point targetCell;

    protected Vector3 direction;
    public Vector3 Direction
    {
      get { return direction; }
    }

    #endregion

    #region AI structures
    protected CharacterNPC character;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected Map aiMap;
    protected MapCellInfo[,] npcMap;

    protected FSM fsm;
    internal FSM Fsm
    {
      get { return fsm; }
    }

    protected Astar pathFinding;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal Astar PathFinding
    {
      get { return pathFinding; }
    }

    protected NpcTask currentTask;
    internal NpcTask CurrentTask
    {
      get { return currentTask; }
    }

    protected ActionSelection actionSelection;

    internal ActionSelection ActionSelection
    {
      get { return actionSelection; }
    }
    #endregion

    #region World input
    protected IWalkable terrain;
    internal IWalkable Terrain
    {
      get { return terrain; }
    }

    protected IControlable entity;
    internal IControlable Entity
    {
      get { return entity; }
    }
    #endregion

    #region Moving

    protected Traveling taskMove;

    internal Traveling TaskMove
    {
      get { return taskMove; }
    }

    protected Traveling fightMove;

    internal Traveling FightMove
    {
      get { return fightMove; }
    }

    //for 3D moving
    private float stepProgress;
    private float step;

    #endregion Moving


    protected Stack<NpcTask> taskStack;

    protected IActing targetedEnemy;
    protected Status currentStatus;
    protected int cooldown;
    protected int visualRange;


    public GeneralNPC(CharacterNPC character, Vector3 position, IWalkable terrain, IControlable entity)
    {
      this.character = character;
      this.terrain = terrain;
      this.entity = entity;
      this.position3D = position;
      this.position2D = terrain.Get2DMapPosition(this.position3D);
      this.taskStack = new Stack<NpcTask>();
      this.taskMove = new Traveling(new Astar());
      this.fightMove = new Traveling(new Astar());

      //init start status
      this.currentStatus = new Status();
      this.currentStatus.hp = character.hp;
      this.currentStatus.mana = character.mana;
      this.currentStatus.energy = 100;
      this.currentStatus.position = this.position2D;
      this.currentStatus.enemySeen = 0;
      this.currentStatus.alive = true;
      this.currentStatus.nearEnemies = new List<IActing>();

      this.targetCell = this.position2D;
      aiMap = Map.GetInstance();
      this.npcMap = aiMap.getRelatedMap(this.position2D, this.visualRange);
    }


    ////////////////////////////////////
    //moving methods

    /// <summary>
    /// calculate distance from this npc location to the given point using Manhatan distance
    /// </summary>
    /// <param name="position">position of point where to calculate distance</param>
    /// <returns>distance</returns>
    protected int CalculateDistance(Point position)
    {
      int xd = position.X - this.x;
      int yd = position.Y - this.y;

      int distance = Math.Max(Math.Abs(xd), Math.Abs(yd));
      return distance;
    }

    public static int CalculateDistance(Point p0, Point p1)
    {
      int xd = p0.X - p1.X;
      int yd = p0.Y - p1.Y;

      int distance = Math.Max(Math.Abs(xd), Math.Abs(yd));
      return distance;
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
    }

    /// <summary>
    /// Move with npc in 3D, do linear interpolation, calls entitity.ChangePosition(...)
    /// </summary>
    /// <param name="targettarged 3D position</param>
    /// <param name="step">time step from last update from seconds</param>
    private void Move3D(Vector3 target, float step)
    {
      Vector3 startPosition = terrain.Get3Dposition(new System.Drawing.Point(this.x, this.y));
      this.position3D = Interpolate(startPosition, target, this.stepProgress);

      entity.ChangePosition(this.position3D);
      this.direction = target - this.terrain.Get3Dposition(new System.Drawing.Point(this.x, this.y));

      entity.ChangeDirection(this.direction);

      this.stepProgress += (step / 1f);
      if (this.stepProgress >= 1)
      {
        this.x = targetCell.X;
        this.y = targetCell.Y;

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
    private Vector3 Interpolate(Vector3 start, Vector3 end, float delta)
    {
      Vector3 result = new Vector3();
      result.X = (1 - delta) * start.X + delta * end.X;
      result.Y = (1 - delta) * start.Y + delta * end.Y;
      result.Z = (1 - delta) * start.Z + delta * end.Z;
      return result;
    }



    #region IActing Members

    public void AddTask(NpcTask target)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public CharacterNPC GetCharacter()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Go()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void DoFightMove(Point goal)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public Status GetStatus()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void SetStatus(Status status)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void SufferDamage(int damage)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void DrainMana(int mana)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Heal(int hp)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void SetCoolDown(int miliseconds)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public IControlable GetEntity()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public Vector3 GetPosition3D()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public IActing GetTargetedEnemy()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Rest()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void DoAction(Action selectedAction, ConflictState state)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Stun(int stunTime)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }


  ////////////////////////////////////////////////////
  //helping structures
  ////////////////////////////////////////////////////


  /// <summary>
  /// struct representing NPC's task, representation contains checkpoints, time, interaction attributes
  /// </summary>
  public struct NpcTask
  {
    public List<Point> checkpoints;
    public bool bKill;
    public string name;
    public float time;
    public bool order;

    public bool finished;
    public Result result;

    public override string ToString()
    {
      string ret = "NpcTarget \n";
      foreach (Point p in checkpoints)
      {
        ret += p.ToString();
      }
      ret += "\n";
      ret += "zabit =" + bKill.ToString();
      ret += "cas = " + time.ToString();
      ret += " hotov = " + result.ToString();
      return ret;
    }
  }

  /// <summary>
  /// represents base npc stats as is life, mana, enemies, position
  /// </summary>
  public struct Status
  {
    public int enemySeen, enemyPower;
    public float hp, mana, energy;
    public bool alive;
    public Point position;
    public List<IActing> nearEnemies;
    public int cooldown;

    public Status(int hp, int mana, int energy, bool alive, Point pos, int enemySeen, int enemyPower, List<IActing> nearEnemies)
    {
      this.hp = hp;
      this.mana = mana;
      this.energy = energy;
      this.enemySeen = enemySeen;
      this.alive = alive;
      this.position = pos;
      this.enemyPower = enemyPower;
      this.nearEnemies = nearEnemies;
      this.cooldown = 0;
    }

    public override string ToString()
    {
      return "Status: HP=" + hp + "; Mana= " + mana + "; Energy= " + energy;
    }
  }

  public struct Traveling
  {
    public Astar pathFinding;
    public List<Point> path;
    public bool isOnWay;
    public int progress;

    public Traveling(Astar a)
    {
      pathFinding = a;
      path = new List<Point>();
      isOnWay = false;
      progress = 0;
    }
  }
}
