using Engine;

namespace Game {
    public class TppCamera : BasePerspectiveCamera {
        public Vector3 m_position;

        Vector3 m_vrBaseHmdPosition;
        float m_vrBaseHmdYaw;
        float m_vrLastCharYaw;
        bool m_vrInitialized;

        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => true;

        public TppCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            m_position = previousCamera.ViewPosition;
            SetupPerspectiveCamera(m_position, previousCamera.ViewDirection, previousCamera.ViewUp);
            m_vrInitialized = false;
        }

        public void ResetVrView() {
            m_vrBaseHmdPosition = VrManager.HmdMatrix.Translation;
            m_vrBaseHmdYaw = VrManager.HmdMatrixYpr.X;
        }

        Vector3 ClampToTerrain(Vector3 from, Vector3 to) {
            SubsystemTerrain terrain = GameWidget.SubsystemGameWidgets.SubsystemTerrain;
            Vector3 dir = to - from;
            float len = dir.Length();
            if (len < 0.01f) return to;
            Vector3 dirNorm = dir / len;
            TerrainRaycastResult? hit = terrain.Raycast(
                from, to + dirNorm * 0.5f, false, true,
                delegate(int value, float _) {
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                    for (int k = 0; k < 6; k++) {
                        if (!block.IsFaceTransparent(terrain, k, value)) return true;
                    }
                    return false;
                }
            );
            if (hit.HasValue) {
                return from + dirNorm * MathUtils.Max(hit.Value.Distance - 0.5f, 0.2f);
            }
            return to;
        }

        public override void Update(float dt) {
            if (GameWidget.Target == null) return;

            if (Eye.HasValue) {
                UpdateVr(dt);
            }
            else {
                UpdateNonVr(dt);
            }
        }

        void UpdateVr(float dt) {
            if (!m_vrInitialized) {
                Matrix initMatrix = Matrix.CreateFromQuaternion(GameWidget.Target.ComponentCreatureModel.EyeRotation);
                Vector3 initFwd = new(initMatrix.Forward.X, 0, initMatrix.Forward.Z);
                if (initFwd.LengthSquared() > 0.001f) initFwd = Vector3.Normalize(initFwd);
                m_vrLastCharYaw = MathF.Atan2(initFwd.X, initFwd.Z);
                ResetVrView();
                m_vrInitialized = true;
            }

            // Anchor position: behind character using EyeRotation (same as non-VR)
            Matrix matrix = Matrix.CreateFromQuaternion(GameWidget.Target.ComponentCreatureModel.EyeRotation);
            matrix.Translation = GameWidget.Target.ComponentBody.Position
                + 0.9f * GameWidget.Target.ComponentBody.BoxSize.Y * Vector3.UnitY;
            Vector3 anchor = matrix.Translation + (-2.25f * matrix.Forward + 1.75f * matrix.Up);

            // Terrain collision
            Vector3 clampedAnchor = ClampToTerrain(matrix.Translation, anchor);

            // HMD drift: real-world walking offset
            Vector3 hmdOffset = VrManager.HmdMatrix.Translation - m_vrBaseHmdPosition;
            // Correction = charYaw - baseHmdYaw: updates when character rotates, not when head turns
            Vector3 forward = new(matrix.Forward.X, 0, matrix.Forward.Z);
            float charYaw;
            if (forward.LengthSquared() > 0.001f) {
                charYaw = MathF.Atan2(forward.X, forward.Z);
                m_vrLastCharYaw = charYaw;
            }
            else {
                charYaw = m_vrLastCharYaw;
            }
            float yawCorrection = charYaw - m_vrBaseHmdYaw + MathF.PI;
            Vector3 gameOffset = Vector3.TransformNormal(hmdOffset, Matrix.CreateRotationY(yawCorrection));
            Vector3 cameraPos = clampedAnchor + gameOffset;
            m_position = cameraPos;

            Matrix viewMatrix = Matrix.Identity;
            viewMatrix.Translation = cameraPos;
            viewMatrix.OrientationMatrix = VrManager.HmdMatrix * Matrix.CreateRotationY(yawCorrection);
            SetupPerspectiveCamera(viewMatrix.Translation, viewMatrix.Forward, viewMatrix.Up);
        }

        void UpdateNonVr(float dt) {
            Matrix matrix = Matrix.CreateFromQuaternion(GameWidget.Target.ComponentCreatureModel.EyeRotation);
            matrix.Translation = GameWidget.Target.ComponentBody.Position + 0.9f * GameWidget.Target.ComponentBody.BoxSize.Y * Vector3.UnitY;
            Vector3 v = -2.25f * matrix.Forward + 1.75f * matrix.Up;
            Vector3 vector = matrix.Translation + v;
            if (Vector3.Distance(vector, m_position) < 10f) {
                Vector3 v2 = vector - m_position;
                float s = 3f * dt;
                m_position += s * v2;
            }
            else {
                m_position = vector;
            }
            Vector3 vector2 = m_position - matrix.Translation;
            float? num = null;
            Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector2, Vector3.UnitY));
            Vector3 v3 = Vector3.Normalize(Vector3.Cross(vector2, vector3));
            SubsystemTerrain subsystemTerrain = GameWidget.SubsystemGameWidgets.SubsystemTerrain;
            for (int i = 0; i <= 0; i++) {
                for (int j = 0; j <= 0; j++) {
                    Vector3 v4 = 0.5f * (vector3 * i + v3 * j);
                    Vector3 vector4 = matrix.Translation + v4;
                    Vector3 end = vector4 + vector2 + Vector3.Normalize(vector2) * 0.5f;
                    TerrainRaycastResult? terrainRaycastResult = subsystemTerrain.Raycast(
                        vector4,
                        end,
                        false,
                        true,
                        delegate(int value, float _) {
                            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                            for (int k = 0; k < 6; k++) {
                                if (!block.IsFaceTransparent(subsystemTerrain, k, value)) {
                                    return true;
                                }
                            }
                            return false;
                        }
                    );
                    if (terrainRaycastResult.HasValue) {
                        num = num.HasValue ? MathUtils.Min(num.Value, terrainRaycastResult.Value.Distance) : terrainRaycastResult.Value.Distance;
                    }
                }
            }
            Vector3 vector5 = !num.HasValue
                ? matrix.Translation + vector2
                : matrix.Translation + Vector3.Normalize(vector2) * MathUtils.Max(num.Value - 0.5f, 0.2f);
            SetupPerspectiveCamera(vector5, matrix.Translation - vector5, Vector3.UnitY);
        }
    }
}
