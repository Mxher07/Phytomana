using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana;
using Phytomana.Api;
using TemplatesDatabase;

namespace Game {
    /// <summary>
    /// 泰拉三工具的统一行为子系统：
    /// 1. 泰拉刃——挥击 50% 概率追加发射剑气（直线无重力，命中 3.5 固定伤害可击杀，穿玩家）；
    /// 2. 泰拉砍伐者——破坏原木/树叶时连锁伐木（广度优先同类型扩散 3×3，上限 63³；潜行禁用）；
    /// 3. 泰拉破坏者——存魔升级（D~SS），潜行切换激活档位；激活后破坏时按档位水平范围
    ///    挖掘同类方块（每格 -7mn），掉落进魔法池 3×3×3 内 1000mn/s 充能。
    /// 引擎钩子由 <see cref="TerraToolHooks"/> 经 <see cref="PhytomanaMod"/> 注册并转发给本子系统。
    /// </summary>
    public class SubsystemTerraToolBehavior : Subsystem, IUpdateable {
        public class BreakerState {
            /// <summary>泰拉破坏者自身存魔（升级与范围挖掘消耗）。</summary>
            public float Mana;

            /// <summary>当前激活档位：0=D（无范围挖掘），1=C，2=B，3=A，4=S，5=SS。</summary>
            public int ActiveLevel;

            /// <summary>玩家 Id（与 m_breakerStates 键一致），用于掏出提示的去重。</summary>
            public int PlayerId;

            public bool IsActive => ActiveLevel > 0;
        }

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemProjectiles m_subsystemProjectiles;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemMana m_subsystemMana;

        public int m_terraBladeIndex;

        public int m_terraCutterIndex;

        public int m_terraBreakerIndex;

        public int m_manaPoolIndex;

        public int m_manaTabletIndex;

        public float m_chargeTimer;

        /// <summary>玩家 Id → 破坏者状态（存魔/激活档位）。</summary>
        public Dictionary<int, BreakerState> m_breakerStates = [];

        /// <summary>玩家 Id → 上一帧是否手持破坏者（用于检测掏出瞬间）。</summary>
        public Dictionary<int, bool> m_wasHoldingBreaker = [];

        /// <summary>玩家 Id → 掏出破坏者后提示是否已发（避免手持期间重复刷）。</summary>
        public Dictionary<int, bool> m_breakerHintShown = [];

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemAudio m_subsystemAudio;

        public Game.Random m_random = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
            m_terraBladeIndex = BlocksManager.GetBlockIndex<TerraBladeBlock>();
            m_terraCutterIndex = BlocksManager.GetBlockIndex<TerraCutterBlock>();
            m_terraBreakerIndex = BlocksManager.GetBlockIndex<TerraBreakerBlock>();
            m_manaPoolIndex = BlocksManager.GetBlockIndex<ManaPoolBlock>();
            m_manaTabletIndex = BlocksManager.GetBlockIndex<ManaTabletBlock>();
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            TerraToolHooks.m_terraToolBehavior = this;
        }

        // ===== 泰拉刃：剑气 =====

        /// <summary>
        /// 泰拉刃命中生物：原版近击后 50% 追加一枚剑气。
        /// 剑气命中生物的 3.5 固定伤害见 <see cref="HandleProjectileHitBody"/>。
        /// </summary>
        public void HandleMinerHit2(
            ComponentMiner miner,
            ComponentBody body,
            Vector3 hitPoint,
            Vector3 hitDirection,
            ref int durabilityReduction) {
            if (Terrain.ExtractContents(miner.ActiveBlockValue) != m_terraBladeIndex) {
                return;
            }
            if (!m_random.Bool()) {
                return;
            }
            Vector3 origin = hitPoint + hitDirection * 0.5f;
            Vector3 velocity = hitDirection * TerraSwordQiProjectile.Speed;
            m_subsystemProjectiles.FireProjectile<TerraSwordQiProjectile>(
                Terrain.MakeBlockValue(m_terraBladeIndex),
                origin,
                velocity,
                Vector3.Zero,
                miner.ComponentCreature
            );
            // 剑气发射额外消耗 1 点耐久
            durabilityReduction++;
        }

        /// <summary>剑气命中生物：固定 3.5 伤害（可击杀），对玩家穿透不伤。</summary>
        public void HandleProjectileHitBody(
            Projectile projectile,
            BodyRaycastResult bodyRaycastResult,
            ref Attackment attackment,
            ref bool ignoreBody) {
            if (!(projectile is TerraSwordQiProjectile)) {
                return;
            }
            attackment.AttackPower = TerraSwordQiProjectile.FixedDamage;
            Entity hitEntity = bodyRaycastResult.ComponentBody?.Entity;
            if (hitEntity != null && hitEntity.FindComponent<ComponentPlayer>() != null) {
                ignoreBody = true;
            }
        }

        // ===== 泰拉砍伐者：连锁伐木 =====

        /// <summary>
        /// 挖掘钩子：泰拉砍伐者破坏原木/树叶时连锁伐木。潜行禁用。
        /// 破坏发生在原版 Dig 完成帧（digProgress>=1）。
        /// </summary>
        public void HandleMinerDig(ComponentMiner miner, TerrainRaycastResult raycastResult, float digProgress) {
            if (digProgress < 1f) {
                return;
            }
            int contents = Terrain.ExtractContents(miner.ActiveBlockValue);
            if (contents != m_terraCutterIndex) {
                return;
            }
            CellFace face = raycastResult.CellFace;
            int cellContents = m_subsystemTerrain.Terrain.GetCellContents(face.X, face.Y, face.Z);
            Block cellBlock = BlocksManager.Blocks[cellContents];
            if (!(cellBlock is WoodBlock) && !(cellBlock is LeavesBlock)) {
                return;
            }
            ComponentPlayer player = miner.ComponentPlayer;
            ComponentBody body = player?.Entity?.FindComponent<ComponentBody>();
            if (body != null && body.IsCrouching) {
                return;
            }
            Felling(miner, new Point3(face.X, face.Y, face.Z));
        }

        /// <summary>
        /// 连锁伐木：从破坏点广度优先扩散 3×3 同类型木/叶（上限 63³），
        /// 非主格逐格 DestroyCell。每根原木 -1 耐久、每 3 片树叶 -1 耐久
        /// （主格耐久由原版 OnBlockDug 计 1，这里补上其余格）。
        /// </summary>
        public void Felling(ComponentMiner miner, Point3 start) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            int type = terrain.GetCellContents(start.X, start.Y, start.Z);
            if (type == 0) {
                return;
            }
            List<Point3> queue = [start];
            HashSet<Point3> visited = [start];
            while (queue.Count > 0 && visited.Count < 63 * 63 * 63) {
                Point3 p = queue[^1];
                queue.RemoveAt(queue.Count - 1);
                for (int dx = -3; dx <= 3; dx++) {
                    for (int dy = -3; dy <= 3; dy++) {
                        for (int dz = -3; dz <= 3; dz++) {
                            if (dx == 0 && dy == 0 && dz == 0) {
                                continue;
                            }
                            Point3 n = new(p.X + dx, p.Y + dy, p.Z + dz);
                            if (!visited.Add(n)) {
                                continue;
                            }
                            if (terrain.GetCellContents(n.X, n.Y, n.Z) == type) {
                                queue.Add(n);
                            }
                        }
                    }
                }
            }
            int woodCount = 0;
            int leafCount = 0;
            foreach (Point3 p in visited) {
                int c = terrain.GetCellContents(p.X, p.Y, p.Z);
                if (c != type) {
                    continue;
                }
                if (BlocksManager.Blocks[c] is WoodBlock) {
                    woodCount++;
                }
                else if (BlocksManager.Blocks[c] is LeavesBlock) {
                    leafCount++;
                }
            }
            int extraDurability = Math.Max(0, woodCount + (leafCount + 2) / 3 - 1);
            if (extraDurability > 0) {
                miner.DamageActiveTool(extraDurability);
            }
            foreach (Point3 p in visited) {
                if (p == start) {
                    continue;
                }
                if (terrain.GetCellContents(p.X, p.Y, p.Z) == type) {
                    m_subsystemTerrain.DestroyCell(0, p.X, p.Y, p.Z, 0, false, false);
                }
            }
        }

        // ===== 泰拉破坏者：等级挖掘 =====

        public BreakerState GetBreakerState(ComponentMiner miner) {
            int playerKey = miner.Entity?.Id ?? 0;
            if (!m_breakerStates.TryGetValue(playerKey, out BreakerState state)) {
                state = new BreakerState();
                m_breakerStates[playerKey] = state;
            }
            return state;
        }

        public int GetBreakerRange(int level) {
            return level switch {
                <= 0 => 0,
                1 => 1, // C: 1×3×1
                2 => 1, // B: 1×3×1
                3 => 2, // A: 5×1×5（水平半边）
                4 => 3, // S: 7×1×7
                _ => 4 // SS: 9×1×9
            };
        }

        /// <summary>破坏者激活档位下的水平半边长（C/B 为 1，A/S/SS 递增）。</summary>
        public int GetBreakerHorizontalRange(int level) {
            return GetBreakerRange(level);
        }

        /// <summary>
        /// 破坏者范围挖掘：主格被挖后，水平同层范围内与主格同类的方块按 -7mn/格 顺带挖掉。
        /// D 档（未激活）或魔力不足时不挖。每挖一格扣魔后若存魔跌破当前档位门槛，自动降档。
        /// </summary>
        public void HandleBlockDug(
            ComponentMiner miner,
            BlockPlacementData digValue,
            int cellValue,
            ref int durabilityReduction,
            ref int playerDataDugAdd) {
            int contents = Terrain.ExtractContents(miner.ActiveBlockValue);
            if (contents != m_terraBreakerIndex) {
                return;
            }
            BreakerState state = GetBreakerState(miner);
            int level = state.ActiveLevel;
            if (level <= 0) {
                return;
            }
            int targetContents = Terrain.ExtractContents(cellValue);
            if (targetContents == 0) {
                return;
            }
            float costPerBlock = PhytoConfig.Instance.TerraBreakerPerBlockCost;
            SubsystemTerraSetBehavior setBehavior = Project.FindSubsystem<SubsystemTerraSetBehavior>(false);
            int range = GetBreakerHorizontalRange(level);
            Point3 center = new(digValue.CellFace.X, digValue.CellFace.Y, digValue.CellFace.Z);
            Terrain terrain = m_subsystemTerrain.Terrain;
            for (int dx = -range; dx <= range; dx++) {
                for (int dz = -range; dz <= range; dz++) {
                    if (dx == 0 && dz == 0) {
                        continue;
                    }
                    int x = center.X + dx;
                    int z = center.Z + dz;
                    if (terrain.GetCellContents(x, center.Y, z) != targetContents) {
                        continue;
                    }
                    if (state.Mana < costPerBlock) {
                        return;
                    }
                    state.Mana -= costPerBlock;
                    // 存魔跌破当前档位门槛时自动降档（D 档无门槛）
                    while (state.ActiveLevel > 0
                        && setBehavior != null
                        && state.Mana < setBehavior.RequiredManaForLevel(state.ActiveLevel)) {
                        state.ActiveLevel--;
                    }
                    durabilityReduction++;
                    playerDataDugAdd++;
                    m_subsystemTerrain.DestroyCell(0, x, center.Y, z, 0, false, false);
                }
            }
        }

        public void Update(float dt) {
            m_chargeTimer += dt;
            if (m_chargeTimer >= 1f) {
                m_chargeTimer = 0f;
                ChargeDroppedBreakers();
            }
            CheckBreakerPullOut(dt);
        }

        /// <summary>
        /// 掏出提示：玩家手持泰拉破坏者、且处于潜行（蹲下）状态时，
        /// 首次掏出瞬间显示当前档位与存魔（同档不重复刷）。
        /// </summary>
        public void CheckBreakerPullOut(float dt) {
            foreach (ComponentPlayer player in m_subsystemPlayers.ComponentPlayers) {
                if (player.Entity == null) {
                    continue;
                }
                ComponentMiner miner = player.ComponentMiner;
                if (miner == null) {
                    continue;
                }
                int playerKey = player.Entity.Id;
                bool holdingBreaker =
                    Terrain.ExtractContents(miner.ActiveBlockValue) == m_terraBreakerIndex;
                bool wasHolding = m_wasHoldingBreaker.GetValueOrDefault(playerKey, false);
                m_wasHoldingBreaker[playerKey] = holdingBreaker;

                // 掏出瞬间（false→true）才考虑提示，避免手持期间反复触发
                if (!holdingBreaker || wasHolding) {
                    continue;
                }
                if (m_breakerHintShown.TryGetValue(playerKey, out bool shown) && shown) {
                    continue;
                }
                ComponentBody body = player.Entity.FindComponent<ComponentBody>();
                if (body == null || !body.IsCrouching) {
                    continue;
                }
                BreakerState state = GetBreakerState(miner);
                string levelName = state.ActiveLevel switch {
                    1 => "C",
                    2 => "B",
                    3 => "A",
                    4 => "S",
                    5 => "SS",
                    _ => "D"
                };
                string message = string.Format(
                    LanguageControl.Get("GrownStaffMessages", "BreakerStatusFormat"),
                    levelName,
                    SubsystemGrownStaffBehavior.FormatMana(state.Mana));
                player.ComponentGui.DisplaySmallMessage(message, Color.White, false, true);
                m_breakerHintShown[playerKey] = true;
            }
            // 离开破坏者手持时清除标记，允许下次再掏出时重新提示
            foreach (int key in m_wasHoldingBreaker.Keys.Where(k => !m_wasHoldingBreaker[k]).ToList()) {
                m_breakerHintShown.Remove(key);
            }
        }

        /// <summary>
        /// 每 1s 扫描掉落物：泰拉破坏者落在魔法池 3×3×3 内且池子存量 ≥1000mn/s 充能速率时，
        /// 吸魔充入破坏者自身存魔（经掉落物归属玩家）。优先扣魔法池存量。
        /// </summary>
        public void ChargeDroppedBreakers() {
            float rate = PhytoConfig.Instance.TerraBreakerPoolChargeRate;
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove
                    || Terrain.ExtractContents(pickable.Value) != m_terraBreakerIndex) {
                    continue;
                }
                Entity ownerEntity = pickable.OwnerEntity;
                if (ownerEntity == null) {
                    continue;
                }
                Point3 cell = new(
                    (int)MathF.Round(pickable.Position.X - 0.5f),
                    (int)MathF.Round(pickable.Position.Y - 0.5f),
                    (int)MathF.Round(pickable.Position.Z - 0.5f));
                foreach (Point3 poolPoint in FindPoolsNear(cell)) {
                    if (!TryGetPool(poolPoint, out ManaPool pool)) {
                        continue;
                    }
                    if (pool.ManaStorage.Current < rate) {
                        continue;
                    }
                    float taken = pool.ManaStorage.Take(rate);
                    BreakerState state = GetBreakerStateByEntity(ownerEntity);
                    if (state != null) {
                        state.Mana += taken;
                    }
                }
            }
        }

        public BreakerState GetBreakerStateByEntity(Entity entity) {
            ComponentMiner miner = entity?.FindComponent<ComponentMiner>();
            if (miner == null) {
                return null;
            }
            return GetBreakerState(miner);
        }

        /// <summary>取坐标 3×3×3 内是魔法池的所有格。</summary>
        public IEnumerable<Point3> FindPoolsNear(Point3 cell) {
            for (int dx = -1; dx <= 1; dx++) {
                for (int dy = -1; dy <= 1; dy++) {
                    for (int dz = -1; dz <= 1; dz++) {
                        Point3 p = new(cell.X + dx, cell.Y + dy, cell.Z + dz);
                        if (m_subsystemTerrain.Terrain.GetCellContents(p.X, p.Y, p.Z) == m_manaPoolIndex) {
                            yield return p;
                        }
                    }
                }
            }
        }

        /// <summary>按坐标取魔法池节点（经 SubsystemMana 查活跃池）。</summary>
        public bool TryGetPool(Point3 point, out ManaPool pool) {
            pool = null;
            if (m_subsystemMana == null) {
                return false;
            }
            m_subsystemMana.m_network.GetActiveReceivers(m_subsystemMana.m_receiverBuffer);
            foreach (IManaReceiver receiver in m_subsystemMana.m_receiverBuffer) {
                if (receiver is ManaPool p && p.Position == point) {
                    pool = p;
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 泰拉工具引擎钩子调度器：ModLoader 虚方法转发到当前世界的 <see cref="SubsystemTerraToolBehavior"/>。
    /// 由 <see cref="PhytomanaMod"/> 在 __ModInitialize 注册。
    /// </summary>
    public class TerraToolHooks : ModLoader {
        public static SubsystemTerraToolBehavior m_terraToolBehavior;

        public override void OnMinerHit2(
            ComponentMiner miner,
            ComponentBody componentBody,
            Vector3 hitPoint,
            Vector3 hitDirection,
            ref int durabilityReduction,
            ref Attackment attackment) {
            m_terraToolBehavior?.HandleMinerHit2(miner, componentBody, hitPoint, hitDirection, ref durabilityReduction);
        }

        public override void OnMinerDig(
            ComponentMiner miner,
            TerrainRaycastResult raycastResult,
            ref float digProgress,
            out bool digged) {
            digged = false;
            m_terraToolBehavior?.HandleMinerDig(miner, raycastResult, digProgress);
        }

        public override void OnBlockDug(
            ComponentMiner miner,
            BlockPlacementData digValue,
            int cellValue,
            ref int durabilityReduction,
            ref bool mute,
            ref int playerDataDugAdd) {
            m_terraToolBehavior?.HandleBlockDug(miner, digValue, cellValue, ref durabilityReduction, ref playerDataDugAdd);
        }

        public override void OnProjectileHitBody(
            Projectile projectile,
            BodyRaycastResult bodyRaycastResult,
            ref Attackment attackment,
            ref Vector3 velocityAfterAttack,
            ref Vector3 angularVelocityAfterAttack,
            ref bool ignoreBody) {
            m_terraToolBehavior?.HandleProjectileHitBody(projectile, bodyRaycastResult, ref attackment, ref ignoreBody);
        }
    }
}
