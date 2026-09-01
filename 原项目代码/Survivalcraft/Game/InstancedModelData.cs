using Engine.Graphics;

namespace Game {
    public class InstancedModelData {
        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementSemantic.Normal),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate),
            new VertexElement(32, VertexElementFormat.Single, VertexElementSemantic.Instance)
        );

        public VertexBuffer VertexBuffer;

        public IndexBuffer IndexBuffer;
    }
}