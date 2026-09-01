using Engine;
using Engine.Graphics;

namespace Game {
    public class Subtexture {
        public readonly Texture2D Texture;

        public readonly Vector2 TopLeft;

        public readonly Vector2 BottomRight;

        public Subtexture(Texture2D texture, Vector2 topLeft, Vector2 bottomRight) {
            Texture = texture;
            TopLeft = topLeft;
            BottomRight = bottomRight;
        }

        public Subtexture(Texture2D texture) {
            Texture = texture;
            TopLeft = Vector2.Zero;
            BottomRight = Vector2.One;
        }

        public static implicit operator Texture2D(Subtexture subtexture) => subtexture.Texture;

        public static implicit operator Subtexture(Texture2D texture) => new(texture);
    }
}