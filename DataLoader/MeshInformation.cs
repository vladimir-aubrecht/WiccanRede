using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace WiccanRede
{
    class MeshInformation
    {
        public ProgressiveMesh pMesh;
        public String path;
        public Texture[] textures;
        public String[] texturesUrl;
        public MeshInformation(ProgressiveMesh mesh, String path, Texture[] textures,String[] textUrl)
        {
            pMesh = mesh;
            this.path = path;
            this.textures = textures;
            texturesUrl = textUrl;
        }
    }
}
