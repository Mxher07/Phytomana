using Engine.Core;
using Engine.Graphics;
using Phytomana;

namespace Game {
    /// <summary>
    /// 传箱花：拾取周围的掉落物并送入贴身箱子（功能花）。行为见 TileChestTransFlower。
    /// </summary>
    public class ChestTransFlower : PhytoCrossBlock, IPhytoFlowerBlock {
public static int Index = 302;
        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/ChestTransFlower");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;


        public TilePhytoFlower CreateFlower(Point3 position) => new TileChestTransFlower(position);
    }
}
