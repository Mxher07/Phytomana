using Engine;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 框架自测用产魔花方块：仅验证花朵框架（基座判定、休眠、分片 Tick、网络投递、存档），
    /// 不属于正式玩法，后续阶段可移除。需放置在泥土或草皮上。
    /// </summary>
    public class TestGeneratingFlowerBlock : FlowerBlock, IPhytoFlowerBlock {
        public Texture2D m_texture;

        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = false;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/Mana2");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

        public override Texture2D GetDefaultTexture(int value) => m_texture;

        public TilePhytoFlower CreateFlower(Point3 position) => new TileTestGeneratingFlower(position);
    }
}
