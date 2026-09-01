using System.Collections.Immutable;

namespace Engine.Media {
    /// <summary>
    /// glTF 扩展管理器
    /// </summary>
    public static class MaterialExtensionManager {
        public static ImmutableArray<string> AvailableExtensions = [
            "KHR_materials_clearcoat",
            "KHR_materials_iridescence",
            "KHR_materials_transmission",
            "KHR_materials_volume",
            "KHR_materials_sheen",
            "KHR_materials_specular",
            "KHR_materials_ior",
            "KHR_materials_emissive_strength",
            "KHR_materials_dispersion",
            "KHR_materials_anisotropy",
            "KHR_materials_diffuse_transmission",
            "KHR_materials_volume_scatter",
            "KHR_materials_unlit",
            "KHR_materials_pbrSpecularGlossiness",
            "KHR_lights_punctual",
            "KHR_materials_variants",
            "KHR_animation_pointer",
            "KHR_node_visibility",
            "EXT_mesh_gpu_instancing",
            "KHR_mesh_quantization",
            "EXT_texture_webp"
        ];

        public static HashSet<string> DisabledExtensions = [];

        public static bool IsExtensionAvailable(string extensionName) => AvailableExtensions.Contains(extensionName);

        public static bool IsExtensionEnabled(string extensionName) => !DisabledExtensions.Contains(extensionName);

        public static void DisableExtension(string extensionName) {
            if (IsExtensionAvailable(extensionName)) {
                DisabledExtensions.Add(extensionName);
            }
        }

        public static void EnableExtension(string extensionName) {
            if (IsExtensionAvailable(extensionName)) {
                DisabledExtensions.Remove(extensionName);
            }
        }

        public static bool ToggleExtension(string extensionName) {
            if (IsExtensionAvailable(extensionName)) {
                if (DisabledExtensions.Remove(extensionName)) {
                    return true;
                }
                if (DisabledExtensions.Add(extensionName)) {
                    return false;
                }
            }
            return false;
        }
    }
}