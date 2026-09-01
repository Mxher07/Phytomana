using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class Block {
        public int BlockIndex;

        public string DefaultDisplayName = string.Empty;

        public string DefaultDescription = string.Empty;

        public string DefaultCategory = string.Empty;

        public int DisplayOrder;

        public Vector3 DefaultIconBlockOffset = Vector3.Zero;

        public Vector3 DefaultIconViewOffset = new(1f);

        public float DefaultIconViewScale = 1f;

        public float FirstPersonScale = 1f;

        public Vector3 FirstPersonOffset = Vector3.Zero;

        [Obsolete("overrode \"IsIndexDynamic\" instead.")]
        public bool StaticBlockIndex = false;
#pragma warning disable CS0618
        public virtual bool IsIndexDynamic => !StaticBlockIndex;
#pragma warning restore CS0618
        public bool CanBeBuiltIntoFurniture = false;

        public bool IsCollapsable = false;
        public virtual Vector3 GetFirstPersonOffset(int value) => FirstPersonOffset;

        public Vector3 FirstPersonRotation = Vector3.Zero;
        public virtual Vector3 GetFirstPersonRotation(int value) => FirstPersonRotation;
        public float InHandScale = 1f;
        public virtual float GetInHandScale(int value) => InHandScale;
        public Vector3 InHandOffset = Vector3.Zero;
        public virtual Vector3 GetInHandOffset(int value) => InHandOffset;
        public Vector3 InHandRotation = Vector3.Zero;

        public virtual Vector3 GetInHandRotation(int value) => InHandRotation;
        public string Behaviors = string.Empty;

        public string CraftingId = string.Empty;

        public int DefaultCreativeData;

        public bool IsCollidable = true;

        public bool IsPlaceable = true;

        public bool IsDiggingTransparent;

        public bool IsPlacementTransparent;

        public bool DefaultIsInteractive;

        public bool DefaultIsUseable;

        public bool IsEditable;

        public bool IsNonDuplicable;

        public bool IsGatherable;

        public bool HasCollisionBehavior;

        public bool KillsWhenStuck;

        public bool IsFluidBlocker = true;

        public bool IsTransparent;

        public bool GenerateFacesForSameNeighbors;

        public int DefaultShadowStrength;

        public int LightAttenuation;

        public int DefaultEmittedLightAmount;

        public float ObjectShadowStrength;

        public int DefaultDropContent;

        public float DefaultDropCount = 1f;

        public float DefaultExperienceCount;

        public int RequiredToolLevel;

        public int MaxStacking = 40;

        public float SleepSuitability;

        public float FrictionFactor = 1f;

        public float Density = 4f;

        public bool NoAutoJump;

        public bool NoSmoothRise;

        public int DefaultTextureSlot;

        public float DestructionDebrisScale = 1f;

        public float FuelHeatLevel;

        public float FuelFireDuration;

        public string DefaultSoundMaterialName;

        public float ShovelPower = 1f;

        public float QuarryPower = 1f;

        public float HackPower = 1f;

        public float DefaultMeleePower = 1f;

        public float DefaultMeleeHitProbability = 0.66f;

        public float DefaultProjectilePower = 1f;

        public int ToolLevel;

        public int PlayerLevelRequired = 1;

        public int Durability = -1;

        public BlockDigMethod DigMethod;

        public float DigResilience = 1f;

        public float ProjectileResilience = 1f;

        public bool IsAimable;

        public bool IsStickable;

        public bool AlignToVelocity;

        public float ProjectileSpeed = 15f;

        public float ProjectileDamping = 0.8f;

        public float ProjectileTipOffset;

        public bool DisintegratesOnHit;

        public float ProjectileStickProbability;

        public float DefaultHeat;

        public float FireDuration;

        public float ExplosionResilience;

        public float DefaultExplosionPressure;

        public bool DefaultExplosionIncendiary;

        public bool ExplosionKeepsPickables;

        public float DefaultNutritionalValue;

        public FoodType FoodType;

        public int DefaultRotPeriod;

        public float DefaultSicknessProbability;

        public bool? DefaultIsNonAttachable = null;

        public int PriorityUse = 3000;
        public int PriorityInteract = 2000;
        public int PriorityPlace = 1000;

        public Random Random = new();

        public static BoundingBox[] m_defaultCollisionBoxes = [new(Vector3.Zero, Vector3.One)];
        public virtual float GetDensity(int value) => Density;

        public virtual float GetFirstPersonScale(int value) => FirstPersonScale;

        public virtual void Initialize() {
            if (Durability < -1
                || Durability > 65535) {
                throw new InvalidOperationException(string.Format(LanguageControl.Get(GetType().Name, 1), DefaultDisplayName));
            }
        }

        public virtual TerrainVertex SetDiggingCrackingTextureTransform(TerrainVertex vertex) {
            byte b = (byte)((vertex.Color.R + vertex.Color.G + vertex.Color.B) / 3);
            vertex.Tx = (short)(vertex.Tx * 16f);
            vertex.Ty = (short)(vertex.Ty * 16f);
            vertex.Color = new Color(b, b, b, (byte)128);
            return vertex;
        }

        public virtual Texture2D GetDiggingCrackingTexture(ComponentMiner miner, float digProgress, int value, Texture2D[] defaultCrackTextures) {
            int num2 = Math.Clamp((int)(digProgress * 8f), 0, 7);
            return defaultCrackTextures[num2];
        }

        public virtual bool GetIsDiggingTransparent(int value) => IsDiggingTransparent;

        public virtual float GetObjectShadowStrength(int value) => ObjectShadowStrength;

        public virtual float GetFuelHeatLevel(int value) => FuelHeatLevel;

        public virtual float GetExplosionResilience(int value) => ExplosionResilience;

        public virtual float GetExplosionPressure(int value) => DefaultExplosionPressure;

        public virtual int GetMaxStacking(int value) => MaxStacking;

        public virtual float GetFuelFireDuration(int value) => FuelFireDuration;

        public virtual float GetProjectileResilience(int value) => ProjectileResilience;
        public virtual float GetFireDuration(int value) => FireDuration;

        public virtual float GetProjectileStickProbability(int value) => ProjectileStickProbability;

        public virtual bool MatchCrafingId(string CraftId) => CraftId == CraftingId;

        public virtual int GetPlayerLevelRequired(int value) => PlayerLevelRequired;

        public virtual bool HasCollisionBehavior_(int value) => HasCollisionBehavior;

        public virtual string GetDisplayName(SubsystemTerrain subsystemTerrain, int value) {
            int data = Terrain.ExtractData(value);
            string bn = $"{GetType().Name}:{data}";
            if (LanguageControl.TryGetBlock(bn, "DisplayName", out string result)) {
                return result;
            }
            return DefaultDisplayName;
        }

        /// <summary>
        ///     设置材质(正方形)单行格子(分割后每个材质)数,对放置后的方块无效
        /// </summary>
        /// <param name="value">材质(正方形)单行格子(分割后每个材质)数</param>
        public virtual int GetTextureSlotCount(int value) => 16;

        public virtual bool IsEditable_(int value) => IsEditable;

        public virtual bool IsAimable_(int value) => IsAimable;

        public virtual bool Eat(ComponentVitalStats vitalStats, int value) => false;

        public virtual bool CanWear(int value) => false;

        public virtual ClothingData GetClothingData(int value) => null;

        public virtual int GetToolLevel(int value) => ToolLevel;

        public virtual bool IsCollidable_(int value) => IsCollidable;
        /// <summary>
        /// 用于判断身体组件与某特定方块是否可发生碰撞
        /// </summary>
        /// <param name="componentBody"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool GetIsCollidable(ComponentBody componentBody, int value) => IsCollidable_(value);

        public virtual bool IsTransparent_(int value) => IsTransparent;

        public virtual bool GenerateFacesForSameNeighbors_(int value) => GenerateFacesForSameNeighbors;

        public virtual bool IsFluidBlocker_(int value) => IsFluidBlocker;

        public virtual bool IsGatherable_(int value) => IsGatherable;

        public virtual bool IsNonDuplicable_(int value) => IsNonDuplicable;

        public virtual bool IsPlaceable_(int value) => IsPlaceable;

        public virtual bool IsUseable_(int value) => DefaultIsUseable;

        public virtual bool IsPlacementTransparent_(int value) => IsPlacementTransparent;

        public virtual bool IsStickable_(int value) => IsStickable;

        public virtual float GetProjectileSpeed(int value) => ProjectileSpeed;

        public virtual float GetProjectileDamping(int value) => ProjectileDamping;

        public virtual string GetDescription(int value) {
            int data = Terrain.ExtractData(value);
            string bn = $"{GetType().Name}:{data}";
            if (LanguageControl.TryGetBlock(bn, "Description", out string r)) {
                return r;
            }
            return DefaultDescription;
        }

        public virtual FoodType GetFoodType(int value) => FoodType;

        public virtual string GetCategory(int value) => DefaultCategory;

        public virtual float GetDigResilience(int value) => DigResilience;

        public virtual BlockDigMethod GetBlockDigMethod(int value) => DigMethod;

        public virtual float GetShovelPower(int value) => ShovelPower;

        public virtual float GetQuarryPower(int value) => QuarryPower;

        public virtual float GetHackPower(int value) => HackPower;

        public virtual IEnumerable<int> GetCreativeValues() {
            if (DefaultCreativeData >= 0) {
                yield return Terrain.ReplaceContents(Terrain.ReplaceData(0, DefaultCreativeData), BlockIndex);
            }
        }

        public virtual bool GetAlignToVelocity(int value) => AlignToVelocity;

        public virtual bool IsInteractive(SubsystemTerrain subsystemTerrain, int value) => DefaultIsInteractive;

        public virtual IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes() {
            yield break;
        }

        public virtual CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain subsystemTerrain,
            string[] ingredients,
            float heatLevel,
            float playerLevel) => null;

        public virtual bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value) => IsTransparent;

        public virtual bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain,
            int face,
            int value,
            int neighborValue,
            int x,
            int y,
            int z) {
            int num = Terrain.ExtractContents(neighborValue);
            return BlocksManager.Blocks[num].IsFaceTransparent(subsystemTerrain, CellFace.OppositeFace(face), neighborValue);
        }

        public virtual int GetShadowStrength(int value) => DefaultShadowStrength;

        public virtual int GetFaceTextureSlot(int face, int value) => DefaultTextureSlot;

        public virtual string GetSoundMaterialName(SubsystemTerrain subsystemTerrain, int value) => DefaultSoundMaterialName;

        /// <summary>
        ///     生成地形顶点(用于绘制放置的方块)
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="geometry"></param>
        /// <param name="value"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public abstract void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z);

        public virtual void GenerateTerrainVertices(BlockGeometryGenerator generator,
            TerrainGeometrySubset geometry,
            int value,
            int x,
            int y,
            int z) { }

        /// <summary>
        ///     绘制方块_用于绘制方块物品形态
        /// </summary>
        /// <param name="primitivesRenderer"></param>
        /// <param name="value"></param>
        /// <param name="color"></param>
        /// <param name="size"></param>
        /// <param name="matrix"></param>
        /// <param name="environmentData"></param>
        public abstract void DrawBlock(PrimitivesRenderer3D primitivesRenderer,
            int value,
            Color color,
            float size,
            ref Matrix matrix,
            DrawBlockEnvironmentData environmentData);

        /// <summary>
        ///     方块放置方向
        /// </summary>
        public virtual BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            TerrainRaycastResult raycastResult) {
            BlockPlacementData result = default;
            result.Value = value;
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public virtual string GetCraftingId(int value) => CraftingId;

        public virtual int GetDisplayOrder(int value) => DisplayOrder;

        public virtual BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain,
            ComponentMiner componentMiner,
            int value,
            int toolValue,
            TerrainRaycastResult raycastResult) {
            BlockPlacementData result = default;
            result.Value = 0;
            result.CellFace = raycastResult.CellFace;
            return result;
        }

        public virtual float GetRequiredToolLevel(int value) => RequiredToolLevel;

        public virtual void GetDropValues(SubsystemTerrain subsystemTerrain,
            int oldValue,
            int newValue,
            int toolLevel,
            List<BlockDropValue> dropValues,
            out bool showDebris) {
            showDebris = DestructionDebrisScale > 0f;
            if (toolLevel < RequiredToolLevel) {
                return;
            }
            BlockDropValue item;
            if (DefaultDropContent != 0) {
                int num = (int)DefaultDropCount;
                if (Random.Bool(DefaultDropCount - num)) {
                    num++;
                }
                for (int i = 0; i < num; i++) {
                    item = new BlockDropValue { Value = Terrain.MakeBlockValue(DefaultDropContent), Count = 1 };
                    dropValues.Add(item);
                }
            }
            int num2 = (int)DefaultExperienceCount;
            if (Random.Bool(DefaultExperienceCount - num2)) {
                num2++;
            }
            for (int j = 0; j < num2; j++) {
                item = new BlockDropValue { Value = Terrain.MakeBlockValue(248), Count = 1 };
                dropValues.Add(item);
            }
        }

        public virtual int GetDamage(int value) => (Terrain.ExtractData(value) >> 4) & 0xFFF;

        public virtual int SetDamage(int value, int damage) {
            int num = Terrain.ExtractData(value);
            num &= 0xF;
            num |= Math.Clamp(damage, 0, 4095) << 4;
            return Terrain.ReplaceData(value, num);
        }

        public virtual int GetDamageDestructionValue(int value) => 0;

        public virtual int GetRotPeriod(int value) => DefaultRotPeriod;

        public virtual float GetSicknessProbability(int value) => DefaultSicknessProbability;

        public virtual float GetMeleePower(int value) => DefaultMeleePower;

        public virtual float GetMeleeHitProbability(int value) => DefaultMeleeHitProbability;

        public virtual float GetProjectilePower(int value) => DefaultProjectilePower;

        public virtual float GetHeat(int value) => DefaultHeat;

        public virtual float GetBlockHealth(int value) {
            int dur = GetDurability(value);
            int dag = GetDamage(value);
            if (dur > 0) {
                return (dur - dag) / (float)dur;
            }
            return -1f;
        }

        public virtual int GetDurability(int value) => Durability;

        public virtual bool GetExplosionIncendiary(int value) => DefaultExplosionIncendiary;

        public virtual Vector3 GetIconBlockOffset(int value, DrawBlockEnvironmentData environmentData) => DefaultIconBlockOffset;

        public virtual Vector3 GetIconViewOffset(int value, DrawBlockEnvironmentData environmentData) => DefaultIconViewOffset;

        public virtual float GetIconViewScale(int value, DrawBlockEnvironmentData environmentData) => DefaultIconViewScale;

        public virtual BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain,
            Vector3 position,
            int value,
            float strength) => new(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, GetFaceTextureSlot(4, value));

        public virtual BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value) => m_defaultCollisionBoxes;

        public virtual BoundingBox[] GetCustomInteractionBoxes(SubsystemTerrain terrain, int value) => GetCustomCollisionBoxes(terrain, value);

        public virtual int GetEmittedLightAmount(int value) => DefaultEmittedLightAmount;

        public virtual float GetNutritionalValue(int value) => DefaultNutritionalValue;

        public virtual bool ShouldAvoid(int value) => false;

        public virtual bool ShouldAvoid(int value, ComponentPilot componentPilot) => ShouldAvoid(value);

        public virtual bool IsSwapAnimationNeeded(int oldValue, int newValue) => true;

        public virtual bool IsHeatBlocker(int value) => IsCollidable_(value);

        public virtual float? Raycast(Ray3 ray,
            SubsystemTerrain subsystemTerrain,
            int value,
            bool useInteractionBoxes,
            out int nearestBoxIndex,
            out BoundingBox nearestBox) {
            float? result = null;
            nearestBoxIndex = 0;
            nearestBox = default;
            BoundingBox[] array = useInteractionBoxes
                ? GetCustomInteractionBoxes(subsystemTerrain, value)
                : GetCustomCollisionBoxes(subsystemTerrain, value);
            for (int i = 0; i < array.Length; i++) {
                float? num = ray.Intersection(array[i]);
                if (num.HasValue
                    && (!result.HasValue || num.Value < result.Value)) {
                    nearestBoxIndex = i;
                    result = num;
                }
            }
            nearestBox = array[nearestBoxIndex];
            return result;
        }

        public virtual bool GetIsCollapsable(int value) => IsCollapsable;

        public virtual bool IsCollapseSupportBlock(SubsystemTerrain subsystemTerrain, int value) =>
            !IsFaceNonAttachable(subsystemTerrain, 4, value, 0);

        public virtual bool IsCollapseDestructibleBlock(int value) => true;

        public virtual bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd) {
            isEnd = false;
            if (IsNonDuplicable_(value)) {
                return false;
            }
            if (IsCollidable_(value)) {
                return true;
            }
            return false;
        }

        public virtual bool IsBlockingPiston(int value) => IsCollidable_(value);

        public virtual bool IsSuitableForPlants(int value, int plantValue) => false;

        public virtual bool IsNonAttachable(int value) {
            if (DefaultIsNonAttachable.HasValue) {
                return DefaultIsNonAttachable.Value;
            }
            return IsTransparent_(value);
        }

        public virtual bool IsFaceNonAttachable(SubsystemTerrain subsystemTerrain, int face, int value, int attachBlockValue) {
            if (!IsCollidable_(value)
                || IsNonAttachable(value)) {
                return true;
            }
            return false;
        }

        public virtual bool ShouldBeAddedToProject(SubsystemBlocksManager subsystemBlocksManager) => true;

        public virtual bool CanBlockBeBuiltIntoFurniture(int value) => CanBeBuiltIntoFurniture;

        public virtual int GetPriorityUse(int value, ComponentMiner componentMiner) => IsUseable_(value) ? PriorityUse : 0;

        public virtual int GetPriorityInteract(int value, ComponentMiner componentMiner) {
            if (componentMiner.m_subsystemTerrain != null
                && IsInteractive(componentMiner.m_subsystemTerrain, value)) {
                return PriorityInteract;
            }
            return 0;
        }

        public virtual int GetPriorityPlace(int value, ComponentMiner componentMiner) {
            if (!IsPlaceable_(value)) {
                return 0;
            }
            return PriorityPlace;
        }

        public virtual bool CanBeFiredByDispenser(int value) => true;

        public virtual RecipaediaDescriptionScreen GetBlockDescriptionScreen(int value) => RecipaediaDescriptionScreen.Default;

        public virtual RecipaediaRecipesScreen GetBlockRecipeScreen(int value) => RecipaediaRecipesScreen.Default;

        /// <summary>
        /// 获取默认纹理<br />
        /// 如果返回 null，将使用 SubsystemAnimatedTextures.AnimatedBlocksTexture
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Texture2D GetDefaultTexture(int value) => null;

        /// <summary>
        /// 获取此方块在家具中的渲染类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual GeometrySubsetType GetFurnitureSubsetType(int value) => GeometrySubsetType.Opaque;

        /// <summary>
        /// 获取此方块在家具中的颜色
        /// </summary>
        /// <param name="value"></param>
        /// <param name="subsystemPalette"></param>
        /// <returns></returns>
        public virtual Color GetFurnitureColor(int value, SubsystemPalette subsystemPalette) {
            if (this is IPaintableBlock paintableBlock
                && subsystemPalette != null) {
                int? paintColor = paintableBlock.GetPaintColor(value);
                if (paintColor.HasValue) {
                    return subsystemPalette.GetColor(paintColor.Value);
                }
            }
            return Color.White;
        }

        /// <summary>
        /// 家具中此方块是否为发光体素（如火把、灯）。<br />
        /// 返回 true 时强制使用发光纹理槽 31。
        /// </summary>
        public virtual bool IsFurnitureEmissive(int value) => false;
    }
}