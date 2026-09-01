using Silk.NET.OpenGLES;

namespace Engine.Graphics {
    /// <summary>
    /// 骨骼动画纹理，用于存储关节矩阵
    /// 使用 RGBA32F 纹理存储 mat4 矩阵数组，无骨骼数量限制
    /// </summary>
    public class JointTexture : IDisposable {
        public bool m_disposed;
        public readonly float[] m_textureData;
        public readonly Matrix[] m_normalMatrices;

        /// <summary>
        /// 纹理句柄
        /// </summary>
        public uint TextureHandle { get; private set; }

        /// <summary>
        /// 纹理尺寸（宽高相等）
        /// </summary>
        public int TextureSize { get; }

        /// <summary>
        /// 最大关节数量
        /// </summary>
        public int MaxJointCount { get; }

        /// <summary>
        /// 创建骨骼纹理
        /// </summary>
        /// <param name="maxJoints">最大关节数</param>
        public JointTexture(int maxJoints) {
            MaxJointCount = maxJoints;

            // 每个关节需要 2 个 mat4（jointMatrix + normalMatrix）
            // 每个 mat4 需要 4 个像素（每个像素 RGBA32F = vec4）
            // 所以每个关节需要 8 个像素
            TextureSize = (int)Math.Ceiling(Math.Sqrt(maxJoints * 8));
            m_textureData = new float[TextureSize * TextureSize * 4];
            m_normalMatrices = new Matrix[maxJoints];
            CreateTexture();
        }

        public unsafe void CreateTexture() {
            TextureHandle = GLWrapper.GL.GenTexture();
            GLWrapper.ActiveTexture(TextureUnit.Texture0);
            GLWrapper.BindTexture(TextureTarget.Texture2D, (int)TextureHandle, true);
            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GLWrapper.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GLWrapper.GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba32f,
                (uint)TextureSize,
                (uint)TextureSize,
                0,
                PixelFormat.Rgba,
                PixelType.Float,
                null
            );
            GLWrapper.BindTexture(TextureTarget.Texture2D, 0, true);
        }

        /// <summary>
        /// 更新骨骼纹理数据
        /// </summary>
        /// <param name="jointMatrices">关节矩阵数组（已与逆绑定矩阵相乘）</param>
        public unsafe void Update(Matrix[] jointMatrices) {
            if (jointMatrices == null
                || jointMatrices.Length == 0) {
                return;
            }
            fixed (Matrix* ptr = jointMatrices) {
                UpdateCore(ptr, Math.Min(jointMatrices.Length, MaxJointCount));
            }
        }

        /// <summary>
        /// 更新骨骼纹理数据（使用 span 避免数组分配）
        /// </summary>
        /// <param name="jointMatrices">关节矩阵 span</param>
        public unsafe void Update(ReadOnlySpan<Matrix> jointMatrices) {
            if (jointMatrices.IsEmpty) {
                return;
            }
            fixed (Matrix* ptr = jointMatrices) {
                UpdateCore(ptr, Math.Min(jointMatrices.Length, MaxJointCount));
            }
        }

        public unsafe void UpdateCore(Matrix* matricesPtr, int count) {
            GLWrapper.ActiveTexture(TextureUnit.Texture0);
            GLWrapper.BindTexture(TextureTarget.Texture2D, (int)TextureHandle, true);
            for (int i = 0; i < count; i++) {
                Matrix jointMatrix = matricesPtr[i];

                // 计算法线矩阵（逆转置）
                m_normalMatrices[i] = Matrix.Transpose(Matrix.Invert(jointMatrix));

                // 写入 jointMatrix（offset = i * 32 floats）
                int offset = i * 32;
                WriteMatrixToTextureData(m_textureData, offset, jointMatrix);

                // 写入 normalMatrix（offset = i * 32 + 16 floats）
                WriteMatrixToTextureData(m_textureData, offset + 16, m_normalMatrices[i]);
            }
            fixed (float* ptr = m_textureData) {
                GLWrapper.GL.TexSubImage2D(
                    TextureTarget.Texture2D,
                    0,
                    0,
                    0,
                    (uint)TextureSize,
                    (uint)TextureSize,
                    PixelFormat.Rgba,
                    PixelType.Float,
                    ptr
                );
            }
            GLWrapper.BindTexture(TextureTarget.Texture2D, 0, true);
        }

        public static void WriteMatrixToTextureData(float[] data, int offset, Matrix matrix) {
            data[offset + 0] = matrix.M11;
            data[offset + 1] = matrix.M12;
            data[offset + 2] = matrix.M13;
            data[offset + 3] = matrix.M14;
            data[offset + 4] = matrix.M21;
            data[offset + 5] = matrix.M22;
            data[offset + 6] = matrix.M23;
            data[offset + 7] = matrix.M24;
            data[offset + 8] = matrix.M31;
            data[offset + 9] = matrix.M32;
            data[offset + 10] = matrix.M33;
            data[offset + 11] = matrix.M34;
            data[offset + 12] = matrix.M41;
            data[offset + 13] = matrix.M42;
            data[offset + 14] = matrix.M43;
            data[offset + 15] = matrix.M44;
        }

        /// <summary>
        /// 绑定骨骼纹理到指定纹理单元
        /// </summary>
        public void Bind(int textureSlot) {
            // 必须使用 GLWrapper 封装方法以保持内部缓存同步
            // 直接调用 GLWrapper.GL 会绕过缓存，导致后续渲染的纹理绑定到错误的 texture unit
            GLWrapper.ActiveTexture(TextureUnit.Texture0 + textureSlot);
            GLWrapper.BindTexture(TextureTarget.Texture2D, (int)TextureHandle, true);
        }

        public void Dispose() {
            if (m_disposed) {
                return;
            }
            if (TextureHandle != 0) {
                GLWrapper.GL.DeleteTexture(TextureHandle);
                TextureHandle = 0;
            }
            m_disposed = true;
        }
    }
}