using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemArrowBlockBehavior : SubsystemBlockBehavior {
        public SubsystemProjectiles m_subsystemProjectiles;

        public Random m_random = new();

        public override int[] HandledBlocks => [];

        public override void OnFiredAsProjectile(Projectile projectile) {
            if (ArrowBlock.GetArrowType(Terrain.ExtractData(projectile.Value)) == ArrowBlock.ArrowType.FireArrow) {
                m_subsystemProjectiles.AddTrail(projectile, Vector3.Zero, new SmokeTrailParticleSystem(20, 0.5f, float.MaxValue, Color.White));
                projectile.ProjectileStoppedAction = ProjectileStoppedAction.Disappear;
                projectile.IsIncendiary = true;
            }
        }

        public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem) {
            ArrowBlock.ArrowType arrowType = ArrowBlock.GetArrowType(Terrain.ExtractData(worldItem.Value));
            if (worldItem.Velocity.Length() > 10f) {
                float num = 0.1f;
                if (arrowType == ArrowBlock.ArrowType.FireArrow) {
                    num = 0.5f;
                }
                if (arrowType == ArrowBlock.ArrowType.WoodenArrow) {
                    num = 0.2f;
                }
                if (arrowType == ArrowBlock.ArrowType.DiamondArrow) {
                    num = 0f;
                }
                if (arrowType == ArrowBlock.ArrowType.IronBolt) {
                    num = 0.05f;
                }
                if (arrowType == ArrowBlock.ArrowType.DiamondBolt) {
                    num = 0f;
                }
                if (m_random.Float(0f, 1f) < num) {
                    return true;
                }
            }
            return false;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(true);
        }
    }
}