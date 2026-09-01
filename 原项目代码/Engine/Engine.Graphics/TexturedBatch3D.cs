namespace Engine.Graphics {
    public class TexturedBatch3D : BaseTexturedBatch {
        public void QueueTriangle(Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            Vector2 texCoord1,
            Vector2 texCoord2,
            Vector2 texCoord3,
            Color color) {
            int count = TriangleVertices.Count;
            TriangleVertices.Count += 3;
            TriangleVertices.Array[count] = new VertexPositionColorTexture(p1, color, texCoord1);
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(p2, color, texCoord2);
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(p3, color, texCoord3);
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 3;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
        }

        public void QueueTriangle(Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            Vector2 texCoord1,
            Vector2 texCoord2,
            Vector2 texCoord3,
            Color color1,
            Color color2,
            Color color3) {
            int count = TriangleVertices.Count;
            TriangleVertices.Count += 3;
            TriangleVertices.Array[count] = new VertexPositionColorTexture(p1, color1, texCoord1);
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(p2, color2, texCoord2);
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(p3, color3, texCoord3);
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 3;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
        }

        public void QueueQuad(Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            Vector3 p4,
            Vector2 texCoord1,
            Vector2 texCoord2,
            Vector2 texCoord3,
            Vector2 texCoord4,
            Color color) {
            int count = TriangleVertices.Count;
            TriangleVertices.Count += 4;
            TriangleVertices.Array[count] = new VertexPositionColorTexture(p1, color, texCoord1);
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(p2, color, texCoord2);
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(p3, color, texCoord3);
            TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(p4, color, texCoord4);
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 6;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
            TriangleIndices.Array[count2 + 3] = count + 2;
            TriangleIndices.Array[count2 + 4] = count + 3;
            TriangleIndices.Array[count2 + 5] = count;
        }

        public void QueueQuad(Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            Vector3 p4,
            Vector2 texCoord1,
            Vector2 texCoord2,
            Vector2 texCoord3,
            Vector2 texCoord4,
            Color color1,
            Color color2,
            Color color3,
            Color color4) {
            int count = TriangleVertices.Count;
            TriangleVertices.Count += 4;
            TriangleVertices.Array[count] = new VertexPositionColorTexture(p1, color1, texCoord1);
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(p2, color2, texCoord2);
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(p3, color3, texCoord3);
            TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(p4, color4, texCoord4);
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 6;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
            TriangleIndices.Array[count2 + 3] = count + 2;
            TriangleIndices.Array[count2 + 4] = count + 3;
            TriangleIndices.Array[count2 + 5] = count;
        }
    }
}