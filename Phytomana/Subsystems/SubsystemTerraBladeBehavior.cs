using Engine.Core;
using Game;
using EntitySystem.Core;
using EntitySystem.TemplatesDatabase;

namespace Game {
    /// <summary>
    /// 泰拉刃「使用」行为（参考火柴的 SubsystemMatchBlockBehavior.OnUse 路径）：
    /// 右键点击即触发，不要求准星命中任何目标（命中生物时原版挥击攻击照常结算，
    /// 本行为不拦截）。50% 概率发射一枚剑气（<see cref="TerraSwordQiProjectile"/>），
    /// 剑气命中生物 3.5 固定伤害、对玩家穿透，命中方块或生物后立即消散
    /// （宿主对射弹所属方块派发 OnHitAsProjectile，返回 true 即标记移除）。
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

        /// <summary>
        /// 剑气命中（方块或生物）：立即消散（不破格、不卡入、不转掉落物）。
        /// 宿主在弹道命中时对本方块（射弹所属方块）派发该行为，返回 true 即标记移除；
        /// 伤害结算仍由 OnProjectileHitBody 钩子独立完成。非剑气射弹不接管。
        /// </summary>
        public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem) {
            if (!(worldItem is TerraSwordQiProjectile qi)) {
                return false;
            }
            qi.Die(m_terraTool?.m_subsystemParticles);
            return true;
        }
    }
}
