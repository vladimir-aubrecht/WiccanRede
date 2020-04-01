using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    abstract class ControlElement : IDisposable
    {
        protected string text;
        protected Point position;
        protected Size size;
        protected MenuResult result;

        protected Device device;
        protected Texture textureNormal;
        protected Texture textureHover;
        //protected Texture textureClick;
        protected Texture textureCurrent;
        protected Sprite elementSprite;

        protected Microsoft.DirectX.Direct3D.Font font;
        protected Point textPosition;
        protected Color textColor;

        public Size Size { get { return this.size; } }
        public Point Position { get { return this.position; } set { this.position = value; } }

        public ControlElement(Device device)
        {
            this.device = device;

            this.position = new Point(0, 0);
            this.size = new Size(device.Viewport.Width, device.Viewport.Height);
            this.text = "";
            this.elementSprite = new Sprite(device);
            font = new Microsoft.DirectX.Direct3D.Font(device, new System.Drawing.Font("Tempus Sans ITC", 28, FontStyle.Bold));
            textColor = Color.Black;
            textPosition = new Point(25, 0);
        }

        //public ControlElement(Device device, string text, Point position, Size size, 
        //    Texture textureNormal, Texture textureHover, Texture textureClick)
        //{
        //    this.device = device;
        //    this.elementSprite = new Sprite(device);
        //    this.text = text;
        //    this.position = position;
        //    this.size = size;
        //    this.textureNormal = textureNormal;
        //    this.textureHover = textureHover;
        //    this.textureClick = textureClick;

        //    this.textureCurrent = this.textureNormal;
        //}

        public virtual void Draw()
        {
            if (this.textureCurrent != null)
            {
                elementSprite.Begin(SpriteFlags.AlphaBlend);
                elementSprite.Draw2D(textureCurrent, Rectangle.Empty, this.size, (PointF)this.position, Color.White.ToArgb());
                this.font.DrawText(elementSprite, this.text, textPosition, textColor);
                elementSprite.End(); 
            }
        }

        public virtual bool CheckElement(Point mousePosition)
        {
            if (this is MenuBackGround)
                return false;

            if (mousePosition.X > this.position.X && mousePosition.Y > this.position.Y)
                if (mousePosition.X < (this.position.X + this.size.Width) && mousePosition.Y < (this.position.Y + this.size.Height))
                {
                    return true;
                }
            return false;
        }

        public virtual void ElementHover()
        {
            if (this is MenuBackGround)
                return;
            this.textureCurrent = this.textureHover;
        }
        public virtual void ElementLeave()
        {
            if (this is MenuBackGround)
                return;
            this.textureCurrent = this.textureNormal;
        }
        public virtual void ElementClick()
        {
            if (this is MenuBackGround)
                return;
            //this.textureCurrent = this.textureClick;
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            if (this.textureCurrent != null && !this.textureCurrent.Disposed)
            {
                this.textureCurrent.Dispose();
            }
            if (this.textureHover != null && !this.textureHover.Disposed)
            {
                this.textureHover.Dispose();
            }
            if (this.textureNormal != null && !this.textureNormal.Disposed)
            {
                this.textureNormal.Dispose();
            }
            if (this.elementSprite != null && !this.elementSprite.Disposed)
            {
                this.elementSprite.Dispose();
            }
            if (this.font != null && !this.font.Disposed)
            {
                this.font.Dispose();
            }
        }

        #endregion
    }
}
