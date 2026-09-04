using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Engine;
using Game;
using GameEntitySystem;
using TemplatesDatabase;
using Random = Game.Random;

namespace Phytomana {
    /// <summary>
    /// 白曦菊：将周围的木头与石头缓慢转化为生息变体（功能花）。
    /// 本身不消耗魔力，故关闭网络接收，避免分摊产魔源输出。
    /// </summary>
    public class TileGrownDaisyFlower : TileFunctionalFlower {
        public class ConvertData {
            public Point3 Point;
            public int SourceContents;
            public int DestinationContents;
            public double ConvertTime;
        }

        public const double ScanInterval = 1.0;
        public const double ConvertDuration = 60.0;
        public const float MinParticleInterval = 1.3f;
        public const float MaxParticleInterval = 2.6f;
        public const float ParticleSize = 0.5f;
        public const float ParticleDuration = 2f;

        public SubsystemParticles m_subsystemParticles;

        public Random m_random = new();

        public int m_graniteBlockIndex;

        public int m_grownWoodBlockIndex;

        public int m_grownStoneBlockIndex;

        public Dictionary<int, int> m_sourceToDestination = [];

        public Dictionary<Point3, ConvertData> m_conversions = [];

        public List<ConvertData> m_dueConversions = [];

        public double m_nextScanTime;

        public double m_nextParticleTime;

        public override bool ReceivesMana => false;

        public TileGrownDaisyFlower(Point3 position) : base(position) { }

        public override void OnPlaced() {
            InitializeTimers();
        }

        public override void OnChunkLoad() {
            InitializeTimers();
        }

        public void InitializeTimers() {
            m_nextScanTime = TotalTime;
            m_nextParticleTime = TotalTime + m_random.Float(MinParticleInterval, MaxParticleInterval);
        }

        public override void FlowerTick() {
            ResolveSubsystems();
            double time = TotalTime;
            ProcessConversions(time);
            if (time >= m_nextScanTime) {
                m_nextScanTime = time + ScanInterval;
                ScanAroundDaisy();
            }
            SpawnFlowerParticles(time);
        }

        public void ProcessConversions(double time) {
            m_dueConversions.Clear();
            foreach (ConvertData conversion in m_conversions.Values) {
                if (time >= conversion.ConvertTime) {
                    m_dueConversions.Add(conversion);
                }
            }
            foreach (ConvertData conversion in m_dueConversions) {
                ApplyConversion(conversion);
            }
        }

        public void ApplyConversion(ConvertData conversion) {
            int x = conversion.Point.X;
            int y = conversion.Point.Y;
            int z = conversion.Point.Z;

            Terrain terrain = Scheduler.m_subsystemTerrain.Terrain;
            TerrainChunk chunk = terrain.GetChunkAtCell(x, z);
            if (chunk == null || chunk.State != TerrainChunkState.Valid) {
                return;
            }

            if (terrain.GetCellContents(x, y, z) == conversion.SourceContents) {
                Scheduler.m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(conversion.DestinationContents, 0, 0));
                Vector3 position = new(x + 0.5f, y + 1, z + 0.5f);
                m_subsystemParticles.AddParticleSystem(CreateManaParticle(position));
            }
            m_conversions.Remove(conversion.Point);
        }

        public void ScanAroundDaisy() {
            int x = Position.X;
            int y = Position.Y;
            int z = Position.Z;

            Terrain terrain = Scheduler.m_subsystemTerrain.Terrain;
            TerrainChunk chunk = terrain.GetChunkAtCell(x, z);
            if (chunk == null || chunk.State != TerrainChunkState.Valid) {
                return;
            }

            for (int i = x - 1; i <= x + 1; i++) {
                for (int j = z - 1; j <= z + 1; j++) {
                    TerrainChunk cellChunk = terrain.GetChunkAtCell(i, j);
                    if (cellChunk == null || cellChunk.State != TerrainChunkState.Valid) {
                        continue;
                    }

                    Point3 point = new(i, y, j);
                    int cellContents = terrain.GetCellContents(i, y, j);
                    m_conversions.TryGetValue(point, out ConvertData conversion);

                    if (m_sourceToDestination.TryGetValue(cellContents, out int destinationContents)) {
                        if (conversion == null) {
                            m_conversions[point] = new ConvertData {
                                Point = point,
                                SourceContents = cellContents,
                                DestinationContents = destinationContents,
                                ConvertTime = TotalTime + ConvertDuration
                            };
                        }
                    }
                    else if (conversion != null) {
                        m_conversions.Remove(point);
                    }
                }
            }
        }

        public void SpawnFlowerParticles(double time) {
            if (m_nextParticleTime > time) {
                return;
            }
            m_nextParticleTime = time + m_random.Float(MinParticleInterval, MaxParticleInterval);
            Vector3 position = new(
                Position.X + m_random.Float(0.2f, 0.5f),
                Position.Y + m_random.Float(0.5f, 0.8f),
                Position.Z + m_random.Float(0.2f, 0.5f)
            );
            m_subsystemParticles.AddParticleSystem(CreateManaParticle(position));
        }

        public ManaParticleSystem CreateManaParticle(Vector3 position) {
            return new ManaParticleSystem(
                position,
                ParticleSize,
                ParticleDuration,
                Color.White
            );
        }

        public void ResolveSubsystems() {
            if (m_subsystemParticles != null) {
                return;
            }
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_graniteBlockIndex = BlocksManager.GetBlockIndex<GraniteBlock>();
            m_grownWoodBlockIndex = BlocksManager.GetBlockIndex<GrownWoodBlock>();
            m_grownStoneBlockIndex = BlocksManager.GetBlockIndex<GrownStoneBlock>();
            int[] woodSourceBlockIndexes = [
                BlocksManager.GetBlockIndex<SpruceWoodBlock>(),
                BlocksManager.GetBlockIndex<BirchWoodBlock>(),
                BlocksManager.GetBlockIndex<OakWoodBlock>(),
                BlocksManager.GetBlockIndex<PoplarWoodBlock>(),
                BlocksManager.GetBlockIndex<MimosaWoodBlock>()
            ];
            foreach (int woodSourceBlockIndex in woodSourceBlockIndexes) {
                m_sourceToDestination[woodSourceBlockIndex] = m_grownWoodBlockIndex;
            }
            m_sourceToDestination[m_graniteBlockIndex] = m_grownStoneBlockIndex;
        }

        public override void SaveData(ValuesDictionary values) {
            base.SaveData(values);
            StringBuilder builder = new();
            foreach (ConvertData conversion in m_conversions.Values) {
                builder.Append(conversion.Point.X.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(conversion.Point.Y.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(conversion.Point.Z.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(conversion.SourceContents.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(conversion.DestinationContents.ToString(CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(Math.Max(0.0, conversion.ConvertTime - TotalTime).ToString(CultureInfo.InvariantCulture));
                builder.Append(';');
            }
            values.SetValue("Conversions", builder.ToString());
        }

        public override void LoadData(ValuesDictionary values) {
            base.LoadData(values);
            string text = values.GetValue("Conversions", string.Empty);
            foreach (string item in text.Split([';'], StringSplitOptions.RemoveEmptyEntries)) {
                string[] array = item.Split([','], StringSplitOptions.None);
                if (array.Length != 6
                    || !int.TryParse(array[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    || !int.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                    || !int.TryParse(array[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int z)
                    || !int.TryParse(array[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sourceContents)
                    || !int.TryParse(array[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int destinationContents)
                    || !double.TryParse(array[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double remaining)) {
                    continue;
                }
                m_conversions[new Point3(x, y, z)] = new ConvertData {
                    Point = new Point3(x, y, z),
                    SourceContents = sourceContents,
                    DestinationContents = destinationContents,
                    ConvertTime = TotalTime + remaining
                };
            }
        }
    }
}
