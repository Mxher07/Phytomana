namespace Engine.Media {
    /// <summary>
    /// 材质纹理槽位枚举
    /// 定义纹理绑定到着色器的纹理单元
    /// </summary>
    public enum MaterialTextureSlot {
        BaseColor = 0,
        MetallicRoughness = 1,
        Normal = 2,
        Occlusion = 3,
        Emissive = 4,
        Environment = 5,
        ClearCoat = 6,
        ClearCoatRoughness = 7,
        ClearCoatNormal = 8,
        Iridescence = 9,
        IridescenceThickness = 10,

        // IBL Texture Slots
        IBLLambertian = 11,
        IBLGGX = 12,
        IBLCharlie = 13,
        IBLGGXLUT = 14,
        IBLCharlieLUT = 15,

        // Phase 3 extension slots
        Transmission = 16,
        Thickness = 17,
        SheenColor = 18,
        SheenRoughness = 19,
        Specular = 20,
        SpecularColor = 21,
        DiffuseTransmission = 22,
        DiffuseTransmissionColor = 23,
        Anisotropy = 24,

        // Framebuffer texture for transmission refraction
        TransmissionFramebuffer = 25,

        // SpecularGlossiness workflow
        Diffuse = 26,
        SpecularGlossiness = 27,

        // Scatter Framebuffer for VolumeScatter
        ScatterFramebuffer = 28,
        ScatterDepthFramebuffer = 29,

        // Morph Target Texture (TEXTURE_2D_ARRAY)
        MorphTargets = 30,

        // Joint/Bone Matrix Texture for GPU skinning (TEXTURE_2D, RGBA32F)
        // Used by AdvancedMeshRenderer for skeletal animation
        JointMatrices = 31
    }
}