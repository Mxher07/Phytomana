using System.Diagnostics;

namespace Game {
    [Conditional("DEBUG")]
    public class FloatListDebugMenuItemAttribute : DebugMenuItemAttribute {
        public float[] Items;

        public FloatListDebugMenuItemAttribute(float[] items, int precision, string unit) : base(0.0) {
            Items = items;
            Precision = precision;
            Unit = unit;
        }
    }
}