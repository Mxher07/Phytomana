using Engine;

namespace Game {
    public class PistonElectricElement : ElectricElement {
        public int m_lastLength = -1;

        public PistonElectricElement(SubsystemElectricity subsystemElectricity, Point3 point) : base(
            subsystemElectricity,
            new List<CellFace> {
                new(point.X, point.Y, point.Z, 0),
                new(point.X, point.Y, point.Z, 1),
                new(point.X, point.Y, point.Z, 2),
                new(point.X, point.Y, point.Z, 3),
                new(point.X, point.Y, point.Z, 4),
                new(point.X, point.Y, point.Z, 5)
            }
        ) { }

        public override bool Simulate() {
            float num = 0f;
            foreach (ElectricConnection connection in Connections) {
                if (connection.ConnectorType != ElectricConnectorType.Output
                    && connection.NeighborConnectorType != 0) {
                    num = MathUtils.Max(num, connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
                }
            }
            int num2 = MathUtils.Max((int)(num * 15.999f) - 7, 0);
            if (num2 != m_lastLength) {
                m_lastLength = num2;
                SubsystemElectricity.Project.FindSubsystem<SubsystemPistonBlockBehavior>(true).AdjustPiston(CellFaces[0].Point, num2);
            }
            return false;
        }
    }
}