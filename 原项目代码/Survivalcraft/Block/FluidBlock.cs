using System.Reflection;
using Engine;

namespace Game {
    public abstract class FluidBlock : CubeBlock {
        public float[] m_heightByLevel = new float[16];

        public BoundingBox[][] m_boundingBoxesByLevel = new BoundingBox[16][];

        public bool[] m_theSameFluidsByIndex;

        public readonly int MaxLevel;

        public FluidBlock(int maxLevel) {
            MaxLevel = maxLevel;
            for (int i = 0; i < 16; i++) {
                float num = 0.875f * MathUtils.Saturate(1f - i / (float)MaxLevel);
                m_heightByLevel[i] = num;
                m_boundingBoxesByLevel[i] = [new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, num, 1f))];
            }
        }

        public override void Initialize() {
            base.Initialize();
            TypeInfo typeInfo = null;
            TypeInfo typeInfo2 = GetType().GetTypeInfo();
            while (typeInfo2 != null) {
                if (typeInfo2.BaseType == typeof(FluidBlock)) {
                    typeInfo = typeInfo2;
                    break;
                }
                typeInfo2 = typeInfo2.BaseType.GetTypeInfo();
            }
            if (typeInfo == null) {
                throw new InvalidOperationException("Fluid type not found.");
            }
            m_theSameFluidsByIndex = new bool[BlocksManager.Blocks.Length];
            for (int i = 0; i < BlocksManager.Blocks.Length; i++) {
                Block block = BlocksManager.Blocks[i];
                m_theSameFluidsByIndex[i] = block.GetType().GetTypeInfo() == typeInfo
                    || block.GetType().GetTypeInfo().IsSubclassOf(typeInfo.AsType());
            }
        }

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) =>
            m_boundingBoxesByLevel[GetLevel(Terrain.ExtractData(value))];

        public bool IsTheSameFluid(int contents) => m_theSameFluidsByIndex[contents];

        public float GetLevelHeight(int level) => m_heightByLevel[level];

        public void GenerateFluidTerrainVertices(BlockGeometryGenerator generator,
            int value,
            int x,
            int y,
            int z,
            Color sideColor,
            Color topColor,
            TerrainGeometrySubset[] subset) {
            int data = Terrain.ExtractData(value);
            if (GetIsTop(data)) {
                Terrain terrain = generator.Terrain;
                int cellValueFast = terrain.GetCellValueFast(x - 1, y, z - 1);
                int cellValueFast2 = terrain.GetCellValueFast(x, y, z - 1);
                int cellValueFast3 = terrain.GetCellValueFast(x + 1, y, z - 1);
                int cellValueFast4 = terrain.GetCellValueFast(x - 1, y, z);
                int cellValueFast5 = terrain.GetCellValueFast(x + 1, y, z);
                int cellValueFast6 = terrain.GetCellValueFast(x - 1, y, z + 1);
                int cellValueFast7 = terrain.GetCellValueFast(x, y, z + 1);
                int cellValueFast8 = terrain.GetCellValueFast(x + 1, y, z + 1);
                float h = CalculateNeighborHeight(cellValueFast);
                float num = CalculateNeighborHeight(cellValueFast2);
                float h2 = CalculateNeighborHeight(cellValueFast3);
                float num2 = CalculateNeighborHeight(cellValueFast4);
                float num3 = CalculateNeighborHeight(cellValueFast5);
                float h3 = CalculateNeighborHeight(cellValueFast6);
                float num4 = CalculateNeighborHeight(cellValueFast7);
                float h4 = CalculateNeighborHeight(cellValueFast8);
                float levelHeight = GetLevelHeight(GetLevel(data));
                float height = CalculateFluidVertexHeight(h, num, num2, levelHeight);
                float height2 = CalculateFluidVertexHeight(num, h2, levelHeight, num3);
                float height3 = CalculateFluidVertexHeight(levelHeight, num3, num4, h4);
                float height4 = CalculateFluidVertexHeight(num2, levelHeight, h3, num4);
                float x2 = ZeroSubst(num3, levelHeight) - ZeroSubst(num2, levelHeight);
                float x3 = ZeroSubst(num4, levelHeight) - ZeroSubst(num, levelHeight);
                int overrideTopTextureSlot = DefaultTextureSlot - MathF.Sign(x2) - 16 * Math.Sign(x3);
                generator.GenerateCubeVertices(
                    this,
                    value,
                    x,
                    y,
                    z,
                    height,
                    height2,
                    height3,
                    height4,
                    sideColor,
                    topColor,
                    topColor,
                    topColor,
                    topColor,
                    overrideTopTextureSlot,
                    subset
                );
            }
            else {
                generator.GenerateCubeVertices(
                    this,
                    value,
                    x,
                    y,
                    z,
                    sideColor,
                    subset
                );
            }
        }

        public static float ZeroSubst(float v, float subst) {
            if (v != 0f) {
                return v;
            }
            return subst;
        }

        public static float CalculateFluidVertexHeight(float h1, float h2, float h3, float h4) {
            float num = MathUtils.Max(h1, h2, h3, h4);
            if (num < 1f) {
                if (h1 == 0.01f
                    || h2 == 0.01f
                    || h3 == 0.01f
                    || h4 == 0.01f) {
                    return 0f;
                }
                return num;
            }
            return 1f;
        }

        public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            BlockPlacementData result = default;
            result.Value = Terrain.ReplaceData(Terrain.ReplaceContents(BlockIndex), 0);
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public override int GetFaceTextureSlot(int face, int value) {
            if (face >= 4) {
                return DefaultTextureSlot;
            }
            return DefaultTextureSlot + 16;
        }

        public override bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain,
            int face,
            int value,
            int neighborValue,
            int x,
            int y,
            int z) {
            int contents = Terrain.ExtractContents(neighborValue);
            if (IsTheSameFluid(contents)) {
                return false;
            }
            return base.ShouldGenerateFace(
                subsystemTerrain,
                face,
                value,
                neighborValue,
                x,
                y,
                z
            );
        }

        public float CalculateNeighborHeight(int value) {
            int num = Terrain.ExtractContents(value);
            if (IsTheSameFluid(num)) {
                int data = Terrain.ExtractData(value);
                if (GetIsTop(data)) {
                    return GetLevelHeight(GetLevel(data));
                }
                return 1f;
            }
            if (num == 0) {
                return 0.01f;
            }
            return 0f;
        }

        public override bool IsHeatBlocker(int value) => true;

        public static int GetLevel(int data) => data & 0xF;

        public static int SetLevel(int data, int level) => (data & -16) | (level & 0xF);

        public static bool GetIsTop(int data) => (data & 0x10) != 0;

        public static int SetIsTop(int data, bool isTop) {
            if (!isTop) {
                return data & -17;
            }
            return data | 0x10;
        }

        public override bool IsCollapseDestructibleBlock(int value) => false;
    }
}