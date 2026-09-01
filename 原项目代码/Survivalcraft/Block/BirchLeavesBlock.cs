using Engine;
namespace Game {
    public class BirchLeavesBlock : DeciduousLeavesBlock {
        public static int Index = 13;

        public BirchLeavesBlock() : base(
            0.95f,
            0.1f,
            0.5f,
            0.83f,
            BlockColorsMap.BirchLeaves,
            new Color(220, 170, 30),
            new Color(255, 230, 70),
            1.25f
        ) { }

        public override int GetFaceTextureSlot(int face, int value) {
            Season season = GetSeason(Terrain.ExtractData(value));
            if (season == Season.Winter) {
                return 106;
            }
            return base.GetFaceTextureSlot(face, value);
        }
    }
}