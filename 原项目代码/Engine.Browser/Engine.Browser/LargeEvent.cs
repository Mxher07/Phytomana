using System.Runtime.InteropServices;

namespace Engine.Browser {
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct LargeEvent {
        [FieldOffset(0)] public SmallEvent Header;
        [FieldOffset(4)] public float X;
        [FieldOffset(8)] public float Y;
    }
}