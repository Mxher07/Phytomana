using Engine;

namespace Game {
    public class RandomGeneratorElectricElement : RotateableElectricElement {
        public bool m_clockAllowed = true;

        public float m_voltage;

        public static Random s_random = new();

        public RandomGeneratorElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) {
            float? num = SubsystemElectricity.ReadPersistentVoltage(CellFaces[0].Point);
            m_voltage = num ?? GetRandomVoltage();
        }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            bool flag = false;
            bool flag2 = false;
            _ = Rotation;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    if (IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace))) {
                        if (m_clockAllowed) {
                            flag = true;
                            m_clockAllowed = false;
                        }
                    }
                    else {
                        m_clockAllowed = true;
                    }
                    flag2 = true;
                }
            }
            if (flag2) {
                if (flag) {
                    m_voltage = GetRandomVoltage();
                }
            }
            else {
                m_voltage = GetRandomVoltage();
                SubsystemElectricity.QueueElectricElementForSimulation(
                    this,
                    SubsystemElectricity.CircuitStep + MathUtils.Max((int)(s_random.Float(0.25f, 0.75f) / 0.01f), 1)
                );
            }
            if (m_voltage != voltage) {
                SubsystemElectricity.WritePersistentVoltage(CellFaces[0].Point, m_voltage);
                return true;
            }
            return false;
        }

        public static float GetRandomVoltage() => s_random.Int(0, 15) / 15f;
    }
}