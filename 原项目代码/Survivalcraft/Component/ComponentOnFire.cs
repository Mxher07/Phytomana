using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentOnFire : Component, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemAmbientSounds m_subsystemAmbientSounds;

        public SubsystemParticles m_subsystemParticles;

        public Random m_random = new();

        public double m_nextCheckTime;

        public int m_fireTouchCount;

        public OnFireParticleSystem m_onFireParticleSystem;

        public float m_soundVolume;

        public float m_fireDuration;

        public bool m_hideFireParticle = false;

        public ComponentBody ComponentBody { get; set; }

        public bool IsOnFire => m_fireDuration > 0f;

        public bool TouchesFire { get; set; }

        public ComponentCreature Attacker { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void SetOnFire(ComponentCreature attacker, float duration) {
            if (!IsOnFire) {
                Attacker = attacker;
            }
            m_fireDuration = MathUtils.Max(m_fireDuration, duration);
        }

        public virtual void Extinguish() {
            Attacker = null;
            m_fireDuration = 0f;
        }

        public virtual void Update(float dt) {
            if (!IsAddedToProject) {
                return;
            }
            if (IsOnFire) {
                m_fireDuration = MathUtils.Max(m_fireDuration - dt, 0f);
                if (!m_hideFireParticle) {
                    if (m_onFireParticleSystem == null) {
                        m_onFireParticleSystem = new OnFireParticleSystem();
                        m_subsystemParticles.AddParticleSystem(m_onFireParticleSystem);
                    }
                    BoundingBox boundingBox = ComponentBody.BoundingBox;
                    m_onFireParticleSystem.Position = 0.5f * (boundingBox.Min + boundingBox.Max);
                    m_onFireParticleSystem.Radius = 0.5f
                        * MathUtils.Min(boundingBox.Max.X - boundingBox.Min.X, boundingBox.Max.Z - boundingBox.Min.Z);
                    if (Time.PeriodicEvent(0.5, 0.0)) {
                        float distance = m_subsystemAudio.CalculateListenerDistance(ComponentBody.Position);
                        m_soundVolume = m_subsystemAudio.CalculateVolume(distance, 2f, 5f);
                    }
                    m_subsystemAmbientSounds.FireSoundVolume = MathUtils.Max(m_subsystemAmbientSounds.FireSoundVolume, m_soundVolume);
                }
                if (ComponentBody.ImmersionFactor > 0.5f
                    && ComponentBody.ImmersionFluidBlock is WaterBlock) {
                    Extinguish();
                    m_subsystemAudio.PlaySound("Audio/SizzleLong", 1f, 0f, m_onFireParticleSystem.Position, 4f, true);
                }
            }
            else {
                if (m_onFireParticleSystem != null) {
                    m_onFireParticleSystem.IsStopped = true;
                    m_onFireParticleSystem = null;
                }
                m_soundVolume = 0f;
            }
            if (!(m_subsystemTime.GameTime > m_nextCheckTime)) {
                return;
            }
            m_nextCheckTime = m_subsystemTime.GameTime + m_random.Float(0.9f, 1.1f);
            TouchesFire = CheckIfBodyTouchesFire();
            if (TouchesFire) {
                m_fireTouchCount++;
                if (m_fireTouchCount >= 5) {
                    SetOnFire(null, m_random.Float(12f, 15f));
                }
            }
            else {
                m_fireTouchCount = 0;
            }
            if (ComponentBody.ImmersionFactor > 0.2f
                && ComponentBody.ImmersionFluidBlock is MagmaBlock) {
                SetOnFire(null, m_random.Float(12f, 15f));
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemAmbientSounds = Project.FindSubsystem<SubsystemAmbientSounds>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            ComponentBody = Entity.FindComponent<ComponentBody>();
            float value = valuesDictionary.GetValue<float>("FireDuration");
            if (value > 0f) {
                SetOnFire(null, value);
            }
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("FireDuration", m_fireDuration);
        }

        public override void OnEntityRemoved() {
            if (m_onFireParticleSystem != null) {
                m_onFireParticleSystem.IsStopped = true;
            }
        }

        public virtual bool CheckIfBodyTouchesFire() {
            BoundingBox boundingBox = ComponentBody.BoundingBox;
            boundingBox.Min -= new Vector3(0.25f);
            boundingBox.Max += new Vector3(0.25f);
            int num = Terrain.ToCell(boundingBox.Min.X);
            int num2 = Terrain.ToCell(boundingBox.Min.Y);
            int num3 = Terrain.ToCell(boundingBox.Min.Z);
            int num4 = Terrain.ToCell(boundingBox.Max.X);
            int num5 = Terrain.ToCell(boundingBox.Max.Y);
            int num6 = Terrain.ToCell(boundingBox.Max.Z);
            for (int i = num; i <= num4; i++) {
                for (int j = num2; j <= num5; j++) {
                    for (int k = num3; k <= num6; k++) {
                        int cellValue = m_subsystemTerrain.Terrain.GetCellValue(i, j, k);
                        int num7 = Terrain.ExtractContents(cellValue);
                        int num8 = Terrain.ExtractData(cellValue);
                        switch (num7) {
                            case 104:
                                if (num8 == 0) {
                                    BoundingBox box2 = new(new Vector3(i, j, k), new Vector3(i + 1, j + 1, k + 1));
                                    if (boundingBox.Intersection(box2)) {
                                        return true;
                                    }
                                    break;
                                }
                                if ((num8 & 1) != 0) {
                                    BoundingBox box3 = new(new Vector3(i, j, k + 0.5f), new Vector3(i + 1, j + 1, k + 1));
                                    if (boundingBox.Intersection(box3)) {
                                        return true;
                                    }
                                }
                                if ((num8 & 2) != 0) {
                                    BoundingBox box4 = new(new Vector3(i + 0.5f, j, k), new Vector3(i + 1, j + 1, k + 1));
                                    if (boundingBox.Intersection(box4)) {
                                        return true;
                                    }
                                }
                                if ((num8 & 4) != 0) {
                                    BoundingBox box5 = new(new Vector3(i, j, k), new Vector3(i + 1, j + 1, k + 0.5f));
                                    if (boundingBox.Intersection(box5)) {
                                        return true;
                                    }
                                }
                                if ((num8 & 8) != 0) {
                                    BoundingBox box6 = new(new Vector3(i, j, k), new Vector3(i + 0.5f, j + 1, k + 1));
                                    if (boundingBox.Intersection(box6)) {
                                        return true;
                                    }
                                }
                                break;
                            case 209:
                                if (num8 > 0) {
                                    BoundingBox box = new(
                                        new Vector3(i, j, k) + new Vector3(0.2f),
                                        new Vector3(i + 1, j + 1, k + 1) - new Vector3(0.2f)
                                    );
                                    if (boundingBox.Intersection(box)) {
                                        return true;
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            return false;
        }
    }
}