using Engine;

namespace Game {
    public class FixedCamera : BasePerspectiveCamera {
        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => true;

        Vector3 m_vrBaseHmdPosition;
        Vector3 m_vrBasePosition;
        bool m_vrInitialized;

        public FixedCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
            m_vrInitialized = false;
        }

        public override void Update(float dt) {
            if (!Eye.HasValue) return;

            if (!m_vrInitialized) {
                m_vrBasePosition = ViewPosition;
                m_vrBaseHmdPosition = VrManager.HmdMatrix.Translation;
                m_vrInitialized = true;
            }

            // Fixed base position + HMD drift
            Vector3 hmdOffset = VrManager.HmdMatrix.Translation - m_vrBaseHmdPosition;
            Vector3 cameraPos = m_vrBasePosition + hmdOffset;

            // HMD provides full orientation (yaw/pitch/roll)
            SetupPerspectiveCamera(cameraPos, VrManager.HmdMatrix.Forward, VrManager.HmdMatrix.Up);
        }
    }
}
