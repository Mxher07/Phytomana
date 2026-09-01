using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game {
    public class SubsystemMagnetBlockBehavior : SubsystemBlockBehavior {
        public SubsystemPlayers m_subsystemPlayers;

        public DynamicArray<Vector3> m_magnets = [];

        public const int MaxMagnets = 8;

        public override int[] HandledBlocks => [167];

        public int MagnetsCount => m_magnets.Count;

        public Vector3 FindNearestCompassTarget(Vector3 compassPosition) {
            if (m_magnets.Count > 0) {
                float num = float.MaxValue;
                Vector3 v = Vector3.Zero;
                for (int i = 0; i < m_magnets.Count && i < 8; i++) {
                    Vector3 vector = m_magnets.Array[i];
                    float num2 = Vector3.DistanceSquared(compassPosition, vector);
                    if (num2 < num) {
                        num = num2;
                        v = vector;
                    }
                }
                return v + new Vector3(0.5f);
            }
            float num3 = float.MaxValue;
            Vector3 v2 = Vector3.Zero;
            foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData) {
                Vector3 spawnPosition = playersDatum.SpawnPosition;
                float num4 = Vector3.DistanceSquared(compassPosition, spawnPosition);
                if (num4 < num3) {
                    num3 = num4;
                    v2 = spawnPosition;
                }
            }
            return v2 + new Vector3(0.5f);
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            string value = valuesDictionary.GetValue<string>("Magnets");
            m_magnets = new DynamicArray<Vector3>((IEnumerable<Vector3>)HumanReadableConverter.ValuesListFromString<Vector3>(';', value));
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            base.Save(valuesDictionary);
            string value = HumanReadableConverter.ValuesListToString(';', m_magnets.ToArray());
            valuesDictionary.SetValue("Magnets", value);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            m_magnets.Add(new Vector3(x, y, z));
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            m_magnets.Remove(new Vector3(x, y, z));
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
    }
}