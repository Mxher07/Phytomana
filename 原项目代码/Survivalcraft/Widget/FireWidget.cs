using Engine;

namespace Game {
    public class FireWidget : CanvasWidget {
        public ScreenSpaceFireRenderer m_fireRenderer = new(100);

        public float ParticlesPerSecond {
            get => m_fireRenderer.ParticlesPerSecond;
            set => m_fireRenderer.ParticlesPerSecond = value;
        }

        public FireWidget() => ClampToBounds = true;

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
            base.MeasureOverride(parentAvailableSize);
        }

        public override void Draw(DrawContext dc) {
            m_fireRenderer.Draw(dc.PrimitivesRenderer2D, 0f, GlobalTransform, GlobalColorTransform);
        }

        public override void Update() {
            float dt = Math.Clamp(Time.FrameDuration, 0f, 0.1f);
            m_fireRenderer.Origin = new Vector2(0f, ActualSize.Y);
            m_fireRenderer.CutoffPosition = float.NegativeInfinity;
            m_fireRenderer.ParticleSize = 32f;
            m_fireRenderer.ParticleSpeed = 32f;
            m_fireRenderer.Width = ActualSize.X;
            m_fireRenderer.MinTimeToLive = 0.5f;
            m_fireRenderer.MaxTimeToLive = 2f;
            m_fireRenderer.ParticleAnimationPeriod = 1.25f;
            m_fireRenderer.Update(dt);
        }
    }
}