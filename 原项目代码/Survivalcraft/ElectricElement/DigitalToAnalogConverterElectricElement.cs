namespace Game {
    public class DigitalToAnalogConverterElectricElement : RotateableElectricElement {
        public float m_voltage;

        public DigitalToAnalogConverterElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(
            subsystemElectricity,
            cellFace
        ) { }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            m_voltage = 0f;
            int rotation = Rotation;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0
                    && IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace))) {
                    ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(
                        CellFaces[0].Face,
                        rotation,
                        connection.ConnectorFace
                    );
                    if (connectorDirection.HasValue) {
                        if (connectorDirection.Value == ElectricConnectorDirection.Top) {
                            m_voltage += 71f / (339f * (float)Math.PI);
                        }
                        if (connectorDirection.Value == ElectricConnectorDirection.Right) {
                            m_voltage += 142f / (339f * (float)Math.PI);
                        }
                        if (connectorDirection.Value == ElectricConnectorDirection.Bottom) {
                            m_voltage += 4f / 15f;
                        }
                        if (connectorDirection.Value == ElectricConnectorDirection.Left) {
                            m_voltage += 8f / 15f;
                        }
                    }
                }
            }
            return m_voltage != voltage;
        }
    }
}