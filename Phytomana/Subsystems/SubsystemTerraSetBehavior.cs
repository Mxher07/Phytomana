using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana;
using TemplatesDatabase;

namespace Game {
    /// <summary>
    /// 泰拉套装效果子系统：
    /// 1. 全泰拉装（904~907）时生命恢复不再受饥饿度限制（饱食度为 0 也回血）；
    /// 2. 泰拉工具石板修复 -20% 减免（TerraToolRepairDiscount）；
    /// 3. 魔力钢装（900~903）石板修复 -45% 减免（ManaSteelRepairDiscount）；
    /// 4. 泰拉装备自然耐久恢复：每 2s 耗 60mn（PhytoConfig.TerraEquipRepairCost）修复 1 点耐久。
    /// 潜行档位切换见 <see cref="TerraSetHooks"/>。
    /// </summary>
    public class SubsystemTerraSetBehavior : Subsystem, IUpdateable {
        public class PlayerSetState {
            public int PlayerId;

            public float RepairTimer;
        }

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemMana m_subsystemMana;

        public int m_clothingIndex;

        public int m_manaTabletIndex;

        public int m_terraBladeIndex;

        public int m_terraCutterIndex;

        public int m_terraBreakerIndex;

        public SubsystemTerraToolBehavior m_terraToolBehavior;

        public Dictionary<int, PlayerSetState> m_states = [];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
            m_terraToolBehavior = Project.FindSubsystem<SubsystemTerraToolBehavior>(true);
            m_clothingIndex = BlocksManager.GetBlockIndex<ClothingBlock>();
            m_manaTabletIndex = BlocksManager.GetBlockIndex<ManaTabletBlock>();
            m_terraBladeIndex = BlocksManager.GetBlockIndex<TerraBladeBlock>();
            m_terraCutterIndex = BlocksManager.GetBlockIndex<TerraCutterBlock>();
            m_terraBreakerIndex = BlocksManager.GetBlockIndex<TerraBreakerBlock>();
            TerraSetHooks.m_terraSetBehavior = this;
        }

        // ===== 套装检测 =====

        /// <summary>玩家是否穿着完整泰拉套装（904~907 四件齐全）。</summary>
        public bool HasFullTerraSet(ComponentPlayer player) =>
            HasAllClothing(player, 904, 905, 906, 907);

        /// <summary>玩家是否穿着完整魔力钢套装（900~903 四件齐全）。</summary>
        public bool HasFullManaSteelSet(ComponentPlayer player) =>
            HasAllClothing(player, 900, 901, 902, 903);

        public bool HasAllClothing(ComponentPlayer player, params int[] clothingIndices) {
            ComponentClothing clothing = player.ComponentClothing;
            if (clothing == null) {
                return false;
            }
            HashSet<int> equipped = [];
            foreach (KeyValuePair<ClothingSlot, List<int>> pair in clothing.m_clothes) {
                foreach (int value in pair.Value) {
                    if (Terrain.ExtractContents(value) != m_clothingIndex) {
                        continue;
                    }
                    equipped.Add(ClothingBlock.GetClothingIndex(Terrain.ExtractData(value)));
                }
            }
            foreach (int required in clothingIndices) {
                if (!equipped.Contains(required)) {
                    return false;
                }
            }
            return true;
        }

        // ===== 自然耐久修复 =====

        /// <summary>每 2s 扫一次：全泰拉装的玩家用背包魔力石板修复泰拉装备耐久（60mn/点）。</summary>
        public void Update(float dt) {
            foreach (ComponentPlayer player in m_subsystemPlayers.ComponentPlayers) {
                if (player.Entity == null) {
                    continue;
                }
                PlayerSetState state = GetState(player);
                // 自由回血：全泰拉装时，饱食度为 0 也能自然回血（绕过原版饥饿门控）
                ComponentHealth health = player.ComponentHealth;
                if (health != null
                    && HasFullTerraSet(player)
                    && health.Health > 0f
                    && health.Health < 1f) {
                    float food = player.ComponentVitalStats?.Food ?? 0f;
                    // 饱食度 < 0.5 时原版暂停回血；全泰拉装时按 0.5 档速率回血，不受饥饿限制
                    if (food < 0.5f) {
                        health.Heal(dt * 0.00111111114f);
                    }
                }
                // 耐久自然修复
                IInventory inventory = player.Entity.FindComponent<ComponentMiner>()?.Inventory;
                if (inventory == null) {
                    continue;
                }
                state.RepairTimer += dt;
                if (state.RepairTimer < 2f) {
                    continue;
                }
                state.RepairTimer = 0f;
                if (!HasFullTerraSet(player)) {
                    continue;
                }
                RepairTerraEquipDurability(player);
            }
        }

        /// <summary>
        /// 泰拉装备自然耐久修复：从背包魔力石板扣 60mn（PhytoConfig.TerraEquipRepairCost），
        /// 修复背包中第一件耐久不满的泰拉装备（工具或 904~907 装具）1 点耐久。
        /// </summary>
        public void RepairTerraEquipDurability(ComponentPlayer player) {
            IInventory inventory = player.Entity.FindComponent<ComponentMiner>().Inventory;
            int cost = (int)PhytoConfig.Instance.TerraEquipRepairCost;
            int repairSlot = -1;
            for (int slot = 0; slot < inventory.SlotsCount; slot++) {
                int value = inventory.GetSlotValue(slot);
                if (inventory.GetSlotCount(slot) <= 0) {
                    continue;
                }
                int contents = Terrain.ExtractContents(value);
                bool isTerra = contents == m_terraBladeIndex
                    || contents == m_terraCutterIndex
                    || contents == m_terraBreakerIndex;
                bool isTerraCloth = contents == m_clothingIndex && IsTerraClothing(Terrain.ExtractData(value));
                if (!isTerra && !isTerraCloth) {
                    continue;
                }
                Block block = BlocksManager.Blocks[contents];
                if (block.GetDamage(value) <= 0) {
                    continue;
                }
                repairSlot = slot;
                break;
            }
            if (repairSlot < 0) {
                return;
            }
            int tabletSlot = -1;
            int tabletMana = 0;
            for (int slot = 0; slot < inventory.SlotsCount; slot++) {
                if (inventory.GetSlotCount(slot) <= 0
                    || Terrain.ExtractContents(inventory.GetSlotValue(slot)) != m_manaTabletIndex) {
                    continue;
                }
                int mana = Terrain.ExtractData(inventory.GetSlotValue(slot));
                if (mana > tabletMana) {
                    tabletMana = mana;
                    tabletSlot = slot;
                }
            }
            if (tabletSlot < 0 || tabletMana < cost) {
                return;
            }
            int repairValue = inventory.GetSlotValue(repairSlot);
            Block repairBlock = BlocksManager.Blocks[Terrain.ExtractContents(repairValue)];
            int newValue = repairBlock.SetDamage(repairValue, repairBlock.GetDamage(repairValue) - 1);
            int repairCount = inventory.GetSlotCount(repairSlot);
            inventory.RemoveSlotItems(repairSlot, repairCount);
            if (inventory.GetSlotCount(repairSlot) == 0) {
                inventory.AddSlotItems(repairSlot, newValue, repairCount);
            }
            int tabletValue = inventory.GetSlotValue(tabletSlot);
            int newTabletValue = Terrain.ReplaceData(tabletValue, tabletMana - cost);
            int tabletCount = inventory.GetSlotCount(tabletSlot);
            inventory.RemoveSlotItems(tabletSlot, tabletCount);
            if (inventory.GetSlotCount(tabletSlot) == 0) {
                inventory.AddSlotItems(tabletSlot, newTabletValue, tabletCount);
            }
        }

        public bool IsTerraClothing(int data) {
            int index = ClothingBlock.GetClothingIndex(data);
            return index >= 904 && index <= 907;
        }

        // ===== 石板修复减免 =====

        /// <summary>
        /// 石板充能器修复耗魔减免：泰拉工具 -20%（TerraToolRepairDiscount），
        /// 魔力钢装具 -45%（ManaSteelRepairDiscount）。由 SubsystemManaTableCharger 调用。
        /// </summary>
        public float GetRepairDiscount(ComponentPlayer player, int value) {
            int contents = Terrain.ExtractContents(value);
            float discount = 0f;
            if (contents == m_terraBladeIndex
                || contents == m_terraCutterIndex
                || contents == m_terraBreakerIndex) {
                discount = Math.Max(discount, PhytoConfig.Instance.TerraToolRepairDiscount);
            }
            if (contents == m_clothingIndex) {
                int clothingIndex = ClothingBlock.GetClothingIndex(Terrain.ExtractData(value));
                if (clothingIndex >= 900 && clothingIndex <= 903) {
                    discount = Math.Max(discount, PhytoConfig.Instance.ManaSteelRepairDiscount);
                }
            }
            return discount;
        }

        // ===== 潜行切换破坏者档位 =====

        /// <summary>
        /// 玩家手持泰拉破坏者潜行时：C~SS 逐档切换激活档位（D 无激活态）。
        /// 激活需存魔达标（C 380 / B 3800 / A 38000 / S 380000 / SS 3800000）。
        /// </summary>
        public void ToggleBreakerLevel(ComponentPlayer player) {
            ComponentMiner miner = player.ComponentMiner;
            if (miner == null) {
                return;
            }
            if (Terrain.ExtractContents(miner.ActiveBlockValue) != m_terraBreakerIndex) {
                return;
            }
            SubsystemTerraToolBehavior.BreakerState state = m_terraToolBehavior?.GetBreakerState(miner);
            if (state == null) {
                return;
            }
            int target = state.ActiveLevel + 1;
            if (target > 5) {
                target = 0; // 回到 D（无激活）
            }
            float requiredMana = RequiredManaForLevel(target);
            if (requiredMana > 0f && state.Mana < requiredMana) {
                return; // 魔力不足无法激活该档
            }
            state.ActiveLevel = target;
        }

        /// <summary>各激活档位所需存魔（D 无需求，C..SS 见 PhytoConfig）。</summary>
        public float RequiredManaForLevel(int level) {
            return level switch {
                0 => 0f,
                1 => PhytoConfig.Instance.TerraBreakerLevelCMana,
                2 => PhytoConfig.Instance.TerraBreakerLevelBMana,
                3 => PhytoConfig.Instance.TerraBreakerLevelAMana,
                4 => PhytoConfig.Instance.TerraBreakerLevelSMana,
                5 => PhytoConfig.Instance.TerraBreakerLevelSSMana,
                _ => 0f
            };
        }

        public PlayerSetState GetState(ComponentPlayer player) {
            int key = player.Entity?.Id ?? 0;
            if (!m_states.TryGetValue(key, out PlayerSetState state)) {
                state = new PlayerSetState { PlayerId = key };
                m_states[key] = state;
            }
            return state;
        }
    }

    /// <summary>
    /// 泰拉套装引擎钩子调度器：潜行档位切换（破坏者）转发到 <see cref="SubsystemTerraSetBehavior"/>。
    /// 由 <see cref="PhytomanaMod"/> 在 __ModInitialize 注册。
    /// </summary>
    public class TerraSetHooks : ModLoader {
        public static SubsystemTerraSetBehavior m_terraSetBehavior;

        /// <summary>潜行双击左键（挖掘）手持泰拉破坏者时切换激活档位（D 无激活态）。</summary>
        public override void OnMinerDig(
            ComponentMiner miner,
            TerrainRaycastResult raycastResult,
            ref float digProgress,
            out bool digged) {
            digged = false;
            if (m_terraSetBehavior == null || miner.ComponentPlayer == null) {
                return;
            }
            if (Terrain.ExtractContents(miner.ActiveBlockValue) != m_terraSetBehavior.m_terraBreakerIndex) {
                return;
            }
            ComponentBody body = miner.ComponentPlayer.Entity?.FindComponent<ComponentBody>();
            if (body == null || !body.IsCrouching) {
                return;
            }
            m_terraSetBehavior.ToggleBreakerLevel(miner.ComponentPlayer);
        }
    }
}
