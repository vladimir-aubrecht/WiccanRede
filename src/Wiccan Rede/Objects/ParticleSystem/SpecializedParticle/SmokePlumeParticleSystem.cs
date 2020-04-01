#region File Description
//-----------------------------------------------------------------------------
// SmokePlumeParticleSystem.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
#endregion

namespace WiccanRede.Objects.ParticleSystem.SpecializedParticle
{
    /// <summary>
    /// Custom particle system for creating a giant plume of long lasting smoke.
    /// </summary>
    class SmokePlumeParticleSystem : ParticleSystem
    {
        float lasttime;
        Vector3 position;

        public SmokePlumeParticleSystem(Device device, Texture smokePlume_texture, Matrix world)
            : base(device, smokePlume_texture)
        {
            this.position = new Vector3(world.M41, world.M42, world.M43) * (1f / world.M44);
        }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "smoke";

            settings.MaxParticles = 600;

            settings.Duration = TimeSpan.FromSeconds(10);

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 15;

            settings.MinVerticalVelocity = 10;
            settings.MaxVerticalVelocity = 20;

            // Create a wind effect by tilting the gravity vector sideways.
            settings.Gravity = new Vector3(-20, -5, 0);

            settings.EndVelocity = 0.75f;

            settings.MinRotateSpeed = -1;
            settings.MaxRotateSpeed = 1;

            settings.MinStartSize = 5;
            settings.MaxStartSize = 10;

            settings.MinEndSize = 50;
            settings.MaxEndSize = 200;
        }

        public override void Update(float time)
        {
            if (time - lasttime > 10)
            {
                AddParticleSystemAtOffSet(position, 1, Vector3.Empty);

                lasttime = time;
            }

            base.Update(time);

        }


    }
}
