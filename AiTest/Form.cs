using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using Logging;
using WiccanRede.AI;
using System.IO;

namespace WiccanRede
{
    /// <summary>
    /// main window class
    /// </summary>
    public partial class TestForm : Form
    {
        string mapName;
        //int overEstimate;
        AICore ai;
        Grid grid;
        bool end = false;

        AI.CharacterNPC[] characters;

        /// <summary>
        /// #ctor, initialize components and logger, register events
        /// </summary>
        public TestForm()
        {
            Logger.InitLogger();
            Logger.bWriteToConsole = true;
            InitializeComponent();

            //this.AllowDrop = true;
            //this.gridPanel.AllowDrop = true;

            this.FormClosing += new FormClosingEventHandler(PathFinderForm_FormClosing);
            this.Paint += new PaintEventHandler(PathFinderForm_Paint);
            this.Shown += new EventHandler(TestForm_Shown);

            this.mapName = "Settings/map.png";
            Init();
            Invalidate();

        }

        void TestForm_Shown(object sender, EventArgs e)
        {
            while (!this.end)
            {
                ai.Update();
                Application.DoEvents();
                System.Threading.Thread.Sleep(100);
                Invalidate();
            }
        }

        private void Init()
        {
            Bitmap bmp = new Bitmap(this.mapName);
            grid = new Grid(bmp, this.gridPanel);
            Logger.AddInfo("Map loaded: " + bmp.Width.ToString() + "; " + bmp.Height.ToString());
            ai = new AICore(grid);
            string[] npcList = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\Settings\\level1npc.ini");
            characters = new WiccanRede.AI.CharacterNPC[npcList.Length];
            Entity[] entities = new Entity[npcList.Length];

            for (int i = 0; i < npcList.Length; i++)
            {
                try
                {
                    characters[i] = new WiccanRede.AI.CharacterNPC("Settings\\" + npcList[i] + ".xml");
                    //ai.AddPlayer(characters[i], new Microsoft.DirectX.Vector3(i, i, i), new Entity(), "BasicFSM");
                    entities[i] = new Entity(characters[i], ai, "BasicFSM");
                }
                catch (Exception ex)
                {
                    Logging.Logger.AddWarning("Chyba pri nacitani NPC: " + npcList[i] + " - " + ex.ToString());
                }
            }
        }

        /// <summary>
        /// paint event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PathFinderForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (grid != null)
            {
                //method grid.PaintGrid(sender, e) draw grid and ship into the Image
                //and this image is drawn here
                g.DrawImage(grid.PaintGrid(), 0, 0); 
            }
        }

        /// <summary>
        /// closing event, save log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PathFinderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //just save the log
            Logger.Save();
            this.end = true;
        }


        /// <summary>
        /// set target position text
        /// </summary>
        /// <param name="text">text to draw into the label</param>
        public void SetPositionLabelText(string text)
        {
            lbPosition.Text = text;
        }

    }
}