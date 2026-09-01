using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;

namespace Engine.Media {
    public static class Bmp {
        public enum Format {
            RGBA8 = 0,
            RGB8 = 1,
            Pixel1 = 2,
            Pixel16 = 3,
            Pixel2 = 4,
            Pixel4 = 5,
            Pixel8 = 6
        }

        public struct BmpInfo {
            public int Width;

            public int Height;

            public Format Format;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BitmapHeader {
            public byte Type1;

            public byte Type2;

            public int Size;

            public short Reserved1;

            public short Reserved2;

            public int OffBits;

            public int Size2;

            public int Width;

            public int Height;

            public short Planes;

            public short BitCount;

            public int Compression;

            public int SizeImage;

            public int XPelsPerMeter;

            public int YPelsPerMeter;

            public int ClrUsed;

            public int ClrImportant;
        }

        public static bool IsBmpStream(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            return SixLabors.ImageSharp.Image.DetectFormat(stream).Name == "BMP";
        }

        public static BmpInfo GetInfo(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            ImageInfo info = SixLabors.ImageSharp.Image.Identify(stream);
            if (info.Metadata.DecodedImageFormat.Name != "BMP") {
                throw new FormatException($"Image format({info.Metadata.DecodedImageFormat.Name}) is not Bmp");
            }
            BmpInfo result = default;
            result.Width = info.Width;
            result.Height = info.Height;
            return !ImageSharpBitsPerPixel2EngineBmpFormat.TryGetValue(info.Metadata.GetBmpMetadata().BitsPerPixel, out result.Format)
                ? throw new InvalidOperationException("Unsupported BMP pixel format.")
                : result;
        }

        public static Image Load(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            string formatName = SixLabors.ImageSharp.Image.DetectFormat(stream).Name;
            return formatName != "BMP" ? throw new FormatException($"Image format({formatName}) is not BMP") : Image.Load(stream);
        }

        public static void Save(Image image, Stream stream, Format format, bool sync = false) {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(stream);
            if (!EngineBmpFormat2ImageSharpBitsPerPixel.TryGetValue(format, out BmpBitsPerPixel bitsPerPixel)) {
                throw new InvalidOperationException("Unsupported BMP pixel format.");
            }
            BmpEncoder encoder = new() { BitsPerPixel = bitsPerPixel };
            if (sync) {
                image.m_trueImage.SaveAsBmp(stream, encoder);
            }
            else {
                image.m_trueImage.SaveAsBmpAsync(stream, encoder);
            }
        }

        public static BitmapHeader ReadHeader(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            if (!BitConverter.IsLittleEndian) {
                throw new InvalidOperationException("Unsupported system endianness.");
            }
            byte[] array = new byte[54];
            if (stream.Read(array, 0, array.Length) != array.Length) {
                throw new InvalidOperationException("Invalid BMP header.");
            }
            BitmapHeader result = Utilities.ArrayToStructure<BitmapHeader>(array);
            return result.Type1 != 66 || result.Type2 != 77 ? throw new InvalidOperationException("Invalid BMP header.") :
                result.Compression != 0 ? throw new InvalidOperationException("Unsupported BMP compression.") : result;
        }

        public static readonly Dictionary<Format, BmpBitsPerPixel> EngineBmpFormat2ImageSharpBitsPerPixel = new() {
            { Format.RGBA8, BmpBitsPerPixel.Pixel32 },
            { Format.RGB8, BmpBitsPerPixel.Pixel24 },
            { Format.Pixel1, BmpBitsPerPixel.Pixel1 },
            { Format.Pixel16, BmpBitsPerPixel.Pixel16 },
            { Format.Pixel2, BmpBitsPerPixel.Pixel2 },
            { Format.Pixel4, BmpBitsPerPixel.Pixel4 },
            { Format.Pixel8, BmpBitsPerPixel.Pixel8 }
        };

        public static readonly Dictionary<BmpBitsPerPixel, Format> ImageSharpBitsPerPixel2EngineBmpFormat = new() {
            { BmpBitsPerPixel.Pixel32, Format.RGBA8 },
            { BmpBitsPerPixel.Pixel24, Format.RGB8 },
            { BmpBitsPerPixel.Pixel1, Format.Pixel1 },
            { BmpBitsPerPixel.Pixel16, Format.Pixel16 },
            { BmpBitsPerPixel.Pixel2, Format.Pixel2 },
            { BmpBitsPerPixel.Pixel4, Format.Pixel4 },
            { BmpBitsPerPixel.Pixel8, Format.Pixel8 }
        };
    }
}