using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 须弥手册方块：模组自带的说明图书，由 ShichiyoScribe 书籍系统驱动
    /// （其在 BlocksInitalized 时扫描 *.vlb 并绑定到本方块索引）。
    /// 使用独立纹理与独立绑定，不与 ShichiyoScribe 自带书方块冲突。
    /// </summary>
    public class SumeruManualBlock : FlatBlock {
        public static int Index;

        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/SumeruManual");
        }

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        // 独立纹理必须声明为单槽位：默认的 16 会被当成图集网格，
        // 只取样独立纹理左上 1/16 的空白角，表现为「没有贴图」。
        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        /// <summary>书籍永远可「使用」，手持时点击任意地方即可翻开。</summary>
        public override bool IsUseable_(int value) => true;
    }
}