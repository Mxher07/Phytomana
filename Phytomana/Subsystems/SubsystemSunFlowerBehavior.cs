using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemSunPowerBehavior : SubsystemBlockBehavior, IUpdateable {
        public class SunPowerData {
            public Point3 Point;
            public int Contents;
            public double NextScanTime;
            public bool IsBurning;
            public double BurnEndTime;
            public int BurnHeatLevel;
            public double NextProductionParticleTime;
            public double NextDrainTime;
            public double NextBlueParticleTime;
            public float IdleIsolatedElapsed;
        }

        public const double ScanInterval = 1.0;
        public const double DrainInterval = 1.0;
        public const double ProductionParticleInterval = 3.0;
        public const double DrainParticleInterval = 5.0;
        public const float BaseManaRate = 20f;
        public const float ManaRatePerLevel = 10f;
        public const float ManaRatePeriod = 5f;
        public const float DrainAmount = 5f;
        public const float IsolatedDrainDelay = 1.5f;
        public const float TransferRate = 75f / 0.5f;

        public SubsystemGameInfo m_subsystemGameInfo;
        public SubsystemParticles m_subsystemParticles;
        public SubsystemPickables m_subsystemPickables;
        public SubsystemMana m_subsystemMana;

        public Dictionary<Point3, SunPowerData> m_sunpowers = [];

        public override int[] HandledBlocks => [BlocksManager.GetBlockIndex<SunPowerFlower>()];

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemMana = Project.FindSubsystem<SubsystemMana>(true);
        }

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddSunPower(x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddSunPower(x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveSunPower(new Point3(x, y, z));
            m_subsystemMana.RemoveBlockMana(new Point3(x, y, z));
        }

        public override void OnChunkDiscarding(TerrainChunk chunk) {
            List<Point3> list = [];
            foreach (Point3 point in m_sunpowers.Keys) {
                if (point.X >= chunk.Origin.X
                    && point.X < chunk.Origin.X + 16
                    && point.Z >= chunk.Origin.Y
                    && point.Z < chunk.Origin.Y + 16) {
                    list.Add(point);
                }
            }
            foreach (Point3 point in list) {
                RemoveSunPower(point);
            }
        }

        public virtual void Update(float dt) {
            double time = m_subsystemGameInfo.TotalElapsedGameTime;
            foreach (SunPowerData data in m_sunpowers.Values) {
                UpdateSunPower(data, dt, time);
            }
        }

        public void UpdateSunPower(SunPowerData data, float dt, double time) {
            if (data.IsBurning) {
                if (time >= data.BurnEndTime) {
                    data.IsBurning = false;
                }
                else {
                    data.IdleIsolatedElapsed = 0f;
                    float rate = (BaseManaRate + ManaRatePerLevel * data.BurnHeatLevel) / ManaRatePeriod;
                    m_subsystemMana.AddMana(data.Point, rate * dt);
                    if (time >= data.NextProductionParticleTime) {
                        data.NextProductionParticleTime = time + ProductionParticleInterval;
                        SpawnManaParticle(data.Point, 0.6f, 0.5f, 2f, Color.Red);
                    }
                }
            }
            else {
                if (m_subsystemMana.HasSpreaderNearby(data.Point)) {
                    data.IdleIsolatedElapsed = 0f;
                }
                else {
                    data.IdleIsolatedElapsed += dt;
                }
                if (data.IdleIsolatedElapsed > IsolatedDrainDelay) {
                    if (time >= data.NextDrainTime) {
                        data.NextDrainTime = time + DrainInterval;
                        if (m_subsystemMana.GetManaAmount(data.Point) > 0f) {
                            m_subsystemMana.RemoveMana(data.Point, DrainAmount);
                        }
                    }
                    if (time >= data.NextBlueParticleTime) {
                        data.NextBlueParticleTime = time + DrainParticleInterval;
                        if (m_subsystemMana.GetManaAmount(data.Point) > 0f) {
                            SpawnManaParticle(data.Point, 0.9f, 0.25f, 1.5f, Color.Blue);
                        }
                    }
                }
                if (time >= data.NextScanTime) {
                    data.NextScanTime = time + ScanInterval;
                    TryEatFuel(data);
                }
            }
            m_subsystemMana.TransferManaToBestSpreader(data.Point, TransferRate * dt);
        }

        public void SpawnManaParticle(Point3 point, float yOffset, float size, float duration, Color color) {
            m_subsystemParticles.AddParticleSystem(new ManaParticleSystem(
                new Vector3(point.X + 0.5f, point.Y + yOffset, point.Z + 0.5f),
                size,
                duration,
                color
            ));
        }

        public bool IsWorkingMana(Point3 point) => m_sunpowers.TryGetValue(point, out SunPowerData data) && data.IsBurning;

        public float GetProductionRate(Point3 point) {
            if (m_sunpowers.TryGetValue(point, out SunPowerData data) && data.IsBurning) {
                return (BaseManaRate + ManaRatePerLevel * data.BurnHeatLevel) / ManaRatePeriod;
            }
            return 0f;
        }

        public bool IsDrainingMana(Point3 point) {
            if (m_sunpowers.TryGetValue(point, out SunPowerData data) && !data.IsBurning) {
                return data.IdleIsolatedElapsed > IsolatedDrainDelay;
            }
            return false;
        }

        public void TryEatFuel(SunPowerData data) {
            if (m_subsystemMana.GetManaAmount(data.Point) >= m_subsystemMana.GetMaxManaAmount(data.Contents)) {
                return;
            }

            Pickable best = null;
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (!IsPickableInCell(pickable, data.Point)) {
                    continue;
                }
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
                if (block.GetFuelHeatLevel(pickable.Value) <= 0f) {
                    continue;
                }
                if (block.GetFuelFireDuration(pickable.Value) <= 0f) {
                    continue;
                }
                if (best == null || pickable.Count > best.Count) {
                    best = pickable;
                }
            }
            if (best == null) {
                return;
            }

            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                if (pickable.ToRemove) {
                    continue;
                }
                if (!IsPickableInCell(pickable, data.Point)) {
                    continue;
                }
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
                if (block.GetFuelHeatLevel(pickable.Value) > 0f) {
                    pickable.ToRemove = true;
                }
            }

            Block bestBlock = BlocksManager.Blocks[Terrain.ExtractContents(best.Value)];
            data.IsBurning = true;
            data.BurnHeatLevel = (int)bestBlock.GetFuelHeatLevel(best.Value);
            data.BurnEndTime = m_subsystemGameInfo.TotalElapsedGameTime + bestBlock.GetFuelFireDuration(best.Value);
            data.NextProductionParticleTime = m_subsystemGameInfo.TotalElapsedGameTime + ProductionParticleInterval;
        }

        public bool IsPickableInCell(Pickable pickable, Point3 cell) {
            Vector3 position = pickable.Position;
            return position.X >= cell.X
                && position.X < cell.X + 1f
                && position.Z >= cell.Z
                && position.Z < cell.Z + 1f
                && position.Y >= cell.Y - 0.5f
                && position.Y < cell.Y + 1.5f;
        }

        public void AddSunPower(int x, int y, int z) {
            Point3 point = new(x, y, z);
            m_sunpowers[point] = new SunPowerData {
                Point = point,
                Contents = BlocksManager.GetBlockIndex<SunPowerFlower>(),
                NextScanTime = m_subsystemGameInfo.TotalElapsedGameTime,
                NextDrainTime = m_subsystemGameInfo.TotalElapsedGameTime,
                NextBlueParticleTime = m_subsystemGameInfo.TotalElapsedGameTime + DrainParticleInterval,
                NextProductionParticleTime = m_subsystemGameInfo.TotalElapsedGameTime
            };
        }

        public void RemoveSunPower(Point3 point) {
            m_sunpowers.Remove(point);
        }
    }
}