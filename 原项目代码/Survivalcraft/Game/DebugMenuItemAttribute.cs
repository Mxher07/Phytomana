using System.Diagnostics;

namespace Game {
    [Conditional("DEBUG")]
    public class DebugMenuItemAttribute : DebugItemAttribute {
        public double Step;

        public DebugMenuItemAttribute() => Step = 1.0;

        public DebugMenuItemAttribute(double step) => Step = step;
    }
}