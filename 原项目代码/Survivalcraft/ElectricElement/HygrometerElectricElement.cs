namespace Game {
    public class HygrometerElectricElement : ElectricElement {
        public float m_voltage;

        public HygrometerElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            CellFace cellFace = CellFaces[0];
            int humidity = SubsystemElectricity.SubsystemTerrain.Terrain.GetHumidity(cellFace.X, cellFace.Z);
            m_voltage = humidity / 15f;
            return m_voltage != voltage;
        }
    }
}