using Engine;

namespace Game {
    public struct BlockMeshVertex {
        public Vector3 Position;

        public Vector2 TextureCoordinates;

        public Color Color;

        public byte Face;

        public bool IsEmissive;
    }
}