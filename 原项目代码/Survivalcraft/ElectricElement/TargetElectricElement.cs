namespace Game {
    public class TargetElectricElement : MountedElectricElement {
        public float m_voltage;

        public int m_score;

        public TargetElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool Simulate() {
            float voltage = m_voltage;
            if (m_score > 0) {
                m_voltage = (m_score + 7) / 15f;
                m_score = 0;
                SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 50);
            }
            else {
                m_voltage = 0f;
            }
            return m_voltage != voltage;
        }

        public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) {
            if (m_score == 0
                && !IsSignalHigh(m_voltage)) {
                if (cellFace.Face == 0
                    || cellFace.Face == 2) {
                    float num = worldItem.Position.X - cellFace.X - 0.5f;
                    float num2 = worldItem.Position.Y - cellFace.Y - 0.5f;
                    float num3 = MathF.Sqrt(num * num + num2 * num2);
                    m_score = Math.Clamp((int)MathF.Round(8f * (1f - num3 / 0.707f)), 1, 8);
                }
                else {
                    float num4 = worldItem.Position.Z - cellFace.Z - 0.5f;
                    float num5 = worldItem.Position.Y - cellFace.Y - 0.5f;
                    float num6 = MathF.Sqrt(num4 * num4 + num5 * num5);
                    m_score = Math.Clamp((int)MathF.Round(8f * (1f - num6 / 0.5f)), 1, 8);
                }
                SubsystemElectricity.QueueElectricElementForSimulation(this, SubsystemElectricity.CircuitStep + 1);
            }
        }
    }
}