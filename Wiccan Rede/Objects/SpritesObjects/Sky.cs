using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using WiccanRede;


namespace WiccanRede.Objects.SpritesObjects
{

    class Sky : Sprite
    {
        public Sky(Device device, Texture color_texture0)
            : base(device, Matrix.Identity, new Texture[] { color_texture0 }, null, null)
        {

        }

    }
}
