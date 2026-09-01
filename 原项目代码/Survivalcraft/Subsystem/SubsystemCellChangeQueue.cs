using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemCellChangeQueue : Subsystem, IUpdateable {
        struct CellChange {
            public int RequiredValue;

            public int Value;
        }

        public SubsystemTime m_subsystemTime;

        public SubsystemTerrain m_subsystemTerrain;

        Dictionary<Point3, CellChange> m_toChange = new();

        UpdateOrder IUpdateable.UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
        }

        public void QueueCellChange(int x, int y, int z, int value, bool applyImmediately = false) {
            if (applyImmediately) {
                m_subsystemTerrain.ChangeCell(x, y, z, value);
            }
            else {
                m_toChange[new Point3(x, y, z)] = new CellChange { Value = value, RequiredValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z) };
            }
            if (m_toChange.Count >= 10000) {
                ApplyCellChanges();
            }
        }

        void IUpdateable.Update(float dt) {
            if (m_subsystemTime.PeriodicGameTimeEvent(20.0, 0.0)) {
                ApplyCellChanges();
            }
        }

        void ApplyCellChanges() {
            foreach (KeyValuePair<Point3, CellChange> item in m_toChange) {
                Point3 key = item.Key;
                if (Terrain.ReplaceLight(m_subsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z), 0)
                    == Terrain.ReplaceLight(item.Value.RequiredValue, 0)) {
                    m_subsystemTerrain.ChangeCell(key.X, key.Y, key.Z, item.Value.Value);
                }
            }
            m_toChange.Clear();
        }
    }
}