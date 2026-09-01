using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemPathfinding : Subsystem {
        public class Request {
            public Vector3 Start;

            public Vector3 End;

            public float MinDistance;

            public Vector3 BoxSize;

            public bool IgnoreDoors;

            public int MaxPositionsToCheck;

            public PathfindingResult PathfindingResult;
        }

        public class Storage : IAStarStorage<Vector3> {
            public Dictionary<Vector3, object> Dictionary = [];

            public void Clear() {
                Dictionary.Clear();
            }

            public object Get(Vector3 p) {
                Dictionary.TryGetValue(p, out object value);
                return value;
            }

            public void Set(Vector3 p, object data) {
                Dictionary[p] = data;
            }
        }

        public class World : IAStarWorld<Vector3> {
            public SubsystemTerrain SubsystemTerrain;

            public Request Request;

            public float Cost(Vector3 p1, Vector3 p2) => 0.999f - 0.1f * Vector3.Dot(Vector3.Normalize(p2 - p1), Vector3.Normalize(Request.End - p1));

            public void Neighbors(Vector3 p, DynamicArray<Vector3> neighbors) {
                neighbors.Count = 0;
                AddNeighbor(neighbors, p, 1, 0);
                AddNeighbor(neighbors, p, -1, 0);
                AddNeighbor(neighbors, p, 0, -1);
                AddNeighbor(neighbors, p, 0, 1);
                AddNeighbor(neighbors, p, -1, -1);
                AddNeighbor(neighbors, p, 1, -1);
                AddNeighbor(neighbors, p, 1, 1);
                AddNeighbor(neighbors, p, -1, 1);
            }

            public float Heuristic(Vector3 p1, Vector3 p2) {
                float num = MathF.Abs(p1.X - p2.X);
                float num2 = MathF.Abs(p1.Z - p2.Z);
                if (num > num2) {
                    return 1.41f * num2 + 1f * (num - num2);
                }
                return 1.41f * num + 1f * (num2 - num);
            }

            public bool IsGoal(Vector3 p) => Vector3.DistanceSquared(p, Request.End) <= Request.MinDistance * Request.MinDistance;

            public void AddNeighbor(DynamicArray<Vector3> neighbors, Vector3 p, int dx, int dz) {
                float y = p.Y;
                float num = p.Y;
                int num2 = Terrain.ToCell(p.X) + dx;
                int num3 = Terrain.ToCell(p.Y);
                int num4 = Terrain.ToCell(p.Z) + dz;
                int cellValue = SubsystemTerrain.Terrain.GetCellValue(num2, num3, num4);
                int num5 = Terrain.ExtractContents(cellValue);
                Block block = BlocksManager.Blocks[num5];
                if (block.ShouldAvoid(cellValue)) {
                    return;
                }
                if (block.IsCollidable_(cellValue)
                    && (!Request.IgnoreDoors || (!(block is DoorBlock) && !(block is TrapdoorBlock)))) {
                    float blockWalkingHeight = GetBlockWalkingHeight(block, cellValue);
                    if (blockWalkingHeight > 0.5f
                        && (block.NoAutoJump || block.NoSmoothRise)) {
                        return;
                    }
                    y = num3 + blockWalkingHeight;
                    num = num3 + blockWalkingHeight;
                }
                else {
                    bool flag = false;
                    for (int num6 = -1; num6 >= -4; num6--) {
                        int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(num2, num3 + num6, num4);
                        int num7 = Terrain.ExtractContents(cellValue2);
                        Block block2 = BlocksManager.Blocks[num7];
                        if (block2.ShouldAvoid(cellValue2)) {
                            return;
                        }
                        if (block2.IsCollidable_(cellValue2)) {
                            y = num3 + num6 + 1;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag) {
                        return;
                    }
                }
                int num8 = dx == 0 || dz == 0 ? 2 : 3;
                Vector3 vector = new(p.X, num + 0.01f, p.Z);
                Vector3 v = new(num2 + 0.5f, num + 0.01f, num4 + 0.5f);
                Vector3 v2 = 1f / num8 * (v - vector);
                for (int i = 1; i <= num8; i++) {
                    Vector3 v3 = vector + i * v2;
                    BoundingBox box = new(
                        v3 - new Vector3(Request.BoxSize.X / 2f + 0.01f, 0f, Request.BoxSize.Z / 2f + 0.01f),
                        v3 + new Vector3(Request.BoxSize.X / 2f - 0.01f, Request.BoxSize.Y, Request.BoxSize.Z / 2f - 0.01f)
                    );
                    if (IsBlocked(box)) {
                        return;
                    }
                }
                neighbors.Add(new Vector3(num2 + 0.5f, y, num4 + 0.5f));
            }

            public float GetBlockWalkingHeight(Block block, int value) {
                if (block is DoorBlock
                    || block is FenceGateBlock) {
                    return 0f;
                }
                float num = 0f;
                BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(SubsystemTerrain, value);
                for (int i = 0; i < customCollisionBoxes.Length; i++) {
                    BoundingBox boundingBox = customCollisionBoxes[i];
                    num = MathUtils.Max(num, boundingBox.Max.Y);
                }
                return num;
            }

            public bool IsBlocked(BoundingBox box) {
                int num = Terrain.ToCell(box.Min.X);
                int num2 = MathUtils.Max(Terrain.ToCell(box.Min.Y), 0);
                int num3 = Terrain.ToCell(box.Min.Z);
                int num4 = Terrain.ToCell(box.Max.X);
                int num5 = MathUtils.Min(Terrain.ToCell(box.Max.Y), 255);
                int num6 = Terrain.ToCell(box.Max.Z);
                for (int i = num; i <= num4; i++) {
                    for (int j = num3; j <= num6; j++) {
                        TerrainChunk chunkAtCell = SubsystemTerrain.Terrain.GetChunkAtCell(i, j);
                        if (chunkAtCell == null) {
                            continue;
                        }
                        int num7 = TerrainChunk.CalculateCellIndex(i & 0xF, num2, j & 0xF);
                        int num8 = num2;
                        while (num8 <= num5) {
                            int cellValueFast = chunkAtCell.GetCellValueFast(num7);
                            int num9 = Terrain.ExtractContents(cellValueFast);
                            if (num9 != 0) {
                                Block block = BlocksManager.Blocks[num9];
                                if (block.ShouldAvoid(cellValueFast)) {
                                    return true;
                                }
                                if (block.IsCollidable_(cellValueFast)) {
                                    Vector3 v = new(i, num8, j);
                                    BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(SubsystemTerrain, cellValueFast);
                                    for (int k = 0; k < customCollisionBoxes.Length; k++) {
                                        BoundingBox boundingBox = customCollisionBoxes[k];
                                        if (box.Intersection(new BoundingBox(v + boundingBox.Min, v + boundingBox.Max))) {
                                            return true;
                                        }
                                    }
                                }
                            }
                            num8++;
                            num7++;
                        }
                    }
                }
                return false;
            }
        }

        public SubsystemTerrain m_subsystemTerrain;

        public Queue<Request> m_requests = new();

        public AStar<Vector3> m_astar = new();

        public void QueuePathSearch(Vector3 start,
            Vector3 end,
            float minDistance,
            Vector3 boxSize,
            bool ignoreDoors,
            int maxPositionsToCheck,
            PathfindingResult result) {
            lock (m_requests) {
                if (m_requests.Count < 10) {
                    result.IsCompleted = false;
                    result.IsInProgress = true;
                    m_requests.Enqueue(
                        new Request {
                            Start = start,
                            End = end,
                            MinDistance = minDistance,
                            BoxSize = boxSize,
                            IgnoreDoors = ignoreDoors,
                            MaxPositionsToCheck = maxPositionsToCheck,
                            PathfindingResult = result
                        }
                    );
                    Monitor.Pulse(m_requests);
                }
                else {
                    result.IsCompleted = true;
                    result.IsInProgress = false;
                    result.Path.Clear();
                    result.PathCost = 0f;
                    result.PositionsChecked = 0;
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            World world = new() { SubsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true) };
            m_astar.OpenStorage = new Storage();
            m_astar.ClosedStorage = new Storage();
            m_astar.World = world;
            Task.Run(ThreadFunction);
        }

        public override void Dispose() {
            lock (m_requests) {
                m_requests.Clear();
                m_requests.Enqueue(null);
                Monitor.Pulse(m_requests);
            }
        }
        public void ThreadFunction() {
            while (true) {
                Request request;
                lock (m_requests) {
                    while (m_requests.Count == 0) {
#pragma warning disable CA1416
                        Monitor.Wait(m_requests);
#pragma warning restore CA1416
                    }
                    request = m_requests.Dequeue();
                }
                if (request == null) {
                    break;
                }
                ProcessRequest(request);
                Task.Delay(250).Wait();
            }
        }


        public void ProcessRequest(Request request) {
            ((World)m_astar.World).Request = request;
            m_astar.Path = request.PathfindingResult.Path;
            _ = Time.RealTime;
            m_astar.FindPath(request.Start, request.End, request.MinDistance, request.MaxPositionsToCheck);
            _ = Time.RealTime;
            SmoothPath(m_astar.Path, request.BoxSize);
            _ = Time.RealTime;
            request.PathfindingResult.PathCost = m_astar.PathCost;
            request.PathfindingResult.PositionsChecked = ((Storage)m_astar.ClosedStorage).Dictionary.Count;
            request.PathfindingResult.IsInProgress = false;
            request.PathfindingResult.IsCompleted = true;
        }

        public void SmoothPath(DynamicArray<Vector3> path, Vector3 boxSize) {
            for (int num = path.Count - 2; num > 0; num--) {
                if (IsPassable(path.Array[num + 1], path.Array[num - 1], boxSize)) {
                    path.RemoveAt(num);
                }
            }
        }

        public bool IsPassable(Vector3 p1, Vector3 p2, Vector3 boxSize) {
            Vector3 vector = new(p1.X, p1.Y + 0.5f, p1.Z);
            Vector3 vector2 = new(p2.X, p2.Y + 0.5f, p2.Z);
            Vector3 v = (0.5f * boxSize.X + 0.1f) * Vector3.Normalize(Vector3.Cross(Vector3.UnitY, vector2 - vector));
            if (m_subsystemTerrain.Raycast(vector, vector2, false, true, SmoothingRaycastFunction_Obstacle).HasValue) {
                return false;
            }
            if (m_subsystemTerrain.Raycast(vector - v, vector2 - v, false, true, SmoothingRaycastFunction_Obstacle).HasValue) {
                return false;
            }
            if (m_subsystemTerrain.Raycast(vector + v, vector2 + v, false, true, SmoothingRaycastFunction_Obstacle).HasValue) {
                return false;
            }
            if (m_subsystemTerrain.Raycast(
                    vector + new Vector3(0f, -1f, 0f),
                    vector2 + new Vector3(0f, -1f, 0f),
                    false,
                    false,
                    SmoothingRaycastFunction_Support
                )
                .HasValue) {
                return false;
            }
            if (m_subsystemTerrain.Raycast(
                    vector + new Vector3(0f, -1f, 0f) - v,
                    vector2 + new Vector3(0f, -1f, 0f) - v,
                    false,
                    false,
                    SmoothingRaycastFunction_Support
                )
                .HasValue) {
                return false;
            }
            if (m_subsystemTerrain.Raycast(
                    vector + new Vector3(0f, -1f, 0f) + v,
                    vector2 + new Vector3(0f, -1f, 0f) + v,
                    false,
                    false,
                    SmoothingRaycastFunction_Support
                )
                .HasValue) {
                return false;
            }
            return true;
        }

        public static bool SmoothingRaycastFunction_Obstacle(int value, float distance) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            if (block.ShouldAvoid(value)) {
                return true;
            }
            if (block.IsCollidable_(value)) {
                return true;
            }
            return false;
        }

        public static bool SmoothingRaycastFunction_Support(int value, float distance) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            if (block.ShouldAvoid(value)) {
                return true;
            }
            if (!block.IsCollidable_(value)) {
                return true;
            }
            return false;
        }
    }
}