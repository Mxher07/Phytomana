using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Engine;
using Engine.Graphics;
using Game;
using GameEntitySystem;
using Phytomana;
using Phytomana.Api;
using TemplatesDatabase;

namespace Game {
    /// <summary>
    /// 泰拉三工具的统一行为子系统：
    /// 1. 泰拉刃——「使用」（OnUse，任意方向点击即算，不影响挥击攻击）时 50% 概率
    ///    发射一枚剑气（直线无重力，命中 3.5 固定伤害，穿玩家）；指向生物时
    ///    原版的生物命中攻击逻辑照常结算，剑气是独立追加的一发；
    /// 2. 泰拉砍伐者——破坏原木/树叶时连锁伐木（同类型 BFS 扩散，实际破格在 Update
    ///    中按「0.5s 最多 8 格」限速消化，避免大量 DestroyCell 卡死游戏；潜行禁用）；
    /// 3. 泰拉破坏者——存魔/激活档位（0=D,1=C,2=B,3=A,4=S,5=SS）存于工具 data，
    ///    随存档保存（不随世界重建重置）；潜行双击挖掘切换档位；
    ///    挖掘时玩家处于蹲伏（潜行）才展开水平扩围（按档位 1/1/2/3/4 半边长，
    ///    每格 -7mn，自动降档）；预览边框：中心绿、将扩挖白；
    ///    掉落进魔法池 3×3×3 内 1000mn/s 充能。
    /// 引擎钩子由 <see cref="TerraToolHooks"/> 经 <see cref="PhytomanaMod"/> 注册并转发给本子系统。
    /// </summary>
    public class SubsystemTerraToolBehavior : Subsystem, IUpdateable, IDrawable {
        public class BreakerState {
            /// <summary>泰拉破坏者自身存魔（升级与范围挖掘消耗），随工具 data 保存。</summary>
            public int Mana;

            /// <summary>当前激活档位：0=D（无范围挖掘），1=C，2=B，3=A，4=S，5=SS。随工具 data 保存。</summary>
            public int ActiveLevel;

            public bool IsActive => ActiveLevel > 0;
        }

        /// <summary>砍伐链限速队列：同一次挖掘触发后，待破格在 0.5s 窗口内最多破 8 格。</summary>
        public class FellingQueue {
            public Queue<(Point3, int)> m_queue = [];

            public double m_windowStartTime = -1;

            public int m_windowRemaining = FellingBudget;
        }

        /// <summary>破坏者扩围预览：中心格 + 将被扩挖的格集合（渲染层画边框）。</summary>
        public class BreakerPreview {
            public Point3 Center;

            public List<Point3> Affected = [];

            public bool Valid;
        }

        /// <summary>砍伐链每 0.5s 窗口内最多破坏的格数。</summary>
        public const int FellingBudget = 8;

        /// <summary>砍伐链限速窗口（秒）。</summary>
        public const float FellingWindow = 0.5f;

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

        /// <summary>玩家 Id → 破坏者状态缓存（存魔/档位）；持久化读写见 <see cref="PersistBreakerState"/>。</summary>
        public Dictionary<int, BreakerState> m_breakerStates = [];

        /// <summary>玩家 Id → 上一帧是否手持破坏者（用于检测掏出瞬间）。</summary>
        public Dictionary<int, bool> m_wasHoldingBreaker = [];

        /// <summary>玩家 Id → 掏出破坏者后提示是否已发（避免手持期间重复刷）。</summary>
        public Dictionary<int, bool> m_breakerHintShown = [];

        /// <summary>玩家 Id → 砍伐链限速队列。</summary>
        public Dictionary<int, FellingQueue> m_fellingQueues = [];

        /// <summary>玩家 Id → 破坏者扩围预览（当前帧挖掘中心 + 将扩挖的格）。</summary>
        public Dictionary<int, BreakerPreview> m_breakerPreviews = [];

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemAudio m_subsystemAudio;

        public PrimitivesRenderer3D m_primitivesRenderer3D = new();

        public Game.Random m_random = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public int[] DrawOrders => [203];

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
        /// 泰拉刃「使用」剑气：OnUse 路径（参考火柴——手持工具右键点击即触发，
        /// 无条件、不要求准星命中任何目标），50% 概率发射一枚剑气。
        /// 准星指向生物时原版的挥击攻击照常结算（本方法在 OnUse 派发链上，
        /// 不拦截也不吞击），剑气是独立追加的一发。
        /// </summary>
        public bool TryFireBladeQi(ComponentMiner miner, Ray3 ray) {
            if (!m_random.Bool()) {
                return false;
            }
            Vector3 start = ray.Position;
            Vector3 direction = ray.Direction;
            if (direction.LengthSquared() < 1e-6f) {
                direction = new Vector3(0f, 0f, 1f);
            }
            else {
                direction = Vector3.Normalize(direction);
            }
            // 剑气从玩家前方 2 格出发，避免贴着位置自穿
            Vector3 origin = start + direction * 2f;
            m_subsystemProjectiles.FireProjectile<TerraSwordQiProjectile>(
                Terrain.MakeBlockValue(m_terraBladeIndex),
                origin,
                direction * TerraSwordQiProjectile.Speed,
                Vector3.Zero,
                miner.ComponentCreature
            );
            return true;
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

        // ===== 泰拉砍伐者：连锁伐木（限速） =====

        /// <summary>
        /// 挖掘钩子：泰拉砍伐者破坏原木/树叶时入队连锁伐木。潜行禁用。
        /// 实际破格在 <see cref="Update"/> 中按「0.5s 最多 8 格」限速消化。
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
            QueueFelling(miner, player, new Point3(face.X, face.Y, face.Z), cellContents, 1);
        }

        /// <summary>
        /// 连锁伐木入队：从破坏点广度优先扩散同类型木/叶（半径 3，上限 63³），
        /// 非主格逐格入队；主格耐久 1（原版计），其余原木 1/块、树叶 1/3 块。
        /// 破格由 Update 限速消化。
        /// </summary>
        public void QueueFelling(ComponentMiner miner, ComponentPlayer player, Point3 start, int type, int mainDurability) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            int key = player?.Entity?.Id ?? 0;
            FellingQueue queue;
            if (!m_fellingQueues.TryGetValue(key, out queue)) {
                queue = new FellingQueue();
                m_fellingQueues[key] = queue;
            }
            List<Point3> open = [start];
            HashSet<Point3> visited = [start];
            while (open.Count > 0 && visited.Count < 63 * 63 * 63) {
                Point3 p = open[^1];
                open.RemoveAt(open.Count - 1);
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
                                open.Add(n);
                            }
                        }
                    }
                }
            }
            int woodCount = 0;
            int leafCount = 0;
            foreach (Point3 p in visited) {
                if (p == start) {
                    continue;
                }
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
                queue.m_queue.Enqueue((p, type));
            }
            int extraDurability = Math.Max(0, woodCount + (leafCount + 2) / 3 + mainDurability - 1);
            if (extraDurability > 0) {
                miner.DamageActiveTool(extraDurability);
            }
        }

        /// <summary>
        /// 砍伐链限速消化：每玩家 0.5s 窗口最多破 8 格；破前复核目标格仍是
        /// 入队时的类型（防止期间已被别的挖掘破掉）。
        /// </summary>
        public void DigestFellingQueues(float dt) {
            double time = m_subsystemGameInfo.TotalElapsedGameTime;
            Terrain terrain = m_subsystemTerrain.Terrain;
            foreach (KeyValuePair<int, FellingQueue> pair in m_fellingQueues) {
                FellingQueue queue = pair.Value;
                if (queue.m_queue.Count == 0) {
                    continue;
                }
                if (queue.m_windowStartTime < 0 || time - queue.m_windowStartTime >= FellingWindow) {
                    queue.m_windowStartTime = time;
                    queue.m_windowRemaining = FellingBudget;
                }
                while (queue.m_windowRemaining > 0 && queue.m_queue.Count > 0) {
                    (Point3 p, int type) = queue.m_queue.Dequeue();
                    if (terrain.GetCellContents(p.X, p.Y, p.Z) == type) {
                        m_subsystemTerrain.DestroyCell(0, p.X, p.Y, p.Z, 0, false, false);
                    }
                    queue.m_windowRemaining--;
                }
            }
        }

        // ===== 泰拉破坏者：等级挖掘 =====

        /// <summary>
        /// 取玩家当前破坏者状态：优先从活动槽工具 data 读（工具存魔随存档保存），
        /// 无工具（或存魔在 200mn 以下）时退回玩家级缓存（充能掉落物仍可用）。
        /// 退出世界/读档时状态来自工具 data，不会重置。
        /// </summary>
        public BreakerState GetBreakerState(ComponentMiner miner) {
            int key = miner.Entity?.Id ?? 0;
            if (!m_breakerStates.TryGetValue(key, out BreakerState state)) {
                state = new BreakerState();
                m_breakerStates[key] = state;
            }
            int toolValue = miner.ActiveBlockValue;
            if (Terrain.ExtractContents(toolValue) != m_terraBreakerIndex) {
                return state;
            }
            int data = Terrain.ExtractData(toolValue);
            state.Mana = data >> 5;
            state.ActiveLevel = data & 31;
            return state;
        }

        public int GetBreakerRange(int level) {
            return level switch {
                1 => 1, // C: 水平 3×3
                2 => 1, // B: 水平 3×3
                3 => 2, // A: 水平 5×5
                4 => 3, // S: 水平 7×7
                _ => 4 // SS: 水平 9×9（D 档范围挖掘由调用方拦截，不会到这里）
            };
        }

        /// <summary>
        /// 把破坏者状态（存魔/档位）写回活动槽工具 data（data = 档位低 5 位 | 存魔<<5，
        /// 存魔上限 1048575mn）。读档后、玩家下线（工具进包）后状态不丢。
        /// </summary>
        public void PersistBreakerState(ComponentMiner miner, BreakerState state) {
            int value = miner.ActiveBlockValue;
            if (Terrain.ExtractContents(value) != m_terraBreakerIndex) {
                return;
            }
            int data = (state.ActiveLevel & 31) | (Math.Max(0, state.Mana) << 5);
            int newValue = Terrain.ReplaceData(value, data);
            if (newValue == value) {
                return;
            }
            IInventory inventory = miner.Inventory;
            if (inventory == null) {
                return;
            }
            int slot = inventory.ActiveSlotIndex;
            int count = inventory.GetSlotCount(slot);
            inventory.RemoveSlotItems(slot, count);
            if (inventory.GetSlotCount(slot) == 0) {
                inventory.AddSlotItems(slot, newValue, count);
            }
        }

        /// <summary>
        /// 破坏者范围挖掘：主格被挖后，仅当玩家处于蹲伏（潜行）状态才展开水平同层
        /// 扩围（按档位 1/1/2/3/4 半边长，每格 -7mn，同类才挖，挖不动即停），
        /// 挖中后存魔跌破档位门槛自动降档；被扩挖的格并入预览边框（白色）。
        /// D 档（未激活）或魔力不足时不挖。挖掘中心格预览（绿色）由 <see cref="UpdateBreakerPreview"/> 负责。
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
            ComponentPlayer player = miner.ComponentPlayer;
            Point3 center = new(digValue.CellFace.X, digValue.CellFace.Y, digValue.CellFace.Z);
            int targetContents = Terrain.ExtractContents(cellValue);
            if (targetContents == 0) {
                return;
            }
            BreakerState state = GetBreakerState(miner);
            if (state.ActiveLevel <= 0) {
                return;
            }
            ComponentBody body = player?.Entity?.FindComponent<ComponentBody>();
            if (body == null || !body.IsCrouching) {
                return; // 非蹲下不扩展破坏范围（挖掘照常，仅中心绿框）
            }
            int range = GetBreakerRange(state.ActiveLevel);
            float costPerBlock = PhytoConfig.Instance.TerraBreakerPerBlockCost;
            SubsystemTerraSetBehavior setBehavior = Project.FindSubsystem<SubsystemTerraSetBehavior>(false);
            Terrain terrain = m_subsystemTerrain.Terrain;
            int key = player.Entity?.Id ?? 0;
            if (!m_breakerPreviews.TryGetValue(key, out BreakerPreview preview)) {
                preview = new BreakerPreview();
                m_breakerPreviews[key] = preview;
            }
            preview.Center = center;
            preview.Affected.Clear();
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
                        break; // 魔力见底立即停
                    }
                    state.Mana -= (int)costPerBlock;
                    // 存魔跌破当前档位门槛时自动降档（D 档无门槛）
                    while (state.ActiveLevel > 0
                        && setBehavior != null
                        && state.Mana < setBehavior.RequiredManaForLevel(state.ActiveLevel)) {
                        state.ActiveLevel--;
                    }
                    durabilityReduction++;
                    playerDataDugAdd++;
                    preview.Affected.Add(new Point3(x, center.Y, z));
                    m_subsystemTerrain.DestroyCell(0, x, center.Y, z, 0, false, false);
                }
            }
            PersistBreakerState(miner, state);
        }

        /// <summary>
        /// 瞄准预览（挖掘前）：玩家手持激活的破坏者、非蹲下时，
        /// 当前瞄准的挖掘中心格画绿色边框；处于潜行（蹲伏）状态且魔力足够时，
        /// 同时把将被扩挖的同类格画白色边框。供渲染层每帧调用。
        /// </summary>
        public void UpdateBreakerPreview(ComponentMiner miner, Point3 center, int targetContents) {
            int key = miner.Entity?.Id ?? 0;
            if (!m_breakerPreviews.TryGetValue(key, out BreakerPreview preview)) {
                preview = new BreakerPreview();
                m_breakerPreviews[key] = preview;
            }
            preview.Center = center;
            preview.Affected.Clear();
            preview.Valid = false;
            if (targetContents == 0) {
                return;
            }
            BreakerState state = GetBreakerState(miner);
            if (state.ActiveLevel <= 0) {
                return;
            }
            preview.Valid = true; // 中心格绿框常显
            ComponentPlayer player = miner.ComponentPlayer;
            ComponentBody body = player?.Entity?.FindComponent<ComponentBody>();
            if (body == null || !body.IsCrouching) {
                return; // 非蹲下：只有中心绿框，不扩围
            }
            if (state.Mana < (int)PhytoConfig.Instance.TerraBreakerPerBlockCost) {
                return;
            }
            int range = GetBreakerRange(state.ActiveLevel);
            Terrain terrain = m_subsystemTerrain.Terrain;
            for (int dx = -range; dx <= range; dx++) {
                for (int dz = -range; dz <= range; dz++) {
                    if (dx == 0 && dz == 0) {
                        continue;
                    }
                    Point3 p = new(center.X + dx, center.Y, center.Z + dz);
                    if (terrain.GetCellContents(p.X, p.Y, p.Z) == targetContents) {
                        preview.Affected.Add(p);
                    }
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
            DigestFellingQueues(dt);
            UpdateBreakerPreviews();
        }

        /// <summary>
        /// 挖掘预览：每帧对每个手持激活破坏者的玩家做瞄准射线（Digging 模式），
        /// 把当前瞄准中心格（绿框）与蹲伏状态下将扩挖的格（白框）刷新到
        /// <see cref="m_breakerPreviews"/>，渲染层据此画边框。挖掘完成后预览自然清空。
        /// </summary>
        public void UpdateBreakerPreviews() {
            foreach (ComponentPlayer player in m_subsystemPlayers.ComponentPlayers) {
                if (player.Entity == null) {
                    continue;
                }
                ComponentMiner miner = player.ComponentMiner;
                if (miner == null) {
                    continue;
                }
                if (Terrain.ExtractContents(miner.ActiveBlockValue) != m_terraBreakerIndex) {
                    if (m_breakerPreviews.TryGetValue(player.Entity.Id, out BreakerPreview stale)) {
                        stale.Valid = false;
                    }
                    continue;
                }
                BreakerState state = GetBreakerState(miner);
                if (state.ActiveLevel <= 0) {
                    if (m_breakerPreviews.TryGetValue(player.Entity.Id, out BreakerPreview stale2)) {
                        stale2.Valid = false;
                    }
                    continue;
                }
                Camera camera = player.GameWidget.ActiveCamera;
                if (camera == null) {
                    continue;
                }
                Ray3 ray = new(camera.ViewPosition, camera.ViewDirection);
                TerrainRaycastResult? hit = miner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging);
                if (!hit.HasValue) {
                    if (m_breakerPreviews.TryGetValue(player.Entity.Id, out BreakerPreview stale3)) {
                        stale3.Valid = false;
                    }
                    continue;
                }
                int targetContents = Terrain.ExtractContents(hit.Value.Value);
                Point3 center = hit.Value.CellFace.Point;
                UpdateBreakerPreview(miner, center, targetContents);
            }
        }

        /// <summary>
        /// 掏出提示：玩家手持泰拉破坏者、且处于潜行（蹲下）状态时，
        /// 首次掏出瞬间显示当前档位与存魔（同档不重复刷；读档后从工具 data 恢复）。
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
        /// 数据经 <see cref="PersistBreakerState"/> 写回玩家当前工具。
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
                        state.Mana += (int)taken;
                        ComponentMiner ownerMiner = ownerEntity.FindComponent<ComponentMiner>();
                        if (ownerMiner != null) {
                            PersistBreakerState(ownerMiner, state);
                        }
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

        // ===== 预览边框渲染 =====

        public void Draw(Camera camera, int drawOrder) {
            FlatBatch3D batch = null;
            foreach (ComponentPlayer player in m_subsystemPlayers.ComponentPlayers) {
                if (camera.GameWidget.PlayerData != player.PlayerData || player.PlayerData == null) {
                    continue;
                }
                if (!m_breakerPreviews.TryGetValue(player.Entity?.Id ?? 0, out BreakerPreview preview)) {
                    continue;
                }
                if (!preview.Valid) {
                    continue;
                }
                if (batch == null) {
                    batch = m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None);
                }
                // 挖掘中心点绿色边框
                DrawPreviewBox(batch, preview.Center, new Color(80, 220, 80));
                // 将扩挖的方块白色边框（仅蹲伏扩围时非空）
                foreach (Point3 p in preview.Affected) {
                    DrawPreviewBox(batch, p, Color.White);
                }
            }
            if (batch != null) {
                batch.Flush(camera.ViewProjectionMatrix);
            }
        }

        static void DrawPreviewBox(FlatBatch3D batch, Point3 point, Color color) {
            Vector3 min = new(point.X, point.Y, point.Z);
            batch.QueueBoundingBox(new BoundingBox(min, min + Vector3.One), color);
        }

        /// <summary>
        /// 破坏者档位显示名（与掏出提示一致）：0=D（未激活），1=C，2=B，3=A，4=S，5=SS。
        /// </summary>
        public static string LevelName(int level) {
            return level switch {
                1 => "C",
                2 => "B",
                3 => "A",
                4 => "S",
                5 => "SS",
                _ => "D"
            };
        }
    }

    /// <summary>
    /// 泰拉工具引擎钩子调度器：ModLoader 虚方法转发到当前世界的 <see cref="SubsystemTerraToolBehavior"/>。
    /// 由 <see cref="PhytomanaMod"/> 在 __ModInitialize 注册。
    /// </summary>
    public class TerraToolHooks : ModLoader {
        public static SubsystemTerraToolBehavior m_terraToolBehavior;

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
