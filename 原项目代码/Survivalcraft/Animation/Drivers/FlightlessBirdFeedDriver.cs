using Engine;
using Engine.Animation;
using Engine.Graphics;

namespace Game.Animation.Drivers {
    /// <summary>
    /// 不能飞的鸟类进食驱动器 - 处理进食时的头部动画
    /// </summary>
    public class FlightlessBirdFeedDriver : IAnimationDriver {
        public string Name => "FlightlessBirdFeed";
        public AnimationBlendMode BlendMode => AnimationBlendMode.Override;

        public string[] TargetBones => mTargetBones;
        string[] mTargetBones = ["Head", "Neck"];

        // 参数名称
        public string FeedFactorParam { get; set; } = "FeedFactor";
        public string LookAngleXParam { get; set; } = "LookAngleX";
        public string LookAngleYParam { get; set; } = "LookAngleY";
        public string GameTimeParam { get; set; } = "GameTime";
        public string MovementPhaseParam { get; set; } = "MovementPhase"; // 用于计算头部摆动
        public string GameTimeDeltaParam { get; set; } = "GameTimeDelta"; // 用于平滑过渡

        // 可配置属性
        public float MinPeckAngle { get; set; } = 35f; // 最小啄食角度（度）
        public float MaxPeckAngle { get; set; } = 55f; // 最大啄食角度增量（度）
        public float HeadBobAngle { get; set; } = 5f; // 头部摆动角度（度）
        public float HeadMaxAngleX { get; set; } = 90f;
        public float HeadMaxAngleY { get; set; } = 50f; // 向上的最大角度（度）
        public float HeadMinAngleY { get; set; } = -90f; // 向下的最小角度（度），与原始代码一致
        public float HeadRatio { get; set; } = 0.6f;
        public float NeckRatio { get; set; } = 0.4f;

        float _feedFactor;
        float _lookAngleX;
        float _lookAngleY;
        float _gameTime;
        float _movementPhase;
        float _gameTimeDelta = 1f / 60f; // 默认值

        public void Update(float deltaTime, AnimationParameters parameters) {
            _feedFactor = parameters.GetFloat(FeedFactorParam);
            _lookAngleX = parameters.GetFloat(LookAngleXParam);
            _lookAngleY = parameters.GetFloat(LookAngleYParam);
            _gameTime = parameters.GetFloat(GameTimeParam);
            _movementPhase = parameters.GetFloat(MovementPhaseParam);
            _gameTimeDelta = parameters.GetFloat(GameTimeDeltaParam);
            if (_gameTimeDelta <= 0f) {
                _gameTimeDelta = 1f / 60f; // 防止零或负值
            }
        }

        // 平滑过渡状态（用于头部摆动）
        float _currentHeadAngleY;
        bool _firstUpdate = true;

        public void SampleTransforms(Matrix?[] boneTransforms, Model model) {
            if (_feedFactor <= 0f) {
                _firstUpdate = true; // 重置状态
                return;
            }
            ModelBone neckBone = model.FindBone("Neck", false);
            bool hasNeck = neckBone != null;
            ModelBone headBone = model.FindBone("Head", false);
            if (headBone == null) {
                return;
            }

            // 计算头部摆动角度（与原始代码一致）
            // 原始: num3 = DegToRad(5) * Sin(PI * 4 * MovementAnimationPhase)
            float headBobAngle = MathUtils.DegToRad(HeadBobAngle) * MathF.Sin(MathF.PI * 4f * _movementPhase);

            // 平滑过渡（与原始代码一致）
            // 原始: m_headAngleY += num7 * (num3 - m_headAngleY); num7 = Min(12 * dt, 1)
            if (_firstUpdate) {
                _currentHeadAngleY = headBobAngle;
                _firstUpdate = false;
            }
            else {
                // 使用实际的 GameTimeDelta（原始代码: num7 = Min(12 * GameTimeDelta, 1)）
                float smoothFactor = MathUtils.Min(12f * _gameTimeDelta, 1f);
                _currentHeadAngleY += smoothFactor * (headBobAngle - _currentHeadAngleY);
            }

            // 原始代码逻辑:
            // 1. vector2 = LookAngles
            // 2. vector2.Y += m_headAngleY  // 先加上头部摆动
            // 3. if (feedFactor > 0) vector2 = Lerp(vector2, new Vector2(0, -peckAngle), feedFactor)
            float totalLookAngleY = _lookAngleY + _currentHeadAngleY;

            // 使用 SimplexNoise 生成自然的啄食动作
            // 原始逻辑: y = 0 - DegToRad(35 + 55 * SimplexNoise.OctavedNoise(gameTime, 3f, 2, 2f, 0.75f))
            float noise = SimplexNoise.OctavedNoise(_gameTime, 3f, 2, 2f, 0.75f);
            float peckAngle = MathUtils.DegToRad(MinPeckAngle + MaxPeckAngle * noise);

            // Lerp 混合（原始代码: vector2 = Vector2.Lerp(v1: vector2, v2: new Vector2(0f, y), f: m_feedFactor)）
            float feedAngleX = MathUtils.Lerp(_lookAngleX, 0f, _feedFactor);
            float feedAngleY = MathUtils.Lerp(totalLookAngleY, -peckAngle, _feedFactor);

            // 限制角度（与原始代码一致：X 对称，Y 不对称）
            // 原始: vector2.X = Math.Clamp(vector2.X, -DegToRad(90f), DegToRad(90f));
            // 原始: vector2.Y = Math.Clamp(vector2.Y, -DegToRad(90f), DegToRad(50f));
            float maxAngleX = MathUtils.DegToRad(HeadMaxAngleX);
            float minAngleY = MathUtils.DegToRad(HeadMinAngleY); // -90 度
            float maxAngleY = MathUtils.DegToRad(HeadMaxAngleY); // +50 度
            feedAngleX = Math.Clamp(feedAngleX, -maxAngleX, maxAngleX);
            feedAngleY = Math.Clamp(feedAngleY, minAngleY, maxAngleY);
            float headAngleX = feedAngleX;
            float headAngleY = feedAngleY;
            float neckAngleX = feedAngleX;
            float neckAngleY = feedAngleY;
            if (hasNeck) {
                // 原始代码: vector3 = 0.4f * vector2; vector2 = 0.6f * vector2;
                headAngleX = feedAngleX * HeadRatio;
                headAngleY = feedAngleY * HeadRatio;
                neckAngleX = feedAngleX * NeckRatio;
                neckAngleY = feedAngleY * NeckRatio;
            }
            boneTransforms[headBone.Index] = Matrix.CreateRotationX(headAngleY) * Matrix.CreateRotationZ(-headAngleX);
            if (hasNeck) {
                boneTransforms[neckBone.Index] = Matrix.CreateRotationX(neckAngleY) * Matrix.CreateRotationZ(-neckAngleX);
            }
        }
    }
}