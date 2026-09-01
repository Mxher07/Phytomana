using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentDamage : Component, IUpdateable {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemParticles m_subsystemParticles;

        public ComponentBody m_componentBody;

        public ComponentOnFire m_componentOnFire;

        public float m_lastHitpoints;

        public float m_fallResilience;

        public float m_fireResilience;

        public int m_debrisTextureSlot;

        public float m_debrisStrength;

        public float m_debrisScale;

        public float Hitpoints { get; set; }

        public float HitpointsChange { get; set; }

        public float AttackResilience { get; set; }

        public string DamageSoundName { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public virtual void Damage(float amount) {
            if (amount > 0f
                && Hitpoints > 0f) {
                Hitpoints = MathUtils.Max(Hitpoints - amount, 0f);
            }
        }

        public virtual void Update(float dt) {
            Vector3 position = m_componentBody.Position;
            if (Hitpoints <= 0f) {
                m_subsystemParticles.AddParticleSystem(
                    new BlockDebrisParticleSystem(
                        m_subsystemTerrain,
                        position + m_componentBody.StanceBoxSize.Y / 2f * Vector3.UnitY,
                        m_debrisStrength,
                        m_debrisScale,
                        Color.White,
                        m_debrisTextureSlot
                    )
                );
                m_subsystemAudio.PlayRandomSound(DamageSoundName, 1f, 0f, m_componentBody.Position, 4f, true);
                Project.RemoveEntity(Entity, true);
            }
            if (m_componentBody.CrushedTime > 0f) {
                Damage(1f * dt);
            }
            float num = MathF.Abs(m_componentBody.CollisionVelocityChange.Y);
            if (num > m_fallResilience) {
                float amount = MathUtils.Sqr(MathUtils.Max(num - m_fallResilience, 0f)) / 15f;
                Damage(amount);
            }
            if (position.Y < -10f
                || position.Y > 276f) {
                Damage(Hitpoints);
            }
            if (m_componentOnFire != null
                && (m_componentOnFire.IsOnFire || m_componentOnFire.TouchesFire)) {
                Damage(dt / m_fireResilience);
            }
            HitpointsChange = Hitpoints - m_lastHitpoints;
            m_lastHitpoints = Hitpoints;
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_componentBody = Entity.FindComponent<ComponentBody>(true);
            m_componentOnFire = Entity.FindComponent<ComponentOnFire>();
            Hitpoints = valuesDictionary.GetValue<float>("Hitpoints");
            AttackResilience = valuesDictionary.GetValue<float>("AttackResilience");
            m_fallResilience = valuesDictionary.GetValue<float>("FallResilience");
            m_fireResilience = valuesDictionary.GetValue<float>("FireResilience");
            m_debrisTextureSlot = valuesDictionary.GetValue<int>("DebrisTextureSlot");
            m_debrisStrength = valuesDictionary.GetValue<float>("DebrisStrength");
            m_debrisScale = valuesDictionary.GetValue<float>("DebrisScale");
            DamageSoundName = valuesDictionary.GetValue<string>("DestructionSoundName");
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("Hitpoints", Hitpoints);
        }
    }
}