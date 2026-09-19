using System.Xml.Linq;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;

namespace Phytomana {
    /// <summary>
    /// Phytomana 主入口（ModLoader）。
    /// 方块行为子系统由引擎按 xdb 注册并随每个世界实例化；
    /// 引擎钩子（OnMinerHit2/OnMinerDig/OnBlockDug/OnProjectileHitBody 等）经
    /// <see cref="TerraToolHooks"/> 与 <see cref="TerraSetHooks"/> 两个 ModLoader 子类转发给对应子系统。
    /// </summary>
    public class PhytomanaMod : ModLoader {
        public override void __ModInitialize() {
            ModsManager.RegisterHook("BlocksInitalized", this, 1);
            ModsManager.RegisterHook("OnProjectLoaded", this, 1);
            ModsManager.RegisterHook("OnTerrainContentsGenerated", this, 1);
            TerraToolHooks terraToolHooks = new();
            ModsManager.RegisterHook("OnMinerHit2", terraToolHooks, 1);
            ModsManager.RegisterHook("OnMinerDig", terraToolHooks, 1);
            ModsManager.RegisterHook("OnBlockDug", terraToolHooks, 1);
            ModsManager.RegisterHook("OnProjectileHitBody", terraToolHooks, 1);
            TerraSetHooks terraSetHooks = new();
            ModsManager.RegisterHook("OnMinerDig", terraSetHooks, 2);
        }

        public override void BlocksInitalized() {
            ManaBlockRegistry.Initialize();
            FlowerTableRecipeRegistry.Initialize(Entity);
            ManaPoolRecipeRegistry.Initialize(Entity);
            RunesRecipeRegistry.Initialize(Entity);
            PhytoRegistry.Initialize();
            SumeruPatchGenerator.Initialize();
        }

        public override void OnProjectLoaded(Project project) {
            ManaNetworkManager network = project.FindSubsystem<ManaNetworkManager>(false);
            FlowerTickScheduler scheduler = project.FindSubsystem<FlowerTickScheduler>(false);
            if (network == null || scheduler == null) {
                Log.Error("[PhytoMana]Critical subsystems missing (ManaNetwork / FlowerTickScheduler). Check PhytoManaDatabase.xdb.");
                return;
            }
            // 缓存世界种子供地形线程的花群生成使用（确定性推导，无共享可变状态）。
            SubsystemGameInfo gameInfo = project.FindSubsystem<SubsystemGameInfo>(false);
            if (gameInfo != null) {
                SumeruPatchGenerator.SetWorldSeed(gameInfo.WorldSettings.WorldSeed);
            }
            Log.Information("[PhytoMana]World runtime ready: mana network + flower scheduler loaded.");
        }

        /// <summary>全新区块地形内容生成完毕：种上须弥花群（存档加载的区块不会走这里）。</summary>
        public override void OnTerrainContentsGenerated(TerrainChunk chunk) {
            SumeruPatchGenerator.OnChunkGenerated(chunk);
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
