using Microsoft.DirectX;

namespace WiccanRede.AI
{

    //public struct Character
    //{
    //    public int hp, level, mana, power, defense;
    //    public Side side;
    //    public NPCType type;
    //    public string name;
    //}


    public enum SpellType
    {
        water, fire, air, earth, white, black
    }

    public enum AiEvents
    {
        enemySeen,
        enemyLost,
        enemyKilled,
        weakness,
        ready,
        lowHP,
        disadvantage,
        newTask,
        talk,
        run,
        seek
    }

    public enum Priors
    {
        kill,
        live,
        hide,
        boost,
        overview,
        cover,
        help,
        surprise
    }

    public enum Result
    {
        succes,
        fail,
        none
    }

    public enum NPCType
    {
        beast=0, guard=1, villager=2, enemy=3, boss=4
    }
    public enum Side
    {
        ally=0, neutral=1, enemy=2
    }
    public interface IWalkable
    {
        bool IsPositionOnTereain(Vector3 position);
        bool IsPositionOnTerainBlocked(Vector3 position);
        Vector3 GetPositionOnTerain(Vector3 position);
        Vector2 GetTerrainSize();

        System.Drawing.Point Get2DMapPosition(Vector3 position);
        Vector3 Get3Dposition(System.Drawing.Point position2D);
        System.Drawing.Point GetPlayerPosition();
        Microsoft.DirectX.Vector3 GetPlayerPosition3D();
        int[,] GetMap();
        System.Drawing.PointF GetSize();
        System.Collections.Generic.List<System.Drawing.Point> GetBlockedPositions();
    }
    public interface IActionSelection
    {
        ConflictState GetNextAction(IActing myself, IActing enemy, Priorities priors, out Action selectedAction);
    }
    public interface IScripting
    {
        object[] Update(params object[] pars);
    }
    public interface IControlable
    {
        void ChangePosition(Vector3 v);
        void ChangeDirection(Vector3 v);
        void GetHarm();
        void Spell(ActionInfo info);
        void Die();
        void SetStatus(Status status);
        void Talk(string text);
        void StartMove();
        void StopMove();
    }
    public interface IActing
    {
        void AddTask(NpcTask target);
        CharacterNPC GetCharacter();
        void Go();
        void DoFightMove(System.Drawing.Point goal);
        Status GetStatus();
        void SetStatus(Status status);
        void SufferDamage(int damage);
        void DrainMana(int mana);
        void Heal(int hp);
        void SetCoolDown(int miliseconds);
        IControlable GetEntity();
        Vector3 GetPosition3D();
        IActing GetTargetedEnemy();
        void Rest();
        void DoAction(Action selectedAction, ConflictState state);
        void Stun(int stunTime);
    }
}