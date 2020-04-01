using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Windows.Forms;
using System.Drawing;

using WiccanRede.Graphics.Scene;
using WiccanRede.Graphics.Utils;
using WiccanRede.Game;


namespace WiccanRede.Graphics
{
    class HUD
    {

        struct Sentence
        {
            public float time;
            public string text;
            public string name;
            public Color color;

            public Sentence(String text, String name, Color color)
            {
                this.time = Environment.TickCount;
                this.text = text;
                this.name = name;
                this.color = color;
            }

            public Sentence(String text, String name, Color color, bool permanent)
            {
                if (permanent)
                    this.time = -1;
                else
                    this.time = Environment.TickCount;

                this.text = text;
                this.name = name;
                this.color = color;
            }
        }

        static List<Sentence> sentences = new List<Sentence>();

        private List<SceneManager.SceneObject> NpcObjects;
        private List<WiccanRede.AI.NpcInfo> NpcInfo;
        private SceneManager sm;
        private Device device;
        private Sprite sprite = null;
        private Sprite sprite3d = null;
        private Sprite s = null;

        private Microsoft.DirectX.Direct3D.Font f = null;
        private Microsoft.DirectX.Direct3D.Font f2 = null;
        private Program form;


        private Texture empty;
        private Texture live;
        private Texture mana;
        private Texture energy;
        private Texture empty2;
        private Texture live2;


        private Rectangle srcRectangle = new Rectangle(0, 0, 70, 120);

        private Rectangle liveRectangle = new Rectangle(0, 0, 70, 120);
        private Rectangle manaRectangle = new Rectangle(0, 0, 70, 120);
        private Rectangle energyRectangle = new Rectangle(0, 0, 70, 120);

        private Rectangle npcScrRectange = new Rectangle(0, 0, 64, 8);
        private Rectangle npcRectange = new Rectangle(0, 0, 64, 8);

        private Vector3 center = new Vector3(30f, 0, 0);

        private SizeF size = new SizeF(40f, 120f);

        private SizeF manaSize = new SizeF(40f, 120f);
        private SizeF liveSize = new SizeF(40f, 120f);
        private SizeF energySize = new SizeF(40f, 120f);


        private PointF manaPoint;
        private PointF livePoint;
        private PointF energyPoint;

        private PointF manaEmpty;
        private PointF energyEmpty;
        private PointF liveEmpty;

        bool canRenderNpcInfo = false;

        Vector2 resolution;

        public HUD(Device device, Program form)
        {
            resolution = GraphicCore.GetInitializator().GetResolution();

            manaPoint = new PointF(80f, resolution.Y - 168f);
            livePoint = new PointF(20f, resolution.Y - 168f);
            energyPoint = new PointF(140f, resolution.Y - 168f);

            manaEmpty = new System.Drawing.PointF(80f, resolution.Y - 168f);
            energyEmpty = new System.Drawing.PointF(140f, resolution.Y - 168f);
            liveEmpty = new System.Drawing.PointF(20f, resolution.Y - 168f);

            empty = TextureLoader.FromFile(device, @"Resources/HUD/prazdna.png");
            live = TextureLoader.FromFile(device, @"Resources/HUD/zivot.png");
            mana = TextureLoader.FromFile(device, @"Resources/HUD/mana.png");
            energy = TextureLoader.FromFile(device, @"Resources/HUD/energie.png");
            empty2 = TextureLoader.FromFile(device, @"Resources/HUD/prazdnej.png");
            live2 = TextureLoader.FromFile(device, @"Resources/HUD/zelenejobdelnik.png");



            this.device = device;
            this.form = form;
            NpcObjects = new List<SceneManager.SceneObject>();

            sprite = new Sprite(device);
            sprite3d = new Sprite(device);
            s = new Sprite(device);

            sm = GraphicCore.GetCurrentSceneManager();
            NpcObjects = sm.GetObjectsOfType(Type.GetType("WiccanRede.Objects.Player"), false);

            // NpcInfo = form.Game.GetAllNpcInfo();

            f = new Microsoft.DirectX.Direct3D.Font(device, new System.Drawing.Font(System.Drawing.FontFamily.GenericSerif, 16));
            f2 = new Microsoft.DirectX.Direct3D.Font(device, new System.Drawing.Font(System.Drawing.FontFamily.GenericSerif, 12));

        }

        public void LoadNpcInfo()
        {
            // NpcInfo = form.Game.GetAllNpcInfo();
            canRenderNpcInfo = true;
        }

        public void Render()
        {
            device.VertexShader = null;
            device.PixelShader = null;

            Camera cam = Camera.GetCameraInstance();

            device.Transform.View = cam.GetMatrixView();
            device.Transform.Projection = cam.GetMatrixProjection();

            sprite.Begin(SpriteFlags.AlphaBlend);

            sprite.Draw2D(empty, srcRectangle, size, liveEmpty, Color.FromArgb(255, 255, 255, 255));
            sprite.Draw2D(live, liveRectangle, liveSize, livePoint, Color.FromArgb(255, 255, 255, 255));

            sprite.Draw2D(empty, srcRectangle, size, manaEmpty, Color.FromArgb(255, 255, 255, 255));
            sprite.Draw2D(mana, manaRectangle, manaSize, manaPoint, Color.FromArgb(255, 255, 255, 255));

            sprite.Draw2D(empty, srcRectangle, size, energyEmpty, Color.FromArgb(255, 255, 255, 255));
            sprite.Draw2D(energy, energyRectangle, energySize, energyPoint, Color.FromArgb(255, 255, 255, 255));

            sprite.End();

            RenderText();


            //for (int t = 0; t < sentences.Count; t++)
            //{
            //    Matrix m = Matrix.Identity;
            //    m *= Matrix.RotationX((float)Math.PI);
            //    m *= Matrix.Translation(sentences[t].position) * Matrix.Translation(0, 40, 0);
            //    sprite3d.SetWorldViewLH(m, device.Transform.View);
            //    sprite3d.Begin(SpriteFlags.ObjectSpace | SpriteFlags.Billboard | SpriteFlags.AlphaBlend);
            //    sprite3d.Draw2D(sentences[t].texture, new PointF(0,2),
            //        (float)Math.PI, new PointF(-sentences[t].rect.Width / 2, -16), Color.FromArgb(255, 255, 255, 255));
            //    sprite3d.End();
            //}

            device.Transform.Projection = cam.GetMatrixProjection();
            sprite3d.Transform = Matrix.Identity;

            if (canRenderNpcInfo)
            {
                NpcInfo = form.Game.GetAllNpcInfo();

                for (int i = 0; i < NpcInfo.Count; i++)
                {
                    if (NpcInfo[i].Character.name == "Player")
                    {
                        setEnergy((int)NpcInfo[i].Status.energy);
                        setLive((int)(100 * NpcInfo[i].Status.hp / (float)(NpcInfo[i].Character.hp)));
                        setMana((int)(100 * NpcInfo[i].Status.mana / (float)(NpcInfo[i].Character.mana)));
                        continue;
                    }

                    StringFormat stringFormat = new StringFormat();
                    stringFormat.FormatFlags = StringFormatFlags.NoClip;

                    double percents = (1 - ((double)NpcInfo[i].Status.hp / (double)NpcInfo[i].Character.hp)) * 64;

                    npcRectange.X = (int)percents;

                    GeneralObject npcGeneral = getNpcGeneralObjectByName(NpcInfo[i].Character.name);
                    Matrix world = npcGeneral.GetMatrixWorld();

                    Vector3 min = npcGeneral.GetBoundingBoxRelativeMinimum();
                    Vector3 max = npcGeneral.GetBoundingBoxRelativeMaximum();
                    min = Vector3.TransformCoordinate(min, npcGeneral.GetMatrixWorldBoundingBoxMesh());
                    max = Vector3.TransformCoordinate(max, npcGeneral.GetMatrixWorldBoundingBoxMesh());
                    Vector3 delta = max - min;


                    world *= Matrix.Translation(0, delta.Y, 0);

                    device.Transform.World = world;

                    sprite3d.SetWorldViewLH(device.Transform.World, device.Transform.View);

                    sprite3d.Begin(SpriteFlags.ObjectSpace | SpriteFlags.Billboard | SpriteFlags.AlphaBlend);

                    sprite3d.Draw(empty2, npcScrRectange, center, new Vector3(0, 0, 0), Color.White);
                    sprite3d.Draw(live2, npcRectange, center, new Vector3(0, 0, 0), Color.White);
                    sprite3d.End();

                    Matrix current = Matrix.Identity;
                    Matrix hh = cam.GetMatrixView();
                   
                    Vector3 vect1 = new Vector3(world.M41 / world.M44, 0, world.M43 / world.M44);
                    Vector3 vect2 = new Vector3(hh.M41 / hh.M44, 0, hh.M43 / hh.M44);

                    Vector3 direction = vect2 - vect1;
                    vect1.Normalize();
                    vect2.Normalize();

                    float vect3 = Vector3.Dot(vect1, vect2);
                    double number = vect3;
                    double angle = Math.Acos(number);


                    if (direction.X > 0 && direction.Z > 0) angle *= -1;
                    if (direction.X < 0 && direction.Z > 0) angle *= -1;
                    current *= Matrix.Scaling(0.5f, 0.5f, 0.5f);
                    current *= Matrix.RotationZ((float)Math.PI);
                    current *= Matrix.RotationY((float)angle);

                    current.M41 = world.M41;
                    current.M42 = world.M42;
                    current.M43 = world.M43; 
                    current.M44 = world.M44;

                    current *= Matrix.Translation(0, 15f, 0);

                    sprite3d.SetWorldViewLH(current, device.Transform.View);
                    device.Transform.World = current;

                    sprite3d.Begin(SpriteFlags.ObjectSpace | SpriteFlags.AlphaBlend);
                    Rectangle textRect = f.MeasureString(s, NpcInfo[i].Character.name, DrawTextFormat.Left, Color.DarkRed);
                    textRect.X -= (int)((float)textRect.Width * 0.5f);
                    
                    Color textColor = Color.DarkRed;

                    if (NpcInfo[i].Character.type == WiccanRede.AI.NPCType.villager) textColor = Color.DarkGreen;

                    f.DrawText(sprite3d, NpcInfo[i].Character.name, textRect, DrawTextFormat.Left, textColor);

                    sprite3d.End();
                }
            }
        }

        private void RenderText()
        {
            int length = 70;
            String tsub = String.Empty;
            int offsetY = 0;
            int t = 0;
            String temp = String.Empty;
            for (t = 0; t < sentences.Count; t++)
            {
                int count = (sentences[t].text.Length - (sentences[t].text.Length % length)) / length;
                int offsetX = 0;
                int i = 0;
                for (i = 0; i < count; i++)
                {
                    tsub = temp + sentences[t].text.Substring(i * length, length);
                    int lastspace = tsub.LastIndexOf(" ");
                    temp = tsub.Substring(lastspace + 1);
                    tsub = tsub.Remove(lastspace);

                    if (i == 0)
                        tsub = tsub.Insert(0, sentences[t].name + ": ");
                    else
                        offsetX = 35;

                    f2.DrawText(null, tsub, device.Viewport.Width - 500 + offsetX, device.Viewport.Height - 250 + (t + i + offsetY) * 16, sentences[t].color);
                }

                f2.DrawText(null, temp + sentences[t].text.Substring(i * length), device.Viewport.Width - 500 + offsetX, device.Viewport.Height - 250 + (t + i + offsetY) * 16, sentences[t].color);

                temp = String.Empty;
                offsetY += i;
            }

            bool timeout = false;

            if (sentences.Count > 0)
                timeout = ((Environment.TickCount) - sentences[0].time >= 20000);

            if (device.Viewport.Height - 250 + (t + offsetY) * 16 >= device.Viewport.Height || timeout)
            {
                sentences.RemoveAt(0);
            }
        }
        public void onResolutionChange()
        {
            resolution = GraphicCore.GetInitializator().GetResolution();

            manaPoint.Y = resolution.Y - 168f;
            livePoint.Y = resolution.Y - 168f;
            energyPoint.Y = resolution.Y - 168f;

            manaEmpty.Y = resolution.Y - 168f;
            energyEmpty.Y = resolution.Y - 168f;
            liveEmpty.Y = resolution.Y - 168f;
        }
        public static void drawLoading(int Percentage)
        {

        }
        private GeneralObject getNpcGeneralObjectByName(string name)
        {
            for (int i = 0; i < NpcObjects.Count; i++)
            {
                if (NpcObjects[i].name == name)
                    return NpcObjects[i].generalObject as GeneralObject;
            }
            return null;
        }

        public void setLive(int live)
        {

            if (live < 0) live = 0;

            double liv = (double)live;
            double dif = (1 - (liv / 100)) * 120;
            int diference = (int)dif;
            livePoint.Y = diference + resolution.Y - 168f;
            liveRectangle.Y = diference;
            liveRectangle.Height = 120 - diference;
            liveSize.Height = 120 - diference;
        }

        public void setMana(int mana)
        {

            if (mana < 0) mana = 0;

            double liv = (double)mana;
            double dif = (1 - (liv / 100)) * 120;
            int diference = (int)dif;
            manaPoint.Y = diference + resolution.Y - 168f;
            manaRectangle.Y = diference;
            manaRectangle.Height = 120 - diference;
            manaSize.Height = 120 - diference;
        }

        public void setEnergy(int energy)
        {

            if (energy < 0) energy = 0;

            double liv = (double)energy;
            double dif = (1 - (liv / 100)) * 120;
            int diference = (int)dif;
            energyPoint.Y = diference + resolution.Y - 168f;
            energyRectangle.Y = diference;
            energyRectangle.Height = 120 - diference;
            energySize.Height = 120 - diference;
        }

        public static void DrawText(String text, Vector3 position)
        {
            //Device device = GraphicCore.GetInitializator().GetDevice();
            //Microsoft.DirectX.Direct3D.Font f = new Microsoft.DirectX.Direct3D.Font(device, new System.Drawing.Font(System.Drawing.FontFamily.GenericSerif, 16));
            //Rectangle rect = f.MeasureString(null, text, DrawTextFormat.None, Color.White);

            //RenderToTexture rtt = new RenderToTexture(device, rect.Width,rect.Height, Format.A8R8G8B8, false, DepthFormat.Unknown);
            //rtt.BeginScene();
            //device.Clear(ClearFlags.Target, Color.FromArgb(0), 0f, 0);
            //f.DrawText(null, text, 0, 0, Color.Yellow);            
            //rtt.EndScene(Filter.None);

            ////TextureLoader.Save(@"e:\tt.jpg", ImageFileFormat.Jpg, rtt.GetTexture());
            //Sentence s = new Sentence(text, position, rtt.GetTexture(),rect);
            //sentences.Add(s);
        }

        public static void DrawText(String text)
        {
            DrawText(text, String.Empty, Color.White, true);
        }

        public static void DrawText(String text, String name)
        {
            DrawText(text, name, Color.Yellow, false);
        }

        public static void DrawText(String text, String name, Color color, bool permanent)
        {
            Sentence s = new Sentence(text, name, color, permanent);
            sentences.Add(s);
        }

    }
}
