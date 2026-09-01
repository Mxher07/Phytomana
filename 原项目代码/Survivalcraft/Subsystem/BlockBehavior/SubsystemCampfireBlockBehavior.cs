using Engine;
using TemplatesDatabase;

namespace Game {
    public class SubsystemCampfireBlockBehavior : SubsystemBlockBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemWeather m_subsystemWeather;

        public SubsystemAmbientSounds m_subsystemAmbientSounds;

        public Dictionary<Point3, FireParticleSystem> m_particleSystemsByCell = [];

        public float m_fireSoundVolume;

        public Random m_random = new();

        public int m_updateIndex;

        public List<Point3> m_toReduce = [];

        public Dictionary<Point3, FireParticleSystem>.KeyCollection Campfires => m_particleSystemsByCell.Keys;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override int[] HandledBlocks => [];

        public virtual void Update(float dt) {
            if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0)) {
                m_updateIndex++;
                foreach (Point3 key in m_particleSystemsByCell.Keys) {
                    PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(key.X, key.Z);
                    if ((precipitationShaftInfo.Intensity > 0f && key.Y >= precipitationShaftInfo.YLimit - 1)
                        || m_updateIndex % 6 == 0) {
                        m_toReduce.Add(key);
                    }
                }
                foreach (Point3 item in m_toReduce) {
                    ResizeCampfire(item.X, item.Y, item.Z, -1, true);
                }
                m_toReduce.Clear();
            }
            if (Time.PeriodicEvent(0.5, 0.0)) {
                float num = float.MaxValue;
                foreach (Point3 key2 in m_particleSystemsByCell.Keys) {
                    float x = m_subsystemAmbientSounds.SubsystemAudio.CalculateListenerDistanceSquared(new Vector3(key2.X, key2.Y, key2.Z));
                    num = MathUtils.Min(num, x);
                }
                m_fireSoundVolume = m_subsystemAmbientSounds.SubsystemAudio.CalculateVolume(MathF.Sqrt(num), 2f);
            }
            m_subsystemAmbientSounds.FireSoundVolume = MathUtils.Max(m_subsystemAmbientSounds.FireSoundVolume, m_fireSoundVolume);
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

        public override void OnBlockAdded(int value, int oldValue, int x, int y, int z) {
            AddCampfireParticleSystem(value, x, y, z);
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            RemoveCampfireParticleSystem(x, y, z);
        }

        public override void OnBlockModified(int value, int oldValue, int x, int y, int z) {
            RemoveCampfireParticleSystem(x, y, z);
            AddCampfireParticleSystem(value, x, y, z);
        }

        public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded) {
            AddCampfireParticleSystem(value, x, y, z);
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
                ResizeCampfire(item.X, item.Y, item.Z, -15, false);
                RemoveCampfireParticleSystem(item.X, item.Y, item.Z);
            }
        }

        public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem) {
            if (!worldItem.ToRemove
                && AddFuel(cellFace.X, cellFace.Y, cellFace.Z, worldItem.Value, (worldItem as Pickable)?.Count ?? 1)) {
                worldItem.ToRemove = true;
            }
        }

        public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner) {
            if (AddFuel(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z, componentMiner.ActiveBlockValue, 1)) {
                componentMiner.RemoveActiveTool(1);
            }
            return true;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemWeather = Project.FindSubsystem<SubsystemWeather>(true);
            m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(true);
        }

        public void AddCampfireParticleSystem(int value, int x, int y, int z) {
            int num = Terrain.ExtractData(value);
            if (num > 0) {
                Vector3 v = new(0.5f, 0.15f, 0.5f);
                float size = MathUtils.Lerp(0.2f, 0.5f, num / 15f);
                FireParticleSystem fireParticleSystem = new(new Vector3(x, y, z) + v, size, 256f);
                m_subsystemParticles.AddParticleSystem(fireParticleSystem);
                m_particleSystemsByCell[new Point3(x, y, z)] = fireParticleSystem;
            }
        }

        public void RemoveCampfireParticleSystem(int x, int y, int z) {
            Point3 key = new(x, y, z);
            if (m_particleSystemsByCell.TryGetValue(key, out FireParticleSystem value)) {
                value.IsStopped = true;
                m_particleSystemsByCell.Remove(key);
            }
        }

        public bool AddFuel(int x, int y, int z, int value, int count) {
            if (Terrain.ExtractData(SubsystemTerrain.Terrain.GetCellValue(x, y, z)) > 0) {
                int num = Terrain.ExtractContents(value);
                Block block = BlocksManager.Blocks[num];
                if (Project.FindSubsystem<SubsystemExplosions>(true).TryExplodeBlock(x, y, z, value)) {
                    return true;
                }
                if (block is SnowBlock
                    || block is SnowballBlock
                    || block is IceBlock) {
                    return ResizeCampfire(x, y, z, -1, true);
                }
                if (block.GetFuelHeatLevel(value) > 0f) {
                    float num2 = count * MathUtils.Min(block.GetFuelFireDuration(value), 20f) / 5f;
                    int num3 = (int)num2;
                    float num4 = num2 - num3;
                    if (m_random.Float(0f, 1f) < num4) {
                        num3++;
                    }
                    if (num3 > 0) {
                        return ResizeCampfire(x, y, z, num3, true);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool ResizeCampfire(int x, int y, int z, int steps, bool playSound) {
            int cellValue = SubsystemTerrain.Terrain.GetCellValue(x, y, z);
            int num = Terrain.ExtractData(cellValue);
            if (num > 0) {
                int num2 = Math.Clamp(num + steps, 0, 15);
                if (num2 != num) {
                    int value = Terrain.ReplaceData(cellValue, num2);
                    SubsystemTerrain.ChangeCell(x, y, z, value);
                    if (playSound) {
                        if (steps >= 0) {
                            m_subsystemAmbientSounds.SubsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(x, y, z), 3f, false);
                        }
                        else {
                            m_subsystemAmbientSounds.SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, new Vector3(x, y, z), 3f, true);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
}