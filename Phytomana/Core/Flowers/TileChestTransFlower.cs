using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 传箱花：拾取工作范围内的掉落物并送入贴身（上/前/后/左/右）未满的箱子。
    /// 单次拾取固定消耗 1mn（有魔力时）；无魔力也可工作。
    /// 无魔力时工作半径 6 格，有魔力时 10 格；魔力可由发射器绑链供应，
    /// 未绑链时自行从 23×23×23 内的魔法池取食（机制与荆棘之花相同）。
    /// 不会拾取花药台/符文台 1.5 格内的掉落物，避免干扰合成。
    /// 绑定模式下的须弥法杖点击传箱花可开关机。
    /// </summary>
    public class TileChestTransFlower : TileFunctionalFlower {
        public const float DefaultMaxMana = 30f;
        public const float PickupManaCost = 1f;
        public const float LowManaRange = 6f;
        public const float HighManaRange = 10f;
        public const double WorkInterval = 1.5;
        public const float TableProtectionRadius = 1.5f;
        public const float PoolSearchRange = 11f;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemBlockEntities m_subsystemBlockEntities;

        public SubsystemFlowerTableBehavior m_subsystemFlowerTables;

        public SubsystemRunesTableBehavior m_subsystemRunesTables;

        /// <summary>开关机状态（存档保存，默认开机）。</summary>
        public TileChestTransFlower(Point3 position) : base(position) {
            m_powered = true;
        }

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("ChestTransFlower", DefaultMaxMana);

        public override string PoweredMessageKey => "ChestTransMessages";

        public override string PoweredOnKey => "PowerOn";

        public override string PoweredOffKey => "PowerOff";

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            if (!m_powered || time < m_cooldown) {
                return;
            }
            m_cooldown = time + WorkInterval;
            // 未绑链时先自行从魔法池取食（储满即停）
            if (!HasIncomingLink() && !ManaStorage.IsFull) {
                TryDrawFromPool(PoolSearchRange);
            }
            float range = ManaStorage.Current > 0f ? HighManaRange : LowManaRange;
            List<ComponentChest> chests = GetAdjacentChests();
            if (chests.Count == 0) {
                return;
            }
            Vector3 center = new(Position.X + 0.5f, Position.Y + 0.5f, Position.Z + 0.5f);
            List<Pickable> absorbed = [];
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                Vector3 position = pickable.Position;
                if (MathF.Abs(position.X - center.X) > range
                    || MathF.Abs(position.Y - center.Y) > range
                    || MathF.Abs(position.Z - center.Z) > range) {
                    continue;
                }
                // 不拾取花药台/符文台 1.5 格内的掉落物，避免影响合成
                if (IsNearProtectedTable(position)) {
                    continue;
                }
                absorbed.Add(pickable);
            }
            if (absorbed.Count == 0) {
                return;
            }
            // 单次拾取固定消耗 1mn（有魔力的情况下）
            if (ManaStorage.Current >= PickupManaCost) {
                ManaStorage.Take(PickupManaCost);
            }
            Vector3 muzzle = new(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f);
            foreach (Pickable pickable in absorbed) {
                Vector3 itemPosition = pickable.Position;
                // 灰色引导粒子：从掉落物位置飞向花
                m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    itemPosition,
                    0.35f,
                    0.75f,
                    Color.LightGray,
                    muzzle,
                    1
                ));
                // 收进箱子（塞不下的余量重新弹出，避免丢失）
                pickable.ToRemove = true;
                int leftover = InsertIntoChests(chests, pickable.Value, Math.Max(1, pickable.Count));
                if (leftover > 0) {
                    m_subsystemPickables.AddPickable(pickable.Value, leftover, itemPosition, new Vector3(0f, 2f, 0f), null);
                }
            }
            // 棕色存储粒子：花朵根处
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(Position.X + 0.5f, Position.Y + 0.2f, Position.Z + 0.5f),
                0.35f,
                0.5f,
                new Color(139, 90, 43)
            ));
        }

        /// <summary>是否靠近受保护的合成台（花药台/符文台）1.5 格内。</summary>
        public bool IsNearProtectedTable(Vector3 position) {
            return IsNearTables(m_subsystemFlowerTables?.m_tables, position)
                || IsNearTables(m_subsystemRunesTables?.m_tables, position);
        }

        private static bool IsNearTables(Dictionary<Point3, FlowerTable> tables, Vector3 position) {
            if (tables == null) {
                return false;
            }
            foreach (Point3 table in tables.Keys) {
                Vector3 center = new(table.X + 0.5f, table.Y + 0.5f, table.Z + 0.5f);
                if ((position - center).LengthSquared() <= TableProtectionRadius * TableProtectionRadius) {
                    return true;
                }
            }
            return false;
        }

        private static bool IsNearTables(Dictionary<Point3, RunesTable> tables, Vector3 position) {
            if (tables == null) {
                return false;
            }
            foreach (Point3 table in tables.Keys) {
                Vector3 center = new(table.X + 0.5f, table.Y + 0.5f, table.Z + 0.5f);
                if ((position - center).LengthSquared() <= TableProtectionRadius * TableProtectionRadius) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>贴身（上/前/后/左/右）的未满箱子。</summary>
        public List<ComponentChest> GetAdjacentChests() {
            List<ComponentChest> chests = [];
            Point3[] offsets = [
                new(0, 1, 0),
                new(1, 0, 0),
                new(-1, 0, 0),
                new(0, 0, 1),
                new(0, 0, -1)
            ];
            foreach (Point3 offset in offsets) {
                Point3 cell = new(Position.X + offset.X, Position.Y + offset.Y, Position.Z + offset.Z);
                int contents = Scheduler.m_subsystemTerrain.Terrain.GetCellContents(cell);
                if (contents != BlocksManager.GetBlockIndex<ChestBlock>()) {
                    continue;
                }
                ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(cell);
                ComponentChest chest = blockEntity?.Entity.FindComponent<ComponentChest>(true);
                if (chest != null && HasEmptySlot(chest)) {
                    chests.Add(chest);
                }
            }
            return chests;
        }

        public static bool HasEmptySlot(ComponentChest chest) {
            for (int slot = 0; slot < chest.SlotsCount; slot++) {
                if (chest.GetSlotValue(slot) == 0) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>把物品塞进箱子（可堆叠优先堆叠，否则占空格）；返回塞不下的余量。</summary>
        public static int InsertIntoChests(List<ComponentChest> chests, int value, int count) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            foreach (ComponentChest chest in chests) {
                // 先尝试堆叠
                for (int slot = 0; slot < chest.SlotsCount && count > 0; slot++) {
                    int slotValue = chest.GetSlotValue(slot);
                    int slotCount = chest.GetSlotCount(slot);
                    if (slotCount > 0 && slotValue == value && slotCount < block.GetMaxStacking(value)) {
                        int take = Math.Min(count, block.GetMaxStacking(value) - slotCount);
                        chest.AddSlotItems(slot, value, take);
                        count -= take;
                    }
                }
                // 再占空格
                for (int slot = 0; slot < chest.SlotsCount && count > 0; slot++) {
                    if (chest.GetSlotValue(slot) == 0) {
                        int take = Math.Min(count, block.GetMaxStacking(value));
                        chest.AddSlotItems(slot, value, take);
                        count -= take;
                    }
                }
                if (count <= 0) {
                    return 0;
                }
            }
            return count;
        }

        /// <summary>未绑链时自行从魔法池取食（储满即停）。</summary>
        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(true);
            m_subsystemFlowerTables = Project.FindSubsystem<SubsystemFlowerTableBehavior>(false);
            m_subsystemRunesTables = Project.FindSubsystem<SubsystemRunesTableBehavior>(false);
        }
    }
}
