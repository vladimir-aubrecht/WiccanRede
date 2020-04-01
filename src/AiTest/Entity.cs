using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;

namespace WiccanRede.AI
{
    class Entity : IControlable
    {
        public Entity(CharacterNPC character, AICore ai, string fsmName)
        {
            ai.AddPlayer(character, new Vector3(), this, fsmName, "");
        }

        #region IControlable Members

        public void ChangePosition(Microsoft.DirectX.Vector3 v)
        {
        }

        public void ChangeDirection(Microsoft.DirectX.Vector3 v)
        {
        }

        public void GetHarm()
        {
        }

        public void Spell(ActionInfo info)
        {
        }

        public void Die()
        {
        }

        public void SetStatus(Status status)
        {
        }
        /// <summary>
        /// real 3D position, not 3D position of 2D map field
        /// </summary>
        /// <returns>3D position</returns>
        public Vector3 GetPosition3D()
        {
            return new Vector3();
        }

        public void Talk(string text)
        {

        }

        public void StartMove()
        {
            
        }

        public void StopMove()
        {
            
        }

        #endregion
    }
}
