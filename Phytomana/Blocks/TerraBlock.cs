using Engine.Core;
using Engine.Graphics;

namespace Game {
    public class TerraBlock : PhytoCubeBlock {

        public override void Initialize() {
            base.Initialize();
            m_texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/SumeruIngotBlock");
            int contents = BlocksManager.GetBlockIndex<ManaBlock>();
            Log.Information($"[PhytoMana]{contents} Registered.");
        }

        public override int GetFaceTextureSlot(int face, int value) => 0;

        public override int GetTextureSlotCount(int value) => 1;

    }
}
