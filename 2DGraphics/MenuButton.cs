using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    class MenuButton : ControlElement
    {
        public MenuResult Result
        {
            get { return this.result; }
        }

        public MenuButton(Device device, MenuResult result, string text, System.Drawing.Point position)
            : base(device)
        {
            this.result = result;
            this.text = text;
            this.textureNormal = TextureLoader.FromFile(device, @"Resources/Button.dds");
            this.textureHover = TextureLoader.FromFile(device, @"Resources/Buttonhover.dds");
            //this.textureClick = this.textureHover;

            System.Drawing.Image img = System.Drawing.Image.FromFile(@"Resources/Button.png");
            this.size = img.Size;
            this.textPosition = new System.Drawing.Point(30, 10);
            img.Dispose();

            this.position = position;

        }

    }
}
