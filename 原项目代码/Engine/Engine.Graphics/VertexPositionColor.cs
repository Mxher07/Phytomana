namespace Engine.Graphics {
    public struct VertexPositionColor {
        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, "POSITION"),
            new VertexElement(12, VertexElementFormat.NormalizedByte4, "COLOR")
        );

        public Vector3 Position;

        public Color Color;

        public VertexPositionColor(Vector3 position, Color color) {
            Position = position;
            Color = color;
        }
    }
}