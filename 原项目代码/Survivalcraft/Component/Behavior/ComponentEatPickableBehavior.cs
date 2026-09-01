using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentEatPickableBehavior : ComponentBehavior, IUpdateable {
        public SubsystemTime m_subsystemTime;

        public SubsystemPickables m_subsystemPickables;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public StateMachine m_stateMachine = new();

        public Dictionary<Pickable, bool> m_pickables = [];

        public Random m_random = new();

        public float[] m_foodFactors;

        public float m_importanceLevel;

        public double m_nextFindPickableTime;

        public double m_nextPickablesUpdateTime;

        public Pickable m_pickable;

        public double m_eatTime;

        public float m_satiation;

        public float m_blockedTime;

        public int m_blockedCount;

        public const float m_range = 16f;

        public float Satiation => m_satiation;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Update(float dt) {
            if (m_satiation > 0f) {
                m_satiation = MathUtils.Max(m_satiation - 0.01f * m_subsystemTime.GameTimeDelta, 0f);
            }
            m_stateMachine.Update();
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_foodFactors = new float[EnumUtils.GetEnumValues<FoodType>().Max() + 1];
            foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("FoodFactors")) {
                FoodType foodType = (FoodType)Enum.Parse(typeof(FoodType), item.Key, false);
                m_foodFactors[(int)foodType] = (float)item.Value;
            }
            m_subsystemPickables.PickableAdded += pickable => {
                if (TryAddPickable(pickable)
                    && m_pickable == null) {
                    m_pickable = pickable;
                }
            };
            m_subsystemPickables.PickableRemoved += pickable => {
                m_pickables.Remove(pickable);
                if (m_pickable == pickable) {
                    m_pickable = null;
                }
            };
            m_stateMachine.AddState(
                "Inactive",
                delegate {
                    m_importanceLevel = 0f;
                    m_pickable = null;
                },
                delegate {
                    if (m_satiation < 1f) {
                        if (m_pickable == null) {
                            if (m_subsystemTime.GameTime > m_nextFindPickableTime) {
                                m_nextFindPickableTime = m_subsystemTime.GameTime + m_random.Float(2f, 4f);
                                m_pickable = FindPickable(m_componentCreature.ComponentBody.Position);
                            }
                        }
                        else {
                            m_importanceLevel = m_random.Float(5f, 10f);
                        }
                    }
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Move");
                        m_blockedCount = 0;
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Move",
                delegate {
                    if (m_pickable != null) {
                        float speed = m_satiation == 0f ? m_random.Float(0.5f, 0.7f) : 0.5f;
                        int maxPathfindingPositions = m_satiation == 0f ? 1000 : 500;
                        float num2 = Vector3.Distance(
                            m_componentCreature.ComponentCreatureModel.EyePosition,
                            m_componentCreature.ComponentBody.Position
                        );
                        m_componentPathfinding.SetDestination(
                            m_pickable.Position,
                            speed,
                            1f + num2,
                            maxPathfindingPositions,
                            true,
                            false,
                            true,
                            null
                        );
                        if (m_random.Float(0f, 1f) < 0.66f) {
                            m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                        }
                    }
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    else if (m_pickable == null) {
                        m_importanceLevel = 0f;
                    }
                    else if (m_componentPathfinding.IsStuck) {
                        m_importanceLevel = 0f;
                        m_satiation += 0.75f;
                    }
                    else if (!m_componentPathfinding.Destination.HasValue) {
                        m_stateMachine.TransitionTo("Eat");
                    }
                    else if (Vector3.DistanceSquared(m_componentPathfinding.Destination.Value, m_pickable.Position) > 0.0625f) {
                        m_stateMachine.TransitionTo("PickableMoved");
                    }
                    if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                    }
                    if (m_pickable != null) {
                        m_componentCreature.ComponentCreatureModel.LookAtOrder = m_pickable.Position;
                    }
                    else {
                        m_componentCreature.ComponentCreatureModel.LookRandomOrder = true;
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "PickableMoved",
                null,
                delegate {
                    if (m_pickable != null) {
                        m_componentCreature.ComponentCreatureModel.LookAtOrder = m_pickable.Position;
                    }
                    if (m_subsystemTime.PeriodicGameTimeEvent(0.25, GetHashCode() % 100 * 0.01)) {
                        m_stateMachine.TransitionTo("Move");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Eat",
                delegate {
                    m_eatTime = m_random.Float(4f, 5f);
                    m_blockedTime = 0f;
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("Inactive");
                    }
                    if (m_pickable == null) {
                        m_importanceLevel = 0f;
                    }
                    if (m_pickable != null) {
                        if (Vector3.DistanceSquared(
                                new Vector3(
                                    m_componentCreature.ComponentCreatureModel.EyePosition.X,
                                    m_componentCreature.ComponentBody.Position.Y,
                                    m_componentCreature.ComponentCreatureModel.EyePosition.Z
                                ),
                                m_pickable.Position
                            )
                            < 0.640000045f) {
                            m_eatTime -= m_subsystemTime.GameTimeDelta;
                            m_blockedTime = 0f;
                            if (m_eatTime <= 0.0) {
                                m_satiation += 1f;
                                m_pickable.Count = MathUtils.Max(m_pickable.Count - 1, 0);
                                if (m_pickable.Count == 0) {
                                    m_pickable.ToRemove = true;
                                    m_importanceLevel = 0f;
                                }
                                else if (m_random.Float(0f, 1f) < 0.5f) {
                                    m_importanceLevel = 0f;
                                }
                                ModsManager.HookAction(
                                    "OnEatPickable",
                                    modLoader => {
                                        modLoader.OnEatPickable(this, m_pickable, out bool Dealed);
                                        return Dealed;
                                    }
                                );
                            }
                        }
                        else {
                            float num = Vector3.Distance(
                                m_componentCreature.ComponentCreatureModel.EyePosition,
                                m_componentCreature.ComponentBody.Position
                            );
                            m_componentPathfinding.SetDestination(
                                m_pickable.Position,
                                0.3f,
                                0.5f + num,
                                0,
                                false,
                                true,
                                false,
                                null
                            );
                            m_blockedTime += m_subsystemTime.GameTimeDelta;
                        }
                        if (m_blockedTime > 3f) {
                            m_blockedCount++;
                            if (m_blockedCount >= 3) {
                                m_importanceLevel = 0f;
                                m_satiation += 0.75f;
                            }
                            else {
                                m_stateMachine.TransitionTo("Move");
                            }
                        }
                    }
                    m_componentCreature.ComponentCreatureModel.FeedOrder = true;
                    if (m_random.Float(0f, 1f) < 0.1f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(true);
                    }
                    if (m_random.Float(0f, 1f) < 1.5f * m_subsystemTime.GameTimeDelta) {
                        m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
                    }
                },
                null
            );
            m_stateMachine.TransitionTo("Inactive");
        }

        public virtual float GetFoodFactor(FoodType foodType) => m_foodFactors[(int)foodType];

        public virtual Pickable FindPickable(Vector3 position) {
            if (m_subsystemTime.GameTime > m_nextPickablesUpdateTime) {
                m_nextPickablesUpdateTime = m_subsystemTime.GameTime + m_random.Float(2f, 4f);
                m_pickables.Clear();
                foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                    TryAddPickable(pickable);
                }
                if (m_pickable != null
                    && !m_pickables.ContainsKey(m_pickable)) {
                    m_pickable = null;
                }
            }
            foreach (Pickable key in m_pickables.Keys) {
                float num = Vector3.DistanceSquared(position, key.Position);
                if (m_random.Float(0f, 1f) > num / 256f) {
                    return key;
                }
            }
            return null;
        }

        public virtual bool TryAddPickable(Pickable pickable) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
            if (m_foodFactors[(int)block.GetFoodType(pickable.Value)] > 0f
                && Vector3.DistanceSquared(pickable.Position, m_componentCreature.ComponentBody.Position) < 256f) {
                m_pickables.Add(pickable, true);
                return true;
            }
            return false;
        }
    }
}