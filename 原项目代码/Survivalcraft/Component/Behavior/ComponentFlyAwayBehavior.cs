using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentFlyAwayBehavior : ComponentBehavior, IUpdateable, INoiseListener, IComponentEscapeBehavior {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemTime m_subsystemTime;

        public SubsystemNoise m_subsystemNoise;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public DynamicArray<ComponentBody> m_componentBodies = [];

        public Random m_random = new();

        public StateMachine m_stateMachine = new();

        public float m_importanceLevel;

        public double m_nextUpdateTime;
        public float LowHealthToEscape { get; set; }

        /// <summary>
        ///     ‹‘Î…˘”∞œÏ
        /// </summary>
        public bool AffectedByNoise;

        /// <summary>
        ///     ≥·∞Ú…»∂Ø…˘“Ù
        /// </summary>
        public bool FanSound;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public override bool IsActive {
            set {
                base.IsActive = value;
                if (IsActive) {
                    m_nextUpdateTime = 0.0;
                }
            }
        }

        public virtual void Update(float dt) {
            if (m_componentCreature.ComponentHealth.HealthChange < 0f) {
                m_stateMachine.TransitionTo("DangerDetected");
            }
            if (m_subsystemTime.GameTime >= m_nextUpdateTime) {
                m_nextUpdateTime = m_subsystemTime.GameTime + m_random.Float(0.5f, 1f);
                m_stateMachine.Update();
            }
        }

        public virtual void HearNoise(ComponentBody sourceBody, Vector3 sourcePosition, float loudness) {
            if (loudness >= 0.25f
                && m_stateMachine.CurrentState != "RunningAway"
                && AffectedByNoise) {
                m_stateMachine.TransitionTo("DangerDetected");
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            LowHealthToEscape = valuesDictionary.GetValue("LowHealthToEscape", 0.33f);
            AffectedByNoise = valuesDictionary.GetValue("AffectedByNoise", true);
            FanSound = valuesDictionary.GetValue("FanSound", true);
            m_componentCreature.ComponentBody.CollidedWithBody += delegate {
                if (m_stateMachine.CurrentState != "RunningAway") {
                    m_stateMachine.TransitionTo("DangerDetected");
                }
            };
            m_stateMachine.AddState(
                "LookingForDanger",
                null,
                delegate {
                    if (ScanForDanger()) {
                        m_stateMachine.TransitionTo("DangerDetected");
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "DangerDetected",
                delegate {
                    m_importanceLevel = m_componentCreature.ComponentHealth.Health < LowHealthToEscape ? 300 : 100;
                    m_nextUpdateTime = 0.0;
                },
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("RunningAway");
                        m_nextUpdateTime = 0.0;
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "RunningAway",
                delegate {
                    m_componentPathfinding.SetDestination(
                        FindSafePlace(),
                        1f,
                        1f,
                        0,
                        false,
                        true,
                        false,
                        null
                    );
                    if (FanSound) {
                        m_subsystemAudio.PlayRandomSound(
                            "Audio/Creatures/Wings",
                            0.8f,
                            m_random.Float(-0.1f, 0.2f),
                            m_componentCreature.ComponentBody.Position,
                            3f,
                            true
                        );
                    }
                    m_componentCreature.ComponentCreatureSounds.PlayPainSound();
                    m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 6f);
                },
                delegate {
                    if (!IsActive
                        || !m_componentPathfinding.Destination.HasValue
                        || m_componentPathfinding.IsStuck) {
                        m_stateMachine.TransitionTo("LookingForDanger");
                    }
                    else if (ScoreSafePlace(m_componentCreature.ComponentBody.Position, m_componentPathfinding.Destination.Value, null) < 4f) {
                        m_componentPathfinding.SetDestination(
                            FindSafePlace(),
                            1f,
                            0.5f,
                            0,
                            false,
                            true,
                            false,
                            null
                        );
                    }
                },
                delegate { m_importanceLevel = 0f; }
            );
            m_stateMachine.TransitionTo("LookingForDanger");
        }

        public virtual bool ScanForDanger() {
            Matrix matrix = m_componentCreature.ComponentBody.Matrix;
            Vector3 translation = matrix.Translation;
            Vector3 forward = matrix.Forward;
            if (ScoreSafePlace(translation, translation, forward) < 7f) {
                return true;
            }
            return false;
        }

        public virtual Vector3 FindSafePlace() {
            Vector3 position = m_componentCreature.ComponentBody.Position;
            float num = float.NegativeInfinity;
            Vector3 result = position;
            for (int i = 0; i < 20; i++) {
                int num2 = Terrain.ToCell(position.X + m_random.Float(-20f, 20f));
                int num3 = Terrain.ToCell(position.Z + m_random.Float(-20f, 20f));
                for (int num4 = 255; num4 >= 0; num4--) {
                    int cellValue = m_subsystemTerrain.Terrain.GetCellValue(num2, num4, num3);
                    if (BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].GetIsCollidable(m_componentCreature.ComponentBody, cellValue)
                        || Terrain.ExtractContents(cellValue) == 18) {
                        Vector3 vector = new(num2 + 0.5f, num4 + 1.1f, num3 + 0.5f);
                        float num5 = ScoreSafePlace(position, vector, null);
                        if (num5 > num) {
                            num = num5;
                            result = vector;
                        }
                        break;
                    }
                }
            }
            return result;
        }

        public virtual float ScoreSafePlace(Vector3 currentPosition, Vector3 safePosition, Vector3? lookDirection) {
            float num = 16f;
            Vector3 position = m_componentCreature.ComponentBody.Position;
            m_componentBodies.Clear();
            m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), 16f, m_componentBodies);
            for (int i = 0; i < m_componentBodies.Count; i++) {
                ComponentBody componentBody = m_componentBodies.Array[i];
                if (!IsPredator(componentBody.Entity)) {
                    continue;
                }
                Vector3 position2 = componentBody.Position;
                Vector3 v = safePosition - position2;
                if (!lookDirection.HasValue
                    || 0f - Vector3.Dot(lookDirection.Value, v) > 0f) {
                    if (v.Y >= 4f) {
                        v *= 2f;
                    }
                    num = MathUtils.Min(num, v.Length());
                }
            }
            float num2 = Vector3.Distance(currentPosition, safePosition);
            if (num2 < 8f) {
                return num * 0.5f;
            }
            return num * MathUtils.Lerp(1f, 0.75f, MathUtils.Saturate(num2 / 20f));
        }

        public virtual bool IsPredator(Entity entity) {
            if (entity != Entity) {
                bool isPredator = false;
                bool skipVanilla = false;
                ModsManager.HookAction(
                    "IsPredator",
                    modLoader => {
                        modLoader.IsPredator(this, entity, out isPredator, out skipVanilla);
                        return false;
                    }
                );
                if (skipVanilla) {
                    return isPredator;
                }
                ComponentCreature componentCreature = entity.FindComponent<ComponentCreature>();
                if (componentCreature != null
                    && (componentCreature.Category == CreatureCategory.LandPredator
                        || componentCreature.Category == CreatureCategory.WaterPredator
                        || componentCreature.Category == CreatureCategory.LandOther)) {
                    return true;
                }
            }
            return false;
        }
    }
}