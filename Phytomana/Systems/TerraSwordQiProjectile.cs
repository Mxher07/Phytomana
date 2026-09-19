using Engine;
using Engine.Graphics;
using GameEntitySystem;
using Phytomana;

namespace Game {
    /// <summary>
    /// 泰拉刃剑气：沿发射方向直线飞行（无重力、无减速），命中生物造成固定伤害，
    /// 命中方块或超时（4s）后消散；飞行中每 0.15s 甩出绿色轨迹粒子，消散时迸发浅绿粒子。
    /// 伤害由 TerraToolBehaviorSubsystem 的 OnProjectileHitBody 钩子注入（3.5 固定值）。
    /// </summary>
    public class TerraSwordQiProjectile : Projectile {
        public const float Speed = 28f;

        /// <summary>命中生物的固定伤害点数（与项目「1 点 = 目标最大生命 10%」刻度一致，等效 35% 最大生命）。</summary>
        public const float FixedDamage = 3.5f;

        public const float LifeTime = 4f;

        public const float TrailParticleInterval = 0.15f;

        public double m_flightTime;

        public double m_lastTrailTime;

        public bool m_dead;

        public override void Initialize(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, Entity owner) {
            base.Initialize(value, position, velocity, angularVelocity, owner);
            Gravity = 0f;
            Damping = 1f;
            DampingInFluid = 1f;
            TerrainKnockBack = 0f;
            ProjectileStoppedAction = ProjectileStoppedAction.Disappear;
            ToRemove = false;
            m_dead = false;
            m_flightTime = 0;
            m_lastTrailTime = -TrailParticleInterval;
        }

        public override void UpdateInChunk(float dt) {
            m_flightTime += dt;
            // 飞行轨迹粒子（0.25 大小、0.35s 寿命、绿色）
            if (m_flightTime - m_lastTrailTime >= TrailParticleInterval && SubsystemParticles != null) {
                m_lastTrailTime = m_flightTime;
                SubsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    Position,
                    0.25f,
                    0.35f,
                    new Color(80, 200, 80)
                ));
            }
            if (m_flightTime >= LifeTime) {
                Die();
                return;
            }
            base.UpdateInChunk(dt);
        }

        public override void HitTerrain(TerrainRaycastResult terrainRaycastResult,
            CellFace cellFace,
            ref Vector3 positionAtdt,
            ref Vector3? pickableStuckMatrix) {
            // 撞方块直接消散（不破格、不卡入、不转掉落物）
            positionAtdt = Position;
            Die();
        }

        public override void HitBody(BodyRaycastResult bodyRaycastResult, ref Vector3 positionAtdt) {
            // 伤害结算走基类（OnProjectileHitBody 钩子把 AttackPower 改为固定 3.5），随后消散
            base.HitBody(bodyRaycastResult, ref positionAtdt);
            Die();
        }

        public void Die() {
            if (m_dead || ToRemove) {
                return;
            }
            m_dead = true;
            ToRemove = true;
            // 消散粒子（0.35 大小、0.4s 寿命、浅绿色，上移 0.05）
            if (SubsystemParticles != null) {
                SubsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    Position + new Vector3(0f, 0.05f, 0f),
                    0.35f,
                    0.4f,
                    new Color(150, 230, 150)
                ));
            }
        }
    }
}
