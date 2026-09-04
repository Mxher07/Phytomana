using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 框架自测用产魔行为：按固定速率向节点缓存注入魔力，其余投递全部交由魔力网络。
    /// </summary>
    public class SubsystemTestManaSourceBehavior : SubsystemBlockBehavior, IUpdateable {
        public const float ManaRate = 40f;

        public ManaNetworkManager m_network;

        public Dictionary<Point3, TestManaSource> m_sources = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<TestManaSourceBlock>()];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddSource(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddSource(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveSource(new Point3(x, y, z), true);
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = [];
            foreach (Point3 point in m_sources.Keys) {
                if (point.X >= chunk.Origin.X
                    && point.X < chunk.Origin.X + 16
                    && point.Z >= chunk.Origin.Y
                    && point.Z < chunk.Origin.Y + 16) {
                    list.Add(point);
                }
            }
            foreach (Point3 point in list) {
                RemoveSource(point, false);
            }
        }

        public void Update(float dt) {
            foreach (TestManaSource source in m_sources.Values) {
                source.ManaStorage.TryAdd(ManaRate * dt);
            }
        }

        public void AddSource(int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (m_sources.ContainsKey(point)) {
                return;
            }
            TestManaSource source = new(point);
            m_sources[point] = source;
            m_network.RegisterSource(source);
        }

        public void RemoveSource(Point3 point, bool destroyed) {
            if (!m_sources.TryGetValue(point, out TestManaSource source)) {
                return;
            }
            m_sources.Remove(point);
            m_network.UnregisterSource(source, destroyed);
        }
    }
}
