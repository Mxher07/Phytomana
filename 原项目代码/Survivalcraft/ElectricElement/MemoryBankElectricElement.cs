namespace Game {
    public class MemoryBankElectricElement : RotateableElectricElement {
        public SubsystemMemoryBankBlockBehavior m_subsystemMemoryBankBlockBehavior;

        public float m_voltage;

        public bool m_writeAllowed;

        public bool m_clockAllowed;

        public MemoryBankElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) {
            m_subsystemMemoryBankBlockBehavior = subsystemElectricity.Project.FindSubsystem<SubsystemMemoryBankBlockBehavior>(true);
            MemoryBankData blockData = m_subsystemMemoryBankBlockBehavior.GetBlockData(cellFace.Point);
            if (blockData != null) {
                m_voltage = blockData.LastOutput / 15f;
            }
        }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            float num = 0f;
            int num2 = 0;
            int num3 = 0;
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
                            num2 = (int)MathF.Round(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace) * 15f);
                        }
                        else if (connectorDirection == ElectricConnectorDirection.Left) {
                            num3 = (int)MathF.Round(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace) * 15f);
                        }
                        else if (connectorDirection == ElectricConnectorDirection.Bottom) {
                            int num4 = (int)MathF.Round(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace) * 15f);
                            flag = num4 >= 8;
                            flag3 = num4 > 0 && num4 < 8;
                            flag2 = true;
                        }
                        else if (connectorDirection == ElectricConnectorDirection.In) {
                            num = connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace);
                        }
                    }
                }
            }
            MemoryBankData memoryBankData = m_subsystemMemoryBankBlockBehavior.GetBlockData(CellFaces[0].Point);
            int address = num2 + (num3 << 4);
            if (flag2) {
                if (flag && m_clockAllowed) {
                    m_clockAllowed = false;
                    m_voltage = memoryBankData != null ? memoryBankData.Read(address) / 15f : 0f;
                }
                else if (flag3 && m_writeAllowed) {
                    m_writeAllowed = false;
                    if (memoryBankData == null) {
                        memoryBankData = new MemoryBankData();
                        m_subsystemMemoryBankBlockBehavior.SetBlockData(CellFaces[0].Point, memoryBankData);
                    }
                    memoryBankData.Write(address, (byte)MathF.Round(num * 15f));
                }
            }
            else {
                m_voltage = memoryBankData != null ? memoryBankData.Read(address) / 15f : 0f;
            }
            if (!flag) {
                m_clockAllowed = true;
            }
            if (!flag3) {
                m_writeAllowed = true;
            }
            if (memoryBankData != null) {
                memoryBankData.LastOutput = (byte)MathF.Round(m_voltage * 15f);
            }
            return m_voltage != voltage;
        }
    }
}