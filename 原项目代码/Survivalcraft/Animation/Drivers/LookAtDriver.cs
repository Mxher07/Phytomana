using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 头部追踪驱动器
    /// </summary>
    public class LookAtDriver : IAnimationDriver {
        public string Name => "LookAt";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        // 可配置的目标骨骼名称
        public string TargetBoneName {
            get;
            set {
                field = value;
                m_cachedTargetBones = null;
            }
        } = "Head";

        // IAnimationDriver 接口实现
        public string[] TargetBones => m_cachedTargetBones ??= [TargetBoneName];
        public string[] m_cachedTargetBones;

        // 配置参数
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";

        // 角度限制（度数，内部转换为弧度）

        public float MaxAngleX {
            get;
            set => field = MathUtils.DegToRad(value);
        } = 65f;

        public float MaxAngleY {
            get;
            set => field = MathUtils.DegToRad(value);
        } = 55f;

        // 旋转轴配置（用于适配不同坐标系）
        // "X", "Y", "Z"
        public string PitchAxis { get; set; } = "X"; // 俯仰轴（上下）
        public string YawAxis { get; set; } = "Z"; // 偏航轴（左右）

        // 是否反转方向
        public bool InvertPitch { get; set; } = false;
        public bool InvertYaw { get; set; } = false;

        public float m_lookAngleX; // 弧度
        public float m_lookAngleY; // 弧度

        public void Update(float deltaTime, AnimationParameters parameters) {
            // 参数是弧度
            m_lookAngleX = Math.Clamp(parameters.GetFloat(LookAngleXParam), -MaxAngleX, MaxAngleX);
            m_lookAngleY = Math.Clamp(parameters.GetFloat(LookAngleYParam), -MaxAngleY, MaxAngleY);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            ModelBone targetBone = model.FindBone(TargetBoneName, false);
            if (targetBone == null) {
                return;
            }

            // 应用方向反转
            float pitch = InvertPitch ? -m_lookAngleY : m_lookAngleY;
            float yaw = InvertYaw ? m_lookAngleX : -m_lookAngleX;

            // 根据配置的轴创建旋转
            Matrix pitchRotation = CreateRotationForAxis(PitchAxis, pitch);
            Matrix yawRotation = CreateRotationForAxis(YawAxis, yaw);

            // 先应用 yaw 再应用 pitch（和原始代码顺序一致）
            boneTransforms[targetBone.Index] = pitchRotation * yawRotation;
        }

        public Matrix CreateRotationForAxis(string axis, float angle) {
            return axis?.ToUpperInvariant() switch {
                "X" => Matrix.CreateRotationX(angle),
                "Y" => Matrix.CreateRotationY(angle),
                "Z" => Matrix.CreateRotationZ(angle),
                _ => Matrix.CreateRotationX(angle) // 默认 X 轴
            };
        }
    }
}