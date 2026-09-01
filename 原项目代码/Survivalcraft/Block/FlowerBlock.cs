namespace Game {
    public abstract class FlowerBlock : CrossBlock {
        public override int GetFaceTextureSlot(int face, int value) {
            if (GetIsSmall(Terrain.ExtractData(value))) {
                return 11;
            }
            return base.GetFaceTextureSlot(face, value);
        }

        public override void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            int data = Terrain.ExtractData(oldValue);
            if (!GetIsSmall(data)) {
                dropValues.Add(new BlockDropValue { Value = Terrain.MakeBlockValue(Terrain.ExtractContents(oldValue), 0, data), Count = 1 });
            }
            showDebris = true;
        }

        public override int GetShadowStrength(int value) {
            if (!GetIsSmall(Terrain.ExtractData(value))) {
                return DefaultShadowStrength;
            }
            return DefaultShadowStrength / 2;
        }

        public static bool GetIsSmall(int data) => (data & 1) != 0;

        public static int SetIsSmall(int data, bool isSmall) {
            if (!isSmall) {
                return data & -2;
            }
            return data | 1;
        }
    }
}