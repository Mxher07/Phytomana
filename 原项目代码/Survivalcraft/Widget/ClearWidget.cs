using Engine;
using Engine.Graphics;

namespace Game {
    public class ClearWidget : Widget {
        public Color Color { get; set; }

        public float Depth { get; set; }

        public int Stencil { get; set; }

        public bool ClearColor { get; set; }

        public bool ClearDepth { get; set; }

        public bool ClearStencil { get; set; }

        public ClearWidget() {
            ClearColor = true;
            ClearDepth = true;
            ClearStencil = true;
            Color = Color.Black;
            Depth = 1f;
            Stencil = 0;
            IsHitTestVisible = false;
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
        }

        public override void Draw(DrawContext dc) {
            Display.Clear(
                ClearColor ? new Vector4?(new Vector4(Color)) : null,
                ClearDepth ? new float?(Depth) : null,
                ClearStencil ? new int?(Stencil) : null
            );
        }
    }
}