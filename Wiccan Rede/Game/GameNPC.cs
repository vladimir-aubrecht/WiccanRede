using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Collections.Generic;
using System.Text;
using WiccanRede.AI;
using WiccanRede.Objects;
using WiccanRede.Graphics.Scene;

namespace WiccanRede.Game
{
    class GameNPC : IControlable
    {
        Player player;
        CharacterNPC character;
        Vector3 position;
        Vector3 direction;
        Matrix invertLastRotationY = Matrix.Identity;
        AICore ai;
        Game.GameLogic logic;
        string actionDefinition;
        string fsmName;
        string scriptPath;

        //IWalkable terrain;

        
        //static List<GameNPC> players = new List<GameNPC>();

        /// <summary>
        /// WiccanRede.AI controlled npc
        /// </summary>
        /// <param name="character">his character</param>
        /// <param name="logic">GameLogic object - used for interaction, respawn, etc, contains AICore object</param>
        /// <param name="position">absolute position on map</param>
        /// <param name="fsmName">name of FSM plug-in which will be loaded for this NPC</param>
        /// <param name="actionDefinition">name of config xml file with definitions of actions</param>
        public GameNPC(CharacterNPC character, Game.GameLogic logic, string fsmName, Vector3 position, string actionDefinition)
        {
            this.ai = logic.Ai;
            this.logic = logic;
            this.character = character;
            this.position = position;
            this.fsmName = fsmName;
            this.actionDefinition = actionDefinition;
            this.direction = new Vector3(1, 0, 0);

            JoinToGraphic(character);

            logic.Ai.AddPlayer(character, position, this, fsmName, actionDefinition);
        }

        /// <summary>
        /// WiccanRede.AI controlled npc with script
        /// </summary>
        /// <param name="character">his character</param>
        /// <param name="logic"> GameLogic object - used for interaction, respawn, etc, contains AICore object</param>
        /// <param name="position">absolute position on map</param>
        /// <param name="scriptPath">path to the script file</param>
        public GameNPC(CharacterNPC character, Game.GameLogic logic, string scriptPath, Vector3 position)
        {
            this.ai = logic.Ai;
            this.logic = logic;
            this.character = character;
            this.position = position;
            this.scriptPath = scriptPath;
            this.actionDefinition = "";
            this.direction = new Vector3(1, 0, 0);

            JoinToGraphic(character);

            logic.Ai.AddPlayer(character, position, this, scriptPath);
        }

        /// <summary>
        /// human player
        /// </summary>
        /// <param name="character">his character</param>
        public GameNPC(Game.GameLogic logic, CharacterNPC character)
        {
            this.character = character;
            this.direction = new Vector3(1, 0, 0);
            this.logic = logic;
            this.logic.Ai.AddPlayer(this, character);
        }

        private void JoinToGraphic(CharacterNPC character)
        {
            SceneManager sceneManager = Graphics.GraphicCore.GetCurrentSceneManager();
            player = sceneManager.GetObject(character.name).generalObject as Player;

            if (player.HaveHand())
                player.EquipItem(sceneManager.GetObject(character.name + "_Pochoden").generalObject);

            Matrix world = player.GetMatrixWorld();
            position = new Vector3(world.M41, world.M42, world.M43) * (1f / world.M44);
            ChangePosition(position);
        }


        #region IControlable Members

        /// <summary>
        /// move with player at new position
        /// </summary>
        /// <param name="v">new position</param>
        public void ChangePosition(Vector3 v)
        {
            this.position = v;

            Matrix world = player.GetMatrixWorld();
            Matrix worldOriginal = player.GetMatrixWorldOriginal();

            Vector3 min = player.GetBoundingBoxRelativeMinimum();
            min.TransformCoordinate(world);
            min.Y -= world.M42 / world.M44;

            world.M41 = v.X * world.M44;
            world.M42 = (v.Y - min.Y) * world.M44;
            world.M43 = v.Z * world.M44;
            player.SetMatrixWorld(world);
        }

        /// <summary>
        /// change direction of player
        /// </summary>
        /// <param name="v">new direction</param>
        public void ChangeDirection(Vector3 v)
        {
            v = Vector3.Normalize(v);

            this.direction = v;

            player.SetDirection(v);
        }

        public void StartMove()
        {
            player.EnableAnimation(true);
        }

        public void StopMove()
        {
            player.EnableAnimation(false);
        }

        public void GetHarm()
        {
            //TODO dodelat efekt pro zraneni
        }

        public void Spell(WiccanRede.AI.ActionInfo info)
        {
            Vector3 pos = player.GetPosition();
            //Logging.Logger.AddInfo(info.ToString());
            pos.Y += 50;

            Vector3 dir = info.targetPosition - pos;
            dir.Normalize();

            player.SetDirection(dir);
            logic.Spell(info);
        }

        public void Die()
        {
            SceneManager sm = Graphics.GraphicCore.GetCurrentSceneManager();

            sm.DeleteObject(character.name);
            //logic.RegisterDeadNpc(this, character);

            IRenderable i = player.GetEquipedItem();

            if (i != null)
            {
                GeneralObject ei = ((GeneralObject)i);

                sm.DeleteObject(((Objects.LightningObjects.Torch)ei).GetFireName());
                sm.DeleteObject(ei);
            }

        }

        public void Respawn()
        {
            System.Console.WriteLine("Respawn");
            //SceneManager sm = Graphics.GraphicCore.GetCurrentSceneManager();

            //sm.AddObject(character.name, player, (Effect) null);

            //IGeneralObject i = player.GetEquipedItem();

            //if (i != null)
            //{
            //    sm.AddObject(character.name + "_Pochoden", i, (Effect)null);
            //}

            //this.ai.AddPlayer(this.character, this.position, this, this.fsmName, this.actionDefinition); 
        }

        public void Talk(string text)
        {
            if (this.character.name == "Player")
                WiccanRede.Graphics.HUD.DrawText(text, "Quest");
            else
                WiccanRede.Graphics.HUD.DrawText(text, character.name);
        }


        public void SetStatus(Status status)
        {
            
        }

        #endregion
    }
}
