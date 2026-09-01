using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentPickableGatherer : Component {
        public Vector3 Position;
        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public SubsystemPickables m_subsystemPickables;

        public ComponentBody m_componentBody;

        public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemGameInfo m_subsystemGameInfo;

        public virtual bool IsPickableInAttractRange(Pickable pickable, float distanceToTargetSquare) {
            if (pickable.StuckMatrix.HasValue) {
                return false;
            }
            return distanceToTargetSquare < pickable.DistanceToFlyToTarget * pickable.DistanceToFlyToTarget;
        }

        public virtual bool IsPickableInGatherRange(Pickable pickable, float distanceToTargetSquare) =>
            distanceToTargetSquare < pickable.DistanceToPick * pickable.DistanceToPick;

        public virtual bool CanGatherPickable(Pickable pickable) {
            double pickableTimeExisted = m_subsystemGameInfo.TotalElapsedGameTime - pickable.CreationTime;
            if (pickableTimeExisted < pickable.TimeWaitToAutoPick) { }
            return false;
        }

        public virtual void GatherPickable(Pickable pickable) { }

        public virtual void Update(float dt) {
            if (m_componentBody != null) {
                Position = m_componentBody.Position + new Vector3(0f, 0.75f, 0f);
            }
            for (int i = 0; i < m_subsystemPickables.Pickables.Count; i++) {
                Pickable pickable = m_subsystemPickables.Pickables[i];
                float distanceToTargetSquare = (Position - pickable.Position).LengthSquared();
                if (IsPickableInGatherRange(pickable, distanceToTargetSquare)) {
                    if (CanGatherPickable(pickable)) {
                        lock (pickable) {
                            GatherPickable(pickable);
                            if (!pickable.ToRemove && pickable.Count == 0) {
                                pickable.ToRemove = true;
                            }
                        }
                    }
                }
                else if (IsPickableInAttractRange(pickable, distanceToTargetSquare)) {
                    if (CanGatherPickable(pickable)) {
                        lock (pickable) {
                            pickable.FlyToPosition = Position + 0.1f * MathF.Sqrt(distanceToTargetSquare) * m_componentBody.Velocity;
                            pickable.FlyToGatherer = this;
                        }
                    }
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_componentBody = Entity.FindComponent<ComponentBody>();
        }
    }
}