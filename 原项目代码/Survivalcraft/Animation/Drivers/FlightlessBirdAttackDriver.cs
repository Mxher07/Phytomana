using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 不能飞的鸟类攻击驱动器
    /// 注意：踢腿动画由 FlightlessBirdWalkDriver 处理，因为需要与行走动画混合
    /// 原始代码: num = num4 * num5 + m_kickPhase; 然后 Lerp(kickAngle)
    /// 这个 Driver 目前为空实现，攻击动画在 Walk Driver 中与行走混合
    /// </summary>
    public class FlightlessBirdAttackDriver : IAnimationDriver {
        public string Name => "FlightlessBirdAttack";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = Array.Empty<string>();

        public void Update(float deltaTime, AnimationParameters parameters) {
            // 攻击动画由 FlightlessBirdWalkDriver 处理
            // 原始代码中踢腿是与行走动画混合的，不是独立的动画层
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // 空实现 - 攻击动画由 FlightlessBirdWalkDriver 处理
            // 原始代码逻辑:
            // 1. legAngle1 = walkAngle + kickPhase
            // 2. kickAngle = DegToRad(60) * Sin(PI * Sigmoid(kickPhase, 5))
            // 3. legAngle1 = Lerp(legAngle1, kickAngle, kickFactor)
            // 这需要访问行走动画的中间结果，所以放在 WalkDriver 中处理
        }
    }
}