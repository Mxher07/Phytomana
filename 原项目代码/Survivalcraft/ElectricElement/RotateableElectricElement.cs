using Engine;

namespace Game {
    public abstract class RotateableElectricElement : MountedElectricElement {
        public int Rotation {
            get {
                CellFace cellFace = CellFaces[0];
                return RotateableMountedElectricElementBlock.GetRotation(
                    Terrain.ExtractData(SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z))
                );
            }
            set {
                CellFace cellFace = CellFaces[0];
                int cellValue = SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
                int value2 = Terrain.ReplaceData(
                    cellValue,
                    RotateableMountedElectricElementBlock.SetRotation(Terrain.ExtractData(cellValue), value % 4)
                );
                SubsystemElectricity.SubsystemTerrain.ChangeCell(cellFace.X, cellFace.Y, cellFace.Z, value2);
                SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f, true);
            }
        }

        public RotateableElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace) : base(subsystemElectricity, cellFace) { }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            ++Rotation;
            return true;
        }
    }
}