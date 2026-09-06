using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using Phytomana.Api;
using TemplatesDatabase;
using Random = Game.Random;

namespace Phytomana {
    /// <summary>
    /// 荆棘之花：守卫型功能花。每个攻击周期（1 秒）消耗魔力，向
    /// 14×14×14 立方范围内最近的一只非玩家生物发射一枚纯红荆棘粒子；
    /// 粒子命中（抵达落点时附近有实体）才结算伤害，落点附近无实体则空刺。
    /// 未被法杖绑链供魔时，会自行搜寻 23×23×23 立方内的魔法池吸取魔力
    /// （每次 36mn，存量不足 36 不吸，自身储至 50% 后停止）。
    /// 数值全部经 PhytoConfig 可调。
    /// </summary>
    public class TileThornyRose : TileFunctionalFlower {
        public const float DefaultMaxMana = 360f;
        public const float DefaultManaCost = 12f;
        public const float DefaultDamage = 0.1f;
        public const float DefaultAttackRange = 14f;
        public const float DefaultPoolDrawAmount = 36f;
        public const float DefaultPoolSearchRange = 11f;
        public const float PoolDrawStopRatio = 0.5f;
        public const double AttackInterval = 1.0;

        /// <summary>荆棘粒子的飞行时间（也是伤害结算的延迟）。</summary>
        public const float ParticleFlightTime = 1.5f;

        /// <summary>粒子落点的命中判定半径：落点附近该距离内有实体才算命中。</summary>
        public const float HitRadius = 1.5f;

        /// <summary>
        /// 伤害换算系数：ComponentHealth.Health 为 0~1 的归一化生命值（1.0 即满血），
        /// Injure 的入参同为百分比。配置里的「伤害点数」按 1 点 = 目标最大生命的 10% 换算，
        /// 默认 1.5 点 ≈ 一次削去 15% 生命。
        /// </summary>
        public const float DamageToHealthScale = 0.1f;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemMana m_subsystemMana;

        public ManaNetworkManager m_network;

        public List<IManaReceiver> m_receiverBuffer = [];

        /// <summary>飞行中的荆棘粒子：到点后在落点附近做命中判定。</summary>
        public List<PendingThornHit> m_pendingHits = [];

        public class PendingThornHit {
            public double Time;
            public Vector3 TargetPosition;
        }

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("ThornyRoseFlower", PhytoConfig.Instance.ThornyRoseMaxMana);

        public TileThornyRose(Point3 position) : base(position) { }

        public override void OnPlaced() {
            m_cooldown = TotalTime + AttackInterval;
        }

        public override void OnChunkLoad() {
            m_cooldown = TotalTime + AttackInterval;
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            SettlePendingHits(time);
            if (time < m_cooldown) {
                return;
            }
            m_cooldown = time + AttackInterval;
            // 未被绑链时先自行取食（储至 50% 即停），再尝试发射荆棘
            if (!HasIncomingLink() && ManaStorage.Current < ManaStorage.Max * PoolDrawStopRatio) {
                TryDrawFromPool();
            }
            if (ManaStorage.Current >= PhytoConfig.Instance.ThornyRoseManaCost) {
                TryFireThorn(time);
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

        /// <summary>搜寻范围内的魔法池（每次 36mn，不足不吸），吸取时播放夜影花同款粒子。</summary>
        public void TryDrawFromPool() {
            float searchRange = PhytoConfig.Instance.ThornyRosePoolSearchRange;
            float drawAmount = PhytoConfig.Instance.ThornyRosePoolDrawAmount;
            ManaPool best = null;
            float bestDistance = float.MaxValue;
            m_network.GetActiveReceivers(m_receiverBuffer);
            foreach (IManaReceiver receiver in m_receiverBuffer) {
                if (receiver is not ManaPool pool
                    || pool.Position == Position
                    || pool.ManaStorage.Current < drawAmount) {
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
            best.ManaStorage.Take(drawAmount);
            ManaStorage.TryAdd(drawAmount);
            // 吸取反馈：花位置 +0.2y 处播放夜影花同款粒子
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(Position.X + 0.5f, Position.Y + 0.2f, Position.Z + 0.5f),
                0.6f,
                1.6f,
                new Color(150, 100, 220)
            ));
        }

        /// <summary>
        /// 发射一枚纯红荆棘粒子飞向锁定目标，同时登记一次落点命中判定：
        /// 粒子抵达（飞行 1.5 秒）后，落点附近存在实体才造成伤害。
        /// </summary>
        public void TryFireThorn(double time) {
            Vector3 muzzle = new(Position.X + 0.5f, Position.Y + 0.8f, Position.Z + 0.5f);
            ComponentBody target = FindNearestCreatureBody(muzzle, PhytoConfig.Instance.ThornyRoseAttackRange);
            if (target == null) {
                return;
            }
            ManaStorage.Take(PhytoConfig.Instance.ThornyRoseManaCost);
            Vector3 targetPosition = target.Position;
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                muzzle,
                0.25f,
                ParticleFlightTime,
                Color.Red,
                targetPosition,
                1
            ));
            m_pendingHits.Add(new PendingThornHit {
                Time = time + ParticleFlightTime,
                TargetPosition = targetPosition
            });
        }

        /// <summary>结算到点的荆棘：落点附近（命中半径内）最近的一只非玩家生物承受伤害，无实体则空刺。</summary>
        public void SettlePendingHits(double time) {
            for (int i = m_pendingHits.Count - 1; i >= 0; i--) {
                if (time < m_pendingHits[i].Time) {
                    continue;
                }
                Vector3 hitPosition = m_pendingHits[i].TargetPosition;
                m_pendingHits.RemoveAt(i);
                ComponentBody hit = FindNearestCreatureBody(hitPosition, HitRadius);
                hit?.Entity.FindComponent<ComponentHealth>()?.Injure(
                    PhytoConfig.Instance.ThornyRoseDamage * DamageToHealthScale,
                    null,
                    false,
                    LanguageControl.Get("OtherCauseOfDeath", "ThornyRose")
                );
            }
        }

        /// <summary>找中心点附近（立方范围）最近的一只非玩家生物的躯体；找不到返回 null。</summary>
        public ComponentBody FindNearestCreatureBody(Vector3 center, float range) {
            ComponentBody best = null;
            float bestDistance = float.MaxValue;
            foreach (Entity entity in Project.Entities) {
                ComponentCreature creature = entity.FindComponent<ComponentCreature>();
                if (creature == null) {
                    continue;
                }
                // 不包括玩家
                if (entity.FindComponent<ComponentPlayer>() != null) {
                    continue;
                }
                ComponentBody body = creature.ComponentBody;
                if (body == null || body.m_componentHealth.Health <= 0.01f) {
                    continue;
                }
                Vector3 position = body.Position;
                if (MathF.Abs(position.X - center.X) > range
                    || MathF.Abs(position.Y - center.Y) > range
                    || MathF.Abs(position.Z - center.Z) > range) {
                    continue;
                }
                float distance = (position - center).LengthSquared();
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = body;
                }
            }
            return best;
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