using Engine;
using Engine.Graphics;

namespace Game {
    public class FlourBlock : FlatBlock {
        public static int Index = 175;

        public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData) {
            BlocksManager.DrawFlatOrImageExtrusionBlock(
                primitivesRenderer,
                value,
                size,
                ref matrix,
                GetDefaultTexture(value),
                color * new Color(248, 255, 232),
                false,
                environmentData
            );
        }
    }
}