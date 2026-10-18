using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 荆棘之花：守卫型功能花，以魔力驱使荆棘刺击周围的敌对生物（不伤玩家）。
    /// 行为见 TileThornyRose。
    /// </summary>
    public class ThornyRoseFlower : PhytoCrossBlock, IPhytoFlowerBlock {
    public static int Index = 666;

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ThornyRose");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public TilePhytoFlower CreateFlower(Point3 position) => new TileThornyRose(position);
    }
}