using System.Collections.Generic;
using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 初阶符文（data 0~3）：水之、火之、地之、风之。贴图索引 = 特殊值。
    /// </summary>
    public class BasicRunesBlock : FlatBlock {
        public const int MinData = 0;
        public const int MaxData = 3;

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