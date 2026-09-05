using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 魔力池方块：使用石质底座承载魔力液体，并根据方块当前位置保存的魔力值
    /// 动态调整液体模型的显示高度。
    /// </summary>
    public class ManaPoolBlock : Block {
        // 底座和液体使用独立的网格，绘制时可以只显示需要的部分。
        public BlockMesh m_meshStone = new();
        public BlockMesh m_meshMana = new();
        // 两个网格分别对应模型中的 base 和 mana 部件。
        public Texture2D m_textureStone;
        public Texture2D m_textureMana;
        // 用于查询指定坐标处的当前魔力和最大魔力容量。
        public SubsystemMana m_subsystemMana;
        // 碰撞体数组的第一项对应该方块的唯一碰撞体；液体部分不参与碰撞。
        public BoundingBox[][] m_collisionBoxes = new BoundingBox[1][];

        // 魔力液体具有透明效果，因此整个方块不能阻挡后方方块的可见面。
        public override bool IsTransparent_(int value) => true;

        // 对每个面都启用透明处理，避免相邻方块面被魔力池错误遮挡。
        public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) => true;
        
        public override void Initialize() {
            // 碰撞体覆盖完整的方块底面，但高度只有 0.5 个方块，
            // 这样玩家可以站在石质底座上，同时不会与上方液体区域发生碰撞。
            m_collisionBoxes[0] = [new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.5f, 1f))];
            base.Initialize();
            
            // 读取模型资源。模型中包含两个命名网格：石质底座 base 和魔力液体 mana。
            Model model = ContentManager.Get<Model>("Models/PhytoMana/mana_pool");
            // 底座使用生长石纹理，液体使用魔力纹理。
            m_textureStone = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownStone");
            m_textureMana = ContentManager.Get<Texture2D>("Textures/PhytoMana/Mana2");
            
            
            // 提取底座网格及其父骨骼的绝对变换，确保模型骨骼层级不会丢失。
            var poolMesh = model.FindMesh("base");
            Matrix poolBone = BlockMesh.GetBoneAbsoluteTransform(poolMesh.ParentBone);
            m_meshStone.AppendModelMeshPart(
                poolMesh.MeshParts[0],
                // 模型原点向下偏移半个方块，使底座落在方块底部。
                poolBone * Matrix.CreateTranslation(0f, -0.5f, 0f),
                false, false, false, false,
                Color.White
            );
            
            
            // 提取液体网格，并将其初始位置放在底座上方。
            var manaMesh = model.FindMesh("mana");
            Matrix manaBone = BlockMesh.GetBoneAbsoluteTransform(manaMesh.ParentBone);
            m_meshMana.AppendModelMeshPart(
                manaMesh.MeshParts[0],
                manaBone * Matrix.CreateTranslation(0f, 0.5f, 0f),
                false, false, false, false,
                Color.White
            );
        }
        
        
        // 方块的默认纹理用于普通方块渲染和未指定专用纹理的场景，使用底座纹理即可。
        public override Texture2D GetDefaultTexture(int value) => m_textureStone;
        
        
        // 将石质底座和当前魔力液体添加到地形几何体中。
        public override void GenerateTerrainVertices(
            BlockGeometryGenerator generator, 
            TerrainGeometry geometry, 
            int value, 
            int x, 
            int y, 
            int z
        ) {
            // 地形顶点生成可能在初始化后首次调用，因此在这里延迟获取子系统引用。
            if (m_subsystemMana == null) {
                m_subsystemMana = generator.SubsystemTerrain.Project.FindSubsystem<SubsystemMana>(true);
            }
            // 模型资源单位与游戏方块单位不同：缩放到 1/16，并将模型中心移动到方块中心。
            Matrix baseMatrix = Matrix.CreateScale(0.0625f) * Matrix.CreateTranslation(0.5f, 0f, 0.5f);
            generator.GenerateMeshVertices(
                this,
                x,
                y,
                z,
                m_meshStone,
                Color.White,
                baseMatrix,
                geometry.GetGeometry(m_textureStone).SubsetOpaque
            );
            // 使用方块坐标查询该魔力池的实时魔力值和该方块类型的最大容量。
            float currentMana = m_subsystemMana.GetManaAmount(new Point3(x, y, z));
            float maxMana = m_subsystemMana.GetMaxManaAmount(Terrain.ExtractContents(value));
            if (currentMana > 0f && maxMana > 0f) {
                // 将比例限制在 0 到 1 之间，防止异常数据使液体超出模型预期范围。
                float pct = MathUtils.Clamp(currentMana / maxMana, 0f, 1f);
                // 液体底部位于 0.2，高度最多再增加 0.5，魔力越多显示得越高。
                float manaY = 0.2f + 0.25f * pct;
                Matrix manaMatrix = Matrix.CreateScale(0.0625f) * Matrix.CreateTranslation(0.5f, manaY, 0.5f);
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    m_meshMana,
                    Color.White,
                    manaMatrix,
                    geometry.GetGeometry(m_textureMana).SubsetOpaque
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
            // 世界中的模型使用较小尺寸以匹配方块坐标系；物品栏、手持物品等场景
            // 使用相对完整的尺寸，避免预览模型过小。
            float drawSize = environmentData.DrawBlockMode == DrawBlockMode.World ? 2f * size * 0.05f : 2f * size;
            // 先绘制固定的石质底座，再绘制魔力液体，使两部分使用各自的纹理。
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
