using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using WiccanRede.Graphics.Scene;

namespace WiccanRede.Objects
{
    class Building : GeneralObject
    {
        static int lastLOD = 0;

        public Building(ProgressiveMesh mesh, Matrix world, Texture[] color_textures0, Texture[] normal_textures)
            : base(mesh, world, color_textures0, null, null, normal_textures)
        {

        }

        public override void ApplyLOD()
        {
            BaseMesh bmesh = base.GetModel();

            if (bmesh != null && !bmesh.Disposed)
            {
                float LODquality = 1f;

                if (base.isEnableLOD())
                    LODquality = 1f / (1f + 0.008f * base.GetDistanceFromCamera());

                ProgressiveMesh mesh = bmesh as ProgressiveMesh;
                int LOD = (int)(mesh.MaxFaces * LODquality * base.GetObjectQuality());

                if (lastLOD != LOD)
                {
                    try
                    {
                        mesh.NumberFaces = LOD;
                        lastLOD = LOD;
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.AddError(ex.Message);
                    }
                }
            }

            base.ApplyLOD();
        }

    }
}
