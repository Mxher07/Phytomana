using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentCetaceanBreatheBehavior : ComponentBehavior, IUpdateable {
        public SubsystemParticles m_subsystemParticles;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemTerrain m_subsystemTerrain;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public Random m_random = new();

        public WhalePlumeParticleSystem m_particleSystem;

        public float m_importanceLevel;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            if (!IsActive) {
                m_stateMachine.TransitionTo("Inactive");
            }
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_stateMachine.AddState(
                "Inactive",
                null,
                delegate {
                    m_importanceLevel = MathUtils.Lerp(0f, 400f, MathUtils.Saturate((0.75f - m_componentCreature.ComponentHealth.Air) / 0.75f));
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Surface");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Surface",
                delegate { m_componentPathfinding.Stop(); },
                delegate {
                    _ = m_componentCreature.ComponentBody.Position;
                    if (!m_componentPathfinding.Destination.HasValue) {
                        Vector3? destination = FindSurfaceDestination();
                        if (destination.HasValue) {
                            float speed = m_componentCreature.ComponentHealth.Air < 0.25f ? 1f : m_random.Float(0.4f, 0.6f);
                            m_componentPathfinding.SetDestination(
                                destination,
                                speed,
                                1f,
                                0,
                                false,
                                false,
                                false,
                                null
                            );
                        }
                    }
                    else if (m_componentPathfinding.IsStuck) {
                        m_importanceLevel = 0f;
                    }
                    if (m_componentCreature.ComponentHealth.Air > 0.9f) {
                        m_stateMachine.TransitionTo("Breathe");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Breathe",
                delegate {
                    Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
                    Vector3 value = m_componentCreature.ComponentBody.Matrix.Translation + 10f * forward + new Vector3(0f, 2f, 0f);
                    m_componentPathfinding.SetDestination(
                        value,
                        0.6f,
                        1f,
                        0,
                        false,
                        false,
                        false,
                        null
                    );
                    m_particleSystem = new WhalePlumeParticleSystem(m_subsystemTerrain, m_random.Float(0.8f, 1.1f), m_random.Float(1f, 1.3f));
                    m_subsystemParticles.AddParticleSystem(m_particleSystem);
                    m_subsystemAudio.PlayRandomSound(
                        "Audio/Creatures/WhaleBlow",
                        1f,
                        m_random.Float(-0.2f, 0.2f),
                        m_componentCreature.ComponentBody.Position,
                        10f,
                        true
                    );
                },
                delegate {
                    m_particleSystem.Position = m_componentCreature.ComponentBody.Position
                        + new Vector3(0f, 0.8f * m_componentCreature.ComponentBody.BoxSize.Y, 0f);
                    if (!m_subsystemParticles.ContainsParticleSystem(m_particleSystem)) {
                        m_importanceLevel = 0f;
                    }
                },
                delegate {
                    m_particleSystem.IsStopped = true;
                    m_particleSystem = null;
                }
            );
        }

        public virtual Vector3? FindSurfaceDestination() {
            Vector3 vector = 0.5f * (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max);
            Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
            float s = 2f * m_componentCreature.ComponentBody.ImmersionDepth;
            for (int i = 0; i < 16; i++) {
                Vector2 vector2 = i < 4 ? new Vector2(forward.X, forward.Z) + m_random.Vector2(0f, 0.25f) : m_random.Vector2(0.5f, 1f);
                Vector3 v = Vector3.Normalize(new Vector3(vector2.X, 1f, vector2.Y));
                Vector3 end = vector + s * v;
                TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(
                    vector,
                    end,
                    false,
                    false,
                    (value, _) => Terrain.ExtractContents(value) != 18
                );
                if (terrainRaycastResult.HasValue
                    && Terrain.ExtractContents(terrainRaycastResult.Value.Value) == 0) {
                    return new Vector3(
                        terrainRaycastResult.Value.CellFace.X + 0.5f,
                        terrainRaycastResult.Value.CellFace.Y,
                        terrainRaycastResult.Value.CellFace.Z + 0.5f
                    );
                }
            }
            return null;
        }
    }
}