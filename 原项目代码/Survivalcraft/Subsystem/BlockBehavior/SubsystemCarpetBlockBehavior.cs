using TemplatesDatabase;

namespace Game {
    public class SubsystemCarpetBlockBehavior : SubsystemPollableBlockBehavior {
        public SubsystemWeather m_subsystemWeather;

        public Random m_random = new();

        public override int[] HandledBlocks => [];

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(true);
            base.Load(valuesDictionary);
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
            if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].IsNonAttachable(cellValue)) {
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
            if (m_random.Float(0f, 1f) < 0.25f) {
                PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
                if (precipitationShaftInfo.Intensity > 0f
                    && y >= precipitationShaftInfo.YLimit - 1) {
                    SubsystemTerrain.DestroyCell(
                        0,
                        x,
                        y,
                        z,
                        0,
                        true,
                        false
                    );
                }
            }
        }
    }
}