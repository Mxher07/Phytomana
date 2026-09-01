namespace Game {
    public class LargeDryBushBlock : CrossBlock {
        public static int Index = 99;

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            dropValues.Add(new BlockDropValue { Value = 23, Count = 1 });
            showDebris = true;
        }
    }
}