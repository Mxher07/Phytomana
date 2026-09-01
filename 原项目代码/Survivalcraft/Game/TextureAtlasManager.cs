using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game {
    public static class TextureAtlasManager {
        public static Dictionary<string, Subtexture> m_subtextures = [];
        public static Texture2D AtlasTexture;

        public static void Clear() {
            m_subtextures.Clear();
        }

        public static void Initialize() {
            Texture2D texture = ContentManager.Get<Texture2D>("Atlases/AtlasTexture");
            string s = ContentManager.Get<string>("Atlases/Atlas");
            LoadAtlases(texture, s);
        }

        public static void LoadAtlases(Texture2D AtlasTexture_, string Atlas) {
            Clear();
            AtlasTexture = AtlasTexture_;
            LoadTextureAtlas(AtlasTexture, Atlas, "Textures/Atlas/");
        }

        public static Subtexture GetSubtexture(string name, bool throwOnNotFound) {
            if (!m_subtextures.TryGetValue(name, out Subtexture value)) {
                object value1 = ContentManager.Get(typeof(Texture2D), name, null, throwOnNotFound);
                if (value1 == null) {
                    if (throwOnNotFound) {
                        throw new FileNotFoundException($"Required subtexture {name} not found in TextureAtlasManager.");
                    }
                    return null;
                }
                value = new Subtexture(value1 as Texture2D, Vector2.Zero, Vector2.One);
                m_subtextures.Add(name, value);
            }
            return value;
        }

        public static Subtexture GetSubtexture(string name) => GetSubtexture(name, true);

        public static void LoadTextureAtlas(Texture2D texture, string atlasDefinition, string prefix) {
            string[] array = atlasDefinition.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            int num = 0;
            while (true) {
                if (num < array.Length) {
                    string[] array2 = array[num].Split([' '], StringSplitOptions.RemoveEmptyEntries);
                    if (array2.Length < 5) {
                        break;
                    }
                    string key = prefix + array2[0];
                    int num2 = int.Parse(array2[1], CultureInfo.InvariantCulture);
                    int num3 = int.Parse(array2[2], CultureInfo.InvariantCulture);
                    int num4 = int.Parse(array2[3], CultureInfo.InvariantCulture);
                    int num5 = int.Parse(array2[4], CultureInfo.InvariantCulture);
                    Vector2 topLeft = new(num2 / (float)texture.Width, num3 / (float)texture.Height);
                    Vector2 bottomRight = new((num2 + num4) / (float)texture.Width, (num3 + num5) / (float)texture.Height);
                    Subtexture value = new(texture, topLeft, bottomRight);
                    m_subtextures.Add(key, value);
                    num++;
                    continue;
                }
                return;
            }
            throw new InvalidOperationException("Invalid texture atlas definition.");
        }
    }
}