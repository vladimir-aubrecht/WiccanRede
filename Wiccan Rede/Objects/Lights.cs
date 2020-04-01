using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using WiccanRede.Graphics.Scene;

namespace WiccanRede.Objects
{
    class Lights : GeneralObject, ISceneLight
    {
        Vector3 lightPosition;
        WiccanRede.Graphics.Scene.LightType type = WiccanRede.Graphics.Scene.LightType.Direction;
        float attuentation = 1f;

        public Lights(ProgressiveMesh mesh, Matrix world, Texture texture)
            : base(mesh, world, new Texture[] { texture }, null, null, null)
        {
            lightPosition = new Vector3(world.M41, world.M42, world.M43) * (1f / world.M44);
        }

        public override void SetMatrixWorld(Matrix worldMatrix)
        {
            base.SetMatrixWorld(worldMatrix);

            lightPosition = new Vector3(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43) * (1f / worldMatrix.M44);
        }

        protected void SetLightPosition(Vector3 position)
        {
            this.lightPosition = position;
        }

        #region IEMLight Members

        public virtual bool isEnable()
        {
            return true;
        }

        public virtual Vector4 GetLightPosition()
        {
            return new Vector4(lightPosition.X, lightPosition.Y, lightPosition.Z, 1);
        }

        public void SetType(WiccanRede.Graphics.Scene.LightType type)
        {
            this.type = type;
        }

        public void SetAttuentation(float att)
        {
            this.attuentation = att;
        }

        public new WiccanRede.Graphics.Scene.LightType GetType()
        {
            return type;
        }

        public float GetAttuentation()
        {
            return attuentation;
        }

        #endregion

    }
}
