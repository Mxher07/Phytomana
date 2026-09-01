using System.Reflection;
using System.Text.Json.Nodes;
using SharpGLTF.IO;
using SharpGLTF.Schema2;
using GltfMaterial = SharpGLTF.Schema2.Material;
using IExtraProperties = SharpGLTF.Schema2.IExtraProperties;

namespace Engine.Media {
    /// <summary>
    /// 材质扩展基类
    /// </summary>
    public abstract class MaterialExtension {
        /// <summary>
        /// 扩展名称（如 "KHR_materials_clearcoat"）
        /// </summary>
        public abstract string ExtensionName { get; }

        /// <summary>
        /// 是否启用此扩展
        /// </summary>
        public abstract bool IsEnabled { get; }

        /// <summary>
        /// 获取此扩展使用的纹理槽位列表
        /// </summary>
        public abstract IEnumerable<MaterialTextureSlot> GetTextureSlots();

        /// <summary>
        /// 从 glTF 材质加载扩展数据
        /// </summary>
        /// <param name="material">glTF 材质</param>
        /// <param name="modelData">模型数据（包含纹理索引映射）</param>
        public abstract void LoadFromGltf(GltfMaterial material, ModelData modelData);

        /// <summary>
        /// 附加着色器 defines（用于着色器变体编译）
        /// </summary>
        public virtual void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                string shortName = ExtensionName.Replace("KHR_materials_", "").ToUpper();
                defines.AddMaterialExtension(shortName);
            }
        }

        /// <summary>
        /// 获取着色器 define 名称（如 "MATERIAL_CLEARCOAT"）
        /// </summary>
        public virtual string GetShaderDefineName() {
            string shortName = ExtensionName.Replace("KHR_materials_", "").ToUpper();
            return $"MATERIAL_{shortName}";
        }

        public static float GetChannelFactor(MaterialChannel? channel, string factorName, float defaultValue) {
            if (channel == null) {
                return defaultValue;
            }
            try {
                return channel.Value.GetFactor(factorName);
            }
            catch {
                return defaultValue;
            }
        }

        /// <summary>
        /// 从材质通道加载纹理（使用 ModelData 的纹理索引映射）
        /// </summary>
        public static ModelMaterialTexture LoadTextureFromChannel(ModelData modelData, MaterialChannel? channel) {
            if (channel?.Texture == null) {
                return null;
            }
            int textureIndex = modelData.GetTextureIndex(channel.Value.Texture.LogicalIndex);
            if (textureIndex < 0) {
                return null;
            }
            int texCoord = channel.Value.TextureCoordinate;
            ModelMaterialTexture matTex = new(textureIndex, texCoord);
            TextureTransform transform = channel.Value.TextureTransform;
            if (transform != null) {
                matTex.SetTransform(transform.Offset, transform.Scale, transform.Rotation);
            }
            return matTex;
        }

        #region 反射辅助方法（用于读取 SharpGLTF 不支持的扩展）

        // 缓存的 PropertyInfo，避免重复反射查找
        static readonly Dictionary<Type, PropertyInfo> _namePropertyCache = new();
        static readonly Dictionary<Type, PropertyInfo> _propertiesPropertyCache = new();

        // UnknownNode 类型缓存
        static Type _unknownNodeType;

        /// <summary>
        /// 通过反射获取未知扩展对象（用于 SharpGLTF 不支持的扩展）
        /// 使用 PropertyInfo 缓存优化性能
        /// </summary>
        public static object GetUnknownExtension(IExtraProperties target, string extensionName) {
            // 懒加载 UnknownNode 类型
            _unknownNodeType ??= AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == "SharpGLTF.IO.UnknownNode");
            if (_unknownNodeType == null) {
                return null;
            }
            foreach (JsonSerializable ext in target.Extensions) {
                Type extType = ext.GetType();
                if (extType != _unknownNodeType) {
                    continue;
                }

                // 使用缓存的 PropertyInfo
                if (!_namePropertyCache.TryGetValue(extType, out PropertyInfo nameProp)) {
                    nameProp = extType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _namePropertyCache[extType] = nameProp;
                }
                if (nameProp?.GetValue(ext)?.ToString() == extensionName) {
                    return ext;
                }
            }
            return null;
        }

        /// <summary>
        /// 从未知扩展对象中获取属性值
        /// 使用 PropertyInfo 缓存优化性能
        /// </summary>
        public static object GetExtensionProperty(object unknownExtension, string propertyName) {
            if (unknownExtension == null) {
                return null;
            }
            Type extType = unknownExtension.GetType();

            // 使用缓存的 PropertyInfo
            if (!_propertiesPropertyCache.TryGetValue(extType, out PropertyInfo propsProp)) {
                propsProp = extType.GetProperty("Properties", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _propertiesPropertyCache[extType] = propsProp;
            }
            IReadOnlyDictionary<string, JsonNode> properties = propsProp?.GetValue(unknownExtension) as IReadOnlyDictionary<string, JsonNode>;
            if (properties != null
                && properties.TryGetValue(propertyName, out JsonNode value)) {
                return value;
            }
            return null;
        }

        /// <summary>
        /// 获取扩展的浮点属性值
        /// </summary>
        public static float GetExtensionFloat(object extension, string propertyName, float defaultValue = 0f) {
            object value = GetExtensionProperty(extension, propertyName);
            if (value is JsonValue jsonValue
                && jsonValue.TryGetValue(out float f)) {
                return f;
            }
            if (value != null) {
                try {
                    return Convert.ToSingle(value);
                }
                catch {
                    // ignored
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 获取扩展的颜色属性值（RGB 数组）
        /// </summary>
        public static Vector3 GetExtensionColor(object extension, string propertyName, Vector3? defaultValue = null) {
            object value = GetExtensionProperty(extension, propertyName);
            if (value is JsonArray array
                && array.Count >= 3) {
                float r = array[0]?.GetValue<float>() ?? 0f;
                float g = array[1]?.GetValue<float>() ?? 0f;
                float b = array[2]?.GetValue<float>() ?? 0f;
                return new Vector3(r, g, b);
            }
            return defaultValue ?? Vector3.One;
        }

        /// <summary>
        /// 获取扩展的纹理索引和 UV 集
        /// </summary>
        public static (int? textureIndex, int? texCoord) GetExtensionTextureInfo(object extension, string propertyName) {
            object value = GetExtensionProperty(extension, propertyName);
            if (value is JsonObject obj) {
                int? textureIndex = obj["index"]?.GetValue<int>();
                int? texCoord = obj["texCoord"]?.GetValue<int>();
                return (textureIndex, texCoord);
            }
            return (null, null);
        }

        #endregion
    }
}