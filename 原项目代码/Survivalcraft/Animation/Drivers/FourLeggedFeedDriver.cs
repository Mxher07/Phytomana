using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 四足进食驱动器 - 处理进食时的头部动画
    /// </summary>
    public class FourLeggedFeedDriver : IAnimationDriver {
        public string Name => "FourLeggedFeed";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Head", "Neck"];

        // 参数名称
        public string FeedFactorParam { get; set; } = "FeedFactor";
        public string GameTimeParam { get; set; } = "GameTime";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";

        // 可配置属性
        public float HeadMaxAngleX { get; set; } = 65f;
        public float HeadMaxAngleY { get; set; } = 55f;
        public float HeadRatio { get; set; } = 0.4f;
        public float NeckRatio { get; set; } = 0.6f;

        // 进食动画参数
        public float FeedBaseAngle { get; set; } = 25f;
        public float FeedNoiseRange { get; set; } = 45f;
        public float FeedNoiseFrequency { get; set; } = 3f;
        public int FeedNoiseOctaves { get; set; } = 2;
        public float FeedNoiseFreqStep { get; set; } = 2f;
        public float FeedNoiseAmpStep { get; set; } = 0.75f;

        float _feedFactor;
        float _gameTime;
        float _lookAngleX;
        float _lookAngleY;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _feedFactor = parameters.GetFloat(FeedFactorParam);
            _gameTime = parameters.GetFloat(GameTimeParam);
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

            // 进食动画覆盖 - 使用 SimplexNoise
            float noise = SimplexNoise.OctavedNoise(_gameTime, FeedNoiseFrequency, FeedNoiseOctaves, FeedNoiseFreqStep, FeedNoiseAmpStep);
            float feedY = -MathUtils.DegToRad(FeedBaseAngle + FeedNoiseRange * noise);
            lookAngleX = MathUtils.Lerp(lookAngleX, 0f, _feedFactor);
            lookAngleY = MathUtils.Lerp(lookAngleY, feedY, _feedFactor);
            boneTransforms[headBone.Index] = Matrix.CreateRotationX(lookAngleY) * Matrix.CreateRotationZ(-lookAngleX);

            // 颈部 - 原始实现中进食动画只影响头部，不影响颈部
            if (hasNeck) {
                float neckAngleX = Math.Clamp(_lookAngleX * NeckRatio, -maxAngleX, maxAngleX);
                float neckAngleY = Math.Clamp(_lookAngleY * NeckRatio, -maxAngleY, maxAngleY);
                boneTransforms[neckBone.Index] = Matrix.CreateRotationX(neckAngleY) * Matrix.CreateRotationZ(-neckAngleX);
            }
        }
    }
}