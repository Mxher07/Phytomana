using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 须弥花瓣：按特殊值（data）分 16 种颜色，贴图固定一张，绘制时按调色板上色。
    /// data 布局与 SumeruFlowerBlock 完全一致（bit1~4 = 颜色索引），使花→花瓣的
    /// 数据直传（.fr 配方 CopyData）天然保色。花瓣是纯物品方块（不可放置），
    /// 只需处理 DrawBlock 一条渲染路径。
    /// </summary>
    public class SumeruPetalBlock : FlatBlock {
        public const int ColorCount = 16;

        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/SumeruPetal");
        }

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        /// <summary>颜色索引存于 data 的 bit1~4（与 SumeruFlowerBlock 相同布局）。</summary>
        public static int GetColorIndex(int data) => (data >> 1) & 0xF;

        /// <summary>颜色索引对应的染色：0 为白色，1~15 查世界调色板。</summary>
        public static Color GetColorValue(int data, SubsystemPalette palette) => SumeruFlowerBlock.GetColorValue(data, palette);

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            SubsystemPalette palette = environmentData?.SubsystemTerrain?.SubsystemPalette;
            Color tint = SumeruFlowerBlock.GetColorValue(Terrain.ExtractData(value), palette);
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

        public override IEnumerable<int> GetCreativeValues() {
            List<int> list = [];
            for (int colorIndex = 0; colorIndex < ColorCount; colorIndex++) {
                list.Add(Terrain.MakeBlockValue(BlockIndex, 0, colorIndex << 1));
            }
            return list;
        }
    }
}
