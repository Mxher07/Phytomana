using Engine;

namespace Game {
    public class DeathCamera : BasePerspectiveCamera {
        public Vector3 m_position;

        public Vector3? m_bestPosition;

        public float m_vrDeltaYaw;

        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => false;

        public DeathCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            m_position = previousCamera.ViewPosition;
            Vector3 vector = GameWidget.Target?.ComponentBody.BoundingBox.Center() ?? m_position;
            m_bestPosition = FindBestCameraPosition(vector, 6f);
            SetupPerspectiveCamera(m_position, vector - m_position, Vector3.UnitY);
            if (GameWidget.Target is ComponentPlayer player
                && player.ComponentInput.IsControlledByVr
                && m_bestPosition.HasValue) {
                Vector3 vector2 = Matrix.CreateWorld(Vector3.Zero, vector - m_bestPosition.Value, Vector3.UnitY).ToYawPitchRoll();
                m_vrDeltaYaw = vector2.X - VrManager.HmdMatrixYpr.X;
            }
        }

        public override void Update(float dt) {
            Vector3 v = GameWidget.Target?.ComponentBody.BoundingBox.Center() ?? m_position;
            if (m_bestPosition.HasValue) {
                if (Vector3.Distance(m_bestPosition.Value, m_position) > 20f) {
                    m_position = m_bestPosition.Value;
                }
                m_position += 1.5f * dt * (m_bestPosition.Value - m_position);
            }

            if (!Eye.HasValue) {
                SetupPerspectiveCamera(m_position, v - m_position, Vector3.UnitY);
                return;
            }

            Matrix identity = Matrix.Identity;
            identity.Translation = m_position;
            identity.OrientationMatrix = VrManager.HmdMatrix * Matrix.CreateRotationY(m_vrDeltaYaw);
            SetupPerspectiveCamera(identity.Translation, identity.Forward, identity.Up);
        }

        public Vector3 FindBestCameraPosition(Vector3 targetPosition, float distance) {
            Vector3? vector = null;
            for (int i = 0; i < 36; i++) {
                float x = 1f + (float)Math.PI * 2f * i / 36f;
                Vector3 v2 = Vector3.Normalize(new Vector3(MathF.Sin(x), 0.5f, MathF.Cos(x)));
                Vector3 vector2 = targetPosition + v2 * distance;
                TerrainRaycastResult? terrainRaycastResult = GameWidget.SubsystemGameWidgets.SubsystemTerrain.Raycast(
                    targetPosition,
                    vector2,
                    false,
                    true,
                    (v, _) => !BlocksManager.Blocks[Terrain.ExtractContents(v)].IsTransparent_(v)
                );
                Vector3 zero;
                if (terrainRaycastResult.HasValue) {
                    CellFace cellFace = terrainRaycastResult.Value.CellFace;
                    zero = new Vector3(cellFace.X + 0.5f, cellFace.Y + 0.5f, cellFace.Z + 0.5f) - 1f * v2;
                }
                else {
                    zero = vector2;
                }
                if (!vector.HasValue
                    || Vector3.Distance(zero, targetPosition) > Vector3.Distance(vector.Value, targetPosition)) {
                    vector = zero;
                }
            }
            if (vector.HasValue) {
                return vector.Value;
            }
            return targetPosition;
        }
    }
}
