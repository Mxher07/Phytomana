using Engine;

namespace Game {
    public class LightBulbElectricElement : MountedElectricElement {
        public int m_intensity;

        public int m_lastChangeCircuitStep;

        public LightBulbElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace, int value) : base(
            subsystemElectricity,
            cellFace
        ) {
            m_lastChangeCircuitStep = SubsystemElectricity.CircuitStep;
            int data = Terrain.ExtractData(value);
            m_intensity = LightbulbBlock.GetLightIntensity(data);
        }

        public override bool Simulate() {
            int num = SubsystemElectricity.CircuitStep - m_lastChangeCircuitStep;
            float num2 = 0f;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    num2 = MathUtils.Max(num2, connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                }
            }
            int intensity = m_intensity;
            m_intensity = Math.Clamp((int)MathF.Round((num2 - 0.5f) * 30f), 0, 15);
            if (m_intensity != intensity) {
                m_lastChangeCircuitStep = SubsystemElectricity.CircuitStep;
            }
            if (num >= 10) {
                CellFace cellFace = CellFaces[0];
                int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
                int data = LightbulbBlock.SetLightIntensity(Terrain.ExtractData(cellValue), m_intensity);
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