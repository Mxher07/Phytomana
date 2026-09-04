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

        /// <summary>产魔源与接收器间的投递距离（格）。</summary>
        public float TransferDistance = ManaNetworkManager.DefaultTransferDistance;

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

        public void Save(XElement element) {
            element.SetAttributeValue("TransferInterval", Format(TransferInterval));
            element.SetAttributeValue("TransferDistance", Format(TransferDistance));
            element.SetAttributeValue("FlowerTickInterval", Format(FlowerTickInterval));
            element.SetAttributeValue("FlowersPerSlice", FlowersPerSlice.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("SunPowerBaseManaRate", Format(SunPowerBaseManaRate));
            element.SetAttributeValue("SunPowerMaxMana", Format(SunPowerMaxMana));
            element.SetAttributeValue("WaterDonManaRate", Format(WaterDonManaRate));
            element.SetAttributeValue("WaterDonMaxMana", Format(WaterDonMaxMana));
        }

        public void Load(XElement element) {
            if (element == null) {
                return;
            }
            TransferInterval = Read(element, "TransferInterval", TransferInterval);
            TransferDistance = Read(element, "TransferDistance", TransferDistance);
            FlowerTickInterval = Read(element, "FlowerTickInterval", FlowerTickInterval);
            FlowersPerSlice = Read(element, "FlowersPerSlice", FlowersPerSlice);
            SunPowerBaseManaRate = Read(element, "SunPowerBaseManaRate", SunPowerBaseManaRate);
            SunPowerMaxMana = Read(element, "SunPowerMaxMana", SunPowerMaxMana);
            WaterDonManaRate = Read(element, "WaterDonManaRate", WaterDonManaRate);
            WaterDonMaxMana = Read(element, "WaterDonMaxMana", WaterDonMaxMana);
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
