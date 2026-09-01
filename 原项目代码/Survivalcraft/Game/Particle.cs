using Engine;

namespace Game {
    public class Particle {
        public bool IsActive;

        public Vector3 Position;

        public Vector3 Right;

        public Vector3 Up;

        public Vector2 Size;

        public float Rotation;

        public Color Color;

        public int TextureSlot;

        public bool UseAdditiveBlending;

        public bool FlipX;

        public bool FlipY;

        public ParticleBillboardingMode BillboardingMode;
    }
}