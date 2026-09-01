using Engine;

namespace Game {
    public class ChestBlock : CubeBlock {
        public static int Index = 45;

        public override int GetFaceTextureSlot(int face, int value) {
            return face switch {
                4 => 42,
                5 => 42,
                _ => Terrain.ExtractData(value) switch {
                    0 => face switch {
                        0 => 27,
                        2 => 26,
                        _ => 25
                    },
                    1 => face switch {
                        1 => 27,
                        3 => 26,
                        _ => 25
                    },
                    2 => face switch {
                        2 => 27,
                        0 => 26,
                        _ => 25
                    },
                    _ => face switch {
                        3 => 27,
                        1 => 26,
                        _ => 25
                    }
                }
            };
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            Vector3 forward = Matrix.CreateFromQuaternion(componentMiner.ComponentCreature.ComponentCreatureModel.EyeRotation).Forward;
            float num = Vector3.Dot(forward, Vector3.UnitZ);
            float num2 = Vector3.Dot(forward, Vector3.UnitX);
            float num3 = Vector3.Dot(forward, -Vector3.UnitZ);
            float num4 = Vector3.Dot(forward, -Vector3.UnitX);
            int data = num == MathUtils.Max(num, num2, num3, num4) ? 2 :
                num2 == MathUtils.Max(num2, num3, num4) ? 3 :
                num3 == Math.Max(num3, num4) ? 0 : 1;
            BlockPlacementData result = default;
            result.Value = Terrain.ReplaceData(Terrain.ReplaceContents(45), data);
            result.CellFace = raycastResult.CellFace;
            return result;
        }
    }
}