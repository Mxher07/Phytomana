namespace Engine.Graphics {
    public abstract class BaseFlatBatch : BaseBatch {
        public static UnlitShader m_shader = new(true, false, false, false);

        public readonly DynamicArray<VertexPositionColor> LineVertices = [];

        public readonly DynamicArray<int> LineIndices = [];

        public readonly DynamicArray<VertexPositionColor> TriangleVertices = [];

        public readonly DynamicArray<int> TriangleIndices = [];

        public override bool IsEmpty() => LineIndices.Count == 0 && TriangleIndices.Count == 0;

        public override void Clear() {
            LineVertices.Clear();
            LineIndices.Clear();
            TriangleVertices.Clear();
            TriangleIndices.Clear();
        }

        public void Flush(Matrix matrix, bool clearAfterFlush = true) {
            Flush(matrix, Vector4.One, clearAfterFlush);
        }

        public override void Flush(Matrix matrix, Vector4 color, bool clearAfterFlush = true) {
            Display.DepthStencilState = DepthStencilState;
            Display.RasterizerState = RasterizerState;
            Display.BlendState = BlendState;
            FlushWithDeviceState(matrix, color, clearAfterFlush);
        }

        public void FlushWithDeviceState(Matrix matrix, Vector4 color, bool clearAfterFlush = true) {
            if (!IsEmpty()) {
                m_shader.Transforms.World[0] = matrix;
                m_shader.Color = color;
                FlushWithDeviceState(m_shader, clearAfterFlush);
            }
        }

        public void FlushWithDeviceState(Shader shader, bool clearAfterFlush = true) {
            if (TriangleIndices.Count > 0) {
                int num = 0;
                int num2 = TriangleIndices.Count;
                while (num2 > 0) {
                    int num3 = Math.Min(num2, 196605);
                    Display.DrawUserIndexed(
                        PrimitiveType.TriangleList,
                        shader,
                        VertexPositionColor.VertexDeclaration,
                        TriangleVertices.Array,
                        0,
                        TriangleVertices.Count,
                        TriangleIndices.Array,
                        num,
                        num3
                    );
                    num += num3;
                    num2 -= num3;
                }
            }
            if (LineIndices.Count > 0) {
                int num4 = 0;
                int num5 = LineIndices.Count;
                while (num5 > 0) {
                    int num6 = Math.Min(num5, 131070);
                    Display.DrawUserIndexed(
                        PrimitiveType.LineList,
                        shader,
                        VertexPositionColor.VertexDeclaration,
                        LineVertices.Array,
                        0,
                        LineVertices.Count,
                        LineIndices.Array,
                        num4,
                        num6
                    );
                    num4 += num6;
                    num5 -= num6;
                }
            }
            if (clearAfterFlush) {
                Clear();
            }
        }

        public void TransformLines(Matrix matrix, int start = 0, int end = -1) {
            VertexPositionColor[] array = LineVertices.Array;
            if (end < 0) {
                end = LineVertices.Count;
            }
            for (int i = start; i < end; i++) {
                Vector3.Transform(ref array[i].Position, ref matrix, out array[i].Position);
            }
        }

        public void TransformLinesColors(Color color, int start = 0, int end = -1) {
            VertexPositionColor[] array = LineVertices.Array;
            if (end < 0) {
                end = LineVertices.Count;
            }
            for (int i = start; i < end; i++) {
                array[i].Color *= color;
            }
        }

        public void TransformTriangles(Matrix matrix, int start = 0, int end = -1) {
            VertexPositionColor[] array = TriangleVertices.Array;
            if (end < 0) {
                end = TriangleVertices.Count;
            }
            for (int i = start; i < end; i++) {
                Vector3.Transform(ref array[i].Position, ref matrix, out array[i].Position);
            }
        }

        public void TransformTrianglesColors(Color color, int start = 0, int end = -1) {
            VertexPositionColor[] array = TriangleVertices.Array;
            if (end < 0) {
                end = TriangleVertices.Count;
            }
            for (int i = start; i < end; i++) {
                array[i].Color *= color;
            }
        }
    }
}