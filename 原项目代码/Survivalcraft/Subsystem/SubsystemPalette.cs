using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemPalette : Subsystem {
        public static readonly Color[] m_defaultFabricColors;
        public static string m_nameFormat = LanguageControl.Get("SubsystemPalette", "1");

        public string[] m_names;

        public Color[] m_colors;

        public Color[] m_fabricColors;

        static SubsystemPalette() {
            m_defaultFabricColors = new Color[16];
            m_defaultFabricColors = CreateFabricColors(WorldPalette.DefaultColors);
        }

        public Color GetColor(int index) => m_colors[index];

        public string GetName(int index) => m_names[index];

        public Color GetFabricColor(int index) => m_fabricColors[index];

        public static Color GetColor(BlockGeometryGenerator generator, int? index) {
            if (index.HasValue) {
                if (generator.SubsystemPalette != null) {
                    return generator.SubsystemPalette.GetColor(index.Value);
                }
                return WorldPalette.DefaultColors[index.Value];
            }
            return Color.White;
        }

        public static Color GetColor(DrawBlockEnvironmentData environmentData, int? index) => GetColor(environmentData.SubsystemTerrain, index);

        public static Color GetColor(SubsystemTerrain subsystemTerrain, int? index) {
            if (index.HasValue) {
                if (subsystemTerrain != null
                    && subsystemTerrain.SubsystemPalette != null) {
                    return subsystemTerrain.SubsystemPalette.GetColor(index.Value);
                }
                return WorldPalette.DefaultColors[index.Value];
            }
            return Color.White;
        }

        public static Color GetFabricColor(BlockGeometryGenerator generator, int? index) {
            if (index.HasValue) {
                if (generator.SubsystemPalette != null) {
                    return generator.SubsystemPalette.GetFabricColor(index.Value);
                }
                return m_defaultFabricColors[index.Value];
            }
            return Color.White;
        }

        public static Color GetFabricColor(DrawBlockEnvironmentData environmentData, int? index) =>
            GetFabricColor(environmentData.SubsystemTerrain, index);

        public static Color GetFabricColor(SubsystemTerrain subsystemTerrain, int? index) {
            if (index.HasValue) {
                if (subsystemTerrain != null
                    && subsystemTerrain.SubsystemPalette != null) {
                    return subsystemTerrain.SubsystemPalette.GetFabricColor(index.Value);
                }
                return m_defaultFabricColors[index.Value];
            }
            return Color.White;
        }

        public static string GetName(SubsystemTerrain subsystemTerrain, int? index, string suffix) {
            if (index.HasValue) {
                string text = LanguageControl.GetWorldPalette(index.Value);
                if (!string.IsNullOrEmpty(suffix)) {
                    return string.Format(m_nameFormat, text, suffix);
                }
                return text;
            }
            return suffix ?? string.Empty;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_colors = subsystemGameInfo.WorldSettings.Palette.Colors.ToArray();
            m_names = subsystemGameInfo.WorldSettings.Palette.Names.ToArray();
            m_fabricColors = CreateFabricColors(m_colors);
        }

        public static Color[] CreateFabricColors(Color[] colors) {
            Color[] array = new Color[16];
            for (int i = 0; i < 16; i++) {
                Vector3 rgb = new(colors[i]);
                Vector3 hsv = Color.RgbToHsv(rgb);
                hsv.Y *= 0.85f;
                rgb = Color.HsvToRgb(hsv);
                array[i] = new Color(rgb);
            }
            return array;
        }
    }
}