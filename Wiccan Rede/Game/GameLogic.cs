using System;
using System.Collections.Generic;
using System.Text;
using WiccanRede.Objects;
using Microsoft.DirectX;
using WiccanRede.AI;
using System.Xml;
using System.Timers;

namespace WiccanRede.Game
{
    /// <summary>
    /// shoud serve like interface between ai and graphic reactions, for player actions etc
    /// </summary>
    class GameLogic
    {
        AI.AICore ai;
        List<AI.Action> actions;
        Graphics.Camera camera;

        internal AI.AICore Ai
        {
            get { return ai; }
        }

        Game.GameManager game;
        AI.ActionInfo fireballInfo;

        /// <summary>
        /// respawn interval in seconds
        /// </summary>
        const int RespawnInterval = 60;

        /// <summary>
        /// for respawn
        /// </summary>
        List<GameNPC> deadGuards;
        List<Timer> respawnTimers;

        public GameLogic(Game.GameManager game, AI.AICore ai)
        {
            this.ai = ai;
            this.game = game;

            this.deadGuards = new List<GameNPC>();
            this.respawnTimers = new List<Timer>();

            actions = this.LoadActions("Settings\\AttackSpells.xml");

            fireballInfo = new WiccanRede.AI.ActionInfo();
            this.camera = Graphics.Camera.GetCameraInstance();
            fireballInfo.startPosition = camera.GetVector3Position();
            fireballInfo.targetPosition = camera.GetVector3Position();
            fireballInfo.targetName = "NPC";
            fireballInfo.npcName = "Hrac";
            fireballInfo.action = actions[0];

        }

        /// <summary>
        /// loads action from xml file
        /// </summary>
        /// <param name="config">path to xml file</param>
        /// <returns>List of actions loaded from xml file</returns>
        private List<WiccanRede.AI.Action> LoadActions(string config)
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

        /// <summary>
        /// fireball from player to some else NPC
        /// </summary>
        /// <param name="nameOfHited">name of hited NPC</param>
        public void FireballHit(string nameOfHited)
        {
            ai.AcceptAction(nameOfHited, fireballInfo); 
            Logging.Logger.AddInfo(nameOfHited + " zasazen");
        }

        /// <summary>
        /// fireball from NPC to anybody else
        /// </summary>
        /// <param name="nameOfHited"></param>
        /// <param name="info"></param>
        public void FireballHit(string nameOfHited, AI.ActionInfo info)
        {
            ai.AcceptAction(nameOfHited, info);
            Logging.Logger.AddInfo(nameOfHited + " zasazen " + info.action.name);
        }

        /// <summary>
        /// AI controled NPC do some action
        /// </summary>
        /// <param name="info">info about action</param>
        public void Spell(AI.ActionInfo info)
        {
            CreateFireball(info);
        }

        /// <summary>
        /// human player do some action
        /// </summary>
        /// <param name="action">action from input</param>
        /// <param name="dir">direction where is player looking</param>
        internal void Spell(Input.Action action, Microsoft.DirectX.Vector3 dir)
        {
            switch (action)
            {
                case Input.Action.Action1:
                    //at action button send fireball
                    this.fireballInfo.startPosition = this.camera.GetVector3Position();
                    this.fireballInfo.targetPosition = this.camera.GetVector3Position() + dir;
                    CharacterNPC playerChar = this.ai.GetPlayerInfo().Character;

                    ActionInfo info = this.fireballInfo;
                    info.action.enemyHpTaken += playerChar.level * playerChar.power;

                    if (ai.AcceptPlayerAction(info))
                    {
                        CreateFireball(info); 
                    }
                    break;
                case Input.Action.Action2:
                    break;
                case Input.Action.Action3:
                    break;
                case Input.Action.Wheel:
                    break;
                default:
                    break;
            }
        }

        internal void PlayerMove(float angle)
        {
            if (!this.ai.IsPlayerStuned())
            {
                WiccanRede.Graphics.CameraDriver.MoveByAngle(angle); 
            }
            else
            {
                Logging.Logger.AddImportant("Player stuned");
            }
        }

        private void CreateFireball(AI.ActionInfo info)
        {
            if (info.action.name.ToLower().StartsWith("fire"))
            {
                Vector3 pos = info.startPosition;
                Vector3 dir = info.targetPosition - pos;
                dir.Normalize();
                Fireball f = new Fireball(info.npcName, Graphics.GraphicCore.GetInitializator().GetDevice(), pos, dir, info, this);
                Graphics.GraphicCore.GetCurrentSceneManager().AddObject(f.GetFireName(), f, null as Microsoft.DirectX.Direct3D.Effect);
            }
        }


        public void RegisterDeadNpc(GameNPC gameNpc, CharacterNPC character)
        {
            if (character.type == NPCType.guard)
            {
                Timer t = new Timer();
                t.AutoReset = false;
                t.Interval = RespawnInterval * 1000;
                t.Elapsed += new ElapsedEventHandler(respawn_Elapsed);
                t.Start();

                this.respawnTimers.Add(t);
                this.deadGuards.Add(gameNpc);
                
            }
        }

        void respawn_Elapsed(object sender, ElapsedEventArgs e)
        {
            Timer t = sender as Timer;
            int index = this.respawnTimers.IndexOf(t);
            this.deadGuards[index].Respawn();
            
            this.respawnTimers.RemoveAt(index);
            this.deadGuards.RemoveAt(index);
           

            t.Dispose();
        }

    }
}
