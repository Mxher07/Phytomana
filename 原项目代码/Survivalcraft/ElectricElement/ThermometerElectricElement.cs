using Engine;

namespace Game {
    public class ThermometerElectricElement : ElectricElement {
        public SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;

        public float m_voltage;

        public const float m_pollingPeriod = 0.5f;

        public ThermometerElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) =>
            m_subsystemMetersBlockBehavior = SubsystemElectricity.Project.FindSubsystem<SubsystemMetersBlockBehavior>(true);

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            CellFace cellFace = CellFaces[0];
            m_voltage = MathUtils.Saturate(m_subsystemMetersBlockBehavior.GetThermometerReading(cellFace.X, cellFace.Y, cellFace.Z) / 15f);
            float num = m_pollingPeriod * (0.9f + 0.0002f * (GetHashCode() % 1000));
            SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + MathUtils.Max((int)(num / 0.01f), 1));
            return m_voltage != voltage;
        }
    }
}