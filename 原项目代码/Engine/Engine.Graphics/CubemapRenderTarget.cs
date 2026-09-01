using Silk.NET.OpenGLES;
using System.Runtime.InteropServices;
using Engine.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = Engine.Media.Image;

namespace Engine.Graphics {
    public class CubemapRenderTarget : CubemapTexture {
        DepthFormat m_depthFormat;
        public int m_frameBuffer;
        public int m_depthBuffer;

        public DepthFormat DepthFormat {
            get => m_depthFormat;
            set => m_depthFormat = value;
        }

        public CubemapRenderTarget() { }

        public CubemapRenderTarget(int size, int mipLevelsCount, ColorFormat colorFormat, DepthFormat depthFormat)
            : base(size, mipLevelsCount, colorFormat) {
            try {
                m_depthFormat = depthFormat;
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

        public virtual void AllocateRenderTarget() {
            GLWrapper.GL.GenFramebuffers(1u, out uint frameBuffer);
            m_frameBuffer = (int)frameBuffer;
            GLWrapper.BindFramebuffer(m_frameBuffer);

            GLWrapper.GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX,
                (uint)m_texture,
                0
            );

            if (m_depthFormat != DepthFormat.None) {
                GLWrapper.GL.GenRenderbuffers(1u, out uint depthBuffer);
                m_depthBuffer = (int)depthBuffer;
                GLWrapper.GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
                GLWrapper.GL.RenderbufferStorage(
                    RenderbufferTarget.Renderbuffer,
                    GLWrapper.TranslateDepthFormat(m_depthFormat),
                    (uint)Size,
                    (uint)Size
                );
                GLWrapper.GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer,
                    depthBuffer
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

            GLEnum status = GLWrapper.GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete) {
                throw new InvalidOperationException(string.Format("Error creating cubemap framebuffer ({0}).", status));
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

        public void BindFace(int face, int mipLevel = 0) {
            if (face < 0 || face > 5) {
                throw new ArgumentOutOfRangeException(nameof(face), "Face must be 0-5.");
            }
            if (mipLevel < 0 || mipLevel >= MipLevelsCount) {
                throw new ArgumentOutOfRangeException(nameof(mipLevel));
            }
            GLWrapper.BindFramebuffer(m_frameBuffer);
            GLWrapper.GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + face,
                (uint)m_texture,
                mipLevel
            );
            if (m_depthBuffer != 0) {
                GLWrapper.GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer,
                    (uint)m_depthBuffer
                );
            }
        }

        public void SetViewport(int mipLevel = 0) {
            int mipSize = MathUtils.Max(Size >> mipLevel, 1);
            GLWrapper.GL.Viewport(0, 0, (uint)mipSize, (uint)mipSize);
        }

        public unsafe Image GetData(int face, int mipLevel = 0) {
            VerifyNotDisposed();
            if (face < 0 || face > 5) throw new ArgumentOutOfRangeException(nameof(face));
            if (mipLevel < 0 || mipLevel >= MipLevelsCount) throw new ArgumentOutOfRangeException(nameof(mipLevel));
            BindFace(face, mipLevel);
            int mipSize = MathUtils.Max(Size >> mipLevel, 1);
            Image<Rgba32> image = new(Image.DefaultImageSharpConfiguration, mipSize, mipSize);
            image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory);
            GLWrapper.GL.ReadPixels(
                0, 0,
                (uint)mipSize, (uint)mipSize,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                memory.Pin().Pointer
            );
            return new Image(image);
        }

        public unsafe void GetData<T>(int face, T[] target, int targetStartIndex = 0, int mipLevel = 0) where T : unmanaged {
            VerifyNotDisposed();
            if (face < 0 || face > 5) throw new ArgumentOutOfRangeException(nameof(face));
            if (mipLevel < 0 || mipLevel >= MipLevelsCount) throw new ArgumentOutOfRangeException(nameof(mipLevel));
            int mipSize = MathUtils.Max(Size >> mipLevel, 1);
            int elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
            int pixelSize = ColorFormat.GetSize();
            int requiredSize = pixelSize * mipSize * mipSize;
            if (targetStartIndex < 0 || (target.Length - targetStartIndex) * elementSize < requiredSize)
                throw new InvalidOperationException("Not enough space in target array.");
            BindFace(face, mipLevel);
            GCHandle handle = GCHandle.Alloc(target, GCHandleType.Pinned);
            try {
                GLWrapper.GL.ReadPixels(
                    0, 0,
                    (uint)mipSize, (uint)mipSize,
                    m_pixelFormat,
                    m_pixelType,
                    (handle.AddrOfPinnedObject() + targetStartIndex * elementSize).ToPointer()
                );
            }
            finally {
                handle.Free();
            }
        }

        public override int GetGpuMemoryUsage() => base.GetGpuMemoryUsage() + m_depthFormat.GetSize() * Size * Size;

        public override void HandleDeviceLost() {
            DeleteRenderTarget();
            base.HandleDeviceLost();
        }

        public override void HandleDeviceReset() {
            base.HandleDeviceReset();
            AllocateRenderTarget();
        }
    }
}
