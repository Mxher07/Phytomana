using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 泰拉合成毯：独立渲染的薄板方块（CubeBlock 系，与地毯染色体系完全解耦，不可染色）。
    /// 结构校验与合成逻辑见 SubsystemTerraSlabBehavior。
    /// 渲染按 data bit0：0 = 结构正确（原色），1 = 结构无效（代码染红）；bit0 由行为子系统翻转。
    /// </summary>
    public class TerraSlabBlock : CubeBlock {
        /// <summary>薄板厚度（1/16 格高，与地毯一致）。</summary>
        public const float Thickness = 0.0625f;

        public Texture2D m_texture;

        public BoundingBox[] m_collisionBoxes = [new(new Vector3(0, 0, 0), new Vector3(1f, Thickness, 1f))];

        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/TerraSlab");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;

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
            BlocksManager.DrawCubeBlock(
                primitivesRenderer,
                value,
                new Vector3(size),
                Thickness,
                ref matrix,
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
