using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.IO;

namespace WiccanRede.Graphics.Scene.SpecialObjects
{
    public class AnimatedObject : GeneralObject
    {

        AnimationRootFrame rootFrame;
        Matrix world;
        Device device;
        AnimationFrame animation;
        bool isAnimating = true;
        float speed = 0.001f;
        float oldTime = 0f;
        int lastLOD = 0;
        ProgressiveMesh pMesh;
        List<AnimationFrame> meshes;


        public AnimatedObject(Device device, Matrix world, AnimationRootFrame rootFrame, Texture[] textures, ProgressiveMesh pMesh)
            : base(pMesh, world, textures, null, null, null)
        {
            meshes = new List<AnimationFrame>();
            Frame rf = rootFrame.FrameHierarchy;

            if (rootFrame.FrameHierarchy != null)
                getAnimationMesh(rf, meshes);

            this.world = world;
            this.rootFrame = rootFrame;
            this.device = device;

            if (meshes.Count > 0)
                animation = meshes[0];

            this.pMesh = pMesh;
            //pMesh.NumberFaces = pMesh.MaxFaces;

        }


        /// <summary>
        /// Zapnuti / Vypnuti animovani
        /// </summary>
        /// <param name="isAnimating"></param>

        public void EnableAnimation(bool isAnimating)
        {
            if (isAnimating)
            {
                this.isAnimating = true;
            }
            else
            {
                this.isAnimating = false;

                if (rootFrame.AnimationController != null)
                    rootFrame.AnimationController.ResetTime();

            }

        }

        private void getAnimationMesh(Frame frame, List<AnimationFrame> meshes)
        {
            if (frame.MeshContainer != null)
                meshes.Add(frame as AnimationFrame);
            if (frame.FrameFirstChild != null)
                getAnimationMesh(frame.FrameFirstChild, meshes);
            if (frame.FrameSibling != null)
                getAnimationMesh(frame.FrameSibling, meshes);
        }
        /// <summary>
        /// Posunuti Animace nezavisle na FPS
        /// </summary>
        /// <param name="time">cas od zapnuti programu</param>
        public override void Update(float time)
        {
            float frameSpeed = time - oldTime;
            oldTime = time;
            frameSpeed *= speed;
            OnFrameMove(frameSpeed);

        }
        /// <summary>
        /// Nastaveni 4 world matic podle kosti pro dany subset meshe
        /// </summary>
        /// <param name="subset">subset</param>
        public override void UpdateSubset(int subset)
        {
            if (animation != null)
                DrawMeshContainer((animation.MeshContainer as AnimationMeshContainer), animation, subset);
        }

        /// <summary>
        /// Renderovani subsetu meshe
        /// </summary>
        /// <param name="subset">subset</param>
        public override void Render(int subset)
        {
            pMesh.DrawSubset(subset);

        }
        /// <summary>
        /// Aplikace level of detail 
        /// </summary>
        public override void ApplyLOD()
        {
            BaseMesh bmesh = base.GetModel();

            if (bmesh != null && !bmesh.Disposed)
            {
                float LODquality = 1f;

                if (base.isEnableLOD())
                    LODquality = 1f / (1f + 0.008f * base.GetDistanceFromCamera());

                ProgressiveMesh mesh = bmesh as ProgressiveMesh;
                int LOD = (int)(mesh.MaxFaces * LODquality * base.GetObjectQuality());

                if (lastLOD != LOD)
                {
                    mesh.NumberFaces = LOD;
                    lastLOD = LOD;
                }
            }

            base.ApplyLOD();
        }
        /// <summary>
        /// Nastaaveni pozice , velikosti a otoceni modelu ve svete
        /// </summary>
        /// <param name="worldMatrix">world matice</param>
        public override void SetMatrixWorld(Matrix worldMatrix)
        {
            world = worldMatrix;
            base.SetMatrixWorld(world);
        }
        /// <summary>
        /// getr world matice
        /// </summary>
        /// <returns>world matice</returns>
        public override Matrix GetMatrixWorld()
        {
            return world;
        }

        /// <summary>
        /// Nastaveni matic pro aktualni pozici modelu ve svete
        /// </summary>
        /// <param name="frame">rootFrame s animaci</param>
        /// <param name="parentMatrix">world matice</param>
        private void UpdateFrameMatrices(AnimationFrame frame, Matrix parentMatrix)
        {

            frame.CombinedTransformationMatrix = frame.TransformationMatrix *
                parentMatrix;

            if (frame.FrameSibling != null)
            {
                UpdateFrameMatrices(frame.FrameSibling as AnimationFrame, parentMatrix);
            }

            if (frame.FrameFirstChild != null)
            {
                UpdateFrameMatrices(frame.FrameFirstChild as AnimationFrame,
                    frame.CombinedTransformationMatrix);
            }
        }
        /// <summary>
        /// Posunuti Animace 
        /// </summary>
        /// <param name="elapsedTime">cas od posledniho posunuti</param>
        public void OnFrameMove(float elapsedTime)
        {

            // Has any time elapsed?
            if (elapsedTime > 0.0f)
            {

                if (rootFrame.AnimationController != null && isAnimating)
                    rootFrame.AnimationController.AdvanceTime(elapsedTime);

                if (rootFrame.FrameHierarchy != null)
                    UpdateFrameMatrices(rootFrame.FrameHierarchy.FrameFirstChild as AnimationFrame, world);
            }
        }

        /// <summary>Vypocet a nastaveni world matic podle kosti</summary>
        private void DrawMeshContainer(AnimationMeshContainer mesh, AnimationFrame parent, int iAttrib)
        {


            BoneCombination[] bones = mesh.GetBones();

            for (int iPaletteEntry = 0; iPaletteEntry < mesh.NumberPaletteEntries;
                ++iPaletteEntry)
            {
                int iMatrixIndex = bones[iAttrib].BoneId[iPaletteEntry];
                if (iMatrixIndex != -1)
                {
                    SetMatricesWorldByIndex(mesh.GetOffsetMatrices()[iMatrixIndex] *
                        mesh.GetFrames()[iMatrixIndex].
                        CombinedTransformationMatrix, iPaletteEntry);
                }
            }

        }

        /// <summary>
        /// Vraci pocet matic mezi kterzmi se interpoluje pri vykreslovani animace
        /// </summary>
        /// <returns>pocet matic</returns>
        public override int GetMatrixWorldCount()
        {

            if (meshes.Count > 0)
            {
                AnimationMeshContainer mesh = meshes[0].MeshContainer as AnimationMeshContainer;
                return mesh.NumberPaletteEntries;
            }

            return base.GetMatrixWorldCount();

        }
        /// <summary>
        /// Vraci pocet subsetu meshe
        /// </summary>
        /// <returns>pocet subsetu meshe</returns>
        public override int GetSubsetCount()
        {
            return pMesh.NumberAttributes;

        }
        /* /// <summary>
         /// nalezeni framu z meshem
         /// </summary>
         /// <param name="rootFrame">rootFrame</param>
         /// <returns>frame s meshem</returns>
         private AnimationFrame getAnimationFrame(AnimationRootFrame rootFrame)
         {
             Frame rf = rootFrame.FrameHierarchy;
             while (rf.MeshContainer == null)
             {
                 rf = rf.FrameFirstChild;
             }
             return (rf as AnimationFrame);

         }*/


        public class AnimationFrame : Frame
        {
            // Store the combined transformation matrix
            private Matrix combined = Matrix.Identity;
            /// <summary>The combined transformation matrix</summary>
            public Matrix CombinedTransformationMatrix
            {
                get { return combined; }
                set { combined = value; }
            }
        }
        public class AnimationMeshContainer : MeshContainer
        {
            public GraphicsStream adjency = null;
            // Array data
            private Texture[] meshTextures = null;
            private BoneCombination[] bones;
            private Matrix[] offsetMatrices;
            private AnimationFrame[] frameMatrices;

            // Instance data
            private int numAttributes = 0;
            private int numInfluences = 0;
            private int numPalette = 0;

            // Public properties

            /// <summary>Retrieve the textures used for this container</summary>
            public Texture[] GetTextures() { return meshTextures; }
            /// <summary>Set the textures used for this container</summary>
            public void SetTextures(Texture[] textures) { meshTextures = textures; }

            /// <summary>Retrieve the bone combinations used for this container</summary>
            public BoneCombination[] GetBones() { return bones; }
            /// <summary>Set the bone combinations used for this container</summary>
            public void SetBones(BoneCombination[] b) { bones = b; }

            /// <summary>Retrieve the animation frames used for this container</summary>
            public AnimationFrame[] GetFrames() { return frameMatrices; }
            /// <summary>Set the animation frames used for this container</summary>
            public void SetFrames(AnimationFrame[] frames) { frameMatrices = frames; }

            /// <summary>Retrieve the offset matrices used for this container</summary>
            public Matrix[] GetOffsetMatrices() { return offsetMatrices; }
            /// <summary>Set the offset matrices used for this container</summary>
            public void SetOffsetMatrices(Matrix[] matrices) { offsetMatrices = matrices; }

            /// <summary>Total number of attributes this mesh container contains</summary>
            public int NumberAttributes { get { return numAttributes; } set { numAttributes = value; } }
            /// <summary>Total number of influences this mesh container contains</summary>
            public int NumberInfluences { get { return numInfluences; } set { numInfluences = value; } }
            /// <summary>Total number of palette entries this mesh container contains</summary>
            public int NumberPaletteEntries { get { return numPalette; } set { numPalette = value; } }


        }
        public class AnimationAllocation : AllocateHierarchy
        {
            /// <summary>Create a new frame</summary>
            public override Frame CreateFrame(string name)
            {
                AnimationFrame frame = new AnimationFrame();
                frame.Name = name;
                frame.TransformationMatrix = Matrix.Identity;
                frame.CombinedTransformationMatrix = Matrix.Identity;


                return frame;
            }

            /// <summary>Create a new mesh container</summary>
            public override MeshContainer CreateMeshContainer(string name,
                MeshData meshData, ExtendedMaterial[] materials,
                EffectInstance[] effectInstances, GraphicsStream adjacency,
                SkinInformation skinInfo)
            {
                // We only handle meshes here
                if (meshData.Mesh == null)
                    throw new ArgumentException();

                // We must have a vertex format mesh
                if (meshData.Mesh.VertexFormat == VertexFormats.None)
                    throw new ArgumentException();

                AnimationMeshContainer mesh = new AnimationMeshContainer();
                mesh.adjency = adjacency;
                mesh.Name = name;
                int numFaces = meshData.Mesh.NumberFaces;
                Device dev = meshData.Mesh.Device;


                // Store the materials
                mesh.SetMaterials(materials);
                mesh.SetAdjacency(adjacency);

                Texture[] meshTextures = new Texture[materials.Length];
                mesh.MeshData = meshData;

                // If there is skinning info, save any required data
                if (skinInfo != null)
                {
                    mesh.SkinInformation = skinInfo;
                    int numBones = skinInfo.NumberBones;
                    Matrix[] offsetMatrices = new Matrix[numBones];

                    for (int i = 0; i < numBones; i++)
                        offsetMatrices[i] = skinInfo.GetBoneOffsetMatrix(i);

                    mesh.SetOffsetMatrices(offsetMatrices);

                    GenerateSkinnedMesh(mesh, adjacency);
                }

                return mesh;
            }
            /// <summary>
            /// Vztvoreni skinned meshe
            /// </summary>
            /// <param name="mesh">Container s meshem</param>
            /// <param name="adjacency">adjacency</param>
            static void GenerateSkinnedMesh(AnimationMeshContainer mesh, GraphicsStream adjacency)
            {

                if (mesh.SkinInformation == null)
                    throw new ArgumentException();  // There is nothing to generate

                MeshFlags flags = MeshFlags.OptimizeVertexCache;

                flags |= MeshFlags.Managed;


                int numMaxFaceInfl;
                using (IndexBuffer ib = mesh.MeshData.Mesh.IndexBuffer)
                {
                    numMaxFaceInfl = mesh.SkinInformation.GetMaxFaceInfluences(ib,
                        mesh.MeshData.Mesh.NumberFaces);
                }
                // 12 entry palette guarantees that any triangle (4 independent 
                // influences per vertex of a tri) can be handled
                numMaxFaceInfl = (int)Math.Min(numMaxFaceInfl, 12);


                mesh.NumberPaletteEntries = 4;

                int influences = 0;
                BoneCombination[] bones = null;

                // Use ConvertToBlendedMesh to generate a drawable mesh
                MeshData data = mesh.MeshData;

                data.Mesh = mesh.SkinInformation.ConvertToIndexedBlendedMesh(data.Mesh, flags, mesh.GetAdjacencyStream(), mesh.NumberPaletteEntries, out influences, out bones);



                int use32Bit = (int)(data.Mesh.Options.Value & MeshFlags.Use32Bit);


                mesh.NumberInfluences = influences;
                mesh.SetBones(bones);

                // Get the number of attributes
                mesh.NumberAttributes = bones.Length;

                mesh.MeshData = data;
            }


        }

    }

}
