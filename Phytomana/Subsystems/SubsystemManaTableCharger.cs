using System;
using Engine;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 魔力石板充能器：玩家背包内有存有魔力的魔力石板，且有耐久不满的
    /// 魔力钢工具/装备时，每 2 秒消耗石板魔力修复 1 点耐久。
    /// 修复耗魔：镐/弯刀/斧 52mn，胸甲与护腿 45mn，其余（铲、头盔、靴子）35mn。
    /// </summary>
    public class SubsystemManaTableCharger : Subsystem, IUpdateable {
        public const float ChargeInterval = 2f;

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemGameInfo m_subsystemGameInfo;

        public int m_manaTabletIndex;

        public int m_manaPickaxeIndex;

        public int m_manaMacheteIndex;

        public int m_manaAxeIndex;

        public int m_manaShovelIndex;

        public int m_clothingIndex;

        public float m_chargeTimer;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_manaTabletIndex = BlocksManager.GetBlockIndex<ManaTabletBlock>();
            m_manaPickaxeIndex = BlocksManager.GetBlockIndex<ManaPickaxeBlock>();
            m_manaMacheteIndex = BlocksManager.GetBlockIndex<ManaMacheteBlock>();
            m_manaAxeIndex = BlocksManager.GetBlockIndex<ManaAxeBlock>();
            m_manaShovelIndex = BlocksManager.GetBlockIndex<ManaShovelBlock>();
            m_clothingIndex = BlocksManager.GetBlockIndex<ClothingBlock>();
        }

        public void Update(float dt) {
            if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) {
                return;
            }
            m_chargeTimer += dt;
            if (m_chargeTimer < ChargeInterval) {
                return;
            }
            m_chargeTimer = 0f;
            foreach (ComponentPlayer player in m_subsystemPlayers.ComponentPlayers) {
                try {
                    ChargePlayerTools(player);
                }
                catch (Exception e) {
                    Log.Error($"[PhytoMana]ManaTablet charge error: {e}");
                }
            }
        }

        /// <summary>扫描玩家背包：找到耐久不满的魔力钢工具/装备，用背包中魔力足够的石板修复 1 点耐久。</summary>
        public void ChargePlayerTools(ComponentPlayer player) {
            IInventory inventory = player.Entity.FindComponent<ComponentMiner>()?.Inventory;
            if (inventory == null) {
                return;
            }
            // 第一轮：找一件耐久不满的魔力钢工具/装备，记下修复耗魔
            int repairSlot = -1;
            int repairCost = 0;
            for (int slot = 0; slot < inventory.SlotsCount; slot++) {
                int value = inventory.GetSlotValue(slot);
                int count = inventory.GetSlotCount(slot);
                if (count <= 0) {
                    continue;
                }
                int cost = GetRepairCost(value);
                if (cost <= 0) {
                    continue;
                }
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                if (block.GetDamage(value) <= 0) {
                    continue; // 满耐久
                }
                repairSlot = slot;
                repairCost = cost;
                break;
            }
            if (repairSlot < 0) {
                return;
            }
            // 第二轮：从魔力足够的石板中扣魔（优先扣存量最多的）
            int tabletSlot = -1;
            int tabletMana = 0;
            for (int slot = 0; slot < inventory.SlotsCount; slot++) {
                if (inventory.GetSlotCount(slot) <= 0) {
                    continue;
                }
                if (Terrain.ExtractContents(inventory.GetSlotValue(slot)) != m_manaTabletIndex) {
                    continue;
                }
                int mana = Terrain.ExtractData(inventory.GetSlotValue(slot));
                if (mana > tabletMana) {
                    tabletMana = mana;
                    tabletSlot = slot;
                }
            }
            if (tabletSlot < 0 || tabletMana < repairCost) {
                return;
            }
            int tabletValue = inventory.GetSlotValue(tabletSlot);
            int tabletCount = inventory.GetSlotCount(tabletSlot);
            // 修复 1 点耐久
            int repairValue = inventory.GetSlotValue(repairSlot);
            Block repairBlock = BlocksManager.Blocks[Terrain.ExtractContents(repairValue)];
            int newValue = repairBlock.SetDamage(repairValue, repairBlock.GetDamage(repairValue) - 1);
            int repairCount = inventory.GetSlotCount(repairSlot);
            inventory.RemoveSlotItems(repairSlot, repairCount);
            if (inventory.GetSlotCount(repairSlot) == 0) {
                inventory.AddSlotItems(repairSlot, newValue, repairCount);
            }
            // 石板扣魔
            int newTabletValue = Terrain.ReplaceData(tabletValue, tabletMana - repairCost);
            inventory.RemoveSlotItems(tabletSlot, tabletCount);
            if (inventory.GetSlotCount(tabletSlot) == 0) {
                inventory.AddSlotItems(tabletSlot, newTabletValue, tabletCount);
            }
        }

        /// <summary>
        /// 修复一件魔力钢工具/装备的耗魔：镐/弯刀/斧 52mn，胸甲与护腿 45mn，
        /// 其余（铲、头盔、靴子）35mn；非魔力钢工具/装备或耐久已满返回 0。
        /// </summary>
        public int GetRepairCost(int value) {
            int contents = Terrain.ExtractContents(value);
            int data = Terrain.ExtractData(value);
            if (contents == m_manaPickaxeIndex
                || contents == m_manaMacheteIndex
                || contents == m_manaAxeIndex) {
                return 52;
            }
            if (contents == m_clothingIndex) {
                // 魔力钢护甲：按服饰索引区分（900 头盔 / 901 胸甲 / 902 护腿 / 903 靴子）
                int clothingIndex = ClothingBlock.GetClothingIndex(data);
                if (clothingIndex == 901 || clothingIndex == 902) {
                    return 45;
                }
                if (clothingIndex == 900 || clothingIndex == 903) {
                    return 35;
                }
                return 0;
            }
            if (contents == m_manaShovelIndex) {
                return 35;
            }
            return 0;
        }
    }
}