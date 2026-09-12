using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 中阶符文（data 4~8）：春之、夏之、秋之、冬之、魔力。贴图索引 = 特殊值。
    /// </summary>
    public class MediumRunesBlock : FlatBlock {
        public const int MinData = 4;
        public const int MaxData = 8;

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => RuneTextures.Get(Terrain.ExtractData(value));

        public override IEnumerable<int> GetCreativeValues() {
            List<int> list = [];
            for (int data = MinData; data <= MaxData; data++) {
                list.Add(Terrain.MakeBlockValue(BlockIndex, 0, data));
            }
            return list;
        }
    }
}