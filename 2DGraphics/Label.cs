using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    class Label : ControlElement
    {
        public Label(Device device, string text)
            : base(device)
        {
            this.textureNormal = TextureLoader.FromFile(device, @"Resoureces/TextBackGround.png");
            this.text = text;
            /*this.textureClick = */this.textureHover = this.textureNormal;
        }
    }
}
