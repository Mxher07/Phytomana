namespace Engine.Media {
    /// <summary>
    /// 材质扩展注册表，支持自动发现和加载扩展
    /// </summary>
    public static class MaterialExtensionRegistry {
        public static readonly Dictionary<string, Func<MaterialExtension>> _factories = new() {
            { "KHR_materials_clearcoat", () => new ClearCoatExtension() },
            { "KHR_materials_iridescence", () => new IridescenceExtension() },
            { "KHR_materials_transmission", () => new TransmissionExtension() },
            { "KHR_materials_volume", () => new VolumeExtension() },
            { "KHR_materials_sheen", () => new SheenExtension() },
            { "KHR_materials_specular", () => new SpecularExtension() },
            { "KHR_materials_ior", () => new IorExtension() },
            { "KHR_materials_emissive_strength", () => new EmissiveStrengthExtension() },
            { "KHR_materials_dispersion", () => new DispersionExtension() },
            { "KHR_materials_anisotropy", () => new AnisotropyExtension() },
            { "KHR_materials_diffuse_transmission", () => new DiffuseTransmissionExtension() },
            { "KHR_materials_volume_scatter", () => new VolumeScatterExtension() },
            { "KHR_materials_unlit", () => new UnlitExtension() },
            { "KHR_materials_pbrSpecularGlossiness", () => new SpecularGlossinessExtension() }
        };

        /// <summary>
        /// 注册扩展类型
        /// </summary>
        public static void Register<T>() where T : MaterialExtension, new() {
            // 创建临时实例获取扩展名
            T instance = new();
            string name = instance.ExtensionName;
            _factories[name] = () => new T();
        }

        /// <summary>
        /// 使用工厂函数注册扩展
        /// </summary>
        public static void Register(string name, Func<MaterialExtension> factory) => _factories[name] = factory;

        /// <summary>
        /// 创建扩展实例
        /// </summary>
        public static MaterialExtension Create(string extensionName) =>
            _factories.TryGetValue(extensionName, out Func<MaterialExtension> factory) ? factory() : null;

        /// <summary>
        /// 清除所有已注册的扩展
        /// </summary>
        public static void Clear() => _factories.Clear();
    }
}