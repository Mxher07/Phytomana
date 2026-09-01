using Engine;
using GameEntitySystem;

namespace Game {
    public class ProjectileAttackment : Attackment {
        public Projectile Projectile;

        public ProjectileAttackment(Entity target, Entity attacker, Vector3 hitPoint, Vector3 hitDirection, float attackPower, Projectile projectile)
            : base(target, attacker, hitPoint, hitDirection, attackPower) {
            Projectile = projectile;
            ImpulseFactor = 2f;
            StunTimeSet = 0.2f;
            AllowImpulseAndStunWhenDamageIsZero = false;
        }

        public ProjectileAttackment(ComponentBody target,
            Entity attacker,
            Vector3 hitPoint,
            Vector3 hitDirection,
            float attackPower,
            Projectile projectile) : this(target.Entity, attacker, hitPoint, hitDirection, attackPower, projectile) { }
    }
}