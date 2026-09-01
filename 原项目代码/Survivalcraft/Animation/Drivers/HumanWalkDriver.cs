using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 人类行走驱动器 - 处理行走时的腿部摆动、手部摆动、身体晃动和头部追踪
    /// </summary>
    public class HumanWalkDriver : IAnimationDriver {
        public string Name => "HumanWalk";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Body", "Head", "Leg1", "Leg2", "Hand1", "Hand2"];

        // 参数名称
        public string PhaseParam { get; set; } = "MovementPhase";
        public string BobParam { get; set; } = "Bob";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";
        public string WalkLegsAngleParam { get; set; } = "WalkLegsAngle";
        public string WalkBobHeightParam { get; set; } = "WalkBobHeight";
        public string HeadingOffsetParam { get; set; } = "HeadingOffset";
        public string CrouchFactorParam { get; set; } = "CrouchFactor";
        public string IsCreativeFlyParam { get; set; } = "IsCreativeFly";
        public string LastTurnOrderXParam { get; set; } = "LastTurnOrderX";
        public string EntityHashParam { get; set; } = "EntityHash";
        public string VelocityXZParam { get; set; } = "VelocityXZ";
        public string TotalElapsedGameTimeParam { get; set; } = "TotalElapsedGameTime";
        public string GameTimeDeltaParam { get; set; } = "GameTimeDelta";
        public string LieDownFactorParam { get; set; } = "LieDownFactor";

        // 可配置属性
        public float LegAngle { get; set; } = 0.5f;
        public float HandSwingAngle { get; set; } = 0.5f;
        public float SmoothSpeed { get; set; } = 12f;
        public float HeadMaxAngleX { get; set; } = 80f;
        public float HeadMaxAngleY { get; set; } = 45f;
        public float CrouchBodyDrop { get; set; } = 0.7f;
        public float CrouchLegScale { get; set; } = 0.5f;
        public float HandNoiseScale { get; set; } = 0.1f;
        public float FlyVelocityScale { get; set; } = 0.03f;
        public float FlyVelocityMax { get; set; } = 0.5f;

        float _phase;
        float _bob;
        float _rotationY;
        Vector3 _position;
        float _lookAngleX;
        float _lookAngleY;
        float _walkLegsAngle;
        float _walkBobHeight;
        float _headingOffset;
        float _crouchFactor;
        bool _isCreativeFly;
        float _lastTurnOrderX;
        int _entityHash;
        float _velocityXZ;
        float _totalElapsedGameTime;
        float _gameTimeDelta;
        float _lieDownFactor;

        // 平滑过渡
        float _currentBob;
        Vector2 _currentHeadAngles = Vector2.Zero;
        Vector2 _currentHandAngles1 = Vector2.Zero;
        Vector2 _currentHandAngles2 = Vector2.Zero;
        Vector2 _currentLegAngles1 = Vector2.Zero;
        Vector2 _currentLegAngles2 = Vector2.Zero;
        bool _firstUpdate = true;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _phase = parameters.GetFloat(PhaseParam);
            _bob = parameters.GetFloat(BobParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);
            _lookAngleX = parameters.GetFloat(LookAngleXParam);
            _lookAngleY = parameters.GetFloat(LookAngleYParam);
            _walkLegsAngle = parameters.GetFloat(WalkLegsAngleParam);
            _walkBobHeight = parameters.GetFloat(WalkBobHeightParam);
            _headingOffset = parameters.GetFloat(HeadingOffsetParam);
            _crouchFactor = parameters.GetFloat(CrouchFactorParam);
            _isCreativeFly = parameters.GetBool(IsCreativeFlyParam);
            _lastTurnOrderX = parameters.GetFloat(LastTurnOrderXParam);
            _entityHash = (int)parameters.GetFloat(EntityHashParam);
            _velocityXZ = parameters.GetFloat(VelocityXZParam);
            _totalElapsedGameTime = parameters.GetFloat(TotalElapsedGameTimeParam);
            _gameTimeDelta = parameters.GetFloat(GameTimeDeltaParam);
            _lieDownFactor = parameters.GetFloat(LieDownFactorParam);

            // 平滑过渡 Bob
            float smoothFactor = MathUtils.Min(SmoothSpeed * deltaTime, 1f);
            if (_firstUpdate) {
                _currentBob = _bob;
                _firstUpdate = false;
            }
            else {
                _currentBob += smoothFactor * (_bob - _currentBob);
            }

            // ========== 计算并更新角度 ==========
            // 原始代码中这些计算在 AnimateCreature 中，每帧都会执行（即使在躺下时）
            // 这样死亡时的角度值是从上一帧继承的
            float num = MathF.Sin((float)Math.PI * 2f * _phase);
            float noiseTime = (float)MathUtils.Remainder(0.75 * _totalElapsedGameTime + (_entityHash & 0xFFFF), 10000.0);

            // 计算腿部角度
            float legAngleX1 = 0f, legAngleX2 = 0f, legAngleY1 = 0f, legAngleY2 = 0f;
            // 计算手部角度
            float handAngleX1 = 0f, handAngleY1 = 0f, handAngleX2 = 0f, handAngleY2 = 0f;
            if (_isCreativeFly) {
                float velocityOffset = MathUtils.Min(FlyVelocityScale * _velocityXZ * _velocityXZ, FlyVelocityMax);
                legAngleX1 = -0.1f - velocityOffset;
                legAngleX2 = legAngleX1;
                legAngleY1 = MathUtils.Lerp(0f, 0.25f, SimplexNoise.Noise(1.07f * noiseTime + 400f));
                legAngleY2 = 0f - MathUtils.Lerp(0f, 0.25f, SimplexNoise.Noise(0.93f * noiseTime + 500f));
            }
            else if (_phase != 0f) {
                handAngleX1 = -HandSwingAngle * num;
                handAngleX2 = HandSwingAngle * num;
                legAngleX1 = _walkLegsAngle * num;
                legAngleX2 = -legAngleX1;
            }

            // 噪声缩放因子
            float noiseScale = _isCreativeFly ? 4f : 1f;

            // 手部噪声叠加（原始代码：Hand1 X = num2, Hand2 X = 0.9 * num2 + 200）
            handAngleX1 += MathUtils.Lerp(-HandNoiseScale, HandNoiseScale, SimplexNoise.Noise(noiseTime));
            handAngleX2 += MathUtils.Lerp(-HandNoiseScale, HandNoiseScale, SimplexNoise.Noise(0.9f * noiseTime + 200f));
            handAngleY1 += MathUtils.Lerp(0f, noiseScale * 0.15f, SimplexNoise.Noise(1.1f * noiseTime + 100f));
            handAngleY2 += 0f - MathUtils.Lerp(0f, noiseScale * 0.15f, SimplexNoise.Noise(1.05f * noiseTime + 300f));

            // 头部角度
            float headNoiseX = MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.Noise(1.02f * noiseTime - 100f));
            float headNoiseY = MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.Noise(0.96f * noiseTime - 200f));
            float targetHeadX = Math.Clamp(
                headNoiseX + _lookAngleX + 1f * _lastTurnOrderX + _headingOffset,
                -MathUtils.DegToRad(HeadMaxAngleX),
                MathUtils.DegToRad(HeadMaxAngleX)
            );
            float targetHeadY = Math.Clamp(headNoiseY + _lookAngleY, -MathUtils.DegToRad(HeadMaxAngleY), MathUtils.DegToRad(HeadMaxAngleY));

            // 平滑过渡（使用实际的 GameTimeDelta）
            float angleSmoothFactor = MathUtils.Min(SmoothSpeed * _gameTimeDelta, 1f);
            _currentHeadAngles += angleSmoothFactor * (new Vector2(targetHeadX, targetHeadY) - _currentHeadAngles);
            _currentHandAngles1 += angleSmoothFactor * (new Vector2(handAngleX1, handAngleY1) - _currentHandAngles1);
            _currentHandAngles2 += angleSmoothFactor * (new Vector2(handAngleX2, handAngleY2) - _currentHandAngles2);
            _currentLegAngles1 += angleSmoothFactor * (new Vector2(legAngleX1, legAngleY1) - _currentLegAngles1);
            _currentLegAngles2 += angleSmoothFactor * (new Vector2(legAngleX2, legAngleY2) - _currentLegAngles2);

            // 蹲下时腿部角度减半
            if (_crouchFactor == 1f) {
                _currentLegAngles1 *= 0.5f;
                _currentLegAngles2 *= 0.5f;
            }

            // 将角度写入参数，供 HumanDeathDriver 使用
            parameters.SetVector2("HumanHeadAngles", _currentHeadAngles);
            parameters.SetVector2("HumanHandAngles1", _currentHandAngles1);
            parameters.SetVector2("HumanHandAngles2", _currentHandAngles2);
            parameters.SetVector2("HumanLegAngles1", _currentLegAngles1);
            parameters.SetVector2("HumanLegAngles2", _currentLegAngles2);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // 原始代码：if (m_lieDownFactorModel == 0f) { ... } else { 死亡/躺下逻辑 }
            // 如果躺下，由 HumanDeathDriver 处理
            if (_lieDownFactor > 0f) {
                return;
            }

            // 计算蹲下因子
            float crouchSigmoid = MathUtils.Sigmoid(_crouchFactor, 4f);

            // 计算身体位置（考虑蹲下）
            Vector3 bodyPosition = new(_position.X, _position.Y + _currentBob - MathUtils.Lerp(0f, CrouchBodyDrop, crouchSigmoid), _position.Z);


            // 设置 Body 骨骼
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                float bodyRotationY = _rotationY + _headingOffset;
                boneTransforms[bodyBone.Index] = Matrix.CreateRotationY(bodyRotationY) * Matrix.CreateTranslation(bodyPosition);
            }

            // 设置 Head 骨骼
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone != null) {
                boneTransforms[headBone.Index] = Matrix.CreateRotationX(_currentHeadAngles.Y) * Matrix.CreateRotationZ(-_currentHeadAngles.X);
            }

            // 设置 Hand1 骨骼
            ModelBone hand1Bone = model.FindBone("Hand1", false);
            if (hand1Bone != null) {
                boneTransforms[hand1Bone.Index] = Matrix.CreateRotationY(_currentHandAngles1.Y) * Matrix.CreateRotationX(_currentHandAngles1.X);
            }

            // 设置 Hand2 骨骼
            ModelBone hand2Bone = model.FindBone("Hand2", false);
            if (hand2Bone != null) {
                boneTransforms[hand2Bone.Index] = Matrix.CreateRotationY(_currentHandAngles2.Y) * Matrix.CreateRotationX(_currentHandAngles2.X);
            }

            // 腿部平移和缩放（蹲下时）
            Vector3 bodyBoneScale = bodyBone?.Transform.Scale ?? Vector3.One;
            Vector3 legTranslate = new(0f, MathUtils.Lerp(0f, 0.16891f / bodyBoneScale.Y, crouchSigmoid), MathUtils.Lerp(0f, 0.67564f / bodyBoneScale.Z, crouchSigmoid));
            Vector3 legScale = new(1f, 1f, MathUtils.Lerp(1f, CrouchLegScale, crouchSigmoid));

            // 设置 Leg1 骨骼
            ModelBone leg1Bone = model.FindBone("Leg1", false);
            if (leg1Bone != null) {
                boneTransforms[leg1Bone.Index] = Matrix.CreateRotationY(_currentLegAngles1.Y)
                    * Matrix.CreateRotationX(_currentLegAngles1.X)
                    * Matrix.CreateTranslation(legTranslate)
                    * Matrix.CreateScale(legScale);
            }

            // 设置 Leg2 骨骼
            ModelBone leg2Bone = model.FindBone("Leg2", false);
            if (leg2Bone != null) {
                boneTransforms[leg2Bone.Index] = Matrix.CreateRotationY(_currentLegAngles2.Y)
                    * Matrix.CreateRotationX(_currentLegAngles2.X)
                    * Matrix.CreateTranslation(legTranslate)
                    * Matrix.CreateScale(legScale);
            }
        }
    }
}