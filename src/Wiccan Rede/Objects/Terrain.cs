using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using TerainVertex = WiccanRede.Graphics.Scene.GeneralObject.GeneralVertex;

using WiccanRede.Graphics.Scene;
using WiccanRede.Graphics.Utils;
using WiccanRede.Graphics;
using WiccanRede.Graphics.Scene.SpecialObjects;

namespace WiccanRede.Objects
{
    class Terrain : QuadTreeGeneralObject, WiccanRede.AI.IWalkable
    {
        private static float heightMeasure = 1f;
        private static float tileWidth = 80f;
        private static float tileHeight = 80f;
        private static Vector3 minPosition;
        private static Vector3 maxPosition;

        public static TerainVertex[] GenerateVertexes(Device device, Bitmap heightMap)
        {
            heightMeasure = (tileWidth + tileHeight) * 0.25f * 0.2f;

            int width = heightMap.Width;
            int height = heightMap.Height;
            int primitiveCount = 2 * (width - 1) * (height - 1);
            int vertexCount = width * height;
            int indexCount = 3 * primitiveCount;

            Color[,] map = BitmapOperation.BitmapToColorArray(heightMap);
            TerainVertex[] vertexes = new TerainVertex[vertexCount];
            int[] indices = new int[indexCount];
            int[] adjency = new int[indexCount];

            Terrain.minPosition = new Vector3(-(0.5f * (width - 1) * tileWidth), -127 * heightMeasure, -(0.5f * (height - 1) * tileHeight));
            Terrain.maxPosition = new Vector3(tileWidth * (width - 1) * 0.5f, 127 * heightMeasure, tileHeight * (height - 1) * 0.5f);

            #region Generate Vertexes and Indexes
            int i = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float posX = x * tileWidth - (0.5f * (width - 1) * tileWidth);
                    float posY = map[x, y].B * heightMeasure;
                    float posZ = y * tileHeight - (0.5f * (height - 1) * tileHeight);

                    Vector3 pos = new Vector3(posX, posY, posZ);
                    Vector3 nor = new Vector3();

                    float u = (float)x;
                    float v = (float)y;

                    if (x + 1 < width)
                    {

                        if (y + 1 < height)
                        {
                            #region Generate Index Buffer
                            indices[i] = (y * width + x);
                            indices[i + 1] = ((y + 1) * width + x);
                            indices[i + 2] = (y * width + x + 1);

                            indices[i + 3] = (indices[i + 2]);
                            indices[i + 4] = (indices[i + 1]);
                            indices[i + 5] = (indices[i + 1] + 1);

                            i += 6;
                            #endregion
                        }
                    }

                    vertexes[y * width + x] = new TerainVertex(pos, nor, u, v);
                }
            }
            #endregion

            return vertexes;
        }
        public static int[] GenerateIndexes(Device device, Bitmap heightMap)
        {
            heightMeasure = (tileWidth + tileHeight) * 0.25f * 0.2f;

            int width = heightMap.Width;
            int height = heightMap.Height;
            int primitiveCount = 2 * (width - 1) * (height - 1);
            int vertexCount = width * height;
            int indexCount = 3 * primitiveCount;

            Color[,] map = BitmapOperation.BitmapToColorArray(heightMap);
            TerainVertex[] vertexes = new TerainVertex[vertexCount];
            int[] indices = new int[indexCount];
            int[] adjency = new int[indexCount];

            #region Generate Vertexes and Indexes
            int i = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float posX = x * tileWidth - (0.5f * (width - 1) * tileWidth);
                    float posY = map[x, y].B * heightMeasure;
                    float posZ = y * tileHeight - (0.5f * (height - 1) * tileHeight);

                    Vector3 pos = new Vector3(posX, posY, posZ);
                    Vector3 nor = new Vector3();

                    float u = (float)x;
                    float v = (float)y;

                    if (x + 1 < width)
                    {

                        if (y + 1 < height)
                        {
                            #region Generate Index Buffer
                            indices[i] = (y * width + x);
                            indices[i + 1] = ((y + 1) * width + x);
                            indices[i + 2] = (y * width + x + 1);

                            indices[i + 3] = (indices[i + 2]);
                            indices[i + 4] = (indices[i + 1]);
                            indices[i + 5] = (indices[i + 1] + 1);

                            i += 6;
                            #endregion
                        }
                    }

                    vertexes[y * width + x] = new TerainVertex(pos, nor, u, v);
                }
            }
            #endregion

            return indices;
        }

        Color[,] heightMap;
        Color[,] collissionMap;
        List<Point> blocked;

        public Terrain(int level, Device device, Bitmap heightMap, Texture mask, Texture layer1, Texture layer2, Texture layer3)
            : base(level, GenerateVertexes(device, heightMap), GenerateIndexes(device, heightMap), Terrain.minPosition, Terrain.maxPosition, Matrix.Identity, new Texture[] { layer1 }, new Texture[] { layer2 }, new Texture[] { layer3 }, null)
        {
            this.heightMap = BitmapOperation.BitmapToColorArray(heightMap);
            blocked = new List<Point>();

            base.SetIsEveryWhere(true);

            CameraDriver.SetAttachedTerain(this);
        }

        public Color[,] GetCollissionMap()
        {
            return collissionMap;
        }

        public void SetCollissionMap(Color[,] collissionMap, bool invertHeight)
        {
            this.collissionMap = collissionMap;
            blocked.Clear();

            if (collissionMap == null)
                return;

            collissionMap = WiccanRede.Graphics.Utils.BitmapOperation.Blur(collissionMap);

            int width = collissionMap.GetLength(0);
            int height = collissionMap.GetLength(1);

            Color[,] nmap = new Color[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (collissionMap[x, y].B < 200)
                    {
                        if (invertHeight)
                            blocked.Add(new Point(x, height - 1 - y));
                        else
                            blocked.Add(new Point(x, y));
                    }

                    nmap[x, y] = collissionMap[x, height - 1 - y]; 
                }
            }

            if (invertHeight)
                this.collissionMap = nmap;
        }

        #region IWalkable Members

        private float GetHeightOnCoordinates(int x, int y)
        {
            if (x < 0 || x >= heightMap.GetLength(0))
                return 0;

            if (y < 0 || y >= heightMap.GetLength(1))
                return 0;

            int t = heightMap[x, y].B;

            return t * heightMeasure;
        }

        public Vector2 ConvertToTerrainPosition(Vector3 worldPosition)
        {
            int x = (int)Math.Round((worldPosition.X + (0.5f * (float)(heightMap.GetLength(0) - 1) * tileWidth)) / tileWidth);
            int y = (int)Math.Round((worldPosition.Z + (0.5f * (float)(heightMap.GetLength(1) - 1) * tileHeight)) / tileHeight);

            return new Vector2(x, y);
        }

        public bool IsPositionOnTereain(Vector3 position)
        {
            int x = (int)Math.Round((position.X + (0.5f * (float)(heightMap.GetLength(0) - 1) * tileWidth)) / tileWidth);
            int y = (int)Math.Round((position.Z + (0.5f * (float)(heightMap.GetLength(1) - 1) * tileHeight)) / tileHeight);

            if (x <= 0 || y <= 0 || x >= heightMap.GetLength(0) - 1 || y >= heightMap.GetLength(1) - 1)
                return false;

            return true;
        }
        public bool IsPositionOnTerainBlocked(Vector3 position)
        {
            if (collissionMap == null)
                return false;

            float x = ((position.X + (0.5f * (float)(heightMap.GetLength(0) - 1) * tileWidth)) / tileWidth);
            float z = ((position.Z + (0.5f * (float)(heightMap.GetLength(1) - 1) * tileHeight)) / tileHeight);

            int xs = (int)Math.Round(x);
            int zs = (int)Math.Round(z);

            if (xs < 0 || zs < 0 || xs >= heightMap.GetLength(0) || zs >= heightMap.GetLength(1))
                return true;

            if (collissionMap[xs, zs].B < 200)
                return true;

            return false;

        }
        public float GetHeightFromCollissionMap(int x, int y)
        {
            float terreinHeight = (((float)collissionMap[x, y].R / 255f) * 1250) + 1000;
            float itemHeight = (((float)collissionMap[x, y].G / 255f) * 1250) + 1000;
            return (terreinHeight < itemHeight)? itemHeight:terreinHeight;
        }
        public Vector3 GetPositionOnTerain(Vector3 position)
        {
            float x = ((position.X + (0.5f * (float)(heightMap.GetLength(0) - 1) * tileWidth)) / tileWidth);
            float z = ((position.Z + (0.5f * (float)(heightMap.GetLength(1) - 1) * tileHeight)) / tileHeight);

            int xs = (int)x;
            int zs = (int)z;

            float dx = x - xs;
            float dy = z - zs;

            float ix1 = (1f - dx) * GetHeightOnCoordinates(xs, zs) + dx * GetHeightOnCoordinates(xs + 1, zs);
            float ix2 = (1f - dx) * GetHeightOnCoordinates(xs, zs + 1) + dx * GetHeightOnCoordinates(xs + 1, zs + 1);

            float iy = (1f - dy) * ix1 + dy * ix2;

            position.Y = iy;

            return position;
        }
        public Vector2 GetTerrainSize()
        {
            return new Vector2(heightMap.GetLength(0) * tileWidth, heightMap.GetLength(1) * tileHeight);
        }
        public PointF GetSize()
        {
            return new PointF(heightMap.GetLength(0) * tileWidth, heightMap.GetLength(1) * tileHeight);
        }


        public Point Get2DMapPosition(Vector3 position)
        {
            if (!IsPositionOnTereain(position))
                return new Point(0, 0);
            int x = (int)Math.Round((position.X + (0.5f * (float)(heightMap.GetLength(0) - 1) * tileWidth)) / tileWidth);
            int y = (int)Math.Round((position.Z + (0.5f * (float)(heightMap.GetLength(1) - 1) * tileHeight)) / tileHeight);
            return new Point(x, y);
        }

        public Vector3 Get3Dposition(Point position2D)
        {
            float height = GetHeightOnCoordinates(position2D.X, position2D.Y);

            float posunX = (0.5f * (float)(heightMap.GetLength(0) - 1) * tileWidth);
            float posunY = (0.5f * (float)(heightMap.GetLength(1) - 1) * tileHeight);

            return new Vector3(position2D.X * tileWidth - posunX, height, position2D.Y * tileHeight - posunY);
        }

        public Point GetPlayerPosition()
        {
            Vector3 pos = Camera.GetCameraInstance().GetVector3Position();
            return Get2DMapPosition(pos);
        }

        public Vector3 GetPlayerPosition3D()
        {
            return Camera.GetCameraInstance().GetVector3Position();
        }

        public int[,] GetMap()
        {
            int x = this.heightMap.GetLength(0);
            int y = this.heightMap.GetLength(1);
            int[,] map = new int[x, y];

            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    map[i, j] = (int)(heightMap[i, j].B * heightMeasure);
                }
            }

            //unsafe
            //{
            //    fixed (int* pMap = map)
            //    {
            //        for (int i = 0; i < x; i++)
            //        {
            //            for (int j = 0; j < y; j++)
            //            {
            //                pMap[j + x * i] = (int)(heightMap[i, j].B * heightMeasure);
            //            }
            //        }
            //    }
            //}

            return map;
        }

        public List<Point> GetBlockedPositions()
        {
            return blocked;
        }

        #endregion
    }
}
