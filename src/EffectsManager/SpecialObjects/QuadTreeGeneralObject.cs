using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using WiccanRede.Utils;
using Vertex = WiccanRede.Graphics.Scene.GeneralObject.GeneralVertex;

namespace WiccanRede.Graphics.Scene.SpecialObjects
{
    public class QuadTreeGeneralObject : GeneralObject
    {

        private ISceneCamera camera;

        public class Key : IRenderable
        {
            private static Device device;
            private static VertexDeclaration vertexDeclaration;

            public bool visible;
            public float radius;
            public int[] indeces;

            public Vector3[] bounds;
            public Vector3 boundingSphereCenter;
            public Vector3 boundingBoxPosition;

            public Vertex[] vertexes;
            public IndexBuffer ib;
            public VertexBuffer vb;
            public Mesh mesh;
            public Mesh boundingMesh;
            public Matrix[] boundingMeshWorld;
            public Matrix[] boundingMeshWorldIT;
            private Matrix[] world;
            private Matrix[] worldIT;
            private float distanceToPosition = 0;

            public Key() { }
            public Key(Device device, Matrix world, Vertex[] vertexes, int[] indeces, Vector3 minPosition, Vector3 maxPosition)
            {
                Key.device = device;
                Key.vertexDeclaration = new VertexDeclaration(device, GeneralObject.GeneralVertex.vertexElements);

                ReInit(device, world, vertexes, indeces, minPosition, maxPosition);
            }
            public void ReInit(Device device, Matrix world, Vertex[] vertexes, int[] indeces, Vector3 minPosition, Vector3 maxPosition)
            {
                this.vertexes = vertexes;
                this.indeces = indeces;
                this.bounds = ComputeBounds(minPosition, maxPosition);
                this.visible = true;

                SetMatrixWorld(world);

                GenerateBuffers(device, world, vertexes, indeces);
            }

            private void GenerateBuffers(Device device, Matrix world, Vertex[] vertexes, int[] indeces)
            {
                if (vertexes.Length > 0)
                {
                    this.vb = new VertexBuffer(typeof(Vertex), vertexes.Length, device, Usage.None, Vertex.Format, Pool.Managed);
                    this.vb.SetData(vertexes, 0, LockFlags.None);

                    GraphicsStream gsm = this.vb.Lock(0, 0, LockFlags.ReadOnly);
                    this.boundingSphereCenter = new Vector3();
                    this.radius = Geometry.ComputeBoundingSphere(gsm, vertexes.Length, Vertex.SizeInBytes, out this.boundingSphereCenter);
                    Vector3 min = new Vector3();
                    Vector3 max = new Vector3();
                    Geometry.ComputeBoundingBox(gsm, vertexes.Length, Vertex.SizeInBytes, out min, out max);
                    this.vb.Unlock();

                    Vector3 size = max - min;

                    this.boundingMesh = Mesh.Box(device, size.X, size.Y, size.Z);
                    this.boundingBoxPosition = 0.5f * (this.bounds[7] + this.bounds[0]);
                    this.boundingBoxPosition.Y = this.bounds[0].Y;

                    this.boundingMeshWorld = new Matrix[4];
                    this.boundingMeshWorldIT = new Matrix[4];
                    this.boundingMeshWorld[0] = world * Matrix.Translation(this.boundingBoxPosition);
                    this.boundingMeshWorldIT[0] = Matrix.TransposeMatrix(Matrix.Invert(this.boundingMeshWorld[0]));
                }

                if (indeces.Length > 0)
                {
                    this.ib = new IndexBuffer(typeof(int), indeces.Length, device, Usage.None, Pool.Managed);
                    this.ib.SetData(indeces, 0, LockFlags.None);
                }
            }

            public bool isVertexInQuadrant(Vector3 vertex)
            {
                Vector3 b0 = bounds[0];
                Vector3 b7 = bounds[7];

                if (b0.X <= vertex.X && b7.X >= vertex.X)
                {
                    if (b0.Z <= vertex.Z && b7.Z >= vertex.Z)
                    {
                        return true;
                    }
                }

                return false;
            }

            public static Vector3[] ComputeBounds(Vector3 minBaseBounds, Vector3 maxBaseBounds)
            {
                Vector3[] bounds = new Vector3[8];

                //levej dolni predni
                bounds[0] = minBaseBounds;

                //levej dolni zadni
                bounds[1] = minBaseBounds;
                bounds[1].Z = maxBaseBounds.Z;

                //pravej dolni predni
                bounds[2] = minBaseBounds;
                bounds[2].X = maxBaseBounds.X;

                //pravej dolni zadni
                bounds[3] = maxBaseBounds;
                bounds[3].Y = minBaseBounds.Y;

                //levej horni predni
                bounds[4] = minBaseBounds;
                bounds[4].Y = maxBaseBounds.Y;

                //levej horni zadni
                bounds[5] = maxBaseBounds;
                bounds[5].X = minBaseBounds.X;

                //pravej horni predni
                bounds[6] = maxBaseBounds;
                bounds[6].Z = minBaseBounds.Z;

                //pravej horni zadni
                bounds[7] = maxBaseBounds;

                return bounds;
            }

            #region IRenderable Members

            public void SetVisible(bool visible)
            {
                this.visible = visible;
            }

            public int GetVertexCount()
            {
                return vertexes.Length;
            }

            public int GetFacesCount()
            {
                return indeces.Length / 3;
            }

            public int GetSubsetCount()
            {
                return 1;
            }

            public float GetDistanceToPosition()
            {
                return distanceToPosition;
            }

            public float ComputeDistanceToPosition(Vector3 position)
            {
                distanceToPosition = Vector3.Length(boundingSphereCenter - position);

                return distanceToPosition;
            }

            public bool GetComputedVisibility()
            {
                return visible;
            }

            public bool ComputeVisibility(ISceneCamera camera)
            {
                if (camera == null)
                {
                    this.visible = true;
                    return true;
                }

                ClipVolume cv = camera.GetClipVolume();

                Plane[] p = new Plane[] { cv.pNear, cv.pFar, cv.pLeft, cv.pRight, cv.pTop, cv.pBottom };

                for (int k = 0; k < p.Length; k++)
                {
                    if (p[k].Dot(this.boundingSphereCenter) + this.radius < 0)
                    {
                        this.visible = false;
                        return this.visible;
                    }
                }

                this.visible = true;
                return this.visible;
            }

            /// <summary>
            /// Metoda zrusi predchozi vypocty viditelnosti (objekt bude viden)
            /// </summary>
            public virtual void ResetVisibility()
            {
                visible = true;
            }

            public void SetMatrixWorld(Matrix worldMatrix)
            {
                if (this.world == null)
                {
                    this.world = new Matrix[4];
                    this.worldIT = new Matrix[4];
                }

                this.world[0] = worldMatrix;
                this.worldIT[0] = Matrix.TransposeMatrix(Matrix.Invert(worldMatrix));
            }

            public Matrix GetMatrixWorld()
            {
                return this.world[0];
            }

            public Matrix GetMatrixWorldIT()
            {
                return this.worldIT[0];
            }

            public Matrix[] GetMatricesWorld()
            {
                return world;
            }

            public Matrix[] GetMatricesWorldIT()
            {
                return worldIT;
            }

            public int GetMatrixWorldCount()
            {
                return 1;
            }

            /// <summary>
            /// Metoda vrati world matici bounding meshe
            /// </summary>
            /// <returns>Metoda vrati world matici bounding meshe</returns>
            public Matrix GetMatrixWorldBoundingSphereMesh()
            {
                return boundingMeshWorld[0];
            }

            /// <summary>
            /// Metoda vrati world matici inverzni a transponovanou bounding meshe
            /// </summary>
            /// <returns>Metoda vrati world matici inverzni a transponovanou bounding meshe</returns>
            public Matrix GetMatrixWorldITBoundingSphereMesh()
            {
                return Matrix.TransposeMatrix(Matrix.Invert(GetMatrixWorldBoundingSphereMesh()));
            }

            public void Render(int subset)
            {
                mesh.DrawSubset(subset);
            }

            public void RenderIndexed(int vertexCount)
            {
                device.Indices = ib;
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexes.Length, 0, indeces.Length * 3);
            }

            public void RenderBoundingSphereMesh()
            {
                //boundingMesh.DrawSubset(0);
            }


            public Matrix[] GetMatricesWorldITBoundingMesh()
            {
                return this.boundingMeshWorldIT;
            }

            public Matrix[] GetMatricesWorldBoundingMesh()
            {
                return this.boundingMeshWorld;
            }

            #endregion

        }

        private VertexDeclaration vertexDecl;
        private bool hidden = false;
        private int facesCount = 0;
        private TreeGraph<Key> quadTree;
        public Vertex[] vertexes;
        protected VertexBuffer vb;
        protected BaseMesh mesh;
        protected Device device;

        public QuadTreeGeneralObject(int level, GeneralObject.GeneralVertex[] vertexes, int[] indexes, Vector3 minPosition, Vector3 maxPosition, Matrix world, Texture[] color_textures0, Texture[] color_textures1, Texture[] color_textures2, Texture[] normal_textures)
            : base(null, world, color_textures0, color_textures1, color_textures2, normal_textures)
        {
            this.vertexes = vertexes;

            this.mesh = null;
            this.device = color_textures0[0].Device;

            this.vertexDecl = new VertexDeclaration(this.device, GeneralObject.GeneralVertex.vertexElements);

            this.vb = new VertexBuffer(typeof(GeneralObject.GeneralVertex), vertexes.Length, this.device, Usage.WriteOnly, GeneralObject.GeneralVertex.Format, Pool.Managed);
            this.vb.SetData(vertexes, 0, LockFlags.NoSystemLock);

            Key key = new Key(this.device, base.GetMatrixWorld(), this.vertexes, indexes, minPosition, maxPosition);

            this.radius = key.radius;
            this.boundingSphereCenter = key.boundingSphereCenter;

            this.quadTree = new TreeGraph<Key>(4, key);

            try
            {
                GenerateQuadTree(level, this.quadTree);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        public override void Dispose()
        {

            if (mesh != null && !mesh.Disposed)
                mesh.Dispose();

            DisposeQuadTree(this.quadTree);

            base.Dispose();
        }

        private void DisposeQuadTree(TreeGraph<Key> tree)
        {

            Key k = tree.GetKeys();


            if (k.boundingMesh != null && !k.boundingMesh.Disposed)
                k.boundingMesh.Dispose();

            if (k.ib != null && !k.ib.Disposed)
                k.ib.Dispose();

            if (k.mesh != null && !k.mesh.Disposed)
                k.mesh.Dispose();

            if (k.vb != null && !k.vb.Disposed)
                k.vb.Dispose();

            k.vertexes = null;
            k.indeces = null;
            k.bounds = null;

            k = null;
            tree.SetKeys(null);

            TreeGraph<Key> childtree = null;
            for (int t = 0; t < 4; t++)
            {
                childtree = tree.GetChild(t);

                if (childtree != null)
                    DisposeQuadTree(childtree);
            }
        }

        private void GenerateQuadTree(int level, TreeGraph<Key> quadTree)
        {
            level--;

            if (level < 0)
                return;

            Key baseKey = quadTree.GetKeys();
            Vector3 center = 0.5f * (baseKey.bounds[7] + baseKey.bounds[0]);

            Key[] keys = new Key[4];

            keys[0] = new Key();
            Vector3 lowerCorner = baseKey.bounds[0];
            Vector3 upperCorner = center;
            upperCorner.Y = baseKey.bounds[7].Y;
            keys[0].bounds = Key.ComputeBounds(lowerCorner, upperCorner);
            keys[0].visible = true;

            keys[1] = new Key();
            lowerCorner = baseKey.bounds[0];
            lowerCorner.Z = center.Z;
            upperCorner = baseKey.bounds[7];
            upperCorner.X = center.X;
            keys[1].bounds = Key.ComputeBounds(lowerCorner, upperCorner);
            keys[1].visible = true;

            keys[2] = new Key();
            lowerCorner = baseKey.bounds[0];
            lowerCorner.X = center.X;
            upperCorner = baseKey.bounds[7];
            upperCorner.Z = center.Z;
            keys[2].bounds = Key.ComputeBounds(lowerCorner, upperCorner);
            keys[2].visible = true;

            keys[3] = new Key();
            lowerCorner = baseKey.bounds[0];
            lowerCorner.X = center.X;
            lowerCorner.Z = center.Z;
            upperCorner = baseKey.bounds[7];
            keys[3].bounds = Key.ComputeBounds(lowerCorner, upperCorner);
            keys[3].visible = true;

            GenerateBuffers(keys, baseKey);

            if (level == 0)
            {
                GenerateMesh(keys);

                for (int r = 0; r < 4; r++)
                    quadTree.AddChild(keys[r]);

                return;
            }

            for (int r = 0; r < 4; r++)
                quadTree.AddChild(keys[r]);

            for (int c = 0; c < 4; c++)
            {
                GenerateQuadTree(level, quadTree.GetChild(c));
            }

        }

        private void GenerateBuffers(Key[] keys, Key basekey)
        {
            List<int> indecesInOctant = new List<int>();
            List<Vertex> localVerteces = new List<Vertex>();

            int indecesCount = basekey.indeces.Length;
            int[] indexes = basekey.indeces;

            for (int t = 0; t < 4; t++)
            {
                indecesInOctant.Clear();
                Key key = keys[t];

                for (int index = 0; index < indecesCount; index += 3)
                {
                    int firstIndex = indexes[index];
                    if (firstIndex == -1)
                        continue;

                    if (key.isVertexInQuadrant(vertexes[firstIndex].Position) || key.isVertexInQuadrant(vertexes[indexes[index + 1]].Position) || key.isVertexInQuadrant(vertexes[indexes[index + 2]].Position))
                    {
                        indecesInOctant.Add(indexes[index + 0]);
                        indecesInOctant.Add(indexes[index + 1]);
                        indecesInOctant.Add(indexes[index + 2]);

                        localVerteces.Add(vertexes[indexes[index + 0]]);
                        localVerteces.Add(vertexes[indexes[index + 1]]);
                        localVerteces.Add(vertexes[indexes[index + 2]]);

                        indexes[index + 0] = -1;
                        indexes[index + 1] = -1;
                        indexes[index + 2] = -1;
                    }
                }

                int[] octantIndeces = indecesInOctant.ToArray();
                Vertex[] pverteces = localVerteces.ToArray();

                key.ReInit(this.device, base.GetMatrixWorld(), pverteces, octantIndeces, key.bounds[0], key.bounds[7]);
            }
        }

        private void GenerateMesh(Key[] keys)
        {
            for (int t = 0; t < 4; t++)
            {
                Mesh mesh = new Mesh(keys[t].GetFacesCount(), this.vertexes.Length, MeshFlags.Managed | MeshFlags.Use32Bit, GeneralObject.GeneralVertex.vertexElements, this.device);
                int[] indexes = keys[t].indeces;

                int[] adjency = new int[indexes.Length];

                mesh.SetVertexBufferData(this.vertexes, LockFlags.None);
                mesh.SetIndexBufferData(indexes, LockFlags.None);
                mesh.ComputeNormals();

                keys[t].mesh = mesh;
            }
        }

        public override void Render(int subset)
        {
            facesCount = 0;

            if (!hidden)
                RenderQuadTree(this.quadTree);
        }

        public override void RenderBoundingSphereMesh()
        {
            facesCount = 0;

            Key key = this.quadTree.GetKeys();

            if (key.boundingMesh != null)
            {
                facesCount += key.boundingMesh.NumberFaces;
                key.RenderBoundingSphereMesh();
            }
        }

        private void RenderQuadTree(TreeGraph<Key> quadTree)
        {
            Key key = quadTree.GetKeys();

            if (key.visible)
            {
                if (quadTree.GetChild(0) == null)
                {
                    if (key.ib != null)
                    {
                        facesCount += key.GetFacesCount();
                        key.Render(0);
                        // key.RenderIndexed(vertexes.Length);
                    }

                    return;
                }
                else
                {
                    for (int t = 0; t < 4; t++)
                    {
                        RenderQuadTree(quadTree.GetChild(t));
                    }
                }
            }
        }

        public override void ResetVisibility()
        {
            ComputeVisibility(this.quadTree, true);
        }

        public override bool ComputeVisibility(ISceneCamera camera)
        {
            this.camera = camera;

            ComputeVisibility(this.quadTree);
            return true;

        }

        public override bool GetComputedVisibility()
        {
            return true;
        }

        private void ComputeVisibility(TreeGraph<Key> tree, bool visible)
        {
            Key key = tree.GetKeys();
            key.visible = visible;
            tree.SetKeys(key);

            TreeGraph<Key> childtree = null;
            for (int t = 0; t < 4; t++)
            {
                childtree = tree.GetChild(t);

                if (childtree != null)
                    ComputeVisibility(childtree, visible);
            }
        }

        private void ComputeVisibility(TreeGraph<Key> tree)
        {
            Key key = tree.GetKeys();

            /*
            for (int k = 0; k < p.Length; k++)
            {
                int count = 8;

                for (int i = 0; i < key.bounds.Length; i++)
                {
                    if (p[k].Dot(key.bounds[i]) < 0)
                    {
                        count--;
                    }
                }

                if (count == 0)
                {
                    key.visible = false;
                    tree.SetKeys(key);
                    return;
                }
            }
            */

            key.ComputeVisibility(camera);
            tree.SetKeys(key);

            TreeGraph<Key> childtree = null;
            for (int t = 0; t < 4; t++)
            {
                childtree = tree.GetChild(t);

                if (childtree != null)
                    ComputeVisibility(childtree);
            }
        }

        public override int GetVertexCount()
        {
            return vertexes.Length;
        }

        public override int GetFacesCount()
        {
            return facesCount;
        }

        public TreeGraph<Key> GetQuadTree()
        {
            return quadTree;
        }

        public List<Key> GetQuadTreeLeaves()
        {
            List<Key> leaves = new List<Key>();

            TreeGraph<Key> tree = this.quadTree.GetTreeLeaves();

            do
            {
                leaves.Add(tree.GetKeys());
                tree = tree.GetRightNeighboure();
            } while (tree != null);

            return leaves;
        }

        public void SetQuadTreeLeaves(List<Key> leaves)
        {

            int index = 0;
            TreeGraph<Key> tree = this.quadTree.GetTreeLeaves();

            do
            {
                tree.SetKeys(leaves[index++]);
                tree = tree.GetRightNeighboure();
            } while (tree != null);

        }

        public override void SetVisible(bool visible)
        {
            base.SetVisible(visible);
            this.hidden = !visible;
        }

    }
}
