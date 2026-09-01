using Engine;

namespace Game {
    public class StraightFlightCamera : BasePerspectiveCamera {
        public Vector3 m_position;

        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => false;

        public StraightFlightCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            m_position = previousCamera.ViewPosition;
            SetupPerspectiveCamera(m_position, previousCamera.ViewDirection, previousCamera.ViewUp);
        }

        public override void Update(float dt) {
            Vector3 vector = 10f * (Vector3.UnitX + (float)Math.Sin(0.20000000298023224 * Time.FrameStartTime) * Vector3.UnitZ);
            m_position.Y = 120f;
            m_position += vector * dt;
            SetupPerspectiveCamera(m_position, vector, Vector3.UnitY);
        }
    }
}