using GltfMaterial = SharpGLTF.Schema2.Material;

namespace Engine.Media {
    /// <summary>
    /// KHR_materials_volume_scatter 扩展
    /// 使用 Burley 扩散剖面进行次表面散射
    /// </summary>
    public class VolumeScatterExtension : MaterialExtension {
        public override string ExtensionName => "KHR_materials_volume_scatter";

        /// <summary>
        /// 多重散射颜色
        /// </summary>
        public Vector3 MultiscatterColor { get; set; } = Vector3.Zero;

        /// <summary>
        /// 散射各向异性 (-1 到 1)
        /// </summary>
        public float ScatterAnisotropy { get; set; }

        /// <summary>
        /// Volume Scatter 始终启用（如果扩展存在）
        /// </summary>
        public override bool IsEnabled => true;

        public override IEnumerable<MaterialTextureSlot> GetTextureSlots() {
            yield break;
        }

        public override void LoadFromGltf(GltfMaterial material, ModelData modelData) {
            object volumeScatterExt = GetUnknownExtension(material, "KHR_materials_volume_scatter");
            if (volumeScatterExt == null) {
                return;
            }
            MultiscatterColor = GetExtensionColor(volumeScatterExt, "multiscatterColor", Vector3.Zero);
            ScatterAnisotropy = GetExtensionFloat(volumeScatterExt, "scatterAnisotropy");
        }

        public override void AppendDefines(IShaderDefineBuilder defines) {
            if (IsEnabled) {
                defines.AddMaterialExtension("VOLUME_SCATTER");
                defines.Add("HAS_VOLUME_SCATTER");
                defines.AddRaw($"SCATTER_SAMPLES_COUNT {ScatterSampleCount}");
            }
        }

        #region Burley Diffusion Profile (移植自官方 material.js)

        /// <summary>
        /// 散射样本数量（官方固定为 55）
        /// </summary>
        public const int ScatterSampleCount = 55;

        /// <summary>
        /// 最小散射半径
        /// </summary>
        public static float ScatterMinRadius { get; private set; } = 1.0f;

        /// <summary>
        /// 预计算的散射样本数组
        /// 格式: [theta0, r0, pdf0, theta1, r1, pdf1, ...]
        /// </summary>
        public static float[] ScatterSamples { get; private set; }

        static VolumeScatterExtension() =>
            // 在静态构造函数中预计算散射样本
            ScatterSamples = ComputeScatterSamples();

        /// <summary>
        /// 使用 Blender 的 Burley 扩散剖面实现预计算样本
        /// </summary>
        static float[] ComputeScatterSamples() {
            // 使用白色反照率预计算样本位置
            float d = BurleySetup(1.0f, 1.0f);
            const float randU = 0.5f; // 固定随机值以保证确定性
            const float randV = 0.5f;

            // 找到我们可以表示的最小半径
            float minRadius = 1.0f;
            float goldenAngle = MathF.PI * (3.0f - MathF.Sqrt(5.0f));
            float[] uniformArray = new float[ScatterSampleCount * 3];
            for (int i = 0; i < ScatterSampleCount; i++) {
                float theta = goldenAngle * i + MathF.PI * 2.0f * randU;
                float x = (randV + i) / ScatterSampleCount;
                float r = BurleySample(d, x);
                minRadius = MathF.Min(minRadius, r);
                int idx = i * 3;
                uniformArray[idx + 0] = theta;
                uniformArray[idx + 1] = r;
                uniformArray[idx + 2] = 1.0f / BurleyPdf(d, r);
            }

            // 避免浮点精度问题
            minRadius = MathF.Max(minRadius, 0.00001f);
            ScatterMinRadius = minRadius;
            return uniformArray;
        }

        static float BurleySample(float d, float xRand) {
            xRand *= 0.9963790093708328f;
            const float tolerance = 1e-6f;
            const int maxIterationCount = 10;
            float r;
            if (xRand <= 0.9f) {
                r = MathF.Exp(xRand * xRand * 2.4f) - 1.0f;
            }
            else {
                r = 15.0f;
            }

            // 求解缩放半径
            for (int i = 0; i < maxIterationCount; i++) {
                float exp_r_3 = MathF.Exp(-r / 3.0f);
                float exp_r = exp_r_3 * exp_r_3 * exp_r_3;
                float f = 1.0f - 0.25f * exp_r - 0.75f * exp_r_3 - xRand;
                float f_ = 0.25f * exp_r + 0.25f * exp_r_3;
                if (MathF.Abs(f) < tolerance
                    || f_ == 0.0f) {
                    break;
                }
                r = r - f / f_;
                r = MathF.Max(r, 0.0f);
            }
            return r * d;
        }

        static float BurleyEval(float d, float r) {
            if (r >= 16 * d) {
                return 0.0f;
            }
            float exp_r_3_d = MathF.Exp(-r / (3.0f * d));
            float exp_r_d = exp_r_3_d * exp_r_3_d * exp_r_3_d;
            return (exp_r_d + exp_r_3_d) / (8.0f * MathF.PI * d);
        }

        static float BurleyPdf(float d, float r) => BurleyEval(d, r) / 0.9963790093708328f;

        static float BurleySetup(float radius, float albedo) {
            float m_1_pi = 1.0f / MathF.PI;
            float s = 1.9f - albedo + 3.5f * ((albedo - 0.8f) * (albedo - 0.8f));
            float l = 0.25f * m_1_pi * radius;
            return l / s;
        }

        #endregion
    }
}