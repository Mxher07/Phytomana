namespace Engine.Graphics {
    public class TexturedBatch2D : BaseTexturedBatch {
        public TexturedBatch2D() {
            DepthStencilState = DepthStencilState.None;
            RasterizerState = RasterizerState.CullNoneScissor;
            BlendState = BlendState.AlphaBlend;
            SamplerState = SamplerState.LinearClamp;
        }

        public void QueueBatch(TexturedBatch2D batch, Matrix? matrix = null, Color? color = null) {
            int count = TriangleVertices.Count;
            TriangleVertices.AddRange(batch.TriangleVertices);
            for (int i = 0; i < batch.TriangleIndices.Count; i++) {
                TriangleIndices.Add(batch.TriangleIndices[i] + count);
            }
            if (matrix.HasValue
                && matrix != Matrix.Identity) {
                TransformTriangles(matrix.Value, count);
            }
            if (color.HasValue
                && color != Color.White) {
                TransformTrianglesColors(color.Value, count);
            }
        }

        public void QueueTriangle(Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            float depth,
            Vector2 texCoord1,
            Vector2 texCoord2,
            Vector2 texCoord3,
            Color color) {
            int count = TriangleVertices.Count;
            TriangleVertices.Count += 3;
            TriangleVertices.Array[count] = new VertexPositionColorTexture(new Vector3(p1.X, p1.Y, depth), color, texCoord1);
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(new Vector3(p2.X, p2.Y, depth), color, texCoord2);
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(new Vector3(p3.X, p3.Y, depth), color, texCoord3);
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 3;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
        }

        public void QueueTriangle(Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            float depth,
            Vector2 texCoord1,
            Vector2 texCoord2,
            Vector2 texCoord3,
            Color color1,
            Color color2,
            Color color3) {
            int count = TriangleVertices.Count;
            TriangleVertices.Count += 3;
            TriangleVertices.Array[count] = new VertexPositionColorTexture(new Vector3(p1.X, p1.Y, depth), color1, texCoord1);
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(new Vector3(p2.X, p2.Y, depth), color2, texCoord2);
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(new Vector3(p3.X, p3.Y, depth), color3, texCoord3);
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 3;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
        }

        public void QueueQuad(Vector2 corner1, Vector2 corner2, float depth, Vector2 texCoord1, Vector2 texCoord2, Color color) {
            int count = TriangleVertices.Count;
            TriangleVertices.Count += 4;
            TriangleVertices.Array[count] = new VertexPositionColorTexture(
                new Vector3(corner1.X, corner1.Y, depth),
                color,
                new Vector2(texCoord1.X, texCoord1.Y)
            );
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(
                new Vector3(corner1.X, corner2.Y, depth),
                color,
                new Vector2(texCoord1.X, texCoord2.Y)
            );
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(
                new Vector3(corner2.X, corner2.Y, depth),
                color,
                new Vector2(texCoord2.X, texCoord2.Y)
            );
            TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(
                new Vector3(corner2.X, corner1.Y, depth),
                color,
                new Vector2(texCoord2.X, texCoord1.Y)
            );
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 6;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
            TriangleIndices.Array[count2 + 3] = count + 2;
            TriangleIndices.Array[count2 + 4] = count + 3;
            TriangleIndices.Array[count2 + 5] = count;
        }

        public void QueueQuad(Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            Vector2 p4,
            float depth,
            Vector2 texCoord1,
            Vector2 texCoord2,
            Vector2 texCoord3,
            Vector2 texCoord4,
            Color color) {
            int count = TriangleVertices.Count;
            TriangleVertices.Count += 4;
            TriangleVertices.Array[count] = new VertexPositionColorTexture(new Vector3(p1.X, p1.Y, depth), color, texCoord1);
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(new Vector3(p2.X, p2.Y, depth), color, texCoord2);
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(new Vector3(p3.X, p3.Y, depth), color, texCoord3);
            TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(new Vector3(p4.X, p4.Y, depth), color, texCoord4);
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 6;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
            TriangleIndices.Array[count2 + 3] = count + 2;
            TriangleIndices.Array[count2 + 4] = count + 3;
            TriangleIndices.Array[count2 + 5] = count;
        }

        public void QueueQuad(Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            Vector2 p4,
            float depth,
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
            TriangleVertices.Array[count] = new VertexPositionColorTexture(new Vector3(p1.X, p1.Y, depth), color1, texCoord1);
            TriangleVertices.Array[count + 1] = new VertexPositionColorTexture(new Vector3(p2.X, p2.Y, depth), color2, texCoord2);
            TriangleVertices.Array[count + 2] = new VertexPositionColorTexture(new Vector3(p3.X, p3.Y, depth), color3, texCoord3);
            TriangleVertices.Array[count + 3] = new VertexPositionColorTexture(new Vector3(p4.X, p4.Y, depth), color4, texCoord4);
            int count2 = TriangleIndices.Count;
            TriangleIndices.Count += 6;
            TriangleIndices.Array[count2] = count;
            TriangleIndices.Array[count2 + 1] = count + 1;
            TriangleIndices.Array[count2 + 2] = count + 2;
            TriangleIndices.Array[count2 + 3] = count + 2;
            TriangleIndices.Array[count2 + 4] = count + 3;
            TriangleIndices.Array[count2 + 5] = count;
        }

        public void Flush(bool clearAfterFlush = true) {
            Flush(PrimitivesRenderer2D.ViewportMatrix(), clearAfterFlush);
        }
    }
}