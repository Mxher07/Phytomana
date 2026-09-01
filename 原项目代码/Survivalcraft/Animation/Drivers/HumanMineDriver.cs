using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 人类挖掘驱动器 - 处理挖掘方块时的手部动画
    /// </summary>
    public class HumanMineDriver : IAnimationDriver {
        public string Name => "HumanMine";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Additive;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Hand2"];

        // 参数名称
        public string PokingPhaseParam { get; set; } = "PokingPhase";
        public string ActiveBlockValueParam { get; set; } = "ActiveBlockValue";
        public string GameTimeDeltaParam { get; set; } = "GameTimeDelta";

        // 可配置属性
        public float SmoothSpeed { get; set; } = 12f;

        float _pokingPhase;
        int _activeBlockValue;
        float _gameTimeDelta;

        float _currentMineAngle;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _pokingPhase = parameters.GetFloat(PokingPhaseParam);
            _activeBlockValue = (int)parameters.GetFloat(ActiveBlockValueParam);
            _gameTimeDelta = parameters.GetFloat(GameTimeDeltaParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (_pokingPhase <= 0f) {
                return;
            }

            // 原始代码：
            // num9 = ActiveBlockValue == 0 ? 1 * sin(sqrt(PokingPhase) * PI) : 0.3 + 1 * sin(sqrt(PokingPhase) * PI)
            float sinValue = MathF.Sin(MathF.Sqrt(_pokingPhase) * (float)Math.PI);
            float mineAngle = _activeBlockValue == 0 ? 1f * sinValue : 0.3f + 1f * sinValue;

            // 平滑过渡（使用实际的 GameTimeDelta）
            float smoothFactor = MathUtils.Min(SmoothSpeed * _gameTimeDelta, 1f);
            _currentMineAngle += smoothFactor * (mineAngle - _currentMineAngle);

            // 设置 Hand2 骨骼（右手挖掘）
            ModelBone hand2Bone = model.FindBone("Hand2", false);
            if (hand2Bone != null) {
                boneTransforms[hand2Bone.Index] = Matrix.CreateRotationX(_currentMineAngle);
            }
        }
    }
}