using Engine;
using Engine.Graphics;

namespace Game {
    public class ManaSpreaderBlock : CubeBlock {
        public Texture2D m_texture;
        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ManaSpreader");
        }
        public override int GetFaceTextureSlot(int face, int value) => face == 4 ? 1 : 0;
        public override int GetTextureSlotCount(int value) => 2;
        public override Texture2D GetDefaultTexture(int value) => m_texture;
    }
}