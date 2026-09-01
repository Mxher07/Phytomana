namespace Game {
    public class SRLatchElectricElement : RotateableElectricElement {
        public bool m_setAllowed = true;

        public bool m_resetAllowed = true;

        public bool m_clockAllowed = true;

        public float m_voltage;

        public SRLatchElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) {
            float? num = subsystemElectricity.ReadPersistentVoltage(cellFace.Point);
            if (num.HasValue) {
                m_voltage = num.Value;
            }
        }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
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
                        if (connectorDirection == ElectricConnectorDirection.Right) {
                            flag2 = IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                        else if (connectorDirection == ElectricConnectorDirection.Left) {
                            flag = IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                        else if (connectorDirection == ElectricConnectorDirection.Bottom) {
                            flag3 = IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                            flag4 = true;
                        }
                    }
                }
            }
            if (flag4) {
                if (flag3 && m_clockAllowed) {
                    m_clockAllowed = false;
                    if (flag && flag2) {
                        m_voltage = !IsSignalHigh(m_voltage) ? 1 : 0;
                    }
                    else if (flag) {
                        m_voltage = 1f;
                    }
                    else if (flag2) {
                        m_voltage = 0f;
                    }
                }
            }
            else if (flag && m_setAllowed) {
                m_setAllowed = false;
                m_voltage = 1f;
            }
            else if (flag2 && m_resetAllowed) {
                m_resetAllowed = false;
                m_voltage = 0f;
            }
            if (!flag3) {
                m_clockAllowed = true;
            }
            if (!flag) {
                m_setAllowed = true;
            }
            if (!flag2) {
                m_resetAllowed = true;
            }
            if (m_voltage != voltage) {
                SubsystemElectricity.WritePersistentVoltage(CellFaces[0].Point, m_voltage);
                return true;
            }
            return false;
        }
    }
}