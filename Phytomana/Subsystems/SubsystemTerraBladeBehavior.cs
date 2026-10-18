using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Network;
using TemplatesDatabase;

namespace Game {
    /// <summary>
    /// 泰拉刃「使用」行为（参考火柴的 SubsystemMatchBlockBehavior.OnUse 路径）：
    /// 右键点击即触发，不要求准星命中任何目标（命中生物时原版挥击攻击照常结算，
    /// 本行为不拦截）。50% 概率发射一枚剑气（<see cref="TerraSwordQiProjectile"/>），
    /// 剑气命中生物 3.5 固定伤害、对玩家穿透。
    /// 发射逻辑经 <see cref="SubsystemTerraToolBehavior.TryFireBladeQi"/> 执行。
    /// 联机：客户端只把用法意图发给服务器，掷骰与发射在服务器权威执行并广播；
    /// 服务器对远程玩家右键的重放被抑制，避免与请求双重执行。
    /// </summary>
    public class SubsystemTerraBladeBehavior : SubsystemBlockBehavior {
        public SubsystemTerraToolBehavior m_terraTool;

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<TerraBladeBlock>()];

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_terraTool = Project.FindSubsystem<SubsystemTerraToolBehavior>(true);
        }

        /// <summary>
        /// 右键「使用」：无条件掷骰，50% 发射剑气。
        /// 指向生物时原版挥击攻击（OnMinerHit/OnMinerHit2）独立结算，不被本方法吞掉。
        /// </summary>
        public override bool OnUse(Ray3 ray, ComponentMiner componentMiner) {
            if (m_terraTool == null) {
                return false;
            }
            ComponentPlayer player = componentMiner.Entity?.FindComponent<ComponentPlayer>();
            bool isMain = player?.PlayerData?.IsMainPlayer ?? true;
            if (NetworkManager.IsServerRunning && !isMain) {
                return false;
            }
            if (NetworkManager.IsClientRunning) {
                // 联机客户端：只发请求，掷骰与发射由服务器权威执行并广播。
                m_terraTool.SendSwordQiRequest(ray);
            }
            else {
                m_terraTool.TryFireBladeQi(componentMiner, ray);
            }
            // 不拦截后续行为（如放置/交互），剑气发射是追加而非独占
            return false;
        }
    }
}
