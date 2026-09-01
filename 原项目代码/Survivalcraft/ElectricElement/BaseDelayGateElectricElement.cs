namespace Game {
    public abstract class BaseDelayGateElectricElement : RotateableElectricElement {
        public float m_voltage;

        public float m_lastStoredVoltage;

        public Dictionary<int, float> m_voltagesHistory = [];

        public abstract int DelaySteps { get; }

        public BaseDelayGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            int delaySteps = DelaySteps;
            float num = 0f;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    num = connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace);
                    break;
                }
            }
            if (delaySteps > 0) {
                if (m_voltagesHistory.TryGetValue(SubsystemElectricity.CircuitStep, out float value)) {
                    m_voltage = value;
                    m_voltagesHistory.Remove(SubsystemElectricity.CircuitStep);
                }
                if (num != m_lastStoredVoltage) {
                    m_lastStoredVoltage = num;
                    if (m_voltagesHistory.Count < 300) {
                        m_voltagesHistory[SubsystemElectricity.CircuitStep + DelaySteps] = num;
                        SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + DelaySteps);
                    }
                }
            }
            else {
                m_voltage = num;
            }
            return m_voltage != voltage;
        }
    }
}