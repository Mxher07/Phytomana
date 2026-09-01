using GameEntitySystem;

namespace Game {
    public abstract class ComponentBehavior : Component {
        public abstract float ImportanceLevel { get; }

        public virtual bool IsActive { get; set; }

        public virtual string DebugInfo => string.Empty;
    }
}