namespace Game {
    public abstract class EvergreenLeavesBlock : LeavesBlock {
        public Random m_random1 = new();

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            if (m_random1.Bool(0.25f)) {
                dropValues.Add(new BlockDropValue { Value = 23, Count = 1 });
                showDebris = true;
            }
            else {
                base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
            }
        }

        public override IEnumerable<int> GetCreativeValues() {
            yield return Terrain.MakeBlockValue(BlockIndex);
        }
    }
}