using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 花药台方块：石质台座 + 可显示的水面网格，合成行为见 SubsystemFlowerTableBehavior。
    /// 注水后绘制水面；缓存中的原料以漂浮小方块展示在台面上方。
    /// </summary>
    public class FlowerTableBlock : Block {
        public BlockMesh m_meshStone = new();
        public BlockMesh m_meshMana = new();
        public Texture2D m_textureStone;
        public Texture2D m_textureMana;
        // 延迟获取的花药台行为子系统，用于查询该位置是否已注水、缓存了哪些原料。
        public SubsystemFlowerTableBehavior m_subsystemFlowerTable;
        // 每种原料的漂浮展示网格（按材质槽位映射到对应纹理），按方块索引缓存。
        public Dictionary<int, IngredientMesh> m_ingredientMeshes = [];

        public struct IngredientMesh {
            public BlockMesh Mesh;
            public Texture2D Texture;
        }

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
                DrawFloatingIngredients(generator, geometry, x, y, z);
            }
        }

        /// <summary>
        /// 台面上方漂浮展示已投入的原料：每种原料一个小方块，2×2 网格排布悬浮在水面上，
        /// 让玩家直接看到台子里「泡着什么」。原料种类最多展示 4 种，数量见空手右键的状态文本。
        /// </summary>
        public void DrawFloatingIngredients(
            BlockGeometryGenerator generator,
            TerrainGeometry geometry,
            int x,
            int y,
            int z
        ) {
            List<int> ingredientContents = m_subsystemFlowerTable.GetIngredientContents(x, y, z);
            if (ingredientContents.Count == 0) {
                return;
            }
            // 原生方块使用地形图集纹理，模组方块使用各自的独立纹理。
            Texture2D atlasTexture = generator.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture;
            int shown = Math.Min(ingredientContents.Count, 4);
            for (int i = 0; i < shown; i++) {
                IngredientMesh ingredientMesh = GetIngredientMesh(ingredientContents[i], atlasTexture);
                if (ingredientMesh.Mesh == null || ingredientMesh.Texture == null) {
                    continue;
                }
                float offsetX = (i % 2) == 0 ? 0.34f : 0.66f;
                float offsetZ = (i / 2) == 0 ? 0.34f : 0.66f;
                Matrix ingredientMatrix = Matrix.CreateScale(0.14f) * Matrix.CreateTranslation(offsetX, 0.72f, offsetZ);
                generator.GenerateMeshVertices(
                    this,
                    x,
                    y,
                    z,
                    ingredientMesh.Mesh,
                    Color.White,
                    ingredientMatrix,
                    geometry.GetGeometry(ingredientMesh.Texture).SubsetTransparent
                );
            }
        }

        /// <summary>获取（并缓存）指定原料的展示立方体网格：使用该方块自己的表面纹理。</summary>
        public IngredientMesh GetIngredientMesh(int contents, Texture2D atlasTexture) {
            if (m_ingredientMeshes.TryGetValue(contents, out IngredientMesh cached)) {
                return cached;
            }
            IngredientMesh result = default;
            Block block = BlocksManager.Blocks[contents];
            if (block != null) {
                int value = Terrain.MakeBlockValue(contents);
                int slotCount = Math.Max(1, block.GetTextureSlotCount(value));
                int slot = block.GetFaceTextureSlot(4, value);
                result = new IngredientMesh {
                    Mesh = BuildTexturedCubeMesh(slot, slotCount),
                    Texture = block.GetDefaultTexture(value) ?? atlasTexture
                };
            }
            m_ingredientMeshes[contents] = result;
            return result;
        }

        /// <summary>
        /// 构建单位立方体网格，六面角点排布与游戏 GenerateCubeVertices 一致（保证绕序/剔除正确），
        /// UV 按「材质槽位 / 槽位总数」映射进对应纹理，末尾平移到以原点为中心供渲染矩阵缩放定位。
        /// </summary>
        public static BlockMesh BuildTexturedCubeMesh(int slot, int slotCount) {
            // 角点 → UV 偏移：0 左下 1 右下 2 右上 3 左上（与游戏角点约定一致）。
            Vector2[] cornerUVs = [
                new(0f, 1f),
                new(1f, 1f),
                new(1f, 0f),
                new(0f, 0f)
            ];
            // 每面 4 个角点：位置偏移 + 角点序号（抄自 GenerateCubeVertices 的调用排布）。
            int[,,] faces = {
                { { 0, 0, 1, 0 }, { 1, 0, 1, 1 }, { 1, 1, 1, 2 }, { 0, 1, 1, 3 } },
                { { 1, 0, 0, 1 }, { 1, 1, 0, 2 }, { 1, 1, 1, 3 }, { 1, 0, 1, 0 } },
                { { 0, 0, 0, 1 }, { 1, 0, 0, 0 }, { 1, 1, 0, 3 }, { 0, 1, 0, 2 } },
                { { 0, 0, 0, 0 }, { 0, 1, 0, 3 }, { 0, 1, 1, 2 }, { 0, 0, 1, 1 } },
                { { 0, 1, 0, 3 }, { 1, 1, 0, 2 }, { 1, 1, 1, 1 }, { 0, 1, 1, 0 } },
                { { 0, 0, 0, 0 }, { 1, 0, 0, 1 }, { 1, 0, 1, 2 }, { 0, 0, 1, 3 } }
            };
            BlockMesh mesh = new();
            for (int face = 0; face < 6; face++) {
                int vertexBase = mesh.Vertices.Count;
                mesh.Vertices.Count += 4;
                for (int corner = 0; corner < 4; corner++) {
                    Vector2 cornerUV = cornerUVs[faces[face, corner, 3]];
                    BlockMeshVertex vertex = default;
                    vertex.Position = new Vector3(faces[face, corner, 0], faces[face, corner, 1], faces[face, corner, 2]);
                    vertex.TextureCoordinates = new Vector2(
                        (cornerUV.X + slot % slotCount) / slotCount,
                        (cornerUV.Y + slot / slotCount) / slotCount
                    );
                    vertex.Color = Color.White;
                    vertex.Face = (byte)face;
                    vertex.IsEmissive = false;
                    mesh.Vertices.Array[vertexBase + corner] = vertex;
                }
                int indexBase = mesh.Indices.Count;
                mesh.Indices.Count += 6;
                mesh.Indices.Array[indexBase] = vertexBase;
                mesh.Indices.Array[indexBase + 1] = vertexBase + 2;
                mesh.Indices.Array[indexBase + 2] = vertexBase + 1;
                mesh.Indices.Array[indexBase + 3] = vertexBase + 2;
                mesh.Indices.Array[indexBase + 4] = vertexBase;
                mesh.Indices.Array[indexBase + 5] = vertexBase + 3;
            }
            mesh.GenerateSidesData();
            mesh.TransformPositions(Matrix.CreateTranslation(-0.5f, -0.5f, -0.5f));
            return mesh;
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