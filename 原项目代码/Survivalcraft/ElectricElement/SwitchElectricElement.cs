using Engine;

namespace Game {
    public class SwitchElectricElement : MountedElectricElement {
        public float m_voltage;

        public SwitchElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace, int value) : base(subsystemElectricity, cellFace) {
            int voltageLevel = SwitchBlock.GetVoltageLevel(Terrain.ExtractData(value));
            m_voltage = SwitchBlock.GetLeverState(value) ? voltageLevel / 15f : 0f;
        }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            CellFace cellFace = CellFaces[0];
            int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
            int value = SwitchBlock.SetLeverState(cellValue, !SwitchBlock.GetLeverState(cellValue));
            SubsystemElectricity.SubsystemTerrain.ChangeCell(cellFace.X, cellFace.Y, cellFace.Z, value);
            SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f, true);
            return true;
        }
    }
}