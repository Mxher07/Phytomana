using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemBulletBlockBehavior : SubsystemBlockBehavior {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemExplosions m_subsystemExplosions;

        public SubsystemAudio m_subsystemAudio;

        public Random m_random = new();

        public override int[] HandledBlocks => [];

        public override bool OnHitAsProjectile(CellFace? cellFace, ComponentBody componentBody, WorldItem worldItem) {
            BulletBlock.BulletType bulletType = BulletBlock.GetBulletType(Terrain.ExtractData(worldItem.Value));
            bool result = true;
            if (cellFace.HasValue) {
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.Value.X, cellFace.Value.Y, cellFace.Value.Z);
                int num = Terrain.ExtractContents(cellValue);
                Block obj = BlocksManager.Blocks[num];
                if (worldItem.Velocity.Length() > 30f) {
                    m_subsystemExplosions.TryExplodeBlock(cellFace.Value.X, cellFace.Value.Y, cellFace.Value.Z, cellValue);
                }
                if (obj.GetDensity(cellValue) >= 1.5f
                    && worldItem.Velocity.Length() > 30f) {
                    float num2 = 1f;
                    float minDistance = 8f;
                    if (bulletType == BulletBlock.BulletType.BuckshotBall) {
                        num2 = 0.25f;
                        minDistance = 4f;
                    }
                    if (m_random.Float(0f, 1f) < num2) {
                        m_subsystemAudio.PlayRandomSound(
                            "Audio/Ricochets",
                            1f,
                            m_random.Float(-0.2f, 0.2f),
                            new Vector3(cellFace.Value.X, cellFace.Value.Y, cellFace.Value.Z),
                            minDistance,
                            true
                        );
                        result = false;
                    }
                }
            }
            return result;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(true);
        }
    }
}