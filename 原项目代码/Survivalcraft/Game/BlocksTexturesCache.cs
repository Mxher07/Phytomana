using Engine.Graphics;

namespace Game {
    public class BlocksTexturesCache {
        public Dictionary<string, Texture2D> m_textures = [];

        public Texture2D GetTexture(string name) {
            if (!m_textures.TryGetValue(name, out Texture2D value)) {
                value = BlocksTexturesManager.LoadTexture(name);
                m_textures.Add(name, value);
            }
            return value;
        }

        public void Clear() {
            foreach (Texture2D value in m_textures.Values) {
                if (!ContentManager.IsContent(value)) {
                    value.Dispose();
                }
            }
            m_textures.Clear();
        }
    }
}