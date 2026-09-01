namespace Game {
    public class DirtStairsBlock : StairsBlock {
        public const int Index = 260;

        public DirtStairsBlock() : base(2) { }

        public override int Paint(SubsystemTerrain terrain, int value, int? color) => value;

        public override int? GetPaintColor(int value) => null;

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(BlockIndex, 0, SetColor(0, null));
        }
    }
}