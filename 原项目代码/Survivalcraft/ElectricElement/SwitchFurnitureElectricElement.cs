using Engine;

namespace Game {
    public class SwitchFurnitureElectricElement : FurnitureElectricElement {
        public float m_voltage;

        public SwitchFurnitureElectricElement(SubsystemElectricity subsystemElectricity, Point3 point, int value) :
            base(subsystemElectricity, point) {
            FurnitureDesign design = FurnitureBlock.GetDesign(subsystemElectricity.SubsystemTerrain.SubsystemFurnitureBlockBehavior, value);
            if (design != null
                && design.LinkedDesign != null) {
                m_voltage = design.Index >= design.LinkedDesign.Index ? 1 : 0;
            }
        }

        public override float GetOutputVoltage(int face) => m_voltage;

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            CellFace cellFace = CellFaces[0];
            SubsystemElectricity.SubsystemTerrain.SubsystemFurnitureBlockBehavior.SwitchToNextState(cellFace.X, cellFace.Y, cellFace.Z, false);
            SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f, true);
            return true;
        }
    }
}