using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 魔力钻石：钻石在魔法池中注入 400mn 魔力凝成，符文台高阶配方的核心材料（物品）。
    /// 渲染与原版钻石晶体一致（Models/Diamond 晶体模型），贴图使用魔力钢锭材质。
    /// </summary>
    public class ManaDiamondChunkBlock : Block {
        public BlockMesh m_standaloneBlockMesh = new();

        public override void Initialize() {
            Model model = ContentManager.Get<Model>("Models/Diamond");
            Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Diamond").ParentBone);
            m_standaloneBlockMesh.AppendModelMeshPart(
                model.FindMesh("Diamond").MeshParts[0],
                boneAbsoluteTransform * Matrix.CreateTranslation(0f, 0f, 0f),
                false,
                false,
                false,
                false,
                Color.White
            );
            base.Initialize();
        }

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) { }

        public override Texture2D GetDefaultTexture(int value) => ContentManager.Get<Texture2D>("Textures/PhytoMana/ManaIngot");

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, GetDefaultTexture(value), color, 2f * size, ref matrix, environmentData);
        }
    }
}