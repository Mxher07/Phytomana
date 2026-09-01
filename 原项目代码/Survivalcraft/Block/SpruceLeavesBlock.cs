using Engine;
namespace Game {
    public class SpruceLeavesBlock : EvergreenLeavesBlock {
        public static int Index = 14;

        public override Color GetLeavesBlockColor(int value, Terrain terrain, int x, int y, int z) =>
            BlockColorsMap.SpruceLeaves.Lookup(terrain, x, y, z);

        public override Color GetLeavesItemColor(int value, DrawBlockEnvironmentData environmentData) =>
            BlockColorsMap.SpruceLeaves.Lookup(environmentData);
    }
}