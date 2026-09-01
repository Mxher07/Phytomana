using Engine;
namespace Game {
    public class OakLeavesBlock : DeciduousLeavesBlock {
        public static int Index = 12;

        public OakLeavesBlock() : base(
            0f,
            0.25f,
            0.54f,
            0.85f,
            BlockColorsMap.OakLeaves,
            new Color(230, 80, 0),
            new Color(255, 130, 20),
            2f
        ) { }

        public override int GetFaceTextureSlot(int face, int value) {
            return GetSeason(Terrain.ExtractData(value)) switch {
                Season.Winter => 106,
                Season.Spring => 107,
                _ => base.GetFaceTextureSlot(face, value)
            };
        }
    }
}