using System;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using d3d = Microsoft.DirectX.Direct3D;

using WiccanRede.Graphics.Utils;
using WiccanRede.Graphics.Scene;
using Logging;
using Settings = global::WiccanRede.Properties.Settings;

namespace WiccanRede.Graphics
{
    /// <summary>
    /// Trida se stara o veskere vykreslovani grafiky
    /// </summary>
    class GraphicCore : IDisposable
    {
        /// <summary>
        /// Definuje ruzne druhy optimalizace
        /// </summary>
        public enum OptimizeType
        {
            QuadTree,
            OcclussionCulling
        }

        #region Global Variables/Objects

        /// <summary>
        /// Instance inicializatoru d3d zarizeni
        /// </summary>
        private static Initializator initializator;

        /// <summary>
        /// Instance spravce sceny (SceneManager)
        /// </summary>
        private static SceneManager sceneManager;

        /// <summary>
        /// inicializace HUD
        /// </summary>
        private static HUD hud;

        /// <summary>
        /// Objekt se stara o udrzovani a load vsech zdroju
        /// </summary>
        private DataControl data;

        /// <summary>
        /// Urcuje, zda maji byt zaple stiny
        /// </summary>
        public static bool enableshadows = Settings.Default.Shadows;

        public static bool enableLights = Settings.Default.EnableLights;

        /// <summary>
        /// Nastavi, zda jsou videt bounding sphery, ci ne
        /// </summary>
        public static bool showBoundingSpheres = false;

        /// <summary>
        /// Urcuje typ pouzivane optimalizace
        /// </summary>
        public static OptimizeType usedoptimize = OptimizeType.QuadTree;

        /// <summary>
        /// Urcuje aktualni uroven detajlu
        /// </summary>
        private static SceneManager.DetailLevel currentDetail = (SceneManager.DetailLevel)Settings.Default.DetailLevel;

        /// <summary>
        /// Urcuje rozliseni okna aplikace
        /// </summary>
        private Size WindowResolution = Settings.Default.WindowResolution;

        /// <summary>
        /// Urcuje maximalni viditelnou vzdalenost
        /// </summary>
        private float currentFarDistance = Settings.Default.FarDistance;

        /// <summary>
        /// Urcuje kvalitu objektu
        /// </summary>
        private float objectQuality = 0.3f;

        /// <summary>
        /// Urcuje, zda je aplikace v okne nebo fullscreenu
        /// </summary>
        private bool windowed = false;

        /// <summary>
        /// Urcuje, zda je nebo neni treba znova nacist zdroje do videopameti (false = neni potreba)
        /// </summary>
        private bool unloadResources = false;

        /// <summary>
        /// reprezentace kurzoru
        /// </summary>
        private Sprite cursorSprite;

        /// <summary>
        /// Textura pro kurzor
        /// </summary>
        Texture cursorTexture;

        /// <summary>
        /// Aktualni pocet snimku ve vterine
        /// </summary>
        private float fps;

        /// <summary>
        /// Pocet fps
        /// </summary>
        private float cfps;

        private Vector2 cursorPosition;

        /// <summary>
        /// Cas posledniho ukonceni funkce Render
        /// </summary>
        private DateTime lastTime = DateTime.Now;

        /// <summary>
        /// Generator nahodnych cisel
        /// </summary>
        private Random rnd = new Random();

        /// <summary>
        /// Instance formu aplikace
        /// </summary>
        private Program form;

        /// <summary>
        /// D3D zarizeni
        /// </summary>
        private Device device;

        /// <summary>
        /// Aktualne nastaveny teren, po kterem se chodi
        /// </summary>
        private WiccanRede.Objects.Terrain terrain;

        /// <summary>
        /// Objekt shadow mapy
        /// </summary>
        private RenderToTexture shadowMap;

        /// <summary>
        /// Objekt kolizni mapy
        /// </summary>
        private RenderToTexture collisionMap;

        /// <summary>
        /// Objekt pro optimalizace pomoci occlussion culling
        /// </summary>
        private OcclusionCulling occlusion;
        #endregion

        /// <summary>
        /// Getr, ktery vrati instanci sprace sceny (SceneManager)
        /// </summary>
        /// <returns>Getr, ktery vrati instanci sprace sceny (SceneManager)</returns>
        public static SceneManager GetCurrentSceneManager()
        {
            return sceneManager;
        }

        /// <summary>
        /// Getr, ktery vrati instanci initializatoru d3d zarizeni
        /// </summary>
        /// <returns>Getr, ktery vrati instanci initializatoru d3d zarizeni</returns>
        public static Initializator GetInitializator()
        {
            return initializator;
        }

        public Vector2 GetCursorPosition()
        {
            return cursorPosition;
        }

        /// <summary>
        /// Funkce se postara o odstraneni vsech alokovanych zdroju
        /// </summary>
        public void Dispose()
        {
            ResourceUnload();

            if (!device.Disposed)
                device.Dispose();

            if (sceneManager != null)
                sceneManager.Dispose();
        }

        /// <summary>
        /// Reaguje na ztratu zarizeni a odstrani vse, co se nachazi ve video pameti
        /// </summary>
        private void ResourceUnload()
        {
            sceneManager.ReleasseObjects();

            if (shadowMap != null)
                shadowMap.Dispose();

            if (collisionMap != null)
                collisionMap.Dispose();

            if (occlusion != null)
                occlusion.Dispose();

            unloadResources = true;
        }

        /// <summary>
        /// Konstruktor, spusti inicializaci D3D
        /// </summary>
        /// <param name="form">Objekt tridy Form, kam se ma vykreslovat</param>
        /// <param name="windowed">Je true, pokud aplikace bude spusteni v okenim rezimu, jinak bude spustena ve fullscreenu</param>
        public GraphicCore(Program form, bool windowed)
        {
            Logger.AddInfo("Graphic core init");
            
            this.windowed = windowed;
            this.form = form;

            form.Size = this.WindowResolution;

            initializator = new Initializator(form);
            initializator.SetResolution(false, form.Size.Width, form.Size.Height);

            if (currentDetail == SceneManager.DetailLevel.High || currentDetail == SceneManager.DetailLevel.UltraHigh || Settings.Default.ForceVSync)
                initializator.SetVSync(true);

            initializator.ReInit();

            device = initializator.GetDevice();

            Microsoft.DirectX.Direct3D.AdapterDetails adapter = new Microsoft.DirectX.Direct3D.AdapterDetails();
            Logger.AddInfo("Info: " + System.Environment.OSVersion.ToString() + "<br/> " + this.device.DisplayMode + "<br/>" + adapter.ToString());

            sceneManager = new SceneManager(device, Camera.GetCameraInstance());

            #region Nastaveni kvality shadow map
            int shadowMapDimension = Settings.Default.MediumShadowMapDimension;

            if (currentDetail == SceneManager.DetailLevel.Low)
                shadowMapDimension = (int)(shadowMapDimension * 0.5f);
            else if (currentDetail == SceneManager.DetailLevel.High)
                shadowMapDimension *= 2;
            #endregion

            shadowMap = new RenderToTexture(device, shadowMapDimension, shadowMapDimension, Format.R32F, true, DepthFormat.D24X8);
            collisionMap = new RenderToTexture(device, 128, 128, Format.A8R8G8B8, true, DepthFormat.D16);

            DeviceReset();


        }

        /// <summary>
        /// Metoda se postara o reinicializaci veci po resetu zarizeni
        /// </summary>
        public void DeviceReset()
        {
            Console.SetWindowXMargin(device.PresentationParameters.BackBufferWidth - 250);
            Console.SetDefaultWindowColor(Color.Red);

            Camera.SetCameraDevice(device);
            Camera.GetCameraInstance().SetFarDistance(currentFarDistance);

            Console.ConsoleSetDevice(device);

            if (occlusion != null)
                occlusion.ReInit();

            #region Inicializace textur, atp.
            shadowMap.ReInit();
            collisionMap.ReInit();
            #endregion

            sceneManager.SetCamera(Camera.GetCameraInstance());
            sceneManager.ReInitObjects();

            cursorPosition = initializator.GetResolution() * 0.5f;

            if (hud != null)
                hud.onResolutionChange();
        }

        /// <summary>
        /// Funkce prepina mezi okennim a fullscreen rezimem
        /// </summary>
        public void ToggleFullscreen()
        {

            ResourceUnload();

            if (windowed)
            {
                this.WindowResolution = form.Size;

                if (Properties.Settings.Default.FullscreenResolution.Width > 0 && Properties.Settings.Default.FullscreenResolution.Height > 0)
                    initializator.ToggleFullscreen(Properties.Settings.Default.FullscreenResolution.Width, Properties.Settings.Default.FullscreenResolution.Height);
                else
                    initializator.ToggleFullscreen(Manager.Adapters[0].CurrentDisplayMode.Width, Manager.Adapters[0].CurrentDisplayMode.Height);
            }
            else
            {
                initializator.ToggleFullscreen(WindowResolution.Width, WindowResolution.Height);
            }

            windowed = !windowed;

        }

        /// <summary>
        /// Metoda nacte vsechny zdroje, mela by byt volana pouze jednou po inicializaci
        /// </summary>
        public void ResourcesLoad()
        {
            Logger.AddInfo("Nacitani resourcu");

            LoadSceneFromXml(@"Resources/data.xml");
            terrain = (WiccanRede.Objects.Terrain)CameraDriver.GetAttachedTerain();
            InitDetails();

            Logger.AddInfo("Resources nacteny");
            if (occlusion == null)
                occlusion = new OcclusionCulling(device, sceneManager);

            cursorTexture = TextureLoader.FromFile(device, @"Resources/Textures/cursor.dds");
            this.cursorSprite = new Sprite(device);

            hud = new HUD(device, form);
            hud.onResolutionChange();

        }

        /// <summary>
        /// Nastavi uroven detailu
        /// </summary>
        /// <param name="detailLevel">Pozadovana uroven detailu</param>
        public void InitDetails(SceneManager.DetailLevel detailLevel)
        {
            currentDetail = detailLevel;
            InitDetails();
        }

        /// <summary>
        /// Nastavi uroven detailu
        /// </summary>
        public void InitDetails()
        {
            if (currentDetail == SceneManager.DetailLevel.UltraHigh)
            {
                currentFarDistance = Settings.Default.FarDistance * 1.2f;
                objectQuality = 1f;
                enableLights = true;
            }
            else if (currentDetail == SceneManager.DetailLevel.High)
            {
                currentFarDistance = Settings.Default.FarDistance * 1f;
                objectQuality = 0.6f;
                enableLights = true;
            }
            else if (currentDetail == SceneManager.DetailLevel.Medium)
            {
                currentFarDistance = Settings.Default.FarDistance * 0.8f;
                objectQuality = 0.3f;
                enableLights = true;
            }
            else if (currentDetail == SceneManager.DetailLevel.Low)
            {
                currentFarDistance = Settings.Default.FarDistance * 0.8f;
                objectQuality = 0.15f;
                enableLights = false;
            }

            Camera.GetCameraInstance().SetFarDistance(currentFarDistance);
            sceneManager.SetDetailLevel(currentDetail);
            sceneManager.SetAllObjectsQuality(objectQuality);

            sceneManager.EnableLights(enableLights);

        }

        /// <summary>
        /// Metoda se postara o nacteni vsech zdroju z xml do sceny
        /// </summary>
        /// <param name="url">Cesta k xml popisujicimu scenu</param>
        public void LoadSceneFromXml(String url)
        {
            sceneManager.Clear();

            Logger.AddInfo("Nacitani data.xml");
            data = new DataControl(device, sceneManager.GetSharedPool());
            List<Entity> entity = data.LoadXML(url);

            Type objectType;
            Object[] par;
            ConstructorInfo[] constructorInfo;
            ParameterInfo[] parameters;

            Assembly asm = Assembly.GetExecutingAssembly();

            foreach (Entity obj in entity)
            {
                List<Parametr> entityParameters = obj.GetParametrs();

                if (obj.Type == "Microsoft.DirectX.Direct3D.Effect")
                {
                    foreach (Parametr v in entityParameters)
                    {
                        sceneManager.AddEffect(v.name, v.value as Effect);
                    }

                    continue;
                }

                objectType = asm.GetType(obj.Type);
                constructorInfo = objectType.GetConstructors();
                List<Object> objPar = new List<object>();
                foreach (ConstructorInfo constructor in constructorInfo)
                {
                    objPar.Clear();
                    parameters = constructor.GetParameters();

                    foreach (ParameterInfo param in parameters)
                    {
                        if (param.ParameterType.ToString() == "Microsoft.DirectX.Direct3D.Device")
                        {
                            objPar.Add(device);
                        }
                        else if (param.ParameterType.ToString() == "WiccanRede.Scene.SceneManager")
                        {
                            objPar.Add(sceneManager);
                        }
                        else
                        {

                            Parametr obj2 = obj[param.Name];

                            if (obj2 != null)
                                objPar.Add(obj2.value);
                            else
                                objPar.Add(null);

                        }
                    }

                }

                par = objPar.ToArray();

                Effect shader = null;
                foreach (Parametr p in entityParameters)
                    if (p.type == "Microsoft.DirectX.Direct3D.Effect")
                        shader = p.value as Effect;
                    else if (p.name.ToLower() == "shader")
                        shader = sceneManager[p.value as String];

                String name = obj.Type;

                if (obj["name"] != null)
                    name = obj["name"].value as String;

                bool shadowEmitter = true;

                if (obj["shadowEmitter"] != null)
                    shadowEmitter = (bool)obj["shadowEmitter"].value;

                sceneManager.AddObject(name, Activator.CreateInstance(objectType, par) as GeneralObject, shader, shadowEmitter);
            }
        }

        /// <summary>
        /// Funkce se postara o rendering cele sceny
        /// </summary>
        /// <param name="time">Cas uplynuly od spusteni aplikace v milisekundach</param>
        public void Render(float time)
        {
            sceneManager.ResetCounters();

            #region Rendering tests
            bool deviceLost = initializator.IsDeviceLost();
            if (deviceLost)
            {
                ResourceUnload();
            }

            if (!initializator.BeginRender())
            {
                return;
            }

            if (unloadResources)
            {
                DeviceReset();
                unloadResources = false;
            }
            #endregion

            #region Vyrenderovani kolizni mapy (1x)
            if (terrain != null && terrain.GetCollissionMap() == null)
            {
                RenderCollisionMap(0);
                terrain.SetCollissionMap(BitmapOperation.TextureToColorArray(collisionMap.GetTexture()), true);
            }
            #endregion

            #region Generovani shadow mapy podle prvniho svetla, existuje-li nejake

            if ((int)currentDetail >= 2)
            {
                List<SceneManager.SceneObject> lights = sceneManager.GetDirectionLights();

                if (lights.Count > 0)
                {
                    Vector4 moonPosition = lights[0].light.GetLightPosition();

                    Matrix view = CameraDriver.GetTerainViewMatrix(new Vector3(moonPosition.X, moonPosition.Y, moonPosition.Z));
                    Matrix OrthoProjectionMatrix = CameraDriver.GetTerainOrthoProjectionMatrix();
                    sceneManager.SetLightViewProjection(view * OrthoProjectionMatrix);
                    sceneManager.SetRenderShadowsNonEmitterObjects(false);
                    sceneManager.SetComputeVisibility(false);
                    shadowMap.BeginScene();
                    device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.White, 1f, 0);

                    if (enableshadows)
                    {
                        sceneManager.RenderSceneBasic(time, sceneManager["ShadowMap"]);
                    }
                    shadowMap.EndScene(Filter.Linear);
                    sceneManager.SetShadowMap(shadowMap.GetTexture());

                    sceneManager.SetRenderShadowsNonEmitterObjects(true);
                    sceneManager.SetComputeVisibility(true);
                }
            }

            #endregion

            #region Nastaveni optimalizace
            if (usedoptimize == OptimizeType.QuadTree)
            {
                sceneManager.SetComputeVisibility(true);
            }
            else if (usedoptimize == OptimizeType.OcclussionCulling)
            {
                occlusion.DoCulling();
                sceneManager.SetComputeVisibility(false);
            }
            #endregion

            device.BeginScene();

            sceneManager.RenderScene(time, sceneManager["Shadows"]);

            if (showBoundingSpheres)
                sceneManager.RenderSceneBasic(time, sceneManager["Base"], true, false);

            RenderHUD();

            device.EndScene();

            initializator.EndRender();
            CountFPS();
        }

        /// <summary>
        /// Metoda vyrenderuje kolizni mapu
        /// </summary>
        /// <remarks>Metoda predpoklada, ze bude volana po testech, ktere provadi metoda Render, proto je doporucovano volat ji z metody Render</remarks>
        /// <param name="time">Cas uplynuly od spusteni aplikace v milisekundach</param>
        /// <seealso cref="Render"/>
        private void RenderCollisionMap(float time)
        {

            Matrix view = CameraDriver.GetTerainViewMatrix(new Vector3(0, 5000, 0), new Vector3(0, 0, 1));
            Matrix proj = CameraDriver.GetTerainOrthoProjectionMatrix();

            collisionMap.BeginScene();
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.White, 1.0f, 0);

            IRenderable terrain = (IRenderable)CameraDriver.GetAttachedTerain();

            if (terrain != null)
                terrain.SetVisible(false);

            List<Scene.SceneManager.SceneObject> objects = sceneManager.GetObjectsOfType(Type.GetType("WiccanRede.Objects.Building"), false);

            sceneManager.BeginRenderObject(time, sceneManager[null], view, proj);

            foreach (Scene.SceneManager.SceneObject obj in objects)
                sceneManager.RenderObject(obj.generalObject);

            sceneManager.EndRenderObject();

            if (terrain != null)
                terrain.SetVisible(true);

            if (terrain != null)
            {
                sceneManager.SetComputeVisibility(false);
                terrain.ResetVisibility();

                sceneManager.BeginRenderObject(time, sceneManager["TerrainCollission"], view, proj);
                sceneManager.RenderObject(terrain);
                sceneManager.EndRenderObject();

                sceneManager.SetComputeVisibility(true);
            }

            collisionMap.EndScene(Filter.Point);

        }

        /// <summary>
        /// Metoda se stara o rendering HUD, tzn. konzole, fonty, atp.
        /// </summary>
        /// <remarks>Metoda predpoklada, ze bude volana po testech, ktere provadi metoda Render, proto je doporucovano volat ji z metody Render</remarks>
        private void RenderHUD()
        {

            if (Console.ConsoleCanBeUsed() && Console.ConsoleIsShowed())
            {
                Console.RenderConsole();
            }

            if (Console.ConsoleCanBeUsed())
            {
                Console.WindowWriteLine(String.Format("{0} fps", cfps.ToString()), 15);
                Vector3 pos = Camera.GetCameraInstance().GetVector3Position();
                Console.WindowWriteLine(String.Format("Position: {0},{1},{2}", ((int)pos.X), ((int)pos.Y), ((int)pos.Z)), 35);

                //Vector2 pos2D = terrain.ConvertToTerrainPosition(pos);

                //Console.WindowWriteLine("Position 2D: " + pos2D.X + "," + pos2D.Y, 55);
                //Console.WindowWriteLine("Objects in World: " + sceneManager.GetAllObjectsCount(), 75);
                //Console.WindowWriteLine("Objects in Scene: " + sceneManager.GetVisibleObjectsCount(), 95);
                //Console.WindowWriteLine("Faces: " + sceneManager.GetRenderedFaceCount(), 115);

                if (this.form.Game != null)
                {
                    List<WiccanRede.AI.NpcInfo> npcs = this.form.Game.GetAllNpcInfo();
                    WiccanRede.AI.NpcInfo playerInfo = npcs[npcs.Count - 1];

                    Console.WindowWriteLine(String.Format("Level: {0}", playerInfo.Character.level - 4), 50, 25);
                    Console.WindowWriteLine(String.Format("Strength: {0}", playerInfo.Character.power), 50, 45);
                    Console.WindowWriteLine(String.Format("Defence: {0}", playerInfo.Character.defense), 50, 65);
                }

            }

            hud.Render();

            PointF cuPos = new PointF(cursorPosition.X - 16, cursorPosition.Y - 16);

            this.cursorSprite.Begin(SpriteFlags.AlphaBlend);
            this.cursorSprite.Draw2D(this.cursorTexture, Rectangle.Empty, Size.Empty, cuPos /*Input.GetInputInstance().MouseLocation*/, Color.White);
            this.cursorSprite.End();
        }

        /// <summary>
        /// Metoda se spousti po prvnim snimku 1x
        /// </summary>
        public void FirstTimeRender()
        {
            hud.LoadNpcInfo();
        }

        /// <summary>
        /// Zaridi renderovani progress baru behem loadu dat
        /// </summary>
        public void ResourceProgressRender()
        {
            if (data != null)
            {
                lock (form)
                {
                    form.Text = "Wiccan Rede - Loading " + data.GetProgressInfo() + "%";
                }
            }
        }

        /// <summary>
        /// Funkce se stara o pocitani snimku za vterinu (FPS)
        /// </summary>
        private void CountFPS()
        {

            if ((DateTime.Now.Subtract(lastTime)).Seconds >= 1)
            {
                lastTime = DateTime.Now;
                cfps = fps;
                Logger.AddFPSInfo((int)cfps);
                fps = 0;
            }

            fps++;
        }

    }
}
