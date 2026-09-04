using Engine;
using Engine.Graphics;

namespace Game {
    public class GrownStaffBlock : FlatBlock {
        public Texture2D m_texture;
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownStaff");
        }
        public override int GetTextureSlotCount(int value) => 1;
        public override Texture2D GetDefaultTexture(int value) => m_texture;
        public override int GetPriorityUse(int value, ComponentMiner componentMiner) => 3000;
    }
}