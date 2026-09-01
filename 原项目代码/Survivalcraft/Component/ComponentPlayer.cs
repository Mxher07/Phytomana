using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentPlayer : ComponentCreature, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTime m_subsystemTime;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemTerrain m_subsystemTerrain;

        public bool m_aimHintIssued;

        public static string fName = "ComponentPlayer";

        public double m_lastActionTime;

        public bool m_speedOrderBlocked;

        public Ray3? m_aim;

        public bool m_isAimBlocked;

        public bool m_isDigBlocked;

        public bool m_doAimBlockLook = true; //手机端在执行Aim操作的时候，允许旋转屏幕，比如RYSH的喝水、武器的格挡

        public bool m_allowAddLookOrder = true;

        public double? m_aimStartTime;

        public double AimDuration {
            get {
                if (m_aimStartTime.HasValue) {
                    return m_subsystemTime.GameTime - m_aimStartTime.Value;
                }
                return -1;
            }
        }

        public PlayerData PlayerData { get; set; }

        public GameWidget GameWidget => PlayerData.GameWidget;

        public ContainerWidget GuiWidget => PlayerData.GameWidget.GuiWidget;

        public ViewWidget ViewWidget => PlayerData.GameWidget.ViewWidget;

        public ComponentGui ComponentGui { get; set; }

        public ComponentInput ComponentInput { get; set; }

        public ComponentBlockHighlight ComponentBlockHighlight { get; set; }

        public ComponentScreenOverlays ComponentScreenOverlays { get; set; }

        public ComponentAimingSights ComponentAimingSights { get; set; }

        public ComponentMiner ComponentMiner { get; set; }

        public ComponentRider ComponentRider { get; set; }

        public ComponentSleep ComponentSleep { get; set; }

        public ComponentVitalStats ComponentVitalStats { get; set; }

        public ComponentSickness ComponentSickness { get; set; }

        public ComponentFlu ComponentFlu { get; set; }

        public ComponentLevel ComponentLevel { get; set; }

        public ComponentClothing ComponentClothing { get; set; }

        public ComponentOuterClothingModel ComponentOuterClothingModel {
            // ReSharper disable UnusedAutoPropertyAccessor.Global
            get;
            // ReSharper restore UnusedAutoPropertyAccessor.Global
            set;
        }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public DragHostWidget m_dragHostWidget;

        public DragHostWidget DragHostWidget {
            get {
                if (m_dragHostWidget == null) {
                    m_dragHostWidget = GameWidget?.FindChild<DragHostWidget>(false);
                }
                return m_dragHostWidget;
            }
        }

        public virtual void DealWithPlayerInteract(int priorityUse,
            int priorityPlace,
            int priorityInteract,
            PlayerInput playerInput,
            TerrainRaycastResult? terrainRaycastResult,
            MovingBlocksRaycastResult? movingBlocksRaycastResult,
            out bool flag) {
            // pending 机制说明：ComponentMiner.Place/Use/Interact 存 pending 后 return false（语义=已暂存待执行，非"未处理"），
            // 分发循环据此继续迭代。但 ComponentMiner 各入口的 AnyIsPending 锁保证一次按键最多存一个 pending——
            // 首个存 pending 的动作返回后，后续 Place/Use/Interact 入口见 AnyIsPending=true 直接 return false，不重复存也不执行 vanilla 动作。
            bool dealed = false;
            for (int t = 0; t < 3 && !dealed; t++) {
                int maxPriority = -1;
                if (maxPriority < priorityUse) {
                    maxPriority = priorityUse;
                }
                if (maxPriority < priorityPlace) {
                    maxPriority = priorityPlace;
                }
                if (maxPriority < priorityInteract) {
                    maxPriority = priorityInteract;
                }
                if (maxPriority <= 0) {
                    break;
                }
                if (maxPriority == priorityUse /* && !dealed*/) {
                    if (playerInput.Interact.HasValue) {
                        dealed = ComponentMiner.Use(playerInput.Interact.Value);
                    }
                    priorityUse = -2;
                }
                if (maxPriority == priorityInteract
                    && !dealed) {
                    if (movingBlocksRaycastResult.HasValue) {
                        dealed = ComponentMiner.Interact(movingBlocksRaycastResult.Value);
                    }
                    else if (terrainRaycastResult.HasValue) {
                        dealed = ComponentMiner.Interact(terrainRaycastResult.Value);
                    }
                    priorityInteract = -2;
                }
                if (maxPriority == priorityPlace
                    && !dealed) {
                    if (terrainRaycastResult.HasValue) {
                        dealed = ComponentMiner.Place(terrainRaycastResult.Value);
                    }
                    priorityPlace = -2;
                }
            }
            if (dealed) {
                m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
                flag = true;
                m_isAimBlocked = true;
                return;
            }
            flag = false;
        }

        public virtual void Update(float dt) {
            PlayerInput playerInput = ComponentInput.PlayerInput;
            if (ComponentInput.IsControlledByTouch
                && m_aim.HasValue
                && m_doAimBlockLook) {
                playerInput.Look = Vector2.Zero;
            }
            if (ComponentMiner.Inventory != null) {
                int max = ComponentGui.ShortInventoryWidget.m_inventory is ComponentCreativeInventory
                    ? ComponentGui.ShortInventoryWidget.MaxVisibleSlotsCountInCreative
                    : ComponentGui.ShortInventoryWidget.MaxVisibleSlotsCount;
                ComponentMiner.Inventory.ActiveSlotIndex = SettingsManager.ShortInventoryLooping
                    ? (ComponentMiner.Inventory.ActiveSlotIndex + playerInput.ScrollInventory + max) % max//加上max再取模，防止出现负数结果
                    : ComponentMiner.Inventory.ActiveSlotIndex + playerInput.ScrollInventory;
                if (playerInput.SelectInventorySlot.HasValue) {
                    ComponentMiner.Inventory.ActiveSlotIndex = Math.Clamp(playerInput.SelectInventorySlot.Value, 0, max - 1);
                }
            }
            ComponentMount mount = ComponentRider.Mount;
            if (mount != null) {
                ComponentSteedBehavior componentSteedBehavior = mount.Entity.FindComponent<ComponentSteedBehavior>();
                ComponentBoat componentBoat = mount.Entity.FindComponent<ComponentBoat>();
                if (componentSteedBehavior != null) {
                    bool skipVanilla_h = false;
                    ModsManager.HookAction(
                        "OnPlayerControlSteed",
                        loader => {
                            loader.OnPlayerControlSteed(this, skipVanilla_h, out bool skipVanilla);
                            skipVanilla_h |= skipVanilla;
                            return false;
                        }
                    );
                    if (!skipVanilla_h) {
                        if (playerInput.Move.Z > 0.5f
                            && !m_speedOrderBlocked) {
                            m_subsystemAudio.PlayRandomSound(
                                PlayerData.PlayerClass == PlayerClass.Male ? "Audio/Creatures/MaleYellFast" : "Audio/Creatures/FemaleYellFast",
                                0.75f,
                                0f,
                                ComponentBody.Position,
                                2f,
                                false
                            );
                            componentSteedBehavior.SpeedOrder = 1;
                            m_speedOrderBlocked = true;
                        }
                        else if (playerInput.Move.Z < -0.5f
                            && !m_speedOrderBlocked) {
                            m_subsystemAudio.PlayRandomSound(
                                PlayerData.PlayerClass == PlayerClass.Male ? "Audio/Creatures/MaleYellSlow" : "Audio/Creatures/FemaleYellSlow",
                                0.75f,
                                0f,
                                ComponentBody.Position,
                                2f,
                                false
                            );
                            componentSteedBehavior.SpeedOrder = -1;
                            m_speedOrderBlocked = true;
                        }
                        else if (MathF.Abs(playerInput.Move.Z) <= 0.25f) {
                            m_speedOrderBlocked = false;
                        }
                        componentSteedBehavior.TurnOrder = playerInput.Move.X;
                        componentSteedBehavior.JumpOrder = playerInput.Jump ? 1 : 0;
                        ComponentLocomotion.LookOrder = new Vector2(playerInput.Look.X, 0f);
                    }
                }
                else if (componentBoat != null) {
                    bool skipVanilla_h = false;
                    ModsManager.HookAction(
                        "OnPlayerControlBoat",
                        loader => {
                            loader.OnPlayerControlBoat(this, skipVanilla_h, out bool skipVanilla);
                            skipVanilla_h |= skipVanilla;
                            return false;
                        }
                    );
                    if (!skipVanilla_h) {
                        componentBoat.TurnOrder = playerInput.Move.X;
                        componentBoat.MoveOrder = playerInput.Move.Z;
                        ComponentLocomotion.LookOrder = new Vector2(playerInput.Look.X, 0f);
                        ComponentCreatureModel.RowLeftOrder = playerInput.Move.X < -0.2f || playerInput.Move.Z > 0.2f;
                        ComponentCreatureModel.RowRightOrder = playerInput.Move.X > 0.2f || playerInput.Move.Z > 0.2f;
                    }
                }
                else {
                    bool skipVanilla_h = false;
                    ModsManager.HookAction(
                        "OnPlayerControlOtherMount",
                        loader => {
                            loader.OnPlayerControlOtherMount(this, skipVanilla_h, out bool skipVanilla);
                            skipVanilla_h |= skipVanilla;
                            return false;
                        }
                    );
                }
            }
            else {
                bool skipVanilla_h = false;
                ModsManager.HookAction(
                    "OnPlayerControlWalk",
                    loader => {
                        loader.OnPlayerControlWalk(this, skipVanilla_h, out bool skipVanilla);
                        skipVanilla_h |= skipVanilla;
                        return false;
                    }
                );
                if (!skipVanilla_h) {
                    ComponentLocomotion.WalkOrder = ComponentBody.IsCrouching
                        ? 0.66f * new Vector2(playerInput.CrouchMove.X, playerInput.CrouchMove.Z)
                        : new Vector2(playerInput.Move.X, playerInput.Move.Z);
                    ComponentLocomotion.FlyOrder = new Vector3(0f, playerInput.Move.Y, 0f);
                    ComponentLocomotion.TurnOrder = playerInput.Look * new Vector2(1f, 0f);
                    ComponentLocomotion.JumpOrder = MathUtils.Max(playerInput.Jump ? 1 : 0, ComponentLocomotion.JumpOrder);
                }
            }
            if (m_allowAddLookOrder) {
                ComponentLocomotion.LookOrder += playerInput.Look * (SettingsManager.FlipVerticalAxis ? new Vector2(0f, -1f) : new Vector2(0f, 1f));
                ComponentLocomotion.VrLookOrder = playerInput.VrLook;
                ComponentLocomotion.VrMoveOrder = playerInput.VrMove;
            }
            int num = Terrain.ExtractContents(ComponentMiner.ActiveBlockValue);
            Block block = BlocksManager.Blocks[num];
            bool flag = false;
            if (playerInput.Interact.HasValue) {
                double timeIntervalLastActionTime = 0.33;
                TerrainRaycastResult? terrainRaycastResult = ComponentMiner.Raycast<TerrainRaycastResult>(
                    playerInput.Interact.Value,
                    RaycastMode.Interaction
                );
                MovingBlocksRaycastResult? movingBlocksRaycastResult = ComponentMiner.Raycast<MovingBlocksRaycastResult>(
                    playerInput.Interact.Value,
                    RaycastMode.Interaction
                );
                int priorityUse = block.GetPriorityUse(ComponentMiner.ActiveBlockValue, ComponentMiner);
                int priorityPlace = 0;
                int priorityInteract = 0;
                if (movingBlocksRaycastResult.HasValue) {
                    int raycastValue = movingBlocksRaycastResult.Value.MovingBlock?.Value ?? 0;
                    if (raycastValue != 0) {
                        priorityInteract = BlocksManager.Blocks[Terrain.ExtractContents(raycastValue)]
                            .GetPriorityInteract(raycastValue, ComponentMiner);
                    }
                }
                else if (terrainRaycastResult.HasValue) {
                    int raycastValue = terrainRaycastResult.Value.Value;
                    priorityPlace = block.GetPriorityPlace(ComponentMiner.ActiveBlockValue, ComponentMiner);
                    priorityInteract = BlocksManager.Blocks[Terrain.ExtractContents(raycastValue)].GetPriorityInteract(raycastValue, ComponentMiner);
                }
                ModsManager.HookAction(
                    "OnPlayerInputInteract",
                    loader => {
                        // ReSharper disable AccessToModifiedClosure
                        loader.OnPlayerInputInteract(
                            this,
                            ref flag,
                            ref timeIntervalLastActionTime,
                            ref priorityUse,
                            ref priorityInteract,
                            ref priorityPlace
                        );
                        // ReSharper restore AccessToModifiedClosure
                        return false;
                    }
                );
                if (!flag
                    && m_subsystemTime.GameTime - m_lastActionTime > timeIntervalLastActionTime) {
                    //处理三者的关系，优先级最高的优先执行
                    DealWithPlayerInteract(
                        priorityUse,
                        priorityPlace,
                        priorityInteract,
                        playerInput,
                        terrainRaycastResult,
                        movingBlocksRaycastResult,
                        out flag
                    );
                }
            }
            float timeIntervalAim = m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative ? 0.1f : 1.4f;
            if (playerInput.Aim.HasValue) {
                bool skipVanilla_h = false;
                ModsManager.HookAction(
                    "UpdatePlayerInputAim",
                    loader => {
                        loader.UpdatePlayerInputAim(this, true, ref flag, ref timeIntervalAim, skipVanilla_h, out bool skip);
                        skipVanilla_h |= skip;
                        return false;
                    }
                );
                if (!skipVanilla_h
                    && block.IsAimable_(ComponentMiner.ActiveBlockValue)
                    && m_subsystemTime.GameTime - m_lastActionTime > timeIntervalAim) {
                    if (!m_isAimBlocked) {
                        Ray3 value = playerInput.Aim.Value;
                        Vector3 vector = GameWidget.ActiveCamera.WorldToScreen(value.Position + value.Direction, Matrix.Identity);
                        Point2 size = Window.Size;
                        if (vector.X >= size.X * 0.02f
                            && vector.X < size.X * 0.98f
                            && vector.Y >= size.Y * 0.02f
                            && vector.Y < size.Y * 0.98f) {
                            m_aim = value;
                            m_aimStartTime ??= m_subsystemTime.GameTime;
                            if (ComponentMiner.Aim(value, AimState.InProgress)) {
                                ComponentMiner.Aim(m_aim.Value, AimState.Cancelled);
                                m_aim = null;
                                m_aimStartTime = null;
                                m_isAimBlocked = true;
                            }
                            else if (!m_aimHintIssued
                                && Time.PeriodicEvent(1.0, 0.0)) {
                                Time.QueueTimeDelayedExecution(
                                    Time.RealTime + 3.0,
                                    delegate {
                                        if (!m_aimHintIssued
                                            && m_aim.HasValue
                                            && !ComponentBody.IsCrouching) {
                                            m_aimHintIssued = true;
                                            ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, true, true);
                                        }
                                    }
                                );
                            }
                        }
                        else if (m_aim.HasValue) {
                            ComponentMiner.Aim(m_aim.Value, AimState.Cancelled);
                            m_aim = null;
                            m_aimStartTime = null;
                            m_isAimBlocked = true;
                        }
                    }
                }
            }
            else {
                m_isAimBlocked = false;
                bool skipVanilla_h = false;
                ModsManager.HookAction(
                    "UpdatePlayerInputAim",
                    loader => {
                        loader.UpdatePlayerInputAim(this, false, ref flag, ref timeIntervalAim, skipVanilla_h, out bool skip);
                        skipVanilla_h |= skip;
                        return false;
                    }
                );
                if (!skipVanilla_h
                    && m_aim.HasValue) {
                    ComponentMiner.Aim(m_aim.Value, AimState.Completed);
                    m_aim = null;
                    m_aimStartTime = null;
                    m_lastActionTime = m_subsystemTime.GameTime;
                }
            }
            flag |= m_aim.HasValue;
            if (playerInput.Hit.HasValue) {
                bool skipVanilla_ = false;
                double timeIntervalHit = 0.33;
                float meleeAttackRange = 2f;
                ModsManager.HookAction(
                    "OnPlayerInputHit",
                    loader => {
                        loader.OnPlayerInputHit(this, ref flag, ref timeIntervalHit, ref meleeAttackRange, skipVanilla_, out bool skip);
                        skipVanilla_ |= skip;
                        return false;
                    }
                );
                if (!skipVanilla_
                    && !flag
                    && m_subsystemTime.GameTime - m_lastActionTime > timeIntervalHit
                    && block.GetMeleeHitProbability(ComponentMiner.ActiveBlockValue) > 0
                    && meleeAttackRange > 0) {
                    BodyRaycastResult? bodyRaycastResult = ComponentMiner.Raycast<BodyRaycastResult>(
                        playerInput.Hit.Value,
                        RaycastMode.Interaction,
                        reach: meleeAttackRange
                    );
                    if (bodyRaycastResult.HasValue) {
                        flag = true;
                        m_isDigBlocked = true;
                        if (Vector3.Distance(bodyRaycastResult.Value.HitPoint(), ComponentCreatureModel.EyePosition) <= meleeAttackRange) {
                            ComponentMiner.Hit(
                                bodyRaycastResult.Value.ComponentBody,
                                bodyRaycastResult.Value.HitPoint(),
                                playerInput.Hit.Value.Direction
                            );
                        }
                    }
                }
            }
            double timeIntervalDig = 0.33;
            if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) {
                timeIntervalDig = SettingsManager.CreativeDigTime;
            }
            if (playerInput.Dig.HasValue) {
                bool skipVanilla_ = false;
                ModsManager.HookAction(
                    "UpdatePlayerInputDig",
                    loader => {
                        loader.UpdatePlayerInputDig(this, true, ref flag, ref timeIntervalDig, skipVanilla_, out bool skip);
                        skipVanilla_ |= skip;
                        return false;
                    }
                );
                if (!skipVanilla_
                    && !flag
                    && !m_isDigBlocked
                    && m_subsystemTime.GameTime - m_lastActionTime > timeIntervalDig) {
                    TerrainRaycastResult? terrainRaycastResult2 = ComponentMiner.Raycast<TerrainRaycastResult>(
                        playerInput.Dig.Value,
                        RaycastMode.Digging
                    );
                    if (terrainRaycastResult2.HasValue
                        && ComponentMiner.Dig(terrainRaycastResult2.Value)) {
                        m_lastActionTime = m_subsystemTime.GameTime;
                        m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
                    }
                }
            }
            if (!playerInput.Dig.HasValue) {
                m_isDigBlocked = false;
                bool skipVanilla_ = false;
                ModsManager.HookAction(
                    "UpdatePlayerInputDig",
                    loader => {
                        loader.UpdatePlayerInputDig(this, false, ref flag, ref timeIntervalDig, skipVanilla_, out bool skip);
                        skipVanilla_ |= skip;
                        return false;
                    }
                );
            }
            if (playerInput.Drop
                && ComponentMiner.Inventory != null) {
                bool skipVanilla_ = false;
                ModsManager.HookAction(
                    "UpdatePlayerInputDrop",
                    loader => {
                        loader.OnPlayerInputDrop(this, skipVanilla_, out bool skip);
                        skipVanilla_ |= skip;
                        return false;
                    }
                );
                if (!skipVanilla_) {
                    IInventory inventory = ComponentMiner.Inventory;
                    int slotValue = inventory.GetSlotValue(inventory.ActiveSlotIndex);
                    int num3 = inventory.RemoveSlotItems(
                        count: inventory.GetSlotCount(inventory.ActiveSlotIndex),
                        slotIndex: inventory.ActiveSlotIndex
                    );
                    if (slotValue != 0
                        && num3 != 0) {
                        Vector3 position = ComponentBody.Position
                            + new Vector3(0f, ComponentBody.StanceBoxSize.Y * 0.66f, 0f)
                            + 0.25f * ComponentBody.Matrix.Forward;
                        Vector3 value2 = 8f * Matrix.CreateFromQuaternion(ComponentCreatureModel.EyeRotation).Forward;
                        m_subsystemPickables.AddPickable(slotValue, num3, position, value2, null, Entity);
                    }
                }
            }
            if (!playerInput.PickBlockType.HasValue || flag) {
                return;
            }
            if (ComponentMiner.Inventory is not ComponentCreativeInventory componentCreativeInventory) {
                return;
            }
            TerrainRaycastResult? terrainRaycastResult3 = ComponentMiner.Raycast<TerrainRaycastResult>(
                playerInput.PickBlockType.Value,
                RaycastMode.Digging,
                true,
                false,
                false
            );
            if (!terrainRaycastResult3.HasValue) {
                return;
            }
            int value3 = terrainRaycastResult3.Value.Value;
            value3 = Terrain.ReplaceLight(value3, 0);
            int num4 = Terrain.ExtractContents(value3);
            Block block2 = BlocksManager.Blocks[num4];
            int num5 = 0;
            IEnumerable<int> creativeValues = block2.GetCreativeValues();
            if (block2.GetCreativeValues().Contains(value3)) {
                num5 = value3;
            }
            if (num5 == 0
                && !block2.IsNonDuplicable_(value3)) {
                List<BlockDropValue> list = new();
                block2.GetDropValues(m_subsystemTerrain, value3, 0, int.MaxValue, list, out bool _);
                if (list.Count > 0
                    && list[0].Count > 0) {
                    num5 = list[0].Value;
                }
            }
            if (num5 == 0) {
                num5 = creativeValues.FirstOrDefault();
            }
            if (num5 == 0) {
                return;
            }
            int num6 = -1;
            for (int i = 0; i < 10; i++) {
                if (componentCreativeInventory.GetSlotCapacity(i, num5) > 0
                    && componentCreativeInventory.GetSlotCount(i) > 0
                    && componentCreativeInventory.GetSlotValue(i) == num5) {
                    num6 = i;
                    break;
                }
            }
            if (num6 < 0) {
                for (int j = 0; j < 10; j++) {
                    if (componentCreativeInventory.GetSlotCapacity(j, num5) > 0
                        && (componentCreativeInventory.GetSlotCount(j) == 0 || componentCreativeInventory.GetSlotValue(j) == 0)) {
                        num6 = j;
                        break;
                    }
                }
            }
            if (num6 < 0) {
                num6 = componentCreativeInventory.ActiveSlotIndex;
            }
            componentCreativeInventory.RemoveSlotItems(num6, int.MaxValue);
            componentCreativeInventory.AddSlotItems(num6, num5, 1);
            componentCreativeInventory.ActiveSlotIndex = num6;
            ComponentGui.DisplaySmallMessage(block2.GetDisplayName(m_subsystemTerrain, value3), Color.White, false, false);
            m_subsystemAudio.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f, 0f);
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            ComponentGui = Entity.FindComponent<ComponentGui>(true);
            ComponentInput = Entity.FindComponent<ComponentInput>(true);
            ComponentScreenOverlays = Entity.FindComponent<ComponentScreenOverlays>(true);
            ComponentBlockHighlight = Entity.FindComponent<ComponentBlockHighlight>(true);
            ComponentAimingSights = Entity.FindComponent<ComponentAimingSights>(true);
            ComponentMiner = Entity.FindComponent<ComponentMiner>(true);
            ComponentRider = Entity.FindComponent<ComponentRider>(true);
            ComponentSleep = Entity.FindComponent<ComponentSleep>(true);
            ComponentVitalStats = Entity.FindComponent<ComponentVitalStats>(true);
            ComponentSickness = Entity.FindComponent<ComponentSickness>(true);
            ComponentFlu = Entity.FindComponent<ComponentFlu>(true);
            ComponentLevel = Entity.FindComponent<ComponentLevel>(true);
            ComponentClothing = Entity.FindComponent<ComponentClothing>(true);
            ComponentOuterClothingModel = Entity.FindComponent<ComponentOuterClothingModel>(true);
            int playerIndex = valuesDictionary.GetValue<int>("PlayerIndex");
            SubsystemPlayers subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            if(PlayerData == null)
                PlayerData = subsystemPlayers.m_playersData.First(d => d.PlayerIndex == playerIndex);
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            base.Save(valuesDictionary, entityToIdMap);
            valuesDictionary.SetValue("PlayerIndex", PlayerData.PlayerIndex);
        }
    }
}