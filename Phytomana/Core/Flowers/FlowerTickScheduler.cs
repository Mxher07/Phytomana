using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 全局花朵 Tick 调度器。每个 World（Project）一个实例：
    /// 维护所有存活花朵，分片轮询调用 FlowerTick（禁止花朵自行挂帧更新）；
    /// 区块卸载的花朵转入休眠存档，重新加载时还原；世界卸载时清理全部数据。
    /// </summary>
    public class FlowerTickScheduler : Subsystem, IUpdateable {
        public const float DefaultTickInterval = 0.25f;
        public const int DefaultFlowersPerSlice = 16;
        public const string SaveKey = "PhytoFlowers";

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemGameInfo m_subsystemGameInfo;

        public ManaNetworkManager m_network;

        public SubsystemMana m_subsystemMana;

        public List<TilePhytoFlower> m_flowers = [];

        public Dictionary<Point3, ValuesDictionary> m_dormantFlowers = [];

        public List<TilePhytoFlower> m_sliceBuffer = [];

        public int m_cursor;

        public float m_timer;

        /// <summary>
        /// 本帧游戏时间缓存，花朵经 TilePhytoFlower.TotalTime 读取。
        /// </summary>
        public double CurrentTime { get; private set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(false);
            CurrentTime = m_subsystemGameInfo.TotalElapsedGameTime;
            ValuesDictionary root = valuesDictionary.GetValue<ValuesDictionary>(SaveKey, null);
            if (root == null) {
                return;
            }
            foreach (KeyValuePair<string, object> pair in root) {
                if (pair.Value is not ValuesDictionary data) {
                    continue;
                }
                string[] array = pair.Key.Split([','], StringSplitOptions.None);
                if (array.Length != 3
                    || !int.TryParse(array[0], out int x)
                    || !int.TryParse(array[1], out int y)
                    || !int.TryParse(array[2], out int z)) {
                    continue;
                }
                m_dormantFlowers[new Point3(x, y, z)] = data;
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            ValuesDictionary root = [];
            foreach (TilePhytoFlower flower in m_flowers) {
                ValuesDictionary data = [];
                flower.SaveData(data);
                root.SetValue(FlowerKey(flower.Position), data);
            }
            foreach (KeyValuePair<Point3, ValuesDictionary> pair in m_dormantFlowers) {
                root.SetValue(FlowerKey(pair.Key), pair.Value);
            }
            valuesDictionary.SetValue(SaveKey, root);
        }

        public override void Dispose() {
            m_flowers.Clear();
            m_dormantFlowers.Clear();
            m_sliceBuffer.Clear();
        }

        /// <summary>
        /// 注册花朵：还原休眠存档（或迁移旧版存档魔力）、接入魔力网络、加入调度列表。
        /// </summary>
        public void RegisterFlower(TilePhytoFlower flower) {
            if (flower == null) {
                return;
            }
            for (int i = m_flowers.Count - 1; i >= 0; i--) {
                if (m_flowers[i].Position == flower.Position) {
                    UnregisterFlower(m_flowers[i], false);
                }
            }
            flower.Scheduler = this;
            flower.LastTickTime = CurrentTime;
            if (m_dormantFlowers.TryGetValue(flower.Position, out ValuesDictionary data)) {
                m_dormantFlowers.Remove(flower.Position);
                if (data.GetValue("Type", string.Empty) == flower.GetType().Name) {
                    flower.LoadData(data);
                }
            }
            else {
                MigrateLegacyMana(flower);
            }
            m_flowers.Add(flower);
            if (flower is IManaSource source) {
                m_network.RegisterSource(source);
            }
            if (flower is TileFunctionalFlower functional) {
                if (functional.ReceivesMana) {
                    m_network.RegisterReceiver(functional);
                }
            }
            else if (flower is IManaReceiver receiver) {
                m_network.RegisterReceiver(receiver);
            }
        }

        /// <summary>
        /// 注销花朵：退出魔力网络与调度列表；区块卸载转入休眠存档，方块破坏则丢弃。
        /// </summary>
        public void UnregisterFlower(TilePhytoFlower flower, bool destroyed) {
            if (flower == null) {
                return;
            }
            m_flowers.Remove(flower);
            if (flower is IManaSource source) {
                m_network.UnregisterSource(source, destroyed);
            }
            if (flower is TileFunctionalFlower functional) {
                if (functional.ReceivesMana) {
                    m_network.UnregisterReceiver(functional, destroyed);
                }
            }
            else if (flower is IManaReceiver receiver) {
                m_network.UnregisterReceiver(receiver, destroyed);
            }
            if (destroyed) {
                m_dormantFlowers.Remove(flower.Position);
            }
            else {
                ValuesDictionary data = [];
                flower.SaveData(data);
                m_dormantFlowers[flower.Position] = data;
            }
            flower.Scheduler = null;
        }

        public bool TryGetFlower(Point3 point, out TilePhytoFlower flower) {
            flower = null;
            foreach (TilePhytoFlower candidate in m_flowers) {
                if (candidate.Position == point) {
                    flower = candidate;
                    return true;
                }
            }
            return false;
        }

        public void Update(float dt) {
            CurrentTime = m_subsystemGameInfo.TotalElapsedGameTime;
            m_timer += dt;
            if (m_timer < PhytoConfig.Instance.FlowerTickInterval) {
                return;
            }
            m_timer = 0f;
            TickSlice();
        }

        /// <summary>
        /// 分片轮询：每次最多调度 FlowersPerSlice 朵花，游标回绕，花朵越多单花周期越长。
        /// </summary>
        public void TickSlice() {
            if (m_flowers.Count == 0) {
                m_cursor = 0;
                return;
            }
            int count = Math.Min(PhytoConfig.Instance.FlowersPerSlice, m_flowers.Count);
            m_sliceBuffer.Clear();
            for (int i = 0; i < count; i++) {
                m_sliceBuffer.Add(m_flowers[(m_cursor + i) % m_flowers.Count]);
            }
            m_cursor = (m_cursor + count) % m_flowers.Count;
            foreach (TilePhytoFlower flower in m_sliceBuffer) {
                if (flower.Scheduler != null) {
                    flower.ScheduledTick(CurrentTime);
                }
            }
        }

        /// <summary>
        /// 旧版存档中花朵魔力曾记在 SubsystemMana 字典里；注册时迁移进花朵缓存并清除残留。
        /// </summary>
        public void MigrateLegacyMana(TilePhytoFlower flower) {
            if (m_subsystemMana == null) {
                return;
            }
            ManaStorage storage = null;
            if (flower is TileGeneratingFlower generating) {
                storage = generating.ManaStorage;
            }
            else if (flower is TileFunctionalFlower functional && functional.ReceivesMana) {
                storage = functional.ManaStorage;
            }
            if (storage == null || !storage.IsEmpty) {
                return;
            }
            if (m_subsystemMana.TakeLegacyMana(flower.Position, out float amount) && amount > 0f) {
                storage.LoadData(Math.Min(amount, storage.Max));
            }
        }

        public static string FlowerKey(Point3 point) => $"{point.X},{point.Y},{point.Z}";
    }
}
