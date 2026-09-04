using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    public class WaterDonFlower : FlowerBlock, IPhytoFlowerBlock {
        public Texture2D m_texture;
        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/WaterDon");
        }
        public override int GetFaceTextureSlot(int face, int value) => 0;
        public override int GetTextureSlotCount(int value) => 1;
        public override Texture2D GetDefaultTexture(int value) => m_texture;
        public TilePhytoFlower CreateFlower(Point3 position) => new TileWaterDonFlower(position);
    }
}