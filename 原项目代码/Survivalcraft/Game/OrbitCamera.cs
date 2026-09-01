using Engine;

namespace Game {
    public class OrbitCamera : BasePerspectiveCamera {
        public Vector3 m_position;

        public Vector2 m_angles = new(0f, MathUtils.DegToRad(30f));

        public float m_distance = 6f;

        Vector3 m_vrBaseHmdPosition;
        float m_vrBaseHmdYaw;
        float m_vrLastViewYaw;
        bool m_vrInitialized;

        public override bool UsesMovementControls => true;

        public override bool IsEntityControlEnabled => true;

        public OrbitCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
            m_vrInitialized = false;
        }

        Vector3 ClampToTerrain(Vector3 from, Vector3 to) {
            SubsystemTerrain terrain = GameWidget.SubsystemGameWidgets.SubsystemTerrain;
            Vector3 dir = to - from;
            float len = dir.Length();
            if (len < 0.01f) return to;
            Vector3 dirNorm = dir / len;
            TerrainRaycastResult? hit = terrain.Raycast(
                from, to + dirNorm * 0.6f, false, true,
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
            ComponentPlayer componentPlayer = GameWidget.PlayerData.ComponentPlayer;
            if (componentPlayer == null || GameWidget.Target == null) return;

            if (Eye.HasValue) {
                UpdateVr(dt);
            }
            else {
                UpdateNonVr(dt);
            }
        }

        void UpdateVr(float dt) {
            ComponentInput componentInput = GameWidget.PlayerData.ComponentPlayer.ComponentInput;
            Vector3 cameraSneakMove = componentInput.PlayerInput.CameraCrouchMove;
            Vector2 cameraLook = componentInput.PlayerInput.CameraLook;

            m_angles.X = MathUtils.NormalizeAngle(m_angles.X + 4f * cameraLook.X * dt + 0.5f * cameraSneakMove.X * dt);
            m_angles.Y = Math.Clamp(MathUtils.NormalizeAngle(m_angles.Y + 4f * cameraLook.Y * dt), MathUtils.DegToRad(-20f), MathUtils.DegToRad(70f));
            m_distance = Math.Clamp(m_distance - 10f * cameraSneakMove.Z * dt, 2f, 16f);
            // Snap turn camera rotation (negated: OrbitCamera uses positive convention for stick-right)
            if (ComponentInput.VrSnapCameraRotation != 0f) {
                m_angles.X -= ComponentInput.VrSnapCameraRotation;
                ComponentInput.VrSnapCameraRotation = 0f;
            }

            if (!m_vrInitialized) {
                ResetVrView();
                m_vrInitialized = true;
            }

            // Orbit offset (same as non-VR)
            Vector3 v = Vector3.Transform(new Vector3(m_distance, 0f, 0f), Matrix.CreateFromYawPitchRoll(m_angles.X, 0f, m_angles.Y));
            Vector3 target = GameWidget.Target.ComponentBody.Position + 0.9f * GameWidget.Target.ComponentBody.BoxSize.Y * Vector3.UnitY;
            Vector3 orbitPos = target + v;

            // Terrain collision
            Vector3 clampedPos = ClampToTerrain(target, orbitPos);

            // HMD drift
            Vector3 hmdOffset = VrManager.HmdMatrix.Translation - m_vrBaseHmdPosition;
            // Compute actual view yaw from orbit offset (direction camera→target)
            Vector3 toTarget = new(-v.X, 0, -v.Z);
            float viewYaw;
            if (toTarget.LengthSquared() > 0.001f) {
                viewYaw = MathF.Atan2(toTarget.X, toTarget.Z);
                m_vrLastViewYaw = viewYaw;
            }
            else {
                viewYaw = m_vrLastViewYaw;
            }
            float yawCorrection = viewYaw - m_vrBaseHmdYaw + MathF.PI;
            Vector3 gameOffset = Vector3.TransformNormal(hmdOffset, Matrix.CreateRotationY(yawCorrection));
            Vector3 cameraPos = clampedPos + gameOffset;
            m_position = cameraPos;

            // View orientation: HMD + yaw correction
            Matrix viewMatrix = Matrix.Identity;
            viewMatrix.Translation = cameraPos;
            viewMatrix.OrientationMatrix = VrManager.HmdMatrix * Matrix.CreateRotationY(yawCorrection);
            SetupPerspectiveCamera(viewMatrix.Translation, viewMatrix.Forward, viewMatrix.Up);
        }

        public void ResetVrView() {
            m_vrBaseHmdPosition = VrManager.HmdMatrix.Translation;
            m_vrBaseHmdYaw = VrManager.HmdMatrixYpr.X;
        }

        void UpdateNonVr(float dt) {
            ComponentInput componentInput = GameWidget.PlayerData.ComponentPlayer.ComponentInput;
            Vector3 cameraSneakMove = componentInput.PlayerInput.CameraCrouchMove;
            Vector2 cameraLook = componentInput.PlayerInput.CameraLook;
            m_angles.X = MathUtils.NormalizeAngle(m_angles.X + 4f * cameraLook.X * dt + 0.5f * cameraSneakMove.X * dt);
            m_angles.Y = Math.Clamp(MathUtils.NormalizeAngle(m_angles.Y + 4f * cameraLook.Y * dt), MathUtils.DegToRad(-20f), MathUtils.DegToRad(70f));
            m_distance = Math.Clamp(m_distance - 10f * cameraSneakMove.Z * dt, 2f, 16f);
            Vector3 v = Vector3.Transform(new Vector3(m_distance, 0f, 0f), Matrix.CreateFromYawPitchRoll(m_angles.X, 0f, m_angles.Y));
            Vector3 vector = GameWidget.Target.ComponentBody.Position + 0.9f * GameWidget.Target.ComponentBody.BoxSize.Y * Vector3.UnitY;
            Vector3 vector2 = vector + v;
            if (Vector3.Distance(vector2, m_position) < 10f) {
                Vector3 v2 = vector2 - m_position;
                float s = MathUtils.Saturate(10f * dt);
                m_position += s * v2;
            }
            else {
                m_position = vector2;
            }
            Vector3 vector3 = m_position - vector;
            float? num = null;
            Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector3, Vector3.UnitY));
            Vector3 v3 = Vector3.Normalize(Vector3.Cross(vector3, vector4));
            SubsystemTerrain subsystemTerrain = GameWidget.SubsystemGameWidgets.SubsystemTerrain;
            for (int i = 0; i <= 0; i++) {
                for (int j = 0; j <= 0; j++) {
                    Vector3 v4 = 0.6f * (vector4 * i + v3 * j);
                    Vector3 vector5 = vector + v4;
                    Vector3 end = vector5 + vector3 + Vector3.Normalize(vector3) * 0.6f;
                    TerrainRaycastResult? terrainRaycastResult = subsystemTerrain.Raycast(
                        vector5,
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
            Vector3 vector6 = !num.HasValue ? vector + vector3 : vector + Vector3.Normalize(vector3) * MathUtils.Max(num.Value - 0.5f, 0.2f);
            SetupPerspectiveCamera(vector6, vector - vector6, Vector3.UnitY);
        }
    }
}
