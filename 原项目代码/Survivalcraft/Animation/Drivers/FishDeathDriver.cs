using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 鱼类死亡驱动器 - 处理死亡时的身体翻转动画
    /// </summary>
    public class FishDeathDriver : IAnimationDriver {
        public string Name => "FishDeath";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Body", "Tail1", "Tail2", "Jaw"];

        // 参数名称
        public string DeathPhaseParam { get; set; } = "DeathPhase";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";
        public string BodyHeightParam { get; set; } = "BodyHeight";

        // 可配置属性
        public float DeathRollAngle { get; set; } = 180f; // 翻转角度
        public float DeathRiseHeight { get; set; } = 1f; // 漂浮高度系数

        float _deathPhase;
        float _rotationY;
        Vector3 _position;
        float _bodyHeight;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _deathPhase = parameters.GetFloat(DeathPhaseParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);
            _bodyHeight = parameters.GetFloat(BodyHeightParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (_deathPhase <= 0f) {
                return;
            }

            // 死亡时身体翻转并向上漂浮
            float rollAngle = MathF.PI * _deathPhase; // 侧翻 180 度
            float riseHeight = DeathRiseHeight * _bodyHeight * _deathPhase;

            // Body 骨骼
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                Vector3 deathPosition = _position + riseHeight * Vector3.UnitY;
                boneTransforms[bodyBone.Index] = Matrix.CreateFromYawPitchRoll(_rotationY, 0f, rollAngle) * Matrix.CreateTranslation(deathPosition);
            }

            // Tail1, Tail2, Jaw 骨骼重置
            ModelBone tail1Bone = model.FindBone("Tail1");
            if (tail1Bone != null) {
                boneTransforms[tail1Bone.Index] = Matrix.Identity;
            }
            ModelBone tail2Bone = model.FindBone("Tail2");
            if (tail2Bone != null) {
                boneTransforms[tail2Bone.Index] = Matrix.Identity;
            }
            ModelBone jawBone = model.FindBone("Jaw", false);
            if (jawBone != null) {
                boneTransforms[jawBone.Index] = Matrix.Identity;
            }
        }
    }
}