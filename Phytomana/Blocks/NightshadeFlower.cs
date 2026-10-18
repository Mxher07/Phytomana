using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 夜影花：被动产魔花，与日耀花互补，仅在夜晚缓慢产出魔力。
    /// </summary>
    public class NightshadeFlower : PhytoCrossBlock, IPhytoFlowerBlock {
    public static int Index = 322;

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/Nightshade");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public TilePhytoFlower CreateFlower(Point3 position) => new TileNightshadeFlower(position);
    }
}