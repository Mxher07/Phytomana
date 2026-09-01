namespace Game {
    public class ChristmasTreeElectricElement : ElectricElement {
        public int m_lastChangeCircuitStep;

        public float m_voltage;

        public ChristmasTreeElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace, int value) : base(
            subsystemElectricity,
            cellFace
        ) {
            m_lastChangeCircuitStep = SubsystemElectricity.CircuitStep;
            m_voltage = ChristmasTreeBlock.GetLightState(Terrain.ExtractData(value)) ? 1 : 0;
        }

        public override bool Simulate() {
            int num = SubsystemElectricity.CircuitStep - m_lastChangeCircuitStep;
            float voltage = CalculateHighInputsCount() > 0 ? 1 : 0;
            if (IsSignalHigh(voltage) != IsSignalHigh(m_voltage)) {
                m_lastChangeCircuitStep = SubsystemElectricity.CircuitStep;
            }
            m_voltage = voltage;
            if (num >= 10) {
                CellFace cellFace = CellFaces[0];
                int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
                int data = ChristmasTreeBlock.SetLightState(Terrain.ExtractData(cellValue), IsSignalHigh(m_voltage));
                int value = Terrain.ReplaceData(cellValue, data);
                SubsystemElectricity.SubsystemTerrain.ChangeCell(cellFace.X, cellFace.Y, cellFace.Z, value);
            }
            else {
                SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 10 - num);
            }
            return false;
        }
    }
}