using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX.Direct3D;
using Logging;
using WiccanRede.Graphics.Scene;
using WiccanRede.Graphics.Scene.SpecialObjects;

namespace WiccanRede.Graphics
{
    class QueryItem : IDisposable
    {
        public Query query;
        public IRenderable renderableObject;

        public QueryItem(Query q, IRenderable obj)
        {
            this.query = q;
            this.renderableObject = obj;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (this.query != null && !this.query.Disposed)
                query.Dispose();
        }

        #endregion
    }

    class OcclusionQueryManager : IDisposable
    {
        QueryItem[] queryItems;
        Device device;
        SceneManager scene;

        int totalPixelsVisible = 0;

        public int TotalPixelsVisible
        {
            get { return totalPixelsVisible; }
        }
        int terrainNodesVisible = 0;

        public int TerrainNodesVisible
        {
            get { return terrainNodesVisible; }
            set { terrainNodesVisible = value; }
        }
        int objectsPixelsVisible = 0;

        public int ObjectsPixelsVisible
        {
            get { return objectsPixelsVisible; }
            set { objectsPixelsVisible = value; }
        }

        int objectCount = 0;
        const uint treshold = 0;

        public OcclusionQueryManager(Device device, SceneManager scene, int objectCount)
        {
            this.device = device;
            this.scene = scene;
            this.objectCount = objectCount;
            this.queryItems = new QueryItem[objectCount];
        }
        public void RegisterObjects(IRenderable[] obj)
        {
            Query[] queries = new Query[obj.GetLength(0)];
            for (int i = 0; i < this.objectCount; i++)
            {
                queries[i] = new Query(device, QueryType.Occlusion);
                QueryItem item = new QueryItem(queries[i], obj[i]);
                this.queryItems[i] = item;
            }
        }

        public void IssueAllQueries()
        {
            scene.BeginRenderObject(0, scene[null]);
            for (int i = 0; i < this.objectCount; i++)
            {
                queryItems[i].query.Issue(IssueFlags.Begin);
                scene.RenderObjectBoundingBox(queryItems[i].renderableObject);
                queryItems[i].query.Issue(IssueFlags.End);
            }
            scene.EndRenderObject();
        }

        public int GetResults()
        {
            int visiblePixels = 0;
            for (int i = 0; i < this.objectCount; i++)
            {
                try
                {
                    visiblePixels += GetQueryResult(i);
                    if (this.queryItems[i].renderableObject is WiccanRede.Objects.Terrain)
                    {
                        this.terrainNodesVisible++;
                    }
                    else
                    {
                        this.objectsPixelsVisible += visiblePixels;
                    }
                }
                catch (Exception ex)
                {
                    Logger.AddWarning(ex.ToString());
                }
            }
            return visiblePixels;
        }

        private bool IsQueryResultAvalaible()
        {
            bool bDone;
            this.queryItems[0].query.GetData(typeof(int), false, out bDone);
            return bDone;
        }

        private int GetQueryResult(int i)
        {
            bool bDone = false;
            int visiblePixels = 0;
            while (!bDone)
            {
                visiblePixels = (int) this.queryItems[i].query.GetData(typeof(int), true, out bDone);
            }

            if (visiblePixels > treshold)
            {
                this.queryItems[i].renderableObject.SetVisible(true);
                this.totalPixelsVisible += visiblePixels;

                if (this.queryItems[i].renderableObject is WiccanRede.Graphics.Scene.SpecialObjects.QuadTreeGeneralObject.Key)
                {
                    this.terrainNodesVisible++;
                }
                else if (this.queryItems[i].renderableObject is Objects.Building ||
                    this.queryItems[i].renderableObject is Objects.Player) 
                {
                    this.objectsPixelsVisible += visiblePixels;
                }
            }
            else
            {
                this.queryItems[i].renderableObject.SetVisible(false);
            }

            return visiblePixels;
        }

        public bool IsQueueEmpty()
        {
            if (this.queryItems == null)
                return true;
            if (this.queryItems.GetLength(0) > 0)
                return false;
            else
                return true;
        }

        public void Reset()
        {
            for (int i = 0; i < this.objectCount; i++)
            {
                this.queryItems[i].renderableObject.SetVisible(true);
                this.queryItems[i].renderableObject.ResetVisibility();
            }
            totalPixelsVisible = 0;
            terrainNodesVisible = 0;
            objectsPixelsVisible = 0;
        }

        #region IDisposable Members

        public void Dispose()
        {
            //while (!this.IsQueueEmpty())
            //{
            //    this.queryQueue.Peek().Dispose();
            //    this.queryQueue.Dequeue();
            //}
            for (int i = 0; i < this.objectCount; i++)
            {
                this.queryItems[i].Dispose();
            }
            //if (this.queryQueue != null)
            //{
            //    this.queryQueue = null; 
            //}
        }

        #endregion
    }
}
