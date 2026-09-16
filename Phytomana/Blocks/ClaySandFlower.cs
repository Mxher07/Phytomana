using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 黏土沙馨：消耗魔力把周围沙子变成粘土（物品形式掉落）的功能花。行为见 TileClaySandFlower。
    /// </summary>
    public class ClaySandFlower : FlowerBlock, IPhytoFlowerBlock {
        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ClaySandFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        public TilePhytoFlower CreateFlower(Point3 position) => new TileClaySandFlower(position);
    }
}
