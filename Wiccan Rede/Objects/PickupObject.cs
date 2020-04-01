using System;
using System.Collections.Generic;
using System.Text;
using WiccanRede.Graphics.Scene;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;

namespace WiccanRede.Objects
{
  public interface IPickup
  {
    void Pickup();
  }

  class PickupObject : GeneralObject
  {

      public PickupObject(ProgressiveMesh mesh, Matrix world, Texture[] color_textures0, Texture[] normal_textures)
          : base(mesh, world, color_textures0, null, null, normal_textures)
      {

      }

    public void Pickup()
    {
      this.Releasse();
    }
  }
}
