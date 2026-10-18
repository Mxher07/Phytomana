using Engine;
using Engine.Graphics;

namespace Game {
    public class GrownStaffBlock : PhytoFlatBlock {
    public static int Index = 666;
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownStaff");
        }
        public override int GetTextureSlotCount(int value) => 1;
    }
}