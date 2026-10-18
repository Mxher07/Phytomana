using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 传输箱洋：无视方块瞬间运输掉落物的功能花。行为见 TileTransChestV2Flower。
    /// </summary>
    public class TransChestV2Flower : PhytoCrossBlock, IPhytoFlowerBlock {
    public static int Index = 666;

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/TransChestV2Flower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public TilePhytoFlower CreateFlower(Point3 position) => new TileTransChestV2Flower(position);
    }
}
