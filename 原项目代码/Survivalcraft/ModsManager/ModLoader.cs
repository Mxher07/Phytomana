using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public abstract class ModLoader {
        public ModEntity Entity;

        /// <summary>
        /// 同一个模组有多个 ModLoader 时，该值越小，越先执行；最小的将成为主要 ModLoader，部分钩子只有主要 ModLoader 的会执行
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        ///     当ModLoader类被实例化时执行
        /// </summary>
        public virtual void __ModInitialize() { }

        /// <summary>
        ///     在加载本模组的资源时触发。<br/>
        ///     只有模组中的主要 ModLoader 的该方法才会被执行。<br/>
        ///     注意：模组的 dll 只能由原版逻辑加载
        /// </summary>
        /// <param name="extension">准备调用的文件的扩展名</param>
        /// <param name="action">执行的操作</param>
        /// <param name="skip">跳过SC本体对模组获取文件的执行。由于该方法只会在调用你的模组的资源时触发，所以不必担心兼容性问题。</param>
        public virtual void GetModFiles(string extension, Action<string, Stream> action, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     在系统读取本模组的文件时触发
        ///     注意：modinfo文件、mod图标只能由原版逻辑加载。如果需要修改调整，请自己在模组中重新写一遍加载逻辑
        /// </summary>
        /// <param name="filename">获取模组文件的名称或前缀</param>
        /// <param name="stream">文件流</param>
        /// <param name="skip">跳过SC本体对模组获取文件的执行。由于该方法只会在调用你的模组的资源时触发，所以不必担心兼容性问题。</param>
        /// <param name="fileFound">在skip过后，返回是否得到文件</param>
        public virtual void GetModFile(string filename, Action<Stream> stream, out bool skip, out bool fileFound) {
            skip = false;
            fileFound = false;
        }

        /// <summary>
        ///     在加载存档前，比较当前模组的版本和存档上次使用模组的版本<br/>
        ///     只有模组中的主要 ModLoader 的该方法才会被执行。<br/>
        ///     仅比较本模组
        /// </summary>
        /// <param name="currentVersion">当前模组的版本</param>
        /// <param name="lastVersion">存档上一次使用模组的版本</param>
        /// <returns>返回true表示当前版本模组能加载目标版本模组的存档，返回false则不加载</returns>
        public virtual bool CompareModVersion(string currentVersion, string lastVersion) => true;

        /// <summary>
        ///     Mod被卸载时执行
        /// </summary>
        public virtual void ModDispose() { }

        /// <summary>
        ///     视图雾颜色调整
        /// </summary>
        /// <param name="viewUnderWaterDepth">大于0则表示在水下</param>
        /// <param name="viewUnderMagmaDepth">大于0则表示在岩浆中</param>
        /// <param name="viewFogColor">视图雾颜色</param>
        public virtual void ViewFogColor(float viewUnderWaterDepth, float viewUnderMagmaDepth, ref Color viewFogColor) { }

        /// <summary>
        ///     方块亮度
        ///     （黑暗区域亮度）
        /// </summary>
        /// <param name="brightness">亮度值</param>
        public virtual void CalculateLighting(ref float brightness) { }

        /// <param name="miner"></param>
        /// <param name="componentBody"></param>
        /// <param name="hitPoint"></param>
        /// <param name="hitDirection"></param>
        /// <param name="attackPower">伤害值</param>
        /// <param name="playerProbability">玩家命中率</param>
        /// <param name="creatureProbability">生物命中率</param>
        /// <param name="hitted"></param>
        public virtual void OnMinerHit(ComponentMiner miner,
            ComponentBody componentBody,
            Vector3 hitPoint,
            Vector3 hitDirection,
            ref float attackPower,
            ref float playerProbability,
            ref float creatureProbability,
            out bool hitted) {
            hitted = false;
        }

        /// <summary>
        ///     设置伤害间隔
        /// </summary>
        public virtual void SetHitInterval(ComponentMiner miner, ref double hitInterval) { }

        /// <summary>
        ///     当前等级是否可以使用手中物品进行：挖掘，使用，伤害，瞄准
        ///     Mod开发者可以自定义物品是否符合使用条件
        /// </summary>
        /// <param name="miner"></param>
        /// <param name="toolValue">方块值</param>
        /// <param name="canUse">能使用</param>
        /// <param name="skip">跳过原版后，返回canUse值</param>
        public virtual void IsLevelSufficientForTool(ComponentMiner miner, int toolValue, ref bool canUse, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     在生物攻击时执行
        /// </summary>
        /// <param name="componentMiner">攻击者</param>
        /// <param name="componentBody">被攻击方的ComponentBody</param>
        /// <param name="hitPoint">命中点</param>
        /// <param name="hitDirection">命中时的击退方向</param>
        /// <param name="durabilityReduction">攻击后，手中武器掉的耐久量</param>
        /// <param name="attackment">发起的攻击</param>
        public virtual void OnMinerHit2(ComponentMiner componentMiner,
            ComponentBody componentBody,
            Vector3 hitPoint,
            Vector3 hitDirection,
            ref int durabilityReduction,
            ref Attackment attackment) { }

        /// <summary>
        ///     当人物挖掘时执行
        /// </summary>
        /// <param name="miner"></param>
        /// <param name="raycastResult"></param>
        /// <param name="digProgress"></param>
        /// <param name="digged"></param>
        /// <returns></returns>
        public virtual void OnMinerDig(ComponentMiner miner, TerrainRaycastResult raycastResult, ref float digProgress, out bool digged) {
            digged = false;
        }

        /// <summary>
        ///     在人物放置时执行，在OnMinerPlace之前
        /// </summary>
        /// <param name="componentMiner"></param>
        /// <param name="terrainRaycastResult"></param>
        /// <param name="x">放置方块的坐标x</param>
        /// <param name="y">放置方块的坐标y</param>
        /// <param name="z">放置方块的坐标z</param>
        /// <param name="placementData">包含放置方块的方块表面等信息</param>
        /// <param name="placementNotAllowed">返回true则玩家不能放置方块</param>
        public virtual void BeforeMinerPlace(ComponentMiner componentMiner,
            TerrainRaycastResult terrainRaycastResult,
            int x,
            int y,
            int z,
            BlockPlacementData placementData,
            out bool placementNotAllowed) {
            placementNotAllowed = false;
        }

        /// <summary>
        ///     当人物放置时执行，若Placed为true则不执行原放置操作
        /// </summary>
        /// <param name="miner"></param>
        /// <param name="raycastResult"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="value"></param>
        /// <param name="placed"></param>
        /// <returns></returns>
        [Obsolete("该方法已过时，请使用OnMinerPlace重载")]
        public virtual void OnMinerPlace(ComponentMiner miner,
            TerrainRaycastResult raycastResult,
            int x,
            int y,
            int z,
            int value,
            out bool placed) {
            placed = false;
        }

        /// <summary>
        ///     当人物放置时执行，若Placed为true则不执行原放置操作
        /// </summary>
        /// <param name="miner"></param>
        /// <param name="raycastResult"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="value"></param>
        /// <param name="placementData"></param>
        /// <param name="placed"></param>
        /// <returns></returns>
        public virtual void OnMinerPlace(ComponentMiner miner,
            TerrainRaycastResult raycastResult,
            int x,
            int y,
            int z,
            int value,
            BlockPlacementData placementData,
            out bool placed) {
            placed = false;
        }

        /// <summary>
        ///     设置雨和雪的颜色
        /// </summary>
        /// <param name="rainColor"></param>
        /// <param name="snowColor"></param>
        /// <returns></returns>
        public virtual bool SetRainAndSnowColor(ref Color rainColor, ref Color snowColor) => false;

        /// <summary>
        ///     设置家具的颜色
        /// </summary>
        public virtual void SetFurnitureDesignColor(FurnitureDesign design, Block block, int value, ref int faceTextureSlot, ref Color color) { }

        /// <summary>
        ///     更改击退和晕眩效果
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="attacker">攻击者</param>
        /// <param name="hitPoint">伤害位置</param>
        /// <param name="hitDirection"></param>
        /// <param name="impulseFactor">击退效果</param>
        /// <param name="stunTimeFactor">眩晕时间</param>
        /// <param name="recalculate">是否重写眩晕？</param>
        [Obsolete("该方法已过时，请使用ProcessAttackment修改Attackment的击退和击晕属性")]
        public virtual void AttackPowerParameter(ComponentBody target,
            ComponentCreature attacker,
            Vector3 hitPoint,
            Vector3 hitDirection,
            ref float impulseFactor,
            ref float stunTimeFactor,
            ref bool recalculate) { }

        /// <summary>
        ///     当人物吃东西时执行
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="block"></param>
        /// <param name="slotIndex"></param>
        /// <param name="value"></param>
        /// <param name="count"></param>
        /// <returns>如果为 true：不移交到下一个 mod 处理</returns>
        [Obsolete("使用另一个ClothingProcessSlotItems接口")]
        public virtual bool ClothingProcessSlotItems(ComponentPlayer componentPlayer, Block block, int slotIndex, int value, int count) => false;

        /// <summary>
        ///     控制物品拖动到人物上方时，是否能执行操作。
        ///     返回1表示可以处理物品。
        ///     多个模组执行时，取最大值
        /// </summary>
        /// <param name="componentClothing"></param>
        /// <param name="slotIndex"></param>
        /// <param name="blockValue"></param>
        /// <param name="notToProcessVanilla"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public virtual void ClothingSlotProcessCapacity(ComponentClothing componentClothing,
            int slotIndex,
            int blockValue,
            ref bool notToProcessVanilla,
            out int capacity) {
            capacity = 0;
        }

        public virtual void SetClothes(ComponentClothing componentClothing, ClothingSlot slot, IEnumerable<int> clothes) { }

        /// <summary>
        ///     动物吃掉落物时执行
        /// </summary>
        public virtual void OnEatPickable(ComponentEatPickableBehavior eatPickableBehavior, Pickable eatPickable, out bool dealed) {
            dealed = false;
        }

        /// <summary>
        ///     人物出生时执行
        /// </summary>
        public virtual bool OnPlayerSpawned(PlayerData.SpawnMode spawnMode, ComponentPlayer componentPlayer, Vector3 position) => false;

        /// <summary>
        ///     当人物死亡时执行。在玩家进入世界且玩家处于死亡状态时也会执行
        ///     可以通过playerData.m_stateMachine.PreviousState == "Playing"，来判断是刚死的，还是加载世界的时候就已经死了。并规避由于退出重进世界造成的“反复死亡判断”bug
        /// </summary>
        /// <param name="playerData"></param>
        public virtual void OnPlayerDead(PlayerData playerData) { }

        /// <summary>
        ///     当Miner执行AttackBody方法时执行
        /// </summary>
        /// <param name="target"></param>
        /// <param name="attacker"></param>
        /// <param name="hitPoint"></param>
        /// <param name="hitDirection"></param>
        /// <param name="attackPower"></param>
        /// <param name="isMeleeAttack"></param>
        /// <returns>false移交到下一个Mod处理,true不移交</returns>
        [Obsolete("该方法已弃用，请使用ProcessAttackment", true)]
        public virtual bool AttackBody(ComponentBody target,
            ComponentCreature attacker,
            Vector3 hitPoint,
            Vector3 hitDirection,
            ref float attackPower,
            bool isMeleeAttack) => false;

        /// <summary>
        ///     在攻击时执行
        /// </summary>
        /// <param name="attackment"></param>
        public virtual void ProcessAttackment(Attackment attackment) { }

        /// <summary>
        ///     当模型对象进行模型设值时执行
        /// </summary>
        public virtual void OnSetModel(ComponentModel componentModel, Model model, out bool isSet) {
            isSet = false;
        }

        [Obsolete("Use OnAnimateModel() instead")]
        public virtual void OnModelAnimate(ComponentCreatureModel componentCreatureModel, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     当模型对象作出动画时执行
        ///     skip为是否跳过原动画代码
        /// </summary>
        public virtual void OnAnimateModel(ComponentModel componentModel, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     当模型对象绘制额外时执行，skip为是否跳过原本的绘制。
        ///     与OnModelRendererDrawExtra不同，本方法主要是人物第三人称模型手上物品的绘制
        /// </summary>
        /// <param name="componentModel"></param>
        /// <param name="camera"></param>
        /// <param name="skip"></param>
        public virtual void OnModelDrawExtra(ComponentModel componentModel, Camera camera, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     当模型计算骨骼绝对变换时执行，Skip为是否跳过原本的计算
        /// </summary>
        /// <param name="componentModel"></param>
        /// <param name="camera"></param>
        /// <param name="skip"></param>
        public virtual void OnModelCalculateBones(ComponentModel componentModel, Camera camera, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     当模型计算是否可见时执行，Skip为是否跳过原本的计算
        /// </summary>
        /// <param name="componentModel"></param>
        /// <param name="camera"></param>
        /// <param name="skip"></param>
        public virtual void OnModelCalculateIsVisible(ComponentModel componentModel, Camera camera, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     重新计算模型眼睛位置
        /// </summary>
        /// <param name="componentModel"></param>
        /// <param name="eyePosition"></param>
        public virtual void RecalculateModelEyePosition(ComponentModel componentModel, ref Vector3 eyePosition) { }

        /// <summary>
        ///     重新计算模型眼睛旋转
        /// </summary>
        /// <param name="componentModel"></param>
        /// <param name="eyeRotation"></param>
        public virtual void RecalculateModelEyeRotation(ComponentModel componentModel, ref Quaternion eyeRotation) { }

        /// <summary>
        ///     计算护甲免伤时执行
        /// </summary>
        /// <param name="componentClothing"></param>
        /// <param name="attackPower">未计算免伤前的伤害</param>
        /// <param name="appliedByOtherMods"></param>
        /// <param name="applied"></param>
        /// <returns>免伤后的伤害，当多个mod都有免伤计算时，取最小值</returns>
        [Obsolete("护甲系统调整，该接口不再适用。对于调整护甲的结算顺序，请调用DecideArmorProtectionSequence接口；对于调整单件护甲的防御效果，请给被调整的护甲创建新的ClothingData。")]
        public virtual float ApplyArmorProtection(ComponentClothing componentClothing, float attackPower, bool appliedByOtherMods, out bool applied) {
            applied = false;
            return attackPower;
        }

        /// <summary>
        ///     等级组件更新时执行，仅限玩家。参考OnFactorsUpdate接口使用说明。
        /// </summary>
        /// <param name="level"></param>
        [Obsolete("You can update your factors in your mod components")]
        public virtual void OnLevelUpdate(ComponentLevel level) { }

        /// <summary>
        ///     因素控制力量、抗性、速度、饥饿速率组件更新时执行
        /// </summary>
        /// <param name="componentFactors"></param>
        /// <param name="dt"></param>
        [Obsolete("You can update your factors in your mod components")]
        public virtual void OnFactorsUpdate(ComponentFactors componentFactors, float dt) { }

        /// <summary>
        ///     Gui组件帧更新时执行
        /// </summary>
        /// <param name="componentGui"></param>
        public virtual void GuiUpdate(ComponentGui componentGui) { }

        /// <summary>
        ///     Gui组件绘制时执行
        /// </summary>
        /// <param name="componentGui"></param>
        /// <param name="camera"></param>
        /// <param name="drawOrder"></param>
        public virtual void GuiDraw(ComponentGui componentGui, Camera camera, int drawOrder) { }

        /// <summary>
        ///     更新输入时执行
        /// </summary>
        /// <param name="componentInput"></param>
        /// <param name="widgetInput"></param>
        public virtual void UpdateInput(ComponentInput componentInput, WidgetInput widgetInput) { }

        /// <summary>
        ///     ViewWidget绘制屏幕时执行
        /// </summary>
        public virtual void DrawToScreen(ViewWidget viewWidget, Widget.DrawContext dc) { }

        /// <summary>
        ///     衣物背包界面被打开时执行
        /// </summary>
        /// <param name="componentGui"></param>
        /// <param name="clothingWidget"></param>
        public virtual void ClothingWidgetOpen(ComponentGui componentGui, ClothingWidget clothingWidget) { }

        /// <summary>
        ///     当方块被炸掉时执行
        /// </summary>
        public virtual void OnBlockExploded(SubsystemTerrain subsystemTerrain, int x, int y, int z, int value) { }

        /// <summary>
        ///     当实体被添加时执行
        /// </summary>
        public virtual void OnEntityAdd(Entity entity) { }

        /// <summary>
        ///     当实体被移除时执行
        /// </summary>
        public virtual void OnEntityRemove(Entity entity) { }

        /// <summary>
        ///     自然生成生物列表初始化时执行
        /// </summary>
        /// <param name="spawn"></param>
        /// <param name="creatureTypes"></param>
        public virtual void InitializeCreatureTypes(SubsystemCreatureSpawn spawn, List<SubsystemCreatureSpawn.CreatureType> creatureTypes) { }

        /// <summary>
        ///     生物出生时执行
        /// </summary>
        /// <param name="spawn"></param>
        /// <param name="entity"></param>
        /// <param name="spawnEntityData"></param>
        /// <param name="spawned"></param>
        public virtual void SpawnEntity(SubsystemSpawn spawn, Entity entity, SpawnEntityData spawnEntityData, out bool spawned) {
            spawned = false;
        }

        /// <summary>
        ///     当生物消失时执行
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="componentSpawn"></param>
        public virtual void OnDespawned(Entity entity, ComponentSpawn componentSpawn) { }

        public virtual void CalculateExplosionPower(SubsystemExplosions subsystemExplosions, ref float explosionPower) { }

        /// <summary>
        ///     生物等实体在遭受爆炸时执行
        /// </summary>
        /// <param name="componentBody">遭受爆炸的实体Body</param>
        /// <param name="explosionInjury">该爆炸的Injury</param>
        /// <param name="impulse">爆炸的击退力</param>
        /// <param name="setOnFire">爆炸是否会让实体着火</param>
        /// <param name="fluctuation">爆炸的击退、伤害浮动系数。目前只对非生物的船等实体有效</param>
        public virtual void OnComponentBodyExploded(ComponentBody componentBody,
            ref Injury explosionInjury,
            ref Vector3 impulse,
            ref bool setOnFire,
            ref float fluctuation) { }

        /// <summary>
        ///     死亡前瞬间执行
        /// </summary>
        public virtual void DeadBeforeDrops(ComponentHealth componentHealth, ref KillParticleSystem killParticleSystem, ref bool dropAllItems) { }

        /// <summary>
        ///     重定义方块更改方法，Skip为true则不执行原ChangeCell代码
        /// </summary>
        public virtual void TerrainChangeCell(SubsystemTerrain subsystemTerrain, int x, int y, int z, int value, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     重定义生物受伤方法，Skip为true则不执行原Injure代码
        /// </summary>
        [Obsolete("该方法已被弃用，请使用CalculateCreatureInjuryAmount, OnCreatureDying, OnCreatureDied代替", true)]
        public virtual void OnCreatureInjure(ComponentHealth componentHealth,
            float amount,
            ComponentCreature attacker,
            bool ignoreInvulnerability,
            string cause,
            out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     计算生物收到伤害的量
        /// </summary>
        public virtual void CalculateCreatureInjuryAmount(Injury injury) { }

        /// <summary>
        ///     如果动物受到Injure且生命值小于0时，执行操作。
        ///     如果在函数执行完毕后Health > 0，则取消死亡判定。
        ///     通常用于各种模组的“不死图腾”机制
        /// </summary>
        /// <param name="componentHealth"></param>
        /// <param name="injury"></param>
        public virtual void OnCreatureDying(ComponentHealth componentHealth, Injury injury) { }

        /// <summary>
        ///     在动物收到Injure()且生命值低于0时，执行操作。
        /// </summary>
        /// <param name="componentHealth"></param>
        /// <param name="injury"></param>
        /// <param name="experienceOrbDrop"></param>
        /// <param name="calculateInKill"></param>
        public virtual void OnCreatureDied(ComponentHealth componentHealth, Injury injury, ref int experienceOrbDrop, ref bool calculateInKill) { }

        /// <summary>
        ///     每帧更新的时候，调整血量带来的视觉效果
        /// </summary>
        /// <param name="componentHealth"></param>
        /// <param name="lastHealth">在扣血之前的生命值</param>
        /// <param name="redScreenFactor">玩家的红屏效果</param>
        /// <param name="playPainSound">是否播放受伤音效</param>
        /// <param name="healthBarFlashCount">玩家血条闪烁次数</param>
        /// <param name="creatureModelRedFactor">生物模型变红，为0时不变红，为1时完全红色</param>
        public virtual void ChangeVisualEffectOnInjury(ComponentHealth componentHealth,
            float lastHealth,
            ref float redScreenFactor,
            ref bool playPainSound,
            ref int healthBarFlashCount,
            ref float creatureModelRedFactor) { }

        public virtual void OnCreatureSpiked(ComponentHealth componentHealth,
            SubsystemBlockBehavior spikeBlockBehavior,
            CellFace cellFace,
            float velocity,
            ref Injury blockInjury) { }

        /// <summary>
        ///     更改天空颜色
        /// </summary>
        public virtual Color ChangeSkyColor(Color oldColor, Vector3 direction, float timeOfDay, int temperature) => oldColor;

        /// <summary>
        ///     设置着色器参数
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="camera"></param>
        public virtual void SetShaderParameter(Shader shader, Camera camera) { }

        /// <summary>
        ///     更改模型着色器参数的值
        /// </summary>
        public virtual void ModelShaderParameter(Shader shader,
            Camera camera,
            List<SubsystemModelsRenderer.ModelData> modelsData,
            float? alphaThreshold) { }

        /// <summary>
        ///     天空额外绘制
        /// </summary>
        public virtual void SkyDrawExtra(SubsystemSky subsystemSky, Camera camera) { }

        /// <summary>
        ///     设置生物最大组件数，多个Mod时取最大
        /// </summary>
        /// <returns></returns>
        public virtual int GetMaxInstancesCount() => 7;

        /// <summary>
        ///     绘制额外模型数据的方法，如人物头顶的名字
        /// </summary>
        /// <param name="modelsRenderer"></param>
        /// <param name="modelData">正在绘制的模型</param>
        /// <param name="camera"></param>
        /// <param name="alphaThreshold"></param>
        public virtual void OnModelRendererDrawExtra(SubsystemModelsRenderer modelsRenderer,
            SubsystemModelsRenderer.ModelData modelData,
            Camera camera,
            float? alphaThreshold) { }

        /// <summary>
        ///     设定伤害粒子参数
        /// </summary>
        /// <param name="hitValueParticleSystem">粒子</param>
        /// <param name="attackment">产生该攻击粒子的攻击，为null表示攻击没有命中</param>
        public virtual void SetHitValueParticleSystem(HitValueParticleSystem hitValueParticleSystem, Attackment attackment) { }

        /// <summary>
        ///     当储存生物数据时
        /// </summary>
        /// <param name="spawn"></param>
        /// <param name="spawnEntityData"></param>
        public virtual void OnSaveSpawnData(ComponentSpawn spawn, SpawnEntityData spawnEntityData) { }

        /// <summary>
        ///     当读取生物数据时
        /// </summary>
        public virtual void OnReadSpawnData(Entity entity, SpawnEntityData spawnEntityData) { }

        /// <summary>
        ///     区块地形生成时
        ///     注意此方法运行在子线程中
        /// </summary>
        /// <param name="chunk"></param>
        public virtual void OnTerrainContentsGenerated(TerrainChunk chunk) { }

        /// <summary>
        ///     当区块即将被释放时
        ///     KeepWorking为True时该区块会继续保持运作，不被释放
        /// </summary>
        public virtual void ToFreeChunks(TerrainUpdater terrainUpdater, TerrainChunk chunk, out bool keepWorking) {
            keepWorking = false;
        }

        /// <summary>
        ///     加载指定区块,如有区块数变动返回 true，否则返回 false
        /// </summary>
        // <param name="chunk"></param>
        public virtual bool ToAllocateChunks(TerrainUpdater terrainUpdater, TerrainUpdater.UpdateLocation[] locations) => false;

        /// <summary>
        ///     子系统帧更新时执行
        /// </summary>
        [Obsolete("Use SubsystemUpdate(SubsystemUpdate subsystemUpdate, float dt)")]
        public virtual void SubsystemUpdate(float dt) { }
        public virtual void SubsystemUpdate(SubsystemUpdate subsystemUpdate, float dt) { }

        /// <summary>
        ///     当Project被加载时执行
        /// </summary>
        /// <param name="project"></param>
        public virtual void OnProjectLoaded(Project project) { }

        /// <summary>
        ///     当Project被释放时执行
        /// </summary>
        public virtual void OnProjectDisposed() { }

        /// <summary>
        ///     方块初始化完成时执行
        /// </summary>
        public virtual void BlocksInitalized() { }

        /// <summary>
        ///     存档开始加载前执行
        /// </summary>
        /// <param name="playScreen">世界选择界面</param>
        /// <param name="item">类型为WorldInfo，存储即将进入的存档信息</param>
        /// <returns>返回类型应当保证为WorldInfo，返回null可以阻止玩家进入存档</returns>
        public virtual object BeforeGameLoading(PlayScreen playScreen, object item) => item;

        /// <summary>
        ///     加载任务开始时执行
        ///     在BlocksManager初始化之前
        /// </summary>
        public virtual void OnLoadingStart(List<Action> actions) { }

        /// <summary>
        ///     加载任务结束时执行
        ///     在BlocksManager初始化之后
        /// </summary>
        /// <param name="actions"></param>
        public virtual void OnLoadingFinished(List<Action> actions) { }

        /// <summary>
        ///     模组设置保存时执行
        /// </summary>
        /// <param name="xElement">一个空容器，模组作者需将自己的设置信息存入这个空容器内</param>
        public virtual void SaveSettings(XElement xElement) { }

        /// <summary>
        ///     模组设置加载时执行
        ///     模组作者有必要在读取模组设置后将设置数据放入自己的类或结构中
        ///     API在运行时不会保存着模组设置
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void LoadSettings(XElement xElement) { }

        /// <summary>
        ///     Xdb文件加载时执行
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void OnXdbLoad(XElement xElement) { }

        /// <summary>
        ///     Project.xml加载时执行
        /// </summary>
        /// <param name="xElement"></param>
        [Obsolete("Use ProjectXmlLoad(XElement xElement, WorldInfo worldInfo, ContainerWidget gameWidget)")]
        public virtual void ProjectXmlLoad(XElement xElement) { }
        public virtual void ProjectXmlLoad(XElement xElement, WorldInfo worldInfo, ContainerWidget gameWidget) { }

        /// <summary>
        ///     Project.xml保存时执行
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void ProjectXmlSave(XElement xElement) { }

        /// <summary>
        ///     Project.xml在所有的Entity和Subsystem保存完毕后再执行
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void OnProjectXmlSaved(XElement xElement) { }

        /// <summary>
        ///     配方解码时执行
        /// </summary>
        /// <param name="m_recipes"></param>
        /// <param name="element">配方的Xelement</param>
        /// <param name="decoded">是否解码成功，不成功交由下一个Mod处理</param>
        public virtual void OnCraftingRecipeDecode(List<CraftingRecipe> m_recipes, XElement element, out bool decoded) {
            decoded = false;
        }

        /// <summary>
        ///     配方匹配时执行
        /// </summary>
        /// <param name="requiredIngredients"></param>
        /// <param name="actualIngredient"></param>
        /// <param name="matched">是否匹配成功，不成功交由下一个Mod处理</param>
        public virtual bool MatchRecipe(string[] requiredIngredients, string[] actualIngredient, out bool matched) {
            matched = false;
            return false;
        }

        /// <summary>
        ///     获得解码结果时执行
        /// </summary>
        /// <param name="result">结果字符串</param>
        /// <param name="decoded">是否解码成功，不成功交由下一个Mod处理</param>
        /// <returns></returns>
        public virtual int DecodeResult(string result, out bool decoded) {
            decoded = false;
            return 0;
        }

        /// <summary>
        ///     解码配方
        /// </summary>
        /// <param name="ingredient"></param>
        /// <param name="craftingId"></param>
        /// <param name="data"></param>
        /// <param name="decoded">是否解码成功，不成功交由下一个Mod处理</param>
        public virtual void DecodeIngredient(string ingredient, out string craftingId, out int? data, out bool decoded) {
            decoded = false;
            craftingId = string.Empty;
            data = null;
        }

        /// <summary>
        ///     改变相机模式时执行
        /// </summary>
        /// <param name="m_componentPlayer"></param>
        /// <param name="componentGui"></param>
        public virtual void OnCameraChange(ComponentPlayer m_componentPlayer, ComponentGui componentGui) { }

        /// <summary>
        ///     当GameWidget被创建时执行，适合添加新的摄像机类型。具体方法可参考SurvivalCraftModLoader.ManageCameras
        /// </summary>
        /// <param name="gameWidget"></param>
        public virtual void ManageCameras(GameWidget gameWidget) { }

        /// <summary>
        ///     屏幕截图时执行
        /// </summary>
        public virtual void OnCapture() { }

        /// <summary>
        ///     更改主页背景音乐
        /// </summary>
        public virtual void MenuPlayMusic(out string contentMusicPath) {
            contentMusicPath = string.Empty;
        }

        /// <summary>
        ///     ComponentPilot组件接口
        ///     用于判断是否地形是否安全
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="isTerrainSafeToGo">前往地形是安全的</param>
        /// <param name="skipVanilla">是否跳过原版逻辑</param>
        /// <param name="componentPilot"></param>
        /// <param name="position"></param>
        public virtual void IsTerrainSafeToGo(ComponentPilot componentPilot,
            Vector3 position,
            Vector3 direction,
            out bool isTerrainSafeToGo,
            out bool skipVanilla) {
            isTerrainSafeToGo = false;
            skipVanilla = false;
        }

        /// <summary>
        ///     ComponentFlyAwayBehavior组件接口
        ///     用于判断是否将目标规划为捕食者，从而飞走
        /// </summary>
        /// <param name="flyAwayBehavior"></param>
        /// <param name="target"></param>
        /// <param name="isPredator">目标是否为捕食者</param>
        /// <param name="skipVanilla">是否跳过原版逻辑</param>
        public virtual void IsPredator(ComponentFlyAwayBehavior flyAwayBehavior, Entity target, out bool isPredator, out bool skipVanilla) {
            isPredator = false;
            skipVanilla = false;
        }

        /// <summary>
        ///     摇人行为。
        ///     仅建议对自己模组的生物使用，不要干涉其他模组生物的行为。
        /// </summary>
        /// <param name="herdBehavior"></param>
        /// <param name="target"></param>
        /// <param name="maxRange"></param>
        /// <param name="maxChaseTime"></param>
        /// <param name="isPersistent"></param>
        /// <param name="skipVanilla">是否跳过原版逻辑</param>
        public virtual void CallNearbyCreaturesHelp(ComponentHerdBehavior herdBehavior,
            ComponentCreature target,
            float maxRange,
            float maxChaseTime,
            bool isPersistent,
            out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     ComponentHerdBehavior组件接口，
        ///     用于生物寻找大部队中心点。
        ///     仅建议对自己模组的生物使用，不要干涉其他模组生物的行为。
        /// </summary>
        /// <param name="componentCreature"></param>
        /// <param name="skipVanilla">是否跳过原版逻辑</param>
        /// <param name="herdCenter">生物的大部队中心点，返回null则表示其行为不受大部队中心点约束</param>
        public virtual void FindHerdCenter(ComponentCreature componentCreature, out Vector3? herdCenter, out bool skipVanilla) {
            herdCenter = null;
            skipVanilla = false;
        }

        /// <summary>
        ///     ComponentRider组件接口
        ///     用于获取可符合骑行条件生物
        ///     通过此接口，模组可以实现自定义骑行，也可以用于生物骑行生物相关逻辑
        ///     为保证模组兼容性，建议预先判断是否是自己模组的载具，且仅处理自己模组的载具
        /// </summary>
        /// <param name="componentRider"></param>
        /// <param name="componentMount"></param>
        /// <param name="score">返回的分数。返回null则表示不处理，移交给原版或下一个模组。返回正数表示允许骑乘，返回负数表示禁止骑乘（即使其他模组允许）。</param>
        public virtual void ScoreMount(ComponentRider componentRider, ComponentMount componentMount, out float? score) {
            score = null;
        }

        /// <summary>
        ///     挖掘触发宝物生成时，注意这里能获取到上个Mod生成宝物的情况
        /// </summary>
        /// <param name="subsystemTerrain"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="neighborX"></param>
        /// <param name="neighborY"></param>
        /// <param name="neighborZ"></param>
        /// <param name="blockValue">宝物的方块值</param>
        /// <param name="count">宝物数量</param>
        /// <param name="isGenerate">是否继续让其它Mod处理</param>
        public virtual void OnTreasureGenerate(SubsystemTerrain subsystemTerrain,
            int x,
            int y,
            int z,
            int neighborX,
            int neighborY,
            int neighborZ,
            ref int blockValue,
            ref int count,
            out bool isGenerate) {
            isGenerate = false;
        }

        /// <summary>
        ///     当界面被创建时
        /// </summary>
        /// <param name="widget"></param>
        public virtual void OnWidgetConstruct(ref Widget widget) { }

        /// <summary>
        ///     当界面内容完成加载时
        /// </summary>
        /// <param name="widget"></param>
        public virtual void OnWidgetContentsLoaded(Widget widget) { }

        /// <summary>
        ///     在 DrawItem 被绘制前。
        /// </summary>
        /// <param name="drawItem">被绘制的 DrawItem。</param>
        /// <param name="skipVanillaDraw">是否跳过原版绘制代码。</param>
        /// <param name="afterWidgetDraw">原版绘制完成后的回调。</param>
        /// <param name="scissorRectangle">绘制时的 ScissorRectangle。</param>
        /// <param name="drawContext">绘制上下文。</param>
        public virtual void BeforeWidgetDrawItemRender(Widget.DrawItem drawItem,
            out bool skipVanillaDraw,
            out Action afterWidgetDraw,
            ref Rectangle scissorRectangle,
            Widget.DrawContext drawContext) {
            skipVanillaDraw = false;
            afterWidgetDraw = null;
        }

        /// <summary>
        ///     在 DrawItem 排序后。
        /// </summary>
        /// <param name="drawContext">绘制上下文。</param>
        public virtual void OnDrawItemAssigned(Widget.DrawContext drawContext) { }

        /// <summary>
        ///     当ModalPanelWidget被设置时执行
        /// </summary>
        /// <param name="componentGui"></param>
        /// <param name="oldWidget"></param>
        /// <param name="newWidget"></param>
        public virtual void OnModalPanelWidgetSet(ComponentGui componentGui, Widget oldWidget, Widget newWidget) { }

        /// <summary>
        ///     生成地形顶点时使用
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="even"></param>
        public virtual void GenerateChunkVertices(TerrainChunk chunk, bool even) { }

        /// <summary>
        ///     生成光源数据
        /// </summary>
        /// <param name="lightSources">光源</param>
        /// <param name="chunk">区块</param>
        public virtual void GenerateChunkLightSources(DynamicArray<TerrainUpdater.LightSource> lightSources, TerrainChunk chunk) { }

        /// <summary>
        ///     计算动物模型光照
        /// </summary>
        /// <param name="subsystemTerrain"></param>
        /// <param name="p">动物位置</param>
        /// <param name="num">原版计算出来的强度</param>
        public virtual void CalculateSmoothLight(SubsystemTerrain subsystemTerrain, Vector3 p, ref float num) { }

        /// <summary>
        ///     当窗口模式改变时执行。
        /// </summary>
        public virtual void WindowModeChanged(WindowMode mode) { }

        /// <summary>
        ///     在执行DamageItem得到方块掉耐久后，得到的新方块值时执行
        /// </summary>
        /// <param name="block"></param>
        /// <param name="oldValue">方块的旧值</param>
        /// <param name="damageCount">损害的耐久量</param>
        /// <param name="owner">方块的拥有者</param>
        /// <param name="skipVanilla">跳过原版执行逻辑</param>
        /// <returns></returns>
        public virtual int DamageItem(Block block, int oldValue, int damageCount, Entity owner, out bool skipVanilla) {
            skipVanilla = false;
            return 0;
        }

        /// <summary>
        ///     当射弹击中方块时执行
        /// </summary>
        /// <param name="projectile">射弹</param>
        /// <param name="terrainRaycastResult">地形映射结果</param>
        /// <param name="triggerBlocksBehavior">是否执行被命中的方块行为</param>
        /// <param name="destroyCell">是否破坏被击中的方块</param>
        /// <param name="impactSoundLoudness">发出的声音大小</param>
        /// <param name="projectileGetStuck">射弹是否会被卡在方块里面</param>
        /// <param name="velocityAfterHit">在击中方块后，射弹的速度</param>
        /// <param name="angularVelocityAfterHit">在击中方块后，射弹的角速度</param>
        public virtual void OnProjectileHitTerrain(Projectile projectile,
            TerrainRaycastResult terrainRaycastResult,
            ref bool triggerBlocksBehavior,
            ref bool destroyCell,
            ref float impactSoundLoudness,
            ref bool projectileGetStuck,
            ref Vector3 velocityAfterHit,
            ref Vector3 angularVelocityAfterHit) { }

        /// <summary>
        ///     当射弹击中生物、船只等实体时执行
        /// </summary>
        /// <param name="projectile">射弹</param>
        /// <param name="bodyRaycastResult">实体映射结果</param>
        /// <param name="attackment">该射弹命中实体时，执行的攻击。可以调整attackment的攻击力等数据</param>
        /// <param name="velocityAfterAttack">在击中方块后，射弹的速度</param>
        /// <param name="angularVelocityAfterAttack">在击中方块后，射弹的角速度</param>
        /// <param name="ignoreBody">射弹行进直接穿过该生物。射弹后续的更新会忽略该生物，速度和角速度保持原状。攻击照常执行。</param>
        public virtual void OnProjectileHitBody(Projectile projectile,
            BodyRaycastResult bodyRaycastResult,
            ref Attackment attackment,
            ref Vector3 velocityAfterAttack,
            ref Vector3 angularVelocityAfterAttack,
            ref bool ignoreBody) { }

        /// <summary>
        ///     绘制射弹的时候执行
        /// </summary>
        /// <param name="projectile">射弹</param>
        /// <param name="subsystemProjectiles">该子系统，可以从中获取项目和其他子系统</param>
        /// <param name="camera"></param>
        /// <param name="drawOrder"></param>
        /// <param name="shouldDrawBlock">是否执行原版绘制方块的方法</param>
        /// <param name="drawBlockSize">绘制方块大小</param>
        /// <param name="drawBlockColor">绘制方块颜色</param>
        public virtual void OnProjectileDraw(Projectile projectile,
            SubsystemProjectiles subsystemProjectiles,
            Camera camera,
            int drawOrder,
            ref bool shouldDrawBlock,
            ref float drawBlockSize,
            ref Color drawBlockColor) { }

        /// <summary>
        ///     射弹离开加载区块的时候执行
        /// </summary>
        /// <param name="projectile"></param>
        public virtual void OnProjectileFlyOutOfLoadedChunks(Projectile projectile) { }

        /// <summary>
        ///     绘制掉落物的时候执行
        /// </summary>
        /// <param name="pickable"></param>
        /// <param name="subsystemPickables"></param>
        /// <param name="camera"></param>
        /// <param name="drawOrder"></param>
        /// <param name="shouldDrawBlock">是否执行原版绘制方块的方法</param>
        /// <param name="drawBlockSize"></param>
        /// <param name="drawBlockColor"></param>
        public virtual void OnPickableDraw(Pickable pickable,
            SubsystemPickables subsystemPickables,
            Camera camera,
            int drawOrder,
            ref bool shouldDrawBlock,
            ref float drawBlockSize,
            ref Color drawBlockColor) { }

        /// <summary>
        ///     执行动物的Update操作。
        ///     建议只在对自己模组的动物才进行带skip的覆盖原版的操作；对原版和其他模组的动物，请不要过多干涉覆盖原有的操作
        /// </summary>
        /// <param name="componentBody"></param>
        /// <param name="dt">动物位置</param>
        /// <param name="skipVanilla">跳过原版的更新操作</param>
        /// <param name="skippedByOtherMods">前面的mod已经执行了带skip操作的Update</param>
        public virtual void UpdateComponentBody(ComponentBody componentBody, float dt, bool skippedByOtherMods, out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     计算动物在Raycast下的表现。输出null表示这个body不计入Raycast结果；输出一个具体的数值表示Raycast计算出来的距离。
        /// </summary>
        /// <param name="componentBody"></param>
        /// <param name="ray">动物位置</param>
        /// <param name="skip">原版计算出来的强度</param>
        public virtual float? BodyCountInRaycast(ComponentBody componentBody, Ray3 ray, out bool skip) {
            skip = false;
            return null;
        }

        /// <summary>
        ///     在添加移动方块时触发
        /// </summary>
        /// <param name="movingBlockSet"></param>
        /// <param name="subsystemMovingBlocks"></param>
        /// <param name="testCollision">对应原方法的TestCollision部分</param>
        /// <param name="doNotAdd">取消添加移动方块</param>
        public virtual void OnMovingBlockSetAdded(ref SubsystemMovingBlocks.MovingBlockSet movingBlockSet,
            SubsystemMovingBlocks subsystemMovingBlocks,
            ref bool testCollision,
            out bool doNotAdd) {
            doNotAdd = false;
        }

        /// <summary>
        ///     移除移动方块时触发
        /// </summary>
        /// <param name="movingBlockSet"></param>
        /// <param name="subsystemMovingBlocks"></param>
        public virtual void OnMovingBlockSetRemoved(IMovingBlockSet movingBlockSet, SubsystemMovingBlocks subsystemMovingBlocks) { }

        /// <summary>
        ///     在移动方块更新时触发
        /// </summary>
        /// <param name="movingBlockSet"></param>
        /// <param name="subsystemMovingBlocks"></param>
        /// <param name="skippedByOtherMods">是否已被其他模组抢先执行更新</param>
        /// <param name="skipVanilla">是否跳过原版执行更新（抢先更新）</param>
        public virtual void OnMovingBlockSetUpdate(IMovingBlockSet movingBlockSet,
            SubsystemMovingBlocks subsystemMovingBlocks,
            bool skippedByOtherMods,
            out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     游戏中添加射弹时触发
        /// </summary>
        /// <param name="subsystemProjectiles"></param>
        /// <param name="projectile"></param>
        /// <param name="loadValuesDictionary">如果是加载世界过程中首次添加，那么会提供该射弹的相关ValuesDictionary；如果是游戏进行过程中添加，则为null</param>
        public virtual void OnProjectileAdded(SubsystemProjectiles subsystemProjectiles,
            ref Projectile projectile,
            ValuesDictionary loadValuesDictionary) { }

        /// <summary>
        ///     游戏中添加掉落物时触发
        /// </summary>
        /// <param name="subsystemPickables"></param>
        /// <param name="pickable"></param>
        /// <param name="loadValuesDictionary">如果是加载世界过程中首次添加，那么会提供该射弹的相关ValuesDictionary；如果是游戏进行过程中添加，则为null</param>
        public virtual void OnPickableAdded(SubsystemPickables subsystemPickables, ref Pickable pickable, ValuesDictionary loadValuesDictionary) { }

        /// <summary>
        ///     保存世界时，存储射弹信息
        /// </summary>
        /// <param name="subsystemProjectiles"></param>
        /// <param name="projectile"></param>
        /// <param name="valuesDictionary">存储射弹信息的ValuesDictionary</param>
        public virtual void SaveProjectile(SubsystemProjectiles subsystemProjectiles,
            Projectile projectile,
            ref ValuesDictionary valuesDictionary) { }

        /// <summary>
        ///     保存世界时，存储掉落物信息
        /// </summary>
        /// <param name="subsystemPickables"></param>
        /// <param name="pickable"></param>
        /// <param name="valuesDictionary">存储掉落物信息的ValuesDictionary</param>
        public virtual void SavePickable(SubsystemPickables subsystemPickables, Pickable pickable, ref ValuesDictionary valuesDictionary) { }

        /// <summary>
        ///     在方块被挖掘完毕时执行
        /// </summary>
        /// <param name="componentMiner"></param>
        /// <param name="digValue"></param>
        /// <param name="cellValue"></param>
        /// <param name="durabilityReduction">挖掘方块所消耗工具的耐久</param>
        /// <param name="mute">挖掘方块是否取消播放音效</param>
        /// <param name="playerDataDugAdd">是否增加玩家统计信息的挖掘方块计数</param>
        public virtual void OnBlockDug(ComponentMiner componentMiner,
            BlockPlacementData digValue,
            int cellValue,
            ref int durabilityReduction,
            ref bool mute,
            ref int playerDataDugAdd) {
            mute = false;
        }

        /// <summary>
        ///     改变SubsystemTime的时间推移速度，偏向底层，一般开发者不必了解
        /// </summary>
        /// <param name="subsystemTime"></param>
        /// <param name="dt"></param>
        public virtual void ChangeGameTimeDelta(SubsystemTime subsystemTime, ref float dt) { }

        /// <summary>
        ///     在IUpdateable添加或删除时执行，用于模组接管IUpdateable的更新行为
        ///     （如恒泰模组将动物放在多线程中进行更新，降低怪物数量多导致的卡顿）
        /// </summary>
        /// <param name="subsystemUpdate"></param>
        /// <param name="updateable"></param>
        /// <param name="toAdd1OrRemove0">这个IUpdateable是准备添加的，该变量为1；这个IUpdateable是准备移除的，该变量为0</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组接管</param>
        /// <param name="skip">宣布接管，则不会被原版的SubsystemUpdate执行Update()</param>
        public virtual void OnIUpdateableAddOrRemove(SubsystemUpdate subsystemUpdate,
            IUpdateable updateable,
            bool toAdd1OrRemove0,
            bool skippedByOtherMods,
            out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     在IDrawable添加或删除时执行，用于模组接管IDrawable的绘制行为
        /// </summary>
        /// <param name="subsystemDrawing"></param>
        /// <param name="drawable"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组接管</param>
        /// <param name="skip">宣布接管，该IDrawable不会放入SubsystemDrawing.m_drawables</param>
        public virtual void OnIDrawableAdded(SubsystemDrawing subsystemDrawing, IDrawable drawable, bool skippedByOtherMods, out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     在创建家具时执行
        /// </summary>
        /// <param name="furnitureDesign"></param>
        /// <param name="designedFromExistingFurniture">是否从已有家具方块创建，通常用于mod禁止家具复制</param>
        /// <param name="pickableCount">产生的掉落物数量</param>
        /// <param name="destroyDesignBlocks">是否移除搭建的建筑原型</param>
        /// <param name="toolDamageCount">家具锤消耗的耐久量，如果家具锤剩余耐久不足以支持消耗量，则玩家无法创建家具并弹出提示</param>
        public virtual void OnFurnitureDesigned(FurnitureDesign furnitureDesign,
            bool designedFromExistingFurniture,
            ref int pickableCount,
            ref bool destroyDesignBlocks,
            ref int toolDamageCount) { }

        /// <summary>
        ///     在创建InventorySlotWidget时执行，可以增加元素
        /// </summary>
        /// <param name="inventorySlotWidget"></param>
        /// <param name="childrenWidgetsToAdd">创建InventorySlotWidget时，返回增加的子Widget</param>
        public virtual void OnInventorySlotWidgetDefined(InventorySlotWidget inventorySlotWidget, out List<Widget> childrenWidgetsToAdd) {
            childrenWidgetsToAdd = null;
        }

        /// <summary>
        ///     绘制物品格子的耐久条、食物条等元素
        /// </summary>
        /// <param name="inventorySlotWidget"></param>
        /// <param name="parentAvailableSize">其父widget的大小</param>
        public virtual void InventorySlotWidgetMeasureOverride(InventorySlotWidget inventorySlotWidget, Vector2 parentAvailableSize) { }

        /// <summary>
        ///     当移动物品时执行。从sourceInventory的第sourceSlotIndex个格子，移动count个物品，到targetInventory的第targetSlotIndex个格子
        /// </summary>
        /// <param name="inventorySlotWidget"></param>
        /// <param name="sourceInventory"></param>
        /// <param name="sourceSlotIndex"></param>
        /// <param name="targetInventory"></param>
        /// <param name="targetSlotIndex"></param>
        /// <param name="count">留给后面模组和原版处理物品的数量</param>
        /// <param name="moved">是否完成移动操作，注意这个不影响跳过原版处理</param>
        public virtual void HandleMoveInventoryItem(InventorySlotWidget inventorySlotWidget,
            IInventory sourceInventory,
            int sourceSlotIndex,
            IInventory targetInventory,
            int targetSlotIndex,
            ref int count,
            out bool moved) {
            moved = false;
        }

        /// <summary>
        ///     在InventorySlotWidget计算分割物品数量时执行（包括拖拽显示数量和实际移动数量），允许修改最终的分割数量
        ///     （比如原版的1个/一半分割逻辑，可通过此方法自定义为固定数量、按物品类型分割等）
        /// </summary>
        /// <param name="inventorySlotWidget">触发分割数量计算的InventorySlotWidget实例（当前物品槽）</param>
        /// <param name="totalItemCount">分割前的物品总数量（当前槽位的物品总数）</param>
        /// <param name="dragMode">当前的拖拽模式（AllItems：全部/SingleItem：单个/HalfItems：一半）</param>
        /// <param name="splitCount">原版逻辑计算后的分割数量（ref关键字允许修改，修改后会作为最终的分割数量使用）</param>
        public virtual void OnInventorySlotWidgetCalculateSplitCount(
            InventorySlotWidget inventorySlotWidget,
            int totalItemCount,
            DragMode dragMode,
            ref int splitCount) { }

        /// <summary>
        ///     在InventorySlotWidget.HandleDragDrop时执行，先执行物品的修改操作
        ///     （比如原版火药拖到枪身上时执行上膛操作）
        /// </summary>
        /// <param name="inventorySlotWidget">目标格子的InventorySlotWidget</param>
        /// <param name="sourceInventory"></param>
        /// <param name="sourceSlotIndex"></param>
        /// <param name="targetInventory"></param>
        /// <param name="targetSlotIndex"></param>
        /// <param name="processCapacity">目标格子接受物品的数量。设置为不大于0的数相当于跳过原版逻辑</param>
        public virtual void HandleInventoryDragProcess(InventorySlotWidget inventorySlotWidget,
            IInventory sourceInventory,
            int sourceSlotIndex,
            IInventory targetInventory,
            int targetSlotIndex,
            ref int processCapacity) { }

        /// <summary>
        ///     在InventorySlotWidget.HandleDragDrop时执行，如果物品没有修改操作，则执行移动物品操作
        /// </summary>
        /// <param name="inventorySlotWidget">目标格子的InventorySlotWidget</param>
        /// <param name="sourceInventory"></param>
        /// <param name="sourceSlotIndex"></param>
        /// <param name="targetInventory"></param>
        /// <param name="targetSlotIndex"></param>
        /// <param name="skippedByOtherMods">执行逻辑是否已经被其他模组跳过</param>
        /// <param name="skip">跳过原版的执行逻辑</param>
        public virtual void HandleInventoryDragMove(InventorySlotWidget inventorySlotWidget,
            IInventory sourceInventory,
            int sourceSlotIndex,
            IInventory targetInventory,
            int targetSlotIndex,
            bool skippedByOtherMods,
            out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     在玩家骑上坐骑时每帧执行，用于调整玩家骑行动物时的控制逻辑
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作</param>
        public virtual void OnPlayerControlSteed(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     在玩家乘坐船时每帧执行，用于调整玩家乘船时的控制逻辑
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作</param>
        public virtual void OnPlayerControlBoat(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     在玩家乘坐船、动物以外的物体时每帧执行，用于控制玩家骑模组坐骑的控制魔力
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过其他模组执行操作</param>
        public virtual void OnPlayerControlOtherMount(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     当玩家既不在坐骑上，也不在船上时执行，用于控制玩家行走的控制逻辑
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作</param>
        public virtual void OnPlayerControlWalk(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     当玩家输入交互逻辑时执行的操作
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="playerOperated">为true则停止之后的挖掘、攻击等操作</param>
        /// <param name="timeIntervalLastActionTime">距离上一次触发该操作距离的时长</param>
        /// <param name="priorityUse">控制使用优先级，使用优先级小于等于0则禁止玩家使用手中物品</param>
        /// <param name="priorityInteract">控制交互优先级，交互优先级小于等于0则禁止玩家交互方块</param>
        /// <param name="priorityPlace">控制放置优先级，放置优先级小于等于0则禁止玩家放置方块</param>
        public virtual void OnPlayerInputInteract(ComponentPlayer componentPlayer,
            ref bool playerOperated,
            ref double timeIntervalLastActionTime,
            ref int priorityUse,
            ref int priorityInteract,
            ref int priorityPlace) { }

        /// <summary>
        ///     在玩家正在瞄准时执行
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="aiming">是否正在瞄准</param>
        /// <param name="playerOperated">为true则停止之后的挖掘、攻击等操作</param>
        /// <param name="timeIntervalAim">和上一次执行瞄准操作，要求的最小时间间隔</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作（为了模组间兼容性，建议只在手持自己模组方块时这样做）</param>
        public virtual void UpdatePlayerInputAim(ComponentPlayer componentPlayer,
            bool aiming,
            ref bool playerOperated,
            ref float timeIntervalAim,
            bool skippedByOtherMods,
            out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     在玩家执行“攻击”动作时执行，比如恒泰左键放箭，工业左键点击船
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="playerOperated">为true则停止之后的挖掘操作</param>
        /// <param name="timeIntervalHit">和上一次输入攻击操作，要求的最小时间间隔，小于该间隔时输入无效。（注意和ComponentMiner.HitInterval作区分）</param>
        /// <param name="meleeAttackRange">近战攻击距离，小于等于0时表示不进行近战操作（比如手持弓时近战距离改为0，就不会拿着弓拍敌人）</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作（为了模组间兼容性，建议只在手持自己模组方块时这样做）</param>
        public virtual void OnPlayerInputHit(ComponentPlayer componentPlayer,
            ref bool playerOperated,
            ref double timeIntervalHit,
            ref float meleeAttackRange,
            bool skippedByOtherMods,
            out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     在玩家执行“挖掘”动作时执行
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="digging">玩家是否正在挖掘</param>
        /// <param name="playerOperated">为true则停止之后的创造模式中键选择物品等操作</param>
        /// <param name="timeIntervalDig">和上一次执行挖掘操作，要求的最小时间间隔。将该值降低可以像恒泰那样极速挖掘</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组跳过逻辑</param>
        /// <param name="skipVanilla">跳过原版执行操作（为了模组间兼容性，建议只在手持自己模组方块时这样做）</param>
        public virtual void UpdatePlayerInputDig(ComponentPlayer componentPlayer,
            bool digging,
            ref bool playerOperated,
            ref double timeIntervalDig,
            bool skippedByOtherMods,
            out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     在玩家电脑上“按Q释放剑弃”时执行
        /// </summary>
        /// <param name="componentPlayer"></param>
        /// <param name="skippedByOtherMods"></param>
        /// <param name="skipVanilla"></param>
        public virtual void OnPlayerInputDrop(ComponentPlayer componentPlayer, bool skippedByOtherMods, out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     在闪电劈下时执行
        /// </summary>
        /// <param name="subsystemSky"></param>
        /// <param name="targetPosition">闪电劈下的位置</param>
        /// <param name="strike">是否能成功执行</param>
        /// <param name="explosionPressure">闪电的爆炸威力</param>
        /// <param name="setBodyOnFire">是否点燃目标</param>
        public virtual void OnLightningStrike(SubsystemSky subsystemSky,
            ref Vector3 targetPosition,
            ref bool strike,
            ref float explosionPressure,
            ref bool setBodyOnFire) { }

        /// <summary>
        ///     用于调整原版已有矿物、水域、植物等地形地貌的生成，例如减少原版矿物生成量
        /// </summary>
        public virtual void OnTerrainBrushesCreated() { }

        /// <summary>
        ///     在创建世界时寻找玩家的初步生成大致位置
        /// </summary>
        /// <param name="terrainContentsGenerator"></param>
        /// <param name="spawnPosition">玩家初步生成大致位置</param>
        public virtual void FindCoarseSpawnPosition(ITerrainContentsGenerator terrainContentsGenerator, ref Vector3 spawnPosition) { }

        /// <summary>
        ///     在动物执行近战攻击命中目标时执行
        /// </summary>
        /// <param name="componentChaseBehavior"></param>
        /// <param name="chaseTimeBefore">在攻击之前的剩余追逐时间</param>
        /// <param name="chaseTime">在攻击之后的剩余追逐时间</param>
        /// <param name="hitBody">是否能够攻击</param>
        /// <param name="playAttackSound">是否发出攻击音效</param>
        public virtual void OnChaseBehaviorAttacked(ComponentChaseBehavior componentChaseBehavior,
            float chaseTimeBefore,
            ref float chaseTime,
            ref bool hitBody,
            ref bool playAttackSound) { }

        /// <summary>
        ///     在动物执行近战攻击没有命中目标时执行
        /// </summary>
        /// <param name="componentChaseBehavior"></param>
        /// <param name="chaseTime">在攻击之后的剩余追逐时间</param>
        public virtual void OnChaseBehaviorAttackFailed(ComponentChaseBehavior componentChaseBehavior, ref float chaseTime) { }

        /// <summary>
        ///     计算动物的坠落伤害
        /// </summary>
        /// <param name="componentHealth">生物的ComponentHealth，至于坠落速度等信息则从ComponentHealth出发寻找</param>
        /// <param name="damage">坠落伤害</param>
        public virtual void CalculateFallDamage(ComponentHealth componentHealth, ref float damage) { }

        /// <summary>
        ///     在动物晕眩或死亡时执行移动
        /// </summary>
        /// <param name="componentLocomotion"></param>
        /// <param name="fallsOnDeathOrStun">在晕眩或死亡时是否坠落</param>
        public virtual void OnLocomotionStopped(ComponentLocomotion componentLocomotion, ref bool fallsOnDeathOrStun) { }

        /// <summary>
        ///     在ComponentLocomotion加载时执行
        /// </summary>
        /// <param name="componentLocomotion"></param>
        /// <param name="mobWalkSpeedFactor">非玩家生物的移速乘数</param>
        /// <param name="mobFlySpeedFactor">非玩家生物的飞行速度乘数</param>
        /// <param name="mobSwimSpeedFactor">非玩家生物的游泳速度乘数</param>
        /// <param name="disableCreativeFlyInSurvivalMode">是否在生存模式中停止创造飞行（通常发生在创造模式切换到生存模式中）</param>
        public virtual void OnComponentLocomotionLoaded(ComponentLocomotion componentLocomotion,
            ref float mobWalkSpeedFactor,
            ref float mobFlySpeedFactor,
            ref float mobSwimSpeedFactor,
            ref bool disableCreativeFlyInSurvivalMode) { }

        /// <summary>
        ///     在发射器投掷物品时执行
        /// </summary>
        /// <param name="componentDispenser">该发射器的Component</param>
        /// <param name="pickable">要发射的掉落物</param>
        /// <param name="removeSlotCount">移除发射器物品栏中物品数量</param>
        public virtual void OnDispenserDispensePickable(ComponentDispenser componentDispenser, ref Pickable pickable, ref int removeSlotCount) { }

        /// <summary>
        ///     在发射器弹射物品时执行
        /// </summary>
        /// <param name="componentDispenser">该发射器的Component</param>
        /// <param name="projectile">要发射的弹射物</param>
        /// <param name="canDispensePickable">发射失败时，是否以掉落物的方式发射（即使不发射也会消耗）</param>
        /// <param name="removeSlotCount">移除发射器物品栏中物品数量</param>
        public virtual void OnDispenserShoot(ComponentDispenser componentDispenser,
            ref Projectile projectile,
            ref bool canDispensePickable,
            ref int removeSlotCount) { }

        /// <summary>
        ///     发射器选择消耗哪一个物品进行发射
        /// </summary>
        /// <param name="componentDispenser"></param>
        /// <param name="slot">选择消耗哪一个格子的物品</param>
        /// <param name="value">选择发射什么物品</param>
        /// <param name="chosen">是否已经选择。若已经选择，则会跳过后面模组中执行。为了兼容性，仅推荐发射器在有自己模组方块的时候才执行</param>
        public virtual void DispenserChooseItemToDispense(ComponentDispenser componentDispenser, ref int slot, ref int value, out bool chosen) {
            chosen = false;
        }

        /// <summary>
        ///     在世界选择列表时，调整存档的外观
        /// </summary>
        /// <param name="worldInfo">世界信息</param>
        /// <param name="savedWorldItemNode">存储世界信息的XElement</param>
        /// <param name="worldInfoWidget">要修改的Widget</param>
        public virtual void LoadWorldInfoWidget(WorldInfo worldInfo, XElement savedWorldItemNode, ref ContainerWidget worldInfoWidget) { }

        /// <summary>
        ///     在方块介绍页面中，增加或减少方块的属性字段
        /// </summary>
        /// <param name="blockProperties"></param>
        [Obsolete("Override EditBlockDescriptionScreen(Dictionary<string,string> blockProperties, int blockValue) instead.")]
        public virtual void EditBlockDescriptionScreen(Dictionary<string, string> blockProperties) { }

        /// <summary>
        ///     在方块介绍页面中，增加或减少方块的属性字段
        /// </summary>
        /// <param name="blockProperties">已有的方块属性表</param>
        /// <param name="blockValue">被介绍的方块完整值</param>
        public virtual void EditBlockDescriptionScreen(Dictionary<string, string> blockProperties, int blockValue) { }

        /// <summary>
        ///     在合成表页面时每帧更新时，编辑该页面
        /// </summary>
        /// <param name="screen"></param>
        public virtual void EditRecipeScreenWidget(RecipaediaRecipesScreen screen) { }

        /// <summary>
        ///     在生物图鉴页面每帧更新时，编辑该页面
        /// </summary>
        /// <param name="bestiaryDescriptionScreen"></param>
        /// <param name="bestiaryCreatureInfo">该生物的基础信息</param>
        /// <param name="entityValuesDictionary">该生物在Database中的ValuesDictionary</param>
        public virtual void UpdateCreaturePropertiesInBestiaryDescriptionScreen(BestiaryDescriptionScreen bestiaryDescriptionScreen,
            BestiaryCreatureInfo bestiaryCreatureInfo,
            ValuesDictionary entityValuesDictionary) { }

        /// <summary>
        ///     在生物图鉴目录列表更新该条目时，编辑该条目
        /// </summary>
        /// <param name="bestiaryScreen"></param>
        /// <param name="creatureInfoWidget">可以更改的生物信息Widget</param>
        /// <param name="bestiaryCreatureInfo">该生物的基础信息</param>
        /// <param name="entityValuesDictionary">该生物在Database中的ValuesDictionary</param>
        public virtual void LoadCreatureInfoInBestiaryScreen(BestiaryScreen bestiaryScreen,
            ContainerWidget creatureInfoWidget,
            BestiaryCreatureInfo bestiaryCreatureInfo,
            ValuesDictionary entityValuesDictionary) { }

        /// <summary>
        ///     在进行世界设置时，如果不是创造模式，则会修改设定
        /// </summary>
        /// <param name="worldSettings">要修改的世界设置</param>
        /// <param name="environmentBehaviorModeBefore"></param>
        /// <param name="timeOfDayModeBefore"></param>
        /// <param name="areWeatherEffectsEnabledBefore"></param>
        /// <param name="areSurvivalMechanicsEnabledBefore"></param>
        public virtual void ResetOptionsForNonCreativeMode(WorldSettings worldSettings,
            EnvironmentBehaviorMode environmentBehaviorModeBefore,
            TimeOfDayMode timeOfDayModeBefore,
            bool areWeatherEffectsEnabledBefore,
            bool areSurvivalMechanicsEnabledBefore) { }

        /// <summary>
        ///     在配方表加载的时候执行，用于删除原版配方
        /// </summary>
        /// <param name="recipes">已经加载的配方</param>
        /// <param name="sort">是否在删除后重新排序</param>
        public virtual void CraftingRecipesManagerInitialize(List<CraftingRecipe> recipes, ref bool sort) { }

        /// <summary>
        ///     在配方表加载完成后执行，在排序完成后执行
        /// </summary>
        public virtual void CraftingRecipesManagerInitialized() { }

        /// <summary>
        ///     在游戏游玩过程中时放音乐
        /// </summary>
        public virtual void PlayInGameMusic() { }

        [Obsolete("Use TerrainContentsGenerator24Initialize")]
        public virtual void TerrainContentsGenerator23Initialize(ITerrainContentsGenerator terrainContentsGenerator,
            SubsystemTerrain subsystemTerrain) { }

        public virtual void TerrainContentsGenerator24Initialize(ITerrainContentsGenerator terrainContentsGenerator,
            SubsystemTerrain subsystemTerrain) { }

        public virtual void PrepareModels(SubsystemModelsRenderer subsystemModelsRenderer, Camera camera, bool skippedByOtherMods, out bool skip) {
            skip = false;
        }

        public virtual void RenderModels(SubsystemModelsRenderer subsystemModelsRenderer,
            Camera camera,
            int drawOrder,
            bool skippedByOtherMods,
            out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     在更新玩家死亡界面时执行
        /// </summary>
        /// <param name="playerData">具体死者</param>
        /// <param name="disableVanillaTapToRespawnAction">是否阻止原版点击任意键就执行复活等下一步的操作</param>
        /// <param name="respawn">是否复活</param>
        public virtual void UpdateDeathCameraWidget(PlayerData playerData, ref bool disableVanillaTapToRespawnAction, ref bool respawn) { }

        /// <summary>
        ///     在爆炸开始，计算动物摇晃程度时执行。
        ///     为了尽可能确保兼容性，建议只对属于自己模组的生物进行接管编辑
        /// </summary>
        /// <param name="componentBody">将要摇晃的动物</param>
        /// <param name="explosionCenter">爆炸中心地点</param>
        /// <param name="explosionPressure">爆炸强度</param>
        /// <param name="shakeStrength">动物摇晃的强度</param>
        public virtual void OnComponentBodyExplodedStart(ComponentBody componentBody,
            Vector3 explosionCenter,
            float explosionPressure,
            ref float shakeStrength) { }

        /// <summary>
        ///     在游戏执行植物生长判定时执行，为保证良好兼容性，建议只在处理属于自己模组的方块时改变控制
        /// </summary>
        /// <param name="subsystemPlantBlockBehavior">子系统，便于定位SubsystemTerrain等必要组件</param>
        /// <param name="x">植物的坐标x</param>
        /// <param name="y">植物的坐标y</param>
        /// <param name="z">植物的坐标z</param>
        /// <param name="pollPass">用途不确定</param>
        /// <param name="skipVanilla">是否跳过原版的执行进程，注意本接口不能跳过其他模组的执行进程</param>
        public virtual void GrowPlant(SubsystemPlantBlockBehavior subsystemPlantBlockBehavior,
            int x,
            int y,
            int z,
            int pollPass,
            out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     射弹转化为掉落物时执行
        /// </summary>
        /// <param name="projectile">即将被转化并删除的射弹</param>
        /// <param name="pickable">即将加入的掉落物</param>
        public virtual void OnProjectileTurnIntoPickable(Projectile projectile, ref Pickable pickable) { }

        /// <summary>
        ///     在实体创建时执行，可用于移除实体的特定组件
        /// </summary>
        /// <param name="entity">被创建的实体</param>
        /// <param name="componentList">这个实体将会拥有的Component，KeyValuePair的第一个表示Component的加载顺序编号，第二个表示实体的Component</param>
        public virtual void EntityComponentsInitialized(Entity entity, List<KeyValuePair<int, Component>> componentList) { }

        /// <summary>
        ///     游戏Project创建时执行，可用于移除Subsystem、Entity、Entity.Component
        /// </summary>
        /// <param name="project"></param>
        public virtual void ProjectBeforeSubsystemsAndEntitiesLoad(Project project) { }

        /// <summary>
        ///     进入Screen时执行
        /// </summary>
        /// <param name="screen">进入的Screen</param>
        /// <param name="screenParameters">进入Screen时传入的参数，参数的详细信息需要去查阅对应screen的代码</param>
        public virtual void OnScreenEntered(Screen screen, object[] screenParameters) { }

        /// <summary>
        ///     离开Screen时执行
        /// </summary>
        /// <param name="screen">离开的Screen</param>
        public virtual void OnScreenLeaved(Screen screen) { }

        /// <summary>
        ///     在Widget即将执行Update()时执行，能用于修改游戏中已有按钮的功能
        ///     并且可以通过一定的操作，拦截原版按钮的功能
        /// </summary>
        /// <param name="widget"></param>
        public virtual void BeforeWidgetUpdate(Widget widget) { }

        /// <summary>
        ///     在Widget完成Update()后立即执行，能用于修改游戏中已有按钮的功能
        /// </summary>
        /// <param name="widget"></param>
        public virtual void AfterWidgetUpdate(Widget widget) { }

        public virtual void OnChaseBehaviorStartChasing(ComponentChaseBehavior chaseBehavior) { }
        public virtual void OnChaseBehaviorStopChasing(ComponentChaseBehavior chaseBehavior) { }

        /// <summary>
        ///     生物在选择追击对象时，对被追击对象的评分。评分越高，则会被选中。
        ///     仅建议在该生物为本模组生物，或目标生物为本模组生物时，修改追击权重。
        /// </summary>
        /// <param name="chaseBehavior"></param>
        /// <param name="creatureToBeTarget">被评估的对象</param>
        /// <param name="score">给出的评分</param>
        public virtual void ChaseBehaviorScoreTarget(ComponentChaseBehavior chaseBehavior, ComponentCreature creatureToBeTarget, ref float score) { }

        public virtual void UpdateChaseBehaviorLookingForTarget(ComponentChaseBehavior chaseBehavior) { }
        public virtual void UpdateChaseBehaviorChasing(ComponentChaseBehavior chaseBehavior) { }

        /// <summary>
        ///     修改生物的战利品。
        ///     建议不要清空dropValues推倒重来，不利于模组兼容性。
        /// </summary>
        /// <param name="componentLoot"></param>
        /// <param name="dropValues">其中包含所有的战利品掉落物的方块值和数量</param>
        public virtual void DecideLoot(ComponentLoot componentLoot, List<BlockDropValue> dropValues) { }

        /// <summary>
        ///     在主界面初始化后执行，你可以通过这个给主界面加些你想要的按钮或者文字等
        ///     开发者也可以使用BeforeWidgetUpdate和AfterWidgetUpdate这两个接口实现更多种屏幕的局部调整
        /// </summary>
        /// <param name="mainMenuScreen">初始化完毕后的主界面</param>
        /// <param name="leftBottomBar">主界面左下角的按钮栏，里面有着API的切换语言和资源管理按钮</param>
        /// <param name="rightBottomBar">主界面右下角的按钮栏，Mod作者们可以在这里面放入想要的按钮（例如Mod设置按钮、Mod作者介绍按钮等）</param>
        public virtual void OnMainMenuScreenCreated(MainMenuScreen mainMenuScreen,
            StackPanelWidget leftBottomBar,
            StackPanelWidget rightBottomBar) { }

        /// <summary>
        ///     在主界面更新
        /// </summary>
        /// <param name="mainMenuScreen"></param>
        /// <param name="leftBottomBar">主界面左下角的按钮栏，里面有着API的切换语言和资源管理按钮</param>
        /// <param name="rightBottomBar">主界面右下角的按钮栏，Mod作者们可以在这里面放入想要的按钮（例如Mod设置按钮、Mod作者介绍按钮等）</param>
        [Obsolete("Use OnMainMenuScreenCreated instead.")]
        public virtual void OnMainMenuScreenUpdate(MainMenuScreen mainMenuScreen, StackPanelWidget leftBottomBar, StackPanelWidget rightBottomBar) { }

        /// <summary>
        ///     在计算一个生物的Factors执行。仅限非玩家生物，玩家需要使用OnLevelUpdate。
        ///     ComponentFactors里面有四个List，这四个List储存了所有对该属性有影响的因子。模组可以修改其中的因子来实现自定义的操作。
        ///     <code>
        /// public List&lt;Factor&gt; m_strengthFactors = [];
        /// public List&lt;Factor&gt; m_speedFactors = [];
        /// public List&lt;Factor&gt; m_hungerFactors = [];
        /// public List&lt;Factor&gt; m_resilienceFactors = [];
        /// </code>
        ///     在执行这个接口后，游戏会根据这四个List计算出StrengthFactor、SpeedFactor、HungerFactor、ResilienceFactor的最终值
        /// </summary>
        /// <param name="componentFactors"></param>
        [Obsolete("You can update your factors in your mod components.")]
        public virtual void OnFactorsGenerate(ComponentFactors componentFactors) { }

        /// <summary>
        ///     计算护甲防御时，修改参与护甲防御的衣物列表
        /// </summary>
        /// <param name="componentClothing">挨打者的护甲组件</param>
        /// <param name="attackment">导致该护甲结算的攻击</param>
        /// <param name="randomNumber">原版用来计算损伤部位的随机数</param>
        /// <param name="clothesList">参与护甲防御的衣物列表</param>
        public virtual void DecideArmorProtectionSequence(ComponentClothing componentClothing,
            Attackment attackment,
            float randomNumber,
            List<int> clothesList) { }

        /// <summary>
        ///     在计算护甲防御前，自定义新的结算
        ///     例如：玩家携带力场护盾时，优先使用力场护盾进行防御
        /// </summary>
        /// <param name="componentClothing">挨打者的护甲组件</param>
        /// <param name="attackment">导致该护甲结算的攻击</param>
        /// <param name="attackPowerAfterProtection">结算护甲后的攻击力</param>
        public virtual void ApplyProtectionBeforeClothes(ComponentClothing componentClothing,
            Attackment attackment,
            ref float attackPowerAfterProtection) { }

        /// <summary>
        ///     在计算护甲防御后，自定义新的结算
        /// </summary>
        /// <param name="componentClothing">挨打者的护甲组件</param>
        /// <param name="attackment">导致该护甲结算的攻击</param>
        /// <param name="listAfterProtection"></param>
        /// <param name="attackPowerAfterProtection">结算护甲后的攻击力</param>
        public virtual void ApplyProtectionAfterClothes(ComponentClothing componentClothing,
            Attackment attackment,
            List<int> listAfterProtection,
            ref float attackPowerAfterProtection) { }

        /// <summary>
        ///     初始化ClothingSlot的时候执行。在此接口添加脖子、手臂等新的可穿戴部位。
        /// </summary>
        public virtual void InitializeClothingSlots() { }

        /// <summary>
        ///     在绘制人物界面中的人物模型的时候执行，可以通过在playerModelWidget.m_modelWidget中增删元素来在人物界面绘制新模型
        /// </summary>
        /// <param name="playerModelWidget"></param>
        public virtual void OnPlayerModelWidgetMeasureOverride(PlayerModelWidget playerModelWidget) { }

        /// <summary>
        ///     设定是否显示默认的十字准星。可用于关闭PC端十字准星的强制显示
        /// </summary>
        /// <param name="componentAimingSights"></param>
        /// <param name="isVisible"></param>
        public virtual void IsCrosshairVisible(ComponentAimingSights componentAimingSights, ref bool isVisible) { }

        /// <summary>
        ///     用于添加新的键盘鼠标按键映射，在重置按键设置时也会使用这里的设置作为默认值。
        ///     使用yield return可返回多个键值对。键：映射名；值：按键(Key)或鼠标按钮(MouseButton)
        /// </summary>
        public virtual IEnumerable<KeyValuePair<string, object>> GetKeyboardMappings() {
            yield break;
        }
        /// <summary>
        ///     用于添加新的手柄按键映射，在重置按键设置时也会使用这里的设置作为默认值。
        ///     使用yield return可返回多个键值对。键：映射名；值：按键(Key)或鼠标按钮(MouseButton)
        /// </summary>
        public virtual IEnumerable<KeyValuePair<string, object>> GetGamepadMappings() {
            ////组合键添加示例：
            ////注：ModifierKey只能是GamePadButton.LeftShoulder或RightShoulder，以及GamePadTrigger.Left或Right
            ////在组合键中ActionKey可以是四个ModifierKey以外的任意按键
            //ValuesDictionary valuesDictionary = [];
            //valuesDictionary.SetValue("ModifierKey", Engine.Input.GamePadButton.LeftShoulder);
            //valuesDictionary.SetValue("ActionKey", Engine.Input.GamePadButton.X);
            //yield return new("TimeOfDay", valuesDictionary);
            yield break;
        }

        /// <summary>
        ///     用于添加新的摄像机顺序，在重置摄像机列表时也会使用这里的设置作为默认值。
        ///     使用yield return可返回多个键值对。键：摄像机类名；值：顺序
        /// </summary>
        public virtual IEnumerable<KeyValuePair<string, int>> GetCameraList() {
            yield break;
        }

        /// <summary>
        ///     初始化按键兼容组
        /// </summary>
        public virtual void InitKeyCompatibleGroups() { }

        /// <summary>
        ///     在设置界面创建时执行。可用于模组添加新的设置按钮。
        ///     此接口可避免切换语言重新创建Screen导致按钮丢失的情况
        /// </summary>
        /// <param name="settingsScreen"></param>
        /// <param name="buttonsToAdd">键为按钮，值为按下后进行的操作</param>
        public virtual void OnSettingsScreenCreated(SettingsScreen settingsScreen, out Dictionary<ButtonWidget, Action> buttonsToAdd) {
            buttonsToAdd = [];
        }

        /// <summary>
        /// 数据驱动设置项值变更通知。idPath = 完整 path 去掉第一段 packageName。
        /// 模组用于"值变了要立即处理"（如改 PBR 着色器质量要重建）。常规读取用 ModSettingsManager.Get&lt;T&gt;、TryGet&lt;T&gt;。
        /// </summary>
        public virtual void OnModSettingChanged(string[] idPath, object value) { }

        /// <summary>
        ///     对单条日志消息进行判定以修改其显示颜色
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public virtual void ModifyLogColor(string text, ref Color color) { }

        /// <summary>
        ///     PlayerData首次更新时执行
        /// </summary>
        /// <param name="playerData"></param>
        public virtual void PlayerDataFirstUpdate(PlayerData playerData) { }

        /// <summary>
        ///     重新计算摄像机投影矩阵
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="projectionMatrix"></param>
        public virtual void RecalculateCameraProjection(Camera camera, ref Matrix projectionMatrix) { }

        /// <summary>
        ///     射弹更新寻找下一个可以命中的Body时执行
        /// </summary>
        /// <param name="componentBody">射弹可能命中的Body</param>
        /// <param name="projectile">触发判定的射弹</param>
        /// <param name="ignore">设置为true会让射弹无视此Body</param>
        /// <param name="distance">射弹和目标的在本帧更新中的距离</param>
        /// 也可以通过将componentBody列入projectile.BodiesToIgnore中，以忽略目标
        public virtual void OnProjectileRaycastBody(ComponentBody componentBody, Projectile projectile, float distance, out bool ignore) {
            ignore = false;
        }

        /// <summary>
        ///     将物品拖动到衣物界面玩家身上时执行
        /// </summary>
        /// <param name="componentClothing"></param>
        /// <param name="slotIndex">拖动到的栏位（头部、躯干、护腿、脚）</param>
        /// <param name="inputValue">拖动过来的方块的方块完整值。修改会改动移交给下一个模组的完整值</param>
        /// <param name="inputCount">拖动过来的方块的数量。修改会改动移交给下一个模组的数量</param>
        /// <param name="processedValue">执行完毕后，留下的物品的Value。例如奶桶在食用完毕后会留下空桶。</param>
        /// <param name="processedCount">执行完毕后，留下的物品的数量。</param>
        /// <param name="skippedByOtherMods">是否已经被其他模组接管执行</param>
        /// <param name="skipVanilla">是否由本模组接管执行。为保证模组间兼容性，建议只在处理本模组物品时，才将skipVanilla设置为true或改动inputValue和inputCount。</param>
        public virtual void ClothingProcessSlotItems(ComponentClothing componentClothing,
            int slotIndex,
            ref int inputValue,
            ref int inputCount,
            ref int processedValue,
            ref int processedCount,
            bool skippedByOtherMods,
            out bool skipVanilla) {
            skipVanilla = false;
        }

        [Obsolete(
            "该接口有误，建议使用另一个同名接口ClothingProcessSlotItems(ComponentClothing componentClothing,int slotIndex,ref int inputValue,ref int inputCount,ref int processedValue,ref int processedCount,bool skippedByOtherMods,out bool skipVanilla)"
        )]
        public virtual void ClothingProcessSlotItems(ComponentPlayer componentPlayer,
            int slotIndex,
            ref int inputValue,
            ref int inputCount,
            ref int processedValue,
            ref int processedCount,
            bool skippedByOtherMods,
            out bool skipVanilla) {
            skipVanilla = false;
        }

        /// <summary>
        ///     当单个模型数据被渲染时执行
        /// </summary>
        /// <param name="modelData"></param>
        /// <param name="modelShader"></param>
        /// <param name="camera"></param>
        /// <param name="subsystemModelsRenderer"></param>
        /// <param name="skip"></param>
        public virtual void OnModelDataDrawing(SubsystemModelsRenderer.ModelData modelData,
            ModelShader modelShader,
            Camera camera,
            SubsystemModelsRenderer subsystemModelsRenderer,
            out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     当第一人称手部模型或手持方块图标被渲染时执行
        /// </summary>
        /// <param name="componentFirstPersonModel"></param>
        /// <param name="camera"></param>
        /// <param name="itemValue">手持方块完整值，为0表示空手</param>
        /// <param name="matrix"></param>
        /// <param name="skip"></param>
        public virtual void OnFirstPersonModelDrawing(ComponentFirstPersonModel componentFirstPersonModel,
            Camera camera,
            int itemValue,
            ref Matrix matrix,
            out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     当玩家开始交互一个带有SubsystemEntityBlockBehavior的方块时执行
        ///     可以用于当玩家打开一个通过地形生成，导致没有ComponentBlockEntity的箱子时，添加宝箱实体并为箱子添加战利品
        ///     该接口可能会引发模组冲突，谨慎使用
        /// </summary>
        /// <param name="raycastResult">可能的类型为TerrainRaycastResult或MovingBlockRaycastResult</param>
        /// <param name="isInitialNull">表示在开始交互方块时，方块并没有对应的交互实体。通常在首次交互野生箱子时发生</param>
        /// <param name="componentBlockEntity">在交互时指向的ComponentBlockEntity，可能为null</param>
        public virtual void OnEntityBehaviorBlockInteracted(object raycastResult,
            bool isInitialNull,
            ref ComponentBlockEntity componentBlockEntity) { }

        /// <summary>
        ///     在BlockMesh.AppendModelMeshPart之前执行
        /// </summary>
        /// <param name="blockMesh"></param>
        /// <param name="meshPart"></param>
        /// <param name="matrix"></param>
        /// <param name="makeEmissive"></param>
        /// <param name="flipWindingOrder"></param>
        /// <param name="doubleSided"></param>
        /// <param name="flipNormals"></param>
        /// <param name="color"></param>
        /// <param name="skip">如果要跳过原版方法，设置此值为true</param>
        public virtual void OnAppendModelMeshPart(BlockMesh blockMesh,
            ModelMeshPart meshPart,
            Matrix matrix,
            bool makeEmissive,
            bool flipWindingOrder,
            bool doubleSided,
            bool flipNormals,
            Color color,
            out bool skip) {
            skip = false;
        }

        /// <summary>
        ///     在BlockMesh.AppendModelMesh之前执行
        /// </summary>
        /// <param name="blockMesh"></param>
        /// <param name="blockMesh1"></param>
        /// <param name="skip">如果要跳过原版方法，设置此值为true</param>
        public virtual void OnAppendModelMesh(BlockMesh blockMesh, BlockMesh blockMesh1, out bool skip) {
            skip = false;
        }

        /// <summary>
        /// 玩家生命状态（进食）钩子
        ///
        /// 说明：该 Hook 在人物尝试进食时调用。参数 <paramref name="value"/> 可被修改，
        /// 且原版实现会使用最终的（被修改后的）值继续执行进食逻辑。
        /// 如果将 <paramref name="skipVanilla"/> 设为 true，vanilla 将不再执行后续进食逻辑，
        /// 此时调用者应当通过 <paramref name="eatSuccess"/> 指示进食是否成功，
        /// 并在需要时自行修改组件状态（例如 `vitalStats.Food`、`vitalStats.m_satiation` 等）。
        /// 多个模组按加载顺序依次调用，后面的修改会覆盖前面的修改。
        /// </summary>
        /// <param name="vitalStats">生命状态组件</param>
        /// <param name="value">物品完整值（ref，可被修改，vanilla 会使用最终值）</param>
        /// <param name="skipVanilla">是否跳过原版逻辑（ref，true 表示已接管）</param>
        /// <param name="eatSuccess">输出：进食是否成功</param>
        public virtual void OnVitalStatsEat(
            ComponentVitalStats vitalStats,
            ref int value,
            ref bool skipVanilla,
            out bool eatSuccess) {
            // 默认返回进食失败，模组可重写修改
            eatSuccess = false;
        }

        /// <summary>
        /// 状态（更新饥饿值）钩子
        ///
        /// 说明：在 UpdateFood 调用前触发。模组可以修改 <paramref name="food"/> 和 <paramref name="gameTimeDelta"/>。
        /// - 如果任一模组将 <paramref name="skipVanilla"/> 设为 true，原版的 UpdateFood 将被跳过，
        ///   此时模组应负责把需要的值写入组件（例如 `vitalStats.Food = food`）。
        /// - 如果所有模组都未设置 skip（即 skipVanilla 保持 false），引擎将在 Hook 返回后把
        ///   最终的 <paramref name="food"/> 写回到 `vitalStats.Food`，然后继续执行原版更新逻辑。
        /// 注意：多个模组按加载顺序依次调用，最后的修改生效；<paramref name="gameTimeDelta"/> 为 ref，
        /// 可用于改变原版中基于时间的计算速度。
        /// </summary>
        /// <param name="vitalStats">生命状态组件</param>
        /// <param name="food">饥饿值（ref，修改在未 skip 时会被写回）</param>
        /// <param name="gameTimeDelta">时间增量（ref，可修改以影响后续 vanilla 计算）</param>
        /// <param name="skipVanilla">是否跳过原版逻辑（ref，true 表示已接管）</param>
        public virtual void OnVitalStatsUpdateFood(
            ComponentVitalStats vitalStats,
            ref float food,
            ref float gameTimeDelta,
            ref bool skipVanilla) { }

        /// <summary>
        /// 状态（更新耐力值）钩子
        ///
        /// 说明：在 UpdateStamina 调用前触发。模组可以修改 <paramref name="stamina"/> 和 <paramref name="gameTimeDelta"/>。
        /// - 如果任一模组将 <paramref name="skipVanilla"/> 设为 true，原版的 UpdateStamina 将被跳过，
        ///   此时模组应负责把需要的值写入组件（例如 `vitalStats.Stamina = stamina`）。
        /// - 如果所有模组都未设置 skip（即 skipVanilla 保持 false），引擎将在 Hook 返回后把
        ///   最终的 <paramref name="stamina"/> 写回到 `vitalStats.Stamina`，然后继续执行原版更新逻辑。
        /// 注意：多个模组按加载顺序依次调用，最后的修改生效；<paramref name="gameTimeDelta"/> 为 ref，
        /// 可用于改变原版中基于时间的计算速度。
        /// </summary>
        /// <param name="vitalStats">生命状态组件</param>
        /// <param name="stamina">耐力值（ref，修改在未 skip 时会被写回）</param>
        /// <param name="gameTimeDelta">时间增量（ref，可修改以影响后续 vanilla 计算）</param>
        /// <param name="skipVanilla">是否跳过原版逻辑（ref，true 表示已接管）</param>
        public virtual void OnVitalStatsUpdateStamina(
            ComponentVitalStats vitalStats,
            ref float stamina,
            ref float gameTimeDelta,
            ref bool skipVanilla) { }

        /// <summary>
        /// 状态（更新睡眠值）钩子
        ///
        /// 说明：在 UpdateSleep 调用前触发。模组可以修改 <paramref name="sleep"/> 和 <paramref name="gameTimeDelta"/>。
        /// - 如果任一模组将 <paramref name="skipVanilla"/> 设为 true，原版的 UpdateSleep 将被跳过，
        ///   此时模组应负责把需要的值写入组件（例如 `vitalStats.Sleep = sleep`）。
        /// - 如果所有模组都未设置 skip（即 skipVanilla 保持 false），引擎将在 Hook 返回后把
        ///   最终的 <paramref name="sleep"/> 写回到 `vitalStats.Sleep`，然后继续执行原版更新逻辑。
        /// 注意：多个模组按加载顺序依次调用，最后的修改生效；<paramref name="gameTimeDelta"/> 为 ref，
        /// 可用于改变原版中基于时间的计算速度。
        /// </summary>
        /// <param name="vitalStats">生命状态组件</param>
        /// <param name="sleep">睡眠值（ref，修改在未 skip 时会被写回）</param>
        /// <param name="gameTimeDelta">时间增量（ref，可修改以影响后续 vanilla 计算）</param>
        /// <param name="skipVanilla">是否跳过原版逻辑（ref，true 表示已接管）</param>
        public virtual void OnVitalStatsUpdateSleep(
            ComponentVitalStats vitalStats,
            ref float sleep,
            ref float gameTimeDelta,
            ref bool skipVanilla) { }

        /// <summary>
        /// 状态（更新体温）钩子
        ///
        /// 说明：在 UpdateTemperature 调用前触发。模组可以修改 <paramref name="temperature"/> 和 <paramref name="gameTimeDelta"/>。
        /// - 如果任一模组将 <paramref name="skipVanilla"/> 设为 true，原版的 UpdateTemperature 将被跳过，
        ///   此时模组应负责把需要的值写入组件（例如 `vitalStats.Temperature = temperature`）。
        /// - 如果所有模组都未设置 skip（即 skipVanilla 保持 false），引擎将在 Hook 返回后把
        ///   最终的 <paramref name="temperature"/> 写回到 `vitalStats.Temperature`，然后继续执行原版更新逻辑。
        /// 注意：多个模组按加载顺序依次调用，最后的修改生效；<paramref name="gameTimeDelta"/> 为 ref，
        /// 可用于改变原版中基于时间的计算速度。
        /// </summary>
        /// <param name="vitalStats">生命状态组件</param>
        /// <param name="temperature">体温值（ref，修改在未 skip 时会被写回）</param>
        /// <param name="gameTimeDelta">时间增量（ref，可修改以影响后续 vanilla 计算）</param>
        /// <param name="skipVanilla">是否跳过原版逻辑（ref，true 表示已接管）</param>
        public virtual void OnVitalStatsUpdateTemperature(
            ComponentVitalStats vitalStats,
            ref float temperature,
            ref float gameTimeDelta,
            ref bool skipVanilla) { }

        /// <summary>
        /// 状态（更新湿度）钩子
        ///
        /// 说明：在 UpdateWetness 调用前触发。模组可以修改 <paramref name="wetness"/> 和 <paramref name="gameTimeDelta"/>。
        /// - 如果任一模组将 <paramref name="skipVanilla"/> 设为 true，原版的 UpdateWetness 将被跳过，
        ///   此时模组应负责把需要的值写入组件（例如 `vitalStats.Wetness = wetness`）。
        /// - 如果所有模组都未设置 skip（即 skipVanilla 保持 false），引擎将在 Hook 返回后把
        ///   最终的 <paramref name="wetness"/> 写回到 `vitalStats.Wetness`，然后继续执行原版更新逻辑。
        /// 注意：多个模组按加载顺序依次调用，最后的修改生效；<paramref name="gameTimeDelta"/> 为 ref，
        /// 可用于改变原版中基于时间的计算速度。
        /// </summary>
        /// <param name="vitalStats">生命状态组件</param>
        /// <param name="wetness">湿度值（ref，修改在未 skip 时会被写回）</param>
        /// <param name="gameTimeDelta">时间增量（ref，可修改以影响后续 vanilla 计算）</param>
        /// <param name="skipVanilla">是否跳过原版逻辑（ref，true 表示已接管）</param>
        public virtual void OnVitalStatsUpdateWetness(
            ComponentVitalStats vitalStats,
            ref float wetness,
            ref float gameTimeDelta,
            ref bool skipVanilla) { }

        /// <summary>
        /// 屏幕切换钩子
        /// </summary>
        /// <param name="screen">将切换到的屏幕；设置为 null 将不进行切换</param>
        /// <param name="parameters">传递给屏幕的参数</param>
        public virtual void OnSwitchScreen(ref Screen screen, object[] parameters) { }

        /// <summary>
        /// 显示对话框钩子
        /// </summary>
        /// <param name="parentWidget">对话框的父容器</param>
        /// <param name="dialog">将显示的对话框；设置为 null 将不进行显示</param>
        public virtual void OnShowDialog(ref ContainerWidget parentWidget, ref Dialog dialog) { }

        /// <summary>
        /// 隐藏对话框钩子
        /// </summary>
        /// <param name="dialog">将隐藏的对话框；设置为 null 将不进行隐藏</param>
        public virtual void OnHideDialog(ref Dialog dialog) { }

        /// <summary>
        /// 当生物脚步声事件触发时执行
        /// </summary>
        /// <param name="componentCreature">触发脚步声的生物</param>
        /// <param name="parameter">事件参数（可自定义）</param>
        public virtual void OnCreatureFootstep(ComponentCreature componentCreature, object parameter) { }

        /// <summary>
        /// 当生物攻击命中事件触发时执行
        /// </summary>
        /// <param name="componentCreature">攻击的生物</param>
        /// <param name="parameter">事件参数（可自定义）</param>
        public virtual void OnCreatureAttackHit(ComponentCreature componentCreature, object parameter) { }

        /// <summary>
        /// 当生物攻击开始事件触发时执行
        /// </summary>
        /// <param name="componentCreature">攻击的生物</param>
        /// <param name="parameter">事件参数（可自定义）</param>
        public virtual void OnCreatureAttackStart(ComponentCreature componentCreature, object parameter) { }

        /// <summary>
        /// 当生物攻击结束事件触发时执行
        /// </summary>
        /// <param name="componentCreature">攻击的生物</param>
        /// <param name="parameter">事件参数（可自定义）</param>
        public virtual void OnCreatureAttackEnd(ComponentCreature componentCreature, object parameter) { }
    }
}