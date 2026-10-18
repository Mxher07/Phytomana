using Engine.Core;
using Engine.Graphics;

namespace Game {
    /// <summary>
    /// 框架自测用产魔方块：仅验证魔力网络投递，不属于正式玩法，后续阶段可移除。
    /// </summary>
    public class TestManaSourceBlock : PhytoCubeBlock {

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/Mana2");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

    }
}
