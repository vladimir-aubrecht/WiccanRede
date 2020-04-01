//by Tomas Vondracek
//class fo handling input from keyboard and mouse
//support for immediate and buffered input
//provides translate to action list


using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using System.Drawing;


namespace WiccanRede
{
    /// <summary>
    /// Class for handling input from keyboard and mouse. When using buffered input from keyboard,
    /// each key press has its start action and key releas stop action. Using immediate input throws start actions only
    /// </summary>
    public class Input : IDisposable  
    {
        /// <summary>
        /// enum of all possible actions
        /// </summary>
        public enum Action
        {
            MoveUpStart,
            MoveDownStart,
            MoveLeftStart,
            MoveRightStart,
            MoveForwardStart,
            MoveBackwardStart,
            MoveUpStop,
            MoveDownStop,
            MoveLeftStop,
            MoveRightStop,
            MoveForwardStop,
            MoveBackwardStop,
            LookAround,
            Exit,
            Action1,
            Action2,
            Action3,
            Wheel,
            InvertMouse,
            ChangeFillMode,
            ShowConsole,
            None
        };

        const int BUFFER_SIZE = 32;
        private Microsoft.DirectX.Vector2 mouseRelativeMove = new Vector2(0,0);
        private int iMouseWheel = 0;
        private static Input instance = null;

        List<Action> actions = new List<Action>();
        List<Action> bufferedActions = new List<Action>();
        
        Microsoft.DirectX.DirectInput.Device keyboard = null;
        Microsoft.DirectX.DirectInput.Device mouse = null;
        System.Windows.Forms.Control windowControl;
        System.Drawing.Point mouseLocation;

        CooperativeLevelFlags flags;

        bool bCanReadInput = true;

        /// <summary>
        /// get Input class reference, if it does not exists, create one
        /// </summary>
        /// <param name="windowControl">window for input</param>
        /// <returns>objekt tridy Input</returns>
        public static Input GetInputInstance(System.Windows.Forms.Control windowControl)
        {
            if (Input.instance == null)
                instance = new Input(windowControl);

            return Input.instance;
        }
        /// <summary>
        /// get the Input class reference, if it does not exists, throw Exception - need to use function Input Input.GetInputInstance(System.Windows.Forms.Control windowControl).
        /// </summary>
        /// <returns>objekt tridy Input</returns>
        public static Input GetInputInstance()
        {
            if (instance == null)
                throw new NullReferenceException("objekt instance neni inicializovan");
            else
                return instance;
        }

        /// <summary>
        /// free input devices and dispose Input class objects
        /// </summary>
        public void Dispose()
        {
            try
            {
                FreeDirectInput();
            }
            catch (Exception ex)
            {
                Logging.Logger.AddError(ex.Message);
            }
        }

        private void FreeDirectInput()
        {
            if (this.mouse != null)
            {
                mouse.Unacquire();
                mouse.Dispose();
                mouse = null;
            }
            if (this.keyboard != null)
            {
                keyboard.Unacquire();
                keyboard.Dispose();
                keyboard = null;
            }
        }

        private Input(System.Windows.Forms.Control windowControl)
        {
            this.windowControl = windowControl;
            this.mouseLocation = new System.Drawing.Point();
            flags = CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground;
            //Resume();
            //InitInputDevices();
        }

        private void InitInputDevices()
        {
            //keyboard
            
            keyboard = new Device(SystemGuid.Keyboard);
            if (keyboard == null)
            {
                System.Windows.Forms.MessageBox.Show("No keyboard found.!");
                throw new Exception("No keyboard found.");

            }
            
            //mouse
            mouse = new Device(SystemGuid.Mouse);

            if (mouse == null)
            {
                System.Windows.Forms.MessageBox.Show("No mouse found.!");
                throw new Exception("No mouse found.");

            }

            //mouse.Properties.AxisModeAbsolute = true;
            //set cooperation
            keyboard.SetCooperativeLevel(
                this.windowControl, flags);

            mouse.SetCooperativeLevel(
                this.windowControl, flags);

            mouse.SetDataFormat(DeviceDataFormat.Mouse);
            keyboard.SetDataFormat(DeviceDataFormat.Keyboard);

            try
            {
                keyboard.Properties.BufferSize = BUFFER_SIZE;
            }
            catch (Exception ex)
            {
                Logging.Logger.AddWarning("CHYBA V INPUTU! Nepodarilo se nastavit velikost bufferu. " + ex.ToString());
                throw;
            }
   
            //ziskat mys a klavesnici
            try
            {
                keyboard.Acquire();
                mouse.Acquire();
            }
            catch (InputException ex)
            {
                Logging.Logger.AddWarning("CHYBA V INPUTU! \nZkusim znovu ziskat input device jako sdileny\n" + ex.ToString());
                try
                {
                    keyboard.Unacquire();
                    mouse.Unacquire();
                    keyboard.SetCooperativeLevel(
                        this.windowControl,
                        CooperativeLevelFlags.NonExclusive |
                        CooperativeLevelFlags.Background);

                    mouse.SetCooperativeLevel(
                        this.windowControl,
                        CooperativeLevelFlags.NonExclusive |
                        CooperativeLevelFlags.Background);

                    keyboard.Acquire();
                    mouse.Acquire();
                }
                catch (InputException iex)
                {
                    Logging.Logger.AddError(" KRITICKA CHYBA V INPUTU!" + iex.ToString());
                    throw iex;
                }
            }
        }

        private void UpdateMouse()
        {
            MouseState mouseState;
            try
            {
                mouse.Poll();
                mouseState = mouse.CurrentMouseState;
            }
            catch (Microsoft.DirectX.DirectInput.InputLostException)
            {
                try
                {
                    FreeDirectInput();
                    InitInputDevices();
                    mouseState = mouse.CurrentMouseState;
                }
                catch (Microsoft.DirectX.DirectInput.InputException ex)
                {
                    Logging.Logger.AddWarning("CHYBA - V INPUTU " + ex.ToString());
                    return;
                }
            }

            this.mouseRelativeMove.X = mouseState.X;
            this.mouseRelativeMove.Y = mouseState.Y;
            this.iMouseWheel = mouseState.Z;

            this.mouseLocation.X += (int) mouseRelativeMove.X;
            this.mouseLocation.Y += (int) mouseRelativeMove.Y;

            if (this.mouseLocation.X < 0)
            {
                this.mouseLocation.X = 0;
            }
            else if (this.mouseLocation.X > this.windowControl.Size.Width -1)
            {
                this.mouseLocation.X = this.windowControl.Size.Width -1;
            }
            if (this.mouseLocation.Y < 0)
            {
                this.mouseLocation.Y = 0;
            }
            else if (this.mouseLocation.Y > this.windowControl.Size.Height -1)
            {
                this.mouseLocation.Y = this.windowControl.Size.Height - 1;
            }
            
            if (this.iMouseWheel > 0)
            {
                actions.Add(Action.Wheel);
            };
            if (this.mouseRelativeMove.X != 0 || this.mouseRelativeMove.Y != 0)
            {
                this.actions.Add(Action.LookAround);
                //Logging.Logger.AddInfo("Udalost mysi");
            }

            byte[] buttons = mouseState.GetMouseButtons();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != 0)
                {
                    switch (i)
                    {
                        case 0:
                            actions.Add(Action.Action1);
                            break;
                        case 1:
                            actions.Add(Action.Action2);
                            break;
                        case 2:
                            actions.Add(Action.Action3);
                            break;
                    }
                }
            }
        }

        private void UpdateKeyboard()
        {
            //Capture pressed keys.
            Key[] keys = null;
            try
            {
                keyboard.Poll();
                keys = keyboard.GetPressedKeys();
            }
            catch (InputLostException)
            {
                try
                {
                    FreeDirectInput();
                    InitInputDevices();
                    keys = keyboard.GetPressedKeys();
                }
                catch (InputException ex)
                {
                    Logging.Logger.AddError("CHYBA - V INPUTU " + ex.ToString());
                    return;
                }
            }
            foreach (Key k in keys)
            {
                switch (k)
                {
                    case Key.W:
                        actions.Add(Action.MoveForwardStart);
                        break;
                    case Key.S:
                        actions.Add(Action.MoveBackwardStart);
                        break;
                    case Key.A:
                        actions.Add(Action.MoveLeftStart);
                        break;
                    case Key.D:
                        actions.Add(Action.MoveRightStart);
                        break;
                    case Key.Escape:
                        actions.Add(Action.Exit);
                        break;
                    case Key.U:
                        actions.Add(Action.InvertMouse);
                        break;
                    case Key.F:
                        actions.Add(Action.Action3);
                        break;
                    case Key.Return:
                        actions.Add(Action.ChangeFillMode);
                        break;
                }
            }
        }

        /// <summary>
        /// Read immediate input, map pressed keys to Input.Action. Holding a key mean many actions
        /// </summary>
        /// <returns>List of Input.Actions</returns>
        public List<Action> Update()
        {
            if (!bCanReadInput)
            {
                List<Action> empty = new List<Action>();
                empty.Add(Action.None);
                return empty;
            }

            this.actions.Clear();
            this.UpdateKeyboard();
            this.UpdateMouse();

            return this.actions;
        }
        /// <summary>
        /// Read buffered input, map pressed (and realesed) keys to Input.Action
        /// </summary>
        /// <param name="buffered">if true, use buffered input, else immediate</param>
        /// <returns>List of Input.Actions</returns>
        public List<Action> Update(bool buffered)
        {
            if (!buffered)
            {
                return this.Update();
            }
            if (!bCanReadInput)
            {
                List<Action> empty = new List<Action>();
                empty.Add(Action.None);
                return empty;
            }
            this.ReadBufferedData();
            this.UpdateMouse();

            //create output action list from buffered keyboard input and immediate mouse input
            List<Action> output = new List<Action>();
            output.AddRange(this.bufferedActions);
            output.AddRange(this.actions);
            this.actions.Clear();
            this.bufferedActions.Clear();
            //if (output.Count > 0)
            //{
            //    System.Diagnostics.Debug.WriteLine("pocet akci v bufferu inputu: " + output.Count.ToString()); 
            //}
            return output;
        }

        private void ReadBufferedData()
        {
            BufferedDataCollection buffData = new BufferedDataCollection();
            try
            {
                keyboard.Poll();
                buffData = keyboard.GetBufferedData();
            }
            catch (NotBufferedException ex)
            {
                Logging.Logger.AddWarning("Chyba pri vyberu bufferovanych dat " + ex.ToString());
                return;
            }
            catch (InputException ex)
            {
                Logging.Logger.AddWarning("CHYBA V INPUTU!\n" + ex.ToString());
                try
                {
                    FreeDirectInput();
                    InitInputDevices();
                    buffData = keyboard.GetBufferedData();
                }
                catch (InputException iex)
                {
                    Logging.Logger.AddError("CHYBA V INPUTU!\n" + iex.ToString());
                    return;
                }
                
            }

            if (buffData == null || buffData.Count == 0)
                return;
            foreach (BufferedData data in buffData)
            {
                if ((data.Data & 0x80) != 0)   //pressed keys
                {
                    switch (data.Offset)
                    {
                        case (int) DXKey.W:
                            this.bufferedActions.Add(Action.MoveForwardStart);
                            break;
                        case (int)DXKey.S:
                            this.bufferedActions.Add(Action.MoveBackwardStart);
                            break;
                        case (int)DXKey.A:
                            this.bufferedActions.Add(Action.MoveLeftStart);
                            break;
                        case (int)DXKey.D:
                            this.bufferedActions.Add(Action.MoveRightStart);
                            break;
                    }
                }
                else        //released keys
                {
                    switch (data.Offset)
                    {
                        case 17:
                            this.bufferedActions.Add(Action.MoveForwardStop);
                            break;
                        case 31:
                            this.bufferedActions.Add(Action.MoveBackwardStop);
                            break;
                        case 30:
                            this.bufferedActions.Add(Action.MoveLeftStop);
                            break;
                        case 32:
                            this.bufferedActions.Add(Action.MoveRightStop);
                            break;
                        case 1:
                            this.bufferedActions.Add(Action.Exit);
                            break;
                        case 0x16:
                            this.bufferedActions.Add(Action.InvertMouse);
                            break;
                        case 0x29:
                            this.bufferedActions.Add(Action.ShowConsole);
                            break;
                        case 0x1C:
                            this.bufferedActions.Add(Action.ChangeFillMode);
                            break;
                        case 0x21:
                            this.bufferedActions.Add(Action.Action3);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// gets mouse move relative to last call of Update() function
        /// </summary>
        public Vector2 MouseRelativeMove
        {
            get
            {
                //Logging.Logger.AddInfo("Ctu relativni souradnice mysi");
                return this.mouseRelativeMove;
            }
        }

        public int MouseWheel
        { 
            get
            {
                return this.iMouseWheel;
            }
        }
        /// <summary>
        /// !!!!!!!!!!!!!!!not functional yet!!!!!!!!!!!!!!
        /// </summary>
        public Point MouseLocation
        {
            get
            {
                //mouse.Unacquire();
                //mouse.Properties.AxisModeAbsolute = true;
                //mouse.Acquire();
                //Vector2 v = new Vector2();
                //v.X = mouse.CurrentMouseState.X;
                //v.Y = mouse.CurrentMouseState.Y;
                //mouse.Unacquire();
                //mouse.Properties.AxisModeAbsolute = false;
                //mouse.Acquire();
                //return v;
                return this.mouseLocation;
            }
        }

        public void Start()
        {
            this.bCanReadInput = true;
            this.InitInputDevices();
        }
        /// <summary>
        /// Stops scaning action, usefull when in shared mode
        /// </summary>
        public void Stop()
        {
            Logging.Logger.AddImportant("Prerusuji snimani akci");
            this.bCanReadInput = false;
        }

        public void ReleaseDevices()
        {
            Logging.Logger.AddImportant("uvolnuji input devices");
            this.bCanReadInput = false;
            this.FreeDirectInput();
        }

        /// <summary>
        /// resums scanning actions, reaction for Input.Stop() function
        /// </summary>
        public void Resume()
        {
            Logging.Logger.AddImportant("Obnovuji snimani akci");
            this.bufferedActions.Clear();
            this.actions.Clear();
            this.bCanReadInput = true;
            //this.InitInputDevices();
        }

        public void SetCooperative(CooperativeLevelFlags flags)
        {
            try
            {
                this.flags = flags;
                this.FreeDirectInput();
                this.InitInputDevices();
            }
            catch (Exception ex )
            {
                Logging.Logger.AddError("Chyba pri nastavovani CooperativeLevel \n" + ex.ToString());
            } 
        }
    }
}
