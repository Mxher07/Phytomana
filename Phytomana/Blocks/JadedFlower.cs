using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 翡翠菜：消耗魔力在周围生成须弥花的功能花。行为见 TileJadedFlower。
    /// </summary>
    public class JadedFlower : FlowerBlock, IPhytoFlowerBlock {
        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/JadedFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        public TilePhytoFlower CreateFlower(Point3 position) => new TileJadedFlower(position);
    }
}
