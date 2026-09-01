using System.Runtime.InteropServices;

namespace Engine.Browser {
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct SmallEvent {
        [FieldOffset(0)] public InputEventType Type;
        [FieldOffset(1)] public byte Param; // KeyCode / Button / FingerId
        [FieldOffset(2)] public ushort Payload; // Char / GamepadIndex / Modifier
    }
}