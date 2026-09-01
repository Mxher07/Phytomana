namespace Game {
    public class AndGateElectricElement : RotateableElectricElement {
        public float m_voltage;

        public AndGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            int num = 0;
            int num2 = 15;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    num2 &= (int)MathF.Round(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace) * 15f);
                    num++;
                }
            }
            m_voltage = num == 2 ? num2 / 15f : 0f;
            return m_voltage != voltage;
        }
    }
}