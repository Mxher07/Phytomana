using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 符文贴图共享缓存。pale_Rune0~15 依次为：
    /// 水、火、地、风（初阶）；春、夏、秋、冬、魔力（中阶）；
    /// 欲望、暴食、贪婪、懒惰、暴怒、嫉妒、傲慢（高阶）。
    /// 符文方块的特殊值（data）即此索引。
    /// </summary>
    public static class RuneTextures {
        public const int Count = 16;

        static readonly Texture2D[] m_textures = new Texture2D[Count];

        /// <summary>按符文索引取贴图（延迟加载，索引越界返回 null）。</summary>
        public static Texture2D Get(int runeIndex) {
            if (runeIndex < 0 || runeIndex >= Count) {
                return null;
            }
            if (m_textures[runeIndex] == null) {
                m_textures[runeIndex] = ContentManager.Get<Texture2D>($"Textures/PhytoMana/Runes/pale_Rune{runeIndex}");
            }
            return m_textures[runeIndex];
        }
    }
}