using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 须弥花：按特殊值（data）分 16 种颜色，贴图固定一张，渲染时按调色板上色。
    /// data 布局与 FlowerBlock 兼容：bit0 = IsSmall（微型花），bit1~4 = 颜色索引 0~15
    /// （0 为默认白色，1~15 对应世界颜料调色板，与原版电线/颜料的取色方式一致）。
    /// </summary>
    public class SumeruFlowerBlock : FlowerBlock {
        public const int ColorCount = 16;

        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/SumeruFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        /// <summary>从 data 中取颜色索引（bit1~4）。</summary>
        public static int GetColorIndex(int data) => (data >> 1) & 0xF;

        /// <summary>把颜色索引写入 data 的 bit1~4，保留 IsSmall 等其他位。</summary>
        public static int SetColorIndex(int data, int colorIndex) => (data & ~0x1E) | ((colorIndex & 0xF) << 1);

        public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z) {
            Texture2D texture = GetDefaultTexture(value);
            Color tint = GetColorValue(Terrain.ExtractData(value), generator.SubsystemPalette);
            generator.GenerateCrossfaceVertices(
                this,
                value,
                x,
                y,
                z,
                tint,
                GetFaceTextureSlot(0, value),
                texture == null ? geometry.SubsetAlphaTest : geometry.GetGeometry(texture).SubsetAlphaTest
            );
        }

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            SubsystemPalette palette = environmentData?.SubsystemTerrain?.SubsystemPalette;
            Color tint = GetColorValue(Terrain.ExtractData(value), palette);
            BlocksManager.DrawFlatOrImageExtrusionBlock(
                primitivesRenderer,
                value,
                size,
                ref matrix,
                m_texture,
                color * tint,
                false,
                environmentData
            );
        }

        /// <summary>颜色索引对应的染色：0 为白色（保持默认外观），1~15 查世界调色板。</summary>
        public static Color GetColorValue(int data, SubsystemPalette palette) {
            int colorIndex = GetColorIndex(data);
            if (colorIndex == 0) {
                return Color.White;
            }
            return palette != null ? palette.GetColor(colorIndex) : Color.White;
        }

        public override IEnumerable<int> GetCreativeValues() {
            List<int> list = [];
            for (int colorIndex = 0; colorIndex < ColorCount; colorIndex++) {
                list.Add(Terrain.MakeBlockValue(BlockIndex, 0, SetColorIndex(0, colorIndex)));
            }
            return list;
        }
    }
}
