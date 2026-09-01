using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentChaseBehavior : ComponentBehavior, IUpdateable {
        // ReSharper disable UnusedMember.Local
        Dictionary<ModLoader, Action> Hooks = [];
        // ReSharper restore UnusedMember.Local

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemSky m_subsystemSky;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemTime m_subsystemTime;

        public SubsystemNoise m_subsystemNoise;

        public ComponentCreature m_componentCreature;

        public ComponentPathfinding m_componentPathfinding;

        public ComponentMiner m_componentMiner;

        public ComponentRandomFeedBehavior m_componentFeedBehavior;

        public ComponentCreatureModel m_componentCreatureModel;

        public DynamicArray<ComponentBody> m_componentBodies = [];

        public Random m_random = new();

        public StateMachine m_stateMachine = new();

        public ComponentFactors m_componentFactors;

        public float m_dayChaseRange;

        public float m_nightChaseRange;

        public float m_dayChaseTime;

        public float m_nightChaseTime;

        public float m_chaseNonPlayerProbability;

        public float m_chaseWhenAttackedProbability;

        public float m_chaseOnTouchProbability;

        public CreatureCategory m_autoChaseMask;

        public float m_importanceLevel;

        public float m_targetUnsuitableTime;

        public float m_targetInRangeTime;

        public double m_nextUpdateTime;

        public ComponentCreature m_target;

        public float m_dt;

        public float m_range;

        public float m_chaseTime;

        public bool m_isPersistent;

        public float m_autoChaseSuppressionTime;

        public float ImportanceLevelNonPersistent = 200f;

        public float ImportanceLevelPersistent = 200f;

        public float MaxAttackRange = 1.75f;

        public bool AllowAttackingStandingOnBody = true;

        public bool JumpWhenTargetStanding = true;

        public bool AttacksPlayer = true;

        public bool AttacksNonPlayerCreature = true;

        public float ChaseRangeOnTouch = 7f;

        public float ChaseTimeOnTouch = 7f;

        public float? ChaseRangeOnAttacked = null;

        public float? ChaseTimeOnAttacked = null;

        public bool? ChasePersistentOnAttacked = null;

        public float MinHealthToAttackActively = 0.4f;

        public bool Suppressed = false;

        public bool PlayIdleSoundWhenStartToChase = true;

        public bool PlayAngrySoundWhenChasing = true;
        public float TargetInRangeTimeToChase = 3f;

        public ComponentCreature Target => m_target;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public override float ImportanceLevel => m_importanceLevel;

        public virtual void Attack(ComponentCreature componentCreature, float maxRange, float maxChaseTime, bool isPersistent) {
            if (Suppressed) {
                return;
            }
            m_target = componentCreature;
            m_nextUpdateTime = 0.0;
            m_range = maxRange;
            m_chaseTime = maxChaseTime;
            m_isPersistent = isPersistent;
            m_importanceLevel = isPersistent ? ImportanceLevelPersistent : ImportanceLevelNonPersistent;
            ModsManager.HookAction(
                "OnChaseBehaviorStartChasing",
                loader => {
                    loader.OnChaseBehaviorStartChasing(this);
                    return false;
                }
            );
        }

        public virtual void StopAttack() {
            m_stateMachine.TransitionTo("LookingForTarget");
            IsActive = false;
            m_target = null;
            m_nextUpdateTime = 0.0;
            m_range = 0;
            m_chaseTime = 0;
            m_isPersistent = false;
            m_importanceLevel = 0;
            ModsManager.HookAction(
                "OnChaseBehaviorStopChasing",
                loader => {
                    loader.OnChaseBehaviorStopChasing(this);
                    return false;
                }
            );
        }

        public virtual void Update(float dt) {
            if (Suppressed) {
                StopAttack();
            }
            m_autoChaseSuppressionTime -= dt;
            if (IsActive && m_target != null) {
                m_chaseTime -= dt;
                m_componentCreature.ComponentCreatureModel.LookAtOrder = m_target.ComponentCreatureModel.EyePosition;
                if (IsTargetInAttackRange(m_target.ComponentBody)) {
                    m_componentCreatureModel.AttackOrder = true;
                }
                if (m_componentCreatureModel.IsAttackHitMoment) {
                    ComponentBody hitBody = GetHitBody(m_target.ComponentBody, out Vector3 hitPoint);
                    if (hitBody != null) {
                        float chaseTimeBefore = m_chaseTime;
                        float x = m_isPersistent ? m_random.Float(8f, 10f) : 2f;
                        m_chaseTime = MathUtils.Max(m_chaseTime, x);
                        bool bodyToHit = true;
                        bool playAttackSound = true;
                        ModsManager.HookAction(
                            "OnChaseBehaviorAttacked",
                            loader => {
                                loader.OnChaseBehaviorAttacked(this, chaseTimeBefore, ref m_chaseTime, ref bodyToHit, ref playAttackSound);
                                return false;
                            }
                        );
                        if (bodyToHit) {
                            m_componentMiner.Hit(hitBody, hitPoint, m_componentCreature.ComponentBody.Matrix.Forward);
                        }
                        if (playAttackSound) {
                            m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
                        }
                    }
                    else {
                        ModsManager.HookAction(
                            "OnChaseBehaviorAttackFailed",
                            loader => {
                                loader.OnChaseBehaviorAttackFailed(this, ref m_chaseTime);
                                return false;
                            }
                        );
                    }
                }
            }
            if (m_subsystemTime.GameTime >= m_nextUpdateTime) {
                m_dt = m_random.Float(0.25f, 0.35f) + MathUtils.Min((float)(m_subsystemTime.GameTime - m_nextUpdateTime), 0.1f);
                m_nextUpdateTime = m_subsystemTime.GameTime + m_dt;
                m_stateMachine.Update();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPathfinding = Entity.FindComponent<ComponentPathfinding>(true);
            m_componentMiner = Entity.FindComponent<ComponentMiner>(true);
            m_componentFeedBehavior = Entity.FindComponent<ComponentRandomFeedBehavior>();
            m_componentCreatureModel = Entity.FindComponent<ComponentCreatureModel>(true);
            m_componentFactors = Entity.FindComponent<ComponentFactors>(true);
            m_dayChaseRange = valuesDictionary.GetValue<float>("DayChaseRange");
            m_nightChaseRange = valuesDictionary.GetValue<float>("NightChaseRange");
            m_dayChaseTime = valuesDictionary.GetValue<float>("DayChaseTime");
            m_nightChaseTime = valuesDictionary.GetValue<float>("NightChaseTime");
            m_autoChaseMask = valuesDictionary.GetValue<CreatureCategory>("AutoChaseMask");
            m_chaseNonPlayerProbability = valuesDictionary.GetValue<float>("ChaseNonPlayerProbability");
            m_chaseWhenAttackedProbability = valuesDictionary.GetValue<float>("ChaseWhenAttackedProbability");
            m_chaseOnTouchProbability = valuesDictionary.GetValue<float>("ChaseOnTouchProbability");
            m_componentCreature.ComponentBody.CollidedWithBody += delegate(ComponentBody body) {
                if (m_target == null
                    && m_autoChaseSuppressionTime <= 0f
                    && m_random.Float(0f, 1f) < m_chaseOnTouchProbability) {
                    ComponentCreature componentCreature2 = body.Entity.FindComponent<ComponentCreature>();
                    if (componentCreature2 != null) {
                        bool flag2 = m_subsystemPlayers.IsPlayer(body.Entity);
                        bool flag3 = (componentCreature2.Category & m_autoChaseMask) != 0;
                        if ((AttacksPlayer && flag2 && m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless)
                            || (AttacksNonPlayerCreature && !flag2 && flag3)) {
                            Attack(componentCreature2, ChaseRangeOnTouch, ChaseTimeOnTouch, false);
                        }
                    }
                }
                if (m_target != null
                    && JumpWhenTargetStanding
                    && body == m_target.ComponentBody
                    && body.StandingOnBody == m_componentCreature.ComponentBody) {
                    m_componentCreature.ComponentLocomotion.JumpOrder = 1f;
                }
            };
            m_componentCreature.ComponentHealth.Injured += delegate(Injury injury) {
                ComponentCreature attacker = injury.Attacker;
                if (m_random.Float(0f, 1f) < m_chaseWhenAttackedProbability) {
                    float chaseRange;
                    float chaseTime;
                    bool chasePersistent = false;
                    if (m_chaseWhenAttackedProbability >= 1f) {
                        chaseRange = 30f;
                        chaseTime = 60f;
                        chasePersistent = true;
                    }
                    else {
                        chaseRange = 7f;
                        chaseTime = 7f;
                        //chasePersistent = false;
                    }
                    chaseRange = ChaseRangeOnAttacked ?? chaseRange;
                    chaseTime = ChaseTimeOnAttacked ?? chaseTime;
                    chasePersistent = ChasePersistentOnAttacked ?? chasePersistent;
                    Attack(attacker, chaseRange, chaseTime, chasePersistent);
                }
            };
            m_stateMachine.AddState(
                "LookingForTarget",
                delegate {
                    m_importanceLevel = 0f;
                    m_target = null;
                },
                delegate {
                    if (IsActive) {
                        m_stateMachine.TransitionTo("Chasing");
                    }
                    else {
                        if (!Suppressed
                            && m_autoChaseSuppressionTime <= 0f
                            && (m_target == null || ScoreTarget(m_target) <= 0f)
                            && m_componentCreature.ComponentHealth.Health > MinHealthToAttackActively) {
                            m_range = m_subsystemSky.SkyLightIntensity < 0.2f ? m_nightChaseRange : m_dayChaseRange;
                            m_range *= m_componentFactors.GetOtherFactorResult("ChaseRange");
                            ComponentCreature componentCreature = FindTarget();
                            if (componentCreature != null) {
                                m_targetInRangeTime += m_dt;
                            }
                            else {
                                m_targetInRangeTime = 0f;
                            }
                            if (m_targetInRangeTime > TargetInRangeTimeToChase) {
                                bool flag = m_subsystemSky.SkyLightIntensity >= 0.1f;
                                float maxRange = flag ? m_dayChaseRange + 6f : m_nightChaseRange + 6f;
                                float maxChaseTime = flag ? m_dayChaseTime * m_random.Float(0.75f, 1f) : m_nightChaseTime * m_random.Float(0.75f, 1f);
                                Attack(componentCreature, maxRange, maxChaseTime, !flag);
                            }
                        }
                        ModsManager.HookAction(
                            "UpdateChaseBehaviorLookingForTarget",
                            loader => {
                                loader.UpdateChaseBehaviorLookingForTarget(this);
                                return false;
                            }
                        );
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "RandomMoving",
                delegate {
                    m_componentPathfinding.SetDestination(
                        m_componentCreature.ComponentBody.Position + new Vector3(6f * m_random.Float(-1f, 1f), 0f, 6f * m_random.Float(-1f, 1f)),
                        1f,
                        1f,
                        0,
                        false,
                        true,
                        false,
                        null
                    );
                },
                delegate {
                    if (m_componentPathfinding.IsStuck
                        || !m_componentPathfinding.Destination.HasValue) {
                        m_stateMachine.TransitionTo("Chasing");
                    }
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("LookingForTarget");
                    }
                },
                delegate { m_componentPathfinding.Stop(); }
            );
            m_stateMachine.AddState(
                "Chasing",
                delegate {
                    m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 6f);
                    if (PlayIdleSoundWhenStartToChase) {
                        m_componentCreature.ComponentCreatureSounds.PlayIdleSound(false);
                    }
                    m_nextUpdateTime = 0.0;
                },
                delegate {
                    if (!IsActive) {
                        m_stateMachine.TransitionTo("LookingForTarget");
                    }
                    else if (m_chaseTime <= 0f) {
                        m_autoChaseSuppressionTime = m_random.Float(10f, 60f);
                        m_importanceLevel = 0f;
                    }
                    else if (m_target == null) {
                        m_importanceLevel = 0f;
                    }
                    else if (m_target.ComponentHealth.Health <= 0f) {
                        if (m_componentFeedBehavior != null) {
                            m_subsystemTime.QueueGameTimeDelayedExecution(
                                m_subsystemTime.GameTime + m_random.Float(1f, 3f),
                                delegate {
                                    if (m_target != null) {
                                        m_componentFeedBehavior.Feed(m_target.ComponentBody.Position);
                                    }
                                }
                            );
                        }
                        m_importanceLevel = 0f;
                    }
                    else if (!m_isPersistent
                        && m_componentPathfinding.IsStuck) {
                        m_importanceLevel = 0f;
                    }
                    else if (m_isPersistent && m_componentPathfinding.IsStuck) {
                        m_stateMachine.TransitionTo("RandomMoving");
                    }
                    else {
                        if (ScoreTarget(m_target) <= 0f) {
                            m_targetUnsuitableTime += m_dt;
                        }
                        else {
                            m_targetUnsuitableTime = 0f;
                        }
                        if (m_targetUnsuitableTime > 3f) {
                            m_importanceLevel = 0f;
                        }
                        else {
                            int maxPathfindingPositions = 0;
                            if (m_isPersistent) {
                                maxPathfindingPositions = m_subsystemTime.FixedTimeStep.HasValue ? 2000 : 500;
                            }
                            BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
                            BoundingBox boundingBox2 = m_target.ComponentBody.BoundingBox;
                            Vector3 v = 0.5f * (boundingBox.Min + boundingBox.Max);
                            Vector3 vector = 0.5f * (boundingBox2.Min + boundingBox2.Max);
                            float num = Vector3.Distance(v, vector);
                            float num2 = num < 4f ? 0.2f : 0f;
                            m_componentPathfinding.SetDestination(
                                vector + num2 * num * m_target.ComponentBody.Velocity,
                                1f,
                                1.5f,
                                maxPathfindingPositions,
                                true,
                                false,
                                true,
                                m_target.ComponentBody
                            );
                            if (PlayAngrySoundWhenChasing && m_random.Float(0f, 1f) < 0.33f * m_dt) {
                                m_componentCreature.ComponentCreatureSounds.PlayAttackSound();
                            }
                        }
                    }
                    ModsManager.HookAction(
                        "UpdateChaseBehaviorChasing",
                        loader => {
                            loader.UpdateChaseBehaviorChasing(this);
                            return false;
                        }
                    );
                },
                null
            );
            m_stateMachine.TransitionTo("LookingForTarget");
        }

        public virtual ComponentCreature FindTarget() {
            Vector3 position = m_componentCreature.ComponentBody.Position;
            ComponentCreature result = null;
            float num = 0f;
            m_componentBodies.Clear();
            m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), m_range, m_componentBodies);
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

        public virtual float ScoreTarget(ComponentCreature componentCreature) {
            float score = 0f;
            bool isPlayer = componentCreature.Entity.FindComponent<ComponentPlayer>() != null;
            bool isLandCreature = m_componentCreature.Category != CreatureCategory.WaterPredator
                && m_componentCreature.Category != CreatureCategory.WaterOther;
            bool notHarmless = componentCreature == Target || m_subsystemGameInfo.WorldSettings.GameMode > GameMode.Harmless;
            bool isAutoChaseMask = (componentCreature.Category & m_autoChaseMask) != 0;
            bool allowToChaseNonCreature = componentCreature == Target
                || (isAutoChaseMask
                    && MathUtils.Remainder(
                        0.004999999888241291 * m_subsystemTime.GameTime
                        + GetHashCode() % 1000 / 1000f
                        + componentCreature.GetHashCode() % 1000 / 1000f,
                        1.0
                    )
                    < m_chaseNonPlayerProbability);
            if (componentCreature != m_componentCreature
                && ((!isPlayer && allowToChaseNonCreature) || (isPlayer && notHarmless))
                && componentCreature.Entity.IsAddedToProject
                && componentCreature.ComponentHealth.Health > 0f
                && (isLandCreature || IsTargetInWater(componentCreature.ComponentBody))) {
                float num = Vector3.Distance(m_componentCreature.ComponentBody.Position, componentCreature.ComponentBody.Position);
                if (num < m_range) {
                    score = m_range - num;
                }
            }
            ModsManager.HookAction(
                "ChaseBehaviorScoreTarget",
                loader => {
                    loader.ChaseBehaviorScoreTarget(this, componentCreature, ref score);
                    return false;
                }
            );
            return score;
        }

        public virtual bool IsTargetInWater(ComponentBody target) {
            if (target.ImmersionDepth > 0f) {
                return true;
            }
            if (target.ParentBody != null
                && IsTargetInWater(target.ParentBody)) {
                return true;
            }
            if (target.StandingOnBody != null
                && target.StandingOnBody.Position.Y < target.Position.Y
                && IsTargetInWater(target.StandingOnBody)) {
                return true;
            }
            return false;
        }

        public virtual bool IsTargetInAttackRange(ComponentBody target) {
            if (IsBodyInAttackRange(target)) {
                return true;
            }
            BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
            BoundingBox boundingBox2 = target.BoundingBox;
            Vector3 v = 0.5f * (boundingBox.Min + boundingBox.Max);
            Vector3 v2 = 0.5f * (boundingBox2.Min + boundingBox2.Max) - v;
            float num = v2.Length();
            Vector3 v3 = v2 / num;
            float num2 = 0.5f * (boundingBox.Max.X - boundingBox.Min.X + boundingBox2.Max.X - boundingBox2.Min.X);
            float num3 = 0.5f * (boundingBox.Max.Y - boundingBox.Min.Y + boundingBox2.Max.Y - boundingBox2.Min.Y);
            if (MathF.Abs(v2.Y) < num3 * 0.99f) {
                if (num < num2 + 0.99f
                    && Vector3.Dot(v3, m_componentCreature.ComponentBody.Matrix.Forward) > 0.25f) {
                    return true;
                }
            }
            else if (num < num3 + 0.3f
                && MathF.Abs(Vector3.Dot(v3, Vector3.UnitY)) > 0.8f) {
                return true;
            }
            if (target.ParentBody != null
                && IsTargetInAttackRange(target.ParentBody)) {
                return true;
            }
            if (AllowAttackingStandingOnBody
                && target.StandingOnBody != null
                && target.StandingOnBody.Position.Y < target.Position.Y
                && IsTargetInAttackRange(target.StandingOnBody)) {
                return true;
            }
            return false;
        }

        public virtual bool IsBodyInAttackRange(ComponentBody target) {
            BoundingBox boundingBox = m_componentCreature.ComponentBody.BoundingBox;
            BoundingBox boundingBox2 = target.BoundingBox;
            Vector3 v = 0.5f * (boundingBox.Min + boundingBox.Max);
            Vector3 v2 = 0.5f * (boundingBox2.Min + boundingBox2.Max) - v;
            float num = v2.Length();
            Vector3 v3 = v2 / num;
            float num2 = 0.5f * (boundingBox.Max.X - boundingBox.Min.X + boundingBox2.Max.X - boundingBox2.Min.X);
            float num3 = 0.5f * (boundingBox.Max.Y - boundingBox.Min.Y + boundingBox2.Max.Y - boundingBox2.Min.Y);
            if (MathF.Abs(v2.Y) < num3 * 0.99f) {
                if (num < num2 + 0.99f
                    && Vector3.Dot(v3, m_componentCreature.ComponentBody.Matrix.Forward) > 0.25f) {
                    return true;
                }
            }
            else if (num < num3 + 0.3f
                && MathF.Abs(Vector3.Dot(v3, Vector3.UnitY)) > 0.8f) {
                return true;
            }
            return false;
        }

        public virtual ComponentBody GetHitBody(ComponentBody target, out Vector3 hitPoint) {
            Vector3 vector = m_componentCreature.ComponentBody.BoundingBox.Center();
            Vector3 v = target.BoundingBox.Center();
            Ray3 ray = new(vector, Vector3.Normalize(v - vector));
            BodyRaycastResult? bodyRaycastResult = m_componentMiner.Raycast<BodyRaycastResult>(ray, RaycastMode.Interaction);
            if (bodyRaycastResult.HasValue
                && bodyRaycastResult.Value.Distance < MaxAttackRange
                && (bodyRaycastResult.Value.ComponentBody == target
                    || bodyRaycastResult.Value.ComponentBody.IsChildOfBody(target)
                    || target.IsChildOfBody(bodyRaycastResult.Value.ComponentBody)
                    || (target.StandingOnBody == bodyRaycastResult.Value.ComponentBody && AllowAttackingStandingOnBody))) {
                hitPoint = bodyRaycastResult.Value.HitPoint();
                return bodyRaycastResult.Value.ComponentBody;
            }
            hitPoint = default;
            return null;
        }
    }
}