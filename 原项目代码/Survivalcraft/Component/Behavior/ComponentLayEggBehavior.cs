using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentLayEggBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemAudio m_subsystemAudio;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public EggBlock.EggType m_eggType;

        public float m_layFrequency;

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel;

        public float m_dt;

        public float m_layTime;

        public Random m_random = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            if (string.IsNullOrEmpty(m_stateMachine.CurrentState)) {
                m_stateMachine.TransitionTo("Move");
            }
            if (m_eggType != null
                && m_random.Float(0f, 1f) < m_layFrequency * dt) {
                m_importanceLevel = m_random.Float(1f, 2f);
            }
            m_dt = dt;
            if (IsActive) {
                m_stateMachine.Update();
            }
            else {
                m_stateMachine.TransitionTo("Inactive");
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            EggBlock eggBlock = (EggBlock)BlocksManager.Blocks[118];
            m_layFrequency = valuesDictionary.GetValue<float>("LayFrequency");
            m_eggType = eggBlock.GetEggTypeByCreatureTemplateName(Entity.ValuesDictionary.DatabaseObject.Name);
            m_stateMachine.AddState(
                "Inactive",
                null,
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Move");
                    }
                },
                null
            );
            m_stateMachine.AddState("Stuck", delegate { m_stateMachine.TransitionTo("Move"); }, null, null);
            m_stateMachine.AddState(
                "Move",
                delegate {
                    Vector3 position2 = m_componentCreature.ComponentBody.Position;
                    float num = 5f;
                    Vector3 value3 = position2 + new Vector3(num * m_random.Float(-1f, 1f), 0f, num * m_random.Float(-1f, 1f));
                    value3.Y = m_subsystemTerrain.Terrain.GetTopHeight(Terrain.ToCell(value3.X), Terrain.ToCell(value3.Z)) + 1;
                    m_componentPathfinding.SetDestination(
                        value3,
                        m_random.Float(0.4f, 0.6f),
                        0.5f,
                        0,
                        false,
                        true,
                        false,
                        null
                    );
                },
                delegate {
                    if (!m_componentPathfinding.Destination.HasValue) {
                        m_stateMachine.TransitionTo("Lay");
                    }
                    else if (m_componentPathfinding.IsStuck) {
                        if (m_random.Float(0f, 1f) < 0.5f) {
                            m_stateMachine.TransitionTo("Stuck");
                        }
                        else {
                            m_importanceLevel = 0f;
                        }
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Lay",
                delegate { m_layTime = 0f; },
                delegate {
                    if (m_eggType != null) {
                        m_layTime += m_dt;
                        if (m_componentCreature.ComponentBody.StandingOnValue.HasValue) {
                            m_componentCreature.ComponentLocomotion.LookOrder = new Vector2(
                                    0f,
                                    0.25f * (float)Math.Sin(20.0 * m_subsystemTime.GameTime) + m_layTime / 3f
                                )
                                - m_componentCreature.ComponentLocomotion.LookAngles;
                            if (m_layTime >= 3f) {
                                m_importanceLevel = 0f;
                                int value = Terrain.MakeBlockValue(118, 0, EggBlock.SetIsLaid(EggBlock.SetEggType(0, m_eggType.EggTypeIndex), true));
                                Matrix matrix = m_componentCreature.ComponentBody.Matrix;
                                Vector3 position = 0.5f
                                    * (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max);
                                Vector3 value2 = 3f
                                    * Vector3.Normalize(-matrix.Forward + 0.1f * matrix.Up + 0.2f * m_random.Float(-1f, 1f) * matrix.Right);
                                m_subsystemPickables.AddPickable(value, 1, position, value2, null, Entity);
                                m_subsystemAudio.PlaySound("Audio/EggLaid", 1f, m_random.Float(-0.1f, 0.1f), position, 2f, true);
                            }
                        }
                        else if (m_layTime >= 3f) {
                            m_importanceLevel = 0f;
                        }
                    }
                    else {
                        m_importanceLevel = 0f;
                    }
                },
                null
            );
        }
    }
}