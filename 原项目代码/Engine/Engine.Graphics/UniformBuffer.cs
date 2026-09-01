using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace Engine.Graphics {
    /// <summary>
    /// 通用 Uniform Buffer Object (UBO) 实现
    /// 使用 std140 布局
    /// </summary>
    public class UniformBuffer<T> : IDisposable where T : unmanaged {
        public readonly uint m_handle;
        public readonly int m_size;

        /// <summary>
        /// UBO 绑定点
        /// </summary>
        public int BindingPoint { get; }

        public unsafe UniformBuffer(int bindingPoint) {
            BindingPoint = bindingPoint;
            m_size = Marshal.SizeOf<T>();
            m_handle = GLWrapper.GL.GenBuffer();
            GLWrapper.GL.BindBuffer(BufferTargetARB.UniformBuffer, m_handle);
            GLWrapper.GL.BufferData(BufferTargetARB.UniformBuffer, (nuint)m_size, null, BufferUsageARB.DynamicDraw);
            GLWrapper.GL.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)BindingPoint, m_handle);
        }

        /// <summary>
        /// 更新 UBO 数据
        /// </summary>
        public unsafe void Update(ref T data) {
            GLWrapper.GL.BindBuffer(BufferTargetARB.UniformBuffer, m_handle);
            fixed (T* ptr = &data) {
                GLWrapper.GL.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)m_size, ptr);
            }
        }

        /// <summary>
        /// 绑定到指定着色器的 uniform block
        /// </summary>
        public void BindToShader(uint programHandle, string blockName) {
            uint blockIndex = GLWrapper.GL.GetUniformBlockIndex(programHandle, blockName);
            if (blockIndex != uint.MaxValue) {
                GLWrapper.GL.UniformBlockBinding(programHandle, blockIndex, (uint)BindingPoint);
            }
        }

        public void Dispose() {
            GLWrapper.GL.DeleteBuffer(m_handle);
        }
    }
}