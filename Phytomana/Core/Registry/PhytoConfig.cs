using System.Globalization;
using System.Xml.Linq;

namespace Phytomana {
    /// <summary>
    /// Phytomana 框架配置。经主入口的 SaveSettings/LoadSettings 由游戏持久化到模组设置文件，
    /// 调整产魔速率、魔力上限、网络与调度参数无需改源码。
    /// 全部字段带默认值；配置缺失或损坏时回落默认。
    /// </summary>
    public class PhytoConfig {
        public static PhytoConfig Instance { get; } = new();

        // ===== 魔力网络 =====

        /// <summary>网络投递周期（秒）。</summary>
        public float TransferInterval = ManaNetworkManager.DefaultTransferInterval;

        // ===== 花朵调度 =====

        /// <summary>花朵调度周期（秒）。</summary>
        public float FlowerTickInterval = FlowerTickScheduler.DefaultTickInterval;

        /// <summary>每次调度轮询的花朵数上限。</summary>
        public int FlowersPerSlice = FlowerTickScheduler.DefaultFlowersPerSlice;

        // ===== 日耀花 =====

        /// <summary>日耀花基础产魔速率。</summary>
        public float SunPowerBaseManaRate = TileSunPowerFlower.DefaultBaseManaRate;

        /// <summary>日耀花魔力上限。</summary>
        public float SunPowerMaxMana = TileSunPowerFlower.DefaultMaxMana;

        // ===== 泉沫珠 =====

        /// <summary>泉沫珠产魔速率。</summary>
        public float WaterDonManaRate = TileWaterDonFlower.DefaultManaRate;

        /// <summary>泉沫珠魔力上限。</summary>
        public float WaterDonMaxMana = TileWaterDonFlower.DefaultMaxMana;

        // ===== 夜影花 =====

        /// <summary>夜影花夜间产魔速率。</summary>
        public float NightshadeManaRate = TileNightshadeFlower.DefaultManaRate;

        /// <summary>夜影花魔力上限。</summary>
        public float NightshadeMaxMana = TileNightshadeFlower.DefaultMaxMana;

        // ===== 暴食花 =====

        /// <summary>暴食花每点营养价值产出的魔力。</summary>
        public float GourmaryllisManaPerNutrition = TileGourmaryllisFlower.DefaultManaPerNutrition;

        /// <summary>暴食花魔力上限。</summary>
        public float GourmaryllisMaxMana = TileGourmaryllisFlower.DefaultMaxMana;

        // ===== 热力百合 =====

        /// <summary>热力百合吞噬一格岩浆产出的魔力。</summary>
        public float ThermalilyManaPerMagma = TileThermalilyFlower.DefaultManaPerMagma;

        /// <summary>热力百合魔力上限。</summary>
        public float ThermalilyMaxMana = TileThermalilyFlower.DefaultMaxMana;

        // ===== 荆棘之花 =====

        /// <summary>荆棘之花魔力上限。</summary>
        public float ThornyRoseMaxMana = TileThornyRose.DefaultMaxMana;

        /// <summary>荆棘之花每次刺击消耗的魔力。</summary>
        public float ThornyRoseManaCost = TileThornyRose.DefaultManaCost;

        /// <summary>荆棘之花每次刺击造成的固定伤害（按 1 点 = 目标最大生命 10% 的点数刻度，2.5 点 = 25%）。</summary>
        public float ThornyRoseDamage = TileThornyRose.DefaultDamage;

        /// <summary>荆棘之花攻击范围的半边长（刺击 2×该值 边长的立方）。</summary>
        public float ThornyRoseAttackRange = TileThornyRose.DefaultAttackRange;

        /// <summary>荆棘之花从魔法池单次吸取的魔力。</summary>
        public float ThornyRosePoolDrawAmount = TileThornyRose.DefaultPoolDrawAmount;

        /// <summary>荆棘之花搜寻魔法池范围的半边长（2×该值 边长的立方）。</summary>
        public float ThornyRosePoolSearchRange = TileThornyRose.DefaultPoolSearchRange;

        // ===== 符文台 =====

        /// <summary>符文台魔力上限（需容纳最高 1200mn 的炼制消耗）。</summary>
        public float RunesTableMaxMana = 2000f;

        // ===== 魔力发射器 =====

        /// <summary>发射器魔力上限（此前硬编码在 SubsystemMana）。</summary>
        public float SpreaderMaxMana = 1200f;

        // ===== 魔力石板 =====

        /// <summary>魔力石板空板丢入魔法池每 1s 吸走的魔量（此前硬编码 500）。</summary>
        public float TabletPoolDrainPerSecond = 500f;

        /// <summary>魔力石板充能器：修复一件魔力钢工具/装备 1 点耐久的耗魔（镐/弯刀/斧）。</summary>
        public float TabletRepairCostTool = 52f;

        /// <summary>魔力石板充能器：修复护甲（胸甲/护腿）1 点耐久的耗魔。</summary>
        public float TabletRepairCostArmor = 45f;

        /// <summary>魔力石板充能器：修复其余（铲/头盔/靴子）1 点耐久的耗魔。</summary>
        public float TabletRepairCostOther = 35f;

        // ===== 世界生成（须弥花群） =====

        /// <summary>每区块生成须弥小散群的概率（百分比，默认 12）。</summary>
        public int SumeruSmallGroupChancePercent = 12;

        /// <summary>大花群数量分布：roll &lt; 该值 → 0 群，&lt; 下一阈值 → 1 群，否则 2 群。</summary>
        public int SumeruBigPatchZeroRoll = 30;

        public int SumeruBigPatchOneRoll = 75;

        // ===== 泰拉装备 =====

        /// <summary>泰拉工具自然魔力修复耗魔（每点耐久，默认 60）。</summary>
        public float TerraEquipRepairCost = 60f;

        /// <summary>泰拉套装：工具魔力修复耗魔减免（默认 0.2 = 20%）。</summary>
        public float TerraToolRepairDiscount = 0.2f;

        /// <summary>魔力钢套装：魔力修复耗魔减免（默认 0.45 = 45%）。</summary>
        public float ManaSteelRepairDiscount = 0.45f;

        // ===== 泰拉破坏者 =====

        /// <summary>泰拉破坏者各级所需魔力：索引 = 等级（D=0 无需求，C=1..SS=5）。</summary>
        public float TerraBreakerLevelMana = 0f;

        public float TerraBreakerLevelCMana = 380f;

        public float TerraBreakerLevelBMana = 3800f;

        public float TerraBreakerLevelAMana = 38000f;

        public float TerraBreakerLevelSMana = 380000f;

        public float TerraBreakerLevelSSMana = 3800000f;

        /// <summary>泰拉破坏者每额外挖掘一格扣魔。</summary>
        public float TerraBreakerPerBlockCost = 7f;

        /// <summary>泰拉破坏者魔力池充能速率（1000mn/s）。</summary>
        public float TerraBreakerPoolChargeRate = 1000f;

        // ===== 荆棘之刺 =====

        /// <summary>荆棘之刺秒杀阈值：最大生命低于该值（即 Injure 百分比）的目标被一击致死。</summary>
        public float ThornyRoseKillThreshold = 0.25f;

        /// <summary>荆棘之刺是否默认开机（存档缺省值）。</summary>
        public bool ThornyRoseDefaultPowered = true;

        public void Save(XElement element) {
            element.SetAttributeValue("TransferInterval", Format(TransferInterval));
            element.SetAttributeValue("FlowerTickInterval", Format(FlowerTickInterval));
            element.SetAttributeValue("FlowersPerSlice", FlowersPerSlice.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("SunPowerBaseManaRate", Format(SunPowerBaseManaRate));
            element.SetAttributeValue("SunPowerMaxMana", Format(SunPowerMaxMana));
            element.SetAttributeValue("WaterDonManaRate", Format(WaterDonManaRate));
            element.SetAttributeValue("WaterDonMaxMana", Format(WaterDonMaxMana));
            element.SetAttributeValue("NightshadeManaRate", Format(NightshadeManaRate));
            element.SetAttributeValue("NightshadeMaxMana", Format(NightshadeMaxMana));
            element.SetAttributeValue("GourmaryllisManaPerNutrition", Format(GourmaryllisManaPerNutrition));
            element.SetAttributeValue("GourmaryllisMaxMana", Format(GourmaryllisMaxMana));
            element.SetAttributeValue("ThermalilyManaPerMagma", Format(ThermalilyManaPerMagma));
            element.SetAttributeValue("ThermalilyMaxMana", Format(ThermalilyMaxMana));
            element.SetAttributeValue("ThornyRoseMaxMana", Format(ThornyRoseMaxMana));
            element.SetAttributeValue("ThornyRoseManaCost", Format(ThornyRoseManaCost));
            element.SetAttributeValue("ThornyRoseDamage", Format(ThornyRoseDamage));
            element.SetAttributeValue("ThornyRoseAttackRange", Format(ThornyRoseAttackRange));
            element.SetAttributeValue("ThornyRosePoolDrawAmount", Format(ThornyRosePoolDrawAmount));
            element.SetAttributeValue("ThornyRosePoolSearchRange", Format(ThornyRosePoolSearchRange));
            element.SetAttributeValue("RunesTableMaxMana", Format(RunesTableMaxMana));
            element.SetAttributeValue("SpreaderMaxMana", Format(SpreaderMaxMana));
            element.SetAttributeValue("TabletPoolDrainPerSecond", Format(TabletPoolDrainPerSecond));
            element.SetAttributeValue("TabletRepairCostTool", Format(TabletRepairCostTool));
            element.SetAttributeValue("TabletRepairCostArmor", Format(TabletRepairCostArmor));
            element.SetAttributeValue("TabletRepairCostOther", Format(TabletRepairCostOther));
            element.SetAttributeValue("SumeruSmallGroupChancePercent", SumeruSmallGroupChancePercent.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("SumeruBigPatchZeroRoll", SumeruBigPatchZeroRoll.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("SumeruBigPatchOneRoll", SumeruBigPatchOneRoll.ToString(CultureInfo.InvariantCulture));
        }

        public void Load(XElement element) {
            if (element == null) {
                return;
            }
            TransferInterval = Read(element, "TransferInterval", TransferInterval);
            FlowerTickInterval = Read(element, "FlowerTickInterval", FlowerTickInterval);
            FlowersPerSlice = Read(element, "FlowersPerSlice", FlowersPerSlice);
            SunPowerBaseManaRate = Read(element, "SunPowerBaseManaRate", SunPowerBaseManaRate);
            SunPowerMaxMana = Read(element, "SunPowerMaxMana", SunPowerMaxMana);
            WaterDonManaRate = Read(element, "WaterDonManaRate", WaterDonManaRate);
            WaterDonMaxMana = Read(element, "WaterDonMaxMana", WaterDonMaxMana);
            NightshadeManaRate = Read(element, "NightshadeManaRate", NightshadeManaRate);
            NightshadeMaxMana = Read(element, "NightshadeMaxMana", NightshadeMaxMana);
            GourmaryllisManaPerNutrition = Read(element, "GourmaryllisManaPerNutrition", GourmaryllisManaPerNutrition);
            GourmaryllisMaxMana = Read(element, "GourmaryllisMaxMana", GourmaryllisMaxMana);
            ThermalilyManaPerMagma = Read(element, "ThermalilyManaPerMagma", ThermalilyManaPerMagma);
            ThermalilyMaxMana = Read(element, "ThermalilyMaxMana", ThermalilyMaxMana);
            ThornyRoseMaxMana = Read(element, "ThornyRoseMaxMana", ThornyRoseMaxMana);
            ThornyRoseManaCost = Read(element, "ThornyRoseManaCost", ThornyRoseManaCost);
            ThornyRoseDamage = Read(element, "ThornyRoseDamage", ThornyRoseDamage);
            ThornyRoseAttackRange = Read(element, "ThornyRoseAttackRange", ThornyRoseAttackRange);
            ThornyRosePoolDrawAmount = Read(element, "ThornyRosePoolDrawAmount", ThornyRosePoolDrawAmount);
            ThornyRosePoolSearchRange = Read(element, "ThornyRosePoolSearchRange", ThornyRosePoolSearchRange);
            RunesTableMaxMana = Read(element, "RunesTableMaxMana", RunesTableMaxMana);
            SpreaderMaxMana = Read(element, "SpreaderMaxMana", SpreaderMaxMana);
            TabletPoolDrainPerSecond = Read(element, "TabletPoolDrainPerSecond", TabletPoolDrainPerSecond);
            TabletRepairCostTool = Read(element, "TabletRepairCostTool", TabletRepairCostTool);
            TabletRepairCostArmor = Read(element, "TabletRepairCostArmor", TabletRepairCostArmor);
            TabletRepairCostOther = Read(element, "TabletRepairCostOther", TabletRepairCostOther);
            SumeruSmallGroupChancePercent = ReadInt(element, "SumeruSmallGroupChancePercent", SumeruSmallGroupChancePercent);
            SumeruBigPatchZeroRoll = ReadInt(element, "SumeruBigPatchZeroRoll", SumeruBigPatchZeroRoll);
            SumeruBigPatchOneRoll = ReadInt(element, "SumeruBigPatchOneRoll", SumeruBigPatchOneRoll);
        }

        public static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);

        public static float Read(XElement element, string name, float defaultValue) {
            string text = (string)element.Attribute(name);
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value >= 0f
                ? value
                : defaultValue;
        }

        /// <summary>读取非负整数配置（区别于 Read 的正整数校验，允许 0，供世界生成阈值使用）。</summary>
        public static int ReadInt(XElement element, string name, int defaultValue) {
            string text = (string)element.Attribute(name);
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value >= 0
                ? value
                : defaultValue;
        }

        public static int Read(XElement element, string name, int defaultValue) {
            string text = (string)element.Attribute(name);
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0
                ? value
                : defaultValue;
        }
    }
}
