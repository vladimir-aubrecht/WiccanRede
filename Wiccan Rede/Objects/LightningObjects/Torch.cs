using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using WiccanRede.Graphics.Scene;
using WiccanRede.Objects;
using WiccanRede.Objects.ParticleSystem;
using WiccanRede.Objects.ParticleSystem.SpecializedParticle;

namespace WiccanRede.Objects.LightningObjects
{
    class Torch : Lights
    {
        private static int TorchCount = 0;
        private int TorchNumber;
        ParticleSystem.ParticleSystem fireParticle;
        float lasttime;


        public Torch(Device device, ProgressiveMesh mesh, Matrix world, Texture[] texture)
            : base(mesh, world, texture[0])
        {
            base.SetType(WiccanRede.Graphics.Scene.LightType.Point);

            fireParticle = new FireParticleSystem(device, TextureLoader.FromFile(mesh.Device, @"Resources/Textures/Fire.dds"));

        }

        public String GetFireName()
        {
            return "ohen" + TorchNumber;
        }

        public override void InitShaderValue(Effect effect)
        {
            base.InitShaderValue(effect);

            WiccanRede.Graphics.GraphicCore.GetCurrentSceneManager().AddObject("ohen" + Torch.TorchCount, fireParticle, "Particles", false);
            TorchNumber = TorchCount;
            
            Torch.TorchCount++;
        }

        public override void Update(float time)
        {
            base.Update(time);

            float frametime = time - lasttime;

            if (frametime > 20)
            {
                int particlesInFrame = 8;

                int count = (int)(frametime * 0.05);

                Vector4 pos = GetLightPosition();

                for (int i = 0; i < count; i++)
                {
                    fireParticle.AddParticleSystemAtOffSet(new Vector3(pos.X, pos.Y, pos.Z), particlesInFrame, Vector3.Empty);
                }

                lasttime = time;
            }

            fireParticle.Update(time);

        }


        public override Vector4 GetLightPosition()
        {
            Matrix world = base.GetMatrixWorld();
            Vector3 position = new Vector3();

            world = Matrix.Translation(0, 0, 110) * world;
            position = Vector3.TransformCoordinate(position, world);

            return new Vector4(position.X, position.Y, position.Z, 1);
        }

        public override float GetSphereRadius()
        {
            return 8f*base.GetSphereRadius();
        }

    }
}
