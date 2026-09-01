namespace Game {
    public class DoorElectricElement : ElectricElement {
        public int m_lastChangeCircuitStep;

        public bool m_needsReset;

        public float m_voltage;

        public DoorElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) {
            m_lastChangeCircuitStep = SubsystemElectricity.CircuitStep;
            m_needsReset = true;
        }

        public override bool Simulate() {
            int num = SubsystemElectricity.CircuitStep - m_lastChangeCircuitStep;
            float voltage = CalculateHighInputsCount() > 0 ? 1 : 0;
            if (IsSignalHigh(voltage) != IsSignalHigh(m_voltage)) {
                m_lastChangeCircuitStep = SubsystemElectricity.CircuitStep;
            }
            m_voltage = voltage;
            if (!IsSignalHigh(m_voltage)) {
                m_needsReset = false;
            }
            if (!m_needsReset) {
                if (num >= 10) {
                    if (IsSignalHigh(m_voltage)) {
                        CellFace cellFace = CellFaces[0];
                        int data = Terrain.ExtractData(
                            SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z)
                        );
                        SubsystemElectricity.Project.FindSubsystem<SubsystemDoorBlockBehavior>(true)
                            .OpenCloseDoor(cellFace.X, cellFace.Y, cellFace.Z, !DoorBlock.GetOpen(data));
                    }
                }
                else {
                    SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 10 - num);
                }
            }
            return false;
        }
    }
}