using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentSpawn : Component, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public ComponentFrame ComponentFrame { get; set; }

        public ComponentCreature ComponentCreature { get; set; }

        public bool AutoDespawn { get; set; }

        public bool IsDespawning => DespawnTime.HasValue;

        public double SpawnTime { get; set; }

        public double? DespawnTime { get; set; }

        public float SpawnDuration { get; set; }

        public float DespawnDuration { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual Action<ComponentSpawn> Despawned { get; set; }

        public virtual void Despawn() {
            if (!DespawnTime.HasValue) {
                DespawnTime = m_subsystemGameInfo.TotalElapsedGameTime;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            ComponentFrame = Entity.FindComponent<ComponentFrame>(true);
            ComponentCreature = Entity.FindComponent<ComponentCreature>();
            AutoDespawn = valuesDictionary.GetValue<bool>("AutoDespawn");
            double value = valuesDictionary.GetValue<double>("SpawnTime");
            double value2 = valuesDictionary.GetValue<double>("DespawnTime");
            SpawnDuration = 2f;
            DespawnDuration = 2f;
            SpawnTime = value < 0.0 ? m_subsystemGameInfo.TotalElapsedGameTime : value;
            DespawnTime = value2 >= 0.0 ? new double?(value2) : null;
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("SpawnTime", SpawnTime);
            if (DespawnTime.HasValue) {
                valuesDictionary.SetValue("DespawnTime", DespawnTime.Value);
            }
        }

        public virtual void Update(float dt) {
            if (DespawnTime.HasValue
                && m_subsystemGameInfo.TotalElapsedGameTime >= DespawnTime.Value + DespawnDuration) {
                Project.RemoveEntity(Entity, true);
                Despawned?.Invoke(this);
            }
        }
    }
}