using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Media {
    public class Image {
        public static IImageFormatConfigurationModule[] ImageSharpModules = [
            new BmpConfigurationModule(),
            new GifConfigurationModule(),
            new JpegConfigurationModule(),
            new PbmConfigurationModule(),
            new PngConfigurationModule(),
            new QoiConfigurationModule(),
            new TgaConfigurationModule(),
            new TiffConfigurationModule(),
            new WebpConfigurationModule()
        ];

        public static Configuration DefaultImageSharpConfiguration = new(ImageSharpModules) { PreferContiguousImageBuffers = true };
        public static DecoderOptions DefaultImageSharpDecoderOptions = new() { Configuration = DefaultImageSharpConfiguration };
        public static readonly JpegEncoder DefaultJpegEncoder = new() { Quality = 95, ColorType = JpegEncodingColor.YCbCrRatio420 };
        public static readonly GifEncoder DefaultGifEncoder = new() { ColorTableMode = GifColorTableMode.Local };

        public int Width => m_trueImage.Width;

        public int Height => m_trueImage.Height;

        public Color[] m_pixels;
        public bool m_shouldUpdatePixelsCache = true;

        /// <summary>
        /// 获取图像的像素数据，只读，写入无效。
        /// </summary>
        public Color[] Pixels {
            get {
                if (m_pixels == null || m_shouldUpdatePixelsCache) {
                    m_pixels = new Color[Width * Height];
                    ProcessPixelRows(
                        accessor => {
                            Span<Color> pixelsSpan = m_pixels.AsSpan();
                            for (int y = 0; y < accessor.Height; y++) {
                                MemoryMarshal.Cast<Rgba32, Color>(accessor.GetRowSpan(y)).CopyTo(pixelsSpan.Slice(y * Width, Width));
                            }
                        },
                        false
                    );
                }
                m_shouldUpdatePixelsCache = false;
                return m_pixels;
            }
        }

        public readonly Image<Rgba32> m_trueImage;
        public bool m_isDisposed;

        public Image() {}

        public Image(Image image) {
            ArgumentNullException.ThrowIfNull(image);
            m_trueImage = image.m_trueImage.Clone();
        }

        public Image(Image<Rgba32> image) {
            ArgumentNullException.ThrowIfNull(image);
            m_trueImage = image;
        }

        public Image(LegacyImage image) {
            ArgumentNullException.ThrowIfNull(image);
            m_trueImage = new Image<Rgba32>(DefaultImageSharpConfiguration, image.Width, image.Height);
            ProcessPixelRows(accessor => {
                    Span<Color> pixels = image.Pixels.AsSpan();
                    for (int y = 0; y < accessor.Height; y++) {
                        MemoryMarshal.Cast<Color, Rgba32>(pixels.Slice(y * image.Width, image.Height)).CopyTo(accessor.GetRowSpan(y));
                    }
                }
            );
        }

        public Image(int width, int height) {
            if (width < 0) {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            if (height < 0) {
                throw new ArgumentOutOfRangeException(nameof(height));
            }
            m_trueImage = new Image<Rgba32>(DefaultImageSharpConfiguration, width, height);
        }

        public Rgba32 GetPixelFast(int x, int y) => m_trueImage[x, y];

        public Color GetPixel(int x, int y) => x < 0 || x >= Width ? throw new ArgumentOutOfRangeException(nameof(x)) :
            y < 0 || y >= Height ? throw new ArgumentOutOfRangeException(nameof(y)) : new Color(m_trueImage[x, y].PackedValue);

        public Rgba32 SetPixelFast(int x, int y, Rgba32 color) => m_trueImage[x, y] = color;

        public void SetPixel(int x, int y, Color color) {
            if (x < 0
                || x >= Width) {
                throw new ArgumentOutOfRangeException(nameof(x));
            }
            if (y < 0
                || y >= Height) {
                throw new ArgumentOutOfRangeException(nameof(y));
            }
            m_trueImage[x, y] = new Rgba32(color.PackedValue);
            m_shouldUpdatePixelsCache = true;
        }

        public static void PremultiplyAlpha(Image image) => image.ProcessPixels(pixel => pixel.PremultiplyAlpha());

        public static ImageFileFormat DetermineFileFormat(string extension) =>
            Name2EngineImageFormat.TryGetValue(extension.Substring(1).ToLower(), out ImageFileFormat format)
                ? format
                : throw new InvalidOperationException("Unsupported image file format.");

        public static ImageFileFormat DetermineFileFormat(Stream stream) =>
            Name2EngineImageFormat.TryGetValue(SixLabors.ImageSharp.Image.DetectFormat(stream).Name.ToLower(), out ImageFileFormat format)
                ? format
                : throw new InvalidOperationException("Unsupported image file format.");

        public static Image Load(Stream stream, ImageFileFormat format) =>
            Name2EngineImageFormat.TryGetValue(SixLabors.ImageSharp.Image.DetectFormat(stream).Name.ToLower(), out ImageFileFormat IdentifiedFormat)
            && IdentifiedFormat == format
                ? Load(stream)
                : throw new FormatException($"Image format({IdentifiedFormat}) is not ${format}");

        public static Image Load(string fileName, ImageFileFormat format) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream, format);
            }
        }

        public static Image Load(Stream stream) => new(SixLabors.ImageSharp.Image.Load<Rgba32>(DefaultImageSharpDecoderOptions, stream));

        public static Image Load(string fileName) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Read)) {
                return Load(stream);
            }
        }

        public static void Save(Image image, Stream stream, ImageFileFormat format, bool saveAlpha, bool sync = false) {
            switch (format) {
                case ImageFileFormat.Bmp: {
                    BmpEncoder encoder = new() { BitsPerPixel = saveAlpha ? BmpBitsPerPixel.Pixel32 : BmpBitsPerPixel.Pixel24 };
                    if (sync) {
                        image.m_trueImage.SaveAsBmp(stream, encoder);
                    }
                    else {
                        image.m_trueImage.SaveAsBmpAsync(stream, encoder);
                    }
                    break;
                }
                case ImageFileFormat.Png: {
                    PngEncoder encoder = new() {
                        ColorType = saveAlpha ? PngColorType.RgbWithAlpha : PngColorType.Rgb, TransparentColorMode = PngTransparentColorMode.Clear
                    };
                    if (sync) {
                        image.m_trueImage.SaveAsPng(stream, encoder);
                    }
                    else {
                        image.m_trueImage.SaveAsPngAsync(stream, encoder);
                    }
                    break;
                }
                case ImageFileFormat.Jpg: {
                    if (sync) {
                        image.m_trueImage.SaveAsJpeg(stream, DefaultJpegEncoder);
                    }
                    else {
                        image.m_trueImage.SaveAsJpegAsync(stream, DefaultJpegEncoder);
                    }
                    break;
                }
                case ImageFileFormat.Gif:
                    if (sync) {
                        image.m_trueImage.SaveAsGif(stream, DefaultGifEncoder);
                    }
                    else {
                        image.m_trueImage.SaveAsGifAsync(stream, DefaultGifEncoder);
                    }
                    break;
                case ImageFileFormat.Pbm:
                    if (sync) {
                        image.m_trueImage.SaveAsPbm(stream);
                    }
                    else {
                        image.m_trueImage.SaveAsPbmAsync(stream);
                    }
                    break;
                case ImageFileFormat.Qoi: {
                    QoiEncoder encoder = new() {
                        ColorSpace = QoiColorSpace.SrgbWithLinearAlpha, Channels = saveAlpha ? QoiChannels.Rgba : QoiChannels.Rgb
                    };
                    if (sync) {
                        image.m_trueImage.SaveAsQoi(stream, encoder);
                    }
                    else {
                        image.m_trueImage.SaveAsQoiAsync(stream, encoder);
                    }
                    break;
                }
                case ImageFileFormat.Tiff: {
                    TiffEncoder encoder = new() { BitsPerPixel = saveAlpha ? TiffBitsPerPixel.Bit32 : TiffBitsPerPixel.Bit24 };
                    if (sync) {
                        image.m_trueImage.SaveAsTiff(stream, encoder);
                    }
                    else {
                        image.m_trueImage.SaveAsTiffAsync(stream, encoder);
                    }
                    break;
                }
                case ImageFileFormat.Tga: {
                    TgaEncoder encoder = new() {
                        BitsPerPixel = saveAlpha ? TgaBitsPerPixel.Pixel32 : TgaBitsPerPixel.Pixel24, Compression = TgaCompression.RunLength
                    };
                    if (sync) {
                        image.m_trueImage.SaveAsTga(stream, encoder);
                    }
                    else {
                        image.m_trueImage.SaveAsTgaAsync(stream, encoder);
                    }
                    break;
                }
                case ImageFileFormat.WebP: {
                    WebpEncoder encoder = new() {
                        TransparentColorMode = saveAlpha ? WebpTransparentColorMode.Preserve : WebpTransparentColorMode.Clear,
                        FileFormat = WebpFileFormatType.Lossless
                    };
                    if (sync) {
                        image.m_trueImage.SaveAsWebp(stream, encoder);
                    }
                    else {
                        image.m_trueImage.SaveAsWebpAsync(stream, encoder);
                    }
                    break;
                }
                default: throw new InvalidOperationException("Unsupported image file format.");
            }
        }

        public static void Save(Image image, string fileName, ImageFileFormat format, bool saveAlpha) {
            using (Stream stream = Storage.OpenFile(fileName, OpenFileMode.Create)) {
                Save(image, stream, format, saveAlpha);
            }
        }

        public void ProcessPixelRows(PixelAccessorAction<Rgba32> accessorAction, bool shouldUpdatePixelsCache = true) {
            m_trueImage.ProcessPixelRows(accessorAction);
            if (shouldUpdatePixelsCache) {
                m_shouldUpdatePixelsCache = true;
            }
        }

        public void ProcessPixels(Func<Rgba32, Rgba32> pixelFunc, bool shouldUpdatePixelsCache = true) {
            ProcessPixelRows(
                accessor => {
                    for (int y = 0; y < accessor.Height; y++) {
                        foreach (ref Rgba32 pixel in accessor.GetRowSpan(y)) {
                            pixel = pixelFunc(pixel);
                        }
                    }
                },
                shouldUpdatePixelsCache
            );
        }

        public static readonly Dictionary<string, ImageFileFormat> Name2EngineImageFormat = new() {
            { "bmp", ImageFileFormat.Bmp },
            { "png", ImageFileFormat.Png },
            { "jpg", ImageFileFormat.Jpg },
            { "jpeg", ImageFileFormat.Jpg },
            { "gif", ImageFileFormat.Gif },
            { "pbm", ImageFileFormat.Pbm },
            { "qoi", ImageFileFormat.Qoi },
            { "tiff", ImageFileFormat.Tiff },
            { "tga", ImageFileFormat.Tga },
            { "webp", ImageFileFormat.WebP }
        };

        public void Dispose() {
            if (!m_isDisposed) {
                m_isDisposed = true;
                m_pixels = null;
                m_trueImage.Dispose();
            }
        }

        public static implicit operator Image(Image<Rgba32> image) => new(image);

        public static implicit operator Image<Rgba32>(Image image) => image.m_trueImage;
    }
}