using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 高阶符文（data 9~15）：欲望、暴食、贪婪、懒惰、暴怒、嫉妒、傲慢。贴图索引 = 特殊值。
    /// </summary>
    public class ExpertRunesBlock : FlatBlock {
        public const int MinData = 9;
        public const int MaxData = 15;

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