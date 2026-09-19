using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 魔法星：无魔力消耗的观测型花，每 3 秒扫描平面 3×3 范围内的魔法池，
    /// 存量上升放蓝色升腾粒子、下降放红色下沉粒子。行为见 TileManaStarFlower。
    /// </summary>
    public class ManaStarFlower : FlowerBlock, IPhytoFlowerBlock {
        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ManaStarFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        public TilePhytoFlower CreateFlower(Point3 position) => new TileManaStarFlower(position);
    }
}
