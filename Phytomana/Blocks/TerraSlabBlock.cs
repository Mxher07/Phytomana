using Engine.Core;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 泰拉合成毯：独立渲染的薄板方块（CubeBlock 系，与地毯染色体系完全解耦，不可染色）。
    /// 结构校验与合成逻辑见 SubsystemTerraSlabBehavior。
    /// 渲染按 data bit0：0 = 结构正确（原色），1 = 结构无效（代码染红）；bit0 由行为子系统翻转。
    /// </summary>
    public class TerraSlabBlock : PhytoCubeBlock {
    public static int Index = 1145;
        /// <summary>薄板厚度（1/16 格高，与地毯一致）。</summary>
        public const float Thickness = 0.0625f;


        public BoundingBox[] m_collisionBoxes = [new(new Vector3(0, 0, 0), new Vector3(1f, Thickness, 1f))];

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/TerraSlab");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain subsystemTerrain, int value) => m_collisionBoxes;

        /// <summary>薄板顶点：顶面 1/16 高，全部套 TerraSlab 贴图；结构无效时顶面染红。</summary>
        public override void GenerateTerrainVertices(BlockGeometryGenerator generator,
            TerrainGeometry geometry,
            int value,
            int x,
            int y,
            int z) {
            Color tint = IsStructureInvalid(value)
                ? new Color(255, 120, 120)
                : Color.White;
            TerrainGeometrySubset[] subsets = m_texture == null
                ? geometry.OpaqueSubsetsByFace
                : geometry.GetGeometry(m_texture).OpaqueSubsetsByFace;
            generator.GenerateCubeVertices(
                this,
                value,
                x,
                y,
                z,
                0f,
                0f,
                0f,
                Thickness,
                tint,
                tint,
                tint,
                tint,
                tint,
                -1,
                subsets
            );
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            if (IsStructureInvalid(value)) {
                color *= new Color(255, 120, 120);
            }
            // 新 API 无「高度 + 贴图」的 DrawCubeBlock 重载：以 Y 轴缩放矩阵复现薄板厚度，
            // 几何与旧高度参数版本等价（两者都绕中心缩放），且保留独立贴图。
            Matrix slabMatrix = Matrix.CreateScale(1f, Thickness, 1f) * matrix;
            BlocksManager.DrawCubeBlock(
                primitivesRenderer,
                value,
                new Vector3(size),
                ref slabMatrix,
                color,
                color,
                environmentData,
                m_texture
            );
        }

        /// <summary>结构无效位（data bit0）：行为子系统按结构校验结果翻转，渲染层据此染红。</summary>
        public static bool IsStructureInvalid(int value) => (Terrain.ExtractData(value) & 1) != 0;
    }
}
