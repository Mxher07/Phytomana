namespace Engine.Media {
    /// <summary>
    /// 着色器定义构建器接口，解耦材质扩展对渲染层的依赖
    /// </summary>
    public interface IShaderDefineBuilder {
        void Add(string define, int value = 1);
        void AddRaw(string define);
        void AddTextureMap(string textureName);
        void AddUVTransform(string textureName);
        void AddMaterialExtension(string extensionName);
    }
}
