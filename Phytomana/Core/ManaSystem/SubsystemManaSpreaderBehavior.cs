using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 魔力发射器网络接线：放置/区块加载时注册进魔力网络，破坏/区块卸载时注销。
    /// 本字典持有节点的强引用，网络侧仅弱引用，区块卸载后不会泄漏。
    /// </summary>
    public class SubsystemManaSpreaderBehavior : SubsystemBlockBehavior {
        public ManaNetworkManager m_network;

        public SubsystemMana m_subsystemMana;

        public Dictionary<Point3, ManaSpreader> m_spreaders = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<ManaSpreaderBlock>()];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddSpreader(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddSpreader(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveSpreader(new Point3(x, y, z), true);
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = [];
            foreach (Point3 point in m_spreaders.Keys) {
                if (point.X >= chunk.Origin.X
                    && point.X < chunk.Origin.X + 16
                    && point.Z >= chunk.Origin.Y
                    && point.Z < chunk.Origin.Y + 16) {
                    list.Add(point);
                }
            }
            foreach (Point3 point in list) {
                RemoveSpreader(point, false);
            }
        }

        public void AddSpreader(int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (m_spreaders.ContainsKey(point)) {
                return;
            }
            ManaSpreader spreader = new(point);
            m_spreaders[point] = spreader;
            m_network.RegisterReceiver(spreader);
            if (spreader.ManaStorage.IsEmpty && m_subsystemMana.TakeLegacyMana(point, out float legacyAmount)) {
                spreader.ManaStorage.LoadData(legacyAmount);
            }
        }

        public void RemoveSpreader(Point3 point, bool destroyed) {
            if (!m_spreaders.TryGetValue(point, out ManaSpreader spreader)) {
                return;
            }
            m_spreaders.Remove(point);
            m_network.UnregisterReceiver(spreader, destroyed);
        }
    }
}
