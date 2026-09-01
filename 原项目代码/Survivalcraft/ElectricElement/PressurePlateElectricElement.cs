using Engine;

namespace Game {
    public class PressurePlateElectricElement : MountedElectricElement {
        public float m_voltage;

        public int m_lastPressFrameIndex;

        public float m_pressure;

        public PressurePlateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public void Press(float pressure) {
            m_lastPressFrameIndex = Time.FrameIndex;
            if (pressure > m_pressure) {
                m_pressure = pressure;
                CellFace cellFace = CellFaces[0];
                SubsystemElectricity.SubsystemAudio.PlaySound(
                    "Audio/BlockPlaced",
                    1f,
                    0.3f,
                    new Vector3(cellFace.X, cellFace.Y, cellFace.Z),
                    2.5f,
                    true
                );
                SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 1);
            }
        }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            if (m_pressure > 0f
                && Time.FrameIndex - m_lastPressFrameIndex < 2) {
                m_voltage = PressureToVoltage(m_pressure);
                SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 10);
            }
            else {
                if (IsSignalHigh(m_voltage)) {
                    CellFace cellFace = CellFaces[0];
                    SubsystemElectricity.SubsystemAudio.PlaySound(
                        "Audio/BlockPlaced",
                        0.6f,
                        -0.1f,
                        new Vector3(cellFace.X, cellFace.Y, cellFace.Z),
                        2.5f,
                        true
                    );
                }
                m_voltage = 0f;
                m_pressure = 0f;
            }
            return m_voltage != voltage;
        }

        public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody) {
            Press(componentBody.Mass);
            componentBody.ApplyImpulse(new Vector3(0f, -2E-05f, 0f));
        }

        public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) {
            int num = Terrain.ExtractContents(worldItem.Value);
            Block block = BlocksManager.Blocks[num];
            Press(1f * block.GetDensity(worldItem.Value));
        }

        public static float PressureToVoltage(float pressure) {
            if (pressure <= 0f) {
                return 0f;
            }
            if (pressure < 1f) {
                return 8f / 15f;
            }
            if (pressure < 2f) {
                return 0.6f;
            }
            if (pressure < 5f) {
                return 2f / 3f;
            }
            if (pressure < 25f) {
                return 11f / 15f;
            }
            if (pressure < 100f) {
                return 0.8f;
            }
            if (pressure < 250f) {
                return 13f / 15f;
            }
            if (pressure < 500f) {
                return 14f / 15f;
            }
            return 1f;
        }
    }
}