using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 不能飞的鸟类死亡驱动器 - 处理死亡时的侧翻动画
    /// </summary>
    public class FlightlessBirdDeathDriver : IAnimationDriver {
        public string Name => "FlightlessBirdDeath";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Body", "Head", "Neck", "Leg1", "Leg2"];

        // 参数名称
        public string DeathPhaseParam { get; set; } = "DeathPhase";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";
        public string DeathCauseOffsetParam { get; set; } = "DeathCauseOffset";
        public string BodyHeightParam { get; set; } = "BodyHeight";
        public string BodyRightParam { get; set; } = "BodyRight";

        // 死亡时的腿部角度（需要保留最后的角度）
        public string LastLegAngle1Param { get; set; } = "LastLegAngle1";
        public string LastLegAngle2Param { get; set; } = "LastLegAngle2";

        // 可配置属性
        public float DeathRollAngle { get; set; } = 90f; // 死亡侧翻角度（度）
        public float SmoothSpeed { get; set; } = 12f;

        float _deathPhase;
        float _rotationY;
        Vector3 _position;
        Vector3 _deathCauseOffset;
        float _bodyHeight;
        Vector3 _bodyRight;

        // 死亡时的腿部角度
        float _lastLegAngle1;
        float _lastLegAngle2;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _deathPhase = parameters.GetFloat(DeathPhaseParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);
            _deathCauseOffset = parameters.GetVector3(DeathCauseOffsetParam);
            _bodyHeight = parameters.GetFloat(BodyHeightParam);
            _bodyRight = parameters.GetVector3(BodyRightParam);

            // 获取最后的腿部角度
            _lastLegAngle1 = parameters.GetFloat(LastLegAngle1Param);
            _lastLegAngle2 = parameters.GetFloat(LastLegAngle2Param);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (_deathPhase <= 0f) {
                return;
            }

            // 死亡时的倒下系数
            float deathInverse = 1f - _deathPhase;

            // 计算侧翻方向（根据死亡原因偏移）
            // 原始代码: Vector3.Dot(m_componentFrame.Matrix.Right, DeathCauseOffset)
            float rollDirection = _bodyRight.LengthSquared() > 0.001f ? Vector3.Dot(_bodyRight, _deathCauseOffset) > 0f ? 1f : -1f :
                Vector3.Dot(Vector3.UnitX, _deathCauseOffset) > 0f ? 1f : -1f;
            float rollAngle = MathUtils.DegToRad(DeathRollAngle) * _deathPhase * rollDirection;

            // 计算高度（用于下沉动画）
            float bodyHeight = _bodyHeight > 0 ? _bodyHeight : 1f;

            // Body 骨骼 - 侧翻倒下
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                // 原始逻辑:
                // Matrix.CreateTranslation(-0.5 * height * phase * UnitY)
                // * Matrix.CreateFromYawPitchRoll(rotation.X, 0, PI/2 * phase * direction)
                // * Matrix.CreateTranslation(0.2 * height * phase * UnitY)
                // * Matrix.CreateTranslation(position)
                boneTransforms[bodyBone.Index] = Matrix.CreateTranslation(-0.5f * bodyHeight * _deathPhase * Vector3.UnitY)
                    * Matrix.CreateFromYawPitchRoll(_rotationY, 0f, MathF.PI / 2f * _deathPhase * rollDirection)
                    * Matrix.CreateTranslation(0.2f * bodyHeight * _deathPhase * Vector3.UnitY)
                    * Matrix.CreateTranslation(_position);
            }

            // Head 和 Neck 骨骼重置
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone != null) {
                boneTransforms[headBone.Index] = Matrix.Identity;
            }
            ModelBone neckBone = model.FindBone("Neck", false);
            if (neckBone != null) {
                boneTransforms[neckBone.Index] = Matrix.Identity;
            }

            // 腿部逐渐放松（保持最后的角度但逐渐减弱）
            // 原始代码: SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationX(m_legAngle1 * num8));
            // num8 = 1f - DeathPhase
            ModelBone leg1Bone = model.FindBone("Leg1", false);
            if (leg1Bone != null) {
                // 死亡时腿部保持最后的角度但逐渐放松
                boneTransforms[leg1Bone.Index] = Matrix.CreateRotationX(_lastLegAngle1 * deathInverse);
            }
            ModelBone leg2Bone = model.FindBone("Leg2", false);
            if (leg2Bone != null) {
                boneTransforms[leg2Bone.Index] = Matrix.CreateRotationX(_lastLegAngle2 * deathInverse);
            }
        }
    }
}