using System.Diagnostics;

namespace Game {
    [Conditional("DEBUG")]
    public class DebugItemAttribute : Attribute {
        public int Precision = 3;

        public string Unit;
    }
}