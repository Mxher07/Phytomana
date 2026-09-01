using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game {
    public static unsafe class ContentFileBridge {
        static byte* _sharedPtr;

        public static IntPtr Initialize() {
            nuint length = (nuint)(ContentFileInfo.FileSize + 8);
            void* ptr = NativeMemory.AlignedAlloc(length, 16);
            NativeMemory.Clear(ptr, length);
            _sharedPtr = (byte*)ptr;
            return (IntPtr)ptr;
        }

        public static UnmanagedMemoryStream GetStream() => new(_sharedPtr + 8, ContentFileInfo.FileSize);

        public static bool GetIsDownloaded() => Volatile.Read(ref Unsafe.AsRef<byte>(_sharedPtr)) == 1;
    }
}