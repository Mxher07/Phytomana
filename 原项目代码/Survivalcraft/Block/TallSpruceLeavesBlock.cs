using Engine;
namespace Game {
    public class TallSpruceLeavesBlock : EvergreenLeavesBlock {
        public static int Index = 225;

        public override Color GetLeavesBlockColor(int value, Terrain terrain, int x, int y, int z) =>
            BlockColorsMap.TallSpruceLeaves.Lookup(terrain, x, y, z);

        public override Color GetLeavesItemColor(int value, DrawBlockEnvironmentData environmentData) =>
            BlockColorsMap.TallSpruceLeaves.Lookup(environmentData);
    }
}