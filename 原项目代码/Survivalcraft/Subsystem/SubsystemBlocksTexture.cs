using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemBlocksTexture : Subsystem {
        public Texture2D BlocksTexture { get; set; }

        public override void Load(ValuesDictionary valuesDictionary) {
            Display.DeviceReset += Display_DeviceReset;
            LoadBlocksTexture();
        }

        public override void Dispose() {
            Display.DeviceReset -= Display_DeviceReset;
            DisposeBlocksTexture();
        }

        public void LoadBlocksTexture() {
            SubsystemGameInfo subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            BlocksTexture = BlocksTexturesManager.LoadTexture(subsystemGameInfo.WorldSettings.BlocksTextureName);
        }

        public void DisposeBlocksTexture() {
            if (BlocksTexture != null
                && !ContentManager.IsContent(BlocksTexture)) {
                BlocksTexture.Dispose();
                BlocksTexture = null;
            }
        }

        public void Display_DeviceReset() {
            LoadBlocksTexture();
        }
    }
}