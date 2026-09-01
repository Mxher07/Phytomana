using Engine;

namespace Game {
    public class IntroCamera : BasePerspectiveCamera {
        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => false;

        public Vector3 CameraPosition { get; set; }

        public Vector3 TargetPosition { get; set; }

        public Vector3 TargetCameraPosition { get; set; }

        public float Speed { get; set; }

        float m_vrDeltaYaw;
        bool m_vrInitialized;

        public IntroCamera(GameWidget gameWidget) : base(gameWidget) => Speed = 1f;

        public override void Activate(Camera previousCamera) {
            SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
            m_vrInitialized = false;
        }

        public override void Update(float dt) {
            float x = Vector3.Distance(TargetCameraPosition, CameraPosition);
            CameraPosition += MathUtils.Min(dt * Speed, x) * Vector3.Normalize(TargetCameraPosition - CameraPosition);

            if (!Eye.HasValue) {
                SetupPerspectiveCamera(CameraPosition, TargetPosition - CameraPosition, Vector3.UnitY);
                return;
            }

            if (!m_vrInitialized) {
                Vector3 dir = TargetPosition - CameraPosition;
                if (dir.LengthSquared() > 0.001f) {
                    dir = Vector3.Normalize(dir);
                }
                else {
                    dir = Vector3.UnitZ;
                }
                Vector3 ypr = Matrix.CreateWorld(Vector3.Zero, dir, Vector3.UnitY).ToYawPitchRoll();
                m_vrDeltaYaw = ypr.X - VrManager.HmdMatrixYpr.X;
                m_vrInitialized = true;
            }

            Matrix viewMatrix = Matrix.Identity;
            viewMatrix.Translation = CameraPosition;
            viewMatrix.OrientationMatrix = VrManager.HmdMatrix * Matrix.CreateRotationY(m_vrDeltaYaw);
            SetupPerspectiveCamera(viewMatrix.Translation, viewMatrix.Forward, viewMatrix.Up);
        }
    }
}
