namespace Game {
    public class AdjustableDelayGateElectricElement : BaseDelayGateElectricElement {
        public int m_delaySteps;

        public override int DelaySteps => m_delaySteps;

        public AdjustableDelayGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) :
            base(subsystemElectricity, cellFace) {
            int data = Terrain.ExtractData(subsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z));
            m_delaySteps = AdjustableDelayGateBlock.GetDelay(data);
        }
    }
}