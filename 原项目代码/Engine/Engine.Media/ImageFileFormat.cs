namespace Engine.Media {
    public enum ImageFileFormat {
        RawRgba = 0, //Not Support
        Bmp = 1, //无压缩无损格式
        Png = 2, //有压缩无损格式
        Jpg = 3, //有压缩有损格式
        Gif = 4,
        Pbm = 5, //纯黑白格式
        Qoi = 6,
        Tiff = 7,
        Tga = 8,
        WebP = 9
    }
}