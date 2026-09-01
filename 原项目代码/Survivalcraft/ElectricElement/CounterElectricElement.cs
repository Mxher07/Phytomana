namespace Game {
    public class CounterElectricElement : RotateableElectricElement {
        public bool m_plusAllowed = true;

        public bool m_minusAllowed = true;

        public bool m_resetAllowed = true;

        public int m_counter;

        public bool m_overflow;

        public CounterElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) {
            float? num = subsystemElectricity.ReadPersistentVoltage(cellFace.Point);
            if (num.HasValue) {
                m_counter = (int)MathF.Round(MathF.Abs(num.Value) * 15f);
                m_overflow = num.Value < 0f;
            }
        }

        public override float GetOutputVoltage(int face) {
            ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(CellFaces[0].Face, Rotation, face);
            if (connectorDirection.HasValue) {
                if (connectorDirection.Value == ElectricConnectorDirection.Top) {
                    return m_counter / 15f;
                }
                if (connectorDirection.Value == ElectricConnectorDirection.Bottom) {
                    return m_overflow ? 1 : 0;
                }
            }
            return 0f;
        }

        public override bool Simulate() {
            int counter = m_counter;
            bool overflow = m_overflow;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
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
                            flag = IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                        else if (connectorDirection == ElectricConnectorDirection.Left) {
                            flag2 = IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                        else if (connectorDirection == ElectricConnectorDirection.In) {
                            flag3 = IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                        }
                    }
                }
            }
            if (flag && m_plusAllowed) {
                m_plusAllowed = false;
                if (m_counter < 15) {
                    m_counter++;
                    m_overflow = false;
                }
                else {
                    m_counter = 0;
                    m_overflow = true;
                }
            }
            else if (flag2 && m_minusAllowed) {
                m_minusAllowed = false;
                if (m_counter > 0) {
                    m_counter--;
                    m_overflow = false;
                }
                else {
                    m_counter = 15;
                    m_overflow = true;
                }
            }
            else if (flag3 && m_resetAllowed) {
                m_counter = 0;
                m_overflow = false;
            }
            if (!flag) {
                m_plusAllowed = true;
            }
            if (!flag2) {
                m_minusAllowed = true;
            }
            if (!flag3) {
                m_resetAllowed = true;
            }
            if (m_counter != counter
                || m_overflow != overflow) {
                SubsystemElectricity.WritePersistentVoltage(CellFaces[0].Point, m_counter / 15f * (!m_overflow ? 1 : -1));
                return true;
            }
            return false;
        }
    }
}