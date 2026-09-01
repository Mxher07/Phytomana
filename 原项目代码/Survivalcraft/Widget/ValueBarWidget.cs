using Engine;
using Engine.Graphics;

namespace Game {
    public class ValueBarWidget : Widget {
        public float m_value;

        public int m_barsCount = 8;

        public Color m_litBarColor = new(16, 140, 0);

        public Color m_litBarColor2 = Color.Transparent;

        public Color m_unlitBarColor = new(48, 48, 48);

        public Subtexture m_barSubtexture;

        public LayoutDirection m_layoutDirection;

        public float m_flashCount;

        public bool m_textureLinearFilter;

        public float Value {
            get => m_value;
            set => m_value = MathUtils.Saturate(value);
        }

        public int BarsCount {
            get => m_barsCount;
            set => m_barsCount = Math.Clamp(value, 1, 1000);
        }

        public bool FlipDirection { get; set; }

        public Vector2 BarSize { get; set; }

        public float Spacing { get; set; }

        public Color LitBarColor {
            get => m_litBarColor;
            set => m_litBarColor = value;
        }

        public Color LitBarColor2 {
            get => m_litBarColor2;
            set => m_litBarColor2 = value;
        }

        public Color UnlitBarColor {
            get => m_unlitBarColor;
            set => m_unlitBarColor = value;
        }

        public bool BarBlending { get; set; }

        public bool HalfBars { get; set; }

        public Subtexture BarSubtexture {
            get => m_barSubtexture;
            set => m_barSubtexture = value;
        }

        public bool TextureLinearFilter {
            get => m_textureLinearFilter;
            set => m_textureLinearFilter = value;
        }

        public LayoutDirection LayoutDirection {
            get => m_layoutDirection;
            set => m_layoutDirection = value;
        }

        public ValueBarWidget() {
            IsHitTestVisible = false;
            BarSize = new Vector2(24f);
            BarBlending = true;
            TextureLinearFilter = true;
        }

        public void Flash(int count) {
            m_flashCount = MathUtils.Max(m_flashCount, count);
        }

        public override void Draw(DrawContext dc) {
            BaseBatch baseBatch = BarSubtexture == null
                ? dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None)
                : dc.PrimitivesRenderer2D.TexturedBatch(
                    BarSubtexture.Texture,
                    false,
                    0,
                    DepthStencilState.None,
                    null,
                    null,
                    TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp
                );
            int num;
            int start = 0;
            if (baseBatch is TexturedBatch2D batch2D) {
                num = batch2D.TriangleVertices.Count;
            }
            else {
                start = ((FlatBatch2D)baseBatch).LineVertices.Count;
                num = ((FlatBatch2D)baseBatch).TriangleVertices.Count;
            }
            Vector2 zero = Vector2.Zero;
            if (m_layoutDirection == LayoutDirection.Horizontal) {
                zero.X += Spacing / 2f;
            }
            else {
                zero.Y += Spacing / 2f;
            }
            int num2 = HalfBars ? 1 : 2;
            for (int i = 0; i < 2 * BarsCount; i += num2) {
                bool flag = i % 2 == 0;
                float num3 = 0.5f * i;
                float num4 = !FlipDirection
                    ? Math.Clamp((Value - num3 / BarsCount) * BarsCount, 0f, 1f)
                    : Math.Clamp((Value - (BarsCount - num3 - 1f) / BarsCount) * BarsCount, 0f, 1f);
                if (!BarBlending) {
                    num4 = MathF.Ceiling(num4);
                }
                float s = m_flashCount > 0f ? 1f - MathF.Abs(MathF.Sin(m_flashCount * (float)Math.PI)) : 1f;
                Color c = LitBarColor;
                if (LitBarColor2 != Color.Transparent
                    && BarsCount > 1) {
                    c = Color.Lerp(LitBarColor, LitBarColor2, num3 / (BarsCount - 1));
                }
                Color color = Color.Lerp(UnlitBarColor, c, num4) * s * GlobalColorTransform;
                if (HalfBars) {
                    if (flag) {
                        Vector2 zero2 = Vector2.Zero;
                        Vector2 v = m_layoutDirection == LayoutDirection.Horizontal ? new Vector2(0.5f, 1f) : new Vector2(1f, 0.5f);
                        if (baseBatch is TexturedBatch2D texturedBatch2D) {
                            if (BarSubtexture != null) {
                                Vector2 topLeft = BarSubtexture.TopLeft;
                                Vector2 texCoord = new(
                                    MathUtils.Lerp(BarSubtexture.TopLeft.X, BarSubtexture.BottomRight.X, v.X),
                                    MathUtils.Lerp(BarSubtexture.TopLeft.Y, BarSubtexture.BottomRight.Y, v.Y)
                                );
                                texturedBatch2D.QueueQuad(zero + zero2 * BarSize, zero + v * BarSize, 0f, topLeft, texCoord, color);
                            }
                        }
                        else {
                            ((FlatBatch2D)baseBatch).QueueQuad(zero + zero2 * BarSize, zero + v * BarSize, 0f, color);
                        }
                    }
                    else {
                        Vector2 v2 = m_layoutDirection == LayoutDirection.Horizontal ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
                        Vector2 one = Vector2.One;
                        if (baseBatch is TexturedBatch2D texturedBatch2D) {
                            if (BarSubtexture != null) {
                                Vector2 texCoord2 = new(
                                    MathUtils.Lerp(BarSubtexture.TopLeft.X, BarSubtexture.BottomRight.X, v2.X),
                                    MathUtils.Lerp(BarSubtexture.TopLeft.Y, BarSubtexture.BottomRight.Y, v2.Y)
                                );
                                Vector2 bottomRight = BarSubtexture.BottomRight;
                                texturedBatch2D.QueueQuad(zero + v2 * BarSize, zero + one * BarSize, 0f, texCoord2, bottomRight, color);
                            }
                        }
                        else {
                            ((FlatBatch2D)baseBatch).QueueQuad(zero + v2 * BarSize, zero + one * BarSize, 0f, color);
                        }
                    }
                }
                else {
                    Vector2 zero3 = Vector2.Zero;
                    Vector2 one2 = Vector2.One;
                    if (baseBatch is TexturedBatch2D texturedBatch2D) {
                        if (BarSubtexture != null) {
                            Vector2 topLeft2 = BarSubtexture.TopLeft;
                            Vector2 bottomRight2 = BarSubtexture.BottomRight;
                            texturedBatch2D.QueueQuad(zero + zero3 * BarSize, zero + one2 * BarSize, 0f, topLeft2, bottomRight2, color);
                        }
                    }
                    else {
                        ((FlatBatch2D)baseBatch).QueueQuad(zero + zero3 * BarSize, zero + one2 * BarSize, 0f, color);
                        ((FlatBatch2D)baseBatch).QueueRectangle(
                            zero + zero3 * BarSize,
                            zero + one2 * BarSize,
                            0f,
                            Color.MultiplyColorOnly(color, 0.75f)
                        );
                    }
                }
                if (!flag
                    || !HalfBars) {
                    if (m_layoutDirection == LayoutDirection.Horizontal) {
                        zero.X += BarSize.X + Spacing;
                    }
                    else {
                        zero.Y += BarSize.Y + Spacing;
                    }
                }
            }
            if (baseBatch is TexturedBatch2D batch) {
                batch.TransformTriangles(GlobalTransform, num);
            }
            else {
                ((FlatBatch2D)baseBatch).TransformLines(GlobalTransform, start);
                ((FlatBatch2D)baseBatch).TransformTriangles(GlobalTransform, num);
            }
            m_flashCount = MathUtils.Max(m_flashCount - 4f * Time.FrameDuration, 0f);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
            DesiredSize = m_layoutDirection == LayoutDirection.Horizontal
                ? new Vector2((BarSize.X + Spacing) * BarsCount, BarSize.Y)
                : new Vector2(BarSize.X, (BarSize.Y + Spacing) * BarsCount);
        }
    }
}