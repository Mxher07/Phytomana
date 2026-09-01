using Engine;
using Engine.Graphics;

namespace Game {
    public struct TerrainVertex {
        public float X;

        public float Y;

        public float Z;

        public short Tx;

        public short Ty;

        public Color Color;

        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position),
            new VertexElement(12, VertexElementFormat.NormalizedShort2, VertexElementSemantic.TextureCoordinate),
            new VertexElement(16, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color)
        );
    }
}