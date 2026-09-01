using Engine.Animation.RootMotion;

namespace Engine.Animation {
    /// <summary>
    /// 根骨骼位移剥离需求（值类型，default = 不剥离）。
    /// 由 RootMotionConfig 在 config 切换时派生，盖戳到运行中 blend 状态。
    /// </summary>
    public readonly struct RootStripInfo {
        public readonly bool NeedsStrip;
        public readonly string SourceBone;   // 剥离链起点；config.SourceBone 或 rootBoneName 回退

        public RootStripInfo(bool needsStrip, string sourceBone) {
            NeedsStrip = needsStrip;
            SourceBone = sourceBone;
        }

        /// <summary>从 root motion 配置派生剥离需求。None/AddImpulse/null → 不剥离。</summary>
        public static RootStripInfo From(RootMotionConfig config, string fallbackBone) {
            if (config?.Translation is { Mode: var mode }
                && mode != TranslationMode.None
                && mode != TranslationMode.AddImpulse) {
                string bone = !string.IsNullOrEmpty(config.SourceBone)
                    ? config.SourceBone
                    : fallbackBone;
                return new RootStripInfo(true, bone);
            }
            return default;
        }
    }
}
