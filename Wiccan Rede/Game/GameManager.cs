using System;
using System.Collections.Generic;
using System.Text;
using WiccanRede.Graphics;
using System.IO;
using System.Xml;
using System.Timers;

namespace WiccanRede.Game
{
  public class GameManager
  {
    static GameManager instance;

    Game.GameNPC[] gameNpcs;
    AI.CharacterNPC[] characters;

    WiccanRede.AI.AICore ai;

    Game.GameLogic logic = null;

    internal Game.GameLogic Logic
    {
      get { return logic; }
    }
    static WiccanRede.AI.IWalkable terrain;
    GameStateMachine gameStateMachine;

    //Loaded npc information
    List<string> npcNames;
    List<string> controlMechanisms;
    List<string> characterNames;
    List<Microsoft.DirectX.Vector3> positions;
    List<string> actionsName;

    /// <summary>
    /// returns class instance - singleton
    /// </summary>
    public static GameManager GetInstance()
    {
      if (instance == null)
        instance = new GameManager();
      return instance;
    }

    public void Restart()
    {
      this.ai.Dispose();

      this.ai = new WiccanRede.AI.AICore(terrain);
      this.logic = new GameLogic(this, ai);
      Initialization();

      CameraDriver.MoveToStartPosition();
    }

    private GameManager()
    {
      GameManager.terrain = CameraDriver.GetAttachedTerain();
      ai = new WiccanRede.AI.AICore(terrain);
      this.logic = new GameLogic(this, ai);

      Initialization();
    }

    private void Initialization()
    {
      try
      {
        this.LoadNpcConfig("Game\\Settings\\npcList.xml", out npcNames, out controlMechanisms, out positions, out characterNames, out actionsName);
      }
      catch (Exception ex)
      {
        Logging.Logger.AddError("Chyba pri nacitani konfigurace NPC! " + ex.ToString());
        throw;
      }


      gameNpcs = new GameNPC[npcNames.Count];
      characters = new WiccanRede.AI.CharacterNPC[npcNames.Count];

      for (int i = 0; i < npcNames.Count; i++)
      {
        try
        {
          characters[i] = new WiccanRede.AI.CharacterNPC("Settings\\NPC\\" + characterNames[i] + ".xml");
          characters[i].name = npcNames[i];

          if (!this.controlMechanisms[i].EndsWith(".lua"))
          {
            gameNpcs[i] = new GameNPC(characters[i], this.logic, controlMechanisms[i], positions[i], "Settings\\" + actionsName[i] + ".xml");
          }
          else
          {
            gameNpcs[i] = new GameNPC(characters[i], this.logic, "Scripting\\Scripts\\" + controlMechanisms[i], positions[i], "Settings\\" + actionsName[i] + ".xml");
          }
        }
        catch (Exception ex)
        {
          Logging.Logger.AddWarning("Chyba pri nacitani NPC: " + ex.ToString());
        }
      }
      GameNPC player = new GameNPC(this.logic, new AI.CharacterNPC("Settings\\NPC\\Player.xml"));

      gameStateMachine = new GameStateMachine("Game\\Settings\\OpeningLevel.xml");
    }

    private void LoadNpcConfig(string path, out List<string> npcNames, out List<string> controlMechanisms,
        out List<Microsoft.DirectX.Vector3> positions, out List<string> characterNames, out List<string> actions)
    {
      controlMechanisms = new List<string>();
      npcNames = new List<string>();
      positions = new List<Microsoft.DirectX.Vector3>();
      characterNames = new List<string>();
      actions = new List<string>();

      System.Xml.XmlDataDocument doc = new System.Xml.XmlDataDocument();
      doc.Load(path);

      XmlNode rootNode = doc.GetElementsByTagName("List")[0];

      foreach (XmlNode npcNode in rootNode.ChildNodes)
      {
        foreach (XmlNode node in npcNode.ChildNodes)
        {
          switch (node.Name)
          {
            case "Name":
              npcNames.Add(node.InnerText);
              break;
            case "Script":
              controlMechanisms.Add(node.InnerText);
              break;
            case "FSM":
              controlMechanisms.Add(node.InnerText);
              break;
            case "Position":
              Microsoft.DirectX.Vector3 position = new Microsoft.DirectX.Vector3();
              int x = Convert.ToInt32(node.ChildNodes[0].InnerText);
              int z = Convert.ToInt32(node.ChildNodes[1].InnerText);
              position = terrain.Get3Dposition(new System.Drawing.Point(x, z));

              positions.Add(position);
              break;
            case "Character":
              characterNames.Add(node.InnerText);
              break;
            case "Actions":
              actions.Add(node.InnerText);
              break;
          }
        }
      }
    }

    /// <summary>
    /// updates ai
    /// </summary>
    /// <param name="frameTime">time for one frame</param>
    public void Update(float frameTime)
    {
      ai.Step = frameTime;
      ai.Update();
    }

    /// <summary>
    /// nahodi herni udalost pro vyhodnoceni
    /// </summary>
    /// <param name="sender">jemno objektu, ktery udalost posila</param>
    /// <param name="gameEvent">jmeno udalosti</param>
    /// <returns>vraci zda vyhovuje podminka a objekt</returns>
    public bool FireGameEventUp(string sender, string gameEvent)
    {
      bool succes = gameStateMachine.Update(sender, gameEvent);
      if (succes)
      {
        TalkRequest("Story");
        ai.Update(this.gameStateMachine.CurrentState.Name);
      }
      else
      {
        TalkRequest(sender);
      }
      return succes;
    }

    public void PickupObject(string name)
    {
      int maxHp, maxMana;

      maxHp = ai.GetPlayerInfo().Character.hp;
      maxMana = ai.GetPlayerInfo().Character.mana;

      WiccanRede.AI.Action action = new WiccanRede.AI.Action();
      action.hpGot = maxHp/4;
      action.manaDrain = -maxMana/4;
      action.name = "StatsUpdate";
      action.time = 1;
      action.probability = 100;

      WiccanRede.AI.ActionInfo info = new WiccanRede.AI.ActionInfo();
      info.action = action;
      info.targetName = "Player";

      ai.AcceptPlayerAction(info);
    }

    public void TalkRequest(string sender)
    {
      this.ai.TalkRequest(sender, this.gameStateMachine.CurrentState.Name);
    }

    /// <summary>
    /// gets all base info abou all NPCs
    /// </summary>
    /// <returns>list of status-character pairs </returns>
    public List<WiccanRede.AI.NpcInfo> GetAllNpcInfo()
    {
      return ai.GetNpcInfo();
    }


    //public void NpcDied(AI.CharacterNPC character)
    //{
    //    logic.RegisterDeadNpc(character);
    //}

    internal GameState GetCurrentGameState()
    {
      if (this.gameStateMachine == null)
      {
        return null;
      }
      return this.gameStateMachine.CurrentState;
    }

    public void RespawnNpc(AI.CharacterNPC character, AI.IControlable entita)
    {
      int index = this.npcNames.IndexOf(character.name);

      GameNPC gameNpc = new GameNPC(character, this.logic, this.controlMechanisms[index], this.positions[index], this.actionsName[index]);
    }
  }
}
