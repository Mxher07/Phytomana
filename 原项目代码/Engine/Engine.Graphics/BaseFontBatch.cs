using Engine.Media;

namespace Engine.Graphics {
    public abstract class BaseFontBatch : BaseBatch {
        public static UnlitShader m_shader = new(true, true, false, false);

        public readonly DynamicArray<VertexPositionColorTexture> TriangleVertices = [];

        public readonly DynamicArray<int> TriangleIndices = [];

        public BitmapFont Font { get; set; }

        public SamplerState SamplerState { get; set; }

        public override bool IsEmpty() => TriangleIndices.Count == 0;

        public override void Clear() {
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
            FlushWithDeviceState(Font, SamplerState, matrix, color, clearAfterFlush);
        }

        public void FlushWithDeviceState(BitmapFont font, SamplerState samplerState, Matrix matrix, Vector4 color, bool clearAfterFlush = true) {
            m_shader.Texture = font.Texture;
            m_shader.SamplerState = samplerState;
            m_shader.Transforms.World[0] = matrix;
            m_shader.Color = color;
            FlushWithDeviceState(m_shader, clearAfterFlush);
        }

        public void FlushWithDeviceState(Shader shader, bool clearAfterFlush = true) {
            if (TriangleIndices.Count > 0) {
                int num = 0;
                int num2 = TriangleIndices.Count;
                while (num2 > 0) {
                    int num3 = MathUtils.Min(num2, 196605);
                    Display.DrawUserIndexed(
                        PrimitiveType.TriangleList,
                        shader,
                        VertexPositionColorTexture.VertexDeclaration,
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
            if (clearAfterFlush) {
                Clear();
            }
        }

        public void FlushWithCurrentStateAndShader(Shader shader, bool clearAfterFlush = true) {
            if (TriangleIndices.Count > 0) {
                int num = 0;
                int num2 = TriangleIndices.Count;
                while (num2 > 0) {
                    int num3 = Math.Min(num2, 196605);
                    Display.DrawUserIndexed(
                        PrimitiveType.TriangleList,
                        shader,
                        VertexPositionColorTexture.VertexDeclaration,
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
            if (clearAfterFlush) {
                Clear();
            }
        }

        public void TransformTriangles(Matrix matrix, int start = 0, int end = -1) {
            VertexPositionColorTexture[] array = TriangleVertices.Array;
            if (end < 0) {
                end = TriangleVertices.Count;
            }
            for (int i = start; i < end; i++) {
                Vector3.Transform(ref array[i].Position, ref matrix, out array[i].Position);
            }
        }

        public void TransformTrianglesColors(Color color, int start = 0, int end = -1) {
            VertexPositionColorTexture[] array = TriangleVertices.Array;
            if (end < 0) {
                end = TriangleVertices.Count;
            }
            for (int i = start; i < end; i++) {
                array[i].Color *= color;
            }
        }

        public Vector2 CalculateTextOffset(string text, int start, int count, TextAnchor anchor, Vector2 scale, Vector2 spacing) {
            Vector2 zero = Vector2.Zero;
            if (anchor != 0) {
                Vector2 vector = Font.MeasureText(text, start, count, scale, spacing);
                if ((anchor & TextAnchor.HorizontalCenter) != 0) {
                    zero.X = (0f - vector.X) / 2f;
                }
                else if ((anchor & TextAnchor.Right) != 0) {
                    zero.X = 0f - vector.X;
                }
                if ((anchor & TextAnchor.VerticalCenter) != 0) {
                    zero.Y = (0f - vector.Y) / 2f;
                }
                else if ((anchor & TextAnchor.Bottom) != 0) {
                    zero.Y = 0f - vector.Y;
                }
            }
            return zero;
        }
    }
}