using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 鸟类行走驱动器 - 处理行走时的腿部摆动和身体晃动
    /// </summary>
    public class BirdWalkDriver : IAnimationDriver {
        public string Name => "BirdWalk";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Body", "Leg1", "Leg2", "Head", "Neck"];

        // 参数名称
        public string PhaseParam { get; set; } = "MovementPhase";
        public string BobParam { get; set; } = "Bob";
        public string RotationParam { get; set; } = "Rotation";
        public string PositionParam { get; set; } = "Position";
        public string IsOnGroundParam { get; set; } = "IsOnGround";
        public string ImmersionFactorParam { get; set; } = "ImmersionFactor";
        public string FlySpeedParam { get; set; } = "FlySpeed";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";
        public string WalkBobHeightParam { get; set; } = "WalkBobHeight";

        // 可配置属性
        public float LegAngle { get; set; } = 0.6f; // 腿部摆动角度（弧度）
        public float FlyLegAngle { get; set; } = 60f; // 飞行时腿部收起角度（度）

        float _phase;
        float _bob;
        Vector3 _rotation;
        Vector3 _position;
        bool _isOnGround;
        float _immersionFactor;
        float _flySpeed;
        float _lookAngleX;
        float _lookAngleY;
        float _walkBobHeight;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _phase = parameters.GetFloat(PhaseParam);
            _bob = parameters.GetFloat(BobParam);
            _rotation = parameters.GetVector3(RotationParam);
            _position = parameters.GetVector3(PositionParam);
            _isOnGround = parameters.GetBool(IsOnGroundParam);
            _immersionFactor = parameters.GetFloat(ImmersionFactorParam);
            _flySpeed = parameters.GetFloat(FlySpeedParam);
            _lookAngleX = parameters.GetFloat(LookAngleXParam);
            _lookAngleY = parameters.GetFloat(LookAngleYParam);
            _walkBobHeight = parameters.GetFloat(WalkBobHeightParam);

            // 注意：Bob 已经在 ComponentBirdModel.Update() 中平滑过了，直接使用
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // 计算腿部角度
            float legAngle1, legAngle2;
            if (_isOnGround
                || _immersionFactor > 0f
                || _flySpeed == 0f) {
                // 站立或在水中：腿部交替摆动
                legAngle1 = LegAngle * MathF.Sin(MathF.PI * 2f * _phase);
                legAngle2 = -legAngle1;
            }
            else {
                // 飞行中：腿部收起
                float flyLegAngle = -MathUtils.DegToRad(FlyLegAngle);
                legAngle1 = flyLegAngle;
                legAngle2 = flyLegAngle;
            }

            // Body 骨骼
            // 原始: Matrix.CreateFromYawPitchRoll(vector.X, 0f, 0f) * Matrix.CreateTranslation(position + Bob)
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                boneTransforms[bodyBone.Index] = Matrix.CreateFromYawPitchRoll(_rotation.X, 0f, 0f)
                    * Matrix.CreateTranslation(_position.X, _position.Y + _bob, _position.Z);
            }

            // 腿部骨骼
            ModelBone leg1Bone = model.FindBone("Leg1", false);
            if (leg1Bone != null) {
                boneTransforms[leg1Bone.Index] = Matrix.CreateRotationX(legAngle1);
            }
            ModelBone leg2Bone = model.FindBone("Leg2", false);
            if (leg2Bone != null) {
                boneTransforms[leg2Bone.Index] = Matrix.CreateRotationX(legAngle2);
            }

            // 头部和颈部 - 严格按照原始逻辑
            ModelBone neckBone = model.FindBone("Neck", false);
            bool hasNeck = neckBone != null;

            // 原始代码计算:
            // yaw = LookAngleX / 2
            // yaw2 = LookAngleX / 2
            // num4 = 0.5 * Sin(π * MovementPhase)  (站立/水中时)
            // num5 = -num4
            // Neck: CreateFromYawPitchRoll(yaw2, num4 + LookAngleY, 0)
            // Head: CreateFromYawPitchRoll(yaw, num5 + Clamp(vector.Y, -π/4, π/4), vector.Z)
            float yaw = _lookAngleX / 2f;
            float yaw2 = _lookAngleX / 2f;

            // 站立时的颈部/头部 pitch 摆动
            float num4 = 0f;
            if (_isOnGround || _immersionFactor > 0f) {
                // 原始: num4 = 0.5f * Sin(π * 2f * MovementPhase / 2f) = Sin(π * MovementPhase)
                num4 = 0.5f * MathF.Sin(MathF.PI * _phase);
            }
            float num5 = -num4;
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone != null) {
                // 头部 pitch: num5 + Clamp(vector.Y, ...)
                float headPitch = num5 + Math.Clamp(_rotation.Y, -(float)Math.PI / 4f, (float)Math.PI / 4f);
                boneTransforms[headBone.Index] = Matrix.CreateFromYawPitchRoll(yaw, headPitch, _rotation.Z);
            }
            if (hasNeck) {
                // 颈部 pitch: num4 + LookAngleY
                boneTransforms[neckBone.Index] = Matrix.CreateFromYawPitchRoll(yaw2, num4 + _lookAngleY, 0f);
            }
        }
    }
}