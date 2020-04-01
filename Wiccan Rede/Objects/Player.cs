using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using WiccanRede.Graphics.Scene;
using WiccanRede.Graphics;

namespace WiccanRede.Objects
{
    class Player : WiccanRede.Graphics.Scene.SpecialObjects.AnimatedObject
    {
        private static int lastLOD = 0;

        private IGeneralObject equipedItem;
        private Vector3 handPosition = new Vector3(-63, 107, 0);
        private Vector3 handScale = new Vector3(1, 1, 1);

        private bool haveHand = true;

        public Player(ProgressiveMesh pMesh, Matrix world, Texture[] textures, Matrix handWorld, AnimationRootFrame rootFrame)
            : base(pMesh.Device, world, rootFrame, textures, pMesh)
        {
            try
            {
                this.handPosition = new Vector3(handWorld.M41, handWorld.M42, handWorld.M43);
                this.handScale = new Vector3(handWorld.M11, handWorld.M22, handWorld.M33);

                haveHand = !(handWorld.M44 == 0);

                Init(pMesh);

                EnableAnimation(false);
            }
            catch (Exception ex)
            {
                Logging.Logger.AddError(ex.Message);
            }
        }

        private void Init(ProgressiveMesh mesh)
        {
            if (mesh != null && mesh is ProgressiveMesh)
            {
                mesh.NumberFaces = (int)(mesh.MaxFaces * base.GetObjectQuality());
            }
        }

        public override void ApplyLOD()
        {
            BaseMesh bmesh = base.GetModel();

            if (bmesh is ProgressiveMesh)
                return;

            if (bmesh != null && !bmesh.Disposed)
            {
                float LODquality = 1f;

                bool enableLOD = base.isEnableLOD();

                if (enableLOD)
                    LODquality = 1f / (1f + 0.003f * base.GetDistanceFromCamera());

                ProgressiveMesh mesh = bmesh as ProgressiveMesh;
                int LOD = (int)(mesh.MaxFaces * LODquality * base.GetObjectQuality());

                if (lastLOD != LOD && enableLOD)
                {
                    mesh.NumberFaces = LOD;
                    lastLOD = LOD;
                }
            }

            base.ApplyLOD();
        }

        public bool HaveHand()
        {
            return haveHand;
        }

        public IGeneralObject GetEquipedItem()
        {
            return equipedItem;
        }

        public void EquipItem(IGeneralObject item)
        {
            if (equipedItem != null)
                equipedItem.SetEquiped(false);   

            
            item.SetEquiped(true);
            equipedItem = item;
        }

        public override void Update(float time)
        {
            base.Update(time);

            if (equipedItem != null)
            {
                Matrix playerWorld = this.GetMatrixWorld();
                Matrix world = Matrix.Scaling(handScale) * Matrix.RotationX((float)Math.PI / -6f) * Matrix.RotationY((float)Math.PI - 0.166f * (float)Math.PI) * Matrix.Translation(handPosition);
                world *= playerWorld;
                

                equipedItem.SetMatrixWorld(world);
            }
        }
    }
}
