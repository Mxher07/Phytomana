using Engine;
using Engine.Graphics;

namespace Game {
    public class ManaSpreaderBlock : PhytoCubeBlock {
    public static int Index = 1145;
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ManaSpreader");
        }
        public override int GetFaceTextureSlot(int face, int value) => face == 4 ? 1 : 0;
        public override int GetTextureSlotCount(int value) => 2;
    }
}