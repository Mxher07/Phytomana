using Engine;
using Engine.Graphics;
using Engine.Input;

namespace Game {
    public class DebugCamera : BasePerspectiveCamera {
        public static string AmbientParameters = string.Empty;

        public static string PlantParameters = string.Empty;

        public Vector3 m_position;

        public Vector3 m_direction;

        public PrimitivesRenderer2D PrimitivesRenderer2D = new();

        Vector3 m_vrBaseHmdPosition;
        float m_vrBaseHmdYaw;
        float m_vrAccumulatedYaw;
        bool m_vrInitialized;

        public override bool UsesMovementControls => true;

        public override bool IsEntityControlEnabled => true;

        public DebugCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            m_position = previousCamera.ViewPosition;
            m_direction = previousCamera.ViewDirection;
            SetupPerspectiveCamera(m_position, m_direction, Vector3.UnitY);
            m_vrInitialized = false;
        }

        public override void Update(float dt) {
            if (Eye.HasValue) {
                UpdateVr(dt);
            }
            else {
                UpdateNonVr(dt);
            }
        }

        void UpdateVr(float dt) {
            dt = MathUtils.Min(dt, 0.1f);

            if (!m_vrInitialized) {
                m_vrBaseHmdPosition = VrManager.HmdMatrix.Translation;
                m_vrBaseHmdYaw = VrManager.HmdMatrixYpr.X;
                // Derive initial accumulated yaw from current view direction
                m_vrAccumulatedYaw = MathF.Atan2(m_direction.X, m_direction.Z) - m_vrBaseHmdYaw;
                m_vrInitialized = true;
            }

            ComponentInput componentInput = GameWidget.PlayerData.ComponentPlayer?.ComponentInput;
            Vector3 moveInput = Vector3.Zero;
            Vector2 lookInput = Vector2.Zero;
            if (componentInput != null) {
                moveInput = componentInput.PlayerInput.CameraMove * new Vector3(1f, 0f, 1f);
                lookInput = componentInput.PlayerInput.CameraLook;
            }

            bool shift = Keyboard.IsKeyDown(Key.Shift);
            bool ctrl = Keyboard.IsKeyDown(Key.Control);
            float speed = 8f;
            if (shift) speed *= 10f;
            if (ctrl) speed /= 10f;

            // Comfort turn: right stick rotates accumulated yaw
            m_vrAccumulatedYaw += -4f * lookInput.X * dt;
            // Snap turn camera rotation
            if (ComponentInput.VrSnapCameraRotation != 0f) {
                m_vrAccumulatedYaw += ComponentInput.VrSnapCameraRotation;
                ComponentInput.VrSnapCameraRotation = 0f;
            }

            // View direction from HMD + accumulated yaw
            Matrix hmdRot = VrManager.HmdMatrix * Matrix.CreateRotationY(m_vrAccumulatedYaw);
            m_direction = hmdRot.Forward;

            // Movement relative to view direction
            Vector3 right = Vector3.Normalize(Vector3.Cross(m_direction, Vector3.UnitY));
            Vector3 velocity = Vector3.Zero;
            velocity += speed * moveInput.X * right;
            velocity += speed * moveInput.Y * Vector3.UnitY;
            velocity += speed * moveInput.Z * m_direction;
            m_position += velocity * dt;

            // HMD drift
            Vector3 hmdOffset = VrManager.HmdMatrix.Translation - m_vrBaseHmdPosition;
            float yawCorrection = m_vrAccumulatedYaw;
            Vector3 gameOffset = Vector3.TransformNormal(hmdOffset, Matrix.CreateRotationY(yawCorrection));
            Vector3 cameraPos = m_position + gameOffset;

            SetupPerspectiveCamera(cameraPos, m_direction, hmdRot.Up);
        }

        void UpdateNonVr(float dt) {
            dt = MathUtils.Min(dt, 0.1f);
            Vector3 zero = Vector3.Zero;
            Vector2 vector = Vector2.Zero;
            ComponentInput componentInput = GameWidget.PlayerData.ComponentPlayer?.ComponentInput;
            if (componentInput != null) {
                zero = componentInput.PlayerInput.CameraMove * new Vector3(1f, 0f, 1f);
                vector = componentInput.PlayerInput.CameraLook;
            }
            bool num = Keyboard.IsKeyDown(Key.Shift);
            bool flag = Keyboard.IsKeyDown(Key.Control);
            Vector3 direction = m_direction;
            Vector3 unitY = Vector3.UnitY;
            Vector3 vector2 = Vector3.Normalize(Vector3.Cross(direction, unitY));
            float num2 = 8f;
            if (num) {
                num2 *= 10f;
            }
            if (flag) {
                num2 /= 10f;
            }
            Vector3 zero2 = Vector3.Zero;
            zero2 += num2 * zero.X * vector2;
            zero2 += num2 * zero.Y * unitY;
            zero2 += num2 * zero.Z * direction;
            m_position += zero2 * dt;
            m_direction = Vector3.Transform(m_direction, Matrix.CreateFromAxisAngle(unitY, -4f * vector.X * dt));
            m_direction = Vector3.Transform(m_direction, Matrix.CreateFromAxisAngle(vector2, 4f * vector.Y * dt));
            SetupPerspectiveCamera(m_position, m_direction, Vector3.UnitY);
            Vector2 v = ViewportSize / 2f;
            FlatBatch2D flatBatch2D = PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
            int count = flatBatch2D.LineVertices.Count;
            flatBatch2D.QueueLine(v - new Vector2(5f, 0f), v + new Vector2(5f, 0f), 0f, Color.White);
            flatBatch2D.QueueLine(v - new Vector2(0f, 5f), v + new Vector2(0f, 5f), 0f, Color.White);
            flatBatch2D.TransformLines(ViewportMatrix, count);
            PrimitivesRenderer2D.Flush();
        }
    }
}
