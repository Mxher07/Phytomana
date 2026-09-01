using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 死亡动画驱动器 - 默认作用于根骨骼产生全身倒下效果
    /// 变换顺序：抬高 -> 旋转 -> 下沉
    /// 这样可以避免身体边缘在侧翻时陷入地面
    /// </summary>
    public class DeathDriver : IAnimationDriver {
        public string Name => "Death";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        // 目标骨骼 - 默认为空，表示作用于根骨骼
        public string[] m_targetBones = Array.Empty<string>();
        public string[] TargetBones => m_targetBones;

        // 可选：指定特定骨骼名称（如果为空则使用根骨骼）
        public string RootBoneName { get; set; } = null;

        // 参数名称
        public string DeathPhaseParam { get; set; } = "DeathPhase";
        public string BodyHeightParam { get; set; } = "BodyHeight";
        public string BodyRightParam { get; set; } = "BodyRight";
        public string DeathCauseOffsetParam { get; set; } = "DeathCauseOffset";

        // 死亡动画配置
        /// <summary>
        /// 侧翻角度（度），默认 90 度
        /// </summary>
        public float RollAngle { get; set; } = 90f;

        /// <summary>
        /// 前后倾斜角度（度），默认 0
        /// </summary>
        public float PitchAngle { get; set; } = 0f;

        /// <summary>
        /// 身体下沉高度（相对于 BodyHeight 的比例），默认 0
        /// 实际下沉 = BodyHeight * BodyDrop * DeathPhase
        /// </summary>
        public float BodyDrop { get; set; } = 0f;

        /// <summary>
        /// 是否根据 DeathCauseOffset 自动决定侧翻方向
        /// true: 向伤害来源方向侧翻
        /// false: 固定向右（正角度）侧翻
        /// </summary>
        public bool AutoRollDirection { get; set; } = true;

        public float m_deathPhase;
        public float m_bodyHeight;
        public Vector3 m_bodyRight;
        public Vector3 m_deathCauseOffset;
        public int m_rootBoneIndex = -1;

        /// <summary>
        /// 获取默认根骨骼索引。优先使用 model.RootBone，否则使用第一个骨骼。
        /// </summary>
        public static int GetDefaultRootBoneIndex(Model model) {
            if (model.RootBone != null) {
                return model.RootBone.Index;
            }
            if (model.Bones.Count > 0) {
                return model.Bones[0].Index;
            }
            return -1;
        }

        public void Update(float deltaTime, AnimationParameters parameters) {
            m_deathPhase = Math.Clamp(parameters.GetFloat(DeathPhaseParam), 0f, 1f);
            m_bodyHeight = parameters.GetFloat(BodyHeightParam);
            m_bodyRight = parameters.GetVector3(BodyRightParam);
            m_deathCauseOffset = parameters.GetVector3(DeathCauseOffsetParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (m_deathPhase <= 0f) {
                return;
            }

            // 获取根骨骼索引
            if (m_rootBoneIndex < 0) {
                if (!string.IsNullOrEmpty(RootBoneName)) {
                    ModelBone bone = model.FindBone(RootBoneName);
                    m_rootBoneIndex = bone?.Index ?? GetDefaultRootBoneIndex(model);
                }
                else {
                    m_rootBoneIndex = GetDefaultRootBoneIndex(model);
                }
            }

            // 如果没有有效骨骼，直接返回
            if (m_rootBoneIndex < 0) {
                return;
            }
            float t = m_deathPhase;

            // 计算侧翻方向
            float rollDirection = 1f;
            if (AutoRollDirection
                && m_bodyRight.LengthSquared() > 0.001f
                && m_deathCauseOffset.LengthSquared() > 0.001f) {
                rollDirection = Vector3.Dot(m_bodyRight, m_deathCauseOffset) > 0f ? 1 : -1;
            }

            // 计算角度（弧度）
            float rollRad = RollAngle * t * MathF.PI / 180f * rollDirection;
            float pitchRad = PitchAngle * t * MathF.PI / 180f;

            // 计算位移
            float dropY = -BodyDrop * m_bodyHeight * t;
            Matrix deathTransform = Matrix.CreateRotationX(pitchRad)
                * Matrix.CreateRotationZ(rollRad)
                * // 先旋转
                Matrix.CreateTranslation(0, dropY, 0); // 再平移（世界坐标系 Y）

            // 应用到根骨骼
            boneTransforms[m_rootBoneIndex] = deathTransform;
        }
    }
}