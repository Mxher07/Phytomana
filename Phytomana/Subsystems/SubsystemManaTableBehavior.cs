using System;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 魔力石板行为：手持石板任意点击显示当前魔力；点击魔法池时把石板存储的
    /// 魔力全部灌入魔法池（池满则余量留在石板中）。
    /// 石板魔力存储在其特殊值（data）中，随存档保存。
    /// </summary>
    public class SubsystemManaTableBehavior : SubsystemBlockBehavior {
        public ManaNetworkManager m_network;

        public int m_manaPoolIndex;

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<ManaTabletBlock>()];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
            m_manaPoolIndex = BlocksManager.GetBlockIndex<ManaPoolBlock>();
        }

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            int tabletMana = Terrain.ExtractData(componentMiner.ActiveBlockValue);
            // 任意点击都先输出当前魔力情况
            ShowMessage(player, "Status",
                tabletMana.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ManaTabletBlock.MaxMana.ToString(System.Globalization.CultureInfo.InvariantCulture));
            // 对准魔法池点击：把石板魔力全部灌入（直到池满）
            TerrainRaycastResult? hit = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
            if (hit.HasValue) {
                Point3 point = hit.Value.CellFace.Point;
                int contents = Terrain.ExtractContents(hit.Value.Value);
                if (contents == m_manaPoolIndex
                    && m_network.TryGetReceiverStorage(point, out ManaStorage poolStorage)) {
                    int transfer = (int)MathF.Min(tabletMana, poolStorage.Free);
                    if (transfer > 0) {
                        poolStorage.TryAdd(transfer);
                        SetHeldTabletMana(componentMiner, tabletMana - transfer);
                        ShowMessage(player, "Poured", transfer.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else {
                        ShowMessage(player, "PoolFull");
                    }
                }
                else {
                    ShowMessage(player, "NoPool");
                }
            }
            return true;
        }

        /// <summary>更新手持石板（活动槽位）存储的魔力：走原版道具耐久的「移除再加回」更新方式。</summary>
        public static void SetHeldTabletMana(ComponentMiner componentMiner, int mana) {
            IInventory inventory = componentMiner.Inventory;
            if (inventory == null) {
                return;
            }
            int slot = inventory.ActiveSlotIndex;
            int count = inventory.GetSlotCount(slot);
            int newValue = Terrain.ReplaceData(componentMiner.ActiveBlockValue, Math.Clamp(mana, 0, ManaTabletBlock.MaxMana));
            inventory.RemoveSlotItems(slot, count);
            if (inventory.GetSlotCount(slot) == 0) {
                inventory.AddSlotItems(slot, newValue, count);
            }
        }

        public void ShowMessage(ComponentPlayer player, string key, string arg0 = null, string arg1 = null) {
            if (player == null) {
                return;
            }
            string text = LanguageControl.Get("ManaTabletMessages", key);
            if (arg0 != null) {
                text = string.Format(text, arg0, arg1);
            }
            player.ComponentGui.DisplaySmallMessage(text, Color.White, false, false);
        }
    }
}