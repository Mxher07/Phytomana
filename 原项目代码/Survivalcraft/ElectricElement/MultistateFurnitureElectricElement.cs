using Engine;

namespace Game {
    public class MultistateFurnitureElectricElement : FurnitureElectricElement {
        public bool m_isActionAllowed;

        public double? m_lastActionTime;

        public MultistateFurnitureElectricElement(SubsystemElectricity subsystemElectricity, Point3 point) : base(subsystemElectricity, point) { }

        public override bool Simulate() {
            if (CalculateHighInputsCount() > 0) {
                if (m_isActionAllowed && (!m_lastActionTime.HasValue || SubsystemElectricity.SubsystemTime.GameTime - m_lastActionTime > 0.1)) {
                    m_isActionAllowed = false;
                    m_lastActionTime = SubsystemElectricity.SubsystemTime.GameTime;
                    SubsystemElectricity.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(true)
                        .SwitchToNextState(CellFaces[0].X, CellFaces[0].Y, CellFaces[0].Z, false);
                }
            }
            else {
                m_isActionAllowed = true;
            }
            return false;
        }
    }
}