using Silk.NET.OpenGLES;
using System.Runtime.InteropServices;

namespace Engine.Graphics {
    public class VertexBuffer : GraphicsResource {
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

        public VertexDeclaration VertexDeclaration { get; set; }

        public int VerticesCount { get; set; }

        public object Tag { get; set; }

        public VertexBuffer(VertexDeclaration vertexDeclaration, int verticesCount) {
            InitializeVertexBuffer(vertexDeclaration, verticesCount);
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
                int vertexStride = VertexDeclaration.VertexStride;
                unsafe {
                    GLWrapper.BindBuffer(BufferTargetARB.ArrayBuffer, m_buffer);
                    GLWrapper.GL.BufferSubData(
                        BufferTargetARB.ArrayBuffer,
                        new IntPtr(targetStartIndex * vertexStride),
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
                GLWrapper.BindBuffer(BufferTargetARB.ArrayBuffer, m_buffer);
                GLWrapper.GL.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    new UIntPtr((uint)(VertexDeclaration.VertexStride * VerticesCount)),
                    null,
                    BufferUsageARB.StaticDraw
                );
            }
        }

        public void DeleteBuffer() {
            if (m_buffer != 0) {
                GLWrapper.DeleteBuffer(BufferTargetARB.ArrayBuffer, m_buffer);
                m_buffer = 0;
            }
        }

        public override int GetGpuMemoryUsage() => VertexDeclaration.VertexStride * VerticesCount;

        public void InitializeVertexBuffer(VertexDeclaration vertexDeclaration, int verticesCount) {
            ArgumentNullException.ThrowIfNull(vertexDeclaration);
            if (verticesCount <= 0) {
                throw new ArgumentException("verticesCount must be greater than 0.");
            }
            VertexDeclaration = vertexDeclaration;
            VerticesCount = verticesCount;
        }

        public void VerifyParametersSetData<T>(T[] source, int sourceStartIndex, int sourceCount, int targetStartIndex = 0) where T : unmanaged {
            VerifyNotDisposed();
            int num = Utilities.SizeOf<T>();
            int vertexStride = VertexDeclaration.VertexStride;
            ArgumentNullException.ThrowIfNull(source);
            if (sourceStartIndex < 0
                || sourceCount < 0
                || sourceStartIndex + sourceCount > source.Length) {
                throw new ArgumentException("Range is out of source bounds.");
            }
            if (targetStartIndex < 0
                || targetStartIndex * vertexStride + sourceCount * num > VerticesCount * vertexStride) {
                throw new ArgumentException("Range is out of target bounds.");
            }
        }
    }
}