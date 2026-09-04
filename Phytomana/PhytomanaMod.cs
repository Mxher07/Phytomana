using System.Xml.Linq;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// Phytomana 主入口（ModLoader）。初始化顺序：
    /// 1. 事件总线——静态随程序集就绪，模组卸载时清空订阅；
    /// 2./3. ManaNetworkManager 与 FlowerTickScheduler——由引擎按 xdb 注册、随每个世界实例化，
    ///       OnProjectLoaded 做存在性体检并确认世界运行时就绪；
    /// 4. PhytoRegistry——方块初始化完成（BlocksInitalized）时构建集中注册表。
    /// </summary>
    public class PhytomanaMod : ModLoader {
        public override void __ModInitialize() {
            ModsManager.RegisterHook("BlocksInitalized", this, 1);
            ModsManager.RegisterHook("OnProjectLoaded", this, 1);
        }

        public override void BlocksInitalized() {
            PhytoRegistry.Initialize();
        }

        public override void OnProjectLoaded(Project project) {
            ManaNetworkManager network = project.FindSubsystem<ManaNetworkManager>(false);
            FlowerTickScheduler scheduler = project.FindSubsystem<FlowerTickScheduler>(false);
            if (network == null || scheduler == null) {
                Log.Error("[PhytoMana]Critical subsystems missing (ManaNetwork / FlowerTickScheduler). Check PhytoManaDatabase.xdb.");
                return;
            }
            Log.Information("[PhytoMana]World runtime ready: mana network + flower scheduler loaded.");
        }

        public override void ModDispose() {
            PhytoEventBus.Clear();
        }

        public override void SaveSettings(XElement xElement) {
            PhytoConfig.Instance.Save(xElement);
        }

        public override void LoadSettings(XElement xElement) {
            PhytoConfig.Instance.Load(xElement);
        }
    }
}
