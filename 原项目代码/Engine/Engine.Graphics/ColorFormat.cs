namespace Engine.Graphics {
    public enum ColorFormat {
        // === 未压缩格式 ===
        Rgba8888 = 0,       // Linear RGBA8
        Rgba8888Srgb = 9,   // sRGB + Alpha
        Rgba5551 = 1,
        Rgb565 = 2,
        R8 = 3,

        // === HDR 未压缩格式 ===
        R32f = 4,           // Single channel float
        RG32f = 5,          // Two channel float
        RGBA32f = 6,        // Four channel float
        Rgba16f = 10,        // Four channel half float (HDR 常用)

        // === ASTC 压缩格式 (仅 CompressedTexture2D) ===
        LinearLDR = 7,      // Linear ASTC
        SrgbLDR = 8         // sRGB ASTC
    }
}