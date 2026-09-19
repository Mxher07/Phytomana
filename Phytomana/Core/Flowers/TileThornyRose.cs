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
        public const float DefaultDamage = 2.5f;
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
        /// Injure 的入参同为百分比，引擎没有绝对生命点数可依。
        /// 「固定伤害」按项目既有「1 点 = 目标最大生命 10%」的点数刻度实现：
        /// 默认 2.5 点一次削去目标最大生命的 25%，对任何目标都是恒定量。
        /// </summary>
        public const float DamageToHealthScale = 0.1f;

        /// <summary>
        /// 秒杀阈值：生命值（Health×10 点数刻度）低于该值的生物被荆棘一击致死（可经 PhytoConfig 调整）。
        /// </summary>
        public float KillThreshold => PhytoConfig.Instance.ThornyRoseKillThreshold;

        /// <summary>开关机状态（默认开机，存档保存）。</summary>
        public new bool m_powered = PhytoConfig.Instance.ThornyRoseDefaultPowered;

        public override string PoweredMessageKey => "ThornyRoseMessages";

        public override string PoweredOnKey => "ThornyRosePowerOn";

        public override string PoweredOffKey => "ThornyRosePowerOff";

        public SubsystemParticles m_subsystemParticles;

        /// <summary>飞行中的荆棘粒子：到点后在落点附近做命中判定。</summary>
        public List<PendingThornHit> m_pendingHits = [];

        public class PendingThornHit {
            public double Time;
            public Vector3 TargetPosition;
        }

        public override float MaxMana => ManaBlockRegistry.GetMaxMana("ThornyRoseFlower", PhytoConfig.Instance.ThornyRoseMaxMana);

        public TileThornyRose(Point3 position) : base(position) { }

        public override void OnPlaced() {
            base.OnPlaced();
            m_cooldown = TotalTime + AttackInterval;
        }

        public override void OnChunkLoad() {
            base.OnChunkLoad();
            // 攻击周期与法线一致：读档后对齐到「1 秒后才可出手」，避免读档瞬间补射一记
            m_cooldown = TotalTime + AttackInterval;
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            SettlePendingHits(time);
            if (!m_powered) {
                return;
            }
            if (time < m_cooldown) {
                return;
            }
            m_cooldown = time + AttackInterval;
            // 未被绑链时先自行取食（每次 36mn，储至 50% 即停），再尝试发射荆棘
            if (!HasIncomingLink() && ManaStorage.Current < ManaStorage.Max * PoolDrawStopRatio) {
                // 荆棘之刺取魔策略：单次抽固定量（低于 50% 停止线才抽），与「吸满即停」的功能花不同
                float drawAmount = PhytoConfig.Instance.ThornyRosePoolDrawAmount;
                if (ManaStorage.Current + drawAmount <= ManaStorage.Max * PoolDrawStopRatio) {
                    TryDrawFromPool(
                        PhytoConfig.Instance.ThornyRosePoolSearchRange,
                        PoolDrawStopRatio,
                        pool => {
                            pool.ManaStorage.Take(drawAmount);
                            ManaStorage.TryAdd(drawAmount);
                            SpawnSuckParticle();
                        });
                }
            }
            if (ManaStorage.Current >= PhytoConfig.Instance.ThornyRoseManaCost) {
                TryFireThorn(time);
            }
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
                if (hit == null) {
                    continue;
                }
                ComponentHealth health = hit.m_componentHealth;
                if (health == null) {
                    continue;
                }
                // 生命值低于 2.5（Health×10 点数刻度）的生物被一击杀死
                if (health.Health * 10f < PhytoConfig.Instance.ThornyRoseKillThreshold * 10f) {
                    health.Injure(1f, null, true, LanguageControl.Get("OtherCauseOfDeath", "ThornyRose"));
                    continue;
                }
                health.Injure(
                    PhytoConfig.Instance.ThornyRoseDamage * DamageToHealthScale,
                    null,
                    false,
                    LanguageControl.Get("OtherCauseOfDeath", "ThornyRose")
                );
            }
        }

        /// <summary>
        /// 找中心点附近（立方范围）最近的一只非玩家生物的躯体；找不到返回 null。
        /// 筛选与「最近」判定统一用欧氏距离（切比雪夫预筛 + 欧氏精排），度量一致。
        /// </summary>
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
                Vector3 delta = position - center;
                // 欧氏距离筛选 + 精排，统一度量
                float squared = delta.LengthSquared();
                float squaredRange = range * range;
                if (squared > squaredRange) {
                    continue;
                }
                if (squared < bestDistance) {
                    bestDistance = squared;
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
        }
    }
}