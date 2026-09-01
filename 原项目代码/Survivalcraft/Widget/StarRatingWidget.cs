using Engine;
using Engine.Graphics;

namespace Game {
    public class StarRatingWidget : Widget {
        public Texture2D m_texture;

        public float m_rating;

        public float StarSize { get; set; }

        public Color ForeColor { get; set; }

        public Color BackColor { get; set; }

        public float Rating {
            get => m_rating;
            set => m_rating = Math.Clamp(value, 0f, 5f);
        }

        public StarRatingWidget() {
            m_texture = ContentManager.Get<Texture2D>("Textures/Gui/RatingStar");
            ForeColor = new Color(255, 192, 0);
            BackColor = new Color(96, 96, 96);
            StarSize = 64f;
        }

        public override void Update() {
            if (Input.Press.HasValue
                && HitTestGlobal(Input.Press.Value) == this) {
                Vector2 vector = ScreenToWidget(Input.Press.Value);
                Rating = (int)MathF.Floor(5f * vector.X / ActualSize.X + 1f);
            }
        }

        public override void Draw(DrawContext dc) {
            TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(
                m_texture,
                false,
                0,
                DepthStencilState.None,
                null,
                null,
                SamplerState.LinearWrap
            );
            float x = 0f;
            float x2 = ActualSize.X * Rating / 5f;
            float x3 = ActualSize.X;
            float y = 0f;
            float y2 = ActualSize.Y;
            int count = texturedBatch2D.TriangleVertices.Count;
            texturedBatch2D.QueueQuad(
                new Vector2(x, y),
                new Vector2(x2, y2),
                0f,
                new Vector2(0f, 0f),
                new Vector2(Rating, 1f),
                ForeColor * GlobalColorTransform
            );
            texturedBatch2D.QueueQuad(
                new Vector2(x2, y),
                new Vector2(x3, y2),
                0f,
                new Vector2(Rating, 0f),
                new Vector2(5f, 1f),
                BackColor * GlobalColorTransform
            );
            texturedBatch2D.TransformTriangles(GlobalTransform, count);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
            DesiredSize = new Vector2(5f * StarSize, StarSize);
        }
    }
}