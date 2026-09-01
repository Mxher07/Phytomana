using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 鸟类飞行动画驱动器 - 处理飞行时的翅膀扇动和腿部收起
    /// </summary>
    public class BirdFlyDriver : IAnimationDriver {
        public string Name => "BirdFly";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;

        string[] mTargetBones = [
            "Body",
            "Wing1",
            "Wing2",
            "Leg1",
            "Leg2",
            "Head",
            "Neck"
        ];

        // 参数名称
        public string FlyPhaseParam { get; set; } = "FlyPhase";
        public string PhaseParam { get; set; } = "MovementPhase";
        public string IsOnGroundParam { get; set; } = "IsOnGround";
        public string ImmersionFactorParam { get; set; } = "ImmersionFactor";
        public string FlySpeedParam { get; set; } = "FlySpeed";
        public string RotationParam { get; set; } = "Rotation";
        public string PositionParam { get; set; } = "Position";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";
        public string BobParam { get; set; } = "Bob";

        // 可配置属性
        public float WingAngle { get; set; } = 1.2f; // 翅膀扇动角度（弧度）
        public float WingGroundAngle { get; set; } = 0.3f; // 站立时的翅膀小摆动
        public float FlyLegAngle { get; set; } = 60f; // 飞行时腿部收起角度（度）

        float _flyPhase;
        float _phase;
        bool _isOnGround;
        float _immersionFactor;
        float _flySpeed;
        Vector3 _rotation;
        Vector3 _position;
        float _lookAngleX;
        float _lookAngleY;
        float _bob;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _flyPhase = parameters.GetFloat(FlyPhaseParam);
            _phase = parameters.GetFloat(PhaseParam);
            _isOnGround = parameters.GetBool(IsOnGroundParam);
            _immersionFactor = parameters.GetFloat(ImmersionFactorParam);
            _flySpeed = parameters.GetFloat(FlySpeedParam);
            _rotation = parameters.GetVector3(RotationParam);
            _position = parameters.GetVector3(PositionParam);
            _lookAngleX = parameters.GetFloat(LookAngleXParam);
            _lookAngleY = parameters.GetFloat(LookAngleYParam);
            _bob = parameters.GetFloat(BobParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // 检查是否有翅膀
            ModelBone wing1Bone = model.FindBone("Wing1", false);
            ModelBone wing2Bone = model.FindBone("Wing2", false);
            bool hasWings = wing1Bone != null && wing2Bone != null;

            // 计算腿部角度
            float legAngle1, legAngle2;
            if (_isOnGround
                || _immersionFactor > 0f
                || _flySpeed == 0f) {
                // 站立或在水中：腿部交替摆动
                legAngle1 = 0.6f * MathF.Sin(MathF.PI * 2f * _phase);
                legAngle2 = -legAngle1;
            }
            else {
                // 飞行中：腿部收起
                float flyLegAngle = -MathUtils.DegToRad(FlyLegAngle);
                legAngle1 = flyLegAngle;
                legAngle2 = flyLegAngle;
            }

            // 计算翅膀角度
            float wingAngle = 0f;
            if (hasWings) {
                // 基础飞行翅膀扇动
                wingAngle = WingAngle * MathF.Sin(MathF.PI * 2f * (_flyPhase + 0.75f));

                // 站立时的小摆动
                if (_isOnGround) {
                    wingAngle += WingGroundAngle * MathF.Sin(MathF.PI * 2f * _phase);
                }
            }

            // Body 骨骼
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                boneTransforms[bodyBone.Index] = Matrix.CreateFromYawPitchRoll(_rotation.X, 0f, 0f)
                    * Matrix.CreateTranslation(_position.X, _position.Y + _bob, _position.Z);
            }

            // 翅膀骨骼
            if (hasWings) {
                boneTransforms[wing1Bone.Index] = Matrix.CreateRotationY(wingAngle);
                boneTransforms[wing2Bone.Index] = Matrix.CreateRotationY(-wingAngle);
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
            // 原始代码:
            // yaw = LookAngleX / 2, yaw2 = LookAngleX / 2
            // num4 = 0, num5 = 0
            // if (Standing || Immersion > 0): num4 = 0.5 * Sin(π * 2 * Phase / 2), num5 = -num4
            // Head: CreateFromYawPitchRoll(yaw, num5 + Clamp(vector.Y, -π/4, π/4), vector.Z)
            // Neck: CreateFromYawPitchRoll(yaw2, num4 + LookAngleY, 0)
            ModelBone neckBone = model.FindBone("Neck", false);
            bool hasNeck = neckBone != null;
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone != null) {
                float yaw = _lookAngleX / 2f;
                float num4 = 0f;

                // 站立时颈部摆动，头部取反
                if (_isOnGround || _immersionFactor > 0f) {
                    // 原始: num4 = 0.5f * Sin(π * 2f * MovementPhase / 2f) = Sin(π * MovementPhase)
                    num4 = 0.5f * MathF.Sin(MathF.PI * _phase);
                }
                float num5 = -num4;
                boneTransforms[headBone.Index] = Matrix.CreateFromYawPitchRoll(
                    yaw,
                    num5 + Math.Clamp(_rotation.Y, -(float)Math.PI / 4f, (float)Math.PI / 4f),
                    _rotation.Z
                );
            }
            if (hasNeck) {
                float yaw2 = _lookAngleX / 2f;
                float num4 = 0f;

                // 站立时颈部摆动
                if (_isOnGround || _immersionFactor > 0f) {
                    num4 = 0.5f * MathF.Sin(MathF.PI * _phase);
                }
                boneTransforms[neckBone.Index] = Matrix.CreateFromYawPitchRoll(yaw2, num4 + _lookAngleY, 0f);
            }
        }
    }
}