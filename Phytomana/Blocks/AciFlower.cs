using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 剧毒树丛：范围毒攻功能花。行为见 TileAciFlower。
    /// </summary>
    public class AciFlower : PhytoCrossBlock, IPhytoFlowerBlock {
        
        public static int Index = 300;
        
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/AciFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public TilePhytoFlower CreateFlower(Point3 position) => new TileAciFlower(position);
    }
}
