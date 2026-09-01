namespace Game {
    public class GravelBlock : CubeBlock {
        public static int Index = 6;

        public GravelBlock() => IsCollapsable = true;

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            showDebris = true;
            if (toolLevel < RequiredToolLevel) {
                return;
            }
            if (Random.Float(0f, 1f) < 0.33f) {
                base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
                return;
            }
            int num = Random.Int(1, 3);
            for (int i = 0; i < num; i++) {
                dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(79), Count = 1 });
            }
        }
    }
}