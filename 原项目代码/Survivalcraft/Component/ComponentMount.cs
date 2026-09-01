using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentMount : Component {
        public ComponentBody ComponentBody { get; set; }

        public Vector3 MountOffset { get; set; }

        public Vector3 DismountOffset { get; set; }
        public float MountAllowToStartRange { get; set; }

        public ComponentRider Rider {
            get {
                foreach (ComponentBody childBody in ComponentBody.ChildBodies) {
                    ComponentRider componentRider = childBody.Entity.FindComponent<ComponentRider>();
                    if (componentRider != null) {
                        return componentRider;
                    }
                }
                return null;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            ComponentBody = Entity.FindComponent<ComponentBody>(true);
            MountOffset = valuesDictionary.GetValue<Vector3>("MountOffset");
            DismountOffset = valuesDictionary.GetValue<Vector3>("DismountOffset");
            MountAllowToStartRange = valuesDictionary.GetValue("MountAllowToStartRange", 2.5f);
        }
    }
}