namespace Engine.Graphics {
    public enum TextureFilterMode {
        Point = 0,
        Linear = 1,
        Anisotropic = 2,
        PointMipLinear = 3,
        LinearMipPoint = 4,
        MinPointMagLinearMipPoint = 5,
        MinPointMagLinearMipLinear = 6,
        MinLinearMagPointMipPoint = 7,
        MinLinearMagPointMipLinear = 8
    }
}