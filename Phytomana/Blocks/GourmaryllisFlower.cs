using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 暴食花：吞食掉落的食物，消化片刻后按食物营养价值产出魔力。
    /// </summary>
    public class GourmaryllisFlower : PhytoCrossBlock, IPhytoFlowerBlock {
public static int Index = 306;
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/Gourmaryllis");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public TilePhytoFlower CreateFlower(Point3 position) => new TileGourmaryllisFlower(position);
    }
}