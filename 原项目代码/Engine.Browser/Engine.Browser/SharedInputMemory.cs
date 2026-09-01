using System.Runtime.InteropServices;

namespace Engine.Browser {
    [StructLayout(LayoutKind.Sequential)]
    public struct SharedInputMemory {
        public int ActiveIndex; // 0 或 1，主线程 JS 写入的 Buffer 序号
        public int Padding; // 确保 Buffer0 从 8 字节边界开始，优化 64 位对齐
        public InputBuffer Buffer0;
        public InputBuffer Buffer1;
    }
}