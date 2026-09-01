using Engine;

namespace Game {
    public class AStar<T> {
        public class Node {
            public T Position;

            public T PreviousPosition;

            public float F;

            public float G;

            public float H;
        }

        public int m_nodesCacheIndex;

        public DynamicArray<Node> m_nodesCache = [];

        public DynamicArray<Node> m_openHeap = [];

        public DynamicArray<T> m_neighbors = [];

        public float PathCost { get; set; }

        public DynamicArray<T> Path { get; set; }

        public IAStarWorld<T> World { get; set; }

        public IAStarStorage<T> OpenStorage { get; set; }

        public IAStarStorage<T> ClosedStorage { get; set; }

        public void BuildPathFromEndNode(Node startNode, Node endNode) {
            PathCost = endNode.G;
            Path.Clear();
            for (Node node = endNode; node != startNode; node = (Node)ClosedStorage.Get(node.PreviousPosition)) {
                Path.Add(node.Position);
            }
        }

        public void FindPath(T start, T end, float minHeuristic, int maxPositionsToCheck) {
            if (Path == null) {
                throw new InvalidOperationException("Path not specified.");
            }
            if (World == null) {
                throw new InvalidOperationException("AStar World not specified.");
            }
            if (OpenStorage == null) {
                throw new InvalidOperationException("AStar OpenStorage not specified.");
            }
            if (OpenStorage == null) {
                throw new InvalidOperationException("AStar ClosedStorage not specified.");
            }
            m_nodesCacheIndex = 0;
            m_openHeap.Clear();
            OpenStorage.Clear();
            ClosedStorage.Clear();
            Node node = NewNode(start, default, 0f, 0f);
            OpenStorage.Set(start, node);
            HeapEnqueue(node);
            Node node2 = null;
            int num = 0;
            Node node3;
            while (true) {
                node3 = m_openHeap.Count > 0 ? HeapDequeue() : null;
                if (node3 == null
                    || num >= maxPositionsToCheck) {
                    if (node2 != null) {
                        BuildPathFromEndNode(node, node2);
                        return;
                    }
                    Path.Clear();
                    PathCost = 0f;
                    return;
                }
                if (World.IsGoal(node3.Position)) {
                    break;
                }
                ClosedStorage.Set(node3.Position, node3);
                OpenStorage.Set(node3.Position, null);
                num++;
                m_neighbors.Clear();
                World.Neighbors(node3.Position, m_neighbors);
                for (int i = 0; i < m_neighbors.Count; i++) {
                    T val = m_neighbors.Array[i];
                    if (ClosedStorage.Get(val) != null) {
                        continue;
                    }
                    float num2 = World.Cost(node3.Position, val);
                    if (num2 == 1f / 0f) {
                        continue;
                    }
                    float num3 = node3.G + num2;
                    float num4 = World.Heuristic(val, end);
                    if (node3 != node
                        && (node2 == null || num4 < node2.H)) {
                        node2 = node3;
                    }
                    Node node4 = (Node)OpenStorage.Get(val);
                    if (node4 != null) {
                        if (num3 < node4.G) {
                            node4.G = num3;
                            node4.F = num3 + node4.H;
                            node4.PreviousPosition = node3.Position;
                            HeapUpdate(node4);
                        }
                    }
                    else {
                        node4 = NewNode(val, node3.Position, num3, num4);
                        OpenStorage.Set(val, node4);
                        HeapEnqueue(node4);
                    }
                }
            }
            BuildPathFromEndNode(node, node3);
        }

        public void HeapEnqueue(Node node) {
            m_openHeap.Add(node);
            HeapifyFromPosToStart(m_openHeap.Count - 1);
        }

        public Node HeapDequeue() {
            Node result = m_openHeap.Array[0];
            if (m_openHeap.Count <= 1) {
                m_openHeap.Clear();
                return result;
            }
            m_openHeap.Array[0] = m_openHeap.Array[m_openHeap.Count - 1];
            --m_openHeap.Count;
            HeapifyFromPosToEnd(0);
            return result;
        }

        public void HeapUpdate(Node node) {
            int pos = -1;
            for (int i = 0; i < m_openHeap.Count; i++) {
                if (m_openHeap.Array[i] == node) {
                    pos = i;
                    break;
                }
            }
            HeapifyFromPosToStart(pos);
        }

        public void HeapifyFromPosToEnd(int pos) {
            while (true) {
                int num = pos;
                int num2 = 2 * pos + 1;
                int num3 = 2 * pos + 2;
                if (num2 < m_openHeap.Count
                    && m_openHeap.Array[num2].F < m_openHeap.Array[num].F) {
                    num = num2;
                }
                if (num3 < m_openHeap.Count
                    && m_openHeap.Array[num3].F < m_openHeap.Array[num].F) {
                    num = num3;
                }
                if (num != pos) {
                    Node node = m_openHeap.Array[num];
                    m_openHeap.Array[num] = m_openHeap.Array[pos];
                    m_openHeap.Array[pos] = node;
                    pos = num;
                    continue;
                }
                break;
            }
        }

        public void HeapifyFromPosToStart(int pos) {
            int num = pos;
            while (num > 0) {
                int num2 = (num - 1) / 2;
                Node node = m_openHeap.Array[num2];
                Node node2 = m_openHeap.Array[num];
                if (node.F > node2.F) {
                    m_openHeap.Array[num2] = node2;
                    m_openHeap.Array[num] = node;
                    num = num2;
                    continue;
                }
                break;
            }
        }

        public Node NewNode(T position, T previousPosition, float g, float h) {
            while (m_nodesCacheIndex >= m_nodesCache.Count) {
                m_nodesCache.Add(new Node());
            }
            Node obj = m_nodesCache.Array[m_nodesCacheIndex++];
            obj.Position = position;
            obj.PreviousPosition = previousPosition;
            obj.F = g + h;
            obj.G = g;
            obj.H = h;
            return obj;
        }
    }
}