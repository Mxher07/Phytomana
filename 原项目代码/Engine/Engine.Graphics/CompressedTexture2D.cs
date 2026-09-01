using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#pragma warning disable CS0809 // 过时成员重写未过时成员
namespace Engine.Graphics {
    // Only ASTC support
    public class CompressedTexture2D : Texture2D {
        InternalFormat m_internalFormat;
        int m_blockWidth;
        int m_blockHeight;

        public Point2 BlockSize => new (m_blockWidth, m_blockHeight);

        public CompressedTexture2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat, int blockWidth, int blockHeight) {
            if (!GLWrapper.GL_KHR_texture_compression_astc_ldr) {
                throw new NotSupportedException("ASTC texture is not supported on this device.");
            }
            InitializeTexture2D(width, height, mipLevelsCount, colorFormat);
            m_blockWidth = blockWidth;
            m_blockHeight = blockHeight;
            m_internalFormat = (ColorFormat, footprintWidth: blockWidth, footprintHeight: blockHeight) switch {
                (ColorFormat.LinearLDR, 4, 4) => InternalFormat.CompressedRgbaAstc4x4,
                (ColorFormat.SrgbLDR, 4, 4) => InternalFormat.CompressedSrgb8Alpha8Astc4x4,
                (ColorFormat.LinearLDR, 5, 4) => InternalFormat.CompressedRgbaAstc5x4,
                (ColorFormat.SrgbLDR, 5, 4) => InternalFormat.CompressedSrgb8Alpha8Astc5x4,
                (ColorFormat.LinearLDR, 5, 5) => InternalFormat.CompressedRgbaAstc5x5,
                (ColorFormat.SrgbLDR, 5, 5) => InternalFormat.CompressedSrgb8Alpha8Astc5x5,
                (ColorFormat.LinearLDR, 6, 5) => InternalFormat.CompressedRgbaAstc6x5,
                (ColorFormat.SrgbLDR, 6, 5) => InternalFormat.CompressedSrgb8Alpha8Astc6x5,
                (ColorFormat.LinearLDR, 6, 6) => InternalFormat.CompressedRgbaAstc6x6,
                (ColorFormat.SrgbLDR, 6, 6) => InternalFormat.CompressedSrgb8Alpha8Astc6x6,
                (ColorFormat.LinearLDR, 8, 5) => InternalFormat.CompressedRgbaAstc8x5,
                (ColorFormat.SrgbLDR, 8, 5) => InternalFormat.CompressedSrgb8Alpha8Astc8x5,
                (ColorFormat.LinearLDR, 8, 6) => InternalFormat.CompressedRgbaAstc8x6,
                (ColorFormat.SrgbLDR, 8, 6) => InternalFormat.CompressedSrgb8Alpha8Astc8x6,
                (ColorFormat.LinearLDR, 8, 8) => InternalFormat.CompressedRgbaAstc8x8,
                (ColorFormat.SrgbLDR, 8, 8) => InternalFormat.CompressedSrgb8Alpha8Astc8x8,
                (ColorFormat.LinearLDR, 10, 5) => InternalFormat.CompressedRgbaAstc10x5,
                (ColorFormat.SrgbLDR, 10, 5) => InternalFormat.CompressedSrgb8Alpha8Astc10x5,
                (ColorFormat.LinearLDR, 10, 6) => InternalFormat.CompressedRgbaAstc10x6,
                (ColorFormat.SrgbLDR, 10, 6) => InternalFormat.CompressedSrgb8Alpha8Astc10x6,
                (ColorFormat.LinearLDR, 10, 8) => InternalFormat.CompressedRgbaAstc10x8,
                (ColorFormat.SrgbLDR, 10, 8) => InternalFormat.CompressedSrgb8Alpha8Astc10x8,
                (ColorFormat.LinearLDR, 10, 10) => InternalFormat.CompressedRgbaAstc10x10,
                (ColorFormat.SrgbLDR, 10, 10) => InternalFormat.CompressedSrgb8Alpha8Astc10x10,
                (ColorFormat.LinearLDR, 12, 10) => InternalFormat.CompressedRgbaAstc12x10,
                (ColorFormat.SrgbLDR, 12, 10) => InternalFormat.CompressedSrgb8Alpha8Astc12x10,
                (ColorFormat.LinearLDR, 12, 12) => InternalFormat.CompressedRgbaAstc12x12,
                (ColorFormat.SrgbLDR, 12, 12) => InternalFormat.CompressedSrgb8Alpha8Astc12x12,
                _ => throw new InvalidOperationException("Unsupported surface format.")
            };
            AllocateTexture();
        }

        public override void AllocateTexture() {
            GLWrapper.GL.GenTextures(1, out uint texture);
            m_texture = (int)texture;
            GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, false);
        }

        [Obsolete("Invalid", true)]
        public override void SetData<T>(int mipLevel, T[] source, int sourceStartIndex = 0) where T : struct => throw new InvalidOperationException();

        [Obsolete("Invalid", true)]
        public override void SetData(int mipLevel, nint source) => throw new InvalidOperationException();

        [Obsolete("Invalid", true)]
        public override void SetDataInternal(int mipLevel, nint source) => throw new InvalidOperationException();

        [Obsolete("Invalid", true)]
        public override unsafe void SetDataInternal(int mipLevel, void* source) => throw new InvalidOperationException();

        [Obsolete("Invalid", true)]
        public override void SetData(Image<Rgba32> source) => throw new InvalidOperationException();

        [Obsolete("Invalid", true)]
        public override void SetData(int mipLevel, Image<Rgba32> source) => throw new InvalidOperationException();

        public void SetData(int mipLevel, int imageSize, nint source) {
            VerifyParametersSetData(mipLevel, imageSize, source);
            SetDataInternal(mipLevel, imageSize, source);
        }

        public unsafe void SetData(int mipLevel, int imageSize, void* source) {
            VerifyParametersSetData(mipLevel, imageSize, source);
            SetDataInternal(mipLevel, imageSize, source);
        }

        public void SetData(int mipLevel, Span<byte> source) {
            VerifyParametersSetData(mipLevel, source);
            SetDataInternal(mipLevel, source);
        }

        public void SetDataInternal(int mipLevel, int imageSize, nint source) {
            int width = MathUtils.Max(Width >> mipLevel, 1);
            int height = MathUtils.Max(Height >> mipLevel, 1);
            GLWrapper.GL.CompressedTexImage2D(
                TextureTarget.Texture2D,
                mipLevel,
                m_internalFormat,
                (uint)width,
                (uint)height,
                0,
                (uint)imageSize,
                in source
            );
        }

        public unsafe void SetDataInternal(int mipLevel, int imageSize, void* source) {
            int width = MathUtils.Max(Width >> mipLevel, 1);
            int height = MathUtils.Max(Height >> mipLevel, 1);
            GLWrapper.GL.CompressedTexImage2D(
                TextureTarget.Texture2D,
                mipLevel,
                m_internalFormat,
                (uint)width,
                (uint)height,
                0,
                (uint)imageSize,
                source
            );
        }

        public unsafe void SetDataInternal(int mipLevel, Span<byte> source) {
            int width = MathUtils.Max(Width >> mipLevel, 1);
            int height = MathUtils.Max(Height >> mipLevel, 1);
            fixed (byte* p = &MemoryMarshal.GetReference(source)) {
                uint imageSize = (uint)source.Length;
                GLWrapper.GL.CompressedTexImage2D(
                    TextureTarget.Texture2D,
                    mipLevel,
                    m_internalFormat,
                    (uint)width,
                    (uint)height,
                    0,
                    imageSize,
                    p
                );
            }
        }

        public override int GetGpuMemoryUsage() {
            int result = 0;
            int width = Width;
            int height = Height;
            while (width > 0
                || height > 0) {
                int blocksX = (width + m_blockWidth - 1) / m_blockWidth;
                int blocksY = (height + m_blockHeight - 1) / m_blockHeight;
                result += blocksX * blocksY * 16;
                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);
                if (width == 1
                    && height == 1) {
                    break;
                }
            }
            return result;
        }

        public new static CompressedTexture2D Load(Stream stream, bool linear = true, int mipLevelsCount = 1) {
            if (stream.Length < 16) {
                throw new Exception("Invalid ASTC stream.");
            }
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            if (stream.Read(buffer) != stream.Length) {
                throw new Exception($"Failed to read stream. The stream length is {stream.Length} bytes, but the stream was only read {stream.Position} bytes.");
            }
            if (buffer[0] != 0x13 || buffer[1] != 0xAB ||
                buffer[2] != 0xA1 || buffer[3] != 0x5C) {
                throw new Exception("Invalid ASTC stream.");
            }
            byte blockWidth = buffer[4];
            byte blockHeight = buffer[5];
            int width = buffer[7] | (buffer[8] << 8) | (buffer[9] << 16);
            int height = buffer[10] | (buffer[11] << 8) | (buffer[12] << 16);
            CompressedTexture2D texture2D = new CompressedTexture2D(
                width,
                height,
                mipLevelsCount,
                linear ? ColorFormat.LinearLDR : ColorFormat.SrgbLDR,
                blockWidth,
                blockHeight
            );
            texture2D.SetData(0, buffer.AsSpan().Slice(16));
            if (mipLevelsCount > 1) {
                //下面代码对于 ASTC 来说不可用，必须手动生成 Mipmap 然后 SetData
                //GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, false);
                //GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            return texture2D;
        }

        public static bool GetParameters(Stream stream, out int width, out int height, out int blockWidth, out int blockHeight) {
            if (stream.Length < 16) {
                width = 0;
                height = 0;
                blockWidth = 0;
                blockHeight = 0;
                return false;
            }
            byte[] buffer = new byte[13];
            stream.Position = 0;
            stream.ReadExactly(buffer, 0, 12);
            if (buffer[0] != 0x13 || buffer[1] != 0xAB ||
                buffer[2] != 0xA1 || buffer[3] != 0x5C) {
                width = 0;
                height = 0;
                blockWidth = 0;
                blockHeight = 0;
                return false;
            }
            blockWidth = buffer[4];
            blockHeight = buffer[5];
            width = buffer[7] | (buffer[8] << 8) | (buffer[9] << 16);
            height = buffer[10] | (buffer[11] << 8) | (buffer[12] << 16);
            return true;
        }

        public override void InitializeTexture2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat) {
            if (width < 1) {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            if (height < 1) {
                throw new ArgumentOutOfRangeException(nameof(height));
            }
            if (mipLevelsCount < 1) {
                throw new ArgumentOutOfRangeException(nameof(mipLevelsCount));
            }
            if (colorFormat != ColorFormat.LinearLDR && colorFormat != ColorFormat.SrgbLDR) {
                throw new ArgumentException(nameof(colorFormat));
            }
            Width = width;
            Height = height;
            ColorFormat = colorFormat;
            if (mipLevelsCount > 1) {
                int num = 0;
                for (int num2 = MathUtils.Max(width, height); num2 >= 1; num2 /= 2) {
                    num++;
                }
                MipLevelsCount = MathUtils.Min(num, mipLevelsCount);
            }
            else {
                MipLevelsCount = 1;
            }
        }

        public void VerifyParametersSetData(int mipLevel, int imageSize, nint source) {
            VerifyParametersSetData(mipLevel, source);
            if (imageSize <= 0) {
                throw new ArgumentNullException(nameof(imageSize));
            }
        }

        public unsafe void VerifyParametersSetData(int mipLevel, int imageSize, void* source) {
            VerifyNotDisposed();
            if (mipLevel < 0
                || mipLevel >= MipLevelsCount) {
                throw new ArgumentOutOfRangeException(nameof(mipLevel));
            }
            if (imageSize <= 0) {
                throw new ArgumentNullException(nameof(imageSize));
            }
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }
        }

        public void VerifyParametersSetData(int mipLevel, Span<byte> source) {
            VerifyNotDisposed();
            if (mipLevel < 0
                || mipLevel >= MipLevelsCount) {
                throw new ArgumentOutOfRangeException(nameof(mipLevel));
            }
            if (source.IsEmpty) {
                throw new ArgumentNullException(nameof(source));
            }
        }
    }
}