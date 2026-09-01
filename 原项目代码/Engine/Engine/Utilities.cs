using System.Runtime.InteropServices;

namespace Engine {
    public static class Utilities {
        public static void Swap<T>(ref T a, ref T b) {
            (b, a) = (a, b);
        }

        public static int SizeOf<T>() where T : unmanaged => Marshal.SizeOf<T>();

        public static unsafe T PtrToStructure<T>(void* ptr) where T : unmanaged => *(T*)ptr;

        public static unsafe T ArrayToStructure<T>(Array array) where T : unmanaged {
            GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try {
                return PtrToStructure<T>(gCHandle.AddrOfPinnedObject().ToPointer());
            }
            finally {
                gCHandle.Free();
            }
        }

        public static byte[] StructureToArray<T>(T structure) where T : unmanaged {
            byte[] array = new byte[SizeOf<T>()];
            GCHandle gCHandle = GCHandle.Alloc(structure, GCHandleType.Pinned);
            try {
                Marshal.Copy(gCHandle.AddrOfPinnedObject(), array, 0, array.Length);
                return array;
            }
            finally {
                gCHandle.Free();
            }
        }

        public static void Dispose<T>(ref T disposable) where T : IDisposable {
            if (disposable != null) {
                disposable.Dispose();
                disposable = default;
            }
        }

        public static void DisposeCollection<T>(ICollection<T> disposableCollection) where T : IDisposable {
            if (disposableCollection != null) {
                foreach (T item in disposableCollection) {
                    item?.Dispose();
                }
                if (!disposableCollection.IsReadOnly) {
                    disposableCollection.Clear();
                }
            }
        }
    }
}