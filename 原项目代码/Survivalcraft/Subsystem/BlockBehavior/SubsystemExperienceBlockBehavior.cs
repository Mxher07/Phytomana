using Engine;

namespace Game {
    public class SubsystemExperienceBlockBehavior : SubsystemBlockBehavior {
        public override void OnPickableGathered(Pickable pickable, ComponentPickableGatherer target, Vector3 distanceToTarget) {
            float distanceSquared = distanceToTarget.LengthSquared();
            if (!pickable.ToRemove
                && distanceSquared < pickable.DistanceToPick * pickable.DistanceToPick) {
                ComponentLevel targetComponentLevel = target.Entity.FindComponent<ComponentLevel>();
                if (targetComponentLevel != null) {
                    targetComponentLevel.AddExperience(pickable.Count, true);
                    pickable.ToRemove = true;
                }
            }
        }
    }
}