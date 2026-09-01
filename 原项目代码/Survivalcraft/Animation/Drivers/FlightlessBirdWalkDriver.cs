using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 不能飞的鸟类行走驱动器 - 处理行走时的腿部摆动、身体晃动和头部摆动
    /// </summary>
    public class FlightlessBirdWalkDriver : IAnimationDriver {
        public string Name => "FlightlessBirdWalk";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Body", "Leg1", "Leg2", "Head", "Neck"];

        // 参数名称
        public string PhaseParam { get; set; } = "MovementPhase";
        public string BobParam { get; set; } = "Bob";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";
        public string WalkBobHeightParam { get; set; } = "WalkBobHeight";
        public string WalkLegsAngleParam { get; set; } = "WalkLegsAngle";
        public string SpeedParam { get; set; } = "Speed";
        public string WalkSpeedParam { get; set; } = "WalkSpeed";
        public string IsOnGroundParam { get; set; } = "IsOnGround";
        public string ImmersionFactorParam { get; set; } = "ImmersionFactor";

        // 踢腿攻击参数（需要在行走中混合）
        public string KickPhaseParam { get; set; } = "KickPhase";
        public string KickFactorParam { get; set; } = "KickFactor";

        // 输出参数（用于死亡动画）
        public string LastLegAngle1Param { get; set; } = "LastLegAngle1";
        public string LastLegAngle2Param { get; set; } = "LastLegAngle2";

        // 可配置属性
        public float LegAngle { get; set; } = 0.55f; // 默认腿部摆动角度（弧度）
        public float SmoothSpeed { get; set; } = 12f;
        public float HeadBobAngle { get; set; } = 5f; // 头部摆动角度（度）
        public float HeadMaxAngleX { get; set; } = 90f;
        public float HeadMinAngleY { get; set; } = -90f; // 向下最大角度（负值）
        public float HeadMaxAngleY { get; set; } = 50f; // 向上最大角度
        public float HeadRatio { get; set; } = 0.6f;
        public float NeckRatio { get; set; } = 0.4f;

        float _phase;
        float _bob;
        float _rotationY;
        Vector3 _position;
        float _lookAngleX;
        float _lookAngleY;
        float _walkBobHeight;
        float _walkLegsAngle;
        float _speed;
        float _walkSpeed;
        bool _isOnGround;
        float _immersionFactor;

        // 踢腿攻击参数
        float _kickPhase;
        float _kickFactor;

        // 平滑过渡
        float _currentLegAngle1;
        float _currentLegAngle2;
        float _currentHeadAngleY;
        float _deltaTime = 1f / 60f; // 默认帧时间
        bool _firstUpdate = true;

        // 保存参数引用（用于输出腿部角度）
        AnimationParameters _parameters;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _parameters = parameters; // 保存参数引用（用于在 SampleTransforms 中输出）
            _deltaTime = MathUtils.Max(deltaTime, 0.001f); // 保存帧时间，最小值 0.001
            _phase = parameters.GetFloat(PhaseParam);
            _bob = parameters.GetFloat(BobParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);
            _lookAngleX = parameters.GetFloat(LookAngleXParam);
            _lookAngleY = parameters.GetFloat(LookAngleYParam);
            _walkBobHeight = parameters.GetFloat(WalkBobHeightParam);
            _walkLegsAngle = parameters.GetFloat(WalkLegsAngleParam);
            _speed = parameters.GetFloat(SpeedParam);
            _walkSpeed = parameters.GetFloat(WalkSpeedParam);
            _isOnGround = parameters.GetBool(IsOnGroundParam);
            _immersionFactor = parameters.GetFloat(ImmersionFactorParam);

            // 踢腿攻击参数
            _kickPhase = parameters.GetFloat(KickPhaseParam);
            _kickFactor = parameters.GetFloat(KickFactorParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // 计算腿部角度
            float legAngle1 = 0f;
            float legAngle2 = 0f;
            float headBobY = 0f;
            if (_phase != 0f
                && (_isOnGround || _immersionFactor > 0f)) {
                // 速度超过 0.75 * WalkSpeed 时角度增大 1.5 倍
                float legAngle = _walkLegsAngle > 0 ? _walkLegsAngle : LegAngle;
                if (_speed > 0.75f * _walkSpeed) {
                    legAngle *= 1.5f;
                }

                // 腿部交替摆动，相位差 0.5
                // 原始代码: num = num4 * num5 + m_kickPhase
                // 注意：Leg1 会加上 kickPhase（用于与攻击动画混合）
                legAngle1 = legAngle * MathF.Sin(MathF.PI * 2f * (_phase + 0f)) + _kickPhase;
                legAngle2 = legAngle * MathF.Sin(MathF.PI * 2f * (_phase + 0.5f));

                // 头部轻微上下摆动 (5度)
                headBobY = MathUtils.DegToRad(HeadBobAngle) * MathF.Sin(MathF.PI * 4f * _phase);
            }

            // 踢腿攻击动画混合
            // 原始代码: if (m_kickFactor != 0f) num = Lerp(num, kickAngle, kickFactor)
            if (_kickFactor > 0f) {
                float kickAngle = MathUtils.DegToRad(60f) * MathF.Sin(MathF.PI * MathUtils.Sigmoid(_kickPhase, 5f));
                legAngle1 = MathUtils.Lerp(legAngle1, kickAngle, _kickFactor);
            }

            // 平滑过渡
            float smoothFactor = MathUtils.Min(SmoothSpeed * _deltaTime, 1f);
            if (_firstUpdate) {
                _currentLegAngle1 = legAngle1;
                _currentLegAngle2 = legAngle2;
                _currentHeadAngleY = headBobY;
                _firstUpdate = false;
            }
            else {
                _currentLegAngle1 += smoothFactor * (legAngle1 - _currentLegAngle1);
                _currentLegAngle2 += smoothFactor * (legAngle2 - _currentLegAngle2);
                _currentHeadAngleY += smoothFactor * (headBobY - _currentHeadAngleY);
            }

            // Body 骨骼
            ModelBone bodyBone = model.FindBone("Body");
            if (bodyBone != null) {
                boneTransforms[bodyBone.Index] = Matrix.CreateRotationY(_rotationY)
                    * Matrix.CreateTranslation(_position.X, _position.Y + _bob, _position.Z);
            }

            // 腿部骨骼
            ModelBone leg1Bone = model.FindBone("Leg1");
            if (leg1Bone != null) {
                boneTransforms[leg1Bone.Index] = Matrix.CreateRotationX(_currentLegAngle1);
            }
            ModelBone leg2Bone = model.FindBone("Leg2");
            if (leg2Bone != null) {
                boneTransforms[leg2Bone.Index] = Matrix.CreateRotationX(_currentLegAngle2);
            }

            // 头部和颈部
            ModelBone neckBone = model.FindBone("Neck", false);
            bool hasNeck = neckBone != null;
            ModelBone headBone = model.FindBone("Head");
            if (headBone != null) {
                float maxAngleX = MathUtils.DegToRad(HeadMaxAngleX);
                float minAngleY = MathUtils.DegToRad(HeadMinAngleY); // 不对称限制
                float maxAngleY = MathUtils.DegToRad(HeadMaxAngleY);

                // 原始代码: vector2.Y += m_headAngleY; 然后再分配比例
                // 先将头部摆动加到 lookAngleY 上
                float totalLookAngleY = _lookAngleY + _currentHeadAngleY;
                float totalLookAngleX = _lookAngleX;

                // 限制角度（Y轴使用不对称限制）
                totalLookAngleX = Math.Clamp(totalLookAngleX, -maxAngleX, maxAngleX);
                totalLookAngleY = Math.Clamp(totalLookAngleY, minAngleY, maxAngleY);
                float headAngleX = totalLookAngleX;
                float headAngleY = totalLookAngleY;
                if (hasNeck) {
                    // 原始代码: vector2 = 0.6f * vector2 (头部 60%)
                    headAngleX = totalLookAngleX * HeadRatio;
                    headAngleY = totalLookAngleY * HeadRatio;
                }
                boneTransforms[headBone.Index] = Matrix.CreateRotationX(headAngleY) * Matrix.CreateRotationZ(-headAngleX);
            }
            if (hasNeck) {
                float maxAngleX = MathUtils.DegToRad(HeadMaxAngleX);
                float minAngleY = MathUtils.DegToRad(HeadMinAngleY); // 不对称限制
                float maxAngleY = MathUtils.DegToRad(HeadMaxAngleY);

                // 原始代码: vector3 = 0.4f * vector2 (颈部 40%)
                float totalLookAngleY = _lookAngleY + _currentHeadAngleY;
                float totalLookAngleX = _lookAngleX;
                totalLookAngleX = Math.Clamp(totalLookAngleX, -maxAngleX, maxAngleX);
                totalLookAngleY = Math.Clamp(totalLookAngleY, minAngleY, maxAngleY);
                float neckAngleX = totalLookAngleX * NeckRatio;
                float neckAngleY = totalLookAngleY * NeckRatio;
                boneTransforms[neckBone.Index] = Matrix.CreateRotationX(neckAngleY) * Matrix.CreateRotationZ(-neckAngleX);
            }

            // 输出腿部角度到参数（用于死亡动画）
            _parameters?.SetFloat(LastLegAngle1Param, _currentLegAngle1);
            _parameters?.SetFloat(LastLegAngle2Param, _currentLegAngle2);
        }
    }
}