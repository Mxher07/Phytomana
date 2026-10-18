using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 功能花基类：自带魔力缓存并以 IManaReceiver 身份注册进魔力网络，可接收产魔源投递的魔力。
    /// 提供取魔、开关机、取魔反馈粒子等公共能力，具体花只需重写业务差异点。
    /// </summary>
    public abstract class TileFunctionalFlower : TilePhytoFlower, IManaReceiver {
        public ManaStorage ManaStorage { get; }

        /// <summary>
        /// 花朵魔力上限。子类重写为常量值即可（重写体不得依赖实例字段，构造期会被读取）。
        /// </summary>
        public virtual float MaxMana => 1000f;

        /// <summary>
        /// 是否接收网络投递的魔力。不需要魔力的功能花重写为 false，避免分摊产魔源的输出。
        /// </summary>
        public virtual bool ReceivesMana => true;

        /// <summary>开关机状态（存档保存）。需要开关机的花把基类 <see cref="TogglePower"/> 接到法杖绑定点击即可。</summary>
        public bool m_powered;

        protected TileFunctionalFlower(Point3 position) : base(position) {
            ManaStorage = new ManaStorage(MaxMana);
        }

        /// <summary>
        /// 法杖绑定模式下点击功能花：开关机并给玩家播报。默认实现切 <see cref="m_powered"/> 并用
        /// 语言的 <see cref="PoweredMessageKey"/> 提示；不需开关机的花可不接。
        /// </summary>
        public virtual void TogglePower(ComponentPlayer player) {
            m_powered = !m_powered;
            if (player != null) {
                string key = m_powered ? PoweredOnKey : PoweredOffKey;
                player.ComponentGui.DisplaySmallMessage(
                    LanguageControl.Get(PoweredMessageKey, key), Color.White, false, false);
            }
        }

        /// <summary>开关机提示的语言表名，子类覆写为各自的消息表（如 "JadedMessages"）。</summary>
        public virtual string PoweredMessageKey => "PoweredMessages";

        /// <summary>开机/关机提示的键名，子类覆写（如 "JadedPowerOn"/"JadedPowerOff"）。</summary>
        public virtual string PoweredOnKey => "PoweredOn";

        public virtual string PoweredOffKey => "PoweredOff";

        /// <summary>是否有发射器链路指向自己（被绑链后通常不再自行取食）。O(1)。</summary>
        public bool HasIncomingLink() {
            SubsystemMana mana = Scheduler?.Project?.FindSubsystem<SubsystemMana>(false);
            return mana != null && mana.HasIncomingLink(Position);
        }

        /// <summary>
        /// 未被绑链时自行从范围内魔法池取食。子类传各自的范围/停止比例；
        /// <paramref name="onSuck"/> 回调在取魔成功时触发（可拿到被抽的池做自定制取量），
        /// 不传则默认「抽满可用量」（受 <paramref name="stopWhenFullRatio"/> 限制）。
        /// </summary>
        protected void TryDrawFromPool(
            float searchRange,
            float stopWhenFullRatio = 1f,
            Action<ManaPool> onSuck = null) {
            SubsystemMana mana = Scheduler?.Project?.FindSubsystem<SubsystemMana>(false);
            if (mana == null || ManaStorage.IsFull) {
                return;
            }
            ManaPool best = null;
            float bestDistance = float.MaxValue;
            List<IManaReceiver> buffer = new();
            mana.m_network.GetActiveReceivers(buffer);
            foreach (IManaReceiver receiver in buffer) {
                if (receiver is not ManaPool pool
                    || pool.Position == Position
                    || pool.ManaStorage.IsEmpty) {
                    continue;
                }
                float dx = pool.Position.X - Position.X;
                float dy = pool.Position.Y - Position.Y;
                float dz = pool.Position.Z - Position.Z;
                if (MathF.Abs(dx) > searchRange
                    || MathF.Abs(dy) > searchRange
                    || MathF.Abs(dz) > searchRange) {
                    continue;
                }
                float distance = dx * dx + dy * dy + dz * dz;
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = pool;
                }
            }
            if (best == null) {
                return;
            }
            if (onSuck != null) {
                onSuck(best);
                return;
            }
            float free = stopWhenFullRatio >= 1f
                ? ManaStorage.Free
                : ManaStorage.Max * stopWhenFullRatio - ManaStorage.Current;
            float take = MathF.Min(best.ManaStorage.Current, free);
            if (take <= 0f) {
                return;
            }
            best.ManaStorage.Take(take);
            ManaStorage.TryAdd(take);
            SpawnSuckParticle();
        }

        /// <summary>取魔反馈粒子：花位置 +0.2y 处的夜影花同款紫色粒子，供 <see cref="TryDrawFromPool"/> 回调复用。</summary>
        protected void SpawnSuckParticle() {
            SubsystemParticles particles = Scheduler?.Project?.FindSubsystem<SubsystemParticles>(false);
            if (particles == null) {
                return;
            }
            particles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(Position.X + 0.5f, Position.Y + 0.2f, Position.Z + 0.5f),
                0.6f,
                1.6f,
                new Color(150, 100, 220)
            ));
        }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            values.SetValue("Mana", ManaStorage.SaveData());
            values.SetValue("Powered", m_powered);
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            ManaStorage.LoadData(values.GetValue("Mana", 0f));
            // 注意：必须读写基类 m_powered。子类的 `new m_powered` 只是隐藏字段，
            // 存档写读的都是基类字段，写错字段开关机就变成「表面工作」了。
            m_powered = values.GetValue("Powered", m_powered);
        }
    }
}
