using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using WiccanRede.Graphics.Scene;
using WiccanRede.Graphics;
using WiccanRede.Objects;

namespace WiccanRede.Objects
{
    class Sprite : GeneralObject
    {
        public Sprite(Device device, Matrix world, Texture texture)
            : base(GeneralObject.GenerateSpriteGeometry(device), world, new Texture[] { texture }, null, null, null)
        {

        }

    }
}
