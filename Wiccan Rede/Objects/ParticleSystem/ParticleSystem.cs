#region File Description
//-----------------------------------------------------------------------------
// ParticleSystem.cs
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
using WiccanRede.Graphics;
using WiccanRede.Graphics.Scene;
#endregion

namespace WiccanRede.Objects.ParticleSystem
{
    /// <summary>
    /// The main component in charge of displaying particles.
    /// </summary>
    public abstract class ParticleSystem : GeneralObject
    {
        #region Fields

        Vector3 currentPosition = new Vector3();
        Vector3 lastPosition = new Vector3();
        Vector3 startMovePosition = new Vector3();
        bool startMove = false;

        ParticleSettings settings = new ParticleSettings();


        EffectHandle effectViewParameter;
        EffectHandle effectProjectionParameter;
        EffectHandle effectViewportHeightParameter;
        EffectHandle effectTimeParameter;
        EffectHandle effectGravity;
        EffectHandle effectCorrection;
        EffectHandle effectMinColor;
        EffectHandle effectMaxColor;
        EffectHandle effectStartSize;
        EffectHandle effectEndSize;


        ParticleVertex[] particles;


        VertexBuffer vertexBuffer;


        VertexDeclaration vertexDeclaration;


        int firstActiveParticle;
        int firstNewParticle;
        int firstFreeParticle;
        int firstRetiredParticle;


        float currentTime;
        float lasttime = 0;
        float frametime = 0;

        int drawCounter;


        static Random random = new Random();

        Device device;

        #endregion

        #region Initialization

        protected ParticleSystem(Device device, Texture texture)
            : base(null, Matrix.Identity, new Texture[] { texture }, null, null, null)
        {
            this.device = device;

            InitializeSettings(settings);
            particles = new ParticleVertex[settings.MaxParticles];

            vertexDeclaration = new VertexDeclaration(device, ParticleVertex.VertexElements);

            ReInit();

            base.SetUseAlphaBlending(true);
        }


        public override void ReInit()
        {
            vertexBuffer = new VertexBuffer(typeof(ParticleVertex), particles.Length, device, Usage.WriteOnly | Usage.Points | Usage.Dynamic, ParticleVertex.Format, Pool.Default);
        }

        public override void Releasse()
        {
            base.Releasse();
            if (vertexBuffer != null && !vertexBuffer.Disposed)
            {
                vertexBuffer.Dispose();
            }
        }

        protected abstract void InitializeSettings(ParticleSettings settings);

        public override void InitShaderValue(Effect effect)
        {
            base.InitShaderValue(effect);

            // Look up shortcuts for parameters that change every frame.
            effectViewParameter = effect.GetParameter(null, "View");
            effectProjectionParameter = effect.GetParameter(null, "Projection");
            effectViewportHeightParameter = effect.GetParameter(null, "ViewportHeight");
            effectTimeParameter = effect.GetParameter(null, "CurrentTime");
            effectGravity = effect.GetParameter(null, "Gravity");
            effectCorrection = effect.GetParameter(null, "Correction");
            effectMinColor = effect.GetParameter(null, "MinColor");
            effectMaxColor = effect.GetParameter(null, "MaxColor");
            effectStartSize = effect.GetParameter(null, "StartSize");
            effectEndSize = effect.GetParameter(null, "EndSize");

            // Set the values of parameters that do not change.
            effect.SetValue("Duration", (float)settings.Duration.TotalSeconds);
            effect.SetValue("DurationRandomness", settings.DurationRandomness);
            effect.SetValue(effectGravity, new Vector4(settings.Gravity.X, settings.Gravity.Y, settings.Gravity.Z, 1));
            effect.SetValue("EndVelocity", settings.EndVelocity);

            effect.SetValue("RotateSpeed", new Vector4(settings.MinRotateSpeed, settings.MaxRotateSpeed, 1, 1));
            effect.SetValue("Texture", base.GetTexturesColor0()[0]);


            effect.Technique = "Specialized";

        }


        #endregion

        #region Update and Draw

        public override void Update(float time)
        {
            frametime = time - lasttime;
            currentTime += frametime / 1000f;

            RetireActiveParticles();
            FreeRetiredParticles();

            if (firstActiveParticle == firstFreeParticle)
                currentTime = 0;

            if (firstRetiredParticle == firstActiveParticle)
                drawCounter = 0;

            lasttime = time;
        }

        void RetireActiveParticles()
        {
            float particleDuration = (float)settings.Duration.TotalSeconds;

            while (firstActiveParticle != firstNewParticle)
            {
                // Is this particle old enough to retire?
                float particleAge = currentTime - particles[firstActiveParticle].Time;

                if (particleAge < particleDuration)
                    break;

                // Remember the time at which we retired this particle.
                particles[firstActiveParticle].Time = drawCounter;

                // Move the particle from the active to the retired queue.
                firstActiveParticle++;

                if (firstActiveParticle >= particles.Length)
                    firstActiveParticle = 0;
            }
        }

        public override bool isAlphaObject()
        {
            return true;
        }

        void FreeRetiredParticles()
        {
            while (firstRetiredParticle != firstActiveParticle)
            {
                // Has this particle been unused long enough that
                // the GPU is sure to be finished with it?
                int age = drawCounter - (int)particles[firstRetiredParticle].Time;

                // The GPU is never supposed to get more than 2 frames behind the CPU.
                // We add 1 to that, just to be safe in case of buggy drivers that
                // might bend the rules and let the GPU get further behind.
                if (age < 3)
                    break;

                // Move the particle from the retired to the free queue.
                firstRetiredParticle++;

                if (firstRetiredParticle >= particles.Length)
                    firstRetiredParticle = 0;
            }
        }

        public override void UpdateShaderValue(Effect effect)
        {
            effect.SetValue(effectViewParameter, Camera.GetCameraInstance().GetMatrixView());
            effect.SetValue(effectProjectionParameter, Camera.GetCameraInstance().GetMatrixProjection());
            effect.SetValue(effectMinColor, settings.MinColor);
            effect.SetValue(effectMaxColor, settings.MaxColor);
            effect.SetValue(effectStartSize, new Vector4(settings.MinStartSize, settings.MaxStartSize, 1, 1));
            effect.SetValue(effectEndSize, new Vector4(settings.MinEndSize, settings.MaxEndSize, 1, 1));

            Vector3 dir = currentPosition;

            /*if (!startMove)
            {
                dir.X = 0;
                dir.Y = 0;
                dir.Z = 0;
            }*/

            effect.SetValue(effectCorrection, new Vector4(dir.X, dir.Y, dir.Z, 1));
            

            if (firstActiveParticle != firstFreeParticle)
            {
                effect.SetValue(effectViewportHeightParameter, device.Viewport.Height);
                effect.SetValue(effectTimeParameter, currentTime);
            }

            base.UpdateShaderValue(effect);
        }
        
        public override void Render(int subset)
        {
            
            if (vertexBuffer == null || vertexBuffer.Disposed)
                return;

            if (firstNewParticle != firstFreeParticle)
            {
                AddNewParticlesToVertexBuffer();
            }

            if (firstActiveParticle != firstFreeParticle)
            {

                SetParticleRenderStates();

                device.SetStreamSource(0, vertexBuffer, 0, ParticleVertex.SizeInBytes);

                device.VertexDeclaration = vertexDeclaration;

                if (firstActiveParticle < firstFreeParticle)
                {
                    device.DrawPrimitives(PrimitiveType.PointList, firstActiveParticle, firstFreeParticle - firstActiveParticle);
                }
                else
                {
                    device.DrawPrimitives(PrimitiveType.PointList, firstActiveParticle, particles.Length - firstActiveParticle);

                    if (firstFreeParticle > 0)
                    {
                        device.DrawPrimitives(PrimitiveType.PointList, 0, firstFreeParticle);
                    }
                }

                device.RenderState.PointSpriteEnable = false;
                device.RenderState.ZBufferWriteEnable = true;
            }

            drawCounter++;
        }

        void AddNewParticlesToVertexBuffer()
        {
            int stride = ParticleVertex.SizeInBytes;

            if (firstNewParticle < firstFreeParticle)
            {
                SetData(firstNewParticle * stride, particles, firstNewParticle, firstFreeParticle - firstNewParticle, stride);
            }
            else
            {
                SetData(firstNewParticle * stride, particles, firstNewParticle, particles.Length - firstNewParticle, stride);

                if (firstFreeParticle > 0)
                {
                    SetData(0, particles, 0, firstFreeParticle, stride);
                }
            }

            firstNewParticle = firstFreeParticle;
        }

        void SetParticleRenderStates()
        {
            // Enable point sprites.
            device.RenderState.PointSpriteEnable = true;
            device.RenderState.PointSizeMax = 256;

            // Set the alpha blend mode.
            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.AlphaBlendOperation = BlendOperation.Add;
            device.RenderState.SourceBlend = settings.SourceBlend;
            device.RenderState.DestinationBlend = settings.DestinationBlend;

            // Set the alpha test mode.
            device.RenderState.AlphaTestEnable = true;
            device.RenderState.AlphaFunction = Compare.Greater;
            device.RenderState.ReferenceAlpha = 0;

            // Enable the depth buffer (so particles will not be visible through
            // solid objects like the ground plane), but disable depth writes
            // (so particles will not obscure other particles).
            device.RenderState.ZBufferEnable = true;
            device.RenderState.ZBufferWriteEnable = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new particle to the system.
        /// </summary>
        public void AddParticle(Vector3 position, Vector3 velocity)
        {
            // Figure out where in the circular queue to allocate the new particle.
            int nextFreeParticle = firstFreeParticle + 1;

            if (nextFreeParticle >= particles.Length)
                nextFreeParticle = 0;

            // If there are no free particles, we just have to give up.
            if (nextFreeParticle == firstRetiredParticle)
                return;

            // Adjust the input velocity based on how much
            // this particle system wants to be affected by it.
            velocity *= settings.EmitterVelocitySensitivity;

            // Add in some random amount of horizontal velocity.
            float horizontalVelocity = Lerp(settings.MinHorizontalVelocity,
                                                       settings.MaxHorizontalVelocity,
                                                       (float)random.NextDouble());

            double horizontalAngle = random.NextDouble() * 2f * (float)Math.PI;

            velocity.X += horizontalVelocity * (float)Math.Cos(horizontalAngle);
            velocity.Z += horizontalVelocity * (float)Math.Sin(horizontalAngle);

            // Add in some random amount of vertical velocity.
            velocity.Y += Lerp(settings.MinVerticalVelocity,
                                          settings.MaxVerticalVelocity,
                                          (float)random.NextDouble());

            // Choose four random control values. These will be used by the vertex
            // shader to give each particle a different size, rotation, and color.
            Color randomValues = Color.FromArgb((byte)random.Next(255),
                                           (byte)random.Next(255),
                                           (byte)random.Next(255),
                                           (byte)random.Next(255));

            // Fill in the particle vertex structure.
            particles[firstFreeParticle].Position = position;
            particles[firstFreeParticle].Velocity = velocity;
            particles[firstFreeParticle].Random = randomValues.ToArgb();
            particles[firstFreeParticle].Time = currentTime;

            firstFreeParticle = nextFreeParticle;
        }

        public override Vector3 GetPosition()
        {
            return currentPosition;
        }

        public override float GetSphereRadius()
        {
            return 9.5f;
        }

        #endregion

        void SetData(int offsetInBytes, ParticleVertex[] data, int startIndex, int ElementCount, int stride)
        {
            if (vertexBuffer != null && !vertexBuffer.Disposed)
            {
                //vertexBuffer.SetData(data, 0, LockFlags.NoOverwrite);
                GraphicsStream gsm = vertexBuffer.Lock(offsetInBytes, ElementCount * stride, LockFlags.NoOverwrite);

                int end = startIndex + ElementCount;

                ParticleVertex[] d = new ParticleVertex[ElementCount];

                for (int i = startIndex, t=0; i < end; i++, t++)
                {
                    d[t] = data[i];
                }

                gsm.Write(d);

                vertexBuffer.Unlock();
            }
        }

        float Lerp(float A, float B, float t)
        {
            return (1 - t) * A + t * B;
        }

        public void AddParticleSystemAtOffSet(Vector3 pos, int particleCount, Vector3 velocity)
        {
            //world = Matrix.Translation(pos) * world;

            //Vector3 position = new Vector3();
            Vector3 position = pos;//Vector3.TransformCoordinate(position, world);

            if (lastPosition != currentPosition)
            {
                if (!startMove)
                {
                    startMove = true;
                    startMovePosition = position;
                }
            }
            else
                startMove = false;

            lastPosition = currentPosition;
            currentPosition = position;

            for (int t = 0; t < particleCount; t++)
                this.AddParticle(position, velocity);
        }
    }
}
