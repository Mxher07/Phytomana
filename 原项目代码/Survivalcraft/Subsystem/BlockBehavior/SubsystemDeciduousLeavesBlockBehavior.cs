using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemDeciduousLeavesBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable {
        struct LeafParticles {
            public double Time;

            public Point3 Position;
        }

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemSeasons m_subsystemSeasons;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemTime m_subsystemTime;

        public SubsystemGameWidgets m_subsystemGameWidgets;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemCellChangeQueue m_subsystemCellChangeQueue;

        Random m_random = new();

        DynamicArray<LeafParticles> m_leafParticles = [];

        DynamicArray<LeafParticles> m_tmpLeafParticles = [];

        public override int[] HandledBlocks => [];

        UpdateOrder IUpdateable.UpdateOrder => UpdateOrder.Default;

        public void CreateFallenLeaves(Point3 p, bool applyImmediately) {
            int? num = null;
            while (p.Y >= 1
                && p.Y < 256) {
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z);
                if (num.HasValue) {
                    if (SubsystemFallenLeavesBlockBehavior.CanSupportFallenLeaves(cellValue)
                        && SubsystemFallenLeavesBlockBehavior.CanBeReplacedByFallenLeaves(num.Value)) {
                        m_subsystemCellChangeQueue.QueueCellChange(p.X, p.Y + 1, p.Z, Terrain.MakeBlockValue(261), applyImmediately);
                        break;
                    }
                    if (SubsystemFallenLeavesBlockBehavior.StopsFallenLeaves(cellValue)) {
                        break;
                    }
                }
                num = cellValue;
                p.Y--;
            }
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            UpdateTimeOfYear(value, x, y, z, true);
            QueueLeafParticles(value, x, y, z);
        }

        public override void OnPoll(int value, int x, int y, int z, int pollPass) {
            UpdateTimeOfYear(value, x, y, z, false);
            QueueLeafParticles(value, x, y, z);
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemSeasons = Project.FindSubsystem<SubsystemSeasons>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemGameWidgets = Project.FindSubsystem<SubsystemGameWidgets>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemCellChangeQueue = Project.FindSubsystem<SubsystemCellChangeQueue>(true);
        }

        void IUpdateable.Update(float dt) {
            if (!m_subsystemTime.PeriodicGameTimeEvent(1.0, 0.0)) {
                return;
            }
            foreach (LeafParticles leafParticle in m_leafParticles) {
                if (m_subsystemTime.GameTime >= leafParticle.Time) {
                    if (m_subsystemGameWidgets.CalculateDistanceFromNearestView(new Vector3(leafParticle.Position)) < 32f) {
                        int cellValue = m_subsystemTerrain.Terrain.GetCellValue(
                            leafParticle.Position.X,
                            leafParticle.Position.Y,
                            leafParticle.Position.Z
                        );
                        int num = Terrain.ExtractContents(cellValue);
                        if (BlocksManager.Blocks[num] is DeciduousLeavesBlock deciduousLeavesBlock
                            && deciduousLeavesBlock.GetLeafDropProbability(cellValue) > 0f) {
                            m_subsystemParticles.AddParticleSystem(
                                new LeavesParticleSystem(m_subsystemTerrain, leafParticle.Position, m_random.Int(1, 2), true, false, cellValue)
                            );
                        }
                    }
                }
                else {
                    m_tmpLeafParticles.Add(leafParticle);
                }
            }
            Utilities.Swap(ref m_leafParticles, ref m_tmpLeafParticles);
            m_tmpLeafParticles.Clear();
        }

        void UpdateTimeOfYear(int value, int x, int y, int z, bool applyImmediately) {
            float num = 0.03f * MathUtils.Hash((uint)(x + y * 59 + z * 3319)) / 4.2949673E+09f;
            float timeOfYear = IntervalUtils.Normalize(m_subsystemGameInfo.WorldSettings.TimeOfYear + num);
            DeciduousLeavesBlock obj = (DeciduousLeavesBlock)BlocksManager.Blocks[Terrain.ExtractContents(value)];
            int num2 = Terrain.ExtractData(value);
            int num3 = obj.SetTimeOfYear(num2, timeOfYear);
            if (num3 != num2) {
                int value2 = Terrain.ReplaceData(value, num3);
                m_subsystemCellChangeQueue.QueueCellChange(x, y, z, value2, applyImmediately);
                Season season = DeciduousLeavesBlock.GetSeason(num2);
                if (DeciduousLeavesBlock.GetSeason(num3) == Season.Winter
                    && season != Season.Winter) {
                    CreateFallenLeaves(new Point3(x, y, z), applyImmediately);
                }
            }
        }

        void QueueLeafParticles(int value, int x, int y, int z) {
            DeciduousLeavesBlock deciduousLeavesBlock = (DeciduousLeavesBlock)BlocksManager.Blocks[Terrain.ExtractContents(value)];
            if (m_leafParticles.Count < 30000
                && m_random.Bool(deciduousLeavesBlock.GetLeafDropProbability(value) / 60f * 60f)
                && m_subsystemGameWidgets.CalculateDistanceFromNearestView(new Vector3(x, y, z)) < 128f) {
                m_leafParticles.Add(new LeafParticles { Position = new Point3(x, y, z), Time = m_subsystemTime.GameTime + m_random.Float(0f, 60f) });
            }
        }
    }
}