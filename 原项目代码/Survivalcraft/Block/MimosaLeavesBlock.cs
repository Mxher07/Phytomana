using Engine;
namespace Game {
    public class MimosaLeavesBlock : DeciduousLeavesBlock {
        public static int Index = 256;

        public MimosaLeavesBlock() : base(
            0f,
            0.25f,
            0.54f,
            0.85f,
            BlockColorsMap.MimosaLeaves,
            new Color(192, 100, 0),
            new Color(192, 150, 0),
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