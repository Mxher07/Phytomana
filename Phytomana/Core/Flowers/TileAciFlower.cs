using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;

namespace Phytomana {
    /// <summary>
    /// 剧毒树丛：范围毒攻功能花。工作范围为以花为中心的 13×13×13 立方体。
    /// 耗魔速率 = 范围内每只生物 30mn/s（内部缓存 360mn），魔力不足时不标记。
    /// 每轮攻击：对范围内除玩家以外的生物施毒；被标记者获得 3s 标记，标记期内不会被重复标记，
    /// 且无论标记者身处何处，每 1s 受到固定 10% 生命的中毒伤害（不致死，余血 1 点保底）；
    /// 生命已小于 1 点的生物不会被标记。
    /// 被标记者每 0.15s 身上生成 0.15f 大小、存活 1.2f、向上移动 0.05f 的绿色粒子；
    /// 施毒时花先放一枚 0.5f 大小、存活 1f 的绿色攻击粒子飞向目标。
    /// 取魔方式：与其他功能花一样范围搜寻魔力池；亦接受发射器主动供魔。
    /// </summary>
    public class TileAciFlower : TileFunctionalFlower {
        public const float DefaultMaxMana = 360f;
        public const float ManaPerCreaturePerSecond = 30f;
        public const int RangeHalf = 6;
        public const double MarkDuration = 3.0;
        public const double PoisonTickInterval = 1.0;
        /// <summary>每次中毒结算削去的归一化生命值（1.0 = 满血）。</summary>
        public const float PoisonDamagePerTick = 0.1f;
        /// <summary>中毒伤害的保底余血（归一化），不低于此值。</summary>
        public const float PoisonMinHealthFloor = 0.01f;
        public const double VictimParticleInterval = 0.15;
        public const float VictimParticleSize = 0.15f;
        public const double VictimParticleDuration = 1.2;
        public const float VictimParticleRise = 0.05f;
        public const float AttackParticleSize = 0.5f;
        public const double AttackParticleDuration = 1.0;

        /// <summary>已标记（标记期内）的目标：避免重复标记。</summary>
        public class MarkedCreature {
            public ComponentBody Body;
            public double ExpireTime;
            public double NextPoisonTick;
            public double NextParticleTime;
        }

        public SubsystemParticles m_subsystemParticles;

        public SubsystemMana m_subsystemMana;

        public ManaNetworkManager m_network;

        public List<IManaReceiver> m_receiverBuffer = [];

        public List<MarkedCreature> m_marked = [];

        public const float PoolSearchRange = 11f;

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("AciFlower", DefaultMaxMana);

        public TileAciFlower(Point3 position) : base(position) { }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            // 标记列表跨会话无意义：存档时清空，重新加载后由范围内的生物重新标记。
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            TickMarked(time);
            if (!HasIncomingLink() && !ManaStorage.IsFull) {
                TryDrawFromPool();
            }
            Attack(time);
        }

        /// <summary>
        /// 每轮攻击：找出范围内除玩家以外的生物，扣除魔力（每只 30mn×本轮 Δt）后为未标记者打标；
        /// 已标记的继续维持标记，不重复施毒。
        /// </summary>
        public void Attack(double time) {
            List<ComponentBody> targets = FindCreaturesInRange(time);
            if (targets.Count == 0) {
                return;
            }
            // 耗魔速率 = 范围内生物数 × 30mn/s
            float manaCost = targets.Count * ManaPerCreaturePerSecond * DeltaTime;
            if (manaCost > 0f && ManaStorage.Current < manaCost) {
                return;
            }
            if (manaCost > 0f) {
                ManaStorage.Take(manaCost);
            }
            Vector3 muzzle = new(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f);
            foreach (ComponentBody body in targets) {
                MarkedCreature existing = FindMarked(body);
                if (existing != null) {
                    continue;
                }
                MarkedCreature marked = new() {
                    Body = body,
                    ExpireTime = time + MarkDuration,
                    NextPoisonTick = time + PoisonTickInterval,
                    NextParticleTime = time
                };
                m_marked.Add(marked);
                // 攻击粒子：0.5f 大小、存活 1f 的绿色粒子飞向目标
                m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    muzzle,
                    AttackParticleSize,
                    (float)AttackParticleDuration,
                    new Color(80, 200, 80),
                    body.Position,
                    1
                ));
            }
        }

        /// <summary>范围内（13×13×13 立方）除玩家以外的活体生物。</summary>
        public List<ComponentBody> FindCreaturesInRange(double time) {
            List<ComponentBody> result = [];
            Vector3 center = new(Position.X + 0.5f, Position.Y + 0.5f, Position.Z + 0.5f);
            foreach (Entity entity in Project.Entities) {
                ComponentCreature creature = entity.FindComponent<ComponentCreature>();
                if (creature == null) {
                    continue;
                }
                if (entity.FindComponent<ComponentPlayer>() != null) {
                    continue;
                }
                ComponentBody body = creature.ComponentBody;
                if (body == null || body.m_componentHealth.Health <= 0.01f) {
                    continue;
                }
                Vector3 position = body.Position;
                if (MathF.Abs(position.X - center.X) > RangeHalf
                    || MathF.Abs(position.Y - center.Y) > RangeHalf
                    || MathF.Abs(position.Z - center.Z) > RangeHalf) {
                    continue;
                }
                result.Add(body);
            }
            return result;
        }

        MarkedCreature FindMarked(ComponentBody body) {
            foreach (MarkedCreature marked in m_marked) {
                if (ReferenceEquals(marked.Body, body)) {
                    return marked;
                }
            }
            return null;
        }

        /// <summary>
        /// 驱动所有标记：每 1s 结算一次固定 10% 中毒伤害（不致死，保底余血 1 点）；
        /// 每 0.15s 生成一枚 0.15f 大小、存活 1.2f、向上移动 0.05f 的绿色粒子；
        /// 标记到期或目标死亡/离开时移除。
        /// </summary>
        public void TickMarked(double time) {
            for (int i = m_marked.Count - 1; i >= 0; i--) {
                MarkedCreature marked = m_marked[i];
                ComponentBody body = marked.Body;
                if (body == null
                    || body.m_componentHealth == null
                    || body.m_componentHealth.Health <= 0.01f
                    || time >= marked.ExpireTime) {
                    m_marked.RemoveAt(i);
                    continue;
                }
                ComponentHealth health = body.m_componentHealth;
                if (time >= marked.NextPoisonTick) {
                    marked.NextPoisonTick = time + PoisonTickInterval;
                    // 固定 10% 的中毒伤害，但不致死（余血不低于 1 点）
                    float currentHealth = health.Health;
                    float damaged = MathUtils.Max(
                        currentHealth - PoisonDamagePerTick,
                        PoisonMinHealthFloor
                    );
                    if (damaged < currentHealth) {
                        health.Health = damaged;
                    }
                }
                if (time >= marked.NextParticleTime) {
                    marked.NextParticleTime = time + VictimParticleInterval;
                    // 0.15f 大小、存活 1.2f、生成后向上移动 0.05f 的绿色粒子
                    Vector3 start = body.Position;
                    m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                        start,
                        VictimParticleSize,
                        (float)VictimParticleDuration,
                        new Color(80, 200, 80),
                        start + new Vector3(0f, VictimParticleRise, 0f),
                        1
                    ));
                }
            }
        }

        /// <summary>是否有发射器链路指向自己（被绑链后不再自行取食）。</summary>
        public bool HasIncomingLink() {
            foreach (ManaLink link in m_subsystemMana.m_links) {
                if (link.To == Position) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>未被绑链时自行从魔法池取食（储满即停）。</summary>
        public void TryDrawFromPool() {
            ManaPool best = null;
            float bestDistance = float.MaxValue;
            m_network.GetActiveReceivers(m_receiverBuffer);
            foreach (IManaReceiver receiver in m_receiverBuffer) {
                if (receiver is not ManaPool pool
                    || pool.Position == Position
                    || pool.ManaStorage.IsEmpty) {
                    continue;
                }
                float dx = pool.Position.X - Position.X;
                float dy = pool.Position.Y - Position.Y;
                float dz = pool.Position.Z - Position.Z;
                if (MathF.Abs(dx) > PoolSearchRange
                    || MathF.Abs(dy) > PoolSearchRange
                    || MathF.Abs(dz) > PoolSearchRange) {
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
            float take = MathF.Min(best.ManaStorage.Current, ManaStorage.Free);
            if (take <= 0f) {
                return;
            }
            best.ManaStorage.Take(take);
            ManaStorage.TryAdd(take);
        }

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
            m_network = Project.FindSubsystem<ManaNetworkManager>(true);
        }
    }
}
