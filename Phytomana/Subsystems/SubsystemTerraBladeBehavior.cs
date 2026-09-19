using Engine;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    /// <summary>
    /// 泰拉刃「使用」行为（参考火柴的 SubsystemMatchBlockBehavior.OnUse 路径）：
    /// 右键点击即触发，不要求准星命中任何目标（命中生物时原版挥击攻击照常结算，
    /// 本行为不拦截）。50% 概率发射一枚剑气（<see cref="TerraSwordQiProjectile"/>），
    /// 剑气命中生物 3.5 固定伤害、对玩家穿透。
    /// 发射逻辑经 <see cref="SubsystemTerraToolBehavior.TryFireBladeQi"/> 执行。
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
            m_terraTool.TryFireBladeQi(componentMiner, ray);
            // 不拦截后续行为（如放置/交互），剑气发射是追加而非独占
            return false;
        }
    }
}
