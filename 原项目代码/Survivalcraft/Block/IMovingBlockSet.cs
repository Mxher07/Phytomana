using Engine;

namespace Game {
    public interface IMovingBlockSet {
        Vector3 Position { get; }

        string Id { get; }

        object Tag { get; }

        Vector3 CurrentVelocity { get; }

        List<MovingBlock> Blocks { get; }

        public bool Stopped { get; }

        BoundingBox BoundingBox(bool extendToFillCells);

        void SetBlock(Point3 offset, int value);

        void Stop();
    }
}