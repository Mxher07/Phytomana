using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 黏土沙馨：消耗魔力把周围沙子变成粘土（物品形式掉落）的功能花。行为见 TileClaySandFlower。
    /// </summary>
    public class ClaySandFlower : PhytoCrossBlock, IPhytoFlowerBlock {
public static int Index = 303;
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ClaySandFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public TilePhytoFlower CreateFlower(Point3 position) => new TileClaySandFlower(position);
    }
}
