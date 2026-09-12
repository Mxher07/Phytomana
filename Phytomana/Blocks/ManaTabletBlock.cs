using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 魔力石板：可存储 3000mn 魔力的板子（不堆叠）。
    /// 特殊值（data）= 石板当前存储的魔力（0~3000，随存档保存）。
    /// 动态贴图：无魔力（≤5mn）时为暗版 _manaTablet0，充能（>5mn）时为亮版 _manaTablet1。
    /// 手持点击魔法池灌注魔力；手持时任意点击显示当前魔力；空板丢进魔法池会缓慢吸魔。
    /// 行为见 SubsystemManaTableBehavior 与 SubsystemMana 的石板吸魔逻辑。
    /// </summary>
    public class ManaTabletBlock : FlatBlock {
        public const int MaxMana = 3000;

        /// <summary>魔力高于该阈值时显示充能（亮）贴图。</summary>
        public const int ChargedThreshold = 5;

        public Texture2D m_textureEmpty;

        public Texture2D m_textureCharged;

        public override void Initialize() {
            base.Initialize();
            m_textureEmpty = ContentManager.Get<Texture2D>("Textures/PhytoMana/_manaTablet0");
            m_textureCharged = ContentManager.Get<Texture2D>("Textures/PhytoMana/_manaTablet1");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) =>
            Terrain.ExtractData(value) > ChargedThreshold ? m_textureCharged : m_textureEmpty;
    }
}