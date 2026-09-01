using Engine;

namespace Game {
    public class RandomJumpCamera : BasePerspectiveCamera {
        public const float frequencyFactor = 0.5f;

        public Random m_random = new();

        public float m_frequency = 0.5f;

        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => false;

        public RandomJumpCamera(GameWidget gameWidget) : base(gameWidget) { }

        public override void Activate(Camera previousCamera) {
            SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
        }

        public override void Update(float dt) {
            if (m_random.Float(0f, 1f) < 0.1f * dt) {
                m_frequency = m_random.Float(0.33f, 5f) * 0.5f;
            }
            if (m_random.Float(0f, 1f) < m_frequency * dt) {
                SubsystemPlayers subsystemPlayers = GameWidget.SubsystemGameWidgets.Project.FindSubsystem<SubsystemPlayers>(true);
                if (subsystemPlayers.PlayersData.Count > 0) {
                    Vector3 spawnPosition = subsystemPlayers.PlayersData[0].SpawnPosition;
                    spawnPosition.X += m_random.Float(-150f, 150f);
                    spawnPosition.Y = m_random.Float(70f, 120f);
                    spawnPosition.Z += m_random.Float(-150f, 150f);
                    Vector3 direction = m_random.Vector3(1f);
                    SetupPerspectiveCamera(spawnPosition, direction, Vector3.UnitY);
                }
            }
            if (m_random.Float(0f, 1f) < 0.5f * m_frequency * dt) {
                GameWidget.SubsystemGameWidgets.Project.FindSubsystem<SubsystemTimeOfDay>(true).TimeOfDayOffset = m_random.Float(0f, 1f);
            }
            if (m_random.Float(0f, 1f) < 1f * dt * 0.5f) {
                GameManager.SaveProject(false, false);
            }
        }
    }
}