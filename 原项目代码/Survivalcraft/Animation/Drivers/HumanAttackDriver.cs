using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 人类攻击驱动器 - 处理左右手交替出拳动画
    /// </summary>
    public class HumanAttackDriver : IAnimationDriver {
        public string Name => "HumanAttack";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Additive;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Hand1", "Hand2"];

        // 参数名称
        public string PunchPhaseParam { get; set; } = "PunchPhase";
        public string PunchCounterParam { get; set; } = "PunchCounter";
        public string GameTimeDeltaParam { get; set; } = "GameTimeDelta";

        // 可配置属性
        public float PunchAngle { get; set; } = 90f; // 出拳角度（度）
        public float SmoothSpeed { get; set; } = 12f;

        float _punchPhase;
        int _punchCounter;
        float _gameTimeDelta;

        float _currentPunchAngle1;
        float _currentPunchAngle2;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _punchPhase = parameters.GetFloat(PunchPhaseParam);
            _punchCounter = (int)parameters.GetFloat(PunchCounterParam);
            _gameTimeDelta = parameters.GetFloat(GameTimeDeltaParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (_punchPhase <= 0f) {
                return;
            }

            // 计算出拳角度
            float punchAngle = -MathUtils.DegToRad(PunchAngle) * MathF.Sin((float)Math.PI * 2f * MathUtils.Sigmoid(_punchPhase, 4f));

            // 左右手交替
            bool isLeftPunch = (_punchCounter & 1) == 0;
            float targetAngle1 = isLeftPunch ? punchAngle : 0f;
            float targetAngle2 = isLeftPunch ? 0f : punchAngle;

            // 平滑过渡（使用实际的 GameTimeDelta）
            float smoothFactor = MathUtils.Min(SmoothSpeed * _gameTimeDelta, 1f);
            _currentPunchAngle1 += smoothFactor * (targetAngle1 - _currentPunchAngle1);
            _currentPunchAngle2 += smoothFactor * (targetAngle2 - _currentPunchAngle2);

            // 设置 Hand1 骨骼
            ModelBone hand1Bone = model.FindBone("Hand1", false);
            if (hand1Bone != null) {
                boneTransforms[hand1Bone.Index] = Matrix.CreateRotationX(_currentPunchAngle1);
            }

            // 设置 Hand2 骨骼
            ModelBone hand2Bone = model.FindBone("Hand2", false);
            if (hand2Bone != null) {
                boneTransforms[hand2Bone.Index] = Matrix.CreateRotationX(_currentPunchAngle2);
            }
        }
    }
}