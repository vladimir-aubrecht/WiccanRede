#region File Description
//-----------------------------------------------------------------------------
// ProjectileTrailParticleSystem.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
#endregion

namespace WiccanRede.Objects.ParticleSystem.SpecializedParticle
{
    /// <summary>
    /// Custom particle system for leaving smoke trails behind the rocket projectiles.
    /// </summary>
    class ProjectileTrailParticleSystem : ParticleSystem
    {
        public ProjectileTrailParticleSystem(Device device, Texture projectile_texture)
            : base(device, projectile_texture)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "smoke";

            settings.MaxParticles = 1000;

            settings.Duration = TimeSpan.FromSeconds(3);

            settings.DurationRandomness = 1.5f;

            settings.EmitterVelocitySensitivity = 0.1f;

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 1;

            settings.MinVerticalVelocity = -1;
            settings.MaxVerticalVelocity = 1;

            settings.MinColor = ColorValue.FromColor(Color.FromArgb(255, 64, 96, 128));
            settings.MaxColor = ColorValue.FromColor(Color.FromArgb(128, 255, 255, 255));

            settings.MinRotateSpeed = -4;
            settings.MaxRotateSpeed = 4;

            settings.MinStartSize = 2;
            settings.MaxStartSize = 4;

            settings.MinEndSize = 5;
            settings.MaxEndSize = 15;
        }
    }
}
