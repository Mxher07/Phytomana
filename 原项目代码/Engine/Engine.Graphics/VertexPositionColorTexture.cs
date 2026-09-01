namespace Engine.Graphics {
    public struct VertexPositionColorTexture {
        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, "POSITION"),
            new VertexElement(12, VertexElementFormat.NormalizedByte4, "COLOR"),
            new VertexElement(16, VertexElementFormat.Vector2, "TEXCOORD")
        );

        public Vector3 Position;

        public Color Color;

        public Vector2 TexCoord;

        public VertexPositionColorTexture(Vector3 position, Color color, Vector2 texCoord) {
            Position = position;
            Color = color;
            TexCoord = texCoord;
        }
    }
}