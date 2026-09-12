using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 传箱花：拾取周围的掉落物并送入贴身箱子（功能花）。行为见 TileChestTransFlower。
    /// </summary>
    public class ChestTransFlower : FlowerBlock, IPhytoFlowerBlock {
        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ChestTransFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        public TilePhytoFlower CreateFlower(Point3 position) => new TileChestTransFlower(position);
    }
}
