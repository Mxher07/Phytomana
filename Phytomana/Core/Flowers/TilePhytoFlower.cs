using System;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 花朵顶层抽象基类（逐位置花朵节点，等价于其他游戏中 TileEntity 的角色）。
    /// 框架负责：网络注册、生命周期钩子派发、基座有效性检测、状态机、分片 Tick 调度、存档模板。
    /// 子类只需重写 <see cref="FlowerTick"/> 实现业务逻辑。
    /// </summary>
    public abstract class TilePhytoFlower {
        public Point3 Position { get; }

        public FlowerState State { get; private set; } = FlowerState.Idle;

        /// <summary>
        /// 注册后由 FlowerTickScheduler 赋值；未注册的花朵不持有任何框架引用，可被 GC。
        /// </summary>
        public FlowerTickScheduler Scheduler { get; internal set; }

        /// <summary>
        /// 通用冷却截止时间（绝对游戏时间），存档时自动换算为相对时长。
        /// </summary>
        public double m_cooldown;

        /// <summary>
        /// 通用内部计时器截止时间（绝对游戏时间），存档时自动换算为相对时长。
        /// </summary>
        public double m_timer;

        /// <summary>
        /// 距离上次 FlowerTick 的实际间隔秒数（分片调度下不恒定，速率类逻辑必须乘它）。
        /// </summary>
        public float DeltaTime { get; private set; }

        /// <summary>
        /// 上次被调度的游戏时间。
        /// </summary>
        public double LastTickTime { get; internal set; }

        /// <summary>
        /// 当前游戏总时间（SubsystemGameInfo.TotalElapsedGameTime）。
        /// </summary>
        public double TotalTime => Scheduler.CurrentTime;

        public Project Project => Scheduler.Project;

        protected TilePhytoFlower(Point3 position) {
            Position = position;
        }

        /// <summary>
        /// 玩家放置时调用（方块被实际改变）。
        /// </summary>
        public virtual void OnPlaced() { }

        /// <summary>
        /// 方块被破坏时调用，随后节点注销并丢弃存档。
        /// </summary>
        public virtual void OnDestroyed() { }

        /// <summary>
        /// 区块加载（或首次生成）时调用。
        /// </summary>
        public virtual void OnChunkLoad() { }

        /// <summary>
        /// 区块卸载时调用，随后节点注销并转入休眠存档。
        /// </summary>
        public virtual void OnChunkUnload() { }

        public void SetState(FlowerState newState) {
            if (State == newState) {
                return;
            }
            FlowerState oldState = State;
            State = newState;
            PhytoEventBus.Fire(new FlowerStateChangedEvent(Position, oldState, newState, GetType().Name));
            OnStateChanged(oldState, newState);
        }

        /// <summary>
        /// 状态切换钩子（FlowerStateChangedEvent 已由框架在 SetState 中统一触发，此处仅供子类扩展）。
        /// </summary>
        protected virtual void OnStateChanged(FlowerState oldState, FlowerState newState) { }

        /// <summary>
        /// 基座有效性检测：默认要求花朵下方是泥土或草皮，子类可重写放宽或收紧。
        /// </summary>
        public virtual bool IsValidBase() {
            int contents = Scheduler.m_subsystemTerrain.Terrain.GetCellContents(Position.X, Position.Y - 1, Position.Z);
            return contents == DirtBlock.Index || contents == GrassBlock.Index;
        }

        /// <summary>
        /// 业务逻辑入口，由全局调度器分片轮询调用。禁止自行挂帧更新。
        /// </summary>
        public virtual void FlowerTick() { }

        /// <summary>
        /// 框架调度包装：计时、基座休眠判定之后才进入业务 FlowerTick。仅调度器调用。
        /// </summary>
        internal void ScheduledTick(double time) {
            DeltaTime = (float)Math.Clamp(time - LastTickTime, 0f, 10f);
            LastTickTime = time;
            if (State == FlowerState.Dead) {
                return;
            }
            if (!IsValidBase()) {
                if (State != FlowerState.Sleep) {
                    SetState(FlowerState.Sleep);
                }
                return;
            }
            if (State == FlowerState.Sleep) {
                SetState(FlowerState.Idle);
            }
            FlowerTick();
        }

        /// <summary>
        /// 统一存档模板：保存状态、冷却与内部计时器（时间类字段按相对时长保存，跨会话稳定）。
        /// 子类重写时先调用 base。
        /// </summary>
        public virtual void SaveData(ValuesDictionary values) {
            values.SetValue("Type", GetType().Name);
            values.SetValue("State", (int)State);
            values.SetValue("Cooldown", Math.Max(0.0, m_cooldown - TotalTime));
            values.SetValue("Timer", Math.Max(0.0, m_timer - TotalTime));
        }

        /// <summary>
        /// 统一读档模板。子类重写时先调用 base。
        /// </summary>
        public virtual void LoadData(ValuesDictionary values) {
            State = (FlowerState)values.GetValue("State", 0);
            m_cooldown = TotalTime + values.GetValue("Cooldown", 0.0);
            m_timer = TotalTime + values.GetValue("Timer", 0.0);
        }

        /// <summary>
        /// 掉落物是否位于指定格子（含少量垂直容差），供取物类花朵业务复用。
        /// </summary>
        public static bool IsPickableInCell(Pickable pickable, Point3 cell) {
            Vector3 position = pickable.Position;
            return position.X >= cell.X
                && position.X < cell.X + 1f
                && position.Z >= cell.Z
                && position.Z < cell.Z + 1f
                && position.Y >= cell.Y - 0.5f
                && position.Y < cell.Y + 1.5f;
        }
    }
}
