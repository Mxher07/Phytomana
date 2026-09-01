using Engine;

namespace Game {
    public class RealTimeClockElectricElement : RotateableElectricElement {
        public SubsystemTimeOfDay m_subsystemTimeOfDay;

        public int m_lastClockValue = -1;

        public const int m_periodsPerDay = 4096;

        public RealTimeClockElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) =>
            m_subsystemTimeOfDay = SubsystemElectricity.Project.FindSubsystem<SubsystemTimeOfDay>(true);

        public override float GetOutputVoltage(int face) {
            ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(CellFaces[0].Face, Rotation, face);
            if (connectorDirection.HasValue) {
                if (connectorDirection.Value == ElectricConnectorDirection.Top) {
                    return (GetClockValue() & 0xF) / 15f;
                }
                if (connectorDirection.Value == ElectricConnectorDirection.Right) {
                    return ((GetClockValue() >> 4) & 0xF) / 15f;
                }
                if (connectorDirection.Value == ElectricConnectorDirection.Bottom) {
                    return ((GetClockValue() >> 8) & 0xF) / 15f;
                }
                if (connectorDirection.Value == ElectricConnectorDirection.Left) {
                    return ((GetClockValue() >> 12) & 0xF) / 15f;
                }
                if (connectorDirection.Value == ElectricConnectorDirection.In) {
                    return ((GetClockValue() >> 16) & 0xF) / 15f;
                }
            }
            return 0f;
        }

        public override bool Simulate() {
            double day = m_subsystemTimeOfDay.Day;
            int num = (int)(((Math.Ceiling(day * 4096.0) + 0.5) / 4096.0 - day) * m_subsystemTimeOfDay.DayDuration / 0.0099999997764825821);
            int circuitStep = MathUtils.Max(SubsystemElectricity.FrameStartCircuitStep + num, SubsystemElectricity.CircuitStep + 1);
            SubsystemElectricity.QueueElectricElementForSimulation(this, circuitStep);
            int clockValue = GetClockValue();
            if (clockValue != m_lastClockValue) {
                m_lastClockValue = clockValue;
                return true;
            }
            return false;
        }

        public int GetClockValue() => (int)(m_subsystemTimeOfDay.Day * 4096.0);
    }
}