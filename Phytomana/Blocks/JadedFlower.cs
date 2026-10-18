using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 翡翠菜：消耗魔力在周围生成须弥花的功能花。行为见 TileJadedFlower。
    /// </summary>
    public class JadedFlower : PhytoCrossBlock, IPhytoFlowerBlock {
public static int Index = 312;
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/JadedFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public TilePhytoFlower CreateFlower(Point3 position) => new TileJadedFlower(position);
    }
}
