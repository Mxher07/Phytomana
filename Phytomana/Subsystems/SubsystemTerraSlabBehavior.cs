using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana;
using Phytomana.Api;
using TemplatesDatabase;

namespace Game {
    /// <summary>
    /// 泰拉合成毯行为：结构校验 + 两档合成 + 供魔粒子。
    /// 毯以 IManaReceiver 身份（容量 38000mn，见 ManaBlocks.xml）接入魔力网络，
    /// 经法杖绑定发射器供魔；材料由毯顶面（y+1~y+2，水平 ±1）的掉落物提供，
    /// 也可由玩家点击存放（5 种材料、每类限存，自动存档；破块时弹出）。
    /// 结构有效时带内材料掉落物自动吸附到毯顶中心；供魔门（IManaGate）在材料未齐前关闭——
    /// 发射器绑定了也要等合成可以开始才供魔。
    /// 结构（以毯为中心、高度减一的地面上，毯悬空 3×3 层）：
    ///   毯层（y）：中心为毯，外围 8 格须为空气；
    ///   下层（y-1）：
    ///                 岩 锗 岩
    ///                 锗 岩 锗
    ///                 岩 锗 岩
    /// 其中「岩」= 生息岩，「锗」= 仅锗晶块。
    /// 结构错误时：供入的魔力每周期清空，法杖指向提示「结构无效」。
    /// 结构正确且材料齐备 + 魔力足额时：
    ///   一档（魔力钻石 + 魔力钢锭 + 锗晶）→ 消耗魔力池半分魔力，产 泰拉锭×1；
    ///   二档（魔力钻石块 + 魔力钢锭块 + 锗晶块）→ 消耗魔力池半分魔力，产 泰拉锭块×1。
    /// 合成时 4 个角（+1 高度）深蓝 1f 粒子每 0.25s 向毯+1 收敛。
    /// </summary>
    public class SubsystemTerraSlabBehavior : SubsystemBlockBehavior, IUpdateable {
        public class TerraSlabReceiver : IManaReceiver, IManaGate {
            public const float DefaultMaxMana = 38000f;

            public Point3 Position { get; }

            public ManaStorage ManaStorage { get; }

            /// <summary>回引用：供魔门（IManaGate）读 <see cref="SlabInfo"/> 的结构/材料状态。</summary>
            public SlabInfo Info;

            public TerraSlabReceiver(Point3 position) {
                Position = position;
                ManaStorage = new ManaStorage(ManaBlockRegistry.GetMaxMana("TerraSlabBlock", DefaultMaxMana));
            }

            /// <summary>供魔门：结构有效且材料齐备（合成可以开始）才收魔，绑定发射器也不提前供。</summary>
            public bool AcceptsMana() => Info != null && Info.StructureValid && Info.MaterialsReady;
        }

        public class SlabInfo {
            public TerraSlabReceiver Receiver;

            public bool StructureValid;

            public bool StructureBitApplied;

            /// <summary>点击存放的材料：完整方块值 → 数量。共享 <see cref="SubsystemTerraSlabBehavior.m_slabItems"/> 的每毯字典
            /// （区块卸载期间毯节点销毁重建也不丢）；区块已卸载的毯由网络侧 dormant 存魔兜住魔力，材料账留在 m_slabItems。</summary>
            public Dictionary<int, int> Items = [];

            /// <summary>材料齐备缓存（0.25s 节流重算），供魔门与发射器供魔判定读此。</summary>
            public bool MaterialsReady;

            /// <summary><see cref="MaterialsReady"/> 下次重算时间。</summary>
            public double NextMaterialsCheckTime;

            public double NextParticleTime;
        }

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemMana m_subsystemMana;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemGameInfo m_subsystemGameInfo;

        public int m_slabIndex;

        public int m_grownStoneIndex;

        public int m_terraBlockIndex;

        public int m_semicontductorIndex;

        public int m_terraIngotIndex;

        public int m_manaDiamondChunkIndex;

        public int m_manaDiamondBlockIndex;

        public int m_manaIngotIndex;

        public int m_manaSteelBlockIndex;

        public Dictionary<Point3, SlabInfo> m_slabs = [];

        /// <summary>点击存放材料的持久账本：坐标 → 每毯字典。独立于 SlabInfo 节点——
        /// 区块卸载/重载期间毯节点重建也不丢；破块（destroyed=true）时弹出并移除。</summary>
        public Dictionary<Point3, Dictionary<int, int>> m_slabItems = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<TerraSlabBlock>()];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_slabIndex = BlocksManager.GetBlockIndex<TerraSlabBlock>();
            m_grownStoneIndex = BlocksManager.GetBlockIndex<GrownStoneBlock>();
            m_terraBlockIndex = BlocksManager.GetBlockIndex<TerraBlock>();
            m_semicontductorIndex = BlocksManager.GetBlockIndex<SemiconductorBlock>();
            m_terraIngotIndex = BlocksManager.GetBlockIndex<TerraIngotBlock>();
            m_manaDiamondChunkIndex = BlocksManager.GetBlockIndex<ManaDiamondChunkBlock>();
            m_manaDiamondBlockIndex = BlocksManager.GetBlockIndex<ManaDiamondBlock>();
            m_manaIngotIndex = BlocksManager.GetBlockIndex<ManaIngotBlock>();
            m_manaSteelBlockIndex = BlocksManager.GetBlockIndex<ManaBlock>();
            // 点击存放的材料账本：「x,y,z,v1,c1,v2,c2,...;」（区块已卸载的毯其账本也保留）
            string slabItems = valuesDictionary.GetValue("TerraSlabItems", string.Empty);
            foreach (string entry in slabItems.Split([';'], StringSplitOptions.RemoveEmptyEntries)) {
                string[] parts = entry.Split([','], StringSplitOptions.None);
                if (parts.Length < 4
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int z)) {
                    continue;
                }
                Dictionary<int, int> dict = [];
                for (int i = 3; i + 1 < parts.Length; i += 2) {
                    if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                        && int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
                        && count > 0) {
                        dict[value] = count;
                    }
                }
                m_slabItems[new Point3(x, y, z)] = dict;
            }
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddSlab(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddSlab(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveSlab(new Point3(x, y, z), true);
        }

        /// <summary>
        /// 区块卸载：dormant 保留存魔（destroyed=false）并移除强引用，
        /// 避免 <see cref="Update"/> 把卸载区块的结构全判无效后把存魔清零；
        /// 重载时 <see cref="AddSlab"/> 经 RegisterReceiver→AttachDormantMana 自动恢复。
        /// </summary>
        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = [];
            foreach (Point3 point in m_slabs.Keys) {
                if (point.X >= chunk.Origin.X
                    && point.X < chunk.Origin.X + 16
                    && point.Z >= chunk.Origin.Y
                    && point.Z < chunk.Origin.Y + 16) {
                    list.Add(point);
                }
            }
            foreach (Point3 point in list) {
                RemoveSlab(point, false);
            }
        }

        public void AddSlab(int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (m_slabs.ContainsKey(point)) {
                return;
            }
            TerraSlabReceiver receiver = new(point);
            SlabInfo info = new() {
                Receiver = receiver,
                StructureValid = IsStructureValid(point),
                StructureBitApplied = false,
                NextMaterialsCheckTime = 0,
                NextParticleTime = m_subsystemGameInfo.TotalElapsedGameTime
            };
            receiver.Info = info;
            // 材料账本独立于毯节点：区块重载/读档重建毯时挂回原账本，存量不丢
            Dictionary<int, int> items;
            if (!m_slabItems.TryGetValue(point, out items)) {
                items = m_slabItems[point] = [];
            }
            info.Items = items;
            m_subsystemMana.m_network.RegisterReceiver(receiver);
            m_slabs[point] = info;
        }

        /// <param name="destroyed">true 方块被破坏（存魔丢弃、存量材料弹出）；false 区块卸载（存魔与材料账本休眠保留）。</param>
        public void RemoveSlab(Point3 point, bool destroyed) {
            if (!m_slabs.TryGetValue(point, out SlabInfo info)) {
                return;
            }
            m_slabs.Remove(point);
            if (destroyed) {
                EjectStoredItems(point, info);
                m_slabItems.Remove(point);
            }
            m_subsystemMana.m_network.UnregisterReceiver(info.Receiver, destroyed);
        }

        /// <summary>点击存放的全部材料弹出为毯顶掉落物（破块退回 / 潜行取回共用），账本清空。</summary>
        public void EjectStoredItems(Point3 point, SlabInfo info) {
            if (info.Items.Count == 0) {
                return;
            }
            Vector3 center = new(point.X + 0.5f, point.Y + 1.1f, point.Z + 0.5f);
            foreach (KeyValuePair<int, int> item in info.Items) {
                if (item.Value > 0) {
                    m_subsystemPickables.AddPickable(item.Key, item.Value, center, new Vector3(0f, 2f, 0f), null);
                }
            }
            info.Items.Clear();
        }

        /// <summary>单毯单类材料的点击存放上限。</summary>
        public const int SlabItemCap = 4;

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            Point3 point = raycastResult.CellFace.Point;
            if (!m_slabs.TryGetValue(point, out SlabInfo info)) {
                return false;
            }
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            if (player == null) {
                return false;
            }
            int heldContents = Terrain.ExtractContents(componentMiner.ActiveBlockValue);
            // 手持生息法杖：回落到法杖自身的 use（工作模式 ShowStatus）
            if (heldContents == BlocksManager.GetBlockIndex<GrownStaffBlock>()) {
                return false;
            }
            ComponentBody body = player.Entity.FindComponent<ComponentBody>();
            bool crouching = body != null && body.IsCrouching;
            if (heldContents == 0) {
                if (crouching) {
                    return ReturnAllItems(point, info);
                }
                ShowStatus(point, player);
                return true;
            }
            if (IsSlabMaterial(heldContents)) {
                return PlaceItem(point, info, componentMiner);
            }
            ShowMessage(player, "SlabNotNeeded");
            return true;
        }

        /// <summary>可点击进毯的 5 种材料：魔力钻石 / 魔力钢锭 / 魔力钻石块 / 魔力钢锭块 / 锗晶块。</summary>
        public bool IsSlabMaterial(int contents) {
            return contents == m_manaDiamondChunkIndex
                || contents == m_manaIngotIndex
                || contents == m_manaDiamondBlockIndex
                || contents == m_manaSteelBlockIndex
                || contents == m_semicontductorIndex;
        }

        /// <summary>点击放置一件材料进毯（合成时先消耗存量；每类上限 <see cref="SlabItemCap"/>）。</summary>
        public bool PlaceItem(Point3 point, SlabInfo info, ComponentMiner componentMiner) {
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            int heldValue = componentMiner.ActiveBlockValue;
            if (info.Items.GetValueOrDefault(heldValue) >= SlabItemCap) {
                ShowMessage(player, "SlabEnough");
                return true;
            }
            IInventory inventory = componentMiner.Inventory;
            int activeSlot = inventory.ActiveSlotIndex;
            inventory.RemoveSlotItems(activeSlot, 1);
            info.Items[heldValue] = info.Items.GetValueOrDefault(heldValue) + 1;
            info.NextMaterialsCheckTime = 0;
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(point.X + 0.5f, point.Y + 1.2f, point.Z + 0.5f),
                0.25f,
                1.5f,
                Color.White
            ));
            SubsystemAudio audio = Project.FindSubsystem<SubsystemAudio>(false);
            audio?.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f, 0f);
            return true;
        }

        /// <summary>空手潜行右键：取回全部点击存放的材料（弹出为毯顶掉落物）。</summary>
        public bool ReturnAllItems(Point3 point, SlabInfo info) {
            EjectStoredItems(point, info);
            return true;
        }

        public void ShowMessage(ComponentPlayer player, string key) {
            player?.ComponentGui.DisplaySmallMessage(
                LanguageControl.Get("GrownStaffMessages", key), Color.White, false, false);
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            // 结构校验每 Update 周期全量重算（结构变化不触发 ChangeCell，只由渲染读 bit0）
        }

        /// <summary>
        /// 结构状态写入方块 data 的 bit0（渲染层据此变色）。
        /// 注意：不直接 ChangeCell，避免触发 OnBlockModified 自递归；
        /// 渲染层（TerraSlabBlock.DrawBlock）在每帧按 <see cref="IsStructureValid"/> 结果变色。
        /// </summary>
        public void ApplyStructureBit(Point3 point, bool valid) {
            int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
            int data = Terrain.ExtractData(cellValue);
            int newData = valid ? (data & ~1) : (data | 1);
            if (newData == data) {
                return;
            }
            m_subsystemTerrain.ChangeCell(
                point.X, point.Y, point.Z,
                Terrain.ReplaceData(cellValue, newData),
                true,
                null);
        }

        /// <summary>
        /// 结构校验（毯悬空在 3×3 空气层中心，中心列下方一层为棋盘）：
        ///   毯层（毯 Y）：外围 8 格须为空气（中心是毯本身，已保证）；
        ///   下层（毯 Y-1）：
        ///     岩 锗 岩
        ///     锗 岩 锗
        ///     岩 锗 岩
        ///   「岩」= 生息岩（GrownStoneBlock）或锗晶块（SemiconductorBlock）；
        ///   「锗」= 仅锗晶块（SemiconductorBlock）。
        /// 只要任一格不符合即判结构无效（渲染变红 + 法杖提示）。
        /// </summary>
        public bool IsStructureValid(Point3 slab) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            // 毯层：外围 8 格须为空气
            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    if (dx == 0 && dz == 0) {
                        continue;
                    }
                    if (terrain.GetCellContents(slab.X + dx, slab.Y, slab.Z + dz) != 0) {
                        return false;
                    }
                }
            }
            // 下层（Y-1）棋盘 岩锗岩/锗岩锗/岩锗岩：
            //   「岩」位（中心 + 4 角，dx+dz 为偶数）= 生息岩或锗晶块；
            //   「锗」位（4 个边中点，dx+dz 为奇数）= 仅锗晶块。
            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    int cell = terrain.GetCellContents(slab.X + dx, slab.Y - 1, slab.Z + dz);
                    if ((dx + dz) % 2 == 0) {
                        // 岩位：中心与 4 个角
                        if (!IsRock(cell)) {
                            return false;
                        }
                    }
                    else {
                        // 锗位：4 个边中点
                        if (!IsSemiconductor(cell)) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>「岩」位：生息岩或锗晶块（入参为 <c>GetCellContents</c> 返回的方块索引，直接比较）。</summary>
        public bool IsRock(int contents) =>
            contents == m_grownStoneIndex || contents == m_semicontductorIndex;

        /// <summary>「锗」位：仅锗晶块（SemiconductorBlock）。</summary>
        public bool IsSemiconductor(int contents) =>
            contents == m_semicontductorIndex;

        public void Update(float dt) {
            double time = m_subsystemGameInfo.TotalElapsedGameTime;
            foreach (Point3 point in m_slabs.Keys) {
                SlabInfo info = m_slabs[point];
                bool valid = IsStructureValid(point);
                if (valid != info.StructureValid || !info.StructureBitApplied) {
                    info.StructureValid = valid;
                    ApplyStructureBit(point, valid);
                    info.StructureBitApplied = true;
                }
                if (!info.StructureValid) {
                    // 结构错误：供入的魔力直接丢弃
                    info.Receiver.ManaStorage.SetCurrent(0f);
                    info.MaterialsReady = false;
                    continue;
                }
                if (time >= info.NextMaterialsCheckTime) {
                    info.NextMaterialsCheckTime = time + 0.25;
                    info.MaterialsReady = ComputeMaterialsReady(point, info);
                }
                AttractMaterials(point, info);
                if (info.Receiver.ManaStorage.IsEmpty) {
                    continue;
                }
                // 材料齐备 + 魔力足额 → 合成；材料错/不足时存魔清空
                try {
                    TryCraft(point, info);
                }
                catch (Exception e) {
                    Log.Error($"[PhytoMana]TerraSlab craft error at {point}: {e}");
                }
            }
        }

        /// <summary>材料齐备（掉落带 + 存量，一档或二档）：供魔门与吸附判定共用。</summary>
        public bool ComputeMaterialsReady(Point3 point, SlabInfo info) {
            CollectMaterials(point, out int diamondChunks, out int diamondBlocks,
                out int manaIngotItems, out int manaSteelBlocks, out int terraIngotBlocks);
            bool tier2 = diamondBlocks + GetStoredCount(info, m_manaDiamondBlockIndex) >= 1
                && manaSteelBlocks + GetStoredCount(info, m_manaSteelBlockIndex) >= 1
                && terraIngotBlocks + GetStoredCount(info, m_semicontductorIndex) >= 1;
            bool tier1 = diamondChunks + GetStoredCount(info, m_manaDiamondChunkIndex) >= 1
                && manaIngotItems + GetStoredCount(info, m_manaIngotIndex) >= 1;
            return tier1 || tier2;
        }

        /// <summary>毯存量材料中某类（按 contents）的数量。</summary>
        public int GetStoredCount(SlabInfo info, int contents) {
            int count = 0;
            foreach (KeyValuePair<int, int> item in info.Items) {
                if (Terrain.ExtractContents(item.Key) == contents) {
                    count += item.Value;
                }
            }
            return count;
        }

        /// <summary>吸附：结构有效时，带内（y+1~y+2，水平 ±1）5 种材料掉落物自动飞向毯顶中心
        /// （引擎 Pickable 飞行机制，0.25 格内清除目标落体静止）。</summary>
        public void AttractMaterials(Point3 point, SlabInfo info) {
            if (!info.StructureValid) {
                return;
            }
            Vector3 center = new(point.X + 0.5f, point.Y + 1.0f, point.Z + 0.5f);
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                Vector3 position = pickable.Position;
                if (position.X < point.X - 1 || position.X > point.X + 1
                    || position.Z < point.Z - 1 || position.Z > point.Z + 1
                    || position.Y < point.Y + 0.5f || position.Y > point.Y + 2.5f) {
                    continue;
                }
                if (IsSlabMaterial(Terrain.ExtractContents(pickable.Value))) {
                    pickable.FlyToPosition = center;
                }
            }
        }

        /// <summary>
        /// 两档合成（二档优先）：
        /// 一档 魔力钻石 + 魔力钢锭 + 锗晶锭 → 泰拉锭×1；
        /// 二档 魔力钻石块 + 魔力钢锭块 + 锗晶块 → 泰拉锭块×1。
        /// 材料齐备且魔力足额时合成；材料错/不足时不注入魔力且清空已存魔力（spec：存魔丢失）。
        /// </summary>
        public void TryCraft(Point3 point, SlabInfo info) {
            CollectMaterials(point, out int diamondChunks, out int diamondBlocks,
                out int manaIngotItems, out int manaSteelBlocks, out int terraIngotBlocks);
            bool tier2Ready = diamondBlocks + GetStoredCount(info, m_manaDiamondBlockIndex) >= 1
                && manaSteelBlocks + GetStoredCount(info, m_manaSteelBlockIndex) >= 1
                && terraIngotBlocks + GetStoredCount(info, m_semicontductorIndex) >= 1;
            bool tier1Ready = diamondChunks + GetStoredCount(info, m_manaDiamondChunkIndex) >= 1
                && manaIngotItems + GetStoredCount(info, m_manaIngotIndex) >= 1;
            if (!tier2Ready && !tier1Ready) {
                // 材料错/不足：不注入魔力，已存魔力丢失
                info.Receiver.ManaStorage.SetCurrent(0f);
                return;
            }
            if (tier2Ready) {
                float cost = Tier2Cost();
                if (info.Receiver.ManaStorage.Current >= cost) {
                    info.Receiver.ManaStorage.Take(cost);
                    ConsumeMaterials(point, info, m_manaDiamondBlockIndex, 1);
                    ConsumeMaterials(point, info, m_manaSteelBlockIndex, 1);
                    ConsumeMaterials(point, info, m_semicontductorIndex, 1);
                    SpawnResult(point, m_terraBlockIndex);
                    SpawnCornerBurst(point);
                    return;
                }
            }
            if (tier1Ready) {
                float cost = Tier1Cost();
                if (info.Receiver.ManaStorage.Current >= cost) {
                    info.Receiver.ManaStorage.Take(cost);
                    ConsumeMaterials(point, info, m_manaDiamondChunkIndex, 1);
                    ConsumeMaterials(point, info, m_manaIngotIndex, 1);
                    SpawnResult(point, m_terraIngotIndex);
                    SpawnCornerBurst(point);
                }
            }
        }

        /// <summary>消耗某类材料（按 contents）count 件：先扣毯存量（逐件减），缺口由掉落带补齐。</summary>
        public void ConsumeMaterials(Point3 point, SlabInfo info, int contents, int count) {
            int fromStored = Math.Min(GetStoredCount(info, contents), count);
            int remaining = fromStored;
            foreach (KeyValuePair<int, int> item in info.Items) {
                if (remaining <= 0) {
                    break;
                }
                if (Terrain.ExtractContents(item.Key) != contents) {
                    continue;
                }
                int take = Math.Min(item.Value, remaining);
                info.Items[item.Key] = item.Value - take;
                remaining -= take;
            }
            int fromDrops = count - fromStored;
            if (fromDrops > 0) {
                ConsumePickables(point, contents, fromDrops);
            }
        }

        /// <summary>一档耗魔：魔力池半分（1900mn，池满 3800 的一半）。</summary>
        public float Tier1Cost() => 1900f;

        /// <summary>二档耗魔：魔力池半分 ×10（19000mn）。</summary>
        public float Tier2Cost() => 19000f;

        /// <summary>收集毯顶面（y+1~y+2，水平 ±1）掉落物中的各类材料数量。</summary>
        public void CollectMaterials(Point3 point,
            out int diamondChunks,
            out int diamondBlocks,
            out int manaIngotItems,
            out int manaSteelBlocks,
            out int terraIngotBlocks) {
            diamondChunks = 0;
            diamondBlocks = 0;
            manaIngotItems = 0;
            manaSteelBlocks = 0;
            terraIngotBlocks = 0;
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                Vector3 position = pickable.Position;
                if (position.X < point.X - 1 || position.X > point.X + 1
                    || position.Z < point.Z - 1 || position.Z > point.Z + 1
                    || position.Y < point.Y + 0.5f || position.Y > point.Y + 2.5f) {
                    continue;
                }
                int contents = Terrain.ExtractContents(pickable.Value);
                if (contents == m_manaDiamondChunkIndex) {
                    diamondChunks += pickable.Count;
                }
                else if (contents == m_manaDiamondBlockIndex) {
                    diamondBlocks += pickable.Count;
                }
                else if (contents == m_manaIngotIndex) {
                    manaIngotItems += pickable.Count;
                }
                else if (contents == m_manaSteelBlockIndex) {
                    manaSteelBlocks += pickable.Count;
                }
                else if (contents == m_semicontductorIndex) {
                    terraIngotBlocks += pickable.Count;
                }
            }
        }

        /// <summary>消耗指定格内的掉落物 count 个（逐件扣除）。</summary>
        public void ConsumePickables(Point3 point, int contents, int count) {
            int remaining = count;
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove || remaining <= 0) {
                    continue;
                }
                Vector3 position = pickable.Position;
                if (position.X < point.X - 1 || position.X > point.X + 1
                    || position.Z < point.Z - 1 || position.Z > point.Z + 1
                    || position.Y < point.Y + 0.5f || position.Y > point.Y + 2.5f) {
                    continue;
                }
                if (Terrain.ExtractContents(pickable.Value) != contents) {
                    continue;
                }
                int take = Math.Min(pickable.Count, remaining);
                pickable.Count -= take;
                remaining -= take;
                if (pickable.Count <= 0) {
                    pickable.ToRemove = true;
                }
            }
        }

        /// <summary>产物从毯面弹出（毯 +1 上方）。</summary>
        public void SpawnResult(Point3 point, int contents) {
            Vector3 position = new(point.X + 0.5f, point.Y + 1.1f, point.Z + 0.5f);
            m_subsystemPickables.AddPickable(Terrain.MakeBlockValue(contents), 1, position, null, null);
        }

        /// <summary>合成时 4 个角（+1 高度）深蓝 1f 粒子每 0.25s 向毯+1 收敛。</summary>
        public void SpawnCornerBurst(Point3 point) {
            Vector3 center = new(point.X + 0.5f, point.Y + 1.0f, point.Z + 0.5f);
            for (int i = 0; i < 4; i++) {
                int dx = i % 2 == 0 ? 1 : -1;
                int dz = i / 2 == 0 ? 1 : -1;
                Vector3 corner = new(point.X + 0.5f + dx, point.Y + 1.0f, point.Z + 0.5f + dz);
                m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    corner,
                    1f,
                    0.25f,
                    Color.DarkBlue,
                    center,
                    1
                ));
            }
        }

        /// <summary>
        /// 法杖工作模式指向毯：结构无效提示红色；有效时显示魔力与所缺材料。
        /// </summary>
        public void ShowStatus(Point3 point, ComponentPlayer player) {
            bool valid = IsStructureValid(point);
            if (!valid) {
                player.ComponentGui.DisplaySmallMessage(
                    LanguageControl.Get("GrownStaffMessages", "SlabStructureInvalid"),
                    Color.Red, false, false);
                return;
            }
            float max = m_subsystemMana.GetMaxManaAmount(m_slabIndex);
            float current = m_subsystemMana.GetManaAmount(point);
            CollectMaterials(point, out int diamondChunks, out int diamondBlocks,
                out int manaIngotItems, out int manaSteelBlocks, out int terraIngotBlocks);
            int storedDiamondChunks = 0, storedDiamondBlocks = 0, storedManaIngot = 0, storedManaSteel = 0, storedSemi = 0;
            if (m_slabs.TryGetValue(point, out SlabInfo info)) {
                storedDiamondChunks = GetStoredCount(info, m_manaDiamondChunkIndex);
                storedDiamondBlocks = GetStoredCount(info, m_manaDiamondBlockIndex);
                storedManaIngot = GetStoredCount(info, m_manaIngotIndex);
                storedManaSteel = GetStoredCount(info, m_manaSteelBlockIndex);
                storedSemi = GetStoredCount(info, m_semicontductorIndex);
            }
            int missingTier1 = 0;
            if (diamondChunks + storedDiamondChunks < 1) missingTier1++;
            if (manaIngotItems + storedManaIngot < 1) missingTier1++;
            int missingTier2 = 0;
            if (diamondBlocks + storedDiamondBlocks < 1) missingTier2++;
            if (manaSteelBlocks + storedManaSteel < 1) missingTier2++;
            if (terraIngotBlocks + storedSemi < 1) missingTier2++;
            string materials = missingTier1 == 0 && missingTier2 == 0
                ? LanguageControl.Get("GrownStaffMessages", "SlabMaterialsReady")
                : string.Format(LanguageControl.Get("GrownStaffMessages", "SlabMissingItems"),
                    Math.Min(missingTier1, missingTier2));
            string message = string.Format(
                LanguageControl.Get("GrownStaffMessages", "SlabStatusFormat"),
                SubsystemGrownStaffBehavior.FormatMana(current),
                SubsystemGrownStaffBehavior.FormatMana(max),
                materials);
            player.ComponentGui.DisplaySmallMessage(message, Color.Green, false, false);
            SubsystemAudio audio = Project.FindSubsystem<SubsystemAudio>(false);
            audio?.PlaySound("Audio/PhytoMana/ding", 1f, 0f, 0f, 0f);
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            base.Save(valuesDictionary);
            // 点击存放的材料账本：「x,y,z,v1,c1,v2,c2,...;」（区块已卸载的毯其存量也随账本保留）
            StringBuilder items = new();
            foreach (KeyValuePair<Point3, Dictionary<int, int>> pair in m_slabItems) {
                if (pair.Value.Count == 0) {
                    continue;
                }
                items.Append(pair.Key.X.ToString(CultureInfo.InvariantCulture)).Append(',');
                items.Append(pair.Key.Y.ToString(CultureInfo.InvariantCulture)).Append(',');
                items.Append(pair.Key.Z.ToString(CultureInfo.InvariantCulture));
                foreach (KeyValuePair<int, int> item in pair.Value) {
                    if (item.Value > 0) {
                        items.Append(',').Append(item.Key.ToString(CultureInfo.InvariantCulture))
                            .Append(',').Append(item.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }
                items.Append(';');
            }
            valuesDictionary.SetValue("TerraSlabItems", items.ToString());
        }
    }
}