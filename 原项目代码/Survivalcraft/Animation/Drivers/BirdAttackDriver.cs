using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 鸟类攻击驱动器 - 处理攻击时的头部/颈部动画
    /// 注意：腿部踢击动画由 BirdWalkDriver 或 BirdFlyDriver 处理，此驱动器只处理头部/颈部
    /// </summary>
    public class BirdAttackDriver : IAnimationDriver {
        public string Name => "BirdAttack";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Head", "Neck"];

        // 参数名称
        public string KickPhaseParam { get; set; } = "KickPhase";
        public string PeckPhaseParam { get; set; } = "PeckPhase";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";
        public string RotationParam { get; set; } = "Rotation";
        public string PhaseParam { get; set; } = "MovementPhase";
        public string IsOnGroundParam { get; set; } = "IsOnGround";
        public string ImmersionFactorParam { get; set; } = "ImmersionFactor";

        // 可配置属性
        public float PeckAngle { get; set; } = 1.25f; // 啄食角度（弧度）

        float _kickPhase;
        float _peckPhase;
        float _lookAngleX;
        float _lookAngleY;
        Vector3 _rotation;
        float _phase;
        bool _isOnGround;
        float _immersionFactor;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _kickPhase = parameters.GetFloat(KickPhaseParam);
            _peckPhase = parameters.GetFloat(PeckPhaseParam);
            _lookAngleX = parameters.GetFloat(LookAngleXParam);
            _lookAngleY = parameters.GetFloat(LookAngleYParam);
            _rotation = parameters.GetVector3(RotationParam);
            _phase = parameters.GetFloat(PhaseParam);
            _isOnGround = parameters.GetBool(IsOnGroundParam);
            _immersionFactor = parameters.GetFloat(ImmersionFactorParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // 原始代码逻辑:
            // num4 = 0
            // if (Standing || Immersion > 0): num4 = 0.5 * Sin(π * 2 * MovementPhase / 2)
            // num6 = Cos(2π * (kickPhase != 0 ? kickPhase : peckPhase))
            // num4 -= 1.25 * (1 - (cos >= 0 ? cos : -0.5 * cos))
            // num4 += LookAngleY
            // num5 = -num4 (之前计算的站立摆动，但在攻击时不使用)
            float yaw = _lookAngleX / 2f;
            float yaw2 = _lookAngleX / 2f;

            // 颈部基础摆动（站立时）
            float num4 = 0f;
            if (_isOnGround || _immersionFactor > 0f) {
                // 原始: num4 = 0.5f * Sin(π * 2f * MovementPhase / 2f) = Sin(π * MovementPhase)
                num4 = 0.5f * MathF.Sin(MathF.PI * _phase);
            }

            // 攻击/啄食动画
            // 原始: 使用 kickPhase != 0 ? kickPhase : peckPhase
            float activePhase = _kickPhase != 0f ? _kickPhase : _peckPhase;
            float cosPhase = MathF.Cos(MathF.PI * 2f * activePhase);
            float peckAmount = PeckAngle * (1f - (cosPhase >= 0f ? cosPhase : -0.5f * cosPhase));

            // 颈部: num4 -= peckAmount, then += LookAngleY
            float neckPitch = num4 - peckAmount + _lookAngleY;

            // 头部: num5 = -num4 (站立时), 无 peckAmount，但有 Clamp(vector.Y) 和 vector.Z
            float num5 = -num4; // 站立摆动反向
            float headPitch = num5 + Math.Clamp(_rotation.Y, -(float)Math.PI / 4f, (float)Math.PI / 4f);

            // Neck 骨骼
            ModelBone neckBone = model.FindBone("Neck", false);
            if (neckBone != null) {
                boneTransforms[neckBone.Index] = Matrix.CreateFromYawPitchRoll(yaw2, neckPitch, 0f);
            }

            // Head 骨骼
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone != null) {
                boneTransforms[headBone.Index] = Matrix.CreateFromYawPitchRoll(yaw, headPitch, _rotation.Z);
            }
        }
    }
}