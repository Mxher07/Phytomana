using Engine;
namespace Game {
    public class PoplarLeavesBlock : DeciduousLeavesBlock {
        public const int Index = 263;

        public PoplarLeavesBlock() : base(
            0.97f,
            0.17f,
            0.52f,
            0.84f,
            BlockColorsMap.PoplarLeaves,
            new Color(220, 130, 20),
            new Color(255, 190, 60),
            1.5f
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