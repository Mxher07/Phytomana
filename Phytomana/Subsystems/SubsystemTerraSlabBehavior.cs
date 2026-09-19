using System;
using System.Collections.Generic;
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
    /// 经法杖绑定发射器供魔；材料由毯顶面（y+1~y+2，水平 ±1）的掉落物提供。
    /// 结构（毯平铺在地面上）：
    ///   毯层（y）：3×3 全部为 生息岩/锗晶块（棋盘 G B G / B G B / G B G）；
    ///   上层（y+1）：中心为毯本身，外围 8 格为 生息岩/锗晶块。
    /// 结构错误时：供入的魔力每周期清空，法杖指向提示「结构无效」。
    /// 结构正确且材料齐备 + 魔力足额时：
    ///   一档（魔力钻石 + 魔力钢锭 + 锗晶锭）→ 消耗魔力池半分魔力，产 泰拉锭×1；
    ///   二档（魔力钻石块 + 魔力钢锭块 + 锗晶块）→ 消耗魔力池半分魔力，产 泰拉锭块×1。
    /// 合成时 4 个角（+1 高度）深蓝 1f 粒子每 0.25s 向毯+1 收敛。
    /// </summary>
    public class SubsystemTerraSlabBehavior : SubsystemBlockBehavior, IUpdateable {
        public class TerraSlabReceiver : IManaReceiver {
            public const float DefaultMaxMana = 38000f;

            public Point3 Position { get; }

            public ManaStorage ManaStorage { get; }

            public TerraSlabReceiver(Point3 position) {
                Position = position;
                ManaStorage = new ManaStorage(ManaBlockRegistry.GetMaxMana("TerraSlabBlock", DefaultMaxMana));
            }
        }

        public class SlabInfo {
            public TerraSlabReceiver Receiver;

            public bool StructureValid;

            public bool StructureBitApplied;

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

        public int m_terraIngotIndex;

        public int m_manaDiamondChunkIndex;

        public int m_manaDiamondBlockIndex;

        public int m_manaIngotIndex;

        public int m_manaSteelBlockIndex;

        public Dictionary<Point3, SlabInfo> m_slabs = [];

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
            m_terraIngotIndex = BlocksManager.GetBlockIndex<TerraIngotBlock>();
            m_manaDiamondChunkIndex = BlocksManager.GetBlockIndex<ManaDiamondChunkBlock>();
            m_manaDiamondBlockIndex = BlocksManager.GetBlockIndex<ManaDiamondBlock>();
            m_manaIngotIndex = BlocksManager.GetBlockIndex<ManaIngotBlock>();
            m_manaSteelBlockIndex = BlocksManager.GetBlockIndex<ManaBlock>();
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            Point3 point = new(x, y, z);
            TerraSlabReceiver receiver = new(point);
            m_subsystemMana.m_network.RegisterReceiver(receiver);
            SlabInfo info = new() {
                Receiver = receiver,
                StructureValid = IsStructureValid(point),
                StructureBitApplied = false,
                NextParticleTime = m_subsystemGameInfo.TotalElapsedGameTime
            };
            m_slabs[point] = info;
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            OnBlockAdded(value, 0, x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            Point3 point = new(x, y, z);
            if (m_slabs.TryGetValue(point, out SlabInfo info)) {
                m_subsystemMana.m_network.UnregisterReceiver(info.Receiver, true);
                m_slabs.Remove(point);
            }
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
        /// 结构校验（毯平铺在棋盘地面上，毯本体在 Y+1 层中心）：
        ///   下层（毯 Y）：3×3 棋盘 —— G B G / B G B / G B G（G=生息岩 B=锗晶块，可同色）；
        ///   上层（毯 Y+1）：8 邻格须为 生息岩/锗晶块，中心是毯本身（已保证）。
        /// 只要任一格不符合即判结构无效（渲染变红 + 法杖提示）。
        /// </summary>
        public bool IsStructureValid(Point3 slab) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            // 下层 3×3 棋盘
            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    if (!IsGrownOrTerra(terrain.GetCellContents(slab.X + dx, slab.Y, slab.Z + dz))) {
                        return false;
                    }
                }
            }
            // 上层 8 邻格
            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    if (dx == 0 && dz == 0) {
                        continue;
                    }
                    if (!IsGrownOrTerra(terrain.GetCellContents(slab.X + dx, slab.Y + 1, slab.Z + dz))) {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool IsGrownOrTerra(int cellContents) {
            int contents = Terrain.ExtractContents(cellContents);
            return contents == m_grownStoneIndex || contents == m_terraBlockIndex;
        }

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
                    continue;
                }
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

        /// <summary>
        /// 两档合成（二档优先）：
        /// 一档 魔力钻石 + 魔力钢锭 + 锗晶锭 → 泰拉锭×1；
        /// 二档 魔力钻石块 + 魔力钢锭块 + 锗晶块 → 泰拉锭块×1。
        /// 材料齐备且魔力足额时合成；材料错/不足时不注入魔力且清空已存魔力（spec：存魔丢失）。
        /// </summary>
        public void TryCraft(Point3 point, SlabInfo info) {
            CollectMaterials(point, out int diamondChunks, out int diamondBlocks,
                out int manaIngotItems, out int manaSteelBlocks, out int terraIngotBlocks);
            bool tier2Ready = diamondBlocks >= 1 && manaSteelBlocks >= 1 && terraIngotBlocks >= 1;
            bool tier1Ready = diamondChunks >= 1 && manaIngotItems >= 1;
            if (!tier2Ready && !tier1Ready) {
                // 材料错/不足：不注入魔力，已存魔力丢失
                info.Receiver.ManaStorage.SetCurrent(0f);
                return;
            }
            if (tier2Ready) {
                float cost = Tier2Cost();
                if (info.Receiver.ManaStorage.Current >= cost) {
                    info.Receiver.ManaStorage.Take(cost);
                    ConsumePickables(point, m_manaDiamondBlockIndex, 1);
                    ConsumePickables(point, m_manaSteelBlockIndex, 1);
                    ConsumePickables(point, m_terraBlockIndex, 1);
                    SpawnResult(point, m_terraBlockIndex);
                    SpawnCornerBurst(point);
                    return;
                }
            }
            if (tier1Ready) {
                float cost = Tier1Cost();
                if (info.Receiver.ManaStorage.Current >= cost) {
                    info.Receiver.ManaStorage.Take(cost);
                    ConsumePickables(point, m_manaDiamondChunkIndex, 1);
                    ConsumePickables(point, m_manaIngotIndex, 1);
                    SpawnResult(point, m_terraIngotIndex);
                    SpawnCornerBurst(point);
                }
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
                else if (contents == m_terraBlockIndex) {
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
            int missingTier1 = 0;
            if (diamondChunks < 1) missingTier1++;
            if (manaIngotItems < 1) missingTier1++;
            int missingTier2 = 0;
            if (diamondBlocks < 1) missingTier2++;
            if (manaSteelBlocks < 1) missingTier2++;
            if (terraIngotBlocks < 1) missingTier2++;
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
        }
    }
}
