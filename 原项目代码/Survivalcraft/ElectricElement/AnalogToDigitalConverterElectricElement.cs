namespace Game {
    public class AnalogToDigitalConverterElectricElement : RotateableElectricElement {
        public int m_bits;

        public AnalogToDigitalConverterElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(
            subsystemElectricity,
            cellFace
        ) { }

        public override float GetOutputVoltage(int face) {
            ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(CellFaces[0].Face, Rotation, face);
            if (connectorDirection.HasValue) {
                if (connectorDirection.Value == ElectricConnectorDirection.Top) {
                    return (m_bits & 1) != 0 ? 1 : 0;
                }
                if (connectorDirection.Value == ElectricConnectorDirection.Right) {
                    return (m_bits & 2) != 0 ? 1 : 0;
                }
                if (connectorDirection.Value == ElectricConnectorDirection.Bottom) {
                    return (m_bits & 4) != 0 ? 1 : 0;
                }
                if (connectorDirection.Value == ElectricConnectorDirection.Left) {
                    return (m_bits & 8) != 0 ? 1 : 0;
                }
            }
            return 0f;
        }

        public override bool Simulate() {
            int bits = m_bits;
            int rotation = Rotation;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(
                        CellFaces[0].Face,
                        rotation,
                        connection.ConnectorFace
                    );
                    if (connectorDirection.HasValue
                        && connectorDirection.Value == ElectricConnectorDirection.In) {
                        float outputVoltage = connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace);
                        m_bits = (int)MathF.Round(outputVoltage * 15f);
                    }
                }
            }
            return m_bits != bits;
        }
    }
}