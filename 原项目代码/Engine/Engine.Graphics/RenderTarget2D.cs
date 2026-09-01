using Silk.NET.OpenGLES;
using System.Runtime.InteropServices;
using Engine.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = Engine.Media.Image;

namespace Engine.Graphics {
    public class RenderTarget2D : Texture2D {
        DepthFormat m_depthFormat;
        public int m_frameBuffer;
        public int m_depthBuffer;

        public DepthFormat DepthFormat {
            get => m_depthFormat;
            set => m_depthFormat = value;
        }

        public RenderTarget2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat, DepthFormat depthFormat) : base(
            width,
            height,
            mipLevelsCount,
            colorFormat
        ) {
            try {
                InitializeRenderTarget2D(width, height, mipLevelsCount, colorFormat, depthFormat);
                AllocateRenderTarget();
            }
            catch {
                Dispose();
                throw;
            }
        }

        public override void Dispose() {
            base.Dispose();
            DeleteRenderTarget();
        }

        public void GetData<T>(T[] target, int targetStartIndex, Rectangle sourceRectangle) where T : unmanaged {
            VerifyParametersGetData(target, targetStartIndex, sourceRectangle);
            GCHandle gCHandle = GCHandle.Alloc(target, GCHandleType.Pinned);
            try {
                int num = Utilities.SizeOf<T>();
                GetDataInternal(gCHandle.AddrOfPinnedObject() + targetStartIndex * num, sourceRectangle);
            }
            finally {
                gCHandle.Free();
            }
        }

        public unsafe Image GetData(Rectangle sourceRectangle) {
            VerifyNotDisposed();
            Image<Rgba32> image = new(Image.DefaultImageSharpConfiguration, sourceRectangle.Width, sourceRectangle.Height);
            image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory);
            GetDataInternal((nint)memory.Pin().Pointer, sourceRectangle);
            return new Image(image);
        }

        public void GetData(nint target, Rectangle sourceRectangle) {
            VerifyParametersGetData(target, sourceRectangle);
            GetDataInternal(target, sourceRectangle);
        }

        public void GetDataInternal(nint target, Rectangle sourceRectangle) {
            unsafe {
                GLWrapper.BindFramebuffer(m_frameBuffer);
                GLWrapper.GL.ReadPixels(
                    sourceRectangle.Left,
                    sourceRectangle.Top,
                    (uint)sourceRectangle.Width,
                    (uint)sourceRectangle.Height,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    target.ToPointer()
                );
            }
        }

        public void GenerateMipMaps() {
            GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, false);
            GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
        }

        /// <summary>
        /// 从 backbuffer blit 颜色内容到本 RenderTarget（支持格式转换如 RGBA8→RGBA16F）
        /// </summary>
        public void BlitFromBackbuffer(int screenWidth, int screenHeight) {
            // READ framebuffer = backbuffer, DRAW framebuffer = this
            GLWrapper.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (uint)GLWrapper.m_mainFramebuffer);
            GLWrapper.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, (uint)m_frameBuffer);
            GLWrapper.GL.BlitFramebuffer(
                0, 0, (int)Math.Min(screenWidth, Width), (int)Math.Min(screenHeight, Height),
                0, 0, Width, Height,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Linear
            );
            // Raw GL 调用绕过了 GLWrapper 缓存，必须使缓存失效
            GLWrapper.m_framebuffer = -1;
            GLWrapper.BindFramebuffer(GLWrapper.m_mainFramebuffer);
        }

        /// <summary>
        /// Blits a color rectangle from another render target into this target.
        /// </summary>
        public void BlitFromRenderTarget(RenderTarget2D source, Rectangle sourceRectangle) {
            ArgumentNullException.ThrowIfNull(source);
            GLWrapper.GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (uint)source.m_frameBuffer);
            GLWrapper.GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, (uint)m_frameBuffer);
            GLWrapper.GL.BlitFramebuffer(
                sourceRectangle.Left,
                sourceRectangle.Top,
                sourceRectangle.Right,
                sourceRectangle.Bottom,
                0,
                0,
                Width,
                Height,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Linear
            );
            GLWrapper.m_framebuffer = -1;
        }

        public override void HandleDeviceLost() {
            DeleteRenderTarget();
        }

        public override void HandleDeviceReset() {
            AllocateRenderTarget();
        }

        public virtual void AllocateRenderTarget() {
            GLWrapper.GL.GenFramebuffers(1u, out uint frameBuffer);
            m_frameBuffer = (int)frameBuffer;
            GLWrapper.BindFramebuffer(m_frameBuffer);
            GLWrapper.GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                (uint)m_texture,
                0
            );
            if (DepthFormat != DepthFormat.None) {
                GLWrapper.GL.GenRenderbuffers(1u, out uint depthBuffer);
                m_depthBuffer = (int)depthBuffer;
                GLWrapper.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
                GLWrapper.GL.RenderbufferStorage(
                    RenderbufferTarget.Renderbuffer,
                    GLWrapper.TranslateDepthFormat(DepthFormat),
                    (uint)Width,
                    (uint)Height
                );
                GLWrapper.GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer,
                    depthBuffer
                );
                GLWrapper.GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.StencilAttachment,
                    RenderbufferTarget.Renderbuffer,
                    0
                );
            }
            else {
                GLWrapper.GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer,
                    0
                );
                GLWrapper.GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.StencilAttachment,
                    RenderbufferTarget.Renderbuffer,
                    0
                );
            }
            GLEnum framebufferErrorCode = GLWrapper.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (framebufferErrorCode != GLEnum.FramebufferComplete) {
                throw new InvalidOperationException($"Error creating framebuffer ({framebufferErrorCode.ToString()}).");
            }
        }

        public virtual void DeleteRenderTarget() {
            if (m_depthBuffer != 0) {
                uint depthBuffer = (uint)m_depthBuffer;
                GLWrapper.GL.DeleteRenderbuffers(1, in depthBuffer);
                m_depthBuffer = 0;
            }
            if (m_frameBuffer != 0) {
                GLWrapper.DeleteFramebuffer(m_frameBuffer);
                m_frameBuffer = 0;
            }
        }

        public new static RenderTarget2D Load(Color color, int width, int height) {
            RenderTarget2D renderTarget2D = new(width, height, 1, ColorFormat.Rgba8888, DepthFormat.None);
            Color[] array = new Color[width * height];
            for (int i = 0; i < array.Length; i++) {
                array[i] = color;
            }
            renderTarget2D.SetData(0, array);
            return renderTarget2D;
        }

        public new static RenderTarget2D Load(Image image, int mipLevelsCount = 1) {
            RenderTarget2D renderTarget2D = new(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888, DepthFormat.None);
            renderTarget2D.SetData(image.m_trueImage);
            if (mipLevelsCount > 1) {
                GLWrapper.BindTexture(TextureTarget.Texture2D, renderTarget2D.m_texture, false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            return renderTarget2D;
        }

        public new static RenderTarget2D Load(Stream stream, bool premultiplyAlpha = false, int mipLevelsCount = 1) {
            Image image = Image.Load(stream);
            if (premultiplyAlpha) {
                Image.PremultiplyAlpha(image);
            }
            return Load(image, mipLevelsCount);
        }

        public new static RenderTarget2D Load(string fileName, bool premultiplyAlpha = false, int mipLevelsCount = 1) {
            using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read);
            return Load(stream, premultiplyAlpha, mipLevelsCount);
        }

        public static Image Save(RenderTarget2D renderTarget) {
            if (renderTarget.ColorFormat != ColorFormat.Rgba8888) {
                throw new InvalidOperationException("Unsupported color format.");
            }
            return renderTarget.GetData(new Rectangle(0, 0, renderTarget.Width, renderTarget.Height));
        }

        public static void Save(RenderTarget2D renderTarget, Stream stream, ImageFileFormat format, bool saveAlpha) {
            if (renderTarget.ColorFormat != ColorFormat.Rgba8888) {
                throw new InvalidOperationException("Unsupported color format.");
            }
            Image.Save(renderTarget.GetData(new Rectangle(0, 0, renderTarget.Width, renderTarget.Height)), stream, format, saveAlpha);
        }

        public static void Save(RenderTarget2D renderTarget, string fileName, ImageFileFormat format, bool saveAlpha) {
            using Stream stream = Storage.OpenFile(fileName, OpenFileMode.Create);
            Save(renderTarget, stream, format, saveAlpha);
        }

        public override int GetGpuMemoryUsage() => base.GetGpuMemoryUsage() + DepthFormat.GetSize() * Width * Height;

        // ReSharper disable UnusedParameter.Local
        void InitializeRenderTarget2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat, DepthFormat depthFormat)
            // ReSharper restore UnusedParameter.Local
        {
            DepthFormat = depthFormat;
        }

        void VerifyParametersGetData<T>(T[] target, int targetStartIndex, Rectangle sourceRectangle) where T : unmanaged {
            VerifyNotDisposed();
            int size = ColorFormat.GetSize();
            int num = Utilities.SizeOf<T>();
            ArgumentNullException.ThrowIfNull(target);
            if (num > size) {
                throw new ArgumentException("Target array element size is larger than pixel size.");
            }
            if (size % num != 0) {
                throw new ArgumentException("Pixel size is not an integer multiple of target array element size.");
            }
            if (sourceRectangle.Left < 0
                || sourceRectangle.Width <= 0
                || sourceRectangle.Top < 0
                || sourceRectangle.Height <= 0
                || sourceRectangle.Left + sourceRectangle.Width > Width
                || sourceRectangle.Top + sourceRectangle.Height > Height) {
                throw new ArgumentOutOfRangeException(nameof(sourceRectangle));
            }
            if (targetStartIndex < 0
                || targetStartIndex >= target.Length) {
                throw new ArgumentOutOfRangeException(nameof(targetStartIndex));
            }
            if ((target.Length - targetStartIndex) * num < sourceRectangle.Width * sourceRectangle.Height * size) {
                throw new InvalidOperationException("Not enough space in target array.");
            }
        }

        public void VerifyParametersGetData(nint target, Rectangle sourceRectangle) {
            VerifyNotDisposed();
            if (target == IntPtr.Zero) {
                throw new ArgumentNullException(nameof(target));
            }
            if (sourceRectangle.Left < 0
                || sourceRectangle.Width <= 0
                || sourceRectangle.Top < 0
                || sourceRectangle.Height <= 0
                || sourceRectangle.Left + sourceRectangle.Width > Width
                || sourceRectangle.Top + sourceRectangle.Height > Height) {
                throw new ArgumentOutOfRangeException(nameof(sourceRectangle));
            }
        }

        public static void VerifyParametersSwap(RenderTarget2D renderTarget1, RenderTarget2D renderTarget2) {
            if (renderTarget1 == null) {
                throw new ArgumentNullException(nameof(renderTarget1));
            }
            if (renderTarget2 == null) {
                throw new ArgumentNullException(nameof(renderTarget2));
            }
            renderTarget1.VerifyNotDisposed();
            renderTarget2.VerifyNotDisposed();
        }
    }
}
