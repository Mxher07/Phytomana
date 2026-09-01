using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class WoodBlock : CubeBlock {
        public int m_cutTextureSlot;

        public int m_sideTextureSlot;

        public WoodBlock(int cutTextureSlot, int sideTextureSlot) {
            m_cutTextureSlot = cutTextureSlot;
            m_sideTextureSlot = sideTextureSlot;
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            TerrainGeometrySubset[] subsets = texture == null ? geometry.OpaqueSubsetsByFace : geometry.GetGeometry(texture).OpaqueSubsetsByFace;
            switch (GetCutFace(Terrain.ExtractData(value))) {
                case 4:
                    generator.GenerateCubeVertices(
                        this,
                        value,
                        x,
                        y,
                        z,
                        Color.White,
                        subsets
                    ); break;
                case 0:
                    generator.GenerateCubeVertices(
                        this,
                        value,
                        x,
                        y,
                        z,
                        1,
                        0,
                        0,
                        Color.White,
                        subsets
                    ); break;
                default:
                    generator.GenerateCubeVertices(
                        this,
                        value,
                        x,
                        y,
                        z,
                        0,
                        1,
                        1,
                        Color.White,
                        subsets
                    ); break;
            }
        }

        public override int GetFaceTextureSlot(int face, int value) {
            int cutFace = GetCutFace(Terrain.ExtractData(value));
            if (cutFace == face
                || CellFace.OppositeFace(cutFace) == face) {
                return m_cutTextureSlot;
            }
            return m_sideTextureSlot;
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
            float num = float.NegativeInfinity;
            int cutFace = 0;
            for (int i = 0; i < 6; i++) {
                float num2 = Vector3.Dot(CellFace.FaceToVector3(i), forward);
                if (num2 > num) {
                    num = num2;
                    cutFace = i;
                }
            }
            BlockPlacementData result = default;
            result.Value = Terrain.MakeBlockValue(BlockIndex, 0, SetCutFace(0, cutFace));
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            if (toolLevel >= GetRequiredToolLevel(oldValue)) {
                int data = Terrain.ExtractData(oldValue);
                data = SetCutFace(data, 4);
                dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(BlockIndex, 0, data), Count = 1 });
            }
            showDebris = true;
        }

        public static int GetCutFace(int data) {
            data &= 3;
            switch (data) {
                case 0: return 4;
                case 1: return 0;
                default: return 1;
            }
        }

        public static int SetCutFace(int data, int cutFace) {
            data &= -4;
            switch (cutFace) {
                case 0:
                case 2: return data | 1;
                case 1:
                case 3: return data | 2;
                default: return data;
            }
        }
    }
}