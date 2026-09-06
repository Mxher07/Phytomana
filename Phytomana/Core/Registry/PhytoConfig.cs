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
        }

        public static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);

        public static float Read(XElement element, string name, float defaultValue) {
            string text = (string)element.Attribute(name);
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value >= 0f
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
