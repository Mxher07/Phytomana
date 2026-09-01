using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 四足攻击驱动器 - 处理攻击（顶撞）动画
    /// </summary>
    public class FourLeggedAttackDriver : IAnimationDriver {
        public string Name => "FourLeggedAttack";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Head", "Neck"];

        // 参数名称
        public string ButtFactorParam { get; set; } = "ButtFactor";
        public string ButtPhaseParam { get; set; } = "ButtPhase";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";

        // 可配置属性
        public float HeadMaxAngleX { get; set; } = 65f;
        public float HeadMaxAngleY { get; set; } = 55f;
        public float HeadRatio { get; set; } = 0.4f;
        public float NeckRatio { get; set; } = 0.6f;

        // 攻击动画参数
        public float ButtAngle { get; set; } = 40f;
        public float ButtSigmoidK { get; set; } = 4f;

        float _buttFactor;
        float _buttPhase;
        float _lookAngleX;
        float _lookAngleY;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _buttFactor = parameters.GetFloat(ButtFactorParam);
            _buttPhase = parameters.GetFloat(ButtPhaseParam);
            _lookAngleX = parameters.GetFloat(LookAngleXParam);
            _lookAngleY = parameters.GetFloat(LookAngleYParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            ModelBone neckBone = model.FindBone("Neck", false);
            bool hasNeck = neckBone != null;
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone == null) {
                return;
            }

            // 基础角度
            float maxAngleX = MathUtils.DegToRad(HeadMaxAngleX);
            float maxAngleY = MathUtils.DegToRad(HeadMaxAngleY);
            float lookAngleX = Math.Clamp(_lookAngleX, -maxAngleX, maxAngleX);
            float lookAngleY = Math.Clamp(_lookAngleY, -maxAngleY, maxAngleY);
            if (hasNeck) {
                lookAngleX *= HeadRatio;
                lookAngleY *= HeadRatio;
            }

            // 攻击动画覆盖
            float buttY = -MathUtils.DegToRad(ButtAngle) * MathF.Sin(MathF.PI * 2f * MathUtils.Sigmoid(_buttPhase, ButtSigmoidK));
            // 原始实现：攻击时将 X 分量插值到 0（头部不左右转动）
            lookAngleX = lookAngleX + (0f - lookAngleX) * _buttFactor;
            lookAngleY = lookAngleY + (buttY - lookAngleY) * _buttFactor;
            boneTransforms[headBone.Index] = Matrix.CreateRotationX(lookAngleY) * Matrix.CreateRotationZ(-lookAngleX);

            // 颈部 - 原始实现中攻击动画只影响头部，不影响颈部
            if (hasNeck) {
                float neckAngleX = Math.Clamp(_lookAngleX * NeckRatio, -maxAngleX, maxAngleX);
                float neckAngleY = Math.Clamp(_lookAngleY * NeckRatio, -maxAngleY, maxAngleY);
                boneTransforms[neckBone.Index] = Matrix.CreateRotationX(neckAngleY) * Matrix.CreateRotationZ(-neckAngleX);
            }
        }
    }
}