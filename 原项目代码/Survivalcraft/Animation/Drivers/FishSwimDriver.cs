using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 鱼类游泳驱动器 - 处理游泳时的尾巴摆动动画
    /// </summary>
    public class FishSwimDriver : IAnimationDriver {
        public string Name => "FishSwim";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Body", "Tail1", "Tail2"];

        // 参数名称
        public string PhaseParam { get; set; } = "MovementPhase";
        public string TailWagPhaseParam { get; set; } = "TailWagPhase";
        public string TailTurnXParam { get; set; } = "TailTurnX";
        public string TailTurnYParam { get; set; } = "TailTurnY";
        public string HasVerticalTailParam { get; set; } = "HasVerticalTail";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";
        public string DigInDepthParam { get; set; } = "DigInDepth";
        public string DigInTailPhaseParam { get; set; } = "DigInTailPhase";
        public string IsEmbeddedInIceParam { get; set; } = "IsEmbeddedInIce";

        // 可配置属性
        public float Tail1Angle { get; set; } = 25f;
        public float Tail2Angle { get; set; } = 30f;
        public float TailPhaseOffset { get; set; } = 0.25f;
        public float SmoothSpeed { get; set; } = 12f;

        float _phase;
        float _tailWagPhase;
        float _tailTurnX;
        float _tailTurnY;
        bool _hasVerticalTail;
        float _rotationY;
        Vector3 _position;
        float _digInDepth;
        float _digInTailPhase;
        bool _isEmbeddedInIce;

        // 平滑过渡用的当前值
        float _currentTail1Z;
        float _currentTail1X;
        float _currentTail2Z;
        float _currentTail2X;
        bool _firstUpdate = true;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _phase = parameters.GetFloat(PhaseParam);
            _tailWagPhase = parameters.GetFloat(TailWagPhaseParam);
            _tailTurnX = parameters.GetFloat(TailTurnXParam);
            _tailTurnY = parameters.GetFloat(TailTurnYParam);
            _hasVerticalTail = parameters.GetBool(HasVerticalTailParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);
            _digInDepth = parameters.GetFloat(DigInDepthParam);
            _digInTailPhase = parameters.GetFloat(DigInTailPhaseParam);
            _isEmbeddedInIce = parameters.GetBool(IsEmbeddedInIceParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            // Body 骨骼
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                // 位置下沉（如果嵌入冰中）
                float yOffset = -_digInDepth;
                boneTransforms[bodyBone.Index] = Matrix.CreateRotationY(_rotationY)
                    * Matrix.CreateTranslation(_position.X, _position.Y + yOffset, _position.Z);
            }

            // 如果嵌入冰中，只设置 Body 骨骼，不处理尾巴动画
            // 原始代码：IsEmbeddedInIce 为 true 时直接返回，不处理尾巴
            if (_isEmbeddedInIce) {
                // 尾巴保持默认姿态，不做任何变换
                return;
            }

            // 计算尾巴摆动角度
            // 原始代码：num2 = digInTailPhase + tailWagPhase
            float extraPhase = _digInTailPhase + _tailWagPhase;
            float tail1Z, tail1X, tail2Z, tail2X;
            if (_hasVerticalTail) {
                // 垂直尾巴：
                // - Z轴（水平摆动）由 extraPhase 控制（不包含游泳相位）
                // - X轴（垂直摆动）由游泳相位控制
                // 原始代码：num3 = sin(2π * num2) - tailTurn.X

                // Z轴：使用 extraPhase（原始代码用 num2 = digInTailPhase + tailWagPhase）
                tail1Z = MathUtils.DegToRad(Tail1Angle) * Math.Clamp(0.5f * MathF.Sin(MathF.PI * 2f * extraPhase) - _tailTurnX, -1f, 1f);
                tail2Z = MathUtils.DegToRad(Tail2Angle)
                    * Math.Clamp(0.5f * MathF.Sin(2f * (MathF.PI * MathUtils.Max(extraPhase - TailPhaseOffset, 0f))) - _tailTurnX, -1f, 1f);

                // X轴：使用游泳相位（原始代码用 MovementAnimationPhase）
                tail1X = MathUtils.DegToRad(Tail1Angle) * Math.Clamp(0.5f * MathF.Sin(MathF.PI * 2f * _phase) - _tailTurnY, -1f, 1f);
                tail2X = MathUtils.DegToRad(Tail2Angle)
                    * Math.Clamp(0.5f * MathF.Sin(MathF.PI * 2f * MathUtils.Max(_phase - TailPhaseOffset, 0f)) - _tailTurnY, -1f, 1f);
            }
            else {
                // 水平尾巴：
                // - Z轴（水平摆动）由游泳相位 + extraPhase 控制
                // - X轴（垂直摆动）只由 tailTurn.Y 控制
                // 原始代码：num3 = sin(2π * (MovementAnimationPhase + num2)) - tailTurn.X
                float combinedPhase = _phase + extraPhase;
                tail1Z = MathUtils.DegToRad(Tail1Angle) * Math.Clamp(0.5f * MathF.Sin(MathF.PI * 2f * combinedPhase) - _tailTurnX, -1f, 1f);
                tail2Z = MathUtils.DegToRad(Tail2Angle)
                    * Math.Clamp(0.5f * MathF.Sin(2f * (MathF.PI * MathUtils.Max(combinedPhase - TailPhaseOffset, 0f))) - _tailTurnX, -1f, 1f);
                tail1X = MathUtils.DegToRad(Tail1Angle) * Math.Clamp(-_tailTurnY, -1f, 1f);
                tail2X = MathUtils.DegToRad(Tail2Angle) * Math.Clamp(-_tailTurnY, -1f, 1f);
            }

            // 平滑过渡
            if (_firstUpdate) {
                _currentTail1Z = tail1Z;
                _currentTail1X = tail1X;
                _currentTail2Z = tail2Z;
                _currentTail2X = tail2X;
                _firstUpdate = false;
            }
            else {
                // 这里不使用平滑过渡，因为尾巴摆动是快速动画
                _currentTail1Z = tail1Z;
                _currentTail1X = tail1X;
                _currentTail2Z = tail2Z;
                _currentTail2X = tail2X;
            }

            // Tail1 骨骼
            ModelBone tail1Bone2 = model.FindBone("Tail1", false);
            if (tail1Bone2 != null) {
                Matrix transform = Matrix.Identity;
                if (_currentTail1Z != 0f) {
                    transform *= Matrix.CreateRotationZ(_currentTail1Z);
                }
                if (_currentTail1X != 0f) {
                    transform *= Matrix.CreateRotationX(_currentTail1X);
                }
                boneTransforms[tail1Bone2.Index] = transform;
            }

            // Tail2 骨骼
            ModelBone tail2Bone2 = model.FindBone("Tail2", false);
            if (tail2Bone2 != null) {
                Matrix transform = Matrix.Identity;
                if (_currentTail2Z != 0f) {
                    transform *= Matrix.CreateRotationZ(_currentTail2Z);
                }
                if (_currentTail2X != 0f) {
                    transform *= Matrix.CreateRotationX(_currentTail2X);
                }
                boneTransforms[tail2Bone2.Index] = transform;
            }
        }
    }
}