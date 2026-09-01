using TemplatesDatabase;

namespace Game {
    public class SubsystemCactusBlockBehavior : SubsystemPollableBlockBehavior {
        public SubsystemTime m_subsystemTime;

        public SubsystemCellChangeQueue m_subsystemCellChangeQueue;

        public SubsystemGameInfo m_subsystemGameInfo;

        public Random m_random = new();

        public int m_sandBlockIndex;

        public int m_cactusBlockIndex;
        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<CactusBlock>()];

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
            if (cellContents != m_sandBlockIndex
                && cellContents != m_cactusBlockIndex) {
                SubsystemTerrain.DestroyCell(
                    0,
                    x,
                    y,
                    z,
                    0,
                    false,
                    false
                );
            }
        }

        public override void OnPoll(int value, int x, int y, int z, int pollPass) {
            if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != 0) {
                return;
            }
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
            if (Terrain.ExtractContents(cellValue) == 0
                && Terrain.ExtractLight(cellValue) >= 12) {
                int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
                int cellContents2 = SubsystemTerrain.Terrain.GetCellContents(x, y - 2, z);
                if ((cellContents != m_cactusBlockIndex || cellContents2 != m_cactusBlockIndex)
                    && m_random.Float(0f, 1f) < 0.25f) {
                    m_subsystemCellChangeQueue.QueueCellChange(x, y + 1, z, Terrain.MakeBlockValue(m_cactusBlockIndex, 0, 0));
                }
            }
        }

        public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody) {
            ComponentHealth componentHealth = componentBody.Entity.FindComponent<ComponentHealth>();
            if (componentHealth != null) {
                componentHealth.OnSpiked(
                    this,
                    0.1f / componentHealth.SpikeResilience * MathF.Abs(velocity),
                    cellFace,
                    velocity,
                    componentBody,
                    LanguageControl.Get("OtherCauseOfDeath", "Cactus")
                );
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemCellChangeQueue = Project.FindSubsystem<SubsystemCellChangeQueue>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_sandBlockIndex = BlocksManager.GetBlockIndex<SandBlock>();
            m_cactusBlockIndex = BlocksManager.GetBlockIndex<CactusBlock>();
            base.Load(valuesDictionary);
        }
    }
}