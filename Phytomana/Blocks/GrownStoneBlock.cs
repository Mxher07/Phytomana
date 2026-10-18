using Engine;
using Engine.Graphics;

namespace Game {
    public class GrownStoneBlock : PhytoCubeBlock {
    public static int Index = 310;
        public override void Initialize() {
        
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownStone");
        }
        public override int GetFaceTextureSlot(int face, int value) => 0;
        public override int GetTextureSlotCount(int value) => 1;
    }
}