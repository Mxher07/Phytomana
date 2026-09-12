using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 魔力钻石块：九个魔力钻石压缩成的可放置方块，钻石块在魔法池中注入 4000mn 获得。
    /// </summary>
    public class ManaDiamondBlock : CubeBlock {
        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = true;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ManaIngot");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;
    }
}