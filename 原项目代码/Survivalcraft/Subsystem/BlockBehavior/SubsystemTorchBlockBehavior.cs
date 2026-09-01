using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemTorchBlockBehavior : SubsystemBlockBehavior {
        public SubsystemParticles m_subsystemParticles;

        public Dictionary<Point3, FireParticleSystem> m_particleSystemsByCell = [];

        public override int[] HandledBlocks => [31, 17, 132];

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
            switch (Terrain.ExtractContents(cellValueFast)) {
                case 31: {
                    Point3 point = CellFace.FaceToPoint3(Terrain.ExtractData(cellValueFast));
                    int x2 = x - point.X;
                    int y2 = y - point.Y;
                    int z2 = z - point.Z;
                    int cellContents2 = SubsystemTerrain.Terrain.GetCellContents(x2, y2, z2);
                    if (!BlocksManager.Blocks[cellContents2].IsCollidable_(cellValueFast)) {
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
                    break;
                }
                case 132: {
                    int cellContents = SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
                    if (!BlocksManager.Blocks[cellContents].IsCollidable_(cellValueFast)) {
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
                    break;
                }
            }
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddTorch(value, x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveTorch(x, y, z);
        }

        public override void OnBlockModified(int value, int oldValue, int x, int y, int z) {
            RemoveTorch(x, y, z);
            AddTorch(value, x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddTorch(value, x, y, z);
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = new();
            foreach (Point3 key in m_particleSystemsByCell.Keys) {
                if (key.X >= chunk.Origin.X
                    && key.X < chunk.Origin.X + 16
                    && key.Z >= chunk.Origin.Y
                    && key.Z < chunk.Origin.Y + 16) {
                    list.Add(key);
                }
            }
            foreach (Point3 item in list) {
                RemoveTorch(item.X, item.Y, item.Z);
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
        }

        public void AddTorch(int value, int x, int y, int z) {
            Vector3 v;
            float size;
            switch (Terrain.ExtractContents(value)) {
                case 31:
                    switch (Terrain.ExtractData(value)) {
                        case 0: v = new Vector3(0.5f, 0.58f, 0.27f); break;
                        case 1: v = new Vector3(0.27f, 0.58f, 0.5f); break;
                        case 2: v = new Vector3(0.5f, 0.58f, 0.73f); break;
                        case 3: v = new Vector3(0.73f, 0.58f, 0.5f); break;
                        default: v = new Vector3(0.5f, 0.53f, 0.5f); break;
                    }
                    size = 0.15f;
                    break;
                case 132:
                    v = new Vector3(0.5f, 0.1f, 0.5f);
                    size = 0.1f;
                    break;
                default:
                    v = new Vector3(0.5f, 0.2f, 0.5f);
                    size = 0.2f;
                    break;
            }
            FireParticleSystem fireParticleSystem = new(new Vector3(x, y, z) + v, size, 32f);
            m_subsystemParticles.AddParticleSystem(fireParticleSystem);
            m_particleSystemsByCell[new Point3(x, y, z)] = fireParticleSystem;
        }

        public void RemoveTorch(int x, int y, int z) {
            Point3 key = new(x, y, z);
            FireParticleSystem particleSystem = m_particleSystemsByCell[key];
            m_subsystemParticles.RemoveParticleSystem(particleSystem);
            m_particleSystemsByCell.Remove(key);
        }
    }
}