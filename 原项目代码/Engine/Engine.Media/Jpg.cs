using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Engine.Media {
    public static class Jpg {
        public static bool IsJpgStream(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            return SixLabors.ImageSharp.Image.DetectFormat(stream).Name == "JPEG";
        }

        public static Image Load(Stream stream) {
            ArgumentNullException.ThrowIfNull(stream);
            string formatName = SixLabors.ImageSharp.Image.DetectFormat(stream).Name;
            return formatName != "JPEG" ? throw new FormatException($"Image format({formatName}) is not Jpeg") : Image.Load(stream);
        }

        public static void Save(Image image, Stream stream, int quality, bool sync = false) {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(stream);
            if (quality < 0
                || quality > 100) {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }
            JpegEncoder encoder = new() { Quality = quality, ColorType = JpegEncodingColor.YCbCrRatio420 };
            if (sync) {
                image.m_trueImage.SaveAsJpeg(stream, encoder);
            }
            else {
                image.m_trueImage.SaveAsJpegAsync(stream, encoder);
            }
        }
    }
}