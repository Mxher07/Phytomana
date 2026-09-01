using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemSoilBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public Random m_random = new();

        public Dictionary<Point3, bool> m_toDegrade = [];

        public Dictionary<Point3, bool> m_toHydrate = [];

        public override int[] HandledBlocks => [168];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody) {
            if (componentBody.Mass > 20f
                && !componentBody.IsCrouching) {
                Vector3 velocity2 = componentBody.Velocity;
                if (velocity2.Y < -3f
                    || (velocity2.Y < 0f && m_random.Float(0f, 1f) < 1.5f * m_subsystemTime.GameTimeDelta && velocity2.LengthSquared() > 1f)) {
                    m_toDegrade[cellFace.Point] = true;
                }
            }
        }

        public override void OnPoll(int value, int x, int y, int z, int pollPass) {
            bool hydration = SoilBlock.GetHydration(Terrain.ExtractData(value));
            if (DetermineHydration(x, y, z, 3)) {
                if (!hydration) {
                    m_toHydrate[new Point3(x, y, z)] = true;
                }
            }
            else if (hydration) {
                m_toHydrate[new Point3(x, y, z)] = false;
            }
        }

        public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
            if (DegradesSoilIfOnTopOfIt(cellValue)) {
                int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
                SubsystemTerrain.ChangeCell(x, y, z, Terrain.ReplaceContents(cellValue2, 2));
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
        }

        public virtual void Update(float dt) {
            if (m_subsystemTime.PeriodicGameTimeEvent(2.5, 0.0)) {
                foreach (Point3 key2 in m_toDegrade.Keys) {
                    if (SubsystemTerrain.Terrain.GetCellContents(key2.X, key2.Y, key2.Z) == 168) {
                        int cellValue = SubsystemTerrain.Terrain.GetCellValue(key2.X, key2.Y, key2.Z);
                        SubsystemTerrain.ChangeCell(key2.X, key2.Y, key2.Z, Terrain.ReplaceContents(cellValue, 2));
                    }
                }
                m_toDegrade.Clear();
            }
            if (m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0)) {
                foreach (KeyValuePair<Point3, bool> item in m_toHydrate) {
                    Point3 key = item.Key;
                    bool value = item.Value;
                    int cellValue2 = SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z);
                    if (Terrain.ExtractContents(cellValue2) == 168) {
                        int data = SoilBlock.SetHydration(Terrain.ExtractData(cellValue2), value);
                        int value2 = Terrain.ReplaceData(cellValue2, data);
                        SubsystemTerrain.ChangeCell(key.X, key.Y, key.Z, value2);
                    }
                }
                m_toHydrate.Clear();
            }
        }

        public bool DegradesSoilIfOnTopOfIt(int value) {
            int num = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[num];
            if (!block.IsFaceTransparent(SubsystemTerrain, 5, value)) {
                return block.IsCollidable_(value);
            }
            return false;
        }

        public bool DetermineHydration(int x, int y, int z, int steps) {
            if (steps > 0
                && y > 0
                && y < 254) {
                if (DetermineHydrationHelper(x - 1, y, z, steps - 1)) {
                    return true;
                }
                if (DetermineHydrationHelper(x + 1, y, z, steps - 1)) {
                    return true;
                }
                if (DetermineHydrationHelper(x, y, z - 1, steps - 1)) {
                    return true;
                }
                if (DetermineHydrationHelper(x, y, z + 1, steps - 1)) {
                    return true;
                }
                if (steps >= 2) {
                    if (DetermineHydrationHelper(x, y - 1, z, steps - 2)) {
                        return true;
                    }
                    if (DetermineHydrationHelper(x, y + 1, z, steps - 2)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool DetermineHydrationHelper(int x, int y, int z, int steps) {
            int cellValueFast = SubsystemTerrain.Terrain.GetCellValueFast(x, y, z);
            int num = Terrain.ExtractContents(cellValueFast);
            int data = Terrain.ExtractData(cellValueFast);
            switch (num) {
                case 18: return true;
                case 168:
                    if (SoilBlock.GetHydration(data)) {
                        return DetermineHydration(x, y, z, steps);
                    }
                    break;
            }
            if (num == 2) {
                return DetermineHydration(x, y, z, steps);
            }
            return false;
        }
    }
}