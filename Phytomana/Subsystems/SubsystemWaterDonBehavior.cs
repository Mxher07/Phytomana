using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemWaterDonBehavior : SubsystemBlockBehavior, IUpdateable {
        public class WaterDonData {
            public Point3 Point;
            public int Contents;
            public double NextScanTime;
            public bool IsAbsorbing;
            public double AbsorbEndTime;
            public double NextParticleTime;
        }

        public const double ScanInterval = 1.0;
        public const double AbsorbDuration = 3.0;
        public const double ParticleInterval = 7.5;
        public const float ManaRate = 7f / 1.65f;
        public const float TransferRate = 35f / 0.5f;

        public SubsystemGameInfo m_subsystemGameInfo;
        public SubsystemParticles m_subsystemParticles;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemMana m_subsystemMana;
        public Random m_random = new();

        public Dictionary<Point3, WaterDonData> m_waterDons = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<WaterDonFlower>()];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddWaterDon(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddWaterDon(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveWaterDon(new Point3(x, y, z));
            m_subsystemMana.RemoveBlockMana(new Point3(x, y, z));
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = [];
            foreach (Point3 point in m_waterDons.Keys) {
                if (point.X >= chunk.Origin.X
                    && point.X < chunk.Origin.X + 16
                    && point.Z >= chunk.Origin.Y
                    && point.Z < chunk.Origin.Y + 16) {
                    list.Add(point);
                }
            }
            foreach (Point3 point in list) {
                RemoveWaterDon(point);
            }
        }

        public void Update(float dt) {
            double time = m_subsystemGameInfo.TotalElapsedGameTime;
            foreach (WaterDonData data in m_waterDons.Values) {
                UpdateWaterDon(data, dt, time);
            }
        }

        public void UpdateWaterDon(WaterDonData data, float dt, double time) {
            m_subsystemMana.TransferManaToBestSpreader(data.Point, TransferRate * dt);
            if (data.IsAbsorbing) {
                if (time >= data.AbsorbEndTime) {
                    data.IsAbsorbing = false;
                    data.NextScanTime = time;
                }
                else {
                    m_subsystemMana.AddMana(data.Point, ManaRate * dt);
                }
            }
            else if (time >= data.NextScanTime) {
                data.NextScanTime = time + ScanInterval;
                TryEatWater(data);
            }
            if (time >= data.NextParticleTime) {
                data.NextParticleTime = time + ParticleInterval;
                SpawnManaParticle(data.Point);
            }
        }

        public void TryEatWater(WaterDonData data) {
            if (m_subsystemMana.GetManaAmount(data.Point) >= m_subsystemMana.GetMaxManaAmount(data.Contents)) {
                return;
            }
            List<Point3> staticWaterCells = [];
            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    int x = data.Point.X + dx;
                    int z = data.Point.Z + dz;
                    int value = m_subsystemTerrain.Terrain.GetCellValue(x, data.Point.Y, z);
                    if (Terrain.ExtractContents(value) == WaterBlock.Index
                        && FluidBlock.GetLevel(Terrain.ExtractData(value)) == 0) {
                        staticWaterCells.Add(new Point3(x, data.Point.Y, z));
                    }
                }
            }
            if (staticWaterCells.Count == 0) {
                return;
            }
            Point3 waterCell = staticWaterCells[m_random.Int(staticWaterCells.Count)];
            m_subsystemTerrain.DestroyCell(0, waterCell.X, waterCell.Y, waterCell.Z, 0, false, false);
            SpawnDrinkParticle(waterCell);
            data.IsAbsorbing = true;
            data.AbsorbEndTime = m_subsystemGameInfo.TotalElapsedGameTime + AbsorbDuration;
        }

        public void SpawnDrinkParticle(Point3 waterCell) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(waterCell.X + 0.5f, waterCell.Y + 0.3f, waterCell.Z + 0.5f),
                0.25f,
                1.5f,
                Color.SkyBlue
            ));
        }

        public void SpawnManaParticle(Point3 point) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(point.X + 0.5f, point.Y + 0.3f, point.Z + 0.5f),
                0.25f,
                1.5f,
                Color.SkyBlue
            ));
        }

        public bool IsWorkingMana(Point3 point) => m_waterDons.TryGetValue(point, out WaterDonData data) && data.IsAbsorbing;

        public float GetProductionRate(Point3 point) => m_waterDons.TryGetValue(point, out WaterDonData data) && data.IsAbsorbing ? ManaRate : 0f;

        public void AddWaterDon(int x, int y, int z) {
            Point3 point = new(x, y, z);
            m_waterDons[point] = new WaterDonData {
                Point = point,
                Contents = BlocksManager.GetBlockIndex<WaterDonFlower>(),
                NextScanTime = m_subsystemGameInfo.TotalElapsedGameTime,
                NextParticleTime = m_subsystemGameInfo.TotalElapsedGameTime + ParticleInterval
            };
        }

        public void RemoveWaterDon(Point3 point) {
            m_waterDons.Remove(point);
        }
    }
}