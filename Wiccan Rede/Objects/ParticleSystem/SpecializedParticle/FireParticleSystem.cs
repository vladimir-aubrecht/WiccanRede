#region File Description
//-----------------------------------------------------------------------------
// FireParticleSystem.cs
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
    /// Custom particle system for creating a flame effect.
    /// </summary>
    class FireParticleSystem : ParticleSystem
    {
        public FireParticleSystem(Device device, Texture fire_texture)
            : base(device, fire_texture)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "fire";

            settings.MaxParticles = 2400;

            settings.Duration = TimeSpan.FromSeconds(1.8);  //1.8

            settings.DurationRandomness = 1f;

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 1.0f;

            settings.MinVerticalVelocity = -2;
            settings.MaxVerticalVelocity = 2;

            // Set gravity upside down, so the flames will 'fall' upward.
            settings.Gravity = new Vector3(0, 6.5f, 0);

            settings.MinColor = ColorValue.FromColor(Color.FromArgb(10, 255, 255, 255));
            settings.MaxColor = ColorValue.FromColor(Color.FromArgb(40, 255, 255, 255));

            settings.MinStartSize = 1f;
            settings.MaxStartSize = 2f;

            settings.MinEndSize = 2;
            settings.MaxEndSize = 8;

            // Use additive blending.
            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;
        }
    }
}
