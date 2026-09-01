namespace Game {
    public class XorGateElectricElement : RotateableElectricElement {
        public float m_voltage;

        public XorGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            int? num = null;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    int num2 = (int)MathF.Round(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace) * 15f);
                    num = num.HasValue ? num ^ num2 : num2;
                }
            }
            m_voltage = num.HasValue ? num.Value / 15f : 0f;
            return m_voltage != voltage;
        }
    }
}