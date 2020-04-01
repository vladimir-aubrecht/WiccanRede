#region File Description
//-----------------------------------------------------------------------------
// ParticleVertex.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
#endregion

namespace WiccanRede.Objects.ParticleSystem
{
    /// <summary>
    /// Custom vertex structure for drawing point sprite particles.
    /// </summary>
    struct ParticleVertex
    {
        // Stores the starting position of the particle.
        public Vector3 Position;

        // Stores the starting velocity of the particle.
        public Vector3 Velocity;

        // Four random values, used to make each particle look slightly different.
        public int Random;

        // The time (in seconds) at which this particle was created.
        public float Time;


        // Describe the layout of this vertex structure.
        public static readonly VertexElement[] VertexElements =
        {
            new VertexElement(0, 0, DeclarationType.Float3,
                                    DeclarationMethod.Default,
                                    DeclarationUsage.Position, 0),

            new VertexElement(0, 12, DeclarationType.Float3,
                                     DeclarationMethod.Default,
                                     DeclarationUsage.Normal, 0),

            new VertexElement(0, 24, DeclarationType.Color,
                                     DeclarationMethod.Default,
                                     DeclarationUsage.Color, 0),

            new VertexElement(0, 28, DeclarationType.Float1,
                                     DeclarationMethod.Default,
                                     DeclarationUsage.TextureCoordinate, 0),
            VertexElement.VertexDeclarationEnd
        };

        public static VertexFormats Format
        {
            get
            {
                return VertexFormats.Position | VertexFormats.Normal | VertexFormats.Diffuse | VertexFormats.Texture0;
            }
        }

        // Describe the size of this vertex structure.
        public const int SizeInBytes = 32;
    }
}
