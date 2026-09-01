using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 人类死亡/躺下驱动器 - 处理死亡动画和躺下动画
    /// </summary>
    public class HumanDeathDriver : IAnimationDriver {
        public string Name => "HumanDeath";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Body", "Head", "Leg1", "Leg2", "Hand1", "Hand2"];

        // 参数名称
        public string DeathPhaseParam { get; set; } = "DeathPhase";
        public string LieDownFactorParam { get; set; } = "LieDownFactor";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";
        public string BodyHeightParam { get; set; } = "BodyHeight";
        public string BodyDepthParam { get; set; } = "BodyDepth";
        public string BodyForwardParam { get; set; } = "BodyForward";

        // 从 HumanWalkDriver 继承的角度参数
        public string HandAngles1Param { get; set; } = "HumanHandAngles1";
        public string HandAngles2Param { get; set; } = "HumanHandAngles2";
        public string LegAngles1Param { get; set; } = "HumanLegAngles1";
        public string LegAngles2Param { get; set; } = "HumanLegAngles2";

        // 可配置属性
        public float LieDownRollAngle { get; set; } = 90f; // 躺下时侧翻角度
        public float SmoothSpeed { get; set; } = 12f;

        float _deathPhase;
        float _lieDownFactor;
        float _rotationY;
        Vector3 _position;
        float _bodyHeight;
        float _bodyDepth;
        Vector3 _bodyForward;

        // 从 HumanWalkDriver 继承的角度
        Vector2 _handAngles1 = Vector2.Zero;
        Vector2 _handAngles2 = Vector2.Zero;
        Vector2 _legAngles1 = Vector2.Zero;
        Vector2 _legAngles2 = Vector2.Zero;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _deathPhase = parameters.GetFloat(DeathPhaseParam);
            _lieDownFactor = parameters.GetFloat(LieDownFactorParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);
            _bodyHeight = parameters.GetFloat(BodyHeightParam);
            _bodyDepth = parameters.GetFloat(BodyDepthParam);
            _bodyForward = parameters.GetVector3(BodyForwardParam);

            // 读取从 HumanWalkDriver 传入的角度
            _handAngles1 = parameters.GetVector2(HandAngles1Param);
            _handAngles2 = parameters.GetVector2(HandAngles2Param);
            _legAngles1 = parameters.GetVector2(LegAngles1Param);
            _legAngles2 = parameters.GetVector2(LegAngles2Param);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            float lieDownPhase = MathUtils.Max(_deathPhase, _lieDownFactor);
            if (lieDownPhase <= 0f) {
                return;
            }
            float inversePhase = 1f - lieDownPhase;

            // 计算侧翻时身体位置偏移
            // 原始代码使用 BoxSize.Y 作为高度，BoxSize.Z 作为深度
            Vector3 forwardFlat = Vector3.Normalize(_bodyForward * new Vector3(1f, 0f, 1f));
            Vector3 bodyOffset = lieDownPhase * 0.5f * _bodyHeight * forwardFlat + lieDownPhase * Vector3.UnitY * _bodyDepth * 0.1f;

            // 设置 Body 骨骼 - 侧翻躺下
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                boneTransforms[bodyBone.Index] = Matrix.CreateFromYawPitchRoll(_rotationY, (float)Math.PI / 2f * lieDownPhase, 0f)
                    * Matrix.CreateTranslation(_position + bodyOffset);
            }

            // 设置 Head 骨骼 - 重置
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone != null) {
                boneTransforms[headBone.Index] = Matrix.Identity;
            }

            // 设置 Hand 骨骼 - 逐渐放松（使用从 HumanWalkDriver 继承的角度）
            ModelBone hand1Bone = model.FindBone("Hand1", false);
            if (hand1Bone != null) {
                boneTransforms[hand1Bone.Index] = Matrix.CreateRotationY(_handAngles1.Y * inversePhase)
                    * Matrix.CreateRotationX(_handAngles1.X * inversePhase);
            }
            ModelBone hand2Bone = model.FindBone("Hand2", false);
            if (hand2Bone != null) {
                boneTransforms[hand2Bone.Index] = Matrix.CreateRotationY(_handAngles2.Y * inversePhase)
                    * Matrix.CreateRotationX(_handAngles2.X * inversePhase);
            }

            // 设置 Leg 骨骼 - 逐渐放松
            ModelBone leg1Bone = model.FindBone("Leg1", false);
            if (leg1Bone != null) {
                boneTransforms[leg1Bone.Index] = Matrix.CreateRotationY(_legAngles1.Y * inversePhase)
                    * Matrix.CreateRotationX(_legAngles1.X * inversePhase);
            }
            ModelBone leg2Bone = model.FindBone("Leg2", false);
            if (leg2Bone != null) {
                boneTransforms[leg2Bone.Index] = Matrix.CreateRotationY(_legAngles2.Y * inversePhase)
                    * Matrix.CreateRotationX(_legAngles2.X * inversePhase);
            }
        }
    }
}