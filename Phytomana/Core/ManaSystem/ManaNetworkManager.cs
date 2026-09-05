using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 魔力网络管理器。每个 World（Project）一个实例，天然按世界隔离，不做全局共享。
    /// 以弱引用保存产魔源与接收器：区块卸载后业务侧释放强引用，网络侧不会阻止回收。
    /// </summary>
    public class ManaNetworkManager : Subsystem, IUpdateable {
        public const float DefaultTransferInterval = 0.25f;
        public const float DefaultTransferDistance = 12f;
        public const string SaveKey = "ManaStorages";

        public SubsystemTerrain m_subsystemTerrain;

        public List<WeakReference<IManaSource>> m_sources = [];

        public List<WeakReference<IManaReceiver>> m_receivers = [];

        public Dictionary<Point3, float> m_dormantMana = [];

        public List<IManaReceiver> m_targetBuffer = [];

        public float m_transferTimer;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            string text = valuesDictionary.GetValue(SaveKey, string.Empty);
            foreach (string item in text.Split([';'], StringSplitOptions.RemoveEmptyEntries)) {
                string[] array = item.Split([','], StringSplitOptions.None);
                if (array.Length != 4
                    || !int.TryParse(array[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    || !int.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                    || !int.TryParse(array[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int z)
                    || !float.TryParse(array[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount)) {
                    continue;
                }
                if (amount > 0f) {
                    m_dormantMana[new Point3(x, y, z)] = amount;
                }
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            StringBuilder builder = new();
            HashSet<Point3> savedPositions = [];
            foreach (WeakReference<IManaSource> reference in m_sources) {
                if (reference.TryGetTarget(out IManaSource source)
                    && source.ManaStorage.Current > 0f
                    && savedPositions.Add(source.Position)) {
                    AppendMana(builder, source.Position, source.ManaStorage.Current);
                }
            }
            foreach (WeakReference<IManaReceiver> reference in m_receivers) {
                if (reference.TryGetTarget(out IManaReceiver receiver)
                    && receiver.ManaStorage.Current > 0f
                    && savedPositions.Add(receiver.Position)) {
                    AppendMana(builder, receiver.Position, receiver.ManaStorage.Current);
                }
            }
            foreach (KeyValuePair<Point3, float> pair in m_dormantMana) {
                if (savedPositions.Add(pair.Key)) {
                    AppendMana(builder, pair.Key, pair.Value);
                }
            }
            valuesDictionary.SetValue(SaveKey, builder.ToString());
        }

        public override void Dispose() {
            m_sources.Clear();
            m_receivers.Clear();
            m_dormantMana.Clear();
            m_targetBuffer.Clear();
        }

        public void RegisterSource(IManaSource source) {
            if (source == null) {
                return;
            }
            AttachDormantMana(source.Position, source.ManaStorage);
            for (int i = 0; i < m_sources.Count; i++) {
                if (m_sources[i].TryGetTarget(out IManaSource existing) && existing.Position == source.Position) {
                    m_sources[i] = new WeakReference<IManaSource>(source);
                    return;
                }
            }
            m_sources.Add(new WeakReference<IManaSource>(source));
        }

        public void RegisterReceiver(IManaReceiver receiver) {
            if (receiver == null) {
                return;
            }
            AttachDormantMana(receiver.Position, receiver.ManaStorage);
            for (int i = 0; i < m_receivers.Count; i++) {
                if (m_receivers[i].TryGetTarget(out IManaReceiver existing) && existing.Position == receiver.Position) {
                    m_receivers[i] = new WeakReference<IManaReceiver>(receiver);
                    return;
                }
            }
            m_receivers.Add(new WeakReference<IManaReceiver>(receiver));
        }

        /// <param name="destroyed">true 表示方块被破坏（魔力丢弃）；false 表示区块卸载（魔力休眠保留）。</param>
        public void UnregisterSource(IManaSource source, bool destroyed) {
            if (source == null) {
                return;
            }
            for (int i = m_sources.Count - 1; i >= 0; i--) {
                if (!m_sources[i].TryGetTarget(out IManaSource existing)) {
                    m_sources.RemoveAt(i);
                    continue;
                }
                if (ReferenceEquals(existing, source)) {
                    m_sources.RemoveAt(i);
                    break;
                }
            }
            StoreDormantMana(source.Position, source.ManaStorage.Current, destroyed);
        }

        /// <param name="destroyed">true 表示方块被破坏（魔力丢弃）；false 表示区块卸载（魔力休眠保留）。</param>
        public void UnregisterReceiver(IManaReceiver receiver, bool destroyed) {
            if (receiver == null) {
                return;
            }
            for (int i = m_receivers.Count - 1; i >= 0; i--) {
                if (!m_receivers[i].TryGetTarget(out IManaReceiver existing)) {
                    m_receivers.RemoveAt(i);
                    continue;
                }
                if (ReferenceEquals(existing, receiver)) {
                    m_receivers.RemoveAt(i);
                    break;
                }
            }
            StoreDormantMana(receiver.Position, receiver.ManaStorage.Current, destroyed);
        }

        public bool TryGetReceiverStorage(Point3 point, out ManaStorage storage) {
            storage = null;
            foreach (WeakReference<IManaReceiver> reference in m_receivers) {
                if (reference.TryGetTarget(out IManaReceiver receiver) && receiver.Position == point) {
                    storage = receiver.ManaStorage;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 查询某坐标的魔力存量：优先活跃接收器，其次休眠数据（区块未加载/存档待还原）。
        /// </summary>
        public bool TryGetMana(Point3 point, out float amount) {
            foreach (WeakReference<IManaReceiver> reference in m_receivers) {
                if (reference.TryGetTarget(out IManaReceiver receiver) && receiver.Position == point) {
                    amount = receiver.ManaStorage.Current;
                    return true;
                }
            }
            foreach (WeakReference<IManaSource> reference in m_sources) {
                if (reference.TryGetTarget(out IManaSource source) && source.Position == point) {
                    amount = source.ManaStorage.Current;
                    return true;
                }
            }
            return m_dormantMana.TryGetValue(point, out amount);
        }

        public float GetStoredMana(Point3 point) => TryGetMana(point, out float amount) ? amount : 0f;

        public void GetActiveReceivers(List<IManaReceiver> buffer) {
            buffer.Clear();
            for (int i = m_receivers.Count - 1; i >= 0; i--) {
                if (m_receivers[i].TryGetTarget(out IManaReceiver receiver)) {
                    buffer.Add(receiver);
                }
                else {
                    m_receivers.RemoveAt(i);
                }
            }
        }

        public void Update(float dt) {
            m_transferTimer += dt;
            if (m_transferTimer < PhytoConfig.Instance.TransferInterval) {
                return;
            }
            m_transferTimer = 0f;
            TransferAll();
        }

        public void TransferAll() {
            for (int i = m_sources.Count - 1; i >= 0; i--) {
                if (!m_sources[i].TryGetTarget(out IManaSource source)) {
                    m_sources.RemoveAt(i);
                    continue;
                }
                TransferFromSource(source);
            }
            for (int i = m_receivers.Count - 1; i >= 0; i--) {
                if (!m_receivers[i].TryGetTarget(out IManaReceiver _)) {
                    m_receivers.RemoveAt(i);
                }
            }
        }

        public void TransferFromSource(IManaSource source) {
            ManaStorage sourceStorage = source.ManaStorage;
            if (sourceStorage.IsEmpty) {
                return;
            }
            m_targetBuffer.Clear();
            float totalFree = 0f;
            foreach (WeakReference<IManaReceiver> reference in m_receivers) {
                if (!reference.TryGetTarget(out IManaReceiver receiver)) {
                    continue;
                }
                if (receiver.Position == source.Position) {
                    continue;
                }
                ManaStorage storage = receiver.ManaStorage;
                if (storage.Free <= 0f) {
                    continue;
                }
                if (!IsInRange(source.Position, receiver.Position)) {
                    continue;
                }
                if (!HasLineOfSight(source.Position, receiver.Position)) {
                    continue;
                }
                m_targetBuffer.Add(receiver);
                totalFree += storage.Free;
            }
            if (m_targetBuffer.Count == 0 || totalFree <= 0f) {
                return;
            }
            float remaining = Math.Min(sourceStorage.Current, totalFree);
            int guard = m_targetBuffer.Count + 1;
            while (remaining > 0.001f && m_targetBuffer.Count > 0 && guard > 0) {
                guard--;
                float share = remaining / m_targetBuffer.Count;
                for (int i = m_targetBuffer.Count - 1; i >= 0; i--) {
                    ManaStorage storage = m_targetBuffer[i].ManaStorage;
                    float give = Math.Min(Math.Min(share, storage.Free), remaining);
                    if (give <= 0f) {
                        m_targetBuffer.RemoveAt(i);
                        continue;
                    }
                    ManaTransmitEvent transmitEvent = new(source.Position, m_targetBuffer[i].Position, give);
                    PhytoEventBus.Fire(transmitEvent);
                    if (transmitEvent.Cancelled) {
                        m_targetBuffer.RemoveAt(i);
                        continue;
                    }
                    give = Math.Min(Math.Min(transmitEvent.Amount, storage.Free), remaining);
                    if (give <= 0f) {
                        m_targetBuffer.RemoveAt(i);
                        continue;
                    }
                    storage.TryAdd(give);
                    sourceStorage.Take(give);
                    remaining -= give;
                    if (storage.Free <= 0f) {
                        m_targetBuffer.RemoveAt(i);
                    }
                }
            }
        }

        public static bool IsInRange(Point3 from, Point3 to) {
            float distance = PhytoConfig.Instance.TransferDistance;
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float dz = to.Z - from.Z;
            return (dx * dx) + (dy * dy) + (dz * dz) <= distance * distance;
        }

        /// <summary>
        /// 该产魔源当前是否存在可达（距离内且视线无遮挡）的接收器，用于花朵孤立判定。
        /// </summary>
        public bool HasReachableReceiver(IManaSource source) {
            if (source == null) {
                return false;
            }
            foreach (WeakReference<IManaReceiver> reference in m_receivers) {
                if (!reference.TryGetTarget(out IManaReceiver receiver)) {
                    continue;
                }
                if (!IsInRange(source.Position, receiver.Position)) {
                    continue;
                }
                if (!HasLineOfSight(source.Position, receiver.Position)) {
                    continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 视线遮挡检测：对两端点之间的体素做 DDA 穿行，途经任何可碰撞方块即视为遮挡（端点自身方块不计）。
        /// </summary>
        public bool HasLineOfSight(Point3 from, Point3 to) {
            if (from == to) {
                return true;
            }
            Terrain terrain = m_subsystemTerrain.Terrain;
            int dirX = to.X - from.X;
            int dirY = to.Y - from.Y;
            int dirZ = to.Z - from.Z;
            int stepX = Math.Sign(dirX);
            int stepY = Math.Sign(dirY);
            int stepZ = Math.Sign(dirZ);
            float tDeltaX = dirX == 0 ? float.MaxValue : Math.Abs(1f / dirX);
            float tDeltaY = dirY == 0 ? float.MaxValue : Math.Abs(1f / dirY);
            float tDeltaZ = dirZ == 0 ? float.MaxValue : Math.Abs(1f / dirZ);
            float tMaxX = dirX == 0 ? float.MaxValue : 0.5f * tDeltaX;
            float tMaxY = dirY == 0 ? float.MaxValue : 0.5f * tDeltaY;
            float tMaxZ = dirZ == 0 ? float.MaxValue : 0.5f * tDeltaZ;
            int x = from.X;
            int y = from.Y;
            int z = from.Z;
            while (x != to.X || y != to.Y || z != to.Z) {
                if (tMaxX <= tMaxY && tMaxX <= tMaxZ) {
                    x += stepX;
                    tMaxX += tDeltaX;
                }
                else if (tMaxY <= tMaxZ) {
                    y += stepY;
                    tMaxY += tDeltaY;
                }
                else {
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                }
                if (x == to.X && y == to.Y && z == to.Z) {
                    break;
                }
                int value = terrain.GetCellValue(x, y, z);
                int contents = Terrain.ExtractContents(value);
                if (contents != 0 && BlocksManager.Blocks[contents].IsCollidable_(value)) {
                    return false;
                }
            }
            return true;
        }

        public void AttachDormantMana(Point3 point, ManaStorage storage) {
            if (m_dormantMana.TryGetValue(point, out float amount)) {
                storage.LoadData(amount);
                m_dormantMana.Remove(point);
            }
        }

        public void StoreDormantMana(Point3 point, float amount, bool destroyed) {
            if (!destroyed && amount > 0f) {
                m_dormantMana[point] = amount;
            }
            else {
                m_dormantMana.Remove(point);
            }
        }

        public static void AppendMana(StringBuilder builder, Point3 point, float amount) {
            builder.Append(point.X.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(point.Y.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(point.Z.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(amount.ToString(CultureInfo.InvariantCulture));
            builder.Append(';');
        }
    }
}
