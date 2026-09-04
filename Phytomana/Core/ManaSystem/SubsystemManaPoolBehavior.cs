using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 魔法池网络接线：放置/区块加载时注册进魔力网络，破坏/区块卸载时注销。
    /// 本字典持有节点的强引用，网络侧仅弱引用，区块卸载后不会泄漏。
    /// </summary>
    public class SubsystemManaPoolBehavior : SubsystemBlockBehavior {
        public ManaNetworkManager m_network;

        public SubsystemMana m_subsystemMana;

        public Dictionary<Point3, ManaPool> m_pools = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<ManaPoolBlock>()];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddPool(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddPool(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemovePool(new Point3(x, y, z), true);
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = [];
            foreach (Point3 point in m_pools.Keys) {
                if (point.X >= chunk.Origin.X
                    && point.X < chunk.Origin.X + 16
                    && point.Z >= chunk.Origin.Y
                    && point.Z < chunk.Origin.Y + 16) {
                    list.Add(point);
                }
            }
            foreach (Point3 point in list) {
                RemovePool(point, false);
            }
        }

        public void AddPool(int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (m_pools.ContainsKey(point)) {
                return;
            }
            ManaPool pool = new(point);
            m_pools[point] = pool;
            m_network.RegisterReceiver(pool);
            if (pool.ManaStorage.IsEmpty && m_subsystemMana.TakeLegacyMana(point, out float legacyAmount)) {
                pool.ManaStorage.LoadData(legacyAmount);
            }
        }

        public void RemovePool(Point3 point, bool destroyed) {
            if (!m_pools.TryGetValue(point, out ManaPool pool)) {
                return;
            }
            m_pools.Remove(point);
            m_network.UnregisterReceiver(pool, destroyed);
        }
    }
}
