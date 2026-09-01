using Engine;
using Engine.Graphics;

namespace Game {
    public class BusyBarWidget : Widget {
        public const int m_barsCount = 5;

        public const float m_barSize = 8f;

        public const float m_barsSpacing = 24f;

        public int m_boxIndex;

        public double m_lastBoxesStepTime;

        public Color LitBarColor { get; set; }

        public Color UnlitBarColor { get; set; }

        public BusyBarWidget() {
            IsHitTestVisible = false;
            LitBarColor = new Color(16, 140, 0);
            UnlitBarColor = new Color(48, 48, 48);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
            DesiredSize = new Vector2(120f, 12f);
        }

        public override void Draw(DrawContext dc) {
            if (Time.RealTime - m_lastBoxesStepTime > 0.25) {
                m_boxIndex++;
                m_lastBoxesStepTime = Time.RealTime;
            }
            FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch();
            int count = flatBatch2D.TriangleVertices.Count;
            for (int i = 0; i < 5; i++) {
                Vector2 v = new((i + 0.5f) * 24f, 6f);
                Color c = i == m_boxIndex % 5 ? LitBarColor : UnlitBarColor;
                float v2 = i == m_boxIndex % 5 ? 12f : 8f;
                flatBatch2D.QueueQuad(v - new Vector2(v2) / 2f, v + new Vector2(v2) / 2f, 0f, c * GlobalColorTransform);
            }
            flatBatch2D.TransformTriangles(GlobalTransform, count);
        }
    }
}