using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using WiccanRede.Graphics.Scene;
using WiccanRede.Graphics;
using WiccanRede.Objects;

namespace WiccanRede.Objects.LightningObjects
{
    class LightingSprite : Lights
    {
        public LightingSprite(Device device, Matrix world, Texture texture)
            : base(GeneralObject.GenerateSpriteGeometry(device), world, texture)
        {
            base.SetAttuentation(0.1f);
            base.SetUseAlphaBlending(true);
        }

    }
}
