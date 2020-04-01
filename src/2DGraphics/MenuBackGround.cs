using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;
using System.Drawing;

namespace WiccanRede
{
    class MenuBackGround : ControlElement
    {
        Texture texture;

        public Texture Texture
        {
            get
            {
                return texture;
            }
            set
            {
                this.texture = value;
                this.textureCurrent = this.texture;
                this.textureNormal = this.texture;
                this.textureHover = this.texture;
            }
        }

        public MenuBackGround(Device device) : base(device)
        {
            texture = TextureLoader.FromFile(device, @"Resources/Title.dds");
            this.textureCurrent = this.texture;
            this.textureNormal = this.texture;
            this.textureHover = this.texture;
            //this.textureClick = this.texture;

            this.device.DeviceReset += new EventHandler(device_DeviceReset);
            device_DeviceReset(null, null);
        }

        void device_DeviceReset(object sender, EventArgs e)
        {
            this.size = new Size(device.Viewport.Width, device.Viewport.Height);
        }
    }
}
