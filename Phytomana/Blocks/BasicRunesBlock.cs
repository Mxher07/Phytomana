using System.Collections.Generic;
using Engine.Core;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 初阶符文（data 0~3）：水之、火之、地之、风之。贴图索引 = 特殊值。
    /// </summary>
    public class BasicRunesBlock : PhytoFlatBlock {
        public const int MinData = 0;
        public const int MaxData = 3;
    public static int Index = 666;
        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetPhytoTexture(int value) => RuneTextures.Get(Terrain.ExtractData(value));

        public override IEnumerable<int> GetCreativeValues() {
            List<int> list = [];
            for (int data = MinData; data <= MaxData; data++) {
                list.Add(Terrain.MakeBlockValue(BlockIndex, 0, data));
            }
            return list;
        }
    }
}