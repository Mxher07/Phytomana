namespace Engine.Graphics {
    public class ModelMesh : IDisposable {
        public List<ModelMeshPart> m_meshParts = [];

        public BoundingBox m_boundingBox;

        public string Name { get; set; }

        public ModelBone ParentBone { get; set; }

        public bool IsVisible { get; set; } = true;

        public BoundingBox BoundingBox {
            get => m_boundingBox;
            set => m_boundingBox = value;
        }

        public ReadOnlyList<ModelMeshPart> MeshParts => new(m_meshParts);

        public void Dispose() {
            Utilities.DisposeCollection(m_meshParts);
        }

        public ModelMeshPart NewMeshPart(VertexBuffer vertexBuffer,
            IndexBuffer indexBuffer,
            int startIndex,
            int indicesCount,
            BoundingBox boundingBox) => NewMeshPart(
            vertexBuffer,
            indexBuffer,
            startIndex,
            indicesCount,
            boundingBox,
            -1,
            PrimitiveType.TriangleList,
            null,
            0
        );

        public ModelMeshPart NewMeshPart(VertexBuffer vertexBuffer,
            IndexBuffer indexBuffer,
            int startIndex,
            int indicesCount,
            BoundingBox boundingBox,
            int materialIndex = -1,
            PrimitiveType primitiveType = PrimitiveType.TriangleList,
            System.Numerics.Matrix4x4[] instanceMatrices = null,
            int instanceCount = 0) {
            ArgumentNullException.ThrowIfNull(vertexBuffer);
            ArgumentNullException.ThrowIfNull(indexBuffer);
            if (startIndex < 0
                || indicesCount < 0
                || startIndex + indicesCount > indexBuffer.IndicesCount) {
                throw new InvalidOperationException("Specified range is outside of index buffer.");
            }
            ModelMeshPart modelMeshPart = new();
            m_meshParts.Add(modelMeshPart);
            modelMeshPart.VertexBuffer = vertexBuffer;
            modelMeshPart.IndexBuffer = indexBuffer;
            modelMeshPart.StartIndex = startIndex;
            modelMeshPart.IndicesCount = indicesCount;
            modelMeshPart.BoundingBox = boundingBox;
            modelMeshPart.MaterialIndex = materialIndex;
            modelMeshPart.PrimitiveType = primitiveType;
            modelMeshPart.InstanceCount = instanceCount;
            modelMeshPart.InstanceMatrices = instanceMatrices;
            return modelMeshPart;
        }
    }
}