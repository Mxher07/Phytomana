using Engine;
using GameEntitySystem;

namespace Game {
    public class MeleeAttackment : Attackment {
        public MeleeAttackment(Entity target, Entity attacker, Vector3 hitPoint, Vector3 hitDirection, float attackPower) : base(
            target,
            attacker,
            hitPoint,
            hitDirection,
            attackPower
        ) {
            ComponentBody attackerBody = Attacker.FindComponent<ComponentBody>();
            ComponentBody targetBody = target.FindComponent<ComponentBody>();
            float num5 = attackPower >= 2f ? 1.25f : 1f;
            float num6 = MathF.Pow(attackerBody.Mass / targetBody.Mass, 0.5f);
            float x2 = num5 * num6;
            ImpulseFactor = 5.5f * MathUtils.Saturate(x2);
            StunTimeSet = 0.25f * MathUtils.Saturate(x2);
        }

        public MeleeAttackment(ComponentBody target, Entity attacker, Vector3 hitPoint, Vector3 hitDirection, float attackPower) : this(
            target.Entity,
            attacker,
            hitPoint,
            hitDirection,
            attackPower
        ) { }
    }
}