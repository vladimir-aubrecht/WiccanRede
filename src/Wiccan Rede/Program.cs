using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.DirectX;
using System.Threading;
using Logging;

using WiccanRede.Multimedia;
using WRG = WiccanRede.Graphics;
using Details = WiccanRede.Graphics.Scene.SceneManager.DetailLevel;
using WiccanRede.Objects;

namespace WiccanRede
{
  /// <summary>
  /// Trida s hernim jadrem
  /// </summary>
  class Program : Form
  {
    enum NetActions { LoggedUsers };
    private bool exit = false;
    private bool drawMenu = Properties.Settings.Default.DrawMenu;
    private bool win = false;
    private MenuManager menu = null;
    private int startTime = 0;
    private WRG.GraphicCore graphic = null;
    private Game.GameManager game = null;
    private SoundManager multimediaManager;

    public Game.GameManager Game
    {
      get { return game; }
    }
    private Input input = null;
    private Microsoft.DirectX.Vector2 direction = new Microsoft.DirectX.Vector2();
    private int milisecondsElapsed = 0;
    private float action1Stoper = 0f;
    private float action2Stoper = 0f;
    Thread resourceLoadThread;
    System.Drawing.Rectangle windowRect;

    List<NetActions> actions = new List<NetActions>();

    [System.Runtime.InteropServices.DllImport("User32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);

    [STAThread]
    static void Main()
    {
      Application.Run(new Program());
    }

    Message msg;
    private bool IsAppStillIdle()
    {
      if (!Created)
      {
        return false;
      }
      return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    public Program()
    {
      this.Text = "Wiccan Rede";
      this.ClientSize = Properties.Settings.Default.WindowResolution;
      this.FormBorderStyle = Properties.Settings.Default.WindowBorder;
      this.Show();

      this.Shown += new EventHandler(ProgramShown);
      this.FormClosing += new FormClosingEventHandler(ProgramClosing);
      this.KeyDown += new KeyEventHandler(Program_KeyDown);
      this.LostFocus += new EventHandler(Program_LostFocus);
      this.GotFocus += new EventHandler(Program_GotFocus);
      this.Move += new EventHandler(Program_Move);

    }


    #region Events

    void Application_Idle(object sender, EventArgs e)
    {
      System.Console.WriteLine("Idle");
      if (IsAppStillIdle())
      {
        System.Console.WriteLine("still Idle");
      }
    }

    /// <summary>
    /// Postara se o korektni ukonceni aplikace
    /// </summary>
    void ProgramClosing(object sender, FormClosingEventArgs e)
    {
      exit = true;
    }

    /// <summary>
    /// Postara se o schovani kurzoru, spusteni herni smycky a po jejim skonceni ukonci aplikaci
    /// </summary>
    void ProgramShown(object sender, EventArgs e)
    {
      InitCore();
      Application.Exit();
    }

    void Program_GotFocus(object sender, EventArgs e)
    {
      if (this.input != null)
      {
        this.input.Resume();
      }
      Cursor.Clip = new System.Drawing.Rectangle(this.Location, this.Size);
    }

    void Program_LostFocus(object sender, EventArgs e)
    {
      if (this.input != null)
      {
        this.input.Stop();
      }
      Cursor.Clip = System.Drawing.Rectangle.Empty;
    }

    void Program_Move(object sender, EventArgs e)
    {
      this.windowRect = new System.Drawing.Rectangle(this.Location, this.Size);
    }
    #endregion

    /// <summary>
    /// Funkce se stara o zpracovani udalosti z DXInput vstupu
    /// </summary>
    /// <param name="actions">Vycet akci, ktere se maji provest</param>
    void ProcessActions(List<Input.Action> actions)
    {
      #region Actions from keyboard and mouse
      while (actions.Count > 0)
      {
        switch (actions[0])
        {
          case Input.Action.Action1:
            if (game != null)
            {
              if (action1Stoper > 0)
                break;

              WRG.Camera cam = WRG.Camera.GetCameraInstance();
              Vector3 dir = cam.GetDirectionVector2(graphic.GetCursorPosition());

              Input.Action action = actions[0];
              game.Logic.Spell(action, dir);

              //WiccanRede.Objects.Fireball f = new WiccanRede.Objects.Fireball("Hrac", Graphics.GraphicCore.GetInitializator().GetDevice(), cam.GetVector3Position(), dir, new AI.ActionInfo(), null);
              //Graphics.GraphicCore.GetCurrentSceneManager().AddObject(f.GetFireName(), f, null as Microsoft.DirectX.Direct3D.Effect);

              action1Stoper = 50f;
            }
            break;
          case Input.Action.Action2:
            if (game != null)
            {
              if (action2Stoper > 0)
                break;

              Vector2 pos = graphic.GetCursorPosition();
              List<Graphics.Scene.SceneManager.SceneObject> selectedObjects = Graphics.GraphicCore.GetCurrentSceneManager().GetAllObjects((int)pos.X, (int)pos.Y);

              if (selectedObjects.Count > 0)
              {
                int t = 0;

                while (t < selectedObjects.Count && selectedObjects[t].name.StartsWith("Hrac") || selectedObjects[t].generalObject.isEquiped()) t++;

                if (t >= selectedObjects.Count)
                  break;

                float distance = selectedObjects[t].generalObject.GetDistanceFromCamera();

                if (distance > selectedObjects[t].generalObject.GetSphereRadius() + 100)
                {
                  break;
                }

                WiccanRede.Graphics.HUD.DrawText("Byl označen objekt " + selectedObjects[t].name, "System", System.Drawing.Color.Red, false);
                Logger.AddInfo("Oznacen objekt: " + selectedObjects[t].name);

                if (!(selectedObjects[t].generalObject is WiccanRede.Objects.PickupObject))
                {
                  game.FireGameEventUp(selectedObjects[t].name, "click"); 
                }
                else
                {
                  game.PickupObject(selectedObjects[t].name);
                  (selectedObjects[t].generalObject as PickupObject).Pickup();
                }

                action2Stoper = 300f;
              }

            }
            break;
          case Input.Action.MoveForwardStart:
            direction.X += 1;
            break;
          case Input.Action.MoveForwardStop:
            direction.X -= 1;
            break;
          case Input.Action.MoveRightStart:
            direction.Y += 1;
            break;
          case Input.Action.MoveRightStop:
            direction.Y -= 1;
            break;
          case Input.Action.MoveBackwardStart:
            direction.X += -1;
            break;
          case Input.Action.MoveBackwardStop:
            direction.X -= -1;
            break;
          case Input.Action.MoveLeftStart:
            direction.Y += -1;
            break;
          case Input.Action.MoveLeftStop:
            direction.Y -= -1;
            break;
          case Input.Action.LookAround:
            WRG.CameraDriver.LookAround(input.MouseRelativeMove.X, input.MouseRelativeMove.Y);
            break;
          case Input.Action.Exit:
            drawMenu = true;
            this.menu.CurrentMenuResult = MenuResult.None;
            break;
        }

        actions.RemoveAt(0);
      }
      #endregion

      #region Moving by Camera
      if (direction.Length() != 0 && !WRG.Console.ConsoleIsShowed())
      {
        float angle = 0f;

        if (direction.X == 1 && direction.Y == 1)
          angle = (float)Math.PI / 4f;
        else if (direction.X == 1 && direction.Y == -1)
          angle = -(float)Math.PI / 4f;
        else if (direction.X == -1 && direction.Y == -1)
          angle = -3f * (float)Math.PI / 4f;
        else if (direction.X == -1 && direction.Y == 1)
          angle = 3f * (float)Math.PI / 4f;
        else if (direction.X == 1)
          angle = 0;
        else if (direction.X == -1)
          angle = (float)Math.PI;
        else if (direction.Y == 1)
          angle = (float)Math.PI / 2f;
        else if (direction.Y == -1)
          angle = -(float)Math.PI / 2f;

        game.Logic.PlayerMove(angle);
      }

      #endregion

      #region Actions from Console
      List<string> commands = WRG.Console.ConsoleGetCommands();

      if (commands.Count > 0)
      {
        int firstspace = commands[0].IndexOf(" ");
        string command = commands[0];
        string parameters = String.Empty;

        if (firstspace != -1)
        {
          command = command.Substring(0, firstspace);
          parameters = commands[0].Substring(firstspace + 1);
        }

        switch (command)
        {
          case "exit":
          case "quit": exit = true;
            break;

          case "sensitivity":
            float sensitivity = WRG.CameraDriver.GetSensitivity();

            if (parameters == String.Empty)
            {
              WRG.Console.ConsoleWriteLine("Citlivost mysi je: " + sensitivity.ToString());
            }
            else if (!Single.TryParse(parameters, out sensitivity))
              WRG.Console.ConsoleWriteLine("Je nutno zadat cislo, urcujici citlivost mysi");
            else
            {
              WRG.CameraDriver.SetSensitivity(sensitivity);
            }
            break;

          case "fly":
            WRG.CameraDriver.EnableFreeLook();
            break;

          case "walk":
            WRG.CameraDriver.DisableFreeLook();
            break;

          case "help":
            WRG.Console.ConsoleWriteLine("do konzole je mozne zadat prikazy:");
            WRG.Console.ConsoleWriteLine("help");
            WRG.Console.ConsoleWriteLine("exit");
            WRG.Console.ConsoleWriteLine("quit");
            WRG.Console.ConsoleWriteLine("sensitivity [t]");
            WRG.Console.ConsoleWriteLine("fly");
            WRG.Console.ConsoleWriteLine("walk");
            WRG.Console.ConsoleWriteLine("shadowsoff");
            WRG.Console.ConsoleWriteLine("shadowson");
            WRG.Console.ConsoleWriteLine("wire");
            WRG.Console.ConsoleWriteLine("solid");
            WRG.Console.ConsoleWriteLine("quadtree");
            WRG.Console.ConsoleWriteLine("occ");
            WRG.Console.ConsoleWriteLine("lights");
            WRG.Console.ConsoleWriteLine("bspheres");
            break;

          case "shadowson":
            WRG.GraphicCore.enableshadows = true;
            break;

          case "shadowsoff":
            WRG.GraphicCore.enableshadows = false;
            break;

          case "wire":
            WRG.GraphicCore.GetInitializator().GetDevice().RenderState.FillMode = Microsoft.DirectX.Direct3D.FillMode.WireFrame;
            break;

          case "solid":
            WRG.GraphicCore.GetInitializator().GetDevice().RenderState.FillMode = Microsoft.DirectX.Direct3D.FillMode.Solid;
            break;

          case "quadtree":
            WRG.GraphicCore.usedoptimize = WRG.GraphicCore.OptimizeType.QuadTree;
            break;

          case "occ":
            WRG.GraphicCore.usedoptimize = WRG.GraphicCore.OptimizeType.OcclussionCulling;
            break;

          case "bspheres":
            WRG.GraphicCore.showBoundingSpheres = !WRG.GraphicCore.showBoundingSpheres;
            break;

          case "low":
            graphic.InitDetails(Details.Low);
            break;

          case "medium":
            graphic.InitDetails(Details.Medium);
            break;

          case "high":
            graphic.InitDetails(Details.High);
            break;

          case "ultra":
            graphic.InitDetails(Details.UltraHigh);
            break;

          case "lights":
            WRG.GraphicCore.enableLights = !WRG.GraphicCore.enableLights;
            WRG.GraphicCore.GetCurrentSceneManager().EnableLights(WRG.GraphicCore.enableLights);
            break;
          case "restart":
            Restart();
            break;

          default: WRG.Console.ConsoleWriteLine("Zadany prikaz neexistuje");
            break;

        }

        commands.RemoveAt(0);
      }
      #endregion
    }

    #region Switches

    /// <summary>
    /// Funkce se stara o zpracovani stiknutych klaves
    /// </summary>
    void Program_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Alt && e.KeyCode == Keys.Enter)
      {
        graphic.ToggleFullscreen();
      }
      else if (e.KeyCode == Keys.Oemtilde)
      {
        if (WRG.Console.ConsoleIsShowed())
          WRG.Console.ConsoleShow(false);
        else if (WRG.Console.ConsoleCanBeUsed())
          WRG.Console.ConsoleShow();
      }
      else if (e.KeyCode == Keys.U && !WRG.Console.ConsoleIsShowed())
      {
        WRG.CameraDriver.SetInvertedMouse(!WRG.CameraDriver.GetInvertedMouse());
      }
      else if (e.KeyCode == Keys.F5 && !WRG.Console.ConsoleIsShowed())
      {
        graphic.LoadSceneFromXml("Resources/data.xml");
      }



      if (WRG.Console.ConsoleIsShowed())
      {
        if (e.KeyCode == Keys.Space)
        {
          WRG.Console.ConsoleWriteCharOnCommandLine(" ");
        }
        else if (e.KeyValue == 110)
        {
          WRG.Console.ConsoleWriteCharOnCommandLine(".");
        }
        else if (e.KeyCode == Keys.Enter)
        {
          WRG.Console.ConsoleSendCommandFromBuffer();
        }
        else if (e.KeyCode == Keys.Back)
        {
          WRG.Console.ConsoleRemoveLastCharOnCommandLine();
        }
        else if (e.KeyCode == Keys.Oemcomma)
        {
          WRG.Console.ConsoleWriteCharOnCommandLine(",");
        }
        else if (e.Shift && e.KeyCode == Keys.Oem1)
        {
          WRG.Console.ConsoleWriteCharOnCommandLine(":");
        }
        else if (e.KeyValue >= 60 && e.KeyValue <= 90)
        {
          WRG.Console.ConsoleWriteCharOnCommandLine(e.KeyData.ToString().Substring(0, 1).ToLower());
        }
        else if (e.KeyValue >= 96 && e.KeyValue <= 105)
        {
          WRG.Console.ConsoleWriteCharOnCommandLine(e.KeyData.ToString().Replace("NumPad", String.Empty));
        }
        else if (e.KeyValue >= 48 && e.KeyValue <= 57)
        {
          WRG.Console.ConsoleWriteCharOnCommandLine(e.KeyData.ToString().Replace("D", String.Empty));
        }

      }

    }
    #endregion

    /// <summary>
    /// Funkce nainicializuje jednotlive podsystemy a spusti herni smycku
    /// </summary>
    public void InitCore()
    {
      VideoManager video = new VideoManager(this, false);
      video.LoadVideo(@"Resources/Videos/logo2.avi", true);

      Logger.InitLogger();
      Logger.bWriteToOutput = true;
      Logger.AddInfo("Inicializace");

      graphic = new WRG.GraphicCore(this, true);
      multimediaManager = new SoundManager(this);

      if (Properties.Settings.Default.EnableMusic)
        multimediaManager.AddToPlayList(@"Resources/Music/");


      multimediaManager.PlayMusic();
      video.PlayVideo();

      input = Input.GetInputInstance(this);
      input.SetCooperative(Microsoft.DirectX.DirectInput.CooperativeLevelFlags.Background | Microsoft.DirectX.DirectInput.CooperativeLevelFlags.NonExclusive);
      menu = new MenuManager(WRG.GraphicCore.GetInitializator().GetDevice());

      #region Init Tests
      if (graphic == null)
      {
        Logger.AddError(global::WiccanRede.Properties.Resources.Graphic_Core_Init_Error);
        return;
      }

      if (input == null)
      {
        Logger.AddError(global::WiccanRede.Properties.Resources.Input_Core_Init_Error);
        return;
      }
      #endregion

      this.Show();

      resourceLoadThread = new Thread(graphic.ResourcesLoad);
      resourceLoadThread.Start();

      List<Input.Action> actions = null;
      startTime = Environment.TickCount;
      input.Start();
      int lastTime = 0;

      while (resourceLoadThread.ThreadState != ThreadState.Stopped)
      {
        graphic.ResourceProgressRender();
        Application.DoEvents();
        Thread.Sleep(200);
      }

      video.StopVideo();
      video.Dispose();


      this.windowRect = new System.Drawing.Rectangle(this.Location, this.Size);
      Cursor.Position = PointToScreen(new System.Drawing.Point(0, 0));
      Cursor.Clip = windowRect;
      Cursor.Hide();

      SetCore();

      WRG.Initializator init = WRG.GraphicCore.GetInitializator();

      while (!exit && this.Created)
      {
        milisecondsElapsed = (Environment.TickCount - startTime);
        int frametime = milisecondsElapsed - lastTime;

        WRG.CameraDriver.SetTime(frametime);
        actions = input.Update(true);

        if (game != null)
        {
          List<WiccanRede.AI.NpcInfo> npcs = game.GetAllNpcInfo();

          //je hrac mrtvy?
          if (npcs[npcs.Count - 1].Status.hp <= 0 && this.menu.MenuState != MenuResult.Restart)
          {
            this.menu.UpdateState("Gameover");
            this.drawMenu = true;
          }
          else if (this.menu.MenuState == MenuResult.Restart)
          {
            this.menu.UpdateState("MainMenu");
            this.menu.MenuState = MenuResult.New;
            Restart();
          }

          //je konec hry?
          if (game.GetCurrentGameState().Name == "Final")
          {
            this.menu.UpdateState("ToBeContinued");
            this.drawMenu = true;
          }
        }

        if (this.Focused)
        {
          Cursor.Clip = this.windowRect;

          if (init.IsFullscreen())
          {
            Cursor.Clip = System.Drawing.Rectangle.Empty;
          }
        }
        else
        {
          Cursor.Clip = System.Drawing.Rectangle.Empty;
        }



        if (this.drawMenu)
        {
          Cursor.Show();
          init.GetDevice().BeginScene();
          MenuResult result = this.menu.Update(this.PointToClient(MousePosition), (actions.Contains(Input.Action.Action1)));
          init.GetDevice().EndScene();
          init.GetDevice().Present();

          if (result == MenuResult.Pub)
          {
            this.drawMenu = false;
            this.menu.UpdateState("MainMenu");
            this.menu.CurrentMenuResult = MenuResult.None;
          }
          else if (result == MenuResult.New)
          {
            this.drawMenu = false;
            this.menu.UpdateState("MainMenu");
            this.menu.CurrentMenuResult = MenuResult.New;
          }
          else if (result == MenuResult.Exit || actions.Contains(Input.Action.Exit))
          {
            this.exit = true;
          }
          System.Threading.Thread.Sleep(20);
          Cursor.Hide();
        }
        else
        {
          action1Stoper -= frametime;
          action2Stoper -= frametime;

          ProcessActions(actions);
          graphic.Render(milisecondsElapsed);

          if (game == null)
          {
            game = WiccanRede.Game.GameManager.GetInstance();
            graphic.FirstTimeRender();
          }

          try
          {
            game.Update((float)(frametime) / 1000f);
          }
          catch (Exception ex)
          {
            Logger.AddError(ex.Message);
          }
        }

        Application.DoEvents();

        lastTime = milisecondsElapsed;
      }
      Logging.Logger.Save();
      graphic.Dispose();
      multimediaManager.Dispose();
    }

    private void Restart()
    {
      //TODO kod pro restart hry

      Logger.AddImportant("Game Restart!");
      this.game.Restart();
      this.input.Stop();
      this.actions.Clear();
      this.direction = new Vector2();
      this.input.Start();
    }


    /// <summary>
    /// Funkce provede load potrebnych dat a nastaveni po startu aplikace
    /// </summary>
    private void SetCore()
    {
      //graphic.ResourcesLoad();

      if (Properties.Settings.Default.Freelook)
        WRG.CameraDriver.EnableFreeLook();
      else
      {
        WRG.CameraDriver.DisableFreeLook();
        WRG.CameraDriver.MoveToStartPosition();
      }

      WRG.CameraDriver.SetShowPlayer(true);

    }

    private void InitializeComponent()
    {
      this.SuspendLayout();
      // 
      // Program
      // 
      this.ClientSize = new System.Drawing.Size(784, 564);
      this.Name = "Program";
      this.ResumeLayout(false);

    }

  }
}
