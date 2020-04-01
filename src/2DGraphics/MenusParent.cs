using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    public enum MenuResult { New, OK, Cancel, Options, Credits, Multiplayer, Exit, Gameover, Title, Pub, ToBeContinued, None, MainMenu, Restart }

    class MenusParent
    {
        protected List<ControlElement> elements;
        private MenuResult result;

        public MenuResult Result
        {
            get { return result; }
            set { result = value; }
        }

        public MenusParent(Device device)
        {
            MenuBackGround back = new MenuBackGround(device);
            elements = new List<ControlElement>();
            elements.Add(back);

            result = MenuResult.None;
        }

        public MenuResult Update(System.Drawing.Point mousePos, bool clicked)
        {
            foreach (ControlElement elm in this.elements)
            {
                if (elm.CheckElement(mousePos))
                {
                    if (clicked)
                    {
                        elm.ElementClick();
                        if (elm is MenuButton)
                        {
                            this.result = (elm as MenuButton).Result;
                            //return this.result;
                        }
                    }
                    else
                    {
                        elm.ElementHover();
                    }
                }
                else
                {
                    elm.ElementLeave();
                }
                elm.Draw();
            }
            return this.result;
        }

    }
}
