using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;
using Game;

namespace Game {
    public class SubsystemGrownDaisyBehavior : SubsystemBlockBehavior, IUpdateable {
        public class DaisyData {
            public Point3 Point;
            public double NextScanTime;
        }

        public class ConvertData {
    public Point3 DaisyPoint;
    public Point3 Point;
    public int SourceContents;
    public int DestinationContents;
    public double ConvertTime;
    public bool ParticleSpawned;
}

        public const double ScanInterval = 1.0;
        public const double ConvertDuration = 60.0;

        public SubsystemGameInfo m_subsystemGameInfo;

        public int m_graniteBlockIndex;
        public int m_grownWoodBlockIndex;
        public int m_grownStoneBlockIndex;

        public int[] m_woodSourceBlockIndexes = [
            BlocksManager.GetBlockIndex<SpruceWoodBlock>(),
            BlocksManager.GetBlockIndex<BirchWoodBlock>(),
            BlocksManager.GetBlockIndex<OakWoodBlock>(),
            BlocksManager.GetBlockIndex<PoplarWoodBlock>(),
            BlocksManager.GetBlockIndex<MimosaWoodBlock>()
        ];

        public Dictionary<int, int> m_sourceToDestination = [];
        public Dictionary<Point3, DaisyData> m_daisies = [];
        public Dictionary<Point3, ConvertData> m_conversions = [];
        public List<ConvertData> m_dueConversions = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<GrownDaisyFlower>()];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;
        
        public SubsystemTerrain m_subsystemTerrain;
        public List<ManaParticleSystem> m_activeParticleSystems = [];

        public override void Load(ValuesDictionary valuesDictionary) {
    base.Load(valuesDictionary);
    m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
    m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
    m_graniteBlockIndex = BlocksManager.GetBlockIndex<GraniteBlock>();
    m_grownWoodBlockIndex = BlocksManager.GetBlockIndex<GrownWoodBlock>();
    m_grownStoneBlockIndex = BlocksManager.GetBlockIndex<GrownStoneBlock>();
    foreach (int woodSourceBlockIndex in m_woodSourceBlockIndexes) {
        m_sourceToDestination[woodSourceBlockIndex] = m_grownWoodBlockIndex;
    }
    m_sourceToDestination[m_graniteBlockIndex] = m_grownStoneBlockIndex;
}

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddDaisy(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddDaisy(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveDaisy(new Point3(x, y, z));
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = [];
            foreach (Point3 point in m_daisies.Keys) {
                if (point.X >= chunk.Origin.X
                    && point.X < chunk.Origin.X + 16
                    && point.Z >= chunk.Origin.Y
                    && point.Z < chunk.Origin.Y + 16) {
                    list.Add(point);
                }
            }
            foreach (Point3 point in list) {
                RemoveDaisy(point);
            }
        }

        public virtual void Update(float dt) {
    double time = m_subsystemGameInfo.TotalElapsedGameTime;
    
    UpdateParticleSystems(dt);
    
    ProcessConversions(time);
    ScanDaisies(time);
    SpawnIdleParticles(time);
}
        
        public void SpawnIdleParticles(double time) {
    foreach (ConvertData conversion in m_conversions.Values) {
        if (time >= conversion.ConvertTime - 2f && !conversion.ParticleSpawned) {
            conversion.ParticleSpawned = true;
            
            Vector3 position = new Vector3(conversion.Point.X, conversion.Point.Y, conversion.Point.Z);
            
            ManaParticleSystem particleSystem = new ManaParticleSystem(
                position,    // 位置
                2f,          // 大小
                2f,          // 持续时间
                Color.White  // 白色
            );
            
            m_activeParticleSystems.Add(particleSystem);
        }
    }
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
    
    TerrainChunk chunk = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
    if (chunk == null || chunk.State != TerrainChunkState.Valid) {
        return;
    }
    
    if (m_subsystemTerrain.Terrain.GetCellContents(x, y, z) == conversion.SourceContents) {
        m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(conversion.DestinationContents, 0, 0));
        
        Vector3 position = new Vector3(x, y, z);
        ManaParticleSystem completionParticle = new ManaParticleSystem(
            position,   // 位置
            2f,         // 大小
            2f,         // 持续时间
            Color.White // 白色
        );
        m_activeParticleSystems.Add(completionParticle);
    }
    m_conversions.Remove(conversion.Point);
}

        public void ScanDaisies(double time) {
            foreach (DaisyData daisy in m_daisies.Values) {
                if (daisy.NextScanTime <= time) {
                    daisy.NextScanTime = time + ScanInterval;
                    ScanAroundDaisy(daisy);
                }
            }
        }

        public void ScanAroundDaisy(DaisyData daisy) {
    int x = daisy.Point.X;
    int y = daisy.Point.Y;
    int z = daisy.Point.Z;
    
    TerrainChunk chunk = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
    if (chunk == null || chunk.State != TerrainChunkState.Valid) {
        return;
    }
    
    for (int i = x - 1; i <= x + 1; i++) {
        for (int j = z - 1; j <= z + 1; j++) {
            TerrainChunk cellChunk = m_subsystemTerrain.Terrain.GetChunkAtCell(i, j);
            if (cellChunk == null || cellChunk.State != TerrainChunkState.Valid) {
                continue;
            }
            
            Point3 point = new(i, y, j);
            int cellContents = m_subsystemTerrain.Terrain.GetCellContents(i, y, j);
            m_conversions.TryGetValue(point, out ConvertData conversion);
            
            if (m_sourceToDestination.TryGetValue(cellContents, out int destinationContents)) {
                if (conversion == null) {
                    AddConversion(daisy.Point, point, cellContents, destinationContents);
                }
            }
            else if (conversion != null && conversion.DaisyPoint == daisy.Point) {
                m_conversions.Remove(point);
            }
        }
    }
}

    public void UpdateParticleSystems(float dt) {
    List<ManaParticleSystem> finishedSystems = [];
    foreach (ManaParticleSystem system in m_activeParticleSystems) {
        bool isFinished = system.Simulate(dt);
        if (isFinished) {
            finishedSystems.Add(system);
        }
    }
    
    foreach (ManaParticleSystem system in finishedSystems) {
        m_activeParticleSystems.Remove(system);
    }
}


        public void AddConversion(Point3 daisyPoint, Point3 point, int sourceContents, int destinationContents) {
    m_conversions[point] = new ConvertData {
        DaisyPoint = daisyPoint,
        Point = point,
        SourceContents = sourceContents,
        DestinationContents = destinationContents,
        ConvertTime = m_subsystemGameInfo.TotalElapsedGameTime + ConvertDuration,
        ParticleSpawned = false // 新增
    };
}

        public void AddDaisy(int x, int y, int z) {
            Point3 point = new(x, y, z);
            m_daisies[point] = new DaisyData {
                Point = point,
                NextScanTime = m_subsystemGameInfo.TotalElapsedGameTime
            };
        }

        public void RemoveDaisy(Point3 point) {
            m_daisies.Remove(point);
            List<Point3> list = [];
            foreach (ConvertData conversion in m_conversions.Values) {
                if (conversion.DaisyPoint == point) {
                    list.Add(conversion.Point);
                }
            }
            foreach (Point3 conversionPoint in list) {
                m_conversions.Remove(conversionPoint);
            }
        }
    }
}