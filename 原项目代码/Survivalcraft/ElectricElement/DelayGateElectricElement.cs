namespace Game {
    public class DelayGateElectricElement : BaseDelayGateElectricElement {
        public int? m_delaySteps;

        public int m_lastDelayCalculationStep;

        public static int[] m_delaysByPredecessorsCount = [20, 80, 400];

        public override int DelaySteps {
            get {
                if (SubsystemElectricity.CircuitStep - m_lastDelayCalculationStep > 50) {
                    m_delaySteps = null;
                }
                if (!m_delaySteps.HasValue) {
                    int count = 0;
                    CountDelayPredecessors(this, ref count);
                    m_delaySteps = m_delaysByPredecessorsCount[count];
                    m_lastDelayCalculationStep = SubsystemElectricity.CircuitStep;
                }
                return m_delaySteps.Value;
            }
        }

        public DelayGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public static void CountDelayPredecessors(DelayGateElectricElement delayGate, ref int count) {
            if (count < 2) {
                foreach (ElectricConnection connection in delayGate.Connections) {
                    if (connection.ConnectorType == ElectricConnectorType.Input) {
                        if (connection.NeighborElectricElement is DelayGateElectricElement delayGateElectricElement) {
                            count++;
                            CountDelayPredecessors(delayGateElectricElement, ref count);
                            break;
                        }
                    }
                }
            }
        }
    }
}