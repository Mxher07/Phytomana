using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFindPlayerBehavior : ComponentBehavior, IUpdateable {
        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemTime m_subsystemTime;

        public SubsystemSky m_subsystemSky;

        public SubsystemBodies m_subsystemBodies;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public DynamicArray<ComponentBody> m_componentBodies = [];

        public Random m_random = new();

        public float m_importanceLevel;

        public float m_dayRange;

        public float m_nightRange;

        public float m_minRange;

        public float m_dt;

        public ComponentCreature m_target;

        public double m_nextUpdateTime;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            if (m_subsystemTime.GameTime >= m_nextUpdateTime) {
                m_dt = m_random.Float(1.25f, 1.75f) + MathUtils.Min((float)(m_subsystemTime.GameTime - m_nextUpdateTime), 0.1f);
                m_nextUpdateTime = m_subsystemTime.GameTime + m_dt;
                m_stateMachine.Update();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_dayRange = valuesDictionary.GetValue<float>("DayRange");
            m_nightRange = valuesDictionary.GetValue<float>("NightRange");
            m_minRange = valuesDictionary.GetValue<float>("MinRange");
            m_stateMachine.AddState(
                "Inactive",
                delegate {
                    m_importanceLevel = 0f;
                    m_target = null;
                },
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Move");
                    }
                    if (m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless) {
                        m_target = FindTarget();
                        if (m_target != null) {
                            ComponentPlayer componentPlayer = m_target.Entity.FindComponent<ComponentPlayer>();
                            if (componentPlayer != null
                                && componentPlayer.ComponentSleep.IsSleeping) {
                                m_importanceLevel = 5f;
                            }
                            else if (m_random.Float(0f, 1f) < 0.05f * m_dt) {
                                m_importanceLevel = m_random.Float(1f, 4f);
                            }
                        }
                        else {
                            m_importanceLevel = 0f;
                        }
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Move",
                delegate {
                    if (m_target != null) {
                        m_componentPathfinding.SetDestination(
                            m_target.ComponentBody.Position,
                            m_random.Float(0.5f, 0.7f),
                            m_minRange,
                            500,
                            true,
                            true,
                            false,
                            null
                        );
                    }
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_target == null
                        || m_componentPathfinding.IsStuck
                        || !m_componentPathfinding.Destination.HasValue
                        || ScoreTarget(m_target) <= 0f) {
                        m_importanceLevel = 0f;
                    }
                    if (m_random.Float(0f, 1f) < 0.1f * m_dt) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                    }
                    m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual ComponentCreature FindTarget() {
            Vector3 position = m_componentCreature.ComponentBody.Position;
            ComponentCreature result = null;
            float num = 0f;
            m_componentBodies.Clear();
            m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), MathUtils.Max(m_nightRange, m_dayRange), m_componentBodies);
            for (int i = 0; i < m_componentBodies.Count; i++) {
                ComponentCreature componentCreature = m_componentBodies.Array[i].Entity.FindComponent<ComponentCreature>();
                if (componentCreature != null) {
                    float num2 = ScoreTarget(componentCreature);
                    if (num2 > num) {
                        num = num2;
                        result = componentCreature;
                    }
                }
            }
            return result;
        }

        public virtual float ScoreTarget(ComponentCreature target) {
            float num = m_subsystemSky.SkyLightIntensity > 0.2f ? m_dayRange : m_nightRange;
            if (!target.IsAddedToProject
                || target.ComponentHealth.Health <= 0f
                || target.Entity.FindComponent<ComponentPlayer>() == null) {
                return 0f;
            }
            float num2 = Vector3.DistanceSquared(target.ComponentBody.Position, m_componentCreature.ComponentBody.Position);
            if (num2 < m_minRange * m_minRange) {
                return 0f;
            }
            return num * num - num2;
        }
    }
}