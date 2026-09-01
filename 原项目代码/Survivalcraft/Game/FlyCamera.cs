using Engine;
using Engine.Input;

namespace Game {
    public class FlyCamera : BasePerspectiveCamera {
        public Vector3 m_position;

        public Vector3 m_direction;

        public Vector3 m_velocity;

        public float m_rollSpeed;

        public float m_pitchSpeed;

        public float m_rollAngle;

        public override bool UsesMovementControls => true;

        public override bool IsEntityControlEnabled => false;

        public FlyCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            m_position = previousCamera.ViewPosition;
            m_direction = previousCamera.ViewDirection;
            SetupPerspectiveCamera(m_position, m_direction, Vector3.UnitY);
        }

        public override void Update(float dt) {
            Vector3 vector = Vector3.Zero;
            Vector2 vector2 = Vector2.Zero;
            ComponentInput componentInput = GameWidget.PlayerData.ComponentPlayer?.ComponentInput;
            if (componentInput != null) {
                vector = componentInput.PlayerInput.CameraMove * new Vector3(1f, 0f, 1f);
                vector2 = componentInput.PlayerInput.CameraLook;
            }
            bool num = Keyboard.IsKeyDown(Key.Shift);
            bool flag = Keyboard.IsKeyDown(Key.Control);
            Vector3 direction = m_direction;
            Vector3 unitY = Vector3.UnitY;
            Vector3 vector3 = Vector3.Normalize(Vector3.Cross(direction, unitY));
            float num2 = 10f;
            if (num) {
                num2 *= 5f;
            }
            if (flag) {
                num2 /= 5f;
            }
            Vector3 zero = Vector3.Zero;
            zero += num2 * vector.X * vector3;
            zero += num2 * vector.Y * unitY;
            zero += num2 * vector.Z * direction;
            m_rollSpeed = MathUtils.Lerp(m_rollSpeed, -1.5f * vector2.X, 3f * dt);
            m_rollAngle += m_rollSpeed * dt;
            m_rollAngle *= MathF.Pow(0.33f, dt);
            m_pitchSpeed = MathUtils.Lerp(m_pitchSpeed, -0.2f * vector2.Y, 3f * dt);
            m_pitchSpeed *= MathF.Pow(0.33f, dt);
            m_velocity += 1.5f * (zero - m_velocity) * dt;
            m_position += m_velocity * dt;
            m_direction = Vector3.Transform(m_direction, Matrix.CreateFromAxisAngle(unitY, 0.05f * m_rollAngle));
            m_direction = Vector3.Transform(m_direction, Matrix.CreateFromAxisAngle(vector3, 0.2f * m_pitchSpeed));
            Vector3 up = Vector3.TransformNormal(Vector3.UnitY, Matrix.CreateFromAxisAngle(m_direction, 0f - m_rollAngle));
            SetupPerspectiveCamera(m_position, m_direction, up);
        }
    }
}