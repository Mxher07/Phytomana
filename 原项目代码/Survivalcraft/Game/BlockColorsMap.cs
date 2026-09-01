using Engine;

namespace Game {
    public class BlockColorsMap {
        public Color[] m_map = new Color[256];

        public static BlockColorsMap Water = new(new Color(0, 0, 120), new Color(0, 80, 100), new Color(0, 40, 85), new Color(0, 113, 97));

        public static BlockColorsMap Grass = new(new Color(151, 184, 195), new Color(210, 201, 93), new Color(151, 184, 195), new Color(79, 225, 56));

        public static BlockColorsMap OakLeaves = new(
            new Color(96, 161, 123),
            new Color(174, 164, 42),
            new Color(96, 161, 123),
            new Color(30, 191, 1)
        );

        public static BlockColorsMap BirchLeaves = new(
            new Color(76, 181, 96),
            new Color(174, 109, 42),
            new Color(66, 215, 116),
            new Color(77, 235, 96)
        );

        public static BlockColorsMap MimosaLeaves = new(
            new Color(146, 191, 176),
            new Color(160, 191, 176),
            new Color(146, 191, 166),
            new Color(150, 201, 141)
        );

        public static BlockColorsMap PoplarLeaves = new(
            new Color(76, 181, 96),
            new Color(174, 109, 42),
            new Color(56, 205, 106),
            new Color(67, 215, 86)
        );

        public static BlockColorsMap SpruceLeaves = new(
            new Color(96, 161, 155),
            new Color(129, 174, 42),
            new Color(96, 161, 155),
            new Color(1, 191, 53)
        );

        public static BlockColorsMap TallSpruceLeaves = new(
            new Color(90, 141, 165),
            new Color(119, 152, 51),
            new Color(86, 141, 165),
            new Color(1, 158, 65)
        );

        public static BlockColorsMap Ivy = new(new Color(106, 161, 143), new Color(174, 164, 42), new Color(106, 161, 143), new Color(30, 191, 1));

        public static BlockColorsMap Kelp = new(new Color(80, 110, 90), new Color(110, 110, 50), new Color(80, 110, 90), new Color(110, 110, 50));

        public static BlockColorsMap Seagrass = new(new Color(50, 120, 110), new Color(80, 120, 70), new Color(50, 120, 110), new Color(80, 120, 70));

        public BlockColorsMap(Color th11, Color th21, Color th12, Color th22) {
            for (int i = 0; i < 16; i++) {
                for (int j = 0; j < 16; j++) {
                    float f = MathUtils.Saturate(i / 8f);
                    float f2 = MathUtils.Saturate((j - 4) / 10f);
                    Color c = Color.Lerp(th11, th21, f);
                    Color c2 = Color.Lerp(th12, th22, f);
                    Color color = Color.Lerp(c, c2, f2);
                    int num = i + j * 16;
                    m_map[num] = color;
                }
            }
        }

        public Color Lookup(int temperature, int humidity) {
            int num = Math.Clamp(temperature, 0, 15) + 16 * Math.Clamp(humidity, 0, 15);
            return m_map[num];
        }

        public Color Lookup(Terrain terrain, int x, int y, int z) {
            int shaftValue = terrain.GetShaftValue(x, z);
            int temperature = terrain.GetSeasonalTemperature(shaftValue) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
            int seasonalHumidity = terrain.GetSeasonalHumidity(shaftValue);
            return Lookup(temperature, seasonalHumidity);
        }

        public Color Lookup(DrawBlockEnvironmentData environmentData) => Lookup(environmentData.Temperature, environmentData.Humidity);
    }
}