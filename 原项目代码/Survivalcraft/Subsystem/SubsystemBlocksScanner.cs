using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemBlocksScanner : Subsystem, IUpdateable {
        public float ScanPeriod = 60f;

        public SubsystemPollableBlockBehavior[][] m_pollableBehaviorsByContents;

        public Point2 m_pollChunkCoordinates;

        public int m_pollX;

        public int m_pollZ;

        public int m_pollPass;

        public float m_pollShaftsCount;

        public SubsystemTime m_subsystemTime;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

        /// <summary>
        ///     每帧会进行多少xz格的方块检查
        /// </summary>
        public float MaxShaftsToPollPerFrame = 500f;

        public UpdateOrder UpdateOrder => UpdateOrder.BlocksScanner;

        public virtual Action<TerrainChunk> ScanningChunkCompleted { get; set; }

        public virtual void Update(float dt) {
            Terrain terrain = m_subsystemTerrain.Terrain;
            m_pollShaftsCount += terrain.AllocatedChunks.Length * 16 * 16 * dt / ScanPeriod;
            m_pollShaftsCount = Math.Clamp(m_pollShaftsCount, 0f, MaxShaftsToPollPerFrame);
            TerrainChunk terrainChunk = terrain.LoopChunks(m_pollChunkCoordinates.X, m_pollChunkCoordinates.Y, false);
            if (terrainChunk == null) {
                return;
            }
            while (m_pollShaftsCount >= 1f) {
                if (terrainChunk.State <= TerrainChunkState.InvalidContents4) {
                    m_pollShaftsCount -= 256f;
                }
                else {
                    while (m_pollX < 16) {
                        while (m_pollZ < 16) {
                            if (m_pollShaftsCount < 1f) {
                                return;
                            }
                            m_pollShaftsCount -= 1f;
                            int topHeightFast = terrainChunk.GetTopHeightFast(m_pollX, m_pollZ);
                            int num = TerrainChunk.CalculateCellIndex(m_pollX, 0, m_pollZ);
                            int num2 = 0;
                            while (num2 <= topHeightFast) {
                                int cellValueFast = terrainChunk.GetCellValueFast(num);
                                int num3 = Terrain.ExtractContents(cellValueFast);
                                if (num3 != 0) {
                                    SubsystemPollableBlockBehavior[] array = m_pollableBehaviorsByContents[num3];
                                    for (int i = 0; i < array.Length; i++) {
                                        int x = terrainChunk.Origin.X + m_pollX;
                                        int y = num2;
                                        int z = terrainChunk.Origin.Y + m_pollZ;
                                        try {
                                            array[i].OnPoll(cellValueFast, x, y, z, m_pollPass);
                                        }
                                        catch (Exception e) {
                                            Log.Error(
                                                $"{array[i]} Poll {BlocksManager.Blocks[num3].GetType().Name} {cellValueFast} at ({x},{y},{z}) \n{e}"
                                            );
                                        }
                                    }
                                }
                                num2++;
                                num++;
                            }
                            m_pollZ++;
                        }
                        m_pollZ = 0;
                        m_pollX++;
                    }
                    m_pollX = 0;
                }
                ScanningChunkCompleted?.Invoke(terrainChunk);
                terrainChunk = terrain.LoopChunks(terrainChunk.Coords.X, terrainChunk.Coords.Y, true, out bool hasLooped);
                if (terrainChunk == null) {
                    break;
                }
                if (hasLooped) {
                    m_pollPass++;
                }
                m_pollChunkCoordinates = terrainChunk.Coords;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true);
            m_pollChunkCoordinates = valuesDictionary.GetValue<Point2>("PollChunkCoordinates");
            Point2 value = valuesDictionary.GetValue<Point2>("PollPoint");
            m_pollX = value.X;
            m_pollZ = value.Y;
            m_pollPass = valuesDictionary.GetValue<int>("PollPass");
            m_pollableBehaviorsByContents = new SubsystemPollableBlockBehavior[BlocksManager.Blocks.Length][];
            for (int i = 0; i < m_pollableBehaviorsByContents.Length; i++) {
                m_pollableBehaviorsByContents[i] = (from s in m_subsystemBlockBehaviors.GetBlockBehaviors(i)
                    where s is SubsystemPollableBlockBehavior
                    select (SubsystemPollableBlockBehavior)s).ToArray();
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            valuesDictionary.SetValue("PollChunkCoordinates", m_pollChunkCoordinates);
            valuesDictionary.SetValue("PollPoint", new Point2(m_pollX, m_pollZ));
            valuesDictionary.SetValue("PollPass", m_pollPass);
        }
    }
}