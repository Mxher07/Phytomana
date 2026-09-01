using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 动画数据
    /// </summary>
    public class ModelAnimation {
        public string Name { get; set; } = string.Empty;
        public float Duration { get; set; }
        public List<AnimationChannel> Channels { get; set; } = [];

        /// <summary>
        /// KHR_animation_pointer targets. Action&lt;float&gt; 接受时间（秒），
        /// 采样曲线并直接修改目标属性。
        /// </summary>
        public List<Action<float>> PointerTargets { get; set; } = [];

        /// <summary>
        /// KHR_node_visibility targets. Action&lt;float, Model&gt; 接受时间（秒）和 Model，
        /// 采样曲线并设置对应 ModelMesh 的 IsVisible。
        /// </summary>
        public List<Action<float, Model>> NodeVisibilityTargets { get; set; } = [];

        /// <summary>
        /// 动画通道，对应一个骨骼的某个属性
        /// </summary>
        public class AnimationChannel {
            public string TargetBoneName { get; set; } = string.Empty;
            public AnimationProperty Property { get; set; }
            public AnimationSampler Sampler { get; set; } = new();
        }

        public enum AnimationProperty {
            Translation,
            Rotation,
            Scale,
            Weights
        }

        /// <summary>
        /// 动画采样器
        /// </summary>
        public class AnimationSampler {
            public float[] KeyTimes { get; set; } = [];
            public Vector3[] Translations { get; set; } = [];
            public Quaternion[] Rotations { get; set; } = [];
            public Vector3[] Scales { get; set; } = [];

            /// <summary>
            /// Morph target weights per keyframe. Weights[i] is a float[] with one weight per morph target.
            /// </summary>
            public float[][] Weights { get; set; } = [];

            public InterpolationType Interpolation { get; set; }
        }

        public enum InterpolationType {
            Step,
            Linear,
            CubicSpline
        }
    }
}