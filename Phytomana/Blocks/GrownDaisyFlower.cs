using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    public class GrownDaisyFlower : PhytoCrossBlock, IPhytoFlowerBlock {
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownDaisy");
        }
        public override int GetFaceTextureSlot(int face, int value) => 0;
        public override int GetTextureSlotCount(int value) => 1;
        public TilePhytoFlower CreateFlower(Point3 position) => new TileGrownDaisyFlower(position);
    }
}