using System.Runtime.InteropServices;

namespace Engine.Browser {
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct InputBuffer {
        // --- 状态快照区 (State Snapshot) ---
        public float CanvasWidth;
        public float CanvasHeight;
        public float DevicePixelRatio;
        public int Padding; // 确保 Buffer0 从 8 字节边界开始，优化 64 位对齐

        public float MousePositionX;
        public float MousePositionY;

        // 手柄摇杆状态 (4 Gamepads * 4 Axes * 4 bytes = 64 bytes)
        public fixed float GamepadAxes[16];

        // 手柄扳机状态 (4 Gamepads * 2 Triggers * 4 bytes = 32 bytes)
        public fixed float GamepadTriggers[8];

        // --- 事件流控制 ---
        public int UsedBytes; // 当前缓冲区已写入的事件字节数

        // --- 事件流数据区 ---
        public fixed byte EventData[4096];
    }
}