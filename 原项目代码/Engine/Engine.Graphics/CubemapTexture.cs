using Silk.NET.OpenGLES;
using System.Runtime.InteropServices;

namespace Engine.Graphics {
    public class CubemapTexture : GraphicsResource {
        int m_size;
        ColorFormat m_colorFormat;
        int m_mipLevelsCount;
        public int m_texture;
        public PixelFormat m_pixelFormat;
        public PixelType m_pixelType;
        InternalFormat m_internalFormat;

        public IntPtr NativeHandle => m_texture;

        public int Size {
            get => m_size;
            set => m_size = value;
        }

        public ColorFormat ColorFormat {
            get => m_colorFormat;
            set => m_colorFormat = value;
        }

        public int MipLevelsCount {
            get => m_mipLevelsCount;
            set => m_mipLevelsCount = value;
        }

        public CubemapTexture() { }

        public CubemapTexture(int size, int mipLevelsCount, ColorFormat colorFormat) {
            InitializeCubemapTexture(size, mipLevelsCount, colorFormat);
            switch (ColorFormat) {
                case ColorFormat.Rgba8888:
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.UnsignedByte;
                    m_internalFormat = InternalFormat.Rgba8;
                    break;
                case ColorFormat.Rgba8888Srgb:
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.UnsignedByte;
                    m_internalFormat = InternalFormat.Srgb8Alpha8;
                    break;
                case ColorFormat.Rgba16f:
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.HalfFloat;
                    m_internalFormat = InternalFormat.Rgba16f;
                    break;
                case ColorFormat.RGBA32f:
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.Float;
                    m_internalFormat = InternalFormat.Rgba32f;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported color format for cubemap.");
            }
            AllocateTexture();
        }

        public override void Dispose() {
            base.Dispose();
            DeleteTexture();
        }

        public virtual void InitializeCubemapTexture(int size, int mipLevelsCount, ColorFormat colorFormat) {
            if (size < 1) {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
            if (mipLevelsCount < 1) {
                throw new ArgumentOutOfRangeException(nameof(mipLevelsCount));
            }
            Size = size;
            ColorFormat = colorFormat;
            if (mipLevelsCount > 1) {
                int num = 0;
                for (int num2 = size; num2 >= 1; num2 /= 2) {
                    num++;
                }
                MipLevelsCount = MathUtils.Min(num, mipLevelsCount);
            }
            else {
                MipLevelsCount = 1;
            }
        }

        public virtual unsafe void AllocateTexture() {
            GLWrapper.GL.GenTextures(1, out uint texture);
            m_texture = (int)texture;
            GLWrapper.BindTexture(TextureTarget.TextureCubeMap, m_texture, true);
            for (int face = 0; face < 6; face++) {
                for (int mip = 0; mip < MipLevelsCount; mip++) {
                    int mipSize = MathUtils.Max(Size >> mip, 1);
                    GLWrapper.GL.TexImage2D(
                        TextureTarget.TextureCubeMapPositiveX + face,
                        mip,
                        m_internalFormat,
                        (uint)mipSize,
                        (uint)mipSize,
                        0,
                        m_pixelFormat,
                        m_pixelType,
                        null
                    );
                }
            }
        }

        public virtual void SetData<T>(int face, int mipLevel, T[] source, int sourceStartIndex = 0) where T : unmanaged {
            VerifyNotDisposed();
            if (face < 0 || face > 5) throw new ArgumentOutOfRangeException(nameof(face));
            if (mipLevel < 0 || mipLevel >= MipLevelsCount) throw new ArgumentOutOfRangeException(nameof(mipLevel));
            int mipSize = MathUtils.Max(Size >> mipLevel, 1);
            int elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
            int pixelSize = m_colorFormat.GetSize();
            int requiredSize = pixelSize * mipSize * mipSize;
            if (sourceStartIndex < 0 || (source.Length - sourceStartIndex) * elementSize < requiredSize)
                throw new InvalidOperationException("Not enough data in source array.");
            GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try {
                SetDataInternal(face, mipLevel, handle.AddrOfPinnedObject() + sourceStartIndex * elementSize);
            }
            finally {
                handle.Free();
            }
        }

        public virtual void SetData(int face, int mipLevel, nint source) {
            VerifyNotDisposed();
            if (face < 0 || face > 5) throw new ArgumentOutOfRangeException(nameof(face));
            if (mipLevel < 0 || mipLevel >= MipLevelsCount) throw new ArgumentOutOfRangeException(nameof(mipLevel));
            if (source == IntPtr.Zero) throw new ArgumentNullException(nameof(source));
            SetDataInternal(face, mipLevel, source);
        }

        public virtual unsafe void SetDataInternal(int face, int mipLevel, nint source) {
            int mipSize = MathUtils.Max(Size >> mipLevel, 1);
            GLWrapper.BindTexture(TextureTarget.TextureCubeMap, m_texture, false);
            GLWrapper.GL.TexImage2D(
                TextureTarget.TextureCubeMapPositiveX + face,
                mipLevel,
                m_internalFormat,
                (uint)mipSize,
                (uint)mipSize,
                0,
                m_pixelFormat,
                m_pixelType,
                in source
            );
        }

        public void DeleteTexture() {
            if (m_texture != 0) {
                GLWrapper.DeleteTexture(m_texture);
                m_texture = 0;
            }
        }

        public void GenerateMipMaps() {
            GLWrapper.BindTexture(TextureTarget.TextureCubeMap, m_texture, true);
            GLWrapper.GL.GenerateMipmap(TextureTarget.TextureCubeMap);
        }

        public void SetFilterMode(bool useMipmaps) {
            GLWrapper.BindTexture(TextureTarget.TextureCubeMap, m_texture, true);
            if (useMipmaps && MipLevelsCount > 1) {
                GLWrapper.GL.TexParameter(
                    TextureTarget.TextureCubeMap,
                    TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.LinearMipmapLinear
                );
            }
            else {
                GLWrapper.GL.TexParameter(
                    TextureTarget.TextureCubeMap,
                    TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.Linear
                );
            }
            GLWrapper.GL.TexParameter(
                TextureTarget.TextureCubeMap,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear
            );
        }

        public void SetWrapMode(TextureWrapMode wrapS, TextureWrapMode wrapT) {
            GLWrapper.BindTexture(TextureTarget.TextureCubeMap, m_texture, true);
            GLWrapper.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)wrapS);
            GLWrapper.GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)wrapT);
        }

        public override int GetGpuMemoryUsage() {
            int num = 0;
            for (int mip = 0; mip < MipLevelsCount; mip++) {
                int mipSize = MathUtils.Max(Size >> mip, 1);
                num += ColorFormat.GetSize() * mipSize * mipSize * 6;
            }
            return num;
        }

        public override void HandleDeviceLost() {
            DeleteTexture();
        }

        public override void HandleDeviceReset() {
            AllocateTexture();
        }
    }
}
