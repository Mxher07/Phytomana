using Silk.NET.OpenGLES;
using System.Runtime.InteropServices;

namespace Engine.Graphics {
    public class IndexBuffer : GraphicsResource {
        public int m_buffer;

        public string DebugName {
            get {
                return string.Empty;
            }
            // ReSharper disable ValueParameterNotUsed
            set
            // ReSharper restore ValueParameterNotUsed
            {
                // For Direct3D backend
            }
        }

        public IndexFormat IndexFormat { get; set; }

        public int IndicesCount { get; set; }

        public object Tag { get; set; }

        public IndexBuffer(IndexFormat indexFormat, int indicesCount) {
            InitializeIndexBuffer(indexFormat, indicesCount);
            AllocateBuffer();
        }

        public override void Dispose() {
            base.Dispose();
            DeleteBuffer();
        }

        public void SetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : unmanaged {
            VerifyParametersSetData(source, sourceStartIndex, sourceCount, targetStartIndex);
            GCHandle gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try {
                int num = Utilities.SizeOf<T>();
                int size = IndexFormat.GetSize();
                unsafe {
                    GLWrapper.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
                    GLWrapper.GL.BufferSubData(
                        BufferTargetARB.ElementArrayBuffer,
                        new IntPtr(targetStartIndex * size),
                        new UIntPtr((uint)(num * sourceCount)),
                        (gCHandle.AddrOfPinnedObject() + sourceStartIndex * num).ToPointer()
                    );
                }
            }
            finally {
                gCHandle.Free();
            }
        }

        public override void HandleDeviceLost() {
            DeleteBuffer();
        }

        public override void HandleDeviceReset() {
            AllocateBuffer();
        }

        public void AllocateBuffer() {
            unsafe {
                GLWrapper.GL.GenBuffers(1, out uint buffer);
                m_buffer = (int)buffer;
                GLWrapper.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
                GLWrapper.GL.BufferData(
                    BufferTargetARB.ElementArrayBuffer,
                    new UIntPtr((uint)(IndexFormat.GetSize() * IndicesCount)),
                    null,
                    BufferUsageARB.StaticDraw
                );
            }
        }

        public void DeleteBuffer() {
            if (m_buffer != 0) {
                GLWrapper.DeleteBuffer(BufferTargetARB.ElementArrayBuffer, m_buffer);
                m_buffer = 0;
            }
        }

        public override int GetGpuMemoryUsage() => IndicesCount * IndexFormat.GetSize();

        void InitializeIndexBuffer(IndexFormat indexFormat, int indicesCount) {
            if (indicesCount <= 0) {
                throw new ArgumentException("Indices count must be greater than 0.");
            }
            IndexFormat = indexFormat;
            IndicesCount = indicesCount;
        }

        void VerifyParametersSetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : unmanaged {
            VerifyNotDisposed();
            int num = Utilities.SizeOf<T>();
            int size = IndexFormat.GetSize();
            ArgumentNullException.ThrowIfNull(source);
            if (sourceStartIndex < 0
                || sourceCount < 0
                || sourceStartIndex + sourceCount > source.Length) {
                throw new ArgumentException("Range is out of source bounds.");
            }
            if (targetStartIndex < 0
                || targetStartIndex * size + sourceCount * num > IndicesCount * size) {
                throw new ArgumentException("Range is out of target bounds.");
            }
        }
    }
}