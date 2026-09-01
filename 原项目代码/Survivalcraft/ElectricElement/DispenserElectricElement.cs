using Engine;

namespace Game {
    public class DispenserElectricElement : ElectricElement {
        public bool m_isDispenseAllowed = true;

        public double? m_lastDispenseTime;

        public SubsystemBlockEntities m_subsystemBlockEntities;

        public DispenserElectricElement(SubsystemElectricity subsystemElectricity, Point3 point) : base(
            subsystemElectricity,
            new List<CellFace> {
                new(point.X, point.Y, point.Z, 0),
                new(point.X, point.Y, point.Z, 1),
                new(point.X, point.Y, point.Z, 2),
                new(point.X, point.Y, point.Z, 3),
                new(point.X, point.Y, point.Z, 4),
                new(point.X, point.Y, point.Z, 5)
            }
        ) => m_subsystemBlockEntities = SubsystemElectricity.Project.FindSubsystem<SubsystemBlockEntities>(true);

        public override bool Simulate() {
            if (CalculateHighInputsCount() > 0) {
                if (m_isDispenseAllowed && (!m_lastDispenseTime.HasValue || SubsystemElectricity.SubsystemTime.GameTime - m_lastDispenseTime > 0.1)) {
                    m_isDispenseAllowed = false;
                    m_lastDispenseTime = SubsystemElectricity.SubsystemTime.GameTime;
                    m_subsystemBlockEntities.GetBlockEntity(CellFaces[0].Point.X, CellFaces[0].Point.Y, CellFaces[0].Point.Z)
                        ?.Entity.FindComponent<ComponentDispenser>()
                        ?.Dispense();
                }
            }
            else {
                m_isDispenseAllowed = true;
            }
            return false;
        }
    }
}