using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    public class WaterDonFlower : PhytoCrossBlock, IPhytoFlowerBlock {
    public static int Index = 1145;
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/WaterDon");
        }
        public override int GetFaceTextureSlot(int face, int value) => 0;
        public override int GetTextureSlotCount(int value) => 1;
        public TilePhytoFlower CreateFlower(Point3 position) => new TileWaterDonFlower(position);
    }
}