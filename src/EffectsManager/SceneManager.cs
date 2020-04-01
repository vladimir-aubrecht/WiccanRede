using System;
using System.Collections.Generic;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede.Graphics.Scene
{
    public class SceneManager : IDisposable
    {
        public enum DetailLevel
        {
            Low = 0,
            Medium = 1,
            High = 2,
            UltraHigh = 3
        };

        #region Fields

        DetailLevel currentDetail = DetailLevel.Medium;
        float ambient = 0.3f;
        int vertexCount = 0;
        int facesCount = 0;
        int visibleObjectsCount = 0;
        bool enabledComputeVisibility = false;
        bool renderShadowsNonEmitterObjects = true;
        bool enableLights = true;

        Device device;
        ISceneCamera camera;
        Random rnd = new Random();
        float lastUpdateRandomTime = 0f;

        EffectPool effectPool = new EffectPool();
        List<SceneObject> objects = new List<SceneObject>();
        List<SceneEffect> effects = new List<SceneEffect>();
        List<SceneObject> dirLights = new List<SceneObject>();
        List<SceneObject> pointLights = new List<SceneObject>();

        Effect sharedVariablesShader;
        Effect baseShader;
        Effect sprite;
        Effect terrainCollission;
        Effect currentShader;

        #region EffectHandlers
        EffectHandle fxAttributes;
        EffectHandle fxWorld;
        EffectHandle fxWorldIT;
        EffectHandle fxView;
        EffectHandle fxViewI;
        EffectHandle fxViewProjection;
        EffectHandle fxProjection;
        EffectHandle fxLightViewProjection;
        EffectHandle fxColor_texture0;
        EffectHandle fxColor_texture1;
        EffectHandle fxColor_texture2;
        EffectHandle fxNormal_texture;
        EffectHandle fxShadow_texture;
        EffectHandle fxDirLight;
        EffectHandle fxCameraPosition;
        EffectHandle fxPointLight;
        EffectHandle fxRandom;
        EffectHandle fxInfluences;
        #endregion

        #endregion

        #region Objects

        /// <summary>
        /// Vrati efekt zarazeny ve spravci na zadanym indexu
        /// </summary>
        /// <param name="index">Index chteneho efektu</param>
        /// <returns>Chteny Effect</returns>
        public Effect this[int index]
        {
            get
            {
                if (index < effects.Count)
                    return effects[index].effect;
                else return baseShader;
            }
        }

        /// <summary>
        /// Vrati efekt zarazeny ve spravci pod jmenem
        /// </summary>
        /// <param name="name">Jmeno chteneho efektu</param>
        /// <returns>Chteny Effect</returns>
        public Effect this[String name]
        {
            get
            {
                return GetEffect(name).effect;
            }
        }

        /// <summary>
        /// Struktura sdruzujici informace o shaderech vlozenych do spravce shaderu
        /// </summary>
        public struct SceneEffect
        {
            /// <summary>
            /// Jmeno shaderu
            /// </summary>
            public String name;

            /// <summary>
            /// Konkretni efekt, ktery se vykona
            /// </summary>
            public Effect effect;

            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="name">Jmeno shaderu</param>
            /// <param name="effect">Efekt, ktery se ma vykonat</param>
            public SceneEffect(String name, Effect effect)
            {
                this.name = name;
                this.effect = effect;
            }
        }

        /// <summary>
        /// Trida sdruzujici informace o objektu ve scene
        /// </summary>
        public class SceneObject
        {
            /// <summary>
            /// Jmeno objektu ve scene
            /// </summary>
            public String name;

            /// <summary>
            /// Muze objekt vrhat stiny?
            /// </summary>
            public bool shadowEmitter;

            /// <summary>
            /// objekt, ktery se renderuje, obsahuje geometrii, world matici, popr. dalsi data
            /// </summary>
            public IGeneralObject generalObject;

            /// <summary>
            /// Pokud objekt sviti, tak je zde objekt, ktery poskytne potrebna data (pozici svetla, atp.)
            /// </summary>
            public ISceneLight light;

            /// <summary>
            /// Effect, ktery se pouzije jako defaultni pro rendering v pripade renderingu cele sceny (neni-li explicitne pri volani renderovaci funkce receno jinak)
            /// </summary>
            public Effect currentEffect;

            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="name">Jmeno objektu ve scene</param>
            /// <param name="obj">Vlastni objekt, ktery se renderuje, obsahuje vlastni geometrii, world matici, popr. dalsi data</param>
            /// <param name="effect">Effect, ktery se pouzije jako defaultni pro rendering v pripade renderingu cele sceny (neni-li explicitne pri volani renderovaci funkce receno jinak)</param>
            public SceneObject(String name, IGeneralObject obj, Effect effect)
            {
                this.name = name;
                this.generalObject = obj;
                this.currentEffect = effect;
                this.light = null;
                this.shadowEmitter = false;
            }

            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="name">Jmeno objektu ve scene</param>
            /// <param name="obj">Vlastni objekt, ktery se renderuje, obsahuje vlastni geometrii, world matici, popr. dalsi data</param>
            /// <param name="effect">Effect, ktery se pouzije jako defaultni pro rendering v pripade renderingu cele sceny (neni-li explicitne pri volani renderovaci funkce receno jinak)</param>
            /// <param name="light">Pokud objekt sviti, tak je zde objekt, ktery poskytne potrebna data (pozici svetla, atp.)</param>
            public SceneObject(String name, IGeneralObject obj, Effect effect, ISceneLight light)
            {
                this.name = name;
                this.generalObject = obj;
                this.currentEffect = effect;
                this.light = light;
                this.shadowEmitter = false;
            }

            /// <summary>
            /// Pri pouziti toho pretizeni se porovnaji vzdalenosti objektu od kamery
            /// </summary>
            /// <param name="obj1">Prvni objekt</param>
            /// <param name="obj2">Druhy objekt</param>
            /// <returns>Vrati true, pokud je prvni objekt blize kamere</returns>
            public static bool operator <(SceneObject obj1, SceneObject obj2)
            {
                if (obj1.generalObject.GetDistanceFromCamera() < obj2.generalObject.GetDistanceFromCamera())
                    return true;

                return false;
            }

            /// <summary>
            /// Pri pouziti toho pretizeni se porovnaji vzdalenosti objektu od kamery
            /// </summary>
            /// <param name="obj1">Prvni objekt</param>
            /// <param name="obj2">Druhy objekt</param>
            /// <returns>Vrati true, pokud je druhy objekt blize kamere</returns>
            public static bool operator >(SceneObject obj1, SceneObject obj2)
            {
                if (obj1.generalObject.GetDistanceFromCamera() > obj2.generalObject.GetDistanceFromCamera())
                    return true;

                return false;
            }

            /// <summary>
            /// Pri pouziti toho pretizeni se porovnaji vzdalenosti objektu od kamery
            /// </summary>
            /// <param name="obj1">Prvni objekt</param>
            /// <param name="obj2">Druhy objekt</param>
            /// <returns>Vrati true, pokud je prvni objekt blize kamere nebo stejne daleko</returns>
            public static bool operator <=(SceneObject obj1, SceneObject obj2)
            {
                if (obj1.generalObject.GetDistanceFromCamera() <= obj2.generalObject.GetDistanceFromCamera())
                    return true;

                return false;
            }

            /// <summary>
            /// Pri pouziti toho pretizeni se porovnaji vzdalenosti objektu od kamery
            /// </summary>
            /// <param name="obj1">Prvni objekt</param>
            /// <param name="obj2">Druhy objekt</param>
            /// <returns>Vrati true, pokud je druhy objekt blize kamere nebo stejne daleko</returns>
            public static bool operator >=(SceneObject obj1, SceneObject obj2)
            {
                if (obj1.generalObject.GetDistanceFromCamera() >= obj2.generalObject.GetDistanceFromCamera())
                    return true;

                return false;
            }

            /// <summary>
            /// Metoda vrati nazev hrace, jeho pozici a jeho vzdalenost od kamery
            /// </summary>
            /// <returns>Metoda vrati nazev hrace, jeho pozici a jeho vzdalenost od kamery</returns>
            public override string ToString()
            {
                return "Name: " + name + " Position: " + generalObject.GetBoundingBoxCenter() + " Distance: " + generalObject.GetDistanceFromCamera();
            }
        }

        #endregion

        #region Cleaning Methods

        /// <summary>
        /// Postara se o sklizeni vsech vnitrnich zalezitosti a dat, ktere se renderovali
        /// tzn. po zavolani teto funkce budou vsechny objekty, ktere se nachazeli ve spravci, vcetne efektu nepoutelne
        /// </summary>
        public void Dispose()
        {
            foreach (SceneObject obj in objects)
            {
                obj.generalObject.Dispose();

                if (obj.currentEffect != null && !obj.currentEffect.Disposed)
                    obj.currentEffect.Dispose();
            }

            foreach (SceneEffect effect in effects)
            {
                if (effect.effect != null && !effect.effect.Disposed)
                    effect.effect.Dispose();
            }

            if (sharedVariablesShader != null && !sharedVariablesShader.Disposed)
                sharedVariablesShader.Dispose();

            if (effectPool != null && !effectPool.Disposed)
                effectPool.Dispose();
        }

        /// <summary>
        /// Po zavolani teto funkce se uklidi vsechny objekty a efekty 
        /// (nepodoba se Clearu u tridy List, tato data nebudou pouze vyhozena z bufferu teto tridy, budou disposnuta)
        /// </summary>
        public void Clear()
        {
            foreach (SceneObject obj in objects)
            {
                obj.generalObject.Dispose();

                if (obj.currentEffect != null && !obj.currentEffect.Disposed)
                    obj.currentEffect.Dispose();
            }

            objects.Clear();

            foreach (SceneEffect effect in effects)
            {
                if (effect.effect != null && !effect.effect.Disposed)
                    effect.effect.Dispose();
            }

            effects.Clear();
            dirLights.Clear();
            pointLights.Clear();

            ShaderInit();
        }

        #endregion

        #region (Re)Init Methods

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="device">Objekt tridy device, pres ktery probiha rendering</param>
        /// <param name="camera">Objekt kamery, ktera se ma pouzit pro rendering</param>
        public SceneManager(Device device, ISceneCamera camera)
        {
            this.device = device;
            this.camera = camera;

            ShaderInit();
        }

        /// <summary>
        /// Provede inicializaci vsech defaultnich shaderu a nachystani handleru pro dalsi pouziti
        /// </summary>
        private void ShaderInit()
        {
            String err = String.Empty;
            sharedVariablesShader = Effect.FromString(device, Properties.Resources.sharedVariables, null, null, ShaderFlags.None, effectPool, out err);
            baseShader = Effect.FromString(device, Properties.Resources.fxbase, null, null, ShaderFlags.None, effectPool, out err);
            sprite = Effect.FromString(device, Properties.Resources.sprite, null, null, ShaderFlags.None, effectPool, out err);
            terrainCollission = Effect.FromString(device, Properties.Resources.terrainCollission, null, null, ShaderFlags.None, effectPool, out err);

            fxAttributes = sharedVariablesShader.GetParameter(null, "attributes");
            fxWorld = sharedVariablesShader.GetParameter(null, "world");
            fxWorldIT = sharedVariablesShader.GetParameter(null, "worldIT");
            fxView = sharedVariablesShader.GetParameter(null, "View");
            fxViewI = sharedVariablesShader.GetParameter(null, "ViewI");
            fxViewProjection = sharedVariablesShader.GetParameter(null, "ViewProjection");
            fxProjection = sharedVariablesShader.GetParameter(null, "Projection");
            fxRandom = sharedVariablesShader.GetParameter(null, "random");
            fxInfluences = sharedVariablesShader.GetParameter(null, "influences");
            fxLightViewProjection = sharedVariablesShader.GetParameter(null, "lightViewProjection");
            fxColor_texture0 = sharedVariablesShader.GetParameter(null, "color_texture0");
            fxColor_texture1 = sharedVariablesShader.GetParameter(null, "color_texture1");
            fxColor_texture2 = sharedVariablesShader.GetParameter(null, "color_texture2");
            fxNormal_texture = sharedVariablesShader.GetParameter(null, "normal_texture");
            fxShadow_texture = sharedVariablesShader.GetParameter(null, "shadow_texture");
            fxDirLight = sharedVariablesShader.GetParameter(null, "dirLight");
            fxCameraPosition = sharedVariablesShader.GetParameter(null, "cameraPosition");
            fxPointLight = sharedVariablesShader.GetParameter(null, "pointLight");

            VolumeTexture vt = TextureLoader.FromVolumeFile(device, @"Resources/Textures/noise.dds", 16, 16, 16, 1, Usage.None, Format.A8R8G8B8, Pool.Managed, Filter.Linear, Filter.Linear, 0);
            sharedVariablesShader.SetValue("noise_texture", vt);

            AddEffect("Base", baseShader);
            AddEffect("Sprite", sprite);
            AddEffect("TerrainCollission", terrainCollission);
        }

        /// <summary>
        /// Provede reinicializaci vsech objektu
        /// </summary>
        public void ReInitObjects()
        {
            foreach (SceneObject obj in objects)
                obj.generalObject.ReInit();
        }

        /// <summary>
        /// Metoda uvolni zdroje vsech objektu
        /// </summary>
        public void ReleasseObjects()
        {
            foreach (SceneObject obj in objects)
            {
                obj.generalObject.Releasse();
            }
        }

        /// <summary>
        /// Metoda se postara o vyresetovani pocitadla vertexu a facu
        /// </summary>
        public void ResetCounters()
        {
            vertexCount = 0;
            facesCount = 0;
        }

        #endregion

        #region Object Render

        /// <summary>
        /// Spusti renderovani pres predany shader
        /// </summary>
        /// <param name="time">Aktualni cas v milisekundach, nutne zadat spravne pro animovane objekty</param>
        /// <param name="effect">Effect, pres ktery se bude renderovat</param>
        /// <remarks>Volani teto funkce je nutne ukoncit volanim funkce EndRenderObject, pokud ukonceni nenastane pri dalsim pokusu o rendering je vyhozena vyjimka.</remarks>
        /// <seealso cref="EndRenderObject"/>
        /// <seealso cref="ObjectRender"/>
        public void BeginRenderObject(float time, Effect effect)
        {
            BeginRenderObject(time, effect, camera.GetMatrixView(), camera.GetMatrixProjection());
        }
   
        /// <summary>
        /// Spusti renderovani pres predany shader
        /// </summary>
        /// <param name="time">Aktualni cas v milisekundach, nutne zadat spravne pro animovane objekty</param>
        /// <param name="effect">Effect, pres ktery se bude renderovat</param>
        /// <param name="view">Nastavi view matici, ktera se ma pouzit pro rendering</param>
        /// <param name="projection">Nastavi projekcni matici, ktera se ma pouzit pro rendering</param>
        /// <remarks>Volani teto funkce je nutne ukoncit volanim funkce EndRenderObject, pokud ukonceni nenastane pri dalsim pokusu o rendering je vyhozena vyjimka.</remarks>
        /// <seealso cref="EndRenderObject"/>
        /// <seealso cref="ObjectRender"/>
        public void BeginRenderObject(float time, Effect effect, Matrix view, Matrix projection)
        {
            UpdateSharedVariables(time, view, projection);
            this.currentShader = effect;
            this.currentShader.Begin(FX.DoNotSaveState);
            this.currentShader.BeginPass(0);
        }

        /// <summary>
        /// Metoda slouzi pro rendering jednoho objektu
        /// </summary>
        /// <param name="obj">Objekt, ktery se ma renderovat</param>
        /// <remarks>Metoda je vhodna pro optimalizaci renderingu v pripade, ze chceme renderovat po jednom objektu. 
        /// Pouziti metody je podmineno pouzitim metod BeginRenderObject a EndRenderObject. Metoda si sama nepocita viditelnost a nehlida si ji!</remarks>
        /// <example>
        /// BeginRenderObject(0, effect); 
        /// ObjectRender(obj);
        /// EndRenderObject();
        /// </example>
        /// <seealso cref="BeginRenderObject"/>
        /// <seealso cref="EndRenderObject"/>
        public void RenderObject(IRenderable obj)
        {
            RenderObject(obj, false);
        }

        /// <summary>
        /// Metoda slouzi pro rendering jednoho objektu
        /// </summary>
        /// <param name="obj">Objekt, ktery se ma renderovat</param>
        /// <param name="renderBoundingBox">Ma se renderovat bounding box objektu?</param>
        /// <remarks>Metoda je vhodna pro optimalizaci renderingu v pripade, ze chceme renderovat po jednom objektu. 
        /// Pouziti metody je podmineno pouzitim metod BeginRenderObject a EndRenderObject. Metoda si sama nepocita viditelnost a nehlida si ji!</remarks>
        /// <example>
        /// BeginRenderObject(0, effect); 
        /// ObjectRender(obj);
        /// EndRenderObject();
        /// </example>
        /// <seealso cref="BeginRenderObject"/>
        /// <seealso cref="EndRenderObject"/>
        public void RenderObject(IRenderable obj, bool renderBoundingBox)
        {
            sharedVariablesShader.SetValue(fxWorld, obj.GetMatricesWorld());
            sharedVariablesShader.SetValue(fxWorldIT, obj.GetMatricesWorldIT());
            this.currentShader.CommitChanges();

            RenderGeometry(obj, this.currentShader, renderBoundingBox);
        }

        /// <summary>
        /// Metoda slouzi pro rendering jednoho bounding boxu objektu
        /// </summary>
        /// <param name="obj">Objekt, ktery se ma renderovat</param>
        /// <remarks>Metoda je vhodna pro optimalizaci renderingu v pripade, ze chceme renderovat po jednom objektu. 
        /// Pouziti metody je podmineno pouzitim metod BeginRenderObject a EndRenderObject</remarks>
        /// <example>
        /// BeginRenderObject(0, effect); 
        /// ObjectRender(obj);
        /// EndRenderObject();
        /// </example>
        /// <seealso cref="BeginRenderObject"/>
        /// <seealso cref="EndRenderObject"/>
        public void RenderObjectBoundingBox(IRenderable obj)
        {
            RenderObject(obj, true);
        }

        /// <summary>
        /// Metoda ukonci rendering zahajeny metodou BeginRenderObject.
        /// </summary>
        /// <seealso cref="BeginRenderObject"/>
        /// <seealso cref="EndRenderObject"/>
        public void EndRenderObject()
        {
            currentShader.EndPass();
            currentShader.End();
            currentShader = null;
        }

        #endregion

        #region Scene Render

        /// <summary>
        /// Metoda vyrenderuje celou scenu pres zvoleny shader
        /// </summary>
        /// <param name="time">Aktualni cas v milisekundach, nutne zadat spravne pro animovane objekty</param>
        /// <param name="effect">Shader, pres ktery se ma scena renderovat</param>
        /// <remarks>Metoda nekontroluje, zda je objekt viden, renderuje vse. U metody se take nepocita s zadnym osvetlovacim modelem, atp. a proto funkce nema podporu techto specialnich shaderu.</remarks>
        public void RenderSceneBasic(float time, Effect effect)
        {
            RenderSceneBasic(time, effect, camera.GetMatrixView(), camera.GetMatrixProjection(), false, false);
        }

        /// <summary>
        /// Metoda vyrenderuje celou scenu pres zvoleny shader
        /// </summary>
        /// <param name="time">Aktualni cas v milisekundach, nutne zadat spravne pro animovane objekty</param>
        /// <param name="effect">Shader, pres ktery se ma scena renderovat</param>
        /// <param name="renderBoundingBoxes">Maji se pro rendering pouzit bounding boxy?</param>
        /// <param name="computeVisibility">Ma se pocitat viditelnost?</param>
        /// <remarks>U metody se take nepocita s zadnym osvetlovacim modelem, atp. a proto funkce nema podporu techto specialnich shaderu.</remarks>
        public void RenderSceneBasic(float time, Effect effect, bool renderBoundingBoxes, bool computeVisibility)
        {
            RenderSceneBasic(time, effect, camera.GetMatrixView(), camera.GetMatrixProjection(), renderBoundingBoxes, computeVisibility);
        }

        /// <summary>
        /// Metoda vyrenderuje celou scenu pres zvoleny shader
        /// </summary>
        /// <param name="time">Aktualni cas v milisekundach, nutne zadat spravne pro animovane objekty</param>
        /// <param name="effect">Shader, pres ktery se ma scena renderovat</param>
        /// <param name="view">Nastavi view matici, se kterou se ma renderovat</param>
        /// <param name="projection">Nastavi projection matici, se kterou se ma renderovat</param>
        /// <param name="renderBoundingBoxes">Maji se pro rendering pouzit bounding boxy?</param>
        /// <param name="computeVisibility">Ma se pocitat viditelnost?</param>
        /// <remarks>U metody se take nepocita s zadnym osvetlovacim modelem, atp. a proto funkce nema podporu techto specialnich shaderu.</remarks>
        public void RenderSceneBasic(float time, Effect effect, Matrix view, Matrix projection, bool renderBoundingBoxes, bool computeVisibility)
        {
            UpdateSharedVariables(time, view, projection);

            int passes = effect.Begin(FX.DoNotSaveState);

            effect.BeginPass(0);

            for (int t=0; t < objects.Count; t++)
            {
                SceneObject obj = objects[t];
                if (computeVisibility)
                {
                    if (!ComputeObjectVisibility(obj))
                        continue;
                }
                else
                {
                    obj.generalObject.ResetVisibility();
                }

                if (!obj.shadowEmitter && !renderShadowsNonEmitterObjects)
                    continue;

                IGeneralObject gobj = obj.generalObject;
                gobj.Update(time);

                if (!renderBoundingBoxes)
                {
                    sharedVariablesShader.SetValue(fxWorld, gobj.GetMatricesWorld());
                    sharedVariablesShader.SetValue(fxWorldIT, gobj.GetMatricesWorldIT());
                }
                else
                {
                    sharedVariablesShader.SetValue(fxWorld, gobj.GetMatricesWorldBoundingMesh());
                    sharedVariablesShader.SetValue(fxWorldIT, gobj.GetMatricesWorldITBoundingMesh());
                }

                effect.CommitChanges();

                RenderGeometry(gobj, effect, false, renderBoundingBoxes);

            }

            effect.EndPass();
            effect.End();

        }

        /// <summary>
        /// Metoda vyrenderuje celou scenu
        /// </summary>
        /// <param name="time">Aktualni cas v milisekundach, nutne zadat spravne pro animovane objekty</param>
        /// <remarks>Pri renderingu se pouziva effect, ktery je prirazen ke kazdymu objektu.</remarks>
        public void RenderScene(float time, Effect effect)
        {
            if (effect.GetTechnique("Specialized") != null)
                return;

            ComputeAllObjectDistance();
            UpdateSharedVariables(time);
            ComputeAllObjectVisibility();

            device.Clear(ClearFlags.ZBuffer | ClearFlags.Target, System.Drawing.Color.FromArgb(0x000008), 1f, 0);

            SetBeginSceneRenderStates();
            //device.RenderState.ZBufferWriteEnable = true;
            //device.RenderState.ZBufferFunction = Compare.Less;
            //device.RenderState.SourceBlend = Blend.Zero;
            //device.RenderState.SourceBlend = Blend.Zero;

            //sharedVariablesShader.Begin(FX.DoNotSaveState);
            //sharedVariablesShader.BeginPass(0);
            //RenderAllObjectsGeometry(time, sharedVariablesShader, 0, false, true, true, false);
            //sharedVariablesShader.EndPass();
            //sharedVariablesShader.End();

            //SetBeginSceneRenderStates();


            int passes = effect.Begin(FX.DoNotSaveState);

            effect.BeginPass(0);
            RenderAllObjectsGeometry(time, effect, 0, true, false, false, false);
            effect.EndPass();

            if (enableLights)
            {
                device.RenderState.AlphaBlendEnable = true;

                bool updateRandomNumbers = false;
                if (time - lastUpdateRandomTime > 60)
                {
                    updateRandomNumbers = true;
                    lastUpdateRandomTime = time;
                }


                for (int t = 0; t < pointLights.Count; t++)
                {
                    IGeneralObject light = pointLights[t].generalObject;

                    if (light.isDisposed())
                    {
                        pointLights.RemoveAt(t);
                        t--;
                        continue;
                    }

                    light.Update(time);

                    if (!light.GetComputedVisibility() || !((ISceneLight)light).isEnable())
                        continue;

                    if (updateRandomNumbers)
                    {
                        Vector4 random = new Vector4((float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble(), (float)rnd.NextDouble());
                        sharedVariablesShader.SetValue(fxRandom, random);
                    }

                    ComputeAllObjectDistanceToPosition(pointLights[t].generalObject.GetPosition());

                    Vector4 lp = pointLights[t].light.GetLightPosition();
                    sharedVariablesShader.SetValue(fxPointLight, lp);

                    effect.BeginPass(1);
                    RenderAllObjectsGeometry(time, effect, 1, true, false, true, true);
                    effect.EndPass();
                }
            }

            effect.End();


            device.RenderState.ZBufferWriteEnable = true;
            device.RenderState.ZBufferFunction = Compare.LessEqual;
            device.RenderState.AlphaBlendEnable = false;
            device.RenderState.AlphaTestEnable = false;
        }

        private void SetBeginSceneRenderStates()
        {
            device.RenderState.Clipping = false;
            device.RenderState.AlphaTestEnable = false;
            device.RenderState.AlphaBlendEnable = false;
            device.RenderState.AlphaBlendOperation = BlendOperation.Add;
            device.RenderState.SourceBlend = Blend.One;
            device.RenderState.DestinationBlend = Blend.One;
            device.RenderState.ZBufferEnable = true;
            device.RenderState.StencilEnable = false;
            //device.RenderState.ZBufferWriteEnable = false;
            //device.RenderState.ZBufferFunction = Compare.Equal;
            device.RenderState.ZBufferFunction = Compare.LessEqual;
            device.RenderState.ZBufferWriteEnable = true;
        }


        #endregion

        #region Geometry Render

        /// <summary>
        /// Vyrenderuje geometrii vsech objektu v pozadovanym smeru renderingu
        /// </summary>
        /// <param name="time">Cas od spusteni aplikace v milisekundach</param>
        /// <param name="effect">Shader, pres ktery probiha rendering</param>
        /// <param name="pass">Pruchod, pres ktery probehne rendering</param>
        /// <param name="textured">Maji se objekty texturovat?</param>
        /// <param name="frontToBack">Ma se renderovat od predu do zadu?</param>
        private void RenderAllObjectsGeometry(float time, Effect effect, int pass, bool textured, bool frontToBack, bool skipAlphaObjects, bool skipUpdate)
        {
            SceneObject obj = null;
            if (!frontToBack)
            {
                for (int k = objects.Count - 1; k >= 0; k--)
                {
                    obj = objects[k];

                    if (obj.generalObject.isDisposed())
                    {
                        objects.RemoveAt(k);
                        continue;
                    }

                    bool alphaObject = obj.generalObject.isAlphaObject();

                    if (skipAlphaObjects && alphaObject)
                        continue;

                    if (!CanRenderObject(obj))
                        continue;

                    if (pass >= 1 && obj.generalObject.GetDistanceToPosition()-obj.generalObject.GetSphereRadius() >= 1000)
                        continue;
                    
                    Effect tempEffect = effect;

                    if (obj.currentEffect != null)
                    {
                        effect.EndPass();
                        effect.End();
                        tempEffect = obj.currentEffect;
                        device.RenderState.ZBufferWriteEnable = true;
                        device.RenderState.ZBufferFunction = Compare.LessEqual;
                        int passes = tempEffect.Begin(FX.DoNotSaveState);

                        if (pass < passes)
                            tempEffect.BeginPass(pass);
                        else
                        {
                            tempEffect.End();
                            SetBeginSceneRenderStates();
                            effect.Begin(FX.DoNotSaveState);
                            effect.BeginPass(pass);
                            continue;
                        }
                    }

                    UpdateObjectVariables(time, tempEffect, obj, skipUpdate);
                    RenderGeometry(obj.generalObject, tempEffect, textured, false);

                    if (effect != tempEffect)
                    {
                        tempEffect.EndPass();
                        tempEffect.End();
                        
                        SetBeginSceneRenderStates();
                        effect.Begin(FX.DoNotSaveState);
                        effect.BeginPass(pass);
                    }
                }
            }
            else
            {
                for (int k = 0; k < objects.Count; k++)
                {
                    obj = objects[k];

                    if (obj.generalObject.isDisposed())
                    {
                        objects.RemoveAt(k);

                        k--;
                        continue;
                    }

                    bool alphaObject = obj.generalObject.isAlphaObject();

                    if (skipAlphaObjects && alphaObject)
                        continue;

                    if (!CanRenderObject(obj))
                        continue;

                    if (pass >= 1 && obj.generalObject.GetDistanceToPosition() >= 1000)
                        continue;

                    Effect tempEffect = effect;

                    if (obj.currentEffect != null)
                    {
                        effect.EndPass();
                        effect.End();
                        tempEffect = obj.currentEffect;
                        
                        device.RenderState.ZBufferWriteEnable = true;
                        device.RenderState.ZBufferFunction = Compare.LessEqual;

                        int passes = tempEffect.Begin(FX.DoNotSaveState);

                        if (pass < passes)
                            tempEffect.BeginPass(pass);
                        else
                        {
                            tempEffect.End();
                            SetBeginSceneRenderStates();
                            effect.Begin(FX.DoNotSaveState);
                            effect.BeginPass(pass);
                            continue;
                        }
                    }

                    UpdateObjectVariables(time, tempEffect, obj, skipUpdate);
                    RenderGeometry(obj.generalObject, tempEffect, textured, false);

                    if (effect != tempEffect)
                    {
                        tempEffect.EndPass();
                        tempEffect.End();

                        SetBeginSceneRenderStates();

                        effect.Begin(FX.DoNotSaveState);
                        effect.BeginPass(pass);
                    }
                }
            }
        }

        /// <summary>
        /// Zkontroluje, zda muze byt objekt renderovan
        /// </summary>
        /// <param name="obj">Objekt, ktery se bude renderovat</param>
        /// <returns>Vrati true, pokud se objekt muze renderovat</returns>
        private bool CanRenderObject(SceneObject obj)
        {
            if (!obj.shadowEmitter && !renderShadowsNonEmitterObjects)
                return false;

            return obj.generalObject.GetComputedVisibility();
        }

        /// <summary>
        /// Metoda vyrenderuje vsechny subsety objektu (nebo boundix boxu), popr. postara se o nahrani textur do shaderu
        /// </summary>
        /// <param name="obj">Objekt, jehoz geometrie se ma renderovat</param>
        /// <param name="textured">Ma se objekt texturovat?</param>
        /// <param name="effect">Effect, pres ktery se geometrie bude renderovat</param>
        /// <param name="pass">Pruchod, ktery se pro rendering pouzije</param>
        /// <param name="renderBoundingBox">Ma se renderovat bounding box?</param>
        private void RenderGeometry(IGeneralObject obj, Effect effect, int pass, bool textured, bool renderBoundingBox)
        {
            effect.BeginPass(pass);
            RenderGeometry(obj, effect, textured, renderBoundingBox);
            effect.EndPass();
        }

        /// <summary>
        /// Metoda vyrenderuje vsechny subsety objektu (nebo boundix boxu), popr. postara se o nahrani textur do shaderu
        /// </summary>
        /// <param name="obj">Objekt, jehoz geometrie se ma renderovat</param>
        /// <param name="textured">Ma se objekt texturovat?</param>
        /// <param name="effect">Effect, pres ktery se geometrie bude renderovat</param>
        /// <param name="renderBoundingBox">Ma se renderovat bounding box?</param>
        private void RenderGeometry(IGeneralObject obj, Effect effect, bool textured, bool renderBoundingBox)
        {
            if (!renderBoundingBox)
            {
                int subsets = obj.GetSubsetCount();
                for (int subset = 0; subset < subsets; subset++)
                {
                    if (textured)
                        ApplyTextures(obj, effect, subset);

                    obj.UpdateSubset(subset);
                    UpdateSubsetVariables(obj, effect);

                    obj.Render(subset);
                }

                vertexCount += obj.GetVertexCount();
                facesCount += obj.GetFacesCount();

            }
            else
            {
                obj.RenderBoundingSphereMesh();

                vertexCount += 8;
                facesCount += 12;
            }
        }

        /// <summary>
        /// Metoda vyrenderuje vsechny subsety objektu (nebo boundix boxu)
        /// </summary>
        /// <param name="obj">Objekt, jehoz geometrie se ma renderovat</param>
        /// <param name="renderBoundingBox">Ma se renderovat bounding box?</param>
        private void RenderGeometry(IRenderable obj, Effect effect, bool renderBoundingBox)
        {
            if (!renderBoundingBox)
            {
                int subsets = obj.GetSubsetCount();
                for (int subset = 0; subset < subsets; subset++)
                {
                    UpdateSubsetVariables(obj, effect);
                    obj.Render(subset);

                }

                vertexCount += obj.GetVertexCount();
                facesCount += obj.GetFacesCount();
            }
            else
            {
                obj.RenderBoundingSphereMesh();

                vertexCount += 8;
                facesCount += 12;
            }
        }

        #endregion

        #region Updates

        /// <summary>
        /// Updatuje data pro nutna k updatu u kazdyho subsetu
        /// </summary>
        /// <param name="gobj">Object, pro ktery se budou updatovat data</param>
        private void UpdateSubsetVariables(IRenderable gobj, Effect effect)
        {
            if (gobj.GetMatrixWorldCount() > 1)
            {
                sharedVariablesShader.SetValue(fxWorld, gobj.GetMatricesWorld());
                sharedVariablesShader.SetValue(fxWorldIT, gobj.GetMatricesWorldIT());
            }

            effect.CommitChanges();
        }

        /// <summary>
        /// Z aktualizuje hodnoty v shadery, ktere jsou specificke pro nejaky objekt a zavola metody pro update, atp. u objektu
        /// </summary>
        /// <param name="time">Cas od spusteni aplikace v milisekundach</param>
        /// <param name="effect">Shader, ktery je pouzit pro rendering objektu</param>
        /// <param name="obj">Objekt, ktery se bude renderovat</param>
        private void UpdateObjectVariables(float time, Effect effect, SceneObject obj, bool skipUpdate)
        {
            IGeneralObject gobj = obj.generalObject;

            if (!skipUpdate)
                gobj.Update(time);
            
            gobj.UpdateShaderValue(effect);

            int matrixWorldCount = gobj.GetMatrixWorldCount();
            
            sharedVariablesShader.SetValue(fxInfluences, matrixWorldCount);

            if (matrixWorldCount <= 1)
            {
                sharedVariablesShader.SetValue(fxWorld, gobj.GetMatricesWorld());
                sharedVariablesShader.SetValue(fxWorldIT, gobj.GetMatricesWorldIT());
            }

            //Bug - Nedela LOD na hrad (provizorni reseni, nez bude spravny model hradu)
            if (obj.name == "Hrad" || obj.name == "More")
            {
                obj.generalObject.EnableLOD(false);
                obj.generalObject.SetObjectQuality(1f);
            }

            gobj.ApplyLOD();

        }

        /// <summary>
        /// Metoda nahraje do shaderu sdilene promene a vynuluje citace vertexu a facu
        /// </summary>
        /// <param name="time">Aktualni cas v milisekundach, nutne zadat spravne pro animovane objekty</param>
        private void UpdateSharedVariables(float time)
        {
            UpdateSharedVariables(time, camera.GetMatrixView(), camera.GetMatrixProjection());
        }

        /// <summary>
        /// Metoda nahraje do shaderu sdilene promene a vynuluje citace vertexu a facu
        /// </summary>
        /// <param name="time">Aktualni cas v milisekundach, nutne zadat spravne pro animovane objekty</param>
        /// <param name="view">Nastavi view matici, se kterou se ma renderovat</param>
        /// <param name="projection">Nastavi projection matici, se kterou se ma renderovat</param>
        private void UpdateSharedVariables(float time, Matrix view, Matrix projection)
        {
            if (currentShader != null)
                throw new Exception("Je nutne pred zahajenim noveho renderingu nejdrive ukoncit stavajici rendering");

            SortBuffers();

            Vector3 camPos = camera.GetVector3Position();

            sharedVariablesShader.SetValue(fxView, view);
            sharedVariablesShader.SetValue(fxViewI, Matrix.Invert(view));
            sharedVariablesShader.SetValue(fxViewProjection, view * projection);
            sharedVariablesShader.SetValue(fxAttributes, new Vector4(time, ambient, 1, 1));
            sharedVariablesShader.SetValue(fxProjection, projection);
            sharedVariablesShader.SetValue(fxCameraPosition, new Vector4(camPos.X, camPos.Y, camPos.Z, 1) );

            if (dirLights.Count > 0)
            sharedVariablesShader.SetValue(fxDirLight, dirLights[0].light.GetLightPosition());

        }

        /// <summary>
        /// Provede serazeni objektu pro rendering od nejblizsich po nejvzdalenejsi
        /// </summary>
        private void SortBuffers()
        {
            SceneObject terrain = null;
            int itemsCount = objects.Count;
            int itemsCountMinusOne = itemsCount - 1;

            bool notSorted = !objects[itemsCountMinusOne].generalObject.GetIsEveryWhere();

            if (notSorted)
            {

                for (int index = itemsCountMinusOne; index > 0; index--)
                {
                    if (objects[index].generalObject.GetIsEveryWhere())
                    {
                        terrain = objects[index];
                        objects.RemoveAt(index);
                        break;
                    }
                }

                objects.Add(terrain);
            }


            for (int t = 0; t < itemsCountMinusOne - 1; t++)
            {
                for (int k = t + 1; k < itemsCountMinusOne; k++)
                {
                    if (objects[k] < objects[t])
                    {
                        SceneObject obj = objects[k];
                        objects[k] = objects[t];
                        objects[t] = obj;
                        k--;
                    }
                }
            }

        }

        /// <summary>
        /// Metoda provede upload textur do shaderu
        /// </summary>
        /// <param name="color_textures0">Ukazatel na pole nultyho bufferu textur barev</param>
        /// <param name="color_textures1">Ukazatel na pole prvniho bufferu textur barev</param>
        /// <param name="color_textures2">Ukazatel na pole druhyho bufferu textur barev</param>
        /// <param name="normal_textures">Ukazatel na pole textur normal</param>
        /// <param name="subset">Index textury, ktera se ma nahrat do graficke karty</param>
        /// <remarks>Metoda vraci false i v pripade, ze tato trida do karty naposled uploadovala jiz stejnou texturu</remarks>
        private void ApplyTextures(IGeneralObject gobj, Effect effect, int subset)
        {
            Texture[] color_textures0 = gobj.GetTexturesColor0();
            Texture[] color_textures1 = gobj.GetTexturesColor1();
            Texture[] color_textures2 = gobj.GetTexturesColor2();
            /*Texture[] normal_textures = gobj.GetTexturesNormal();*/

            if (color_textures0 != null)
            {
                if (subset < color_textures0.Length)
                {
                    sharedVariablesShader.SetValue(fxColor_texture0, color_textures0[subset]);
                }
            }
            else
                sharedVariablesShader.SetValue(fxColor_texture0, (Texture)null);

            if (color_textures1 != null)
            {
                if (subset < color_textures1.Length)
                {
                    sharedVariablesShader.SetValue(fxColor_texture1, color_textures1[subset]);
                }
            }
            else
                sharedVariablesShader.SetValue(fxColor_texture1, (Texture)null);

            if (color_textures2 != null)
            {
                if (subset < color_textures2.Length)
                    sharedVariablesShader.SetValue(fxColor_texture2, color_textures2[subset]);
            }
            else
                sharedVariablesShader.SetValue(fxColor_texture2, (Texture)null);

            /*if (normal_textures != null)
            {
                if (subset < normal_textures.Length)    //zatim neni potreba, kvuli optimalizaci zakomentovano
                ;// sharedVariablesShader.SetValue(fxNormal_texture, normal_textures[subset]);
            }*/

        }

        #endregion

        #region Object Methods

        /// <summary>
        /// Prihodi objekt na rendering
        /// </summary>
        /// <param name="name">Jmeno objektu</param>
        /// <param name="obj">Objekt, ktery se prihodi na rendering</param>
        /// <param name="defaultEffect">Effect, pres ktery se objekt bude defaultne renderovat</param>
        /// <param name="shadowEmitter">Urcuje, zda muze objekt vrhat stiny</param>
        /// <remarks>Metoda je schopna detekovat, zda objekt sviti a podle toho nachystat vse potrebne na nasvetlovani 
        /// (objekt sviti, pokud implementuje ISceneLight)</remarks>
        public void AddObject(String name, IGeneralObject obj, String defaultEffect, bool shadowEmitter)
        {
            AddObject(name, obj, this[defaultEffect], shadowEmitter);
        }

        /// <summary>
        /// Prihodi objekt na rendering
        /// </summary>
        /// <param name="name">Jmeno objektu</param>
        /// <param name="obj">Objekt, ktery se prihodi na rendering</param>
        /// <param name="defaultEffect">Effect, pres ktery se objekt bude defaultne renderovat</param>
        /// <param name="shadowEmitter">Urcuje, zda muze objekt vrhat stiny</param>
        /// <remarks>Metoda je schopna detekovat, zda objekt sviti a podle toho nachystat vse potrebne na nasvetlovani 
        /// (objekt sviti, pokud implementuje ISceneLight)</remarks>
        public void AddObject(String name, IGeneralObject obj, Effect defaultEffect, bool shadowEmitter)
        {
            SceneObject eo;
            if (obj is ISceneLight)
            {
                eo = new SceneObject(name, obj, defaultEffect, obj as ISceneLight);

                if ((obj as ISceneLight).GetType() == LightType.Direction)
                    dirLights.Add(eo);
                else
                    pointLights.Add(eo);
            }
            else
                eo = new SceneObject(name, obj, defaultEffect);

            eo.shadowEmitter = shadowEmitter;
            obj.InitShaderValue(defaultEffect);

            objects.Add(eo);
        }

        /// <summary>
        /// Prihodi objekt na rendering
        /// </summary>
        /// <param name="name">Jmeno objektu</param>
        /// <param name="obj">Objekt, ktery se prihodi na rendering</param>
        /// <param name="defaultEffect">Effect, pres ktery se objekt bude defaultne renderovat</param>
        /// <remarks>Metoda je schopna detekovat, zda objekt sviti a podle toho nachystat vse potrebne na nasvetlovani 
        /// (objekt sviti, pokud implementuje ISceneLight)</remarks>
        public void AddObject(String name, IGeneralObject obj, Effect defaultEffect)
        {
            AddObject(name, obj, defaultEffect, true);
        }

        /// <summary>
        /// Prihodi objekt na rendering
        /// </summary>
        /// <param name="name">Jmeno objektu</param>
        /// <param name="obj">Objekt, ktery se prihodi na rendering</param>
        /// <param name="effect">Jmeno effectu, ktery se bude vyhledavat v jiz prihozenych efektech. Tento efekt se pote pouzije pro rendering.</param>
        /// <remarks>Metoda je schopna detekovat, zda objekt sviti a podle toho nachystat vse potrebne na nasvetlovani 
        /// (objekt sviti, pokud implementuje ISceneLight)</remarks>
        public void AddObject(String name, IGeneralObject obj, String effect)
        {
            AddObject(name, obj, this[effect]);
        }

        /// <summary>
        /// Metoda pro prihozeni efektu do knihovny efektu
        /// </summary>
        /// <param name="name">Jmeno efektu, pod kterym bude efekt zarazen</param>
        /// <param name="effect">Effect, ktery bude prihozen</param>
        public void AddEffect(String name, Effect effect)
        {
            EffectHandle technique = effect.GetTechnique(currentDetail.ToString());

            if (this.currentDetail == DetailLevel.UltraHigh)
                technique = effect.GetTechnique(DetailLevel.High.ToString());

            if (technique != null)
                effect.Technique = technique;

            SceneEffect es = new SceneEffect(name, effect);
            effects.Add(es);
        }

        /// <summary>
        /// Metoda smaze objekt
        /// </summary>
        /// <param name="name">Jmeno objektu, ktery se ma smazat</param>
        /// <returns>Vrati true, pokud byl objekt smazan, false, pokud objekt neexistoval</returns>
        public bool DeleteObject(String name)
        {
            bool finded = false;
            for (int t = 0; t < objects.Count; t++)
            {
                if (objects[t].name == name)
                {
                    finded = true;
                    objects.RemoveAt(t);
                    break;
                }
            }

            for (int t = 0; t < dirLights.Count; t++)
            {
                if (dirLights[t].name == name)
                {
                    finded = true;
                    dirLights.RemoveAt(t);
                    break;
                }
            }

            for (int t = 0; t < pointLights.Count; t++)
            {
                if (pointLights[t].name == name)
                {
                    finded = true;
                    pointLights.RemoveAt(t);
                    break;
                }
            }


            return finded;
        }

        /// <summary>
        /// Metoda smaze objekt
        /// </summary>
        /// <param name="name">Jmeno objektu, ktery se ma smazat</param>
        /// <returns>Vrati true, pokud byl objekt smazan, false, pokud objekt neexistoval</returns>
        public bool DeleteObject(GeneralObject obj)
        {
            bool finded = false;
            for (int t = 0; t < objects.Count; t++)
            {
                if (objects[t].generalObject == obj)
                {
                    finded = true;
                    objects.RemoveAt(t);
                    break;
                }
            }

            for (int t = 0; t < dirLights.Count; t++)
            {
                if (dirLights[t].generalObject == obj)
                {
                    finded = true;
                    dirLights.RemoveAt(t);
                    break;
                }
            }

            for (int t = 0; t < pointLights.Count; t++)
            {
                if (pointLights[t].generalObject == obj)
                {
                    finded = true;
                    pointLights.RemoveAt(t);
                    break;
                }
            }


            return finded;
        }

        #endregion

        #region Getry

        /// <summary>
        /// Metoda vrati effect z knihovny naleznuty pod zadanym jmenem
        /// </summary>
        /// <param name="name">Jmeno effectu</param>
        /// <returns>Effect, ktery se vyhledaval</returns>
        public SceneEffect GetEffect(String name)
        {
            foreach (SceneEffect obj in effects)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }

            SceneEffect defaultShader = new SceneEffect("Default shader", sharedVariablesShader);
            return defaultShader;
        }

        /// <summary>
        /// Metoda vraci objekt, ktery se vybere na zaklade jmena
        /// </summary>
        /// <param name="name">Jmeno objektu, ktery se ma vratit</param>
        /// <returns>Naleznuty objekt</returns>
        public SceneObject GetObject(String name)
        {
            foreach (SceneObject obj in objects)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }

            return null;
        }

        /// <summary>
        /// Metoda vraci vsechny spravovane objekty
        /// </summary>
        /// <returns>Metoda vraci vsechny spravovane objekty</returns>
        public List<SceneObject> GetAllObjects()
        {
            return objects;
        }

        /// <summary>
        /// Metoda vrati vsechny objekty, ktere se nachazeji na zadanych souradnicich obrazovky
        /// </summary>
        /// <param name="x">X souradnice</param>
        /// <param name="y">Y souradnice</param>
        /// <returns></returns>
        public List<SceneObject> GetAllObjects(int x, int y)
        {
            List<SceneObject> objs = new List<SceneObject>();

            Matrix proj = camera.GetMatrixProjection();
            Matrix view = camera.GetMatrixView();

            foreach (SceneObject obj in objects)
            {

                Matrix world = obj.generalObject.GetMatrixWorldBoundingSphereMesh();

                Vector3 screenPosFar = new Vector3(x, y, 1);
                Vector3 screenPosNear = new Vector3(x, y, 0);

                screenPosFar.Unproject(device.Viewport, proj, view, world);
                screenPosNear.Unproject(device.Viewport, proj, view, world);

                Vector3 dir = screenPosFar - screenPosNear;
                dir.Normalize();

                Mesh boundingMesh = obj.generalObject.GetBoundingModel();

                if (boundingMesh == null)
                    continue;

                if (boundingMesh.Intersect(screenPosNear, dir))
                {
                    objs.Add(obj);
                }

            }

            return objs;
        }

        /// <summary>
        /// Metoda vrati objekty ve scene, jejichz objekty pro rendering (general object) jsou/nejsou zadaneho typu
        /// </summary>
        /// <param name="type">Typ objektu, ktery hledame</param>
        /// <param name="negate">Urcuje, zda chceme objekty zadaneho typu (true), nebo naopak chceme objekty, ktere nejsou zadaneho typu(false)</param>
        /// <returns>Vrati list objektu, ktere jsou/nejsou zadaneho typu</returns>
        public List<SceneObject> GetObjectsOfType(Type type, bool negate)
        {
            List<SceneObject> objs = new List<SceneObject>();

            foreach(SceneObject obj in objects)
            {
                if (!negate)
                {
                    if (obj.generalObject.GetType().Equals(type))
                        objs.Add(obj);
                }
                else
                {
                    if (!obj.generalObject.GetType().Equals(type))
                        objs.Add(obj);
                }
            }

            return objs;
        }

        /// <summary>
        /// Vrati objekt, ktery sviti vlozenej pod konkretnim jmenem
        /// </summary>
        /// <param name="name">Jmeno objektu</param>
        /// <returns>Vrati objekt, ktery sviti vlozenej pod konkretnim jmenem</returns>
        public SceneObject GetLight(String name)
        {
            foreach (SceneObject obj in dirLights)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }

            foreach (SceneObject obj in pointLights)
            {
                if (obj.name == name)
                {
                    return obj;
                }
            }

            return null;
        }

        /// <summary>
        /// Vrati vsechny objekty, ktere sviti smerovym svetlem
        /// </summary>
        /// <returns>Vrati vsechny objekty, ktere sviti smerovym svetlem</returns>
        public List<SceneObject> GetDirectionLights()
        {
            return dirLights;
        }

        /// <summary>
        /// Vrati vsechny objekty, ktere sviti bodovym svetlem
        /// </summary>
        /// <returns>Vrati vsechny objekty, ktere sviti bodovym svetlem</returns>
        public List<SceneObject> GetPointLights()
        {
            return pointLights;
        }

        /// <summary>
        /// Vrati Pool, ktery se pouziva mezi vsemi shadery
        /// </summary>
        /// <remarks>Diky tomuto je mozne vytvaret si mimo tuto tridu Effecty, ktere mohou pristupovat ke sdilenym promenym</remarks>
        /// <returns>Vrati Pool, ktery se pouziva mezi vsemi shadery</returns>
        public EffectPool GetSharedPool()
        {
            return effectPool;
        }

        /// <summary>
        /// Vrati celkovy pocet vykreslenych vertexu od resetovani pocitadla
        /// </summary>
        /// <returns>Pocet vertexu</returns>
        /// <seealso cref="ResetCounters"/>
        public int GetRenderedVertexCount()
        {
            return vertexCount;
        }

        /// <summary>
        /// Vrati celkovy pocet vykreslenych trojuhelniku od resetovani pocitadla
        /// </summary>
        /// <returns>Pocet trojuhelniku</returns>
        /// <seealso cref="ResetCounters"/>
        public int GetRenderedFaceCount()
        {
            return facesCount;
        }

        /// <summary>
        /// Metoda pro konverzi z objektu svetla na pozici svetla (homogenni slozka je 1)
        /// </summary>
        /// <param name="obj">Objekt svetla</param>
        /// <returns>Pozice svetla</returns>
        private Vector4 EffectObjectToIEMLight(SceneObject obj)
        {
            return obj.light.GetLightPosition();
        }

        /// <summary>
        /// Vrati pocet vsech objektu ve svete
        /// </summary>
        /// <returns>Vrati pocet vsech objektu ve svete</returns>
        public int GetAllObjectsCount()
        {
            return objects.Count;
        }

        /// <summary>
        /// Vrati pocet vsech viditelnych objektu
        /// </summary>
        /// <returns>Vrati pocet vsech viditelnych objektu</returns>
        public int GetVisibleObjectsCount()
        {
            return visibleObjectsCount;
        }

        #endregion

        #region Setry

        /// <summary>
        /// Metoda zapne/vypne interni pocitani viditelnosti ve scene
        /// </summary>
        /// <seealso cref="ComputeVisibility"/>
        public void SetComputeVisibility(bool computeVisibility)
        {
            this.enabledComputeVisibility = computeVisibility;
        }

        /// <summary>
        /// Metoda nastavi, zda se maji renderovat objekty, ktere nevrhaji stiny
        /// </summary>
        /// <param name="renderShadowsEmitterObjects"></param>
        public void SetRenderShadowsNonEmitterObjects(bool renderShadowsNonEmitterObjects)
        {
            this.renderShadowsNonEmitterObjects = renderShadowsNonEmitterObjects;
        }

        /// <summary>
        /// Nastavi se shadow mapu, ktera se bude pouzivat pro rendering (v pripade shaderu, ktery s ni umi pracovat)
        /// </summary>
        /// <param name="shadowMap">Shadow mapa, ktera se pouzije pro rendering</param>
        /// <remarks>Pouziva se pro rendering stinu</remarks>
        public void SetShadowMap(Texture shadowMap)
        {
            sharedVariablesShader.SetValue(fxShadow_texture, shadowMap);
        }

        /// <summary>
        /// Nastavi matici View * Projection matice z pohledu svetla
        /// </summary>
        /// <param name="lightViewProjection">View * Projection matice z pohledu svetla</param>
        /// <remarks>Pouziva se pro rendering stinu</remarks>
        public void SetLightViewProjection(Matrix lightViewProjection)
        {
            sharedVariablesShader.SetValue(fxLightViewProjection, lightViewProjection);
        }

        /// <summary>
        /// Nastavi kameru pouzivanou pro rendering
        /// </summary>
        /// <param name="camera">Objekt kamery, ktery se pouziva pro rendering</param>
        public void SetCamera(ISceneCamera camera)
        {
            this.camera = camera;
        }

        /// <summary>
        /// Metoda nastavi uroven detajlu
        /// </summary>
        /// <param name="level">Enum s moznosti nastaveni urovne detajlu</param>
        public void SetDetailLevel(DetailLevel level)
        {
            currentDetail = level;

            String detail = (currentDetail == DetailLevel.UltraHigh) ? DetailLevel.High.ToString() : currentDetail.ToString();

            EffectHandle technique = null;

            foreach (SceneEffect effect in effects)
            {
                technique = effect.effect.GetTechnique(detail);

                if (technique != null)
                    effect.effect.Technique = technique;

            }
        }

        /// <summary>
        /// Metoda nastavi kvalitu vsech modelu
        /// </summary>
        /// <remarks>Nastavuje kvalitu jak z pohledu geometrie, tak textur, efektu, atp., zavisi na konkretnim objektu</remarks>
        /// <param name="quality">Uroven kvality, plati na intervalu (0,1) vcetne krajnich bodu, kde 0 je nejnizsi kvalita a 1 je nejvyssi kvalita</param>
        public void SetAllObjectsQuality(float quality)
        {
            foreach (SceneObject obj in objects)
            {
                obj.generalObject.SetObjectQuality(quality);
            }
        }

        /// <summary>
        /// Metoda povoli nebo zakaze svetla
        /// </summary>
        /// <param name="enable">True povoli svetla, False zakaze svetla</param>
        public void EnableLights(bool enable)
        {
            this.enableLights = enable;
        }

        #endregion

        #region Compute Methods

        /// <summary>
        /// Pokud je povolene pocitani viditelnosti, tak funkce spocte viditelnost k objektu
        /// </summary>
        /// <param name="obj">Objekt ke kterymu se ma spocist viditelnost</param>
        /// <returns>Vrati, zda je objekt videt, ci ne (pokud je vyple pocitani viditelnost, tak je objekt vzdy videt)</returns>
        /// <seealso cref="SetComputeVisibility"/>
        private bool ComputeObjectVisibility(SceneObject obj)
        {
            if (this.enabledComputeVisibility)
                return obj.generalObject.ComputeVisibility(camera);
            else
            {
                obj.generalObject.ResetVisibility();
                return true;
            }
        }

        /// <summary>
        /// Pokud je povolene pocitani viditelnosti, tak funkce spocte viditelnost k objektu
        /// </summary>
        /// <param name="obj">Objekt ke kterymu se ma spocist viditelnost</param>
        /// <returns>Vrati, zda je objekt videt, ci ne (pokud je vyple pocitani viditelnost, tak je objekt vzdy videt)</returns>
        /// <seealso cref="SetComputeVisibility"/>
        private bool ComputeObjectVisibility(IRenderable obj)
        {
            if (enabledComputeVisibility)
                return obj.ComputeVisibility(camera);
            else
            {
                obj.ResetVisibility();
                return true;
            }
        }

        /// <summary>
        /// Napocita vzdalenosti vsech objektu ve scene
        /// </summary>
        /// <param name="camera">Camera, ktera se ma pouzit pro vypocet vzdalenosti</param>
        private void ComputeAllObjectDistance()
        {
            foreach (SceneObject obj in objects)
            {
                obj.generalObject.ComputeDistanceFromCamera(camera);
            }
        }

        /// <summary>
        /// Spocita viditelnost vsech objektu
        /// </summary>
        private void ComputeAllObjectVisibility()
        {
            visibleObjectsCount = 0;

            foreach (SceneObject obj in objects)
            {
                if (obj.generalObject.ComputeVisibility(camera)) 
                    visibleObjectsCount++;
            }

        }

        /// <summary>
        /// Napocita u vsech objektu vzdalenosti k zadane pozici
        /// </summary>
        /// <param name="position">Pozice, ke ktere se maji pocitat vzdalenosti vsech objektu</param>
        private void ComputeAllObjectDistanceToPosition(Vector3 position)
        {
            foreach (SceneObject obj in objects)
            {
                obj.generalObject.ComputeDistanceToPosition(position);
            }
        }


        /// <summary>
        /// Povoli nebo zakaze LOD u vsech objektu
        /// </summary>
        /// <param name="enable">True, pokud se ma LOD povolit, jinak false</param>
        private void EnableAllObjectsLOD(bool enable)
        {
            foreach (SceneObject obj in objects)
            {
                obj.generalObject.EnableLOD(enable);
            }
        }

        #endregion
    }
}
