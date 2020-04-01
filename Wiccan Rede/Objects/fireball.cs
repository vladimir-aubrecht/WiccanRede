using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using WiccanRede.Graphics.Scene;
using WiccanRede.Objects;
using WiccanRede.Objects.ParticleSystem;
using WiccanRede.Objects.ParticleSystem.SpecializedParticle;

namespace WiccanRede.Objects
{
    class Fireball : Lights //: GeneralObject
    {
        private static int FireballCount = 0;
        private int FireballNumber;
        ParticleSystem.ParticleSystem fireParticle;
        float lasttime;
        Vector3 direction;
        Vector3 position;
        float lifetime = 5000;
        bool collision = false;
        String name;
        bool enabled = true;

        WiccanRede.AI.ActionInfo actionInfo;
        Game.GameLogic logic;

        public static Texture fireballTexture;
        public static List<SceneManager.SceneObject> objects;

        public Fireball(String name, Device device, Vector3 position, Vector3 direction, WiccanRede.AI.ActionInfo infoAction, Game.GameLogic logic)
            : base(null, Matrix.Translation(position), null)
        {
            base.SetType(WiccanRede.Graphics.Scene.LightType.Point);

            this.actionInfo = infoAction;
            this.logic = logic;
            this.direction = Vector3.Normalize(direction) * 600f;
            this.position = position;
            this.name = name;

            if (fireballTexture == null)
            {
                fireballTexture = TextureLoader.FromFile(device, @"Resources/Textures/Fire.dds");
            }

            objects = Graphics.GraphicCore.GetCurrentSceneManager().GetObjectsOfType(Type.GetType("WiccanRede.Objects.Player"), false);

            fireParticle = new FireBallParticleSystem(device, fireballTexture);
        }

        public override Vector4 GetLightPosition()
        {
            return new Vector4(position.X, position.Y, position.Z, 1);
        }

        public String GetFireName()
        {
            return "fireball" + FireballNumber;
        }

        public override bool isEnable()
        {
            return this.enabled;
        }

        public override void InitShaderValue(Effect effect)
        {
            base.InitShaderValue(effect);

            WiccanRede.Graphics.GraphicCore.GetCurrentSceneManager().AddObject("fireball" + Fireball.FireballCount, fireParticle, "Particles", false);
            FireballNumber = FireballCount;

            Fireball.FireballCount++;
        }

        public override void Update(float time)
        {
            float frametime = time - lasttime;

            if (lifetime < -1500)
            {
                fireParticle.Dispose();
                this.Dispose();
                return;
            }

            if (lifetime < 0)
            {
                lifetime -= frametime;
                return;
            }

            Vector3 frametimedirection = frametime * direction;

            if (!collision)
            {
                WiccanRede.AI.IWalkable terrain = WiccanRede.Graphics.CameraDriver.GetAttachedTerain();

                if (terrain.IsPositionOnTerainBlocked(position))
                {
                    System.Drawing.Point coords = terrain.Get2DMapPosition(position);
                    float height = ((Terrain)terrain).GetHeightFromCollissionMap(coords.X, coords.Y);

                    if (position.Y <= height)
                    {
                        collision = true;
                        this.enabled = false;
                        lifetime = 100;
                        return;
                    }
                }

                foreach (SceneManager.SceneObject obj in objects)
                {
                    if (obj.name.StartsWith(this.name))
                        continue;

                    bool col = false;


                    if (obj.generalObject is Objects.Player)
                    {
                        bool sphereCollission = obj.generalObject.ComputeSphereCollission(position) ||
                                            obj.generalObject.ComputeSphereCollission(position + frametimedirection * 0.0005f) ||
                                            obj.generalObject.ComputeSphereCollission(position + frametimedirection * 0.00025f) ||
                                            obj.generalObject.ComputeSphereCollission(position + frametimedirection * 0.000125f) ||
                                            obj.generalObject.ComputeSphereCollission(position + frametimedirection * 0.0000625f) ||
                                            obj.generalObject.ComputeSphereCollission(position + frametimedirection * 0.00003125f);

                        col = sphereCollission;
                    }
                    else
                    {

                        bool boxCollission = obj.generalObject.ComputeBoxCollission(position) ||
                                            obj.generalObject.ComputeBoxCollission(position + frametimedirection * 0.0005f) ||
                                            obj.generalObject.ComputeBoxCollission(position + frametimedirection * 0.00025f) ||
                                            obj.generalObject.ComputeBoxCollission(position + frametimedirection * 0.000125f) ||
                                            obj.generalObject.ComputeBoxCollission(position + frametimedirection * 0.0000625f) ||
                                            obj.generalObject.ComputeBoxCollission(position + frametimedirection * 0.00003125f);

                        col = boxCollission;
                    }

                    if (col)
                    {

                        if (logic != null)
                        {
                            if (obj.name.StartsWith("Hrac"))
                            {
                                logic.FireballHit("Player");
                            }
                            else
                            {
                                logic.FireballHit(obj.name, actionInfo);
                            }
                        }

                        collision = true;
                        this.enabled = false;
                        lifetime = 100;

                        break;
                    }

                }
            }

            base.Update(time);


            if (lasttime == 0)
            {
                lasttime = time;
                return;
            }

            if (frametime > 20)
            {
                int particlesInFrame = 25;

                int count = (int)(frametime * 0.05);

                position += frametime * direction * 0.001f;

                for (int i = 0; i < count; i++)
                {
                    fireParticle.AddParticleSystemAtOffSet(new Vector3(position.X, position.Y, position.Z), particlesInFrame, new Vector3(0, 1, 0));
                }

                lifetime -= frametime;

                lasttime = time;
            }

            fireParticle.Update(time);
        }

        public override float GetSphereRadius()
        {
            return 76f;
        }

    }
}
