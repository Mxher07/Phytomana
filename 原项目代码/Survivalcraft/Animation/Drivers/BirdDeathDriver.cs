using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 鸟类死亡驱动器 - 处理死亡时的侧翻动画
    /// </summary>
    public class BirdDeathDriver : IAnimationDriver {
        public string Name => "BirdDeath";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;

        string[] mTargetBones = [
            "Body",
            "Head",
            "Neck",
            "Wing1",
            "Wing2",
            "Leg1",
            "Leg2"
        ];

        // 参数名称
        public string DeathPhaseParam { get; set; } = "DeathPhase";
        public string RotationParam { get; set; } = "Rotation";
        public string PositionParam { get; set; } = "Position";
        public string BodyForwardParam { get; set; } = "BodyForward";
        public string FlyPhaseParam { get; set; } = "FlyPhase";
        public string IsOnGroundParam { get; set; } = "IsOnGround";
        public string ImmersionFactorParam { get; set; } = "ImmersionFactor";
        public string FlySpeedParam { get; set; } = "FlySpeed";
        public string PhaseParam { get; set; } = "MovementPhase";
        public string BodyHeightParam { get; set; } = "BodyHeight";

        // 可配置属性
        public float DeathPitchAngle { get; set; } = 90f; // 死亡时侧翻角度（度）
        public float WingAngle { get; set; } = 1.2f; // 翅膀基础角度

        float _deathPhase;
        Vector3 _rotation;
        Vector3 _position;
        Vector3 _bodyForward;
        float _flyPhase;
        bool _isOnGround;
        float _immersionFactor;
        float _flySpeed;
        float _phase;
        float _bodyHeight;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _deathPhase = parameters.GetFloat(DeathPhaseParam);
            _rotation = parameters.GetVector3(RotationParam);
            _position = parameters.GetVector3(PositionParam);
            _bodyForward = parameters.GetVector3(BodyForwardParam);
            _flyPhase = parameters.GetFloat(FlyPhaseParam);
            _isOnGround = parameters.GetBool(IsOnGroundParam);
            _immersionFactor = parameters.GetFloat(ImmersionFactorParam);
            _flySpeed = parameters.GetFloat(FlySpeedParam);
            _phase = parameters.GetFloat(PhaseParam);
            _bodyHeight = parameters.GetFloat(BodyHeightParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (_deathPhase <= 0f) {
                return;
            }

            // 检查翅膀
            ModelBone wing1Bone = model.FindBone("Wing1", false);
            ModelBone wing2Bone = model.FindBone("Wing2", false);
            bool hasWings = wing1Bone != null && wing2Bone != null;

            // 计算腿部角度（死亡时保持固定）
            float legAngle1, legAngle2;
            if (_isOnGround
                || _immersionFactor > 0f
                || _flySpeed == 0f) {
                legAngle1 = 0.6f * MathF.Sin(MathF.PI * 2f * _phase);
                legAngle2 = -legAngle1;
            }
            else {
                float flyLegAngle = -MathUtils.DegToRad(60f);
                legAngle1 = flyLegAngle;
                legAngle2 = flyLegAngle;
            }

            // 翅膀角度
            float wingAngle = 0f;
            if (hasWings) {
                wingAngle = WingAngle * MathF.Sin(MathF.PI * 2f * (_flyPhase + 0.75f));
                if (_isOnGround) {
                    wingAngle += 0.3f * MathF.Sin(MathF.PI * 2f * _phase);
                }
            }

            // 死亡时的倒下系数
            float deathInverse = 1f - _deathPhase;

            // Body 骨骼 - 侧翻倒下
            ModelBone bodyBone = model.FindBone("Body");
            if (bodyBone != null) {
                // 使用实际的前向方向（从 Matrix.Forward 传入），与原始代码一致
                // 原始: Vector3.Normalize(m_componentCreature.ComponentBody.Matrix.Forward * new Vector3(1f, 0f, 1f))
                Vector3 horizontalForward = _bodyForward.LengthSquared() > 0.001f
                    ? Vector3.Normalize(_bodyForward * new Vector3(1f, 0f, 1f))
                    : new Vector3(MathF.Sin(_rotation.X), 0f, MathF.Cos(_rotation.X));
                Vector3 deathPosition = _position + 0.5f * _bodyHeight * horizontalForward;
                boneTransforms[bodyBone.Index] = Matrix.CreateFromYawPitchRoll(_rotation.X, MathF.PI / 2f * _deathPhase, 0f)
                    * Matrix.CreateTranslation(deathPosition);
            }

            // Head 和 Neck 骨骼重置
            ModelBone headBone = model.FindBone("Head");
            if (headBone != null) {
                boneTransforms[headBone.Index] = Matrix.Identity;
            }
            ModelBone neckBone = model.FindBone("Neck", false);
            if (neckBone != null) {
                boneTransforms[neckBone.Index] = Matrix.Identity;
            }

            // 翅膀逐渐放松
            if (hasWings) {
                boneTransforms[wing1Bone.Index] = Matrix.CreateRotationY(wingAngle * deathInverse);
                boneTransforms[wing2Bone.Index] = Matrix.CreateRotationY(-wingAngle * deathInverse);
            }

            // 腿部逐渐放松
            ModelBone leg1Bone = model.FindBone("Leg1");
            if (leg1Bone != null) {
                boneTransforms[leg1Bone.Index] = Matrix.CreateRotationX(legAngle1 * deathInverse);
            }
            ModelBone leg2Bone = model.FindBone("Leg2");
            if (leg2Bone != null) {
                boneTransforms[leg2Bone.Index] = Matrix.CreateRotationX(legAngle2 * deathInverse);
            }
        }
    }
}