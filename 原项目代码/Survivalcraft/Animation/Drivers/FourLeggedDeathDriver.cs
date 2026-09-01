using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 四足死亡驱动器 - 处理完整的死亡动画（全身倒下 + 头部下垂）
    /// </summary>
    public class FourLeggedDeathDriver : IAnimationDriver {
        public string Name => "FourLeggedDeath";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;

        string[] mTargetBones = [
            "Body",
            "Head",
            "Neck",
            "Leg1",
            "Leg2",
            "Leg3",
            "Leg4"
        ];

        // 参数名称
        public string DeathPhaseParam { get; set; } = "DeathPhase";
        public string DeathCauseOffsetParam { get; set; } = "DeathCauseOffset";
        public string BodyHeightParam { get; set; } = "BodyHeight";
        public string BodyRightParam { get; set; } = "BodyRight";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";

        // 输入参数名称 - 从 WalkDriver 读取腿部角度
        public string LegAngle1Param { get; set; } = "LegAngle1";
        public string LegAngle2Param { get; set; } = "LegAngle2";
        public string LegAngle3Param { get; set; } = "LegAngle3";
        public string LegAngle4Param { get; set; } = "LegAngle4";

        // 可配置属性
        public float DeathHeadAngle { get; set; } = 50f;
        public float DeathRollAngle { get; set; } = 90f; // 侧翻角度
        public float DeathBodyDrop { get; set; } = 0.5f; // 身体下沉高度
        public float DeathBodyRise { get; set; } = 0.2f; // 身体抬起高度

        float _deathPhase;
        Vector3 _deathCauseOffset;
        float _bodyHeight;
        Vector3 _bodyRight;
        float _rotationY;
        Vector3 _position;

        // 从 WalkDriver 读取的腿部角度
        float _legAngle1;
        float _legAngle2;
        float _legAngle3;
        float _legAngle4;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _deathPhase = parameters.GetFloat(DeathPhaseParam);
            _deathCauseOffset = parameters.GetVector3(DeathCauseOffsetParam);
            _bodyHeight = parameters.GetFloat(BodyHeightParam);
            _bodyRight = parameters.GetVector3(BodyRightParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);

            // 读取腿部角度（由 WalkDriver 输出）
            _legAngle1 = parameters.GetFloat(LegAngle1Param);
            _legAngle2 = parameters.GetFloat(LegAngle2Param);
            _legAngle3 = parameters.GetFloat(LegAngle3Param);
            _legAngle4 = parameters.GetFloat(LegAngle4Param);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (_deathPhase <= 0f) {
                return;
            }

            // 计算侧翻方向
            float rollDirection = Vector3.Dot(_bodyRight, _deathCauseOffset) > 0f ? 1 : -1;
            float rollAngle = MathUtils.DegToRad(DeathRollAngle) * _deathPhase * rollDirection;

            // Body 骨骼 - 侧翻倒下
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                boneTransforms[bodyBone.Index] = Matrix.CreateTranslation(-DeathBodyDrop * _bodyHeight * Vector3.UnitY * _deathPhase)
                    * Matrix.CreateFromYawPitchRoll(_rotationY, 0f, rollAngle)
                    * Matrix.CreateTranslation(DeathBodyRise * _bodyHeight * Vector3.UnitY * _deathPhase)
                    * Matrix.CreateTranslation(_position);
            }

            // Head 骨骼 - 头部下垂
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone != null) {
                boneTransforms[headBone.Index] = Matrix.CreateRotationX(MathUtils.DegToRad(DeathHeadAngle) * _deathPhase);
            }

            // Neck 骨骼 - 重置
            ModelBone neckBone = model.FindBone("Neck", false);
            if (neckBone != null) {
                boneTransforms[neckBone.Index] = Matrix.Identity;
            }

            // Legs 骨骼 - 从当前角度平滑过渡到放松状态
            // 原始实现: m_legAngle * (1 - DeathPhase)
            float deathFactor = 1f - _deathPhase;
            float[] legAngles = [_legAngle1, _legAngle2, _legAngle3, _legAngle4];
            for (int i = 0; i < 4; i++) {
                ModelBone bone = model.FindBone($"Leg{i + 1}", false);
                if (bone != null) {
                    // 腿部角度逐渐减小到 0（放松状态）
                    boneTransforms[bone.Index] = Matrix.CreateRotationX(legAngles[i] * deathFactor);
                }
            }
        }
    }
}