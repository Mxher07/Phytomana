using Engine.Animation;
using GameEntitySystem;

namespace Game {
    /// <summary>
    /// 动画参与者组件基类。挂到任意实体，由同实体的 ComponentModel 在 OnEntityAdded 收集，
    /// 参与 AnimationController 的参数同步、事件处理、控制器就绪通知。
    /// 组合式扩展，替代"继承并 override 模型组件"。
    /// </summary>
    /// <remarks>
    /// 生命周期（由 ComponentModel 驱动）：
    /// - ShouldApplyTo：ComponentModel 收集参与者时调用，决定是否纳入该 model 的转发列表。
    /// - OnControllerCreated：AnimationController 创建/重建后调用，用于设置初始参数。
    /// - SyncAnimationParameters：每帧动画更新前（controller.Update 之前）调用，用于同步参数驱动状态规则。
    /// - HandleAnimationEvent：动画事件触发时（含 onComplete trigger）转发。
    /// 派生类按需 override；不缓存常用依赖，自行 Entity.FindComponent 取。
    /// 多 model 实体：每个 ComponentModel 独立收集参与者。若参与者 Sync 有状态推进，
    /// 须 override ShouldApplyTo 限定到单一 model，避免一帧被调多次导致状态机错乱。
    /// </remarks>
    public abstract class ComponentAnimationParticipant : Component {
        /// <summary>是否纳入指定 model 的参与者列表。默认 true（所有 model）。
        /// 实体含多个带 controller 的 model 且参与者有状态时，override 限定到目标 model。
        /// 仅在 OnEntityAdded 收集时评估一次。</summary>
        public virtual bool ShouldApplyTo(ComponentModel componentModel) => true;

        /// <summary>AnimationController 创建/重建后调用。用于设置初始参数。</summary>
        public virtual void OnControllerCreated(AnimationController controller) { }

        /// <summary>每帧动画更新前调用（controller.Update 之前）。用于同步参数驱动状态规则。</summary>
        public virtual void SyncAnimationParameters(AnimationController controller) { }

        /// <summary>动画事件转发（含 onComplete trigger 触发的事件）。</summary>
        public virtual void HandleAnimationEvent(AnimationController controller, AnimationEvent animationEvent) { }
    }
}
