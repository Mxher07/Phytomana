using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 人类骑乘驱动器 - 处理骑乘和划船动画
    /// </summary>
    public class HumanRideDriver : IAnimationDriver {
        public string Name => "HumanRide";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Body", "Leg1", "Leg2", "Hand1", "Hand2"];

        // 参数名称
        public string IsRidingParam { get; set; } = "IsRiding";
        public string IsBoatParam { get; set; } = "IsBoat";
        public string RowLeftParam { get; set; } = "RowLeft";
        public string RowRightParam { get; set; } = "RowRight";
        public string RotationYParam { get; set; } = "RotationY";
        public string PositionParam { get; set; } = "Position";
        public string GameTimeParam { get; set; } = "GameTime";
        public string MountBobParam { get; set; } = "MountBob";
        public string GameTimeDeltaParam { get; set; } = "GameTimeDelta";
        public string CrouchFactorParam { get; set; } = "CrouchFactor";

        // 可配置属性
        public float SmoothSpeed { get; set; } = 12f;
        public float RowingSpeed { get; set; } = 6.91150426864624f; // 划船动画速度

        bool _isRiding;
        bool _isBoat;
        bool _rowLeft;
        bool _rowRight;
        float _rotationY;
        Vector3 _position;
        float _gameTime;
        float _mountBob;
        float _gameTimeDelta;
        float _crouchFactor;

        Vector2 _currentHandAngles1 = Vector2.Zero;
        Vector2 _currentHandAngles2 = Vector2.Zero;
        Vector2 _currentLegAngles1 = Vector2.Zero;
        Vector2 _currentLegAngles2 = Vector2.Zero;
        bool _firstUpdate = true;

        public void Update(float deltaTime, AnimationParameters parameters) {
            _isRiding = parameters.GetBool(IsRidingParam);
            _isBoat = parameters.GetBool(IsBoatParam);
            _rowLeft = parameters.GetBool(RowLeftParam);
            _rowRight = parameters.GetBool(RowRightParam);
            _rotationY = parameters.GetFloat(RotationYParam);
            _position = parameters.GetVector3(PositionParam);
            _gameTime = parameters.GetFloat(GameTimeParam);
            _mountBob = parameters.GetFloat(MountBobParam);
            _gameTimeDelta = parameters.GetFloat(GameTimeDeltaParam);
            _crouchFactor = parameters.GetFloat(CrouchFactorParam);
        }

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (!_isRiding) {
                return;
            }

            // 使用实际的 GameTimeDelta
            float smoothFactor = MathUtils.Min(SmoothSpeed * _gameTimeDelta, 1f);

            // 计算腿部和手部角度
            float legAngleY1, legAngleY2;
            float legAngleX1, legAngleX2;
            float handAngleX1, handAngleX2;
            float handAngleY1, handAngleY2;
            if (_isBoat) {
                legAngleX1 = 1.1f;
                legAngleX2 = 1.1f;
                legAngleY1 = 0.2f;
                legAngleY2 = -0.2f;
                handAngleX1 = 0.4f;
                handAngleX2 = 0.4f;
                handAngleY1 = 0.2f;
                handAngleY2 = -0.2f;
            }
            else {
                // 普通骑乘姿势
                legAngleX1 = 0f;
                legAngleX2 = 0f;
                legAngleY1 = 0.55f;
                legAngleY2 = -0.55f;
                handAngleX1 = 0.5f;
                handAngleX2 = 0.5f;
                handAngleY1 = 0.15f;
                handAngleY2 = -0.15f;
            }

            // 划船动画
            if (_rowLeft || _rowRight) {
                float rowTime = (float)Math.Sin(RowingSpeed * _gameTime);
                float rowAngle = 0.6f * rowTime;
                float rowSideAngle = 0.2f + 0.2f * (float)Math.Cos(RowingSpeed * (_gameTime + 0.5));
                if (_rowLeft) {
                    handAngleX1 = rowAngle;
                    handAngleY1 = rowSideAngle;
                }
                if (_rowRight) {
                    handAngleX2 = rowAngle;
                    handAngleY2 = -rowSideAngle;
                }
            }

            // 平滑过渡
            if (_firstUpdate) {
                _currentHandAngles1 = new Vector2(handAngleX1, handAngleY1);
                _currentHandAngles2 = new Vector2(handAngleX2, handAngleY2);
                _currentLegAngles1 = new Vector2(legAngleX1, legAngleY1);
                _currentLegAngles2 = new Vector2(legAngleX2, legAngleY2);
                _firstUpdate = false;
            }
            else {
                _currentHandAngles1 += smoothFactor * (new Vector2(handAngleX1, handAngleY1) - _currentHandAngles1);
                _currentHandAngles2 += smoothFactor * (new Vector2(handAngleX2, handAngleY2) - _currentHandAngles2);
                _currentLegAngles1 += smoothFactor * (new Vector2(legAngleX1, legAngleY1) - _currentLegAngles1);
                _currentLegAngles2 += smoothFactor * (new Vector2(legAngleX2, legAngleY2) - _currentLegAngles2);
            }

            // 身体位置（船上需要调整）
            Vector3 bodyPosition = _position;
            float bodyRotationY = _rotationY;
            if (_isBoat) {
                bodyPosition.Y -= 0.2f;
                bodyRotationY += (float)Math.PI;
            }

            // 设置 Body 骨骼
            ModelBone bodyBone = model.FindBone("Body", false);
            if (bodyBone != null) {
                boneTransforms[bodyBone.Index] = Matrix.CreateRotationY(bodyRotationY)
                    * Matrix.CreateTranslation(bodyPosition.X, bodyPosition.Y + _mountBob, bodyPosition.Z);
            }

            // 设置 Leg 骨骼
            // 蹲下时的腿部变换（平移和缩放）
            float crouchSigmoid = MathUtils.Sigmoid(_crouchFactor, 4f);
            Vector3 legTranslate = new(0f, MathUtils.Lerp(0f, 7f, crouchSigmoid), MathUtils.Lerp(0f, 28f, crouchSigmoid));
            Vector3 legScale = new(1f, 1f, MathUtils.Lerp(1f, 0.5f, crouchSigmoid));
            ModelBone leg1Bone = model.FindBone("Leg1", false);
            if (leg1Bone != null) {
                boneTransforms[leg1Bone.Index] = Matrix.CreateRotationY(_currentLegAngles1.Y)
                    * Matrix.CreateRotationX(_currentLegAngles1.X)
                    * Matrix.CreateTranslation(legTranslate)
                    * Matrix.CreateScale(legScale);
            }
            ModelBone leg2Bone = model.FindBone("Leg2", false);
            if (leg2Bone != null) {
                boneTransforms[leg2Bone.Index] = Matrix.CreateRotationY(_currentLegAngles2.Y)
                    * Matrix.CreateRotationX(_currentLegAngles2.X)
                    * Matrix.CreateTranslation(legTranslate)
                    * Matrix.CreateScale(legScale);
            }

            // 设置 Hand 骨骼
            ModelBone hand1Bone = model.FindBone("Hand1", false);
            if (hand1Bone != null) {
                boneTransforms[hand1Bone.Index] = Matrix.CreateRotationY(_currentHandAngles1.Y) * Matrix.CreateRotationX(_currentHandAngles1.X);
            }
            ModelBone hand2Bone = model.FindBone("Hand2", false);
            if (hand2Bone != null) {
                boneTransforms[hand2Bone.Index] = Matrix.CreateRotationY(_currentHandAngles2.Y) * Matrix.CreateRotationX(_currentHandAngles2.X);
            }
        }
    }
}