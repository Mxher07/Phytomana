namespace Game {
    public class TruthTableCircuitElectricElement : RotateableElectricElement {
        public SubsystemTruthTableCircuitBlockBehavior m_subsystemTruthTableCircuitBlockBehavior;

        public float m_voltage;

        public TruthTableCircuitElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) :
            base(subsystemElectricity, cellFace) =>
            m_subsystemTruthTableCircuitBlockBehavior = subsystemElectricity.Project.FindSubsystem<SubsystemTruthTableCircuitBlockBehavior>(true);

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            int num = 0;
            int rotation = Rotation;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(
                        CellFaces[0].Face,
                        rotation,
                        connection.ConnectorFace
                    );
                    if (connectorDirection.HasValue) {
                        if (connectorDirection == ElectricConnectorDirection.Top) {
                            if (IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace))) {
                                num |= 1;
                            }
                        }
                        else if (connectorDirection == ElectricConnectorDirection.Right) {
                            if (IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace))) {
                                num |= 2;
                            }
                        }
                        else if (connectorDirection == ElectricConnectorDirection.Bottom) {
                            if (IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace))) {
                                num |= 4;
                            }
                        }
                        else if (connectorDirection == ElectricConnectorDirection.Left
                            && IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace))) {
                            num |= 8;
                        }
                    }
                }
            }
            TruthTableData blockData = m_subsystemTruthTableCircuitBlockBehavior.GetBlockData(CellFaces[0].Point);
            m_voltage = blockData != null ? blockData.Data[num] / 15f : 0f;
            return m_voltage != voltage;
        }
    }
}