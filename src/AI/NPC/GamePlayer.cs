using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRede.AI
{
  /// <summary>
  /// class for representing human player, singleton
  /// </summary>
  class GamePlayer : IActing
  {
    Status currentStatus;
    CharacterNPC character;
    int cooldown;
    IControlable entity = null;
    bool isStuned;

    public bool IsStuned
    {
      get { return isStuned; }
    }
    /// <summary>
    /// cooldown on which player can't do anything
    /// </summary>
    public int Cooldown
    {
      get { return cooldown; }
    }

    static GamePlayer instance;

    ///// <summary>
    ///// gets instance of Player, if this is null, create new
    ///// </summary>
    ///// <returns></returns>
    //public static GamePlayer GetInstance()
    //{
    //    //if (instance == null)
    //    //{
    //    //    instance = new GamePlayer();
    //    //}
    //    //return instance;
    //    return this;
    //}

    /// <summary>
    /// gets this object as IActing
    /// </summary>
    /// <returns></returns>
    public static IActing GetPlayer()
    {
      return instance;
    }

    /// <summary>
    /// private ctor - singleton
    /// </summary>
    public GamePlayer(IControlable entity, CharacterNPC character)
    {
      //character = new CharacterNPC("Settings//NPC//Player.xml");
      this.character = character;
      this.entity = entity;
      currentStatus = new Status();
      this.currentStatus.hp = character.hp;
      this.currentStatus.mana = character.mana;
      this.currentStatus.energy = 100;
      this.currentStatus.position = AI.Map.GetInstance().GetTerrain().GetPlayerPosition();
      this.currentStatus.enemySeen = 0;
      this.currentStatus.alive = true;
      instance = this;
    }

    /// <summary>
    /// update cooldown
    /// </summary>
    /// <param name="step">duration from last update in miliseconds, typicaly little part of second</param>
    public void Update(float step)
    {
      this.cooldown -= (int)(step * 1000);
      if (this.cooldown <= 0)
      {
        cooldown = 0;
        if (this.isStuned)
          this.isStuned = false;
      }
      this.currentStatus.position = Map.GetInstance().GetTerrain().GetPlayerPosition();
      if (this.currentStatus.mana < this.character.mana - this.character.mana / 4)
      {
        this.currentStatus.mana += (6f * step);
      }
      else if (this.currentStatus.mana > this.character.mana)
      {
        this.currentStatus.mana = this.character.mana;
      }
    }

    public void UpdateCharacter(CharacterNPC character)
    {
      this.character = character;
    }

    /// <summary>
    /// real 3D position, not 3D position of 2D map field
    /// </summary>
    /// <returns>3D position</returns>
    public Microsoft.DirectX.Vector3 GetPosition3D()
    {
      Microsoft.DirectX.Vector3 pos = Map.GetInstance().GetTerrain().GetPlayerPosition3D();
      pos.Y -= 10;
      return pos;
    }

    #region IActing Members

    public void AddTask(NpcTask target)
    {
      //senseless
    }
    /// <summary>
    /// returns player character
    /// </summary>
    /// <returns>Character of npc</returns>
    public CharacterNPC GetCharacter()
    {
      return this.character;
    }

    public void Go()
    {
      //senseless
    }

    /// <summary>
    /// gets status of player
    /// </summary>
    /// <returns>current status</returns>
    public Status GetStatus()
    {
      return this.currentStatus;
    }

    /// <summary>
    /// subtract the damage from current hit points, check if player is death
    /// </summary>
    /// <param name="damage">damage in hit points</param>
    public void SufferDamage(int damage)
    {
      this.entity.GetHarm();
      //this.currentHP -= damage;
      int takenHP = damage - this.character.defense * this.character.level;
      this.currentStatus.hp -= takenHP;
      if (this.currentStatus.hp <= 0)
      {
        this.currentStatus.alive = false;
      }
    }

    /// <summary>
    /// subtract mana from current mana
    /// </summary>
    /// <param name="mana">drained mana</param>
    /// <remarks>negative value means addition</remarks>
    public void DrainMana(int mana)
    {
      this.currentStatus.mana -= mana;

      if (this.currentStatus.mana <= 0)
      {
        this.currentStatus.mana = 0;
      }
      else if (this.currentStatus.mana > this.character.mana)
      {
        this.currentStatus.mana = this.character.mana;
      }
    }
    /// <summary>
    /// set cooldown (after spellcasting for example), when player can't do anything
    /// </summary>
    /// <param name="miliseconds">time in ms to set cooldown</param>
    public void SetCoolDown(int miliseconds)
    {
      this.cooldown = miliseconds;
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
    /// sets status of player
    /// </summary>
    /// <param name="status">new status to set</param>
    public void SetStatus(Status status)
    {
      this.currentStatus = status;
    }

    public IControlable GetEntity()
    {
      return this.entity;
    }
    public void DoFightMove(System.Drawing.Point goal)
    {
      //senseless for player
    }

    public IActing GetTargetedEnemy()
    {
      return this;
    }

    public void Rest()
    {
    }

    public void DoAction(Action selectedAction, ConflictState state)
    {

    }

    public void Stun(int stunTime)
    {
      this.SetCoolDown(stunTime);
      this.isStuned = true;
    }

    #endregion
  }
}
