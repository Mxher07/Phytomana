using Engine;

namespace Game {
    public class PhotodiodeElectricElement : MountedElectricElement {
        public float m_voltage;

        public PhotodiodeElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) =>
            m_voltage = CalculateVoltage();

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            m_voltage = CalculateVoltage();
            SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + MathUtils.Max(50, 1));
            return m_voltage != voltage;
        }

        public float CalculateVoltage() {
            CellFace cellFace = CellFaces[0];
            Point3 point = CellFace.FaceToPoint3(cellFace.Face);
            int cellLight = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellLight(cellFace.X, cellFace.Y, cellFace.Z);
            int cellLight2 = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellLight(
                cellFace.X + point.X,
                cellFace.Y + point.Y,
                cellFace.Z + point.Z
            );
            return MathUtils.Max(cellLight, cellLight2) / 15f;
        }
    }
}