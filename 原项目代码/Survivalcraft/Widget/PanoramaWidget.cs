using Engine;
using Engine.Graphics;

namespace Game {
    public class PanoramaWidget : Widget {
        public static string TexturePath = "Textures/Gui/Panorama";

        public Vector2 m_position;

        public float m_timeOffset;

        public Texture2D Texture { get; set; }

        public PanoramaWidget() {
            Texture = ContentManager.Get<Texture2D>(TexturePath);
            m_timeOffset = new Random().Float(0f, 1000f);
        }

        public virtual void DrawImage(DrawContext dc) {
            float num = (float)MathUtils.Remainder(Time.FrameStartTime + m_timeOffset, 10000.0);
            float x = 2f * SimplexNoise.OctavedNoise(num, 0.02f, 4, 2f, 0.5f) - 1f;
            float y = 2f * SimplexNoise.OctavedNoise(num + 100f, 0.02f, 4, 2f, 0.5f) - 1f;
            m_position += 0.06f * new Vector2(x, y) * MathUtils.Min(Time.FrameDuration, 0.1f);
            m_position.X = MathUtils.Remainder(m_position.X, 1f);
            m_position.Y = MathUtils.Remainder(m_position.Y, 1f);
            float f = 0.5f * MathUtils.PowSign(MathF.Sin(0.21f * num + 2f), 2f) + 0.5f;
            float num2 = MathUtils.Lerp(0.3f, 0.5f, f);
            float num3 = num2 / Texture.Height * Texture.Width / ActualSize.X * ActualSize.Y;
            float x2 = m_position.X;
            float y2 = m_position.Y;
            Vector2 zero = Vector2.Zero;
            Vector2 actualSize = ActualSize;
            Vector2 texCoord = new(x2 - num2, y2 - num3);
            Vector2 texCoord2 = new(x2 + num2, y2 + num3);
            TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(
                Texture,
                false,
                0,
                DepthStencilState.DepthWrite,
                null,
                BlendState.AlphaBlend,
                SamplerState.LinearWrap
            );
            int count = texturedBatch2D.TriangleVertices.Count;
            texturedBatch2D.QueueQuad(zero, actualSize, 1f, texCoord, texCoord2, GlobalColorTransform);
            texturedBatch2D.TransformTriangles(GlobalTransform, count);
        }

        public virtual void DrawSquares(DrawContext dc) {
            FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.None, null, BlendState.AlphaBlend);
            int count = flatBatch2D.LineVertices.Count;
            int count2 = flatBatch2D.TriangleVertices.Count;
            float num = (float)MathUtils.Remainder(Time.FrameStartTime + m_timeOffset, 10000.0);
            float num2 = ActualSize.X / 12f;
            float num3 = GlobalColorTransform.A / 255f;
            for (float num4 = 0f; num4 < ActualSize.X; num4 += num2) {
                for (float num5 = 0f; num5 < ActualSize.Y; num5 += num2) {
                    float num6 = 0.35f
                        * MathF.Pow(
                            MathUtils.Saturate(
                                SimplexNoise.OctavedNoise(
                                    num4 + 1000f,
                                    num5,
                                    0.7f * num,
                                    0.5f,
                                    1,
                                    2f,
                                    1f
                                )
                                - 0.1f
                            ),
                            1f
                        )
                        * num3;
                    float num7 = 0.7f
                        * MathF.Pow(
                            SimplexNoise.OctavedNoise(
                                num4,
                                num5,
                                0.5f * num,
                                0.5f,
                                1,
                                2f,
                                1f
                            ),
                            3f
                        )
                        * num3;
                    Vector2 corner = new(num4, num5);
                    Vector2 corner2 = new(num4 + num2, num5 + num2);
                    if (num6 > 0.01f) {
                        flatBatch2D.QueueRectangle(corner, corner2, 0f, new Color(0f, 0f, 0f, num6));
                    }
                    if (num7 > 0.01f) {
                        flatBatch2D.QueueQuad(corner, corner2, 0f, new Color(0f, 0f, 0f, num7));
                    }
                }
            }
            flatBatch2D.TransformLines(GlobalTransform, count);
            flatBatch2D.TransformTriangles(GlobalTransform, count2);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
        }

        public override void Draw(DrawContext dc) {
            DrawImage(dc);
            DrawSquares(dc);
        }
    }
}