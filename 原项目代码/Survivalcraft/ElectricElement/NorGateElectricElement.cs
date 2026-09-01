namespace Game {
    public class NorGateElectricElement : RotateableElectricElement {
        public float m_voltage;

        public NorGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            int num = 0;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    num |= (int)MathF.Round(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace) * 15f);
                }
            }
            m_voltage = (~num & 0xF) / 15f;
            return m_voltage != voltage;
        }
    }
}