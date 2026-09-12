using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 符文台方块：符文石台座（runes_table 模型），行为见 SubsystemRunesTableBehavior。
    /// 手持材料点击放置（悬浮显示在祭坛上方，由 SubsystemAltarItemDisplay 每帧绘制），
    /// 法杖工作模式右键炼制符文。碰撞箱高为完整方块的 15/16。
    /// </summary>
    public class RunesTableBlock : Block {
        public BlockMesh m_meshTable = new();
        public Texture2D m_textureTable;
        // 延迟获取的符文台行为子系统，用于查询该位置已放置的材料。
        public SubsystemRunesTableBehavior m_subsystemRunesTable;
        // 碰撞箱：占满底面，高为完整方块的 15/16。
        public readonly BoundingBox[] m_collisionBoxes =
            [new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.9375f, 1f))];

        public override bool IsTransparent_(int value) => true;

        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) => true;

        // 右键符文台的交互优先级高于手持物品的「使用」（PriorityUse=3000）与放置；
        // OnInteract 未处理时（如手持法杖）回落到正常使用/放置逻辑。
        public override int GetPriorityInteract(int value, ComponentMiner componentMiner) => 3500;

        public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_collisionBoxes;

        public override void Initialize() {
            base.Initialize();
            Model model = ContentManager.Get<Model>("Models/PhytoMana/runes_table");
            m_textureTable = ContentManager.Get<Texture2D>("Textures/PhytoMana/RunesStone");
            // 模型的网格名可能未绑定，直接取第一个网格（本模型只有一个整体台座网格）。
            ModelMesh tableMesh = model.Meshes[0];
            Matrix tableBone = BlockMesh.GetBoneAbsoluteTransform(tableMesh.ParentBone);
            m_meshTable.AppendModelMeshPart(
                tableMesh.MeshParts[0],
                tableBone * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false, false, false, false,
                Color.White
            );
        }

        public override Texture2D GetDefaultTexture(int value) => m_textureTable;

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override void GenerateTerrainVertices(
            BlockGeometryGenerator generator,
            TerrainGeometry geometry,
            int value,
            int x,
            int y,
            int z
        ) {
            if (m_subsystemRunesTable == null) {
                m_subsystemRunesTable = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemRunesTableBehavior>(false);
            }
            Matrix matrix = Matrix.CreateScale(0.0625f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_meshTable,
                Color.White,
                matrix,
                geometry.GetGeometry(m_textureTable).SubsetOpaque
            );
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
                m_meshTable,
                m_textureTable,
                color,
                drawSize,
                ref matrix,
                environmentData
            );
        }
    }
}