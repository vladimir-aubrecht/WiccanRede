using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;
using WiccanRede.Graphics;
using WiccanRede.Graphics.Scene;
using WiccanRede.Graphics.Scene.SpecialObjects;
using Logging;
using Microsoft.DirectX;

namespace WiccanRede.Graphics
{
    class OcclusionCulling : IDisposable
    {
        Device device;
        SceneManager scene;
        Surface occlusionSurface;
        Texture occlusionTexture;

        RenderToSurface occlusionRenderSurface;
        List<IRenderable> sceneObjects;
        QuadTreeGeneralObject quadTreeObject;
        List<IRenderable> leaves;

        OcclusionQueryManager queryManager;

        int totalPixelsVisible = 0;
        int terrainNodesVisible = 0;
        int objectsPixelsVisible = 0;

        public int ObjectsPixelsVisible
        {
            get { return objectsPixelsVisible; }
        }

        public int TerrainNodesVisible
        {
            get { return terrainNodesVisible; }
        }

        public int PixelsVisible
        {
            get { return totalPixelsVisible; }
        }

        public Texture OcclusionTexture
        {
            get { return occlusionTexture; }
        }


        public OcclusionCulling(Device device, SceneManager scene)
        {
            Logger.AddInfo("Inicializace occlusion culling");
            this.device = device;
            this.scene = scene;
            this.sceneObjects = new List<IRenderable>();
            List<IRenderable> objs = scene.GetAllObjects().ConvertAll<IRenderable>
                (new Converter<SceneManager.SceneObject, IRenderable>(ConvertToIRenderable));

            foreach (IRenderable obj in objs)
            {
                if (obj is Objects.Terrain
                    || obj is Objects.LightningObjects.LightingSprite 
                    || obj is Objects.ParticleSystem.ParticleSystem)
                    continue;
                else
                {
                    this.sceneObjects.Add(obj);
                }
            }

            SceneManager.SceneObject terrain = scene.GetObject("Teren");

            if (terrain == null)
                return;

            quadTreeObject = (terrain.generalObject as QuadTreeGeneralObject);

            List<WiccanRede.Graphics.Scene.SpecialObjects.QuadTreeGeneralObject.Key> keys = quadTreeObject.GetQuadTreeLeaves();
            leaves = keys.ConvertAll<IRenderable>(new Converter<QuadTreeGeneralObject.Key, IRenderable>(ConvertToIRenderable));

            List<IRenderable> allObjects = new List<IRenderable>();
            allObjects.AddRange(leaves);
            allObjects.AddRange(sceneObjects);

            queryManager = new OcclusionQueryManager(device, scene, allObjects.Count);
            queryManager.RegisterObjects(allObjects.ToArray());

            Init(device);

            //Logger.AddInfo("occlusion culling nainicializovan, zbuffer enbaled = " + device.RenderState.ZBufferEnable.ToString());
        }

        private void Init(Device device)
        {
            System.Drawing.Size size = new System.Drawing.Size(device.PresentationParameters.BackBufferWidth / 4, device.PresentationParameters.BackBufferHeight / 4);
            occlusionRenderSurface = new RenderToSurface(device, size.Width, size.Height, Format.R32F, true, DepthFormat.D24S8);
            occlusionTexture = new Texture(device, size.Width, size.Height, 1, Usage.RenderTarget, Format.R32F, Pool.Default);
            occlusionSurface = occlusionTexture.GetSurfaceLevel(0);
        }

        IRenderable ConvertToIRenderable(WiccanRede.Graphics.Scene.SpecialObjects.QuadTreeGeneralObject.Key key)
        {
            return key as IRenderable;
        }
        IRenderable ConvertToIRenderable(SceneManager.SceneObject obj)
        {
            return obj.generalObject as IRenderable;
        }

        public void ReInit()
        {
            Init(device);
        }

        public void Releasse()
        {
            this.Dispose();
        }

        public void DoCulling()
        {

            totalPixelsVisible = 0;
            terrainNodesVisible = 0;
            objectsPixelsVisible = 0;

            //quadTreeObject.ComputeVisibility(Camera.GetCameraInstance());
            queryManager.Reset();

            occlusionRenderSurface.BeginScene(this.occlusionSurface);
            occlusionRenderSurface.Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, System.Drawing.Color.White, 1.0f, 0);

            //scene.RenderScene(0, scene[null], false);
            
            scene.BeginRenderObject(0, scene[null]);
            foreach (IRenderable renderable in this.leaves)
            {
                scene.RenderObjectBoundingBox(renderable);
            }
            scene.EndRenderObject();

            queryManager.IssueAllQueries();

            this.totalPixelsVisible = queryManager.GetResults();
            occlusionRenderSurface.EndScene(Filter.None);

            this.totalPixelsVisible = queryManager.TotalPixelsVisible;
            this.objectsPixelsVisible = queryManager.ObjectsPixelsVisible;
            this.terrainNodesVisible = queryManager.TerrainNodesVisible;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (this.queryManager != null)
                this.queryManager.Dispose();

            if (this.sceneObjects != null)
            {
                this.sceneObjects.Clear();
                this.sceneObjects = null;
            }
            if (this.leaves != null)
            {
                this.leaves.Clear();
                this.leaves = null;
            }

            if (this.occlusionRenderSurface != null && !this.occlusionRenderSurface.Disposed)
            {
                this.occlusionRenderSurface.Dispose();
            }
            if (this.occlusionSurface != null && !this.occlusionSurface.Disposed)
            {
                this.occlusionSurface.Dispose();
            }
            if (this.occlusionTexture != null && !this.occlusionTexture.Disposed)
            {
                this.occlusionTexture.Dispose();
            }

        }

        #endregion
    }
}
