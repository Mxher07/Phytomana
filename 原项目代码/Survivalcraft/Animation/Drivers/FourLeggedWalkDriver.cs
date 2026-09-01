using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 四足行走驱动器 - 处理行走动画（Body + Legs + Head 摆动）
    /// 驱动器自己管理相位，根据 Speed 和 DeltaTime 计算 MovementPhase。
    /// 这样可以避免组件和配置文件之间的循环依赖。
    /// </summary>
    public class FourLeggedWalkDriver : IAnimationDriver {
        public string Name => "FourLeggedWalk";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        // 目标骨骼 - 行走时需要控制的所有骨骼
        public string[] TargetBones => mTargetBones;

        string[] mTargetBones = [
            "Body",
            "Leg1",
            "Leg2",
            "Leg3",
            "Leg4",
            "Head",
            "Neck"
        ];

        // 输入参数名称
        public string SpeedParam { get; set; } = "Speed";
        public string DeltaTimeParam { get; set; } = "DeltaTime";
        public string WalkSpeedParam { get; set; } = "WalkSpeed";
        public string FrontAngleParam { get; set; } = "WalkFrontLegsAngle";
        public string HindAngleParam { get; set; } = "WalkHindLegsAngle";
        public string GaitParam { get; set; } = "Gait";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";
        public string CanterLegsAngleFactorParam { get; set; } = "CanterLegsAngleFactor";
        public string IsOnGroundParam { get; set; } = "IsOnGround";
        public string ImmersionFactorParam { get; set; } = "ImmersionFactor";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";
        public string AnimationSpeedParam { get; set; } = "WalkAnimationSpeed";

        // 输出参数名称 - 用于传递给 DeathDriver 和外部读取
        public string PhaseOutputParam { get; set; } = "MovementPhase";
        public string LegAngle1OutputParam { get; set; } = "LegAngle1";
        public string LegAngle2OutputParam { get; set; } = "LegAngle2";
        public string LegAngle3OutputParam { get; set; } = "LegAngle3";
        public string LegAngle4OutputParam { get; set; } = "LegAngle4";

        // ========== 可配置的动画参数 ==========

        // 步态相位偏移 (Leg1, Leg2, Leg3, Leg4)
        public float[] WalkPhases { get; set; } = [0.0f, 0.5f, 0.25f, 0.75f];
        public float[] TrotPhases { get; set; } = [0.0f, 0.5f, 0.5f, 0.0f];
        public float[] CanterPhases { get; set; } = [0.0f, 0.25f, 0.15f, 0.4f];

        // 步态速度系数（从原始组件代码提取）
        // Canter: 0.7f, Trot/Walk: 1.0f
        public float CanterSpeedFactor { get; set; } = 0.7f;
        public float TrotSpeedFactor { get; set; } = 1.0f;
        public float WalkSpeedFactor { get; set; } = 1.0f;

        // 平滑过渡速度
        public float SmoothSpeed { get; set; } = 12f;

        // 头部追踪参数
        public float HeadMaxAngleX { get; set; } = 65f;
        public float HeadMaxAngleY { get; set; } = 55f;
        public float HeadRatio { get; set; } = 0.4f;
        public float NeckRatio { get; set; } = 0.6f;

        // 行走时头部摆动参数
        public float WalkHeadAngle { get; set; } = 3f;
        public float TrotHeadAngle { get; set; } = 3f;
        public float CanterHeadAngle { get; set; } = 8f;
        public float WalkHeadFrequency { get; set; } = 4f;
        public float TrotHeadFrequency { get; set; } = 4f;
        public float CanterHeadFrequency { get; set; } = 2f;

        // Bob 参数
        public string BobHeightParam { get; set; } = "WalkBobHeight";

        // 内部状态
        float _phase;
        float _speed;
        float _deltaTime;
        float _walkSpeed;
        float _animationSpeed = 1f;
        float _frontAngle;
        float _hindAngle;
        int _gait;
        float _rotationY;
        Vector3 _position;
        float _canterLegsAngleFactor;
        bool _isOnGround;
        float _immersionFactor;
        float _lookAngleX;
        float _lookAngleY;
        float _bobHeight;

        // 平滑过渡用的当前角度
        float _legAngle1;
        float _legAngle2;
        float _legAngle3;
        float _legAngle4;
        float _headAngleY;
        float _currentBob;

        // 首次更新标记
        bool _firstUpdate = true;

        public void Update(float deltaTime, AnimationParameters parameters) {
            // 读取输入参数（缺失时使用默认值）
            _speed = parameters.GetFloat(SpeedParam);
            _deltaTime = parameters.GetFloat(DeltaTimeParam);
            _walkSpeed = parameters.GetFloat(WalkSpeedParam);

            // AnimationSpeed 缺失时使用默认值 1.0f
            _animationSpeed = parameters.HasParameter(AnimationSpeedParam) ? parameters.GetFloat(AnimationSpeedParam) : 1.0f;
            _frontAngle = parameters.GetFloat(FrontAngleParam);
            _hindAngle = parameters.GetFloat(HindAngleParam);
            _gait = (int)parameters.GetFloat(GaitParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);
            _canterLegsAngleFactor = parameters.GetFloat(CanterLegsAngleFactorParam);
            _isOnGround = parameters.GetBool(IsOnGroundParam);
            _immersionFactor = parameters.GetFloat(ImmersionFactorParam);
            _lookAngleX = parameters.GetFloat(LookAngleXParam);
            _lookAngleY = parameters.GetFloat(LookAngleYParam);
            _bobHeight = parameters.GetFloat(BobHeightParam);

            // 计算相位增量（根据步态使用不同的速度系数）
            // 原始代码：
            // - Canter: MovementAnimationPhase += speed * dt * 0.7f * m_walkAnimationSpeed
            // - Trot/Walk: MovementAnimationPhase += speed * dt * m_walkAnimationSpeed
            float speedFactor = _gait switch {
                2 => CanterSpeedFactor, // Canter
                1 => TrotSpeedFactor, // Trot
                _ => WalkSpeedFactor // Walk
            };

            // 更新相位（只有移动时才更新）
            if (MathF.Abs(_speed) > 0.2f) {
                _phase += _speed * _deltaTime * speedFactor * _animationSpeed;
            }
            else {
                // 速度太低时重置相位
                _phase = 0f;
            }

            // 输出相位供外部读取（如脚步声计算）
            parameters.SetFloat(PhaseOutputParam, _phase);

            // 计算腿部角度
            float targetAngle1 = 0f, targetAngle2 = 0f, targetAngle3 = 0f, targetAngle4 = 0f;
            if (_phase != 0f
                && (_isOnGround || _immersionFactor > 0f)) {
                // 获取对应步态的相位偏移
                float[] phaseOffsets = _gait switch {
                    2 => CanterPhases,
                    1 => TrotPhases,
                    _ => WalkPhases
                };
                if (_gait == 2) // Canter
                {
                    float factor = _canterLegsAngleFactor > 0 ? _canterLegsAngleFactor : 1.5f;
                    targetAngle1 = _frontAngle * factor * MathF.Sin(2f * MathF.PI * (_phase + phaseOffsets[0]));
                    targetAngle2 = _frontAngle * factor * MathF.Sin(2f * MathF.PI * (_phase + phaseOffsets[1]));
                    targetAngle3 = _hindAngle * factor * MathF.Sin(2f * MathF.PI * (_phase + phaseOffsets[2]));
                    targetAngle4 = _hindAngle * factor * MathF.Sin(2f * MathF.PI * (_phase + phaseOffsets[3]));
                }
                else {
                    targetAngle1 = _frontAngle * MathF.Sin(2f * MathF.PI * (_phase + phaseOffsets[0]));
                    targetAngle2 = _frontAngle * MathF.Sin(2f * MathF.PI * (_phase + phaseOffsets[1]));
                    targetAngle3 = _hindAngle * MathF.Sin(2f * MathF.PI * (_phase + phaseOffsets[2]));
                    targetAngle4 = _hindAngle * MathF.Sin(2f * MathF.PI * (_phase + phaseOffsets[3]));
                }
            }

            // 计算行走时的头部摆动
            float targetHeadY = 0f;
            if (_phase != 0f) {
                targetHeadY = _gait switch {
                    2 => MathUtils.DegToRad(CanterHeadAngle) * MathF.Sin(CanterHeadFrequency * MathF.PI * _phase),
                    1 => MathUtils.DegToRad(TrotHeadAngle) * MathF.Sin(TrotHeadFrequency * MathF.PI * _phase),
                    _ => MathUtils.DegToRad(WalkHeadAngle) * MathF.Sin(WalkHeadFrequency * MathF.PI * _phase)
                };
            }

            // 计算 Bob（根据步态不同）
            float targetBob = 0f;
            if (_phase != 0f) {
                targetBob = _gait switch {
                    2 => -_bobHeight * 1.5f * MathF.Sin(2f * MathF.PI * _phase), // Canter: 正弦，1.5倍
                    1 => _bobHeight * 1.5f * MathUtils.Sqr(MathF.Sin(2f * MathF.PI * _phase)), // Trot: 平方，正向
                    _ => -_bobHeight * MathUtils.Sqr(MathF.Sin(2f * MathF.PI * _phase)) // Walk: 平方，负向
                };
            }

            // 平滑过渡
            float smoothFactor = MathUtils.Min(SmoothSpeed * deltaTime, 1f);

            // 首次更新时直接设置目标值，避免从 0 平滑过渡导致的闪烁
            if (_firstUpdate) {
                _legAngle1 = targetAngle1;
                _legAngle2 = targetAngle2;
                _legAngle3 = targetAngle3;
                _legAngle4 = targetAngle4;
                _headAngleY = targetHeadY;
                _currentBob = targetBob;
                _firstUpdate = false;
            }
            else {
                _legAngle1 += smoothFactor * (targetAngle1 - _legAngle1);
                _legAngle2 += smoothFactor * (targetAngle2 - _legAngle2);
                _legAngle3 += smoothFactor * (targetAngle3 - _legAngle3);
                _legAngle4 += smoothFactor * (targetAngle4 - _legAngle4);
                _headAngleY += smoothFactor * (targetHeadY - _headAngleY);
                _currentBob += smoothFactor * (targetBob - _currentBob);
            }

            // 输出腿部角度供 DeathDriver 使用
            parameters.SetFloat(LegAngle1OutputParam, _legAngle1);
            parameters.SetFloat(LegAngle2OutputParam, _legAngle2);
            parameters.SetFloat(LegAngle3OutputParam, _legAngle3);
            parameters.SetFloat(LegAngle4OutputParam, _legAngle4);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // Body 骨骼（包含位置和旋转）
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                boneTransforms[bodyBone.Index] = Matrix.CreateRotationY(_rotationY)
                    * Matrix.CreateTranslation(_position.X, _position.Y + _currentBob, _position.Z);
            }

            // 腿部骨骼
            for (int i = 0; i < 4; i++) {
                string boneName = $"Leg{i + 1}";
                ModelBone bone = model.FindBone(boneName, false);
                if (bone != null) {
                    float angle = i switch {
                        0 => _legAngle1,
                        1 => _legAngle2,
                        2 => _legAngle3,
                        _ => _legAngle4
                    };
                    boneTransforms[bone.Index] = Matrix.CreateRotationX(angle);
                }
            }

            // 头部和颈部 - 行走时的头部摆动 + 头部追踪
            ModelBone neckBone = model.FindBone("Neck", false);
            bool hasNeck = neckBone != null;
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone != null) {
                float maxAngleX = MathUtils.DegToRad(HeadMaxAngleX);
                float maxAngleY = MathUtils.DegToRad(HeadMaxAngleY);
                float lookAngleX = Math.Clamp(_lookAngleX, -maxAngleX, maxAngleX);
                float lookAngleY = Math.Clamp(_lookAngleY + _headAngleY, -maxAngleY, maxAngleY);
                if (hasNeck) {
                    lookAngleX *= HeadRatio;
                    lookAngleY *= HeadRatio;
                }
                boneTransforms[headBone.Index] = Matrix.CreateRotationX(lookAngleY) * Matrix.CreateRotationZ(-lookAngleX);
            }

            // 颈部动画
            if (hasNeck) {
                float maxAngleX = MathUtils.DegToRad(HeadMaxAngleX);
                float maxAngleY = MathUtils.DegToRad(HeadMaxAngleY);
                // 原始代码: 先 Clamp 总角度，再乘以比例分配
                float totalLookAngleX = Math.Clamp(_lookAngleX, -maxAngleX, maxAngleX);
                float totalLookAngleY = Math.Clamp(_lookAngleY + _headAngleY, -maxAngleY, maxAngleY);
                float lookAngleX = totalLookAngleX * NeckRatio;
                float lookAngleY = totalLookAngleY * NeckRatio;
                boneTransforms[neckBone.Index] = Matrix.CreateRotationX(lookAngleY) * Matrix.CreateRotationZ(-lookAngleX);
            }
        }
    }
}