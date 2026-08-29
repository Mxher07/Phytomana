using Engine;
using Engine.Graphics;

namespace Game {
    public class GrownWoodBlock : CubeBlock {
        public Texture2D m_texture;
        public override void Initialize() {
            base.Initialize();
            CanBeBuiltIntoFurniture = true;
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/GrownWood");
            int contents = BlocksManager.GetBlockIndex<GrownWoodBlock>();
            Log.Information($"[PhytoMana]{contents} Registered.");
        }
        public override int GetFaceTextureSlot(int face, int value) => 0;
        public override int GetTextureSlotCount(int value) => 1;
        public override Texture2D GetDefaultTexture(int value) => m_texture;
    }
}