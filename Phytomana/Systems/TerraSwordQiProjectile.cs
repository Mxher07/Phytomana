using Engine.Core;

namespace Game {
    /// <summary>
    /// 泰拉刃剑气：沿发射方向直线飞行，命中生物造成固定伤害，命中方块或超时（4s）后消散；
    /// 飞行中每 0.15s 甩出绿色轨迹粒子，消散时迸发浅绿粒子。
    /// 新联机 API 的 Projectile 为纯数据类、弹道物理集中在 SubsystemProjectiles，
    /// 故直线轨迹由 <see cref="SubsystemTerraToolBehavior"/> 每帧把速度与位置锁定到
    /// 理想轨迹上实现（抵消宿主对全部射弹统一施加的重力下沉项）；
    /// 实例化经 ModLoader.OnProjectileAdded 钩子（服务器与客户端一致）把宿主新建的
    /// 普通射弹原位替换为本类型；伤害由 OnProjectileHitBody 钩子注入（3.5 固定值）。
    /// </summary>
    public class TerraSwordQiProjectile : Projectile {
        public const float Speed = 28f;

        /// <summary>命中生物的固定伤害点数（与项目「1 点 = 目标最大生命 10%」刻度一致，等效 35% 最大生命）。</summary>
        public const float FixedDamage = 3.5f;

        public const float LifeTime = 4f;

        public const float TrailParticleInterval = 0.15f;

        /// <summary>理想轨迹原点（发射点；客户端以收到射弹的位置重建）。</summary>
        public Vector3 m_origin;

        /// <summary>发射方向（单位向量，飞行期间恒定）。</summary>
        public Vector3 m_direction;

        /// <summary>已飞行时间（秒）。</summary>
        public double m_flightTime;

        /// <summary>上一次甩轨迹粒子的时间点（秒）。</summary>
        public double m_lastTrailTime;

        public bool m_dead;

        /// <summary>
        /// 原位替换宿主新建的普通射弹：仅当射弹方块为泰拉刃（bladeIndex）时接管。
        /// 复制宿主已填装的公共字段，避免丢失 Id（网络同步键）、Owner（伤害归属）、
        /// CreationTime（存档恢复）等运行时状态。
        /// </summary>
        public static bool TryReplace(ref Projectile projectile, int bladeIndex) {
            if (projectile == null || projectile is TerraSwordQiProjectile) {
                return false;
            }
            if (bladeIndex < 0 || Terrain.ExtractContents(projectile.Value) != bladeIndex) {
                return false;
            }
            TerraSwordQiProjectile qi = new() {
                Id = projectile.Id,
                Value = projectile.Value,
                Position = projectile.Position,
                Velocity = projectile.Velocity,
                Rotation = projectile.Rotation,
                AngularVelocity = projectile.AngularVelocity,
                IsInWater = projectile.IsInWater,
                LastNoiseTime = projectile.LastNoiseTime,
                Owner = projectile.Owner,
                ProjectileStoppedAction = ProjectileStoppedAction.Disappear,
                TrailParticleSystem = projectile.TrailParticleSystem,
                TrailOffset = projectile.TrailOffset,
                NoChunk = projectile.NoChunk,
                IsIncendiary = projectile.IsIncendiary,
                IsFireProjectile = projectile.IsFireProjectile,
                CreationTime = projectile.CreationTime,
                Light = projectile.Light,
                ToRemove = projectile.ToRemove
            };
            Vector3 velocity = qi.Velocity;
            qi.m_direction = velocity.LengthSquared() > 1e-6f
                ? Vector3.Normalize(velocity)
                : new Vector3(0f, 0f, 1f);
            qi.m_origin = qi.Position;
            qi.m_flightTime = 0.0;
            qi.m_lastTrailTime = -TrailParticleInterval;
            projectile = qi;
            return true;
        }

        /// <summary>消散：标记移除并迸发浅绿粒子（幂等，命中与超时共用）。</summary>
        public void Die(SubsystemParticles subsystemParticles) {
            if (m_dead || ToRemove) {
                return;
            }
            m_dead = true;
            ToRemove = true;
            if (subsystemParticles != null) {
                subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                    Position + new Vector3(0f, 0.05f, 0f),
                    0.35f,
                    0.4f,
                    new Color(150, 230, 150)
                ));
            }
        }
    }
}
