using System.Globalization;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentMiner : Component, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemMovingBlocks m_subsystemMovingBlocks;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTime m_subsystemTime;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemSoundMaterials m_subsystemSoundMaterials;

        public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

        ComponentHealth m_componentHealth;

        public Random m_random = new();

        public static Random s_random = new();

        public double m_digStartTime;

        public float m_digProgress;

        public double m_lastHitTime;

        // Attack pending 存原目标：Hit 入口存（pending 模式），ExecuteHit(Pending) 消费；ExecuteHit(Reraycast) 不用（重射线）。
        ComponentBody m_attackPendingBody;
        Vector3 m_attackPendingHitPoint;
        Vector3 m_attackPendingHitDir;
        // Place pending：存按下时的目标（世界命中）。块值/槽位不在按下时存——ExecutePlace 用当时实际选中槽的方块（背包可能在 pending 期被改）。
        TerrainRaycastResult m_placePendingRaycast;
        // Use pending：存按下时的 Ray3。Use 入口存，ExecuteUse 消费。
        Ray3 m_usePendingRay;
        // Interact pending：两重载结果类型不同，用 isMoving 区分。Interact 入口存，ExecuteInteract 消费。
        TerrainRaycastResult m_interactPendingTerrain;
        MovingBlocksRaycastResult m_interactPendingMoving;
        bool m_interactPendingIsMoving;
        // Aim pending：投掷物松手时存的 ray（Pending 模式 ExecuteAim 消费）。仅投掷物（弓/弩/火枪立即抛不存）。
        Ray3? m_aimPendingRay;

        public static string fName = "ComponentMiner";

        public int m_lastDigFrameIndex;

        public float m_lastPokingPhase;

        double m_lastToolHintTime;

        /// <summary>
        ///     伤害间隔(原版为0.66f)
        /// </summary>
        public virtual double HitInterval {
            get => m_basicHitInterval / ComponentFactors.GetOtherFactorResult("AttackSpeed");
            [Obsolete("Do not set the added hit interval, set m_basicHitInterval instead.")]
            set => m_basicHitInterval = value;
        }

        public double m_basicHitInterval;

        public ComponentCreature ComponentCreature { get; set; }

        public ComponentPlayer ComponentPlayer { get; set; }

        public ComponentFactors ComponentFactors { get; set; }

        public IInventory Inventory { get; set; }

        public int ActiveBlockValue {
            get {
                if (Inventory == null) {
                    return 0;
                }
                return Inventory.GetSlotValue(Inventory.ActiveSlotIndex);
            }
        }

        public float AttackPower { get; set; }

        public float AutoInteractRate { get; set; }

        public float StrengthFactor => ComponentFactors?.StrengthFactor ?? 1;

        /// <summary>
        ///     挖掘速度是否受玩家力量属性加成
        /// </summary>
        public bool m_digSpeedBasedOnStrengthFactor = true;

        public float DigSpeedFactor {
            get {
                float ans = 1f;
                if (m_digSpeedBasedOnStrengthFactor) {
                    ans *= StrengthFactor;
                }
                if (ComponentFactors?.OtherFactorsResults.TryGetValue("DigSpeed", out float result) ?? false) {
                    ans *= result;
                }
                return ans;
            }
        }

        public float PokingPhase { get; set; }

        /// <summary>pending 执行目标来源（由 ExecuteHit/Place/Use/Interact 调用方按触发时机选择）。</summary>
        public enum TargetMode {
            /// <summary>用动作入口存下的原目标（按下时命中体/射线/地形目标）。</summary>
            Pending,
            /// <summary>触发时刻从眼位重新射线取当前目标（目标移动后真实落空/命中）。</summary>
            Reraycast
        }

        /// <summary>动作 pending 标志（全动作 Pending 支持）。当前 Attack/Place/Use/Interact 接入；Dig/Poke 预留扩展。</summary>
        [Flags]
        public enum PendingAction {
            None = 0,
            Attack = 1,
            Place = 4,
            Use = 8,
            Interact = 16,
            Aim = 32
        }

        // 配置：哪些动作走 pending（延迟到事件/回调触发，不立即执行）。吞并原 UseAnimationDrivenAttack
        // （其语义 = Attack requires pending）。由需延迟执行的使用方 OnControllerCreated 用 SetRequiresPending 设。
        PendingAction m_requiresPending = PendingAction.None;

        // 运行时状态：当前 pending 中的动作。入口排他锁读此（AnyIsPending）；ExecuteHit/controller 兜底清。
        PendingAction m_isPending = PendingAction.None;

        // --- requires（配置）：哪些动作走 pending（延迟到事件/回调触发，不立即执行）。由使用方设，miner 不自改。---
        /// <summary>查询某动作是否配置为走 pending（延迟执行，不立即生效）。</summary>
        public bool RequiresPending(PendingAction action) => (m_requiresPending & action) != PendingAction.None;
        /// <summary>标记某动作走 pending（延迟执行）。幂等：重复设同一 flag 无副作用。</summary>
        public void SetRequiresPending(PendingAction action) => m_requiresPending |= action;
        /// <summary>取消某动作的 pending 配置（恢复原版立即执行）。</summary>
        public void ClearRequiresPending(PendingAction action) => m_requiresPending &= ~action;

        // --- is（运行时状态）：当前 pending 中的动作。入口排他锁读；触发方消费后清，使用方兜底清。---
        /// <summary>查询某动作当前是否处于 pending（已请求待执行）。</summary>
        public bool IsPending(PendingAction action) => (m_isPending & action) != PendingAction.None;
        /// <summary>是否有任意动作处于 pending（入口排他锁判据：pending 期丢弃其他动作请求）。</summary>
        public bool AnyIsPending => m_isPending != PendingAction.None;
        /// <summary>标记某动作 pending（仅 miner 动作入口自设，外部勿调）。</summary>
        void SetIsPending(PendingAction action) => m_isPending |= action;
        /// <summary>清除某动作的 pending 标志（触发方消费后清，或使用方兜底防 pending 卡死）。</summary>
        public void ClearIsPending(PendingAction action) => m_isPending &= ~action;
        /// <summary>清除全部 pending 标志（兜底：触发链异常中断时复位，防入口排他锁永久卡死后续动作）。</summary>
        public void ClearAllIsPending() => m_isPending = PendingAction.None;

        public CellFace? DigCellFace { get; set; }

        public float DigTime {
            get {
                if (!DigCellFace.HasValue) {
                    return 0f;
                }
                return (float)(m_subsystemTime.GameTime - m_digStartTime);
            }
        }

        public float DigProgress {
            get {
                if (!DigCellFace.HasValue) {
                    return 0f;
                }
                return m_digProgress;
            }
        }

        public bool m_canSqueezeBlock = true;

        public bool m_canJumpToPlace = false;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void Poke(bool forceRestart) {
            if (AnyIsPending) {
                return;
            }
            PokingPhase = forceRestart ? 0.0001f : MathUtils.Max(0.0001f, PokingPhase);
        }

        public bool Dig(TerrainRaycastResult raycastResult) {
            if (AnyIsPending) {
                return false;
            }
            bool result = false;
            m_lastDigFrameIndex = Time.FrameIndex;
            CellFace cellFace = raycastResult.CellFace;
            int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
            int cellContents = Terrain.ExtractContents(cellValue);
            Block cellBlock = BlocksManager.Blocks[cellContents];
            int activeBlockValue = ActiveBlockValue;
            int activeBlockContents = Terrain.ExtractContents(activeBlockValue);
            Block activeBlock = BlocksManager.Blocks[activeBlockContents];
            if (!DigCellFace.HasValue
                || DigCellFace.Value.X != cellFace.X
                || DigCellFace.Value.Y != cellFace.Y
                || DigCellFace.Value.Z != cellFace.Z) {
                m_digStartTime = m_subsystemTime.GameTime;
                DigCellFace = cellFace;
            }
            float digTimeWithActiveTool = CalculateDigTime(cellValue, activeBlockValue);
            m_digProgress = digTimeWithActiveTool > 0f
                ? MathUtils.Saturate((float)(m_subsystemTime.GameTime - m_digStartTime) / digTimeWithActiveTool)
                : 1f;
            if (!IsLevelSufficientForTool(activeBlockValue)) {
                m_digProgress = 0f;
                if (m_subsystemTime.PeriodicGameTimeEvent(5.0, m_digStartTime + 1.0)) {
                    ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                        string.Format(
                            LanguageControl.Get(fName, 1),
                            activeBlock.GetPlayerLevelRequired(activeBlockValue),
                            activeBlock.GetDisplayName(m_subsystemTerrain, activeBlockValue)
                        ),
                        Color.White,
                        true,
                        true
                    );
                }
            }
            bool flag2 = ComponentPlayer != null
                && !ComponentPlayer.ComponentInput.IsControlledByTouch
                && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
            ModsManager.HookAction(
                "OnMinerDig",
                modLoader => {
                    modLoader.OnMinerDig(this, raycastResult, ref m_digProgress, out bool flag3);
                    flag2 |= flag3;
                    return false;
                }
            );
            if ((m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Survival || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
                && ComponentPlayer != null
                && digTimeWithActiveTool >= 3f
                && m_digProgress > 0.5f
                && (m_lastToolHintTime == 0.0 || Time.FrameStartTime - m_lastToolHintTime > 300.0)) {
                bool flag = digTimeWithActiveTool == CalculateDigTime(cellValue, 0); //flag:该物品挖掘时间和空手相同
                int bestInventoryToolValue = FindBestInventoryToolForDigging(cellValue);
                if (bestInventoryToolValue == 0) {
                    if (activeBlockContents != 23 && flag) {
                        ComponentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, "11"), Color.White, true, true);
                        m_lastToolHintTime = Time.FrameStartTime;
                    }
                }
                else if (CalculateDigTime(cellValue, bestInventoryToolValue) < 0.5f * digTimeWithActiveTool || flag) {
                    string displayName = BlocksManager.Blocks[Terrain.ExtractContents(bestInventoryToolValue)]
                        .GetDisplayName(m_subsystemTerrain, bestInventoryToolValue);
                    ComponentPlayer.ComponentGui.DisplaySmallMessage(
                        string.Format(LanguageControl.Get(fName, "12"), displayName),
                        Color.White,
                        true,
                        true
                    );
                    m_lastToolHintTime = Time.FrameStartTime;
                }
            }
            if (flag2 || (m_lastPokingPhase <= 0.5f && PokingPhase > 0.5f)) {
                if (m_digProgress >= 1f) {
                    DigCellFace = null;
                    if (flag2) {
                        Poke(true);
                    }
                    BlockPlacementData digValue = cellBlock.GetDigValue(m_subsystemTerrain, this, cellValue, activeBlockValue, raycastResult);
                    m_subsystemTerrain.DestroyCell(
                        activeBlock.GetToolLevel(activeBlockValue),
                        digValue.CellFace.X,
                        digValue.CellFace.Y,
                        digValue.CellFace.Z,
                        digValue.Value,
                        false,
                        false
                    );
                    int durabilityReduction = 1;
                    int playerDataAdd = 1;
                    bool mute_ = false;
                    ModsManager.HookAction(
                        "OnBlockDug",
                        modLoader => {
                            bool mute = false;
                            modLoader.OnBlockDug(this, digValue, cellValue, ref durabilityReduction, ref mute, ref playerDataAdd);
                            mute_ |= mute;
                            return false;
                        }
                    );
                    if (!mute_) {
                        m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f);
                    }
                    DamageActiveTool(durabilityReduction);
                    if (ComponentCreature.PlayerStats != null) {
                        ComponentCreature.PlayerStats.BlocksDug += playerDataAdd;
                    }
                    result = true;
                }
                else {
                    m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 1f);
                    BlockDebrisParticleSystem particleSystem = cellBlock.CreateDebrisParticleSystem(
                        m_subsystemTerrain,
                        raycastResult.HitPoint(0.1f),
                        cellValue,
                        0.35f
                    );
                    Project.FindSubsystem<SubsystemParticles>(true).AddParticleSystem(particleSystem);
                }
            }
            return result;
        }

        public bool Place(TerrainRaycastResult raycastResult) {
            if (AnyIsPending) {
                return false;
            }
            if (RequiresPending(PendingAction.Place)) {
                // pending 模式：存按下时的目标，延迟到使用方在触发时机调 ExecutePlace。
                // 块值/槽位不存——ExecutePlace 用当时实际选中槽的方块（背包可能在 pending 期被改）。
                m_placePendingRaycast = raycastResult;
                SetIsPending(PendingAction.Place);
                return false;
            }
            if (DoPlace(raycastResult, ActiveBlockValue)) {
                if (Inventory != null) {
                    Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, 1);
                }
                return true;
            }
            return false;
        }

        public bool Place(TerrainRaycastResult raycastResult, int value) {
            if (AnyIsPending) {
                return false;
            }
            return DoPlace(raycastResult, value);
        }

        /// <summary>放置主体（原双参 Place 体）。由 Place（原版立即）或 ExecutePlace（pending）调用。</summary>
        bool DoPlace(TerrainRaycastResult raycastResult, int value) {
            int num = Terrain.ExtractContents(value);
            if (BlocksManager.Blocks[num].IsPlaceable_(value)) {
                Block block = BlocksManager.Blocks[num];
                BlockPlacementData placementData = block.GetPlacementValue(m_subsystemTerrain, this, value, raycastResult);
                if (placementData.Value != 0) {
                    Point3 point = CellFace.FaceToPoint3(placementData.CellFace.Face);
                    int num2 = placementData.CellFace.X + point.X;
                    int num3 = placementData.CellFace.Y + point.Y;
                    int num4 = placementData.CellFace.Z + point.Z;
                    bool placed = false;
                    bool placementNotAllowed_ = false;
                    ModsManager.HookAction(
                        "BeforeMinerPlace",
                        loader => {
                            // ReSharper disable AccessToModifiedClosure
                            loader.BeforeMinerPlace(
                                this,
                                raycastResult,
                                num2,
                                num3,
                                num4,
                                placementData,
                                out bool placementNotAllowed
                            );
                            // ReSharper restore AccessToModifiedClosure
                            placementNotAllowed_ |= placementNotAllowed;
                            return false;
                        }
                    );
                    if (placementNotAllowed_) {
                        return false;
                    }
                    ModsManager.HookAction(
                        "OnMinerPlace",
                        modLoader => {
#pragma warning disable CS0618 // 类型或成员已过时
                            modLoader.OnMinerPlace(
                                this,
                                raycastResult,
                                num2,
                                num3,
                                num4,
                                value,
                                out bool Placed
                            );
#pragma warning restore CS0618 // 类型或成员已过时
                            placed |= Placed;
                            return false;
                        }
                    );
                    ModsManager.HookAction(
                        "OnMinerPlace",
                        modLoader => {
                            modLoader.OnMinerPlace(
                                this,
                                raycastResult,
                                num2,
                                num3,
                                num4,
                                value,
                                placementData,
                                out bool Placed
                            );
                            placed |= Placed;
                            return false;
                        }
                    );
                    if (placed) {
                        return true;
                    }
                    if (!m_canSqueezeBlock) {
                        if (m_subsystemTerrain.Terrain.GetCellContents(num2, num3, num4) != 0) {
                            return false;
                        }
                    }
                    if (num3 > 0
                        && num3 < TerrainChunk.HeightMinusOne
                        && (m_canJumpToPlace
                            || IsBlockPlacingAllowed(ComponentCreature.ComponentBody)
                            || m_subsystemGameInfo.WorldSettings.GameMode <= GameMode.Survival)) {
                        bool flag = false;
                        if (block.GetIsCollidable(ComponentCreature.ComponentBody, value)) {
                            BoundingBox boundingBox = ComponentCreature.ComponentBody.BoundingBox;
                            boundingBox.Min += new Vector3(0.2f);
                            boundingBox.Max -= new Vector3(0.2f);
                            BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, placementData.Value);
                            for (int i = 0; i < customCollisionBoxes.Length; i++) {
                                BoundingBox box = customCollisionBoxes[i];
                                box.Min += new Vector3(num2, num3, num4);
                                box.Max += new Vector3(num2, num3, num4);
                                if (boundingBox.Intersection(box)) {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag) {
                            SubsystemBlockBehavior[] blockBehaviors =
                                m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(placementData.Value));
                            for (int i = 0; i < blockBehaviors.Length; i++) {
                                blockBehaviors[i].OnItemPlaced(num2, num3, num4, ref placementData, value);
                            }
                            m_subsystemTerrain.DestroyCell(
                                0,
                                num2,
                                num3,
                                num4,
                                placementData.Value,
                                false,
                                false
                            );
                            m_subsystemAudio.PlaySound(
                                "Audio/BlockPlaced",
                                1f,
                                0f,
                                new Vector3(placementData.CellFace.X, placementData.CellFace.Y, placementData.CellFace.Z),
                                5f,
                                false
                            );
                            Poke(false);
                            if (ComponentCreature.PlayerStats != null) {
                                ComponentCreature.PlayerStats.BlocksPlaced++;
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool Use(Ray3 ray) {
            if (AnyIsPending) {
                return false;
            }
            int num = Terrain.ExtractContents(ActiveBlockValue);
            Block block = BlocksManager.Blocks[num];
            if (!IsLevelSufficientForTool(ActiveBlockValue)) {
                ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                    string.Format(
                        LanguageControl.Get(fName, 1),
                        block.GetPlayerLevelRequired(ActiveBlockValue),
                        block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)
                    ),
                    Color.White,
                    true,
                    true
                );
                Poke(false);
                return false;
            }
            if (RequiresPending(PendingAction.Use)) {
                // pending 模式：存按下时的射线，延迟到使用方在触发时机调 ExecuteUse。
                m_usePendingRay = ray;
                SetIsPending(PendingAction.Use);
                return false;
            }
            return DoUse(ray);
        }

        /// <summary>Use 主体（原 Use 行为循环）。由 Use（原版立即）或 ExecuteUse（pending）调用。</summary>
        bool DoUse(Ray3 ray) {
            SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(ActiveBlockValue));
            for (int i = 0; i < blockBehaviors.Length; i++) {
                if (blockBehaviors[i].OnUse(ray, this)) {
                    Poke(false);
                    return true;
                }
            }
            return false;
        }

        public bool Interact(TerrainRaycastResult raycastResult) {
            if (AnyIsPending) {
                return false;
            }
            if (RequiresPending(PendingAction.Interact)) {
                // pending 模式：存按下时的地形目标，延迟到使用方在触发时机调 ExecuteInteract。
                m_interactPendingTerrain = raycastResult;
                m_interactPendingIsMoving = false;
                SetIsPending(PendingAction.Interact);
                return false;
            }
            return DoInteractTerrain(raycastResult);
        }

        /// <summary>Interact(Terrain) 主体。由 Interact（原版立即）或 ExecuteInteract（pending）调用。</summary>
        bool DoInteractTerrain(TerrainRaycastResult raycastResult) {
            SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(raycastResult.Value));
            for (int i = 0; i < blockBehaviors.Length; i++) {
                if (blockBehaviors[i].OnInteract(raycastResult, this)) {
                    if (ComponentCreature.PlayerStats != null) {
                        ComponentCreature.PlayerStats.BlocksInteracted++;
                    }
                    Poke(false);
                    return true;
                }
            }
            return false;
        }

        public bool Interact(MovingBlocksRaycastResult raycastResult) {
            if (AnyIsPending) {
                return false;
            }
            if (raycastResult.MovingBlock == null) {
                return false;
            }
            if (RequiresPending(PendingAction.Interact)) {
                // pending 模式：存按下时的移动方块目标，延迟到使用方在触发时机调 ExecuteInteract。
                m_interactPendingMoving = raycastResult;
                m_interactPendingIsMoving = true;
                SetIsPending(PendingAction.Interact);
                return false;
            }
            return DoInteractMoving(raycastResult);
        }

        /// <summary>Interact(MovingBlocks) 主体。由 Interact（原版立即）或 ExecuteInteract（pending）调用。</summary>
        bool DoInteractMoving(MovingBlocksRaycastResult raycastResult) {
            if (raycastResult.MovingBlock == null) {
                return false;
            }
            SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(
                Terrain.ExtractContents(raycastResult.MovingBlock.Value)
            );
            for (int i = 0; i < blockBehaviors.Length; i++) {
                if (blockBehaviors[i].OnInteract(raycastResult, this)) {
                    if (ComponentCreature.PlayerStats != null) {
                        ComponentCreature.PlayerStats.BlocksInteracted++;
                    }
                    Poke(false);
                    return true;
                }
            }
            return false;
        }

        public void Hit(ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection) {
            if (AnyIsPending) {
                return;
            }
            double hitInterval = HitInterval;
            ModsManager.HookAction(
                "SetHitInterval",
                modLoader => {
                    modLoader.SetHitInterval(this, ref hitInterval);
                    return false;
                }
            );
            if (!(m_subsystemTime.GameTime - m_lastHitTime > hitInterval)) {
                return;
            }
            m_lastHitTime = m_subsystemTime.GameTime;
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(ActiveBlockValue)];
            if (!IsLevelSufficientForTool(ActiveBlockValue)) {
                ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                    string.Format(
                        LanguageControl.Get(fName, 1),
                        block.GetPlayerLevelRequired(ActiveBlockValue),
                        block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)
                    ),
                    Color.White,
                    true,
                    true
                );
                Poke(false);
                return;
            }
            if (RequiresPending(PendingAction.Attack)) {
                // pending 模式：存原目标，延迟到使用方在 impact 时机调 ExecuteHit。
                // 不立即伤害、不 Poke（pending 路径由触发方驱动伤害，chop 留给 dig）。
                m_attackPendingBody = componentBody;
                m_attackPendingHitPoint = hitPoint;
                m_attackPendingHitDir = hitDirection;
                SetIsPending(PendingAction.Attack);
                return;
            }
            // 原版立即伤害（dae/AI）
            DoMeleeHit(componentBody, hitPoint, hitDirection, pokeAfter: true);
        }

        /// <summary>
        /// 近战伤害主体（原 Hit :481-555 逻辑）。由 Hit（原版立即）或 ExecuteHit（pending 模式 impact）调用。
        /// </summary>
        /// <param name="componentBody">被攻击的命中体。</param>
        /// <param name="hitPoint">命中点（世界坐标）。</param>
        /// <param name="hitDirection">命中方向（已归一）。</param>
        /// <param name="pokeAfter">true=末尾 Poke(false)（原版立即路径，驱动 chop loop）；false=不 Poke（pending 路径）。</param>
        void DoMeleeHit(ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection, bool pokeAfter) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(ActiveBlockValue)];
            float num; //伤害
            float num2; //玩家命中率
            float num3 = 1f; //生物命中率
            if (ActiveBlockValue != 0) {
                num = block.GetMeleePower(ActiveBlockValue) * AttackPower * m_random.Float(0.8f, 1.2f);
                num2 = block.GetMeleeHitProbability(ActiveBlockValue);
            }
            else {
                num = AttackPower * m_random.Float(0.8f, 1.2f);
                num2 = 0.66f;
            }
            num2 *= componentBody.Velocity.Length() < 0.05f ? 2f : 1f;
            bool flag;
            ModsManager.HookAction(
                "OnMinerHit",
                modLoader => {
                    // ReSharper disable AccessToModifiedClosure
                    modLoader.OnMinerHit(
                        this,
                        componentBody,
                        hitPoint,
                        hitDirection,
                        ref num,
                        ref num2,
                        ref num3,
                        out bool _
                    );
                    // ReSharper restore AccessToModifiedClosure
                    return false;
                }
            );
            if (ComponentPlayer != null) {
                m_subsystemAudio.PlaySound("Audio/Swoosh", 1f, m_random.Float(-0.2f, 0.2f), componentBody.Position, 3f, false);
                flag = m_random.Bool(num2);
            }
            else {
                flag = m_random.Bool(num3);
            }
            num *= StrengthFactor;
            if (flag) {
                int durabilityReduction = 1;
                Attackment attackment = new MeleeAttackment(componentBody, Entity, hitPoint, hitDirection, num);
                ModsManager.HookAction(
                    "OnMinerHit2",
                    loader => {
                        loader.OnMinerHit2(this, componentBody, hitPoint, hitDirection, ref durabilityReduction, ref attackment);
                        return false;
                    }
                );
                AttackBody(attackment);
                DamageActiveTool(durabilityReduction);
            }
            else if (ComponentCreature is ComponentPlayer) {
                HitValueParticleSystem particleSystem = new(
                    hitPoint + 0.75f * hitDirection,
                    1f * hitDirection + ComponentCreature.ComponentBody.Velocity,
                    Color.White,
                    LanguageControl.Get(fName, 2)
                );
                ModsManager.HookAction(
                    "SetHitValueParticleSystem",
                    modLoader => {
                        modLoader.SetHitValueParticleSystem(particleSystem, null);
                        return false;
                    }
                );
                Project.FindSubsystem<SubsystemParticles>(true).AddParticleSystem(particleSystem);
            }
            if (ComponentCreature.PlayerStats != null) {
                ComponentCreature.PlayerStats.MeleeAttacks++;
                if (flag) {
                    ComponentCreature.PlayerStats.MeleeHits++;
                }
            }
            if (pokeAfter) {
                Poke(false);
            }
        }

        /// <summary>目标获取射线（ExecuteXxx 的 Reraycast 起点）。player 用相机 ViewPosition/ViewDirection（与 playerInput.Interact 同源）——
        /// 自定义玩家模型 EyeRotation 可能与相机不同步；非 player creature 无相机，回退眼位射线（与 auto-interact 同源）。
        /// 仅在 ExecuteXxx 重射线时调用（非属性，因含相机/眼位回退逻辑）。</summary>
        Ray3 GetTargetingRay() {
            ComponentPlayer player = ComponentPlayer;
            if (player != null) {
                Camera camera = player.GameWidget?.ActiveCamera;
                if (camera != null) {
                    return new Ray3(camera.ViewPosition, Vector3.Normalize(camera.ViewDirection));
                }
            }
            ComponentCreatureModel componentCreatureModel = ComponentCreature.ComponentCreatureModel;
            return new Ray3(componentCreatureModel.EyePosition, Vector3.Normalize(componentCreatureModel.EyeRotation.GetForwardVector()));
        }

        /// <summary>
        /// pending 执行时重查手持物品等级（背包可能在 pending 期被换，按下时的等级校验已失效）。
        /// 不足则提示并返回 false。ExecuteHit/Use 在 DoXxx 前调（Place/Interact 不查：vanilla 亦不查等级）。
        /// 注意：等级不足时仅提示，故意不 Poke(false)——与原版立即路径（Use/Hit 等级失败 Poke(false) 驱动 chop loop）不同。
        /// pending 路径整体不 Poke（chop 留给 dig/attack），等级失败视为完全 no-op。
        /// </summary>
        bool IsToolLevelSufficientOrFeedback() {
            if (IsLevelSufficientForTool(ActiveBlockValue)) {
                return true;
            }
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(ActiveBlockValue)];
            ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                string.Format(
                    LanguageControl.Get(fName, 1),
                    block.GetPlayerLevelRequired(ActiveBlockValue),
                    block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)
                ),
                Color.White,
                true,
                true
            );
            return false;
        }

        /// <summary>
        /// Attack pending 执行（由使用方在 impact 时机触发）。按 targetMode 解目标后跑 DoMeleeHit。
        /// </summary>
        /// <param name="mode">Pending=Hit 入口存的原目标；Reraycast=impact 时刻眼位重射线取当前命中体。</param>
        public virtual void ExecuteHit(TargetMode mode) {
            ComponentBody componentBody;
            Vector3 hitPoint;
            Vector3 hitDirection;
            if (mode == TargetMode.Reraycast) {
                // 从目标射线重射线取当前命中体（无命中 no-op=真实落空）。用 GetTargetingRay（player=相机 ViewPosition/Direction，
                // 与 playerInput 同源；非 player 回退眼位）——自定义玩家模型 EyeRotation 可能与相机不同步致系统性 miss。
                // ExecutePlace/Use/Interact 均用 GetTargetingRay，此处对齐（原误用 EyePosition/EyeRotation）。
                Ray3 targetingRay = GetTargetingRay();
                Vector3 start = targetingRay.Position;
                Vector3 direction = targetingRay.Direction;
                float reach = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative
                    ? SettingsManager.CreativeReach
                    : 5f;
                reach = Math.Min(reach, SettingsManager.VisibilityRange);
                BodyRaycastResult? bodyRaycastResult = m_subsystemBodies.Raycast(
                    start,
                    start + direction * (reach + 1f),
                    0.35f,
                    (body, distance) => Vector3.DistanceSquared(start + distance * direction, start) <= reach * reach
                        && body.Entity != Entity
                        && !body.IsChildOfBody(ComponentCreature.ComponentBody)
                        && !ComponentCreature.ComponentBody.IsChildOfBody(body)
                        && Vector3.Dot(Vector3.Normalize(body.BoundingBox.Center() - start), direction) > 0.7f
                );
                if (!bodyRaycastResult.HasValue) {
                    ClearIsPending(PendingAction.Attack);
                    return;
                }
                componentBody = bodyRaycastResult.Value.ComponentBody;
                hitPoint = bodyRaycastResult.Value.HitPoint();
                hitDirection = direction;
            }
            else {
                componentBody = m_attackPendingBody;
                hitPoint = m_attackPendingHitPoint;
                hitDirection = m_attackPendingHitDir;
            }
            ClearIsPending(PendingAction.Attack);
            // 目标失效（pending 模式存体已无效）→ no-op
            if (componentBody == null || !componentBody.Entity.IsAddedToProject) {
                return;
            }
            // 重查手持物品等级（pending 期背包可能被换）
            if (!IsToolLevelSufficientOrFeedback()) {
                return;
            }
            DoMeleeHit(componentBody, hitPoint, hitDirection, pokeAfter: false);
        }

        /// <summary>
        /// Place pending 执行（由使用方在触发时机调用）。按 mode 解目标后跑 DoPlace，成功则扣当时选中槽物品。
        /// </summary>
        /// <param name="mode">Pending=入口存的原目标；Reraycast=触发时刻眼位重射线取当前地形目标。块值/槽位均取当时实际选中槽（背包可能在 pending 期被改，不按下时存）。</param>
        public virtual bool ExecutePlace(TargetMode mode) {
            TerrainRaycastResult raycast;
            if (mode == TargetMode.Reraycast) {
                TerrainRaycastResult? r = Raycast<TerrainRaycastResult>(GetTargetingRay(), RaycastMode.Interaction, true, false, false);
                if (!r.HasValue) {
                    ClearIsPending(PendingAction.Place);
                    return false;
                }
                raycast = r.Value;
            }
            else {
                raycast = m_placePendingRaycast;
            }
            // 块值/槽位取当时实际选中槽（背包可能在 pending 期被改，不按下时存）。
            ClearIsPending(PendingAction.Place);
            if (DoPlace(raycast, ActiveBlockValue)) {
                if (Inventory != null) {
                    Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, 1);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Aim pending 执行（投掷物，由外部动画 25%/打断触发）。Pending=松手存的 ray；Reraycast=触发时刻 GetTargetingRay。
        /// 边界：DoAim 用触发时刻 ActiveBlockValue——pending 期若从投掷物切到弓/弩等 aimable 物品，会触发其 OnAim(Completed)
        /// （如射箭，方向用存的松手 ray）。设计选择（pending 期背包可变，仿 ExecutePlace）；切到非 aimable 方块则 DoAim 返 false 无副作用。
        /// </summary>
        /// <param name="mode">Pending=入口存的松手 ray（与 vanilla Completed 一致）；Reraycast=触发时刻重射线。仅投掷物 pending。</param>
        public virtual bool ExecuteAim(TargetMode mode) {
            Ray3 aim;
            if (mode == TargetMode.Reraycast) {
                aim = GetTargetingRay();
            }
            else {
                if (!m_aimPendingRay.HasValue) {
                    return false;  // 已消费（25% event + onInterrupt/兜底防双抛）
                }
                aim = m_aimPendingRay.Value;
            }
            ClearIsPending(PendingAction.Aim);
            m_aimPendingRay = null;
            return DoAim(aim, AimState.Completed);  // 调主体（绕过 Aim 拦截）
        }

        /// <summary>
        /// Use pending 执行（由使用方在触发时机调用）。按 mode 解射线后跑 DoUse。
        /// </summary>
        /// <param name="mode">Pending=入口存的射线；Reraycast=触发时刻眼位重射线（Use 不做地形射线，仅转发行为 OnUse）。</param>
        public virtual bool ExecuteUse(TargetMode mode) {
            Ray3 ray = mode == TargetMode.Reraycast ? GetTargetingRay() : m_usePendingRay;
            ClearIsPending(PendingAction.Use);
            // 重查手持物品等级（pending 期背包可能被换）
            if (!IsToolLevelSufficientOrFeedback()) {
                return false;
            }
            return DoUse(ray);
        }

        /// <summary>
        /// Interact pending 执行（由使用方在触发时机调用）。按 mode + isMoving 解目标后跑对应 DoInteract。
        /// </summary>
        /// <param name="mode">Pending=入口存的原目标；Reraycast=触发时刻眼位重射线取当前目标（按 isMoving 重射线对应类型，取不到则 no-op 清 pending）。</param>
        public virtual bool ExecuteInteract(TargetMode mode) {
            if (mode == TargetMode.Reraycast) {
                Ray3 eye = GetTargetingRay();
                if (m_interactPendingIsMoving) {
                    MovingBlocksRaycastResult? r = Raycast<MovingBlocksRaycastResult>(eye, RaycastMode.Interaction, false, false, true);
                    if (!r.HasValue) {
                        ClearIsPending(PendingAction.Interact);
                        return false;
                    }
                    ClearIsPending(PendingAction.Interact);
                    return DoInteractMoving(r.Value);
                }
                TerrainRaycastResult? rt = Raycast<TerrainRaycastResult>(eye, RaycastMode.Interaction, true, false, false);
                if (!rt.HasValue) {
                    ClearIsPending(PendingAction.Interact);
                    return false;
                }
                ClearIsPending(PendingAction.Interact);
                return DoInteractTerrain(rt.Value);
            }
            ClearIsPending(PendingAction.Interact);
            // 存的目标可能 pending 期失效（移动方块停止/销毁、地形格子被挖/替换）→ no-op，仿 ExecuteHit 的 IsAddedToProject 校验。
            if (m_interactPendingIsMoving) {
                if (MovingBlock.IsNullOrStopped(m_interactPendingMoving.MovingBlock)) {
                    return false;
                }
                return DoInteractMoving(m_interactPendingMoving);
            }
            // 地形分支：当前格子内容与按下时存的 Value 不一致（pending 期格子被改）→ no-op（对称移动分支校验）
            CellFace pendingCell = m_interactPendingTerrain.CellFace;
            if (m_subsystemTerrain.Terrain.GetCellValue(pendingCell.X, pendingCell.Y, pendingCell.Z) != m_interactPendingTerrain.Value) {
                return false;
            }
            return DoInteractTerrain(m_interactPendingTerrain);
        }

        /// <summary>
        /// 当前 Interact pending 的目标方块值（按下交互键时眼位射线命中的目标）。
        /// 供使用方在 pending 期分类目标（如区分箱子开启动画）。地形目标返 terrain value；
        /// 移动方块目标（如活塞推的箱子）返 moving block value；无 Interact pending 或目标已失效返 0。
        /// </summary>
        public int InteractPendingValue {
            get {
                if (!IsPending(PendingAction.Interact)) {
                    return 0;
                }
                if (m_interactPendingIsMoving) {
                    return m_interactPendingMoving.MovingBlock?.Value ?? 0;
                }
                return m_interactPendingTerrain.Value;
            }
        }

        public bool Aim(Ray3 aim, AimState state) {
            // pending 拦截：投掷物松手（Completed）不立即抛，存 pending 等外部动画（投掷动作播 25%/被打断）触发 ExecuteAim。
            // 仅投掷物（blockBehaviors 含 ThrowableBlockBehavior）；弓/弩/火枪无此 behavior 立即抛。InProgress/Cancelled 不拦。
            // AnyIsPending 排他（与 Place/Use/Interact/Dig 入口一致）：他 pending 占用时不创建第二个 pending，回落 DoAim 立即抛。
            if (state == AimState.Completed && !AnyIsPending && RequiresPending(PendingAction.Aim)) {
                SubsystemBlockBehavior[] pendingBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(ActiveBlockValue));
                foreach (SubsystemBlockBehavior b in pendingBehaviors) {
                    if (b is SubsystemThrowableBlockBehavior) {
                        m_aimPendingRay = aim;
                        SetIsPending(PendingAction.Aim);
                        return true;  // 存 pending 不抛
                    }
                }
            }
            return DoAim(aim, state);
        }

        /// <summary>Aim 主体（原 Aim body）：IsAimable_ + 等级 + 遍历 OnAim。由 Aim（立即/非投掷）或 ExecuteAim（pending 消费）调用。</summary>
        bool DoAim(Ray3 aim, AimState state) {
            int num = Terrain.ExtractContents(ActiveBlockValue);
            Block block = BlocksManager.Blocks[num];
            if (block.IsAimable_(ActiveBlockValue)) {
                if (!IsLevelSufficientForTool(ActiveBlockValue)) {
                    ComponentPlayer?.ComponentGui.DisplaySmallMessage(
                        string.Format(
                            LanguageControl.Get(fName, 1),
                            block.GetPlayerLevelRequired(ActiveBlockValue),
                            block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)
                        ),
                        Color.White,
                        true,
                        true
                    );
                    Poke(false);
                    return true;
                }
                SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
                for (int i = 0; i < blockBehaviors.Length; i++) {
                    if (blockBehaviors[i].OnAim(aim, this, state)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///     发出射线检测，检测玩家点击到的目标
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="mode">发出射线的意图</param>
        /// <param name="raycastTerrain">该射线是否和地形交互，为false时则忽略地形</param>
        /// <param name="raycastBodies">该射线是否和生物等实体交互，为false时则忽略实体</param>
        /// <param name="raycastMovingBlocks">该射线是否和移动方块交互，为false时则忽略移动方块</param>
        /// <param name="Reach">进行Raycast的距离</param>
        /// <returns></returns>
        public virtual object Raycast(Ray3 ray,
            RaycastMode mode,
            bool raycastTerrain = true,
            bool raycastBodies = true,
            bool raycastMovingBlocks = true,
            float? Reach = null) {
            float reach = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative ? SettingsManager.CreativeReach : 5f;
            if (Reach.HasValue) {
                reach = Reach.Value;
            }
            reach = Math.Min(reach, SettingsManager.VisibilityRange);
            Vector3 creaturePosition = ComponentCreature.ComponentCreatureModel.EyePosition;
            Vector3 start = ray.Position;
            Vector3 direction = Vector3.Normalize(ray.Direction);
            Vector3 end = ray.Position + direction * Math.Max(reach + 1f, 15f);
            Point3 startCell = Terrain.ToCell(start);
            BodyRaycastResult? bodyRaycastResult = null;
            if (raycastBodies) {
                bodyRaycastResult = m_subsystemBodies.Raycast(
                    start,
                    end,
                    0.35f,
                    (body, distance) => Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach
                        && body.Entity != Entity
                        && !body.IsChildOfBody(ComponentCreature.ComponentBody)
                        && !ComponentCreature.ComponentBody.IsChildOfBody(body)
                        && Vector3.Dot(Vector3.Normalize(body.BoundingBox.Center() - start), direction) > 0.7f
                );
            }
            MovingBlocksRaycastResult? movingBlocksRaycastResult = null;
            if (raycastMovingBlocks) {
                movingBlocksRaycastResult = m_subsystemMovingBlocks.Raycast(start, end, true);
            }
            TerrainRaycastResult? terrainRaycastResult = null;
            if (raycastTerrain) {
                terrainRaycastResult = m_subsystemTerrain.Raycast(
                    start,
                    end,
                    true,
                    true,
                    (value, distance) => {
                        if (Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach) {
                            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                            if (distance == 0f
                                && block is CrossBlock
                                && Vector3.Dot(direction, new Vector3(startCell) + new Vector3(0.5f) - start) < 0f) {
                                return false;
                            }
                            if (mode == RaycastMode.Digging) {
                                return !block.GetIsDiggingTransparent(value);
                            }
                            if (mode == RaycastMode.Interaction) {
                                if (block.IsPlacementTransparent_(value)) {
                                    return block.IsInteractive(m_subsystemTerrain, value);
                                }
                                return true;
                            }
                            if (mode == RaycastMode.Gathering) {
                                return block.IsGatherable_(value);
                            }
                        }
                        return false;
                    }
                );
            }
            float num = bodyRaycastResult?.Distance ?? float.PositiveInfinity;
            float num2 = movingBlocksRaycastResult?.Distance ?? float.PositiveInfinity;
            float num3 = terrainRaycastResult?.Distance ?? float.PositiveInfinity;
            if (bodyRaycastResult.HasValue
                && num < num2
                && num < num3) {
                return bodyRaycastResult.Value;
            }
            if (movingBlocksRaycastResult.HasValue
                && num2 < num
                && num2 < num3) {
                return movingBlocksRaycastResult.Value;
            }
            if (terrainRaycastResult.HasValue
                && num3 < num
                && num3 < num2) {
                return terrainRaycastResult.Value;
            }
            return new Ray3(start, direction);
        }

        public T? Raycast<T>(Ray3 ray,
            RaycastMode mode,
            bool raycastTerrain = true,
            bool raycastBodies = true,
            bool raycastMovingBlocks = true,
            float? reach = null) where T : struct {
            object obj = Raycast(ray, mode, raycastTerrain, raycastBodies, raycastMovingBlocks, reach);
            return obj is T obj1 ? obj1 : null;
        }

        public virtual void RemoveActiveTool(int removeCount) {
            if (Inventory != null) {
                Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, removeCount);
            }
        }

        public virtual void DamageActiveTool(int damageCount) {
            if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative
                || Inventory == null) {
                return;
            }
            int num = BlocksManager.DamageItem(ActiveBlockValue, damageCount, Entity);
            if (num != 0) {
                int slotCount = Inventory.GetSlotCount(Inventory.ActiveSlotIndex);
                Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, slotCount);
                if (Inventory.GetSlotCount(Inventory.ActiveSlotIndex) == 0) {
                    Inventory.AddSlotItems(Inventory.ActiveSlotIndex, num, slotCount);
                }
            }
            else {
                Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, 1);
            }
        }

        public static void AttackBody(Attackment attackment) {
            try {
                attackment.ProcessAttackment();
            }
            catch (Exception e) {
                Log.Error($"Attack execute error: {e}");
            }
        }

        public static void AttackBody(ComponentBody target,
            ComponentCreature attacker,
            Vector3 hitPoint,
            Vector3 hitDirection,
            float attackPower,
            bool isMeleeAttack) {
            if (isMeleeAttack) {
                AttackBody(new MeleeAttackment(target?.Entity, attacker?.Entity, hitPoint, hitDirection, attackPower));
            }
            else {
                AttackBody(new ProjectileAttackment(target?.Entity, attacker?.Entity, hitPoint, hitDirection, attackPower, null));
            }
        }

        [Obsolete("Use AddHitValueParticleSystem() in Attackment instead.", true)]
        public static void AddHitValueParticleSystem(float damage, Entity attacker, Entity attacked, Vector3 hitPoint, Vector3 hitDirection) {
            ComponentBody attackerBody = attacker?.FindComponent<ComponentBody>();
            ComponentPlayer attackerComponentPlayer = attacker?.FindComponent<ComponentPlayer>();
            ComponentHealth attackedComponentHealth = attacked?.FindComponent<ComponentHealth>();
            string text2 = (0f - damage).ToString("0", CultureInfo.InvariantCulture);
            Vector3 hitValueParticleVelocity = Vector3.Zero;
            if (attackerBody != null) {
                hitValueParticleVelocity = attackerBody.Velocity;
            }
            Color color = attackerComponentPlayer != null && damage > 0f && attackedComponentHealth != null ? Color.White : Color.Transparent;
            HitValueParticleSystem particleSystem = new(hitPoint + 0.75f * hitDirection, 1f * hitDirection + hitValueParticleVelocity, color, text2);
            ModsManager.HookAction(
                "SetHitValueParticleSystem",
                modLoader => {
                    modLoader.SetHitValueParticleSystem(particleSystem, null);
                    return false;
                }
            );
            attacked?.Project.FindSubsystem<SubsystemParticles>(true).AddParticleSystem(particleSystem);
        }

        public virtual void Update(float dt) {
            float num = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative ? 1f / SettingsManager.CreativeDigTime : 4f;
            m_lastPokingPhase = PokingPhase;
            if (DigCellFace.HasValue
                || PokingPhase > 0f) {
                PokingPhase += num * m_subsystemTime.GameTimeDelta;
                if (PokingPhase > 1f) {
                    PokingPhase = DigCellFace.HasValue ? MathUtils.Remainder(PokingPhase, 1f) : 0f;
                }
            }
            if (DigCellFace.HasValue
                && Time.FrameIndex - m_lastDigFrameIndex > 1) {
                DigCellFace = null;
            }
            if ((m_componentHealth != null && !(m_componentHealth.Health > 0f))
                || !(AutoInteractRate > 0f)
                || !m_random.Bool(AutoInteractRate)
                || !m_subsystemTime.PeriodicGameTimeEvent(1.0, GetHashCode() % 100 / 100f)) {
                return;
            }
            ComponentCreatureModel componentCreatureModel = ComponentCreature.ComponentCreatureModel;
            Vector3 eyePosition = componentCreatureModel.EyePosition;
            Vector3 forwardVector = componentCreatureModel.EyeRotation.GetForwardVector();
            for (int i = 0; i < 10; i++) {
                TerrainRaycastResult? terrainRaycastResult = Raycast<TerrainRaycastResult>(
                    new Ray3(eyePosition, forwardVector + m_random.Vector3(0.75f)),
                    RaycastMode.Interaction
                );
                if (terrainRaycastResult.HasValue
                    && terrainRaycastResult.Value.Distance < 1.5f
                    && Terrain.ExtractContents(terrainRaycastResult.Value.Value) != 57
                    && Interact(terrainRaycastResult.Value)) {
                    break;
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemSoundMaterials = Project.FindSubsystem<SubsystemSoundMaterials>(true);
            m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true);
            ComponentCreature = Entity.FindComponent<ComponentCreature>(true);
            ComponentPlayer = Entity.FindComponent<ComponentPlayer>();
            m_componentHealth = Entity.FindComponent<ComponentHealth>();
            ComponentFactors = Entity.FindComponent<ComponentFactors>(true);
            Inventory = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && ComponentPlayer != null
                ? Entity.FindComponent<ComponentCreativeInventory>()
                : Entity.FindComponent<ComponentInventory>();
            AttackPower = valuesDictionary.GetValue<float>("AttackPower");
#pragma warning disable CS0618
            HitInterval = valuesDictionary.GetValue<float>("HitInterval");
#pragma warning restore CS0618
            AutoInteractRate = valuesDictionary.GetValue<float>("AutoInteractRate");
            if (string.CompareOrdinal(m_subsystemGameInfo.WorldSettings.OriginalSerializationVersion, "2.4") < 0
                || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless
                || m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Survival) {
                AutoInteractRate = 0f;
            }
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("AttackPower", AttackPower);
        }

        public static bool IsBlockPlacingAllowed(ComponentBody componentBody) {
            if (componentBody.StandingOnBody != null
                || componentBody.StandingOnValue.HasValue) {
                return true;
            }
            if (componentBody.ImmersionFactor > 0.01f) {
                return true;
            }
            if (componentBody.ParentBody != null
                && IsBlockPlacingAllowed(componentBody.ParentBody)) {
                return true;
            }
            ComponentLocomotion componentLocomotion = componentBody.Entity.FindComponent<ComponentLocomotion>();
            if (componentLocomotion != null
                && componentLocomotion.LadderValue.HasValue) {
                return true;
            }
            return false;
        }

        public virtual float CalculateDigTime(int digValue, int toolValue) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(toolValue)];
            Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(digValue)];
            float digResilience = block2.GetDigResilience(digValue);
            BlockDigMethod digBlockMethod = block2.GetBlockDigMethod(digValue);
            float ShovelPower = block.GetShovelPower(toolValue);
            float QuarryPower = block.GetQuarryPower(toolValue);
            float HackPower = block.GetHackPower(toolValue);
            if (ComponentPlayer != null
                && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) {
                if (digResilience < float.PositiveInfinity) {
                    return 0f;
                }
                return float.PositiveInfinity;
            }
            if (ComponentPlayer != null
                && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure) {
                float num = 0f;
                if (digBlockMethod == BlockDigMethod.Shovel
                    && ShovelPower >= 2f) {
                    num = ShovelPower;
                }
                else if (digBlockMethod == BlockDigMethod.Quarry
                    && QuarryPower >= 2f) {
                    num = QuarryPower;
                }
                else if (digBlockMethod == BlockDigMethod.Hack
                    && HackPower >= 2f) {
                    num = HackPower;
                }
                num *= StrengthFactor;
                if (!(num > 0f)) {
                    return float.PositiveInfinity;
                }
                return MathUtils.Max(digResilience / num, 0f);
            }
            float num2 = 1f;
            if (digBlockMethod == BlockDigMethod.Shovel) {
                num2 = ShovelPower;
            }
            else if (digBlockMethod == BlockDigMethod.Quarry) {
                num2 = QuarryPower;
            }
            else if (digBlockMethod == BlockDigMethod.Hack) {
                num2 = HackPower;
            }
            num2 *= DigSpeedFactor;
            if (!(num2 > 0f)) {
                return float.PositiveInfinity;
            }
            return MathUtils.Max(digResilience / num2, 0f);
        }

        public virtual bool IsLevelSufficientForTool(int toolValue) {
            bool canUse = false;
            bool skip = false;
            ModsManager.HookAction(
                "IsLevelSufficientForTool",
                modLoader => {
                    modLoader.IsLevelSufficientForTool(this, toolValue, ref canUse, out skip);
                    return false;
                }
            );
            if (skip) {
                return canUse;
            }
            if (m_subsystemGameInfo.WorldSettings.GameMode != 0
                && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled) {
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(toolValue)];
                if (ComponentPlayer != null
                    && ComponentPlayer.PlayerData.Level < block.GetPlayerLevelRequired(toolValue)) {
                    return false;
                }
            }
            return true;
        }

        public virtual int FindBestInventoryToolForDigging(int digValue) {
            int result = 0;
            float num = CalculateDigTime(digValue, 0);
            foreach (IInventory item in Entity.FindComponents<IInventory>()) {
                if (item is ComponentCreativeInventory) {
                    continue;
                }
                for (int i = 0; i < item.SlotsCount; i++) {
                    int slotValue = item.GetSlotValue(i);
                    if (IsLevelSufficientForTool(slotValue)) {
                        float num2 = CalculateDigTime(digValue, slotValue);
                        if (num2 < num) {
                            num = num2;
                            result = slotValue;
                        }
                    }
                }
            }
            return result;
        }
    }
}