using Engine;

namespace Game {
    public class PathfindingResult {
        public volatile bool IsCompleted;

        public bool IsInProgress;

        public float PathCost;

        public int PositionsChecked;

        public DynamicArray<Vector3> Path = [];
    }
}