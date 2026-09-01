using Silk.NET.OpenGLES;
using System.Runtime.InteropServices;

namespace Engine.Graphics {
    public static class Display {
        static RenderTarget2D m_renderTarget;

        static RasterizerState m_rasterizerState = RasterizerState.CullCounterClockwise;

        static DepthStencilState m_depthStencilState = DepthStencilState.Default;

        static BlendState m_blendState = BlendState.Opaque;

        static bool m_useReducedZRange;

        public static Point2 BackbufferSize { get; private set; }

        public static Viewport Viewport { get; set; }

        public static Rectangle ScissorRectangle { get; set; }

        public static RasterizerState RasterizerState {
            get => m_rasterizerState;
            set {
                ArgumentNullException.ThrowIfNull(value);
                m_rasterizerState = value;
                value.IsLocked = true;
            }
        }

        public static DepthStencilState DepthStencilState {
            get => m_depthStencilState;
            set {
                ArgumentNullException.ThrowIfNull(value);
                m_depthStencilState = value;
                value.IsLocked = true;
            }
        }

        public static BlendState BlendState {
            get => m_blendState;
            set {
                ArgumentNullException.ThrowIfNull(value);
                m_blendState = value;
                value.IsLocked = true;
            }
        }

        public static Point2? BackbufferSizeOverride;

        public static RenderTarget2D RenderTarget {
            get => m_renderTarget;
            set {
                m_renderTarget = value;
                if (value != null) {
                    Viewport = new Viewport(0, 0, value.Width, value.Height);
                    ScissorRectangle = new Rectangle(0, 0, value.Width, value.Height);
                }
                else {
                    Point2 size = BackbufferSizeOverride ?? BackbufferSize;
                    Viewport = new Viewport(0, 0, size.X, size.Y);
                    ScissorRectangle = new Rectangle(0, 0, size.X, size.Y);
                }
            }
        }

        public static bool UseReducedZRange {
            get => m_useReducedZRange;
            set {
                if (value != m_useReducedZRange) {
                    m_useReducedZRange = value;
                    foreach (GraphicsResource resource in GraphicsResource.m_resources) {
                        (resource as Shader)?.CompileShaders();
                    }
                }
            }
        }

        public static string DeviceDescription { get; set; }

        public static int MaxTextureSize {
            get {
                return GLWrapper.GL_MAX_TEXTURE_SIZE;
            }
        }

        public static event Action DeviceLost;

        public static event Action DeviceReset;

        public static void DrawUser<T>(PrimitiveType primitiveType,
            Shader shader,
            VertexDeclaration vertexDeclaration,
            T[] vertexData,
            int startVertex,
            int verticesCount) where T : unmanaged {
            VerifyParametersDrawUser(primitiveType, shader, vertexDeclaration, vertexData, startVertex, verticesCount);
            GCHandle gCHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            try {
                GLWrapper.ApplyRenderTarget(RenderTarget);
                GLWrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
                GLWrapper.ApplyShaderAndBuffers(
                    shader,
                    vertexDeclaration,
                    gCHandle.AddrOfPinnedObject() + startVertex * vertexDeclaration.VertexStride,
                    0,
                    null
                );
                GLWrapper.ApplyRasterizerState(RasterizerState);
                GLWrapper.ApplyDepthStencilState(DepthStencilState);
                GLWrapper.ApplyBlendState(BlendState);
                GLWrapper.GL.DrawArrays(GLWrapper.TranslatePrimitiveType(primitiveType), startVertex, (uint)verticesCount);
            }
            finally {
                gCHandle.Free();
            }
        }

        public static void DrawUserIndexed<T>(PrimitiveType primitiveType,
            Shader shader,
            VertexDeclaration vertexDeclaration,
            T[] vertexData,
            int startVertex,
            int verticesCount,
            int[] indexData,
            int startIndex,
            int indicesCount) where T : unmanaged {
            VerifyParametersDrawUserIndexed(
                primitiveType,
                shader,
                vertexDeclaration,
                vertexData,
                startVertex,
                verticesCount,
                indexData,
                startIndex,
                indicesCount
            );
            unsafe {
                GCHandle gCHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
                GCHandle gCHandle2 = GCHandle.Alloc(indexData, GCHandleType.Pinned);
                try {
                    GLWrapper.ApplyRenderTarget(RenderTarget);
                    GLWrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
                    GLWrapper.ApplyShaderAndBuffers(shader, vertexDeclaration, gCHandle.AddrOfPinnedObject(), 0, 0);
                    GLWrapper.ApplyRasterizerState(RasterizerState);
                    GLWrapper.ApplyDepthStencilState(DepthStencilState);
                    GLWrapper.ApplyBlendState(BlendState);
                    GLWrapper.GL.DrawElements(
                        GLWrapper.TranslatePrimitiveType(primitiveType),
                        (uint)indicesCount,
                        DrawElementsType.UnsignedInt,
                        (gCHandle2.AddrOfPinnedObject() + 4 * startIndex).ToPointer()
                    );
                }
                finally {
                    gCHandle.Free();
                    gCHandle2.Free();
                }
            }
        }

        public static void Draw(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, int startVertex, int verticesCount) {
            VerifyParametersDraw(primitiveType, shader, vertexBuffer, startVertex, verticesCount);
            GLWrapper.ApplyRenderTarget(RenderTarget);
            GLWrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
            GLWrapper.ApplyShaderAndBuffers(shader, vertexBuffer.VertexDeclaration, IntPtr.Zero, vertexBuffer.m_buffer, null);
            GLWrapper.ApplyRasterizerState(RasterizerState);
            GLWrapper.ApplyDepthStencilState(DepthStencilState);
            GLWrapper.ApplyBlendState(BlendState);
            GLWrapper.GL.DrawArrays(GLWrapper.TranslatePrimitiveType(primitiveType), startVertex, (uint)verticesCount);
        }

        public static void DrawIndexed(PrimitiveType primitiveType,
            Shader shader,
            VertexBuffer vertexBuffer,
            IndexBuffer indexBuffer,
            int startIndex,
            int indicesCount) {
            VerifyParametersDrawIndexed(primitiveType, shader, vertexBuffer, indexBuffer, startIndex, indicesCount);
            unsafe {
                GLWrapper.ApplyRenderTarget(RenderTarget);
                GLWrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
                GLWrapper.ApplyShaderAndBuffers(shader, vertexBuffer.VertexDeclaration, IntPtr.Zero, vertexBuffer.m_buffer, indexBuffer.m_buffer);
                GLWrapper.ApplyRasterizerState(RasterizerState);
                GLWrapper.ApplyDepthStencilState(DepthStencilState);
                GLWrapper.ApplyBlendState(BlendState);
                GLWrapper.GL.DrawElements(
                    GLWrapper.TranslatePrimitiveType(primitiveType),
                    (uint)indicesCount,
                    GLWrapper.TranslateIndexFormat(indexBuffer.IndexFormat),
                    new IntPtr(startIndex * indexBuffer.IndexFormat.GetSize()).ToPointer()
                );
            }
        }

        public static void Clear(Vector4? color, float? depth = null, int? stencil = null) {
            GLWrapper.Clear(RenderTarget, color, depth, stencil);
        }

        public static void ResetGLStateCache() {
            GLWrapper.InitializeCache();
        }

        public static void Initialize() {
            GLWrapper.Initialize();
            GLWrapper.InitializeCache();
            ShaderCache.Initialize();
            Resize();
        }

#if ANGLE
        /// <summary>
        /// 初始化无头显示设备（用于测试和离屏渲染）
        /// 不需要窗口，使用 PBuffer 进行离屏渲染
        /// </summary>
        /// <param name="width">渲染缓冲区宽度</param>
        /// <param name="height">渲染缓冲区高度</param>
        public static void InitializeHeadless(int width = 256, int height = 256) {
            GLWrapper.InitializeHeadless(width, height);
            GLWrapper.InitializeCache();
            BackbufferSize = new Point2(width, height);
            Viewport = new Viewport(0, 0, width, height);
            ScissorRectangle = new Rectangle(0, 0, width, height);
        }
#endif

        public static void Dispose() { }

        public static void BeforeFrame() { }

        public static void AfterFrame() { }

        public static void Resize() {
            Point2 size = Window.Size;
            BackbufferSize = new Point2(size.X, size.Y);
            Viewport = new Viewport(0, 0, size.X, size.Y);
            ScissorRectangle = new Rectangle(0, 0, size.X, size.Y);
        }

        public static long GetGpuMemoryUsage() {
            long num = 8 * BackbufferSize.X * BackbufferSize.Y;
            foreach (GraphicsResource resource in GraphicsResource.m_resources) {
                num += resource.GetGpuMemoryUsage();
            }
            return num;
        }

        public static void Clear(Color? color, float? depth = null, int? stencil = null) {
            Clear(color.HasValue ? new Vector4?(new Vector4(color.Value)) : null, depth, stencil);
        }

        public static void VerifyParametersDrawUser<T>(PrimitiveType primitiveType,
            Shader shader,
            VertexDeclaration vertexDeclaration,
            T[] vertexData,
            int startVertex,
            int verticesCount) where T : unmanaged {
            int num = Utilities.SizeOf<T>();
            ArgumentNullException.ThrowIfNull(shader);
            ArgumentNullException.ThrowIfNull(vertexDeclaration);
            ArgumentNullException.ThrowIfNull(vertexData);
            if (vertexDeclaration.VertexStride / num * num != vertexDeclaration.VertexStride) {
                throw new InvalidOperationException(
                    $"Vertex is not an integer multiple of array element, vertex stride is {vertexDeclaration.VertexStride}, array element is {num}."
                );
            }
            if (startVertex < 0
                || verticesCount < 0
                || startVertex + verticesCount > vertexData.Length) {
                throw new ArgumentException("Vertices range is out of bounds.");
            }
            shader.VerifyNotDisposed();
        }

        public static void VerifyParametersDrawUserIndexed<T>(PrimitiveType primitiveType,
            Shader shader,
            VertexDeclaration vertexDeclaration,
            T[] vertexData,
            int startVertex,
            int verticesCount,
            int[] indexData,
            int startIndex,
            int indicesCount) where T : unmanaged {
            int num = Utilities.SizeOf<T>();
            ArgumentNullException.ThrowIfNull(shader);
            ArgumentNullException.ThrowIfNull(vertexDeclaration);
            ArgumentNullException.ThrowIfNull(vertexData);
            ArgumentNullException.ThrowIfNull(indexData);
            if (vertexDeclaration.VertexStride / num * num != vertexDeclaration.VertexStride) {
                throw new InvalidOperationException(
                    $"Vertex is not an integer multiple of array element, vertex stride is {vertexDeclaration.VertexStride}, array element is {num}."
                );
            }
            if (startVertex < 0
                || verticesCount < 0
                || startVertex + verticesCount > vertexData.Length) {
                throw new ArgumentException("Vertices range is out of bounds.");
            }
            if (startIndex < 0
                || indicesCount < 0
                || startIndex + indicesCount > indexData.Length) {
                throw new ArgumentException("Indices range is out of bounds.");
            }
            shader.VerifyNotDisposed();
        }

        public static void VerifyParametersDraw(PrimitiveType primitiveType,
            Shader shader,
            VertexBuffer vertexBuffer,
            int startVertex,
            int verticesCount) {
            vertexBuffer.VerifyNotDisposed();
            ArgumentNullException.ThrowIfNull(shader);
            ArgumentNullException.ThrowIfNull(vertexBuffer);
            if (startVertex < 0
                || verticesCount < 0
                || startVertex + verticesCount > vertexBuffer.VerticesCount) {
                throw new ArgumentException("Vertices range is out of bounds.");
            }
            shader.VerifyNotDisposed();
        }

        public static void VerifyParametersDrawIndexed(PrimitiveType primitiveType,
            Shader shader,
            VertexBuffer vertexBuffer,
            IndexBuffer indexBuffer,
            int startIndex,
            int indicesCount) {
            ArgumentNullException.ThrowIfNull(shader);
            ArgumentNullException.ThrowIfNull(vertexBuffer);
            ArgumentNullException.ThrowIfNull(indexBuffer);
            if (startIndex < 0
                || indicesCount < 0
                || startIndex + indicesCount > indexBuffer.IndicesCount) {
                throw new ArgumentException("Indices range is out of bounds.");
            }
            shader.VerifyNotDisposed();
            vertexBuffer.VerifyNotDisposed();
            indexBuffer.VerifyNotDisposed();
        }

        public static void HandleDeviceLost() {
            foreach (GraphicsResource resource in GraphicsResource.m_resources) {
                resource.HandleDeviceLost();
            }
            DeviceLost?.Invoke();
        }

        public static void HandleDeviceReset() {
            foreach (GraphicsResource resource in GraphicsResource.m_resources) {
                resource.HandleDeviceReset();
            }
            DeviceReset?.Invoke();
        }
    }
}