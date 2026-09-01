using Silk.NET.OpenGLES;
using System.Runtime.InteropServices;
using Engine.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = Engine.Media.Image;
using Half = System.Half;

namespace Engine.Graphics {
    public class Texture2D : GraphicsResource {
        int m_width;
        int m_height;
        ColorFormat m_colorFormat;
        int m_mipLevelsCount;
        object m_tag;
        string m_debugName;
        SamplerState m_samplerState;
        public int m_texture;
        public PixelFormat m_pixelFormat;
        public PixelType m_pixelType;
        InternalFormat m_internalFormat;

        public IntPtr NativeHandle => m_texture;

        /// <summary>
        /// 是否为 sRGB 颜色空间
        /// </summary>
        public bool IsSrgb => ColorFormat == ColorFormat.Rgba8888Srgb || ColorFormat == ColorFormat.SrgbLDR;

        /// <summary>
        /// 是否为 HDR 格式（浮点纹理）
        /// </summary>
        public bool IsHDR => ColorFormat
            is ColorFormat.R32f
            or ColorFormat.RG32f
            or ColorFormat.RGBA32f
            or ColorFormat.Rgba16f;

        public string DebugName {
            get {
                return m_debugName;
            }
            set {
                m_debugName = value;
            }
        }

        public int Width {
            get => m_width;
            set => m_width = value;
        }

        public int Height {
            get => m_height;
            set => m_height = value;
        }

        public ColorFormat ColorFormat {
            get => m_colorFormat;
            set => m_colorFormat = value;
        }

        public int MipLevelsCount {
            get => m_mipLevelsCount;
            set => m_mipLevelsCount = value;
        }

        public object Tag {
            get => m_tag;
            set => m_tag = value;
        }

        public SamplerState SamplerState {
            get => m_samplerState;
            set => m_samplerState = value;
        }

        public Texture2D() { }

        public Texture2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat) {
            InitializeTexture2D(width, height, mipLevelsCount, colorFormat);
            switch (ColorFormat) {
                // 未压缩格式
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
                case ColorFormat.Rgb565:
                    m_pixelFormat = PixelFormat.Rgb;
                    m_pixelType = PixelType.UnsignedShort565;
                    m_internalFormat = InternalFormat.Rgb565;
                    break;
                case ColorFormat.Rgba5551:
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.UnsignedShort5551;
                    m_internalFormat = InternalFormat.Rgb5A1;
                    break;
                case ColorFormat.R8:
                    // GL_LUMINANCE is deprecated in ES 3.0+ but kept for backward compatibility
                    // with existing shaders that expect luminance behavior (r=g=b=value)
                    m_pixelFormat = (PixelFormat)6409; // GL_LUMINANCE
                    m_pixelType = PixelType.UnsignedByte;
                    // Note: internalFormat uses same value as pixelFormat for luminance
                    m_internalFormat = (InternalFormat)6409; // GL_LUMINANCE
                    break;
                // HDR 未压缩格式
                case ColorFormat.RGBA32f:
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.Float;
                    m_internalFormat = InternalFormat.Rgba32f;
                    break;
                case ColorFormat.Rgba16f:
                    m_pixelFormat = PixelFormat.Rgba;
                    m_pixelType = PixelType.HalfFloat;
                    m_internalFormat = InternalFormat.Rgba16f;
                    break;
                // R32f 和 RG32f 使用数值常量（Silk.NET 可能未定义）
                case ColorFormat.R32f:
                    m_pixelFormat = PixelFormat.Red;
                    m_pixelType = PixelType.Float;
                    m_internalFormat = InternalFormat.R32f; // GL_R32F
                    break;
                case ColorFormat.RG32f:
                    m_pixelFormat = PixelFormat.RG; // GL_RG
                    m_pixelType = PixelType.Float;
                    m_internalFormat = InternalFormat.RG32f; // GL_RG32F
                    break;
                default:
                    throw new InvalidOperationException("Unsupported surface format.");
            }
            AllocateTexture();
        }

        public override void Dispose() {
            base.Dispose();
            DeleteTexture();
        }

        public virtual void SetData<T>(int mipLevel, T[] source, int sourceStartIndex = 0) where T : unmanaged {
            VerifyParametersSetData(mipLevel, source, sourceStartIndex);
            GCHandle gCHandle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try {
                int num = Utilities.SizeOf<T>();
                SetDataInternal(mipLevel, gCHandle.AddrOfPinnedObject() + sourceStartIndex * num);
            }
            finally {
                gCHandle.Free();
            }
        }

        public virtual void SetData(int mipLevel, nint source) {
            VerifyParametersSetData(mipLevel, source);
            SetDataInternal(mipLevel, source);
        }

        public virtual void SetDataInternal(int mipLevel, nint source) {
            int width = MathUtils.Max(Width >> mipLevel, 1);
            int height = MathUtils.Max(Height >> mipLevel, 1);
            GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, false);
            GLWrapper.GL.TexImage2D(
                TextureTarget.Texture2D,
                mipLevel,
                m_internalFormat,
                (uint)width,
                (uint)height,
                0,
                m_pixelFormat,
                m_pixelType,
                in source
            );
        }

        public virtual unsafe void SetDataInternal(int mipLevel, void* source) {
            int width = MathUtils.Max(Width >> mipLevel, 1);
            int height = MathUtils.Max(Height >> mipLevel, 1);
            GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, false);
            GLWrapper.GL.TexImage2D(
                TextureTarget.Texture2D,
                mipLevel,
                m_internalFormat,
                (uint)width,
                (uint)height,
                0,
                m_pixelFormat,
                m_pixelType,
                source
            );
        }

        public virtual void SetData(Image<Rgba32> source) {
            SetData(0, source);
        }

        public virtual unsafe void SetData(int mipLevel, Image<Rgba32> source) {
            VerifyParametersSetData(source);
            source.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> memory);
            SetDataInternal(mipLevel, memory.Pin().Pointer);
        }

        public static void Swap(Texture2D texture1, Texture2D texture2) {
            VerifyParametersSwap(texture1, texture2);
            SwapTexture2D(texture1, texture2);
            Utilities.Swap(ref texture1.m_texture, ref texture2.m_texture);
            Utilities.Swap(ref texture1.m_pixelFormat, ref texture2.m_pixelFormat);
            Utilities.Swap(ref texture1.m_pixelType, ref texture2.m_pixelType);
            Utilities.Swap(ref texture1.m_internalFormat, ref texture2.m_internalFormat);
            Utilities.Swap(ref texture1.m_debugName, ref texture2.m_debugName);
        }

        public static void SwapTexture2D(Texture2D texture1, Texture2D texture2) {
            Utilities.Swap(ref texture1.m_width, ref texture2.m_width);
            Utilities.Swap(ref texture1.m_height, ref texture2.m_height);
            Utilities.Swap(ref texture1.m_colorFormat, ref texture2.m_colorFormat);
            Utilities.Swap(ref texture1.m_mipLevelsCount, ref texture2.m_mipLevelsCount);
            Utilities.Swap(ref texture1.m_tag, ref texture2.m_tag);
        }

        public override void HandleDeviceLost() {
            DeleteTexture();
        }

        public override void HandleDeviceReset() {
            AllocateTexture();
        }

        public virtual void AllocateTexture() {
            unsafe {
                GLWrapper.GL.GenTextures(1, out uint texture);
                m_texture = (int)texture;
                GLWrapper.BindTexture(TextureTarget.Texture2D, m_texture, false);
                for (int i = 0; i < MipLevelsCount; i++) {
                    int width = MathUtils.Max(Width >> i, 1);
                    int height = MathUtils.Max(Height >> i, 1);
                    GLWrapper.GL.TexImage2D(
                        TextureTarget.Texture2D,
                        i,
                        m_internalFormat,
                        (uint)width,
                        (uint)height,
                        0,
                        m_pixelFormat,
                        m_pixelType,
                        null
                    );
                }
            }
        }

        public void DeleteTexture() {
            if (m_texture != 0) {
                GLWrapper.DeleteTexture(m_texture);
                m_texture = 0;
            }
        }

        public override int GetGpuMemoryUsage() {
            int num = 0;
            for (int i = 0; i < MipLevelsCount; i++) {
                int num2 = MathUtils.Max(Width >> i, 1);
                int num3 = MathUtils.Max(Height >> i, 1);
                num += ColorFormat.GetSize() * num2 * num3;
            }
            return num;
        }

        public static Texture2D Load(LegacyImage image, int mipLevelsCount = 1) {
            Texture2D texture2D = new(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888);
            if (mipLevelsCount > 1) {
                LegacyImage[] array = LegacyImage.GenerateMipmaps(image, mipLevelsCount).ToArray();
                for (int i = 0; i < array.Length; i++) {
                    texture2D.SetData(i, array[i].Pixels);
                }
            }
            else {
                texture2D.SetData(0, image.Pixels);
            }
            texture2D.Tag = image;
            return texture2D;
        }

        public static Texture2D Load(Image image, int mipLevelsCount = 1) {
            Texture2D texture2D = new(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888);
            texture2D.SetData(image.m_trueImage);
            if (mipLevelsCount > 1) {
                GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            texture2D.Tag = image;
            return texture2D;
        }

        public static Texture2D Load(Image<Rgba32> image, int mipLevelsCount = 1) {
            Texture2D texture2D = new(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888);
            texture2D.SetData(image);
            if (mipLevelsCount > 1) {
                GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            texture2D.Tag = new Image(image);
            return texture2D;
        }

        public static Texture2D Load(Stream stream, bool premultiplyAlpha = false, int mipLevelsCount = 1) {
            Image image = Image.Load(stream);
            if (premultiplyAlpha) {
                Image.PremultiplyAlpha(image);
            }
            return Load(image, mipLevelsCount);
        }

        public static Texture2D Load(string fileName, bool premultiplyAlpha = false, int mipLevelsCount = 1) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream, premultiplyAlpha, mipLevelsCount);
            }
        }

        public static Texture2D Load(Color color, int width, int height) {
            Texture2D texture2D = new(width, height, 1, ColorFormat.Rgba8888);
            Color[] array = new Color[width * height];
            for (int i = 0; i < array.Length; i++) {
                array[i] = color;
            }
            texture2D.SetData(0, array);
            return texture2D;
        }

        /// <summary>
        /// 从图像创建 sRGB 纹理（用于 BaseColor、Emissive 等颜色纹理）
        /// </summary>
        public static Texture2D LoadSrgb(Image<Rgba32> image, int mipLevelsCount = 1) {
            Texture2D texture2D = new(image.Width, image.Height, mipLevelsCount, ColorFormat.Rgba8888Srgb);
            texture2D.SetData(image);
            if (mipLevelsCount > 1) {
                GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            texture2D.Tag = new Image(image);
            return texture2D;
        }

        /// <summary>
        /// 从流创建 sRGB 纹理
        /// </summary>
        public static Texture2D LoadSrgb(Stream stream, int mipLevelsCount = 1) {
            Image<Rgba32> image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            return LoadSrgb(image, mipLevelsCount);
        }

        /// <summary>
        /// 从文件创建 sRGB 纹理
        /// </summary>
        public static Texture2D LoadSrgb(string fileName, int mipLevelsCount = 1) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return LoadSrgb(stream, mipLevelsCount);
            }
        }

        /// <summary>
        /// 从 HDR float 数据创建纹理
        /// </summary>
        /// <param name="data">像素数据（R、RG 或 RGBA 通道，取决于 format）</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="format">HDR 格式（R32f、RG32f 或 RGBA32f）</param>
        /// <param name="mipLevelsCount">Mipmap 级数</param>
        public static unsafe Texture2D LoadHDR(Span<float> data, int width, int height, ColorFormat format, int mipLevelsCount = 1) {
            if (format is not (ColorFormat.R32f or ColorFormat.RG32f or ColorFormat.RGBA32f)) {
                throw new ArgumentException("Format must be R32f, RG32f, or RGBA32f for float data. Use LoadHDR(Span<Half>) for Rgba16f.");
            }
            // 验证数据大小
            int channels = format.GetSize() / sizeof(float);
            int expectedSize = width * height * channels;
            if (data.Length < expectedSize) {
                throw new ArgumentException($"Data length {data.Length} is insufficient for {width}x{height} {format} texture (expected {expectedSize}).");
            }
            Texture2D texture2D = new(width, height, mipLevelsCount, format);
            fixed (float* ptr = data) {
                texture2D.SetDataInternal(0, ptr);
            }
            if (mipLevelsCount > 1) {
                GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            return texture2D;
        }

        /// <summary>
        /// 从 Half float 数据创建 RGBA16F 纹理
        /// </summary>
        public static unsafe Texture2D LoadHDR(Span<Half> data, int width, int height, int mipLevelsCount = 1) {
            // 验证数据大小
            const int channels = 4; // RGBA16f
            int expectedSize = width * height * channels;
            if (data.Length < expectedSize) {
                throw new ArgumentException($"Data length {data.Length} is insufficient for {width}x{height} Rgba16f texture (expected {expectedSize}).");
            }
            Texture2D texture2D = new(width, height, mipLevelsCount, ColorFormat.Rgba16f);
            fixed (Half* ptr = data) {
                texture2D.SetDataInternal(0, ptr);
            }
            if (mipLevelsCount > 1) {
                GLWrapper.BindTexture(TextureTarget.Texture2D, texture2D.m_texture, false);
                GLWrapper.GL.GenerateMipmap(TextureTarget.Texture2D);
            }
            return texture2D;
        }

        public virtual void InitializeTexture2D(int width, int height, int mipLevelsCount, ColorFormat colorFormat) {
            if (width < 1) {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            if (height < 1) {
                throw new ArgumentOutOfRangeException(nameof(height));
            }
            if (mipLevelsCount < 1) {
                throw new ArgumentOutOfRangeException(nameof(mipLevelsCount));
            }
            if (colorFormat == ColorFormat.LinearLDR || colorFormat == ColorFormat.SrgbLDR) {
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

        public virtual void VerifyParametersSetData<T>(int mipLevel, T[] source, int sourceStartIndex = 0) where T : unmanaged {
            VerifyNotDisposed();
            int num = Utilities.SizeOf<T>();
            int size = ColorFormat.GetSize();
            int num2 = MathUtils.Max(Width >> mipLevel, 1);
            int num3 = MathUtils.Max(Height >> mipLevel, 1);
            int num4 = size * num2 * num3;
            ArgumentNullException.ThrowIfNull(source);
            if (mipLevel < 0
                || mipLevel >= MipLevelsCount) {
                throw new ArgumentOutOfRangeException(nameof(mipLevel));
            }
            if (num > size) {
                throw new ArgumentException("Source array element size is larger than pixel size.");
            }
            if (size % num != 0) {
                throw new ArgumentException("Pixel size is not an integer multiple of source array element size.");
            }
            if (sourceStartIndex < 0
                || (source.Length - sourceStartIndex) * num < num4) {
                throw new InvalidOperationException("Not enough data in source array.");
            }
        }

        public virtual void VerifyParametersSetData(int mipLevel, nint source) {
            VerifyNotDisposed();
            if (source == IntPtr.Zero) {
                throw new ArgumentNullException(nameof(source));
            }
            if (mipLevel < 0
                || mipLevel >= MipLevelsCount) {
                throw new ArgumentOutOfRangeException(nameof(mipLevel));
            }
        }

        static void VerifyParametersSwap(Texture2D texture1, Texture2D texture2) {
            if (texture1 == null) {
                throw new ArgumentNullException(nameof(texture1));
            }
            if (texture2 == null) {
                throw new ArgumentNullException(nameof(texture2));
            }
            if (texture1.GetType() != typeof(Texture2D)) {
                throw new ArgumentException("texture1");
            }
            if (texture2.GetType() != typeof(Texture2D)) {
                throw new ArgumentException("texture2");
            }
            texture1.VerifyNotDisposed();
            texture2.VerifyNotDisposed();
        }

        void VerifyParametersSetData(Image<Rgba32> source) {
            VerifyNotDisposed();
            ArgumentNullException.ThrowIfNull(source);
        }
    }
}