using Engine;
using Engine.Graphics;

namespace Game {
    public class BevelledRectangleWidget : Widget {
        public Texture2D m_texture;

        public bool m_textureLinearFilter;

        float m_roundingRadius;

        int m_roundingCount;

        float m_bevelSize;

        float m_ambientLight;

        float m_directionalLight;

        Color m_centerColor;

        Color m_bevelColor;

        Color m_shadowColor;

        float m_shadowSize;

        FlatBatch2D m_flatBatch;

        TexturedBatch2D m_texturedBatch;

        BevelledShapeRenderer.Point[] m_points = new BevelledShapeRenderer.Point[4];

        FlatBatch2D m_cachedShadowBatch = new();

        FlatBatch2D m_cachedFlatBatch = new();

        TexturedBatch2D m_cachedTexturedBatch = new();

        bool m_cachedBatchesValid;

        float m_cachedPixelsPerUnit;

        Vector2 m_cachedTextureScale;

        public Vector2 Size { get; set; }

        public float RoundingRadius {
            get => m_roundingRadius;
            set {
                if (value != m_roundingRadius) {
                    m_roundingRadius = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public int RoundingCount {
            get => m_roundingCount;
            set {
                if (value != m_roundingCount) {
                    m_roundingCount = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public float BevelSize {
            get => m_bevelSize;
            set {
                if (value != m_bevelSize) {
                    m_bevelSize = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public float DirectionalLight {
            get => m_directionalLight;
            set {
                if (value != m_directionalLight) {
                    m_directionalLight = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public float AmbientLight {
            get => m_ambientLight;
            set {
                if (value != m_ambientLight) {
                    m_ambientLight = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public Texture2D Texture {
            get => m_texture;
            set {
                if (value != m_texture) {
                    m_texture = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public float TextureScale { get; set; }

        public bool TextureLinearFilter {
            get => m_textureLinearFilter;
            set {
                if (value != m_textureLinearFilter) {
                    m_textureLinearFilter = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public Color CenterColor {
            get => m_centerColor;
            set {
                if (value != m_centerColor) {
                    m_centerColor = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public Color BevelColor {
            get => m_bevelColor;
            set {
                if (value != m_bevelColor) {
                    m_bevelColor = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public Color ShadowColor {
            get => m_shadowColor;
            set {
                if (value != m_shadowColor) {
                    m_shadowColor = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public float ShadowSize {
            get => m_shadowSize;
            set {
                if (value != m_shadowSize) {
                    m_shadowSize = value;
                    m_cachedBatchesValid = false;
                }
            }
        }

        public Vector2 TextureOffset => Vector2.Zero;

        public BevelledRectangleWidget() {
            Size = new Vector2(float.PositiveInfinity);
            TextureLinearFilter = false;
            TextureScale = 1f;
            RoundingRadius = 6f;
            RoundingCount = 3;
            BevelSize = 2f;
            AmbientLight = 0.6f;
            DirectionalLight = 0.4f;
            CenterColor = new Color(181, 172, 154);
            BevelColor = new Color(181, 172, 154);
            ShadowColor = new Color(0, 0, 0, 32);
            ShadowSize = 2f;
            IsHitTestVisible = false;
        }

        public override void Draw(DrawContext dc) {
            Color centerColor = CenterColor * new Vector4(0.6f, 0.6f, 0.6f, 1f);
            Color bevelColor = BevelColor;
            Color shadowColor = ShadowColor;
            bool flag = shadowColor != Color.Transparent && BevelSize > 0f;
            float globalScale = GlobalScale;
            if (globalScale != m_cachedPixelsPerUnit) {
                m_cachedPixelsPerUnit = globalScale;
                m_cachedBatchesValid = false;
            }
            Vector2 vector = new Vector2(TextureScale) / RootWidget.GlobalScale;
            if (vector != m_cachedTextureScale) {
                m_cachedTextureScale = vector;
                m_cachedBatchesValid = false;
            }
            float antialiasSize = 1f / globalScale;
            if (Texture != null) {
                if (!m_cachedBatchesValid) {
                    bool flatShading = m_points.Any(p => p.RoundingCount == 0);
                    m_cachedShadowBatch.Clear();
                    m_cachedTexturedBatch.Clear();
                    m_cachedTexturedBatch.Texture = Texture;
                    if (flag) {
                        BevelledShapeRenderer.QueueShapeShadow(m_cachedShadowBatch, m_points, globalScale, ShadowSize, shadowColor);
                    }
                    BevelledShapeRenderer.QueueShape(
                        m_cachedTexturedBatch,
                        m_points,
                        vector,
                        TextureOffset,
                        globalScale,
                        antialiasSize,
                        BevelSize,
                        flatShading,
                        centerColor,
                        bevelColor,
                        DirectionalLight,
                        AmbientLight
                    );
                    m_cachedBatchesValid = true;
                }
                if (flag) {
                    if (m_flatBatch == null) {
                        m_flatBatch = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
                    }
                    m_flatBatch.QueueBatch(
                        m_cachedShadowBatch,
                        Matrix.CreateTranslation(ShadowSize, ShadowSize, 0f) * GlobalTransform,
                        GlobalColorTransform
                    );
                }
                if (m_texturedBatch == null) {
                    m_texturedBatch = dc.PrimitivesRenderer2D.TexturedBatch(
                        Texture,
                        false,
                        1,
                        null,
                        null,
                        null,
                        TextureLinearFilter ? SamplerState.LinearWrap : SamplerState.PointWrap
                    );
                }
                m_texturedBatch.QueueBatch(m_cachedTexturedBatch, GlobalTransform, GlobalColorTransform);
                return;
            }
            if (!m_cachedBatchesValid) {
                bool flatShading2 = m_points.Any(p => p.RoundingCount == 0);
                m_cachedShadowBatch.Clear();
                m_cachedFlatBatch.Clear();
                if (flag) {
                    BevelledShapeRenderer.QueueShapeShadow(m_cachedShadowBatch, m_points, globalScale, ShadowSize, shadowColor);
                }
                BevelledShapeRenderer.QueueShape(
                    m_cachedFlatBatch,
                    m_points,
                    globalScale,
                    antialiasSize,
                    BevelSize,
                    flatShading2,
                    centerColor,
                    bevelColor,
                    DirectionalLight,
                    AmbientLight
                );
                m_cachedBatchesValid = true;
            }
            if (m_flatBatch == null) {
                m_flatBatch = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
            }
            if (flag) {
                m_flatBatch.QueueBatch(
                    m_cachedShadowBatch,
                    Matrix.CreateTranslation(ShadowSize, ShadowSize, 0f) * GlobalTransform,
                    GlobalColorTransform
                );
            }
            m_flatBatch.QueueBatch(m_cachedFlatBatch, GlobalTransform, GlobalColorTransform);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = BevelColor.A != 0 || CenterColor.A != 0;
            DesiredSize = Size;
        }

        public override void ArrangeOverride() {
            Vector2 vector = new(0f, 0f);
            Vector2 vector2 = new(ActualSize.X, 0f);
            Vector2 vector3 = new(ActualSize.X, ActualSize.Y);
            Vector2 vector4 = new(0f, ActualSize.Y);
            if (vector != m_points[0].Position
                || vector2 != m_points[1].Position
                || vector3 != m_points[2].Position
                || vector4 != m_points[3].Position) {
                m_points[0] = new BevelledShapeRenderer.Point { Position = vector, RoundingRadius = RoundingRadius, RoundingCount = RoundingCount };
                m_points[1] = new BevelledShapeRenderer.Point { Position = vector2, RoundingRadius = RoundingRadius, RoundingCount = RoundingCount };
                m_points[2] = new BevelledShapeRenderer.Point { Position = vector3, RoundingRadius = RoundingRadius, RoundingCount = RoundingCount };
                m_points[3] = new BevelledShapeRenderer.Point { Position = vector4, RoundingRadius = RoundingRadius, RoundingCount = RoundingCount };
                m_cachedBatchesValid = false;
            }
        }

        public static void QueueBevelledRectangle(TexturedBatch2D texturedBatch,
            FlatBatch2D flatBatch,
            Vector2 c1,
            Vector2 c2,
            float depth,
            float bevelSize,
            Color color,
            Color bevelColor,
            Color shadowColor,
            float ambientLight,
            float directionalLight,
            float textureScale) {
            float num = MathF.Abs(bevelSize);
            Vector2 vector = c1;
            Vector2 vector2 = c1 + new Vector2(num);
            Vector2 vector3 = c2 - new Vector2(num);
            Vector2 vector4 = c2;
            Vector2 vector5 = c2 + new Vector2(1.5f * num);
            float x = vector.X;
            float x2 = vector2.X;
            float x3 = vector3.X;
            float x4 = vector4.X;
            float x5 = vector5.X;
            float y = vector.Y;
            float y2 = vector2.Y;
            float y3 = vector3.Y;
            float y4 = vector4.Y;
            float y5 = vector5.Y;
            float num2 = MathUtils.Saturate((bevelSize > 0f ? 1f : -0.75f) * directionalLight + ambientLight);
            float num3 = MathUtils.Saturate((bevelSize > 0f ? -0.75f : 1f) * directionalLight + ambientLight);
            float num4 = MathUtils.Saturate((bevelSize > 0f ? -0.375f : 0.5f) * directionalLight + ambientLight);
            float num5 = MathUtils.Saturate((bevelSize > 0f ? 0.5f : -0.375f) * directionalLight + ambientLight);
            float num6 = MathUtils.Saturate(0f * directionalLight + ambientLight);
            Color color2 = new((byte)(num4 * bevelColor.R), (byte)(num4 * bevelColor.G), (byte)(num4 * bevelColor.B), bevelColor.A);
            Color color3 = new((byte)(num5 * bevelColor.R), (byte)(num5 * bevelColor.G), (byte)(num5 * bevelColor.B), bevelColor.A);
            Color color4 = new((byte)(num2 * bevelColor.R), (byte)(num2 * bevelColor.G), (byte)(num2 * bevelColor.B), bevelColor.A);
            Color color5 = new((byte)(num3 * bevelColor.R), (byte)(num3 * bevelColor.G), (byte)(num3 * bevelColor.B), bevelColor.A);
            Color color6 = new((byte)(num6 * color.R), (byte)(num6 * color.G), (byte)(num6 * color.B), color.A);
            if (texturedBatch != null) {
                float num7 = textureScale / texturedBatch.Texture.Width;
                float num8 = textureScale / texturedBatch.Texture.Height;
                float num9 = x * num7;
                float num10 = y * num8;
                float x6 = num9;
                float x7 = (x2 - x) * num7 + num9;
                float x8 = (x3 - x) * num7 + num9;
                float x9 = (x4 - x) * num7 + num9;
                float y6 = num10;
                float y7 = (y2 - y) * num8 + num10;
                float y8 = (y3 - y) * num8 + num10;
                float y9 = (y4 - y) * num8 + num10;
                if (bevelColor.A > 0) {
                    texturedBatch.QueueQuad(
                        new Vector2(x, y),
                        new Vector2(x2, y2),
                        new Vector2(x3, y2),
                        new Vector2(x4, y),
                        depth,
                        new Vector2(x6, y6),
                        new Vector2(x7, y7),
                        new Vector2(x8, y7),
                        new Vector2(x9, y6),
                        color4
                    );
                    texturedBatch.QueueQuad(
                        new Vector2(x3, y2),
                        new Vector2(x3, y3),
                        new Vector2(x4, y4),
                        new Vector2(x4, y),
                        depth,
                        new Vector2(x8, y7),
                        new Vector2(x8, y8),
                        new Vector2(x9, y9),
                        new Vector2(x9, y6),
                        color3
                    );
                    texturedBatch.QueueQuad(
                        new Vector2(x, y4),
                        new Vector2(x4, y4),
                        new Vector2(x3, y3),
                        new Vector2(x2, y3),
                        depth,
                        new Vector2(x6, y9),
                        new Vector2(x9, y9),
                        new Vector2(x8, y8),
                        new Vector2(x7, y8),
                        color5
                    );
                    texturedBatch.QueueQuad(
                        new Vector2(x, y),
                        new Vector2(x, y4),
                        new Vector2(x2, y3),
                        new Vector2(x2, y2),
                        depth,
                        new Vector2(x6, y6),
                        new Vector2(x6, y9),
                        new Vector2(x7, y8),
                        new Vector2(x7, y7),
                        color2
                    );
                }
                if (color6.A > 0) {
                    texturedBatch.QueueQuad(new Vector2(x2, y2), new Vector2(x3, y3), depth, new Vector2(x7, y7), new Vector2(x8, y8), color6);
                }
            }
            else if (flatBatch != null) {
                if (bevelColor.A > 0) {
                    flatBatch.QueueQuad(new Vector2(x, y), new Vector2(x2, y2), new Vector2(x3, y2), new Vector2(x4, y), depth, color4);
                    flatBatch.QueueQuad(new Vector2(x3, y2), new Vector2(x3, y3), new Vector2(x4, y4), new Vector2(x4, y), depth, color3);
                    flatBatch.QueueQuad(new Vector2(x, y4), new Vector2(x4, y4), new Vector2(x3, y3), new Vector2(x2, y3), depth, color5);
                    flatBatch.QueueQuad(new Vector2(x, y), new Vector2(x, y4), new Vector2(x2, y3), new Vector2(x2, y2), depth, color2);
                }
                if (color6.A > 0) {
                    flatBatch.QueueQuad(new Vector2(x2, y2), new Vector2(x3, y3), depth, color6);
                }
            }
            if (bevelSize > 0f
                && flatBatch != null
                && shadowColor.A > 0) {
                Color color7 = shadowColor;
                Color color8 = new(0, 0, 0, 0);
                flatBatch.QueueTriangle(
                    new Vector2(x, y4),
                    new Vector2(x2, y5),
                    new Vector2(x2, y4),
                    depth,
                    color8,
                    color8,
                    color7
                );
                flatBatch.QueueTriangle(
                    new Vector2(x4, y),
                    new Vector2(x4, y2),
                    new Vector2(x5, y2),
                    depth,
                    color8,
                    color7,
                    color8
                );
                flatBatch.QueueTriangle(
                    new Vector2(x4, y4),
                    new Vector2(x4, y5),
                    new Vector2(x5, y4),
                    depth,
                    color7,
                    color8,
                    color8
                );
                flatBatch.QueueQuad(
                    new Vector2(x2, y4),
                    new Vector2(x2, y5),
                    new Vector2(x4, y5),
                    new Vector2(x4, y4),
                    depth,
                    color7,
                    color8,
                    color8,
                    color7
                );
                flatBatch.QueueQuad(
                    new Vector2(x4, y2),
                    new Vector2(x4, y4),
                    new Vector2(x5, y4),
                    new Vector2(x5, y2),
                    depth,
                    color7,
                    color7,
                    color8,
                    color8
                );
            }
        }
    }
}