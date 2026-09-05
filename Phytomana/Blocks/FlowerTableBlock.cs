using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 花药台方块：石质台座 + 可显示的水面网格，合成行为见 SubsystemFlowerTableBehavior。
    /// </summary>
    public class FlowerTableBlock : Block {
        public BlockMesh m_meshStone = new();
        public BlockMesh m_meshMana = new();
        public Texture2D m_textureStone;
        public Texture2D m_textureMana;
        // 延迟获取的花药台行为子系统，用于查询该位置是否已注水。
        public SubsystemFlowerTableBehavior m_subsystemFlowerTable;

        public override bool IsTransparent_(int value) => true;

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) => true;

        // 右键花药台的交互优先级高于手持物品的「使用」优先级（PriorityUse=3000），
        // 使水桶优先注入花药台而不是把水倒在地上；OnInteract 未处理时回落到正常放置/使用逻辑。
        public override int GetPriorityInteract(int value, ComponentMiner componentMiner) => 3500;

        public override void Initialize() {
            base.Initialize();

            Model model = ContentManager.Get<Model>("Models/PhytoMana/flower_table");
            m_textureStone = ContentManager.Get<Texture2D>("Textures/PhytoMana/Stone");
            m_textureMana = ContentManager.Get<Texture2D>("Textures/PhytoMana/Water");


            var poolMesh = model.FindMesh("base");
            Matrix poolBone = BlockMesh.GetBoneAbsoluteTransform(poolMesh.ParentBone);
            m_meshStone.AppendModelMeshPart(
                poolMesh.MeshParts[0],
                poolBone * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false, false, false, false,
                Color.White
            );


            var manaMesh = model.FindMesh("water");
            Matrix manaBone = BlockMesh.GetBoneAbsoluteTransform(manaMesh.ParentBone);
            m_meshMana.AppendModelMeshPart(
                manaMesh.MeshParts[0],
                manaBone * Matrix.CreateTranslation(0f, 0.5f, 0f),
                false, false, false, false,
                Color.White
            );


        }


        public override Texture2D GetDefaultTexture(int value) => m_textureStone;


        public override void GenerateTerrainVertices(
            BlockGeometryGenerator generator,
            TerrainGeometry geometry,
            int value,
            int x,
            int y,
            int z
        ) {
            // 地形顶点生成可能在子系统就绪前首次调用，这里延迟获取引用。
            if (m_subsystemFlowerTable == null) {
                m_subsystemFlowerTable = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemFlowerTableBehavior>(false);
            }
            Matrix matrix = Matrix.CreateScale(0.0625f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_meshStone,
                Color.White,
                matrix,
                geometry.GetGeometry(m_textureStone).SubsetOpaque
            );
            // 仅在注水后绘制水面，走透明通道（与魔力池液体一致的渲染方式）。
            if (m_subsystemFlowerTable != null && m_subsystemFlowerTable.HasWater(x, y, z)) {
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_meshMana,
                    Color.White,
                    matrix,
                    geometry.GetGeometry(m_textureMana).SubsetTransparent
                );
            }
        }


        public override void DrawBlock(
            PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData
        ) {
            float drawSize = environmentData.DrawBlockMode == DrawBlockMode.World ? 2f * size * 0.05f : 2f * size;
            BlocksManager.DrawMeshBlock(
                primitivesRenderer,
                m_meshStone,
                m_textureStone,
                color,
                drawSize,
                ref matrix,
                environmentData
            );

            BlocksManager.DrawMeshBlock(
                primitivesRenderer,
                m_meshMana,
                m_textureMana,
                color,
                drawSize,
                ref matrix,
                environmentData
            );
        }
    }
}