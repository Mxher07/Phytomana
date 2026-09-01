using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 鱼类攻击驱动器 - 处理咬合动画
    /// </summary>
    public class FishAttackDriver : IAnimationDriver {
        public string Name => "FishAttack";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Jaw"];

        // 参数名称
        public string BitingPhaseParam { get; set; } = "BitingPhase";

        // 可配置属性
        public float JawAngle { get; set; } = 30f;

        float _bitingPhase;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _bitingPhase = parameters.GetFloat(BitingPhaseParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            ModelBone jawBone = model.FindBone("Jaw", false);
            if (jawBone == null) {
                return;
            }
            if (_bitingPhase > 0f) {
                // 下颚张开然后闭合
                float jawRotation = -MathUtils.DegToRad(JawAngle) * MathF.Sin(MathF.PI * _bitingPhase);
                boneTransforms[jawBone.Index] = Matrix.CreateRotationX(jawRotation);
            }
            else {
                boneTransforms[jawBone.Index] = Matrix.Identity;
            }
        }
    }
}