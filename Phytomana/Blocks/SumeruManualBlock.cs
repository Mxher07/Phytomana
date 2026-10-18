using Engine.Core;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 须弥手册方块：模组自带的说明图书，由 ShichiyoScribe 书籍系统驱动
    /// （其在 BlocksInitalized 时扫描 *.vlb 并绑定到本方块索引）。
    /// 使用独立纹理与独立绑定，不与 ShichiyoScribe 自带书方块冲突。
    /// </summary>
    public class SumeruManualBlock : PhytoFlatBlock {
        public static int Index = 343;

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/SumeruManual");
        }

        // 独立纹理必须声明为单槽位：默认的 16 会被当成图集网格，
        // 只取样独立纹理左上 1/16 的空白角，表现为「没有贴图」。
        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        // 书籍的「使用」由 ShichiyoScribe 的书籍行为经行为派发处理，
        // 新联机 API 已无 IsUseable_ 概念，无需在方块上覆写。
    }
}