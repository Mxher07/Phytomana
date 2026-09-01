using Engine;
using Engine.Graphics;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentClothing : Component, IUpdateable, IInventory {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemTime m_subsystemTime;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemPickables m_subsystemPickables;

        public ComponentGui m_componentGui;

        public ComponentHumanModel m_componentHumanModel;

        public ComponentBody m_componentBody;

        public ComponentOuterClothingModel m_componentOuterClothingModel;

        public ComponentVitalStats m_componentVitalStats;

        public ComponentLocomotion m_componentLocomotion;

        public ComponentPlayer m_componentPlayer;

        public Texture2D m_skinTexture;

        public string m_skinTextureName;

        public RenderTarget2D m_innerClothedTexture;

        public RenderTarget2D m_outerClothedTexture;

        public PrimitivesRenderer2D m_primitivesRenderer = new();

        public Random m_random = new();

        public float m_densityModifierApplied;

        public double? m_lastTotalElapsedGameTime;

        public bool m_clothedTexturesValid;

        /// <summary>
        ///     穿戴衣服限制
        /// </summary>
        public bool EnableDressLimit;

        public static string fName = "ComponentClothing";

        public List<int> m_clothesList = [];

        public Dictionary<ClothingSlot, List<int>> m_clothes = [];
        public static ClothingSlot[] m_innerSlotsOrder => m_innerSlotsOrderList.ToArray();
        public static ClothingSlot[] m_outerSlotsOrder => m_outerSlotsOrderList.ToArray();

        public static List<ClothingSlot> m_innerSlotsOrderList = [];

        public static List<ClothingSlot> m_outerSlotsOrderList = [];

        /// <summary>
        /// 服装贴图合成总开关。为 false 时 <see cref="UpdateRenderTargets"/> 直接返回，
        /// 不创建合成 RenderTarget、也不把 HumanModel/OuterClothingModel 的贴图覆盖为合成贴图。
        /// 用于自定义贴图布局的模型（如 glTF 玩家模型，UV 与原版 dae 皮肤贴图不一致）。
        /// 实例级，由 ValuesDictionary 的 "ShowClothedTexture" 参数初始化。
        /// </summary>
        public static bool ShowClothedTexture = true;

        /// <summary>
        /// 是否实际把皮肤+服装贴图画进合成 RenderTarget。为 false 时仍创建并覆盖 RenderTarget
        /// （贴图为空），但不执行 2D 绘制。仅在 <see cref="ShowClothedTexture"/> 为 true 时才有意义。
        /// 实例级，由 ValuesDictionary 的 "DrawClothedTexture" 参数初始化。
        /// </summary>
        public static bool DrawClothedTexture = true;

        public Texture2D InnerClothedTexture => m_innerClothedTexture;
        public Texture2D OuterClothedTexture => m_outerClothedTexture;

        public Dictionary<ClothingSlot, float> InsulationBySlots = [];
        public float Insulation { get; set; }

        public ClothingSlot LeastInsulatedSlot { get; set; }

        public float SteedMovementSpeedFactor { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        Project IInventory.Project => Project;

        public int SlotsCount => ClothingSlot.ClothingSlots.Count;

        public int VisibleSlotsCount {
            get => SlotsCount;
            set { }
        }

        public int ActiveSlotIndex {
            get => -1;
            set { }
        }

        public virtual ReadOnlyList<int> GetClothes(ClothingSlot slot) => new(m_clothes[slot]);

        public float CalculateInsulationFromSlots() {
            float x = 0f;
            float leastSlotInsulation = InsulationBySlots[0];
            int leastInsulatedSlot = 0;
            for (int i = 0; i < SlotsCount && i < InsulationBySlots.Count; i++) {
                x += 1f / InsulationBySlots[i];
                if (leastSlotInsulation > InsulationBySlots[i]) {
                    leastInsulatedSlot = i;
                    leastSlotInsulation = InsulationBySlots[i];
                }
            }
            Insulation = 1f / x;
            LeastInsulatedSlot = leastInsulatedSlot;
            return Insulation;
        }

        public virtual void SetClothes(ClothingSlot slot, IEnumerable<int> clothes) {
            IEnumerable<int> enumerable = clothes as List<int> ?? clothes.ToList();
            if (!m_clothes[slot].SequenceEqual(enumerable)) {
                m_clothes[slot].Clear();
                m_clothes[slot].AddRange(enumerable);
                m_clothedTexturesValid = false;
                float densityModiferAppliedBefore = m_densityModifierApplied;
                m_densityModifierApplied = 0f;
                SteedMovementSpeedFactor = 1f;
                foreach (ClothingSlot clothingSlot in ClothingSlot.ClothingSlots.Values) {
                    InsulationBySlots[clothingSlot] = clothingSlot.BasicInsulation;
                }
                foreach (KeyValuePair<ClothingSlot, List<int>> clothe in m_clothes) {
                    foreach (int item in clothe.Value) {
                        Block block = BlocksManager.Blocks[Terrain.ExtractContents(item)];
                        ClothingData clothingData = block.GetClothingData(item);
                        clothingData?.OnClotheSet(this);
                    }
                }
                float num2 = m_densityModifierApplied - densityModiferAppliedBefore;
                m_componentBody.Density += num2;
                CalculateInsulationFromSlots();
            }
            ModsManager.HookAction(
                "SetClothes",
                loader => {
                    loader.SetClothes(this, slot, enumerable);
                    return false;
                }
            );
        }

        [Obsolete("Use ApplyArmorProtection(Attackment attackment) instead.")]
        public float ApplyArmorProtection(float attackPower) =>
            ApplyArmorProtection(new Attackment(Entity, null, Vector3.Zero, Vector3.UnitY, attackPower));

        public float ApplyArmorProtection(Attackment attackment) {
            bool Applied = false;
            float attackPowerAfterProtection = attackment.AttackPower;
            //ApplyArmorProtection接口废弃，并且只有在下面的接口都没有模组用的时候，才允许模组用这个接口。
            if (!ModsManager.ModHooks.ContainsKey("DecideArmorProtectionSequence")
                && !ModsManager.ModHooks.ContainsKey("ApplyProtectionBeforeClothes")
                && !!ModsManager.ModHooks.ContainsKey("ApplyProtectionAfterClothes")) {
                ModsManager.HookAction(
                    "ApplyArmorProtection",
                    modLoader => {
#pragma warning disable CS0618
                        attackment.AttackPower = modLoader.ApplyArmorProtection(this, attackment.AttackPower, Applied, out bool flag2);
#pragma warning restore CS0618
                        Applied |= flag2;
                        return false;
                    }
                );
            }
            if (!Applied) {
                //决定参与结算的衣物列表
                float num = m_random.Float(0f, 1f);
                ClothingSlot slot = num < 0.1f ? ClothingSlot.Feet :
                    num < 0.3f ? ClothingSlot.Legs :
                    num < 0.9f ? ClothingSlot.Torso : ClothingSlot.Head;
                List<int> listBeforeProtection = new(GetClothes(slot));
                ModsManager.HookAction(
                    "ApplyProtectionBeforeClothes",
                    loader => {
                        // ReSharper disable AccessToModifiedClosure
                        loader.ApplyProtectionBeforeClothes(this, attackment, ref attackPowerAfterProtection);
                        // ReSharper restore AccessToModifiedClosure
                        return false;
                    }
                );
                ModsManager.HookAction(
                    "DecideArmorProtectionSequence",
                    loader => {
                        loader.DecideArmorProtectionSequence(this, attackment, num, listBeforeProtection);
                        return false;
                    }
                );
                List<int> listAfterProtection = new(listBeforeProtection);
                //对每件衣物，结算护甲
                for (int i = 0; i < listBeforeProtection.Count; i++) {
                    int value = listBeforeProtection[i];
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                    ClothingData clothingData = block.GetClothingData(value);
                    if (clothingData == null) {
                        continue;
                    }
                    try {
                        clothingData.ApplyArmorProtection(
                            this,
                            listBeforeProtection,
                            listAfterProtection,
                            i,
                            attackment,
                            ref attackPowerAfterProtection
                        );
                    }
                    catch (Exception e) {
                        Log.Error($"ClothingData of clothing{clothingData.DisplayName} applies armor protection error: {e}");
                    }
                }
                //移除护甲结算后，破损衣物
                int num4 = 0;
                while (num4 < listAfterProtection.Count) {
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(listAfterProtection[num4])];
                    if (!block.CanWear(listAfterProtection[num4])) {
                        listAfterProtection.RemoveAt(num4);
                    }
                    else {
                        num4++;
                    }
                }
                listAfterProtection.Sort((a, b) => {
                        ClothingData clothingDataA = BlocksManager.Blocks[Terrain.ExtractContents(a)].GetClothingData(a);
                        ClothingData clothingDataB = BlocksManager.Blocks[Terrain.ExtractContents(b)].GetClothingData(b);
                        return (clothingDataA?.Layer ?? 0) - (clothingDataB?.Layer ?? 0);
                    }
                );
                ModsManager.HookAction(
                    "ApplyProtectionAfterClothes",
                    loader => {
                        loader.ApplyProtectionAfterClothes(this, attackment, listAfterProtection, ref attackPowerAfterProtection);
                        return false;
                    }
                );
                //最后SetClothes
                SetClothes(slot, listAfterProtection);
            }
            return MathF.Max(attackPowerAfterProtection, 0f);
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_innerSlotsOrderList.Clear();
            m_innerSlotsOrderList.AddRange(ClothingSlot.ClothingSlots.Values);
            m_innerSlotsOrderList.Reverse(2, 2); //让Legs显示在feet之前
            m_outerSlotsOrderList.Clear();
            m_outerSlotsOrderList.AddRange(ClothingSlot.ClothingSlots.Values);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_componentGui = Entity.FindComponent<ComponentGui>(true);
            m_componentHumanModel = Entity.FindComponent<ComponentHumanModel>(true);
            m_componentBody = Entity.FindComponent<ComponentBody>(true);
            m_componentOuterClothingModel = Entity.FindComponent<ComponentOuterClothingModel>(true);
            m_componentVitalStats = Entity.FindComponent<ComponentVitalStats>(true);
            m_componentLocomotion = Entity.FindComponent<ComponentLocomotion>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            EnableDressLimit = m_subsystemGameInfo.WorldSettings.GameMode != 0
                && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled;
            SteedMovementSpeedFactor = 1f;
            Insulation = 0f;
            LeastInsulatedSlot = ClothingSlot.Feet;
            foreach (ClothingSlot clothingSlot in m_innerSlotsOrder) {
                m_clothes[clothingSlot] = [];
            }
            ShowClothedTexture = valuesDictionary.GetValue<bool>("ShowClothedTexture", true);
            DrawClothedTexture = valuesDictionary.GetValue<bool>("DrawClothedTexture", true);
            ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("Clothes");
            foreach (string key in ClothingSlot.ClothingSlots.Keys) {
                SetClothes(ClothingSlot.ClothingSlots[key], HumanReadableConverter.ValuesListFromString<int>(';', value.GetValue<string>(key)));
            }
            Display.DeviceReset += Display_DeviceReset;
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Clothes", valuesDictionary2);
            foreach (string key in ClothingSlot.ClothingSlots.Keys) {
                valuesDictionary2.SetValue(key, HumanReadableConverter.ValuesListToString(';', m_clothes[ClothingSlot.ClothingSlots[key]].ToArray()));
            }
        }

        public override void Dispose() {
            base.Dispose();
            if (m_skinTexture != null
                && !ContentManager.IsContent(m_skinTexture)) {
                m_skinTexture.Dispose();
                m_skinTexture = null;
            }
            if (m_innerClothedTexture != null) {
                m_innerClothedTexture.Dispose();
                m_innerClothedTexture = null;
            }
            if (m_outerClothedTexture != null) {
                m_outerClothedTexture.Dispose();
                m_outerClothedTexture = null;
            }
            Display.DeviceReset -= Display_DeviceReset;
        }

        public virtual void Update(float dt) {
            //触发ClothingData.Update
            foreach (ClothingSlot slot in m_innerSlotsOrder) {
                foreach (int clothe in GetClothes(slot)) {
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe)];
                    ClothingData clothingData = block.GetClothingData(clothe);
                    clothingData?.Update?.Invoke(clothe, this);
                }
            }
            foreach (ClothingSlot slot in m_outerSlotsOrder) {
                foreach (int clothe in GetClothes(slot)) {
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe)];
                    ClothingData clothingData = block.GetClothingData(clothe);
                    clothingData?.Update?.Invoke(clothe, this);
                }
            }
            //生存模式每0.5秒执行一次，不允许玩家越级穿衣物
            if (EnableDressLimit && m_subsystemTime.PeriodicGameTimeEvent(0.5, 0.0)) {
                foreach (ClothingSlot clothingSlot in ClothingSlot.ClothingSlots.Values) {
                    bool flag = false;
                    m_clothesList.Clear();
                    m_clothesList.AddRange(GetClothes(clothingSlot));
                    int num = 0;
                    while (num < m_clothesList.Count) {
                        int value = m_clothesList[num];
                        Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                        ClothingData clothingData = block.GetClothingData(value);
                        if (clothingData == null) {
                            m_clothesList.RemoveAt(num);
                            flag = true;
                        }
                        if (clothingData != null
                            && clothingData.PlayerLevelRequired > m_componentPlayer.PlayerData.Level) {
                            m_componentGui.DisplaySmallMessage(
                                string.Format(LanguageControl.Get(fName, 1), clothingData.PlayerLevelRequired, clothingData.DisplayName),
                                Color.White,
                                true,
                                true
                            );
                            m_subsystemPickables.AddPickable(value, 1, m_componentBody.Position, null, null, Entity);
                            m_clothesList.RemoveAt(num);
                            flag = true;
                        }
                        else {
                            num++;
                        }
                    }
                    if (flag) {
                        SetClothes(clothingSlot, m_clothesList);
                    }
                }
            }
            if (m_subsystemTime.PeriodicGameTimeEvent(2.0, 0.0)
                && ((m_componentLocomotion.LastWalkOrder.HasValue && m_componentLocomotion.LastWalkOrder.Value != Vector2.Zero)
                    || (m_componentLocomotion.LastSwimOrder.HasValue && m_componentLocomotion.LastSwimOrder.Value != Vector3.Zero)
                    || m_componentLocomotion.LastJumpOrder != 0f)) {
                if (m_lastTotalElapsedGameTime.HasValue) {
                    foreach (ClothingSlot clothingSlot in ClothingSlot.ClothingSlots.Values) {
                        bool setClothesNeeded = false;
                        m_clothesList.Clear();
                        m_clothesList.AddRange(GetClothes(clothingSlot));
                        for (int i = 0; i < m_clothesList.Count; i++) {
                            int value2 = m_clothesList[i];
                            Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(value2)];
                            ClothingData clothingData2 = block2.GetClothingData(value2);
                            if (clothingData2 == null) {
                                continue;
                            }
                            clothingData2.UpdateGraduallyDamagedOverTime(this, i, dt);
                            if (m_clothesList[i] != value2) {
                                setClothesNeeded = true;
                            }
                        }
                        //移除已经损坏的衣物
                        int num4 = 0;
                        while (num4 < m_clothesList.Count) {
                            Block block = BlocksManager.Blocks[Terrain.ExtractContents(m_clothesList[num4])];
                            if (!block.CanWear(m_clothesList[num4])) {
                                m_clothesList.RemoveAt(num4);
                            }
                            else {
                                num4++;
                            }
                        }
                        if (setClothesNeeded) {
                            SetClothes(clothingSlot, m_clothesList);
                        }
                    }
                }
                m_lastTotalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
            }
            UpdateRenderTargets();
        }

        public virtual int GetSlotValue(int slotIndex) => GetClothes(slotIndex).LastOrDefault();

        public virtual int GetSlotCount(int slotIndex) {
            if (GetClothes(slotIndex).Count <= 0) {
                return 0;
            }
            return 1;
        }

        public virtual int GetSlotCapacity(int slotIndex, int value) => 0;

        public virtual int GetSlotProcessCapacity(int slotIndex, int value) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            bool notToProcessVanilla = false;
            int maxCapacity = 0;
            ModsManager.HookAction(
                "ClothingSlotProcessCapacity",
                loader => {
                    loader.ClothingSlotProcessCapacity(this, slotIndex, value, ref notToProcessVanilla, out int capacity);
                    maxCapacity = Math.Max(capacity, maxCapacity);
                    return false;
                }
            );
            if (!notToProcessVanilla
                && maxCapacity < 1) {
                if (block.GetNutritionalValue(value) > 0f) {
                    return 1;
                }
                if (block.CanWear(value)
                    && CanWearClothing(value)) {
                    return 1;
                }
            }
            return maxCapacity;
        }

        public virtual void AddSlotItems(int slotIndex, int value, int count) { }

        public virtual void ProcessSlotItems(int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount) {
            int pValue = 0;
            int pCount = 0;
            bool skippedByMods = false;
            ModsManager.HookAction(
                "ClothingProcessSlotItems",
                modLoader => {
                    modLoader.ClothingProcessSlotItems(
                        this,
                        slotIndex,
                        ref value,
                        ref count,
                        ref pValue,
                        ref pCount,
                        skippedByMods,
                        out bool skipVanilla
                    );
#pragma warning disable CS0618
                    modLoader.ClothingProcessSlotItems(
                        m_componentPlayer,
                        slotIndex,
                        ref value,
                        ref count,
                        ref pCount,
                        ref pValue,
                        skippedByMods,
                        out bool glitchedLoaderResult
                    );
                    bool oldLoaderResult = modLoader.ClothingProcessSlotItems(
                        m_componentPlayer,
                        BlocksManager.Blocks[Terrain.ExtractContents(value)],
                        slotIndex,
                        value,
                        count
                    );
#pragma warning restore CS0618
                    skippedByMods |= oldLoaderResult;
                    skippedByMods |= skipVanilla;
                    skippedByMods |= glitchedLoaderResult;
                    return false;
                }
            );
            processedCount = pCount;
            processedValue = pValue;
            if (skippedByMods) {
                return;
            }
            if (processCount != 1) {
                return;
            }
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            if (block.GetNutritionalValue(value) > 0f) {
                if (block is BucketBlock) {
                    processedValue = EmptyBucketBlock.Index;
                    processedCount = 1;
                }
                if (count > 1
                    && processedCount > 0
                    && processedValue != value) {
                    processedValue = value;
                    processedCount = processCount;
                }
                else if (block.Eat(m_componentVitalStats, value)
                    || !m_componentVitalStats.Eat(value)) {
                    processedValue = value;
                    processedCount = processCount;
                }
            }
            if (block.CanWear(value)) {
                ClothingData clothingData = block.GetClothingData(value);
                if (clothingData == null) {
                    return;
                }
                clothingData.Mount?.Invoke(value, this);
                List<int> list = [..GetClothes(clothingData.Slot), value];
                SetClothes(clothingData.Slot, list);
            }
        }

        public virtual int RemoveSlotItems(int slotIndex, int count) {
            if (count == 1) {
                List<int> list = new(GetClothes(slotIndex));
                if (list.Count > 0) {
                    int value = list[^1];
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                    ClothingData clothingData = block.GetClothingData(value);
                    clothingData?.Dismount?.Invoke(value, this);
                    list.RemoveAt(list.Count - 1);
                    SetClothes(slotIndex, list);
                    return 1;
                }
            }
            return 0;
        }

        public virtual void DropAllItems(Vector3 position) {
            Random random = new();
            SubsystemPickables subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            for (int i = 0; i < SlotsCount; i++) {
                int slotCount = GetSlotCount(i);
                if (slotCount > 0) {
                    int slotValue = GetSlotValue(i);
                    int count = RemoveSlotItems(i, slotCount);
                    Vector3 value = random.Float(5f, 10f)
                        * Vector3.Normalize(new Vector3(random.Float(-1f, 1f), random.Float(1f, 2f), random.Float(-1f, 1f)));
                    subsystemPickables.AddPickable(slotValue, count, position, value, null, Entity);
                }
            }
        }

        public virtual void Display_DeviceReset() {
            m_clothedTexturesValid = false;
        }

        public virtual bool CanWearClothing(int value) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            ClothingData clothingData = block.GetClothingData(value);
            if (clothingData == null) {
                return false;
            }
            IList<int> list = GetClothes(clothingData.Slot);
            if (list.Count == 0) {
                return true;
            }
            int value2 = list[list.Count - 1];
            Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(value2)];
            ClothingData clothingData2 = block2.GetClothingData(value2);
            if (clothingData2 == null) {
                return false;
            }
            return clothingData.Layer > clothingData2.Layer;
        }

        public virtual void UpdateRenderTargets() {
            if (!ShowClothedTexture || m_componentPlayer.PlayerData.CharacterSkinName == "$Empty") {
                return;
            }
            if (m_skinTexture == null
                || m_componentPlayer.PlayerData.CharacterSkinName != m_skinTextureName) {
                m_skinTexture = CharacterSkinsManager.LoadTexture(m_componentPlayer.PlayerData.CharacterSkinName);
                m_skinTextureName = m_componentPlayer.PlayerData.CharacterSkinName;
                Utilities.Dispose(ref m_innerClothedTexture);
                Utilities.Dispose(ref m_outerClothedTexture);
            }
            if (m_innerClothedTexture == null
                || m_innerClothedTexture.Width != m_skinTexture.Width
                || m_innerClothedTexture.Height != m_skinTexture.Height) {
                m_innerClothedTexture = new RenderTarget2D(m_skinTexture.Width, m_skinTexture.Height, 1, ColorFormat.Rgba8888, DepthFormat.None);
                m_componentHumanModel.TextureOverride = m_innerClothedTexture;
                m_clothedTexturesValid = false;
            }
            if (m_outerClothedTexture == null
                || m_outerClothedTexture.Width != m_skinTexture.Width
                || m_outerClothedTexture.Height != m_skinTexture.Height) {
                m_outerClothedTexture = new RenderTarget2D(m_skinTexture.Width, m_skinTexture.Height, 1, ColorFormat.Rgba8888, DepthFormat.None);
                m_componentOuterClothingModel.TextureOverride = m_outerClothedTexture;
                m_clothedTexturesValid = false;
            }
            if (DrawClothedTexture && !m_clothedTexturesValid) {
                m_clothedTexturesValid = true;
                Rectangle scissorRectangle = Display.ScissorRectangle;
                RenderTarget2D renderTarget = Display.RenderTarget;
                try {
                    Display.RenderTarget = m_innerClothedTexture;
                    Display.Clear(new Vector4(Color.Transparent));
                    int num = 0;
                    TexturedBatch2D texturedBatch2D = m_primitivesRenderer.TexturedBatch(
                        m_skinTexture,
                        false,
                        num++,
                        DepthStencilState.None,
                        null,
                        BlendState.NonPremultiplied,
                        SamplerState.PointClamp
                    );
                    texturedBatch2D.QueueQuad(
                        Vector2.Zero,
                        new Vector2(m_innerClothedTexture.Width, m_innerClothedTexture.Height),
                        0f,
                        Vector2.Zero,
                        Vector2.One,
                        Color.White
                    );
                    ClothingSlot[] innerSlotsOrder = m_innerSlotsOrder;
                    foreach (ClothingSlot slot in innerSlotsOrder) {
                        foreach (int clothe in GetClothes(slot)) {
                            Block block = BlocksManager.Blocks[Terrain.ExtractContents(clothe)];
                            ClothingData clothingData = block.GetClothingData(clothe);
                            if (clothingData == null) {
                                continue;
                            }
                            clothingData.Texture ??= ContentManager.Get<Texture2D>(clothingData._textureName);
                            Color fabricColor = clothingData.GetColor(this, clothe);
                            texturedBatch2D = m_primitivesRenderer.TexturedBatch(
                                clothingData.Texture,
                                false,
                                num++,
                                DepthStencilState.None,
                                null,
                                BlendState.NonPremultiplied,
                                SamplerState.PointClamp
                            );
                            if (!clothingData.IsOuter) {
                                texturedBatch2D.QueueQuad(
                                    new Vector2(0f, 0f),
                                    new Vector2(m_innerClothedTexture.Width, m_innerClothedTexture.Height),
                                    0f,
                                    Vector2.Zero,
                                    Vector2.One,
                                    fabricColor
                                );
                            }
                        }
                    }
                    m_primitivesRenderer.Flush();
                    Display.RenderTarget = m_outerClothedTexture;
                    Display.Clear(new Vector4(Color.Transparent));
                    num = 0;
                    innerSlotsOrder = m_outerSlotsOrder;
                    foreach (ClothingSlot slot2 in innerSlotsOrder) {
                        foreach (int clothe2 in GetClothes(slot2)) {
                            Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(clothe2)];
                            ClothingData clothingData2 = block2.GetClothingData(clothe2);
                            if (clothingData2 == null) {
                                continue;
                            }
                            clothingData2.Texture ??= ContentManager.Get<Texture2D>(clothingData2._textureName);
                            Color fabricColor2 = clothingData2.GetColor(this, clothe2);
                            texturedBatch2D = m_primitivesRenderer.TexturedBatch(
                                clothingData2.Texture,
                                false,
                                num++,
                                DepthStencilState.None,
                                null,
                                BlendState.NonPremultiplied,
                                SamplerState.PointClamp
                            );
                            if (clothingData2.IsOuter) {
                                texturedBatch2D.QueueQuad(
                                    new Vector2(0f, 0f),
                                    new Vector2(m_outerClothedTexture.Width, m_outerClothedTexture.Height),
                                    0f,
                                    Vector2.Zero,
                                    Vector2.One,
                                    fabricColor2
                                );
                            }
                        }
                    }
                    m_primitivesRenderer.Flush();
                }
                finally {
                    Display.RenderTarget = renderTarget;
                    Display.ScissorRectangle = scissorRectangle;
                }
            }
        }
    }
}