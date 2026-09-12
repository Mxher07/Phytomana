using System;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using Phytomana.Network;
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
            PhytoNet.AddTableHandler(HandleTableActionFromClient);
        }

        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            bool isMain = player?.PlayerData?.IsMainPlayer ?? true;
            int tabletMana = Terrain.ExtractData(componentMiner.ActiveBlockValue);
            // 任意点击都先输出当前魔力情况（石板魔力在物品 data 里，客户端读得到）。
            ShowMessage(player, "Status",
                tabletMana.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ManaTabletBlock.MaxMana.ToString(System.Globalization.CultureInfo.InvariantCulture));
            // 服务器重放远程玩家的右键被抑制：权威灌入改走请求。
            if (NetworkManager.IsServerRunning && !isMain) {
                return true;
            }
            // 对准魔法池点击：把石板魔力全部灌入（直到池满）
            TerrainRaycastResult? hit = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
            if (hit.HasValue) {
                Point3 point = hit.Value.CellFace.Point;
                int contents = Terrain.ExtractContents(hit.Value.Value);
                if (contents == m_manaPoolIndex) {
                    if (NetworkManager.IsClientRunning) {
                        // 联机客户端：池内魔力为服务器权威，灌入走请求，结果由回执文本反馈。
                        if (isMain) {
                            PhytoNet.Send(PhytoNet.BuildTableAction(PhytoTableAction.ManaTablet, point));
                        }
                    }
                    else {
                        string key = PourTablet(player, componentMiner, point, out int transferred);
                        ShowMessage(player, key, transferred.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
                else {
                    ShowMessage(player, "NoPool");
                }
            }
            return true;
        }

        /// <summary>把石板魔力灌入魔法池（池满则余量留在石板）。返回结果显示文案键。</summary>
        public string PourTablet(ComponentPlayer player, ComponentMiner componentMiner, Point3 point, out int transferred) {
            transferred = 0;
            int tabletMana = Terrain.ExtractData(componentMiner.ActiveBlockValue);
            if (m_network.TryGetReceiverStorage(point, out ManaStorage poolStorage)) {
                int transfer = (int)MathF.Min(tabletMana, poolStorage.Free);
                if (transfer > 0) {
                    poolStorage.TryAdd(transfer);
                    SetHeldTabletMana(componentMiner, tabletMana - transfer);
                    transferred = transfer;
                    return "Poured";
                }
                return "PoolFull";
            }
            return "NoPool";
        }

        /// <summary>服务器处理客户端石板灌入请求：落权威魔力并回执结果文本。</summary>
        public void HandleTableActionFromClient(PhytoModPacket packet) {
            if (packet.ByteA != PhytoTableAction.ManaTablet || !packet.HasA) {
                return;
            }
            ComponentPlayer player = PhytoNet.FindPlayer(packet.From?.PlayerIndex ?? packet.PlayerIndex);
            if (player == null) {
                return;
            }
            ComponentMiner miner = player.ComponentMiner;
            if (miner == null) {
                return;
            }
            string key = PourTablet(player, miner, packet.PointA, out int transferred);
            string text = key == "Poured"
                ? string.Format(LanguageControl.Get("ManaTabletMessages", key), transferred.ToString(System.Globalization.CultureInfo.InvariantCulture))
                : LanguageControl.Get("ManaTabletMessages", key);
            PhytoNet.ReplyTo(packet, PhytoNet.BuildTableStatusReply(packet, text));
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