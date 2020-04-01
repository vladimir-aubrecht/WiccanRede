using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

using positionOnlyVertex = Microsoft.DirectX.Direct3D.CustomVertex.PositionOnly;

namespace WiccanRede.Graphics.Scene
{
    public abstract class GeneralObject : IGeneralObject
    {

        public struct GeneralVertex
        {
            public Vector3 Position;
            public Vector3 weights;
            public UInt32 boneIndices;
            public Vector3 Normal;
            public Vector2 TuTv;
            public Vector3 tangent;


            public GeneralVertex(Vector3 position, Vector3 normal, float Tu, float Tv)
            {
                this.Position = position;
                this.Normal = normal;
                this.TuTv = new Vector2(Tu, Tv);
                this.weights = new Vector3(1f, 1f, 1f);
                this.boneIndices = 0;
                this.tangent = new Vector3();
            }

            public static readonly VertexElement[] vertexElements =
            {
                new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                new VertexElement(0, 12, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.BlendWeight, 0),
                new VertexElement(0, 24, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.BlendIndices, 0),
                new VertexElement(0, 28, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 40, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, 48, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Tangent, 0),
                VertexElement.VertexDeclarationEnd
            };

            public static VertexFormats Format
            {
                get
                {
                    return VertexFormats.Position | VertexFormats.PositionBlend2 | VertexFormats.LastBetaUByte4 | VertexFormats.Normal | VertexFormats.Texture1;
                }
            }

            // Describe the size of this vertex structure.
            public const int SizeInBytes = 60;
        }

        #region Fields
        BaseMesh mesh;
        Matrix[] world;
        Matrix[] worldIT;
        Matrix worldOriginal;
        Vector3 position;
        float scale;
        Texture[] color_textures0;
        Texture[] color_textures1;
        Texture[] color_textures2;
        Texture[] normal_textures;

        float distance = 0;
        float distanceToPosition = 0;
        int vertexCount = 0;
        int facesCount = 0;

        int numberVerteces = 0;
        int numberFaces = 0;

        bool hidden = false;
        bool visible = true;
        bool useAlphablending = false;
        bool isEveryWhere = false;
        bool disposed = false;
        bool enableLOD = true;
        bool equiped = false;

        float objectQuality = 1f;

        private Matrix[] boundingMeshWorld;
        private Matrix[] boundingMeshWorldIT;
        protected Mesh boundingSphereMesh;
        protected Mesh boundingBoxMesh;
        protected Vector3 boundingSphereCenter;
        protected Vector3 minBoundingPosition;
        protected Vector3 maxBoundingPosition;
        protected Vector3[] boundingBoxNormals;
        protected float radius = 0;
        #endregion

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="mesh">Objekt, ktery se bude kreslit</param>
        /// <param name="world">World matice</param>
        /// <param name="color_textures0">Pole textur nultyho bufferu</param>
        /// <param name="color_textures1">Pole textur prvniho bufferu</param>
        /// <param name="color_textures2">Pole textur druhyho bufferu</param>
        /// <param name="normal_textures">Pole normalovych textur</param>
        public GeneralObject(BaseMesh mesh, Matrix world, Texture[] color_textures0, Texture[] color_textures1, Texture[] color_textures2, Texture[] normal_textures)
        {
            SetModel(mesh);
            this.color_textures0 = color_textures0;
            this.color_textures1 = color_textures1;
            this.color_textures2 = color_textures2;
            this.normal_textures = normal_textures;

            worldOriginal = world;

            SetMatrixWorld(world);

            if (mesh != null)
            {
                VertexBuffer vb = mesh.VertexBuffer;
                GraphicsStream gsm = vb.Lock(0, 0, LockFlags.ReadOnly);
                radius = Geometry.ComputeBoundingSphere(gsm, mesh.NumberVertices, GeneralVertex.SizeInBytes, out boundingSphereCenter);
                Geometry.ComputeBoundingBox(gsm, mesh.NumberVertices, GeneralVertex.SizeInBytes, out minBoundingPosition, out maxBoundingPosition);
                vb.Unlock();

                Vector3 size = maxBoundingPosition - minBoundingPosition;

                boundingSphereMesh = Mesh.Sphere(this.mesh.Device, radius, 8, 8);

                Vector3 length = maxBoundingPosition - minBoundingPosition;

                boundingBoxMesh = Mesh.Box(this.mesh.Device, length.X, length.Y, length.Z);
                Mesh temp = boundingSphereMesh.Clone(MeshFlags.Managed, GeneralObject.GeneralVertex.vertexElements, vb.Device);
                boundingSphereMesh.Dispose();
                boundingSphereMesh = temp;
                temp = boundingBoxMesh.Clone(MeshFlags.Managed, GeneralObject.GeneralVertex.vertexElements, vb.Device);
                boundingBoxMesh.Dispose();
                boundingBoxMesh = temp;

                boundingMeshWorld = new Matrix[4];
                boundingMeshWorldIT = new Matrix[4];
                boundingMeshWorld[0] = Matrix.Translation(boundingSphereCenter) * GetMatrixWorld();
                boundingMeshWorld[1] = Matrix.Translation(0.5f * (minBoundingPosition + maxBoundingPosition)) * GetMatrixWorld();
                boundingMeshWorldIT[0] = Matrix.TransposeMatrix(Matrix.Invert(boundingMeshWorld[0]));
                boundingMeshWorldIT[1] = Matrix.TransposeMatrix(Matrix.Invert(boundingMeshWorld[1]));

                boundingBoxNormals = CreateBoundingBoxNormals();
            }
        }

        /// <summary>
        /// Provede uklid textur, meshe a bounding boxu
        /// </summary>
        public virtual void Dispose()
        {
            if (normal_textures != null)
            {
                for (int t = 0; t < normal_textures.Length; t++)
                {
                    if (normal_textures[t] != null && !normal_textures[t].Disposed)
                    {
                        normal_textures[t].Dispose();
                    }
                }
            }

            if (color_textures0 != null)
            {
                for (int t = 0; t < color_textures0.Length; t++)
                {
                    if (color_textures0[t] != null && !color_textures0[t].Disposed)
                    {
                        color_textures0[t].Dispose();
                    }
                }
            }

            if (mesh != null && !mesh.Disposed)
            {
                try
                {
                    mesh.Dispose();
                    mesh = null;
                }
                catch { }
            }

            if (boundingSphereMesh != null && !boundingSphereMesh.Disposed)
                boundingSphereMesh.Dispose();

            disposed = true;
        }

        /// <summary>
        /// Metoda se vola pri ztrate zarizeni pro obnovu zdroju
        /// </summary>
        public virtual void ReInit()
        {

        }

        /// <summary>
        /// Metoda uvolni vsechny zdroje pri ztrate zarizeni
        /// </summary>
        public virtual void Releasse()
        {
        }

        /// <summary>
        /// Funkce se automaticky spousti pri prihozeni objektu na rendering, slouzi pro inicializaci nestandartnich hodnot do shaderu
        /// </summary>
        /// <param name="effect">Effect, pres ktery je objekt renderovan</param>
        public virtual void InitShaderValue(Effect effect)
        {

        }

        /// <summary>
        /// Funkce je volana automaticky vzdy pred funkci UpdateShaderValue, slouzi k aktualizaci hodnot zavislych na case
        /// </summary>
        /// <param name="time">Cas v milisekundach od spusteni aplikace</param>
        public virtual void Update(float time)
        {
        }

        /// <summary>
        /// Metoda se automaticky spousti pred renderingem kazdeho subsetu
        /// </summary>
        /// <param name="subset">Subset, ktery se bude zrovna renderovat</param>
        public virtual void UpdateSubset(int subset)
        {

        }

        /// <summary>
        /// Funkce provede update hodnot v shaderu - je automaticky volana pred vykreslenim objektu
        /// </summary>
        /// <param name="effect">Effect, pres ktery je objekt renderovan</param>
        public virtual void UpdateShaderValue(Effect effect)
        {

        }

        /// <summary>
        /// Funkce slouzi pro rendering vlastniho objektu
        /// </summary>
        /// <param name="subset">Subset, ktery se ma kreslit</param>
        public virtual void Render(int subset)
        {
            vertexCount = 0;
            facesCount = 0;

            if (mesh == null || mesh.Disposed)
                return;

            if (!this.hidden)
            {
                if (visible)
                {
                    vertexCount = this.numberVerteces;
                    facesCount = this.numberFaces;
                    mesh.DrawSubset(subset);
                }
            }
        }

        /// <summary>
        /// Funkce slouzi pro rendering bounding boxu objektu
        /// </summary>
        public virtual void RenderBoundingSphereMesh()
        {
            if (!this.hidden)
            {
                if (boundingSphereMesh != null)
                {
                    boundingSphereMesh.DrawSubset(0);
                }
            }
        }

        #region Setry
        /// <summary>
        /// Nastavi model, ktery bude pouzit pro rendering
        /// </summary>
        /// <param name="mesh">Model</param>
        public void SetModel(BaseMesh mesh)
        {
            this.mesh = mesh;

            UpdateVerticesFacesCounters();
        }

        /// <summary>
        /// Nastavi bounding box model
        /// </summary>
        /// <param name="mesh">Bounding box model</param>
        public void SetBoundingBoxModel(Mesh mesh)
        {
            boundingSphereMesh = mesh;
        }

        /// <summary>
        /// Nastavi world matici na konkretnim indexu
        /// </summary>
        /// <param name="world">Matice, ktera se ma nastavit</param>
        /// <param name="index">Index na ktery se ma matice nastavit (je na intervalu 0 az 3 vcetne)</param>
        /// <remarks>Tato metoda nema vliv na pozici objektu!</remarks>
        public virtual void SetMatricesWorldByIndex(Matrix world, int index)
        {
            if (index >= this.world.Length)
                return;

            this.world[index] = world;
            this.worldIT[index] = Matrix.TransposeMatrix(Matrix.Invert(world));
        }

        /// <summary>
        /// Funkce nastavi world matici a spocita a nastavi i inverzni transponovanou world matici
        /// </summary>
        /// <param name="worldMatrix">World matice, ktera se ma nastavit</param>
        public virtual void SetMatrixWorld(Matrix worldMatrix)
        {
            if (this.world == null)
            {
                this.world = new Matrix[4];
                this.worldIT = new Matrix[4];
                this.boundingMeshWorld = new Matrix[4];
                this.boundingMeshWorldIT = new Matrix[4];
            }

            this.world[0] = worldMatrix;
            this.worldIT[0] = Matrix.TransposeMatrix(Matrix.Invert(worldMatrix));

            this.position = new Vector3(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
            this.position *= (1f / worldMatrix.M44);

            this.boundingMeshWorld[0] = Matrix.Translation(GetSphereObjectRelativeCenter()) * GetMatrixWorld();
            this.boundingMeshWorldIT[0] = Matrix.TransposeMatrix(Matrix.Invert(this.boundingMeshWorld[0]));
            this.boundingMeshWorld[1] = Matrix.Translation(0.5f * (minBoundingPosition + maxBoundingPosition)) * GetMatrixWorld();
            this.boundingMeshWorldIT[1] = Matrix.TransposeMatrix(Matrix.Invert(boundingMeshWorld[1]));

            this.scale = (float)Math.Sqrt(boundingMeshWorld[0].M11 * boundingMeshWorld[0].M11 + boundingMeshWorld[0].M22 * boundingMeshWorld[0].M22 + boundingMeshWorld[0].M33 * boundingMeshWorld[0].M33);
        }

        /// <summary>
        /// Nastavi viditelnost objektu
        /// </summary>
        /// <param name="visible">Pri nastaveni false se objekt nikdy nenakresli, pri true, pokud je videt ano</param>
        public virtual void SetVisible(bool visible)
        {
            this.hidden = !visible;
        }

        /// <summary>
        /// Nastavi objektu, zda pouziva alphablending
        /// </summary>
        /// <param name="useAlphaBlending">True, pokud objekt pouziva alpha blending</param>
        protected void SetUseAlphaBlending(bool useAlphaBlending)
        {
            this.useAlphablending = useAlphaBlending;
        }

        /// <summary>
        /// Metoda nastavi kvalitu modelu, ktery se bude renderovat
        /// </summary>
        /// <remarks>Nastavuje kvalitu jak z pohledu geometrie, tak textur, efektu, atp., zavisi na konkretnim objektu</remarks>
        /// <param name="quality">Uroven kvality, plati na intervalu (0,1) vcetne krajnich bodu, kde 0 je nejnizsi kvalita a 1 je nejvyssi kvalita</param>
        public virtual void SetObjectQuality(float quality)
        {
            this.objectQuality = quality;
        }

        /// <summary>
        /// Povoli/zakaze LOD objektu
        /// </summary>
        public virtual void EnableLOD(bool enableLOD)
        {
            this.enableLOD = enableLOD;
        }

        /// <summary>
        /// Nastavi novou pozici objektu
        /// </summary>
        /// <param name="position">Pozice objektu ve world space</param>
        public void SetPosition(Vector3 position)
        {
            Matrix world = this.GetMatrixWorld();
            position *= world.M44;

            world.M41 = position.X;
            world.M42 = position.Y;
            world.M43 = position.Z;

            this.SetMatrixWorld(world);
        }

        /// <summary>
        /// Natoci objekt pozadovanym smerem
        /// </summary>
        /// <param name="direction">Smer, do ktereho se ma objekt natocit</param>
        public void SetDirection(Vector3 direction)
        {
            float angle = (float)Math.Acos(Vector3.Dot(direction, new Vector3(0, 0, -1)));

            if (direction.X > 0)
                angle = -angle;

            Matrix rotationY = Matrix.RotationY(angle);

            Matrix world = worldOriginal;
            Vector3 positionPlayer = new Vector3(this.world[0].M41, this.world[0].M42, this.world[0].M43) * (1f / this.world[0].M44);
            positionPlayer *= world.M44;

            world.M41 = positionPlayer.X;
            world.M42 = positionPlayer.Y;
            world.M43 = positionPlayer.Z;
            world = rotationY * world;
            this.SetMatrixWorld(world);
        }

        /// <summary>
        /// Metoda slouzi pro nastaveni priznaku, ze objekt se nachazi vsude
        /// </summary>
        /// <param name="isEveryWhere">True, pokud se objekt nachazi vsude, jinak false</param>
        public void SetIsEveryWhere(bool isEveryWhere)
        {
            this.isEveryWhere = isEveryWhere;
        }
        #endregion

        #region Getry
        /// <summary>
        /// Vraci model, ktery se bude prave renderovat
        /// </summary>
        /// <returns>Vraci model, ktery se bude prave renderovat</returns>
        public BaseMesh GetModel()
        {
            return mesh;
        }

        /// <summary>
        /// Vrati mesh s bounding boxem
        /// </summary>
        /// <returns>Vrati mesh s bounding boxem</returns>
        public Mesh GetBoundingModel()
        {
            return boundingSphereMesh;
        }

        /// <summary>
        /// Vrati pocet vertexu, ktere se renderuji
        /// </summary>
        /// <returns>Vrati pocet vertexu, ktere se renderuji</returns>
        public virtual int GetVertexCount()
        {
            return vertexCount;
        }

        /// <summary>
        /// Vrati pocet trojuhelniku, ktere se renderuji
        /// </summary>
        /// <returns>Vrati pocet trojuhelniku, ktere se renderuji</returns>
        public virtual int GetFacesCount()
        {
            return facesCount;
        }

        /// <summary>
        /// Funkce vraci pocet subsetu v meshi
        /// </summary>
        /// <returns>Funkce vraci pocet subsetu v meshi</returns>
        public virtual int GetSubsetCount()
        {
            if (mesh != null && color_textures0 != null)
                return color_textures0.Length;

            return 1;
        }

        /// <summary>
        /// Vraci souradnice stredu objektu ve World souradnicich
        /// </summary>
        /// <returns>Bod se souradnicema objektu</returns>
        public Vector3 GetSphereObjectCenter()
        {
            return Vector3.TransformCoordinate(boundingSphereCenter, world[0]);
        }

        /// <summary>
        /// Vraci souradnice stredu objektu
        /// </summary>
        /// <returns>Bod se souradnicema objektu</returns>
        public Vector3 GetSphereObjectRelativeCenter()
        {
            return boundingSphereCenter;
        }

        /// <summary>
        /// Metoda vrati pozici stredu bounding boxu ve world souradnicich
        /// </summary>
        /// <returns>Metoda vrati pozici stredu bounding boxu ve world souradnicich</returns>
        public Vector3 GetBoundingBoxCenter()
        {
            return Vector3.TransformCoordinate(0.5f * (minBoundingPosition + maxBoundingPosition), world[0]);
        }

        /// <summary>
        /// Metoda vrati pozici stredu bounding boxu
        /// </summary>
        /// <returns>Metoda vrati pozici stredu bounding boxu</returns>
        public Vector3 GetBoundingBoxRelativeCenter()
        {
            return 0.5f * (minBoundingPosition + maxBoundingPosition);
        }

        /// <summary>
        /// Vrati souradnice bodu na nejvyssich souradnicich
        /// </summary>
        /// <returns>Vrati souradnice bodu na nejvyssich souradnicich</returns>
        public Vector3 GetBoundingBoxRelativeMaximum()
        {
            return maxBoundingPosition;
        }

        /// <summary>
        /// Vrati souradnice bodu na nejnizsich souradnicich
        /// </summary>
        /// <returns>Vrati souradnice bodu na nejnizsich souradnicich</returns>
        public Vector3 GetBoundingBoxRelativeMinimum()
        {
            return minBoundingPosition;
        }

        /// <summary>
        /// Metoda vrati normaly kazde steny bounding boxu (ve world souradnicich)
        /// </summary>
        /// <returns>Metoda vrati normalizovane normaly kazde steny bounding boxu (ve world souradnicich)</returns>
        /// <remarks>Metoda je zavisla na predchozim volani metody CreateBoundingBoxNormals (je automaticky volana z konstruktoru teto tridy)</remarks>
        /// <seealso cref="CreateBoundingBoxNormals"/>
        public Vector3[] GetBoxObjectNormals()
        {
            if (boundingBoxNormals != null)
                return Vector3.TransformCoordinate(boundingBoxNormals, worldIT[0]);
            else
                return null;
        }

        /// <summary>
        /// Metoda vypocita normaly sten bounding boxu
        /// </summary>
        /// <returns>Vraci normalizovane pole normal s normalami sten bounding boxu</returns>
        /// <remarks>Pole je serazeno: 0 - leva stena, 1 - prava stena, 2 - dolni stena, 3 - horni stena, 4 - predni stena, 5 - zadni stena</remarks>
        private Vector3[] CreateBoundingBoxNormals()
        {
            Vector3 length = maxBoundingPosition - minBoundingPosition;
            Vector3 boxCenter = 0.5f * (minBoundingPosition + maxBoundingPosition);
            Vector3 right = new Vector3(1, 0, 0);
            Vector3 up = new Vector3(0, 1, 0);
            Vector3 forward = new Vector3(0, 0, 1);

            Vector3[] A0 = new Vector3[6];
            A0[0] = boxCenter + new Vector3(-length.X, 0, 0);
            A0[1] = boxCenter + new Vector3(+length.X, 0, 0);
            A0[2] = boxCenter + new Vector3(0, -length.Y, 0);
            A0[3] = boxCenter + new Vector3(0, +length.Y, 0);
            A0[4] = boxCenter + new Vector3(0, 0, -length.Z);
            A0[5] = boxCenter + new Vector3(0, 0, +length.Z);

            Vector3[] A1 = new Vector3[6];
            A1[0] = A0[0] + forward;
            A1[1] = A0[1] + forward;
            A1[2] = A0[2] + right;
            A1[3] = A0[3] + right;
            A1[4] = A0[4] + right;
            A1[5] = A0[5] + right;

            Vector3[] A2 = new Vector3[6];
            A2[0] = A0[0] + up;
            A2[1] = A0[1] + up;
            A2[2] = A0[2] + forward;
            A2[3] = A0[3] + forward;
            A2[4] = A0[4] + up;
            A2[5] = A0[5] + up;

            Vector3[] n = new Vector3[6];
            for (int i = 0; i < 6; i++)
                n[i] = Vector3.Normalize(Vector3.Cross(A1[i], A2[i]));

            return n;
        }

        /// <summary>
        /// Vrati pozici objektu
        /// </summary>
        /// <returns>Vrati pozici objektu</returns>
        public virtual Vector3 GetPosition()
        {
            return new Vector3(world[0].M41, world[0].M42, world[0].M43) * (1f / world[0].M44);
        }

        /// <summary>
        /// Vraci radius od stredu obalove koule
        /// </summary>
        /// <returns>Radius obalove koule</returns>
        public virtual float GetSphereRadius()
        {
            return radius;
        }

        /// <summary>
        /// Metoda vrati vzdalenost od kamery
        /// </summary>
        /// <remarks>Vzdalenost je nejdrive nutne vypocitat pomoci metody ComputeDistanceFromCamera</remarks>
        /// <returns>Vzdalenost od kamery</returns>
        /// <seealso cref="ComputeDistanceFromCamera"/>
        public float GetDistanceFromCamera()
        {
            return distance;
        }

        /// <summary>
        /// Vrati vzdalenost od pozice
        /// </summary>
        /// <returns>Vrati vzdalenost od pozice</returns>
        /// <remarks>Jedna se o vzdalenost od pozice, ktera byla naposled predana funkci ComputeDistanceToPosition()</remarks>
        /// <seealso cref="ComputeDistanceToPosition"/>
        public float GetDistanceToPosition()
        {
            return distanceToPosition;
        }

        /// <summary>
        /// Metoda vrati pocet volnosti
        /// </summary>
        /// <returns>Vrati pocet volnosti</returns>
        public virtual int GetMatrixWorldCount()
        {
            return 1;
        }

        /// <summary>
        /// Vrati, zda objekt pouziva alpha blending
        /// </summary>
        /// <returns>Vrati, zda objekt pouziva alpha blending</returns>
        public bool GetUseAlphaBlending()
        {
            return this.useAlphablending;
        }

        /// <summary>
        /// Metoda slouzi pro zjisteni priznaku, zda objekt se nachazi vsude
        /// </summary>
        /// <param name="isEveryWhere">True, pokud se objekt nachazi vsude, jinak false</param>
        public bool GetIsEveryWhere()
        {
            return this.isEveryWhere;
        }

        /// <summary>
        /// Metoda vrati kvalitu modelu, ktery se bude renderovat
        /// </summary>
        /// <returns>Uroven kvality, plati na intervalu (0,1) vcetne krajnich bodu, kde 0 je nejnizsi kvalita a 1 je nejvyssi kvalita</returns>
        public virtual float GetObjectQuality()
        {
            return this.objectQuality;
        }

        /// <summary>
        /// Vrati, zda je objekt viditelny, ci ne z pohledu kamery
        /// </summary>
        /// <returns>Vrati, zda je objekt viditelny, ci ne z pohledu kamery</returns>
        public virtual bool GetComputedVisibility()
        {
            return visible;
        }

        /// <summary>
        /// Vrati vsechny world matice
        /// </summary>
        /// <returns></returns>
        public virtual Matrix[] GetMatricesWorld()
        {
            return this.world;
        }

        /// <summary>
        /// Vrati inverzni transponovane world matice
        /// </summary>
        /// <returns>Vrati inverzni transponovane world matice</returns>
        public virtual Matrix[] GetMatricesWorldIT()
        {
            return this.worldIT;
        }

        /// <summary>
        /// Vrati world matici
        /// </summary>
        /// <returns>Vraci world matici</returns>
        public virtual Matrix GetMatrixWorld()
        {
            return world[0];
        }

        /// <summary>
        /// Vrati puvodni World matici, ktera byla objektu nastavena
        /// </summary>
        /// <returns>Vrati puvodni World matici, ktera byla objektu nastavena</returns>
        public virtual Matrix GetMatrixWorldOriginal()
        {
            return worldOriginal;
        }

        /// <summary>
        /// Vraci inverzni transponovanou world matici
        /// </summary>
        /// <returns>Vraci inverzni transponovanou world matici</returns>
        public virtual Matrix GetMatrixWorldIT()
        {
            return worldIT[0];
        }

        /// <summary>
        /// Metoda vrati world matici bounding sphery meshe
        /// </summary>
        /// <returns>Metoda vrati world matici bounding sphery meshe</returns>
        public virtual Matrix GetMatrixWorldBoundingSphereMesh()
        {
            return boundingMeshWorld[0];
        }

        /// <summary>
        /// Metoda vrati world matici bounding boxu meshe
        /// </summary>
        /// <returns>Metoda vrati world matici bounding boxu meshe</returns>
        public virtual Matrix GetMatrixWorldBoundingBoxMesh()
        {
            return boundingMeshWorld[1];
        }

        /// <summary>
        /// Metoda vrati vsechny inverzni transponovany world matice bounding boxu
        /// </summary>
        /// <returns>Metoda vrati vsechny inverzni transponovany world matice bounding boxu</returns>
        public virtual Matrix[] GetMatricesWorldITBoundingMesh()
        {
            return boundingMeshWorldIT;
        }



        /// <summary>
        /// Metoda vrati vsechny world matice bounding boxu
        /// </summary>
        /// <returns>Metoda vrati vsechny world matice bounding boxu</returns>
        public virtual Matrix[] GetMatricesWorldBoundingMesh()
        {
            return boundingMeshWorld;
        }

        /// <summary>
        /// Metoda vrati world matici inverzni a transponovanou bounding sphery meshe
        /// </summary>
        /// <returns>Metoda vrati world matici inverzni a transponovanou bounding sphery meshe</returns>
        public virtual Matrix GetMatrixWorldITBoundingSphereMesh()
        {
            return Matrix.TransposeMatrix(Matrix.Invert(GetMatrixWorldBoundingSphereMesh()));
        }

        /// <summary>
        /// Metoda vrati world matici inverzni a transponovanou bounding boxu meshe
        /// </summary>
        /// <returns>Metoda vrati world matici inverzni a transponovanou bounding sphery meshe</returns>
        public virtual Matrix GetMatrixWorldITBoundingBoxMesh()
        {
            return Matrix.TransposeMatrix(Matrix.Invert(GetMatrixWorldBoundingBoxMesh()));
        }

        /// <summary>
        /// Vrati pole textur nultyho bufferu s klasickyma texturama
        /// </summary>
        /// <returns>Vrati pole textur nultyho bufferu s klasickyma texturama</returns>
        public Texture[] GetTexturesColor0()
        {
            return color_textures0;
        }

        /// <summary>
        /// Vrati pole textur prvniho bufferu s klasickyma texturama
        /// </summary>
        /// <returns>Vrati pole textur prvniho bufferu s klasickyma texturama</returns>
        public Texture[] GetTexturesColor1()
        {
            return color_textures1;
        }

        /// <summary>
        /// Vrati pole textur druhyho bufferu s klasickyma texturama
        /// </summary>
        /// <returns>Vrati pole textur druhyho bufferu s klasickyma texturama</returns>
        public Texture[] GetTexturesColor2()
        {
            return color_textures2;
        }

        /// <summary>
        /// Vrati pole textur bufferu s normalovyma texturama
        /// </summary>
        /// <returns>Vrati pole textur bufferu s normalovyma texturama</returns>
        public Texture[] GetTexturesNormal()
        {
            return normal_textures;
        }
        #endregion

        /// <summary>
        /// Spocita viditelnost objektu. Na zaklade vypocte se objekt bude nebo nebude renderovat
        /// </summary>
        /// <remarks>Metoda je zavisla na spravne vypocitanych vzdalenostech od kamery!</remarks>
        public virtual bool ComputeVisibility(ISceneCamera camera)
        {
            if (camera == null)
            {
                visible = true;
                return true;
            }

            ClipVolume cv = camera.GetClipVolume();
            Plane[] p = new Plane[] { cv.pNear, cv.pFar, cv.pLeft, cv.pRight, cv.pTop, cv.pBottom };

            Matrix boundingWorld = boundingMeshWorld[0];

            float radius = GetSphereRadius() * scale;
            Vector3 position = new Vector3();
            position.TransformCoordinate(boundingWorld);

            if (mesh == null)
            {
                for (int t = 0; t < p.Length; t++)
                {
                    float dot = p[t].Dot(GetPosition()) + radius;
                    if (dot < 0)
                    {
                        visible = false;
                        return false;
                    }
                }

                visible = true;
                return true;
            }

            if (distance <= radius)
            {
                visible = true;
                return visible;
            }

            for (int k = 0; k < p.Length; k++)
            {

                float dot = p[k].Dot(position);

                if (dot + radius < 0)
                {
                    visible = false;
                    return visible;
                }
            }

            visible = true;
            return visible;
        }

        /// <summary>
        /// Metoda zjisti kolizi bodu s objektem
        /// </summary>
        /// <param name="position">Pozice bodu, kde se ma nachazet kolize</param>
        /// <returns>Vraci, zda nastala kolize, ci ne</returns>
        public bool ComputeSphereCollission(Vector3 position)
        {
            if (mesh == null)
                return false;

            Vector3 sphereCenter = GetSphereObjectCenter();
            float distance = Vector3.Length(sphereCenter - position);

            if (distance <= radius)
                return true;


            return false;
        }

        /// <summary>
        /// Metoda zjisti kolizi bodu s objektem
        /// </summary>
        /// <param name="position">Pozice bodu, kde se ma nachazet kolize</param>
        /// <returns>Vraci, zda nastala kolize, ci ne</returns>
        public bool ComputeBoxCollission(Vector3 position)
        {
            if (mesh == null)
                return false;

            Vector3 min = GetBoundingBoxRelativeMinimum();
            Vector3 max = GetBoundingBoxRelativeMaximum();

            position = Vector3.TransformCoordinate(position, Matrix.Invert(GetMatrixWorld()));

            if (position.X >= min.X && position.X <= max.X)
            {
                if (position.Y >= min.Y && position.Y <= max.Y)
                {
                    if (position.Z >= min.Z && position.Z <= max.Z)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Metoda spocte vzdalenost od kamery a pokud je zapnute LOD, tak ho nastavi
        /// </summary>
        /// <returns>Vzdalenost od kamery</returns>
        public virtual float ComputeDistanceFromCamera(ISceneCamera camera)
        {
            distance = Vector3.Length(GetPosition() - camera.GetVector3Position());

            return distance;
        }

        /// <summary>
        /// Spocte vzdalenost objektu od zadane pozice
        /// </summary>
        /// <param name="position">Pozice, od ktere se pocita vzdalenost</param>
        /// <returns>Vraci vzdalenost od zadane pozice</returns>
        public virtual float ComputeDistanceToPosition(Vector3 position)
        {
            distanceToPosition = Vector3.Length(GetPosition() - position);

            return distanceToPosition;
        }

        /// <summary>
        /// Metoda zrusi predchozi vypocty viditelnosti (objekt bude viden)
        /// </summary>
        public virtual void ResetVisibility()
        {
            visible = true;
        }

        /// <summary>
        /// Zjisti, zda je zaply LOD, ci ne
        /// </summary>
        /// <returns>Vrati true, pokud se LOD uplatnuje, jinak false</returns>
        public bool isEnableLOD()
        {
            return enableLOD;
        }

        /// <summary>
        /// Vrati, zda objekt, potrebuje pro vyrenderovani alpha blendovani, ci ne
        /// </summary>
        /// <returns>Vrati, zda objekt, potrebuje pro vyrenderovani alpha blendovani, ci ne</returns>
        public virtual bool isAlphaObject()
        {
            return false;
        }

        /// <summary>
        /// Nastavi objektu, zda je nekym drzen, ci ne
        /// </summary>
        public void SetEquiped(bool equiped)
        {
            this.equiped = equiped;
        }

        /// <summary>
        /// Zjisti, zda je objekt nekym drzen, ci ne
        /// </summary>
        /// <returns>Vraci true, pokud objekt nekdo drzi</returns>
        public bool isEquiped()
        {
            return equiped;
        }

        /// <summary>
        /// Vrati, zda je objekt uklizen, ci ne
        /// </summary>
        /// <returns>Vrati true, pokud je jiz objekt uklizen, jinak false</returns>
        public bool isDisposed()
        {
            return disposed;
        }

        /// <summary>
        /// Metoda provede aplikaci LODu a prepocita vertexy a facy
        /// </summary>
        public virtual void ApplyLOD()
        {
            if (enableLOD)
                UpdateVerticesFacesCounters();
        }

        /// <summary>
        /// Metoda aktualizuje vnitrni pocitadla vertexu a facu
        /// </summary>
        private void UpdateVerticesFacesCounters()
        {
            numberVerteces = 0;
            numberFaces = 0;

            if (this.mesh == null || this.mesh.Disposed)
                return;

            try
            {
                numberVerteces = this.mesh.NumberVertices;
                numberFaces = this.mesh.NumberFaces;
            }
            catch
            {
                numberVerteces = 0;
                numberFaces = 0;
            }
        }

        /// <summary>
        /// Metoda vygeneruje geometrii pro sprite
        /// </summary>
        /// <param name="device">Zarizeni, ktere ve kterem se bude geometrie zobrazovat</param>
        /// <returns>Vraci geometrii spritu</returns>
        public static ProgressiveMesh GenerateSpriteGeometry(Device device)
        {
            positionOnlyVertex[] rectangle = new positionOnlyVertex[4];

            rectangle[0] = new positionOnlyVertex(-1, 1, 0);
            rectangle[1] = new positionOnlyVertex(1, 1, 0);
            rectangle[2] = new positionOnlyVertex(-1, -1, 0);
            rectangle[3] = new positionOnlyVertex(1, -1, 0);

            int[] indexes = new int[6];
            indexes[0] = 0;
            indexes[1] = 1;
            indexes[2] = 2;
            indexes[3] = 2;
            indexes[4] = 1;
            indexes[5] = 3;

            using (Mesh sprite = new Mesh(2, 4, MeshFlags.Managed | MeshFlags.Use32Bit, positionOnlyVertex.Format, device))
            {
                sprite.SetVertexBufferData(rectangle, LockFlags.NoSystemLock);
                sprite.SetIndexBufferData(indexes, LockFlags.NoSystemLock);

                using (Mesh temp = sprite.Clone(MeshFlags.Managed | MeshFlags.Use32Bit, GeneralObject.GeneralVertex.vertexElements, device))
                {
                    int[] adjency = new int[3 * temp.NumberFaces];
                    temp.GenerateAdjacency(0.0f, adjency);

                    ProgressiveMesh progressiveSpriteMesh = new ProgressiveMesh(temp, adjency, 2, MeshFlags.SimplifyFace);
                    progressiveSpriteMesh.NumberFaces = progressiveSpriteMesh.MaxFaces;

                    return progressiveSpriteMesh;
                }
            }

        }
    }

}
