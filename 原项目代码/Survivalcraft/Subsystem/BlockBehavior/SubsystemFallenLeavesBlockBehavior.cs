using TemplatesDatabase;

namespace Game {
    public class SubsystemFallenLeavesBlockBehavior : SubsystemPollableBlockBehavior {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemSeasons m_subsystemSeasons;

        Random m_random = new();

        public override int[] HandledBlocks => [];

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            if (!CanSupportFallenLeaves(SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z))) {
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

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            UpdateFallenLeaves(x, y, z);
        }

        public override void OnPoll(int value, int x, int y, int z, int pollPass) {
            if (m_random.Bool(0.5f)) {
                UpdateFallenLeaves(x, y, z);
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemSeasons = Project.FindSubsystem<SubsystemSeasons>(true);
        }

        public static bool CanSupportFallenLeaves(int value) {
            int num = Terrain.ExtractContents(value);
            return !BlocksManager.Blocks[num].IsTransparent_(value);
        }

        public static bool StopsFallenLeaves(int value) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            if (!(block is AirBlock)) {
                return !(block is LeavesBlock);
            }
            return false;
        }

        public static bool CanBeReplacedByFallenLeaves(int value) => Terrain.ExtractContents(value) == 0;

        void UpdateFallenLeaves(int x, int y, int z) {
            if (m_subsystemSeasons.Season == Season.Spring
                || m_subsystemSeasons.Season == Season.Summer) {
                m_subsystemTerrain.DestroyCell(
                    0,
                    x,
                    y,
                    z,
                    Terrain.MakeBlockValue(0),
                    true,
                    true
                );
            }
        }
    }
}