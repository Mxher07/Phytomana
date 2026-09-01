namespace Engine.Animation.RootMotion {
    /// <summary>
    /// 位移应用器
    /// </summary>
    public class TranslationApplier {
        public Vector3 m_smoothVelocity; // SmoothDamp 内部状态
        public Vector3 m_springVelocity; // SpringDamper 内部状态

        /// <summary>
        /// 重置内部状态（配置变更时调用）
        /// </summary>
        public void Reset() {
            m_smoothVelocity = Vector3.Zero;
            m_springVelocity = Vector3.Zero;
        }

        /// <summary>
        /// 应用根运动位移
        /// </summary>
        /// <param name="config">位移配置</param>
        /// <param name="localVelocity">本地坐标系速度</param>
        /// <param name="localImpulse">本地坐标系冲量（可空）</param>
        /// <param name="entityRotation">实体旋转</param>
        /// <param name="velocity">当前速度（输出）</param>
        /// <param name="deltaTime">帧时间</param>
        public void ApplyTranslation(TranslationConfig config,
            Vector3 localVelocity,
            Vector3? localImpulse,
            Quaternion entityRotation,
            ref Vector3 velocity,
            float deltaTime) {
            if (config == null
                || config.Mode == TranslationMode.None) {
                return;
            }
            switch (config.Mode) {
                case TranslationMode.Blend: {
                    Vector3 worldVelocity = Vector3.Transform(localVelocity, entityRotation);
                    worldVelocity = ClampVelocity(worldVelocity, config.MaxSpeed);
                    ApplyBlend(config, worldVelocity, ref velocity, deltaTime);
                }
                    break;
                case TranslationMode.AddImpulse:
                    if (localImpulse.HasValue) {
                        Vector3 worldImpulse = Vector3.Transform(localImpulse.Value, entityRotation);
                        worldImpulse = ClampVelocity(worldImpulse, config.MaxImpulse);
                        Vector3 delta = worldImpulse * config.ImpulseScale;
                        delta = ApplyVelocityMask(Vector3.Zero, delta, config.VelocityMask);
                        velocity += delta;
                    }
                    break;
                case TranslationMode.Override: {
                    Vector3 worldVelocity = Vector3.Transform(localVelocity, entityRotation);
                    worldVelocity = ClampVelocity(worldVelocity, config.MaxSpeed);
                    velocity = ApplyVelocityMask(velocity, worldVelocity, config.VelocityMask);
                }
                    break;
            }
        }

        public void ApplyBlend(TranslationConfig config, Vector3 targetVelocity, ref Vector3 velocity, float deltaTime) {
            Vector3 current = velocity;
            Vector3 result;
            switch (config.BlendMethod) {
                case BlendMethod.SmoothDamp: result = SmoothDamp(current, targetVelocity, ref m_smoothVelocity, config.SmoothTime, deltaTime); break;
                case BlendMethod.WeightedAverage: result = Vector3.Lerp(current, targetVelocity, config.BlendWeight); break;
                case BlendMethod.SpringDamper:
                    result = SpringDamper(
                        current,
                        targetVelocity,
                        ref m_springVelocity,
                        config.SpringStiffness,
                        config.SpringDamping,
                        deltaTime
                    ); break;
                default: result = targetVelocity; break;
            }
            result = ApplyVelocityMask(current, result, config.VelocityMask);
            velocity = result;
        }

        public static Vector3 ApplyVelocityMask(Vector3 current, Vector3 target, Vector3 mask) => new(
            mask.X > 0.5f ? target.X : current.X,
            mask.Y > 0.5f ? target.Y : current.Y,
            mask.Z > 0.5f ? target.Z : current.Z
        );

        public static Vector3 ClampVelocity(Vector3 velocity, float maxSpeed) {
            if (maxSpeed <= 0) {
                return velocity;
            }
            float speed = velocity.Length();
            if (speed > maxSpeed) {
                return velocity * (maxSpeed / speed);
            }
            return velocity;
        }

        /// <summary>
        /// 平滑阻尼算法（类似 Unity SmoothDamp）
        /// </summary>
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime, float deltaTime) {
            if (smoothTime <= 0) {
                return target;
            }
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            Vector3 change = current - target;
            Vector3 temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            return target + (change + temp) * exp;
        }

        /// <summary>
        /// 弹簧阻尼算法
        /// 弹簧力 = -stiffness * 位移（将物体拉向目标）
        /// 阻尼力 = -damping * 速度（减缓运动）
        /// </summary>
        public static Vector3 SpringDamper(Vector3 current, Vector3 target, ref Vector3 velocity, float stiffness, float damping, float deltaTime) {
            Vector3 displacement = current - target;
            Vector3 springForce = -stiffness * displacement;
            Vector3 dampingForce = -damping * velocity;
            Vector3 acceleration = springForce + dampingForce;
            velocity += acceleration * deltaTime;
            return current + velocity * deltaTime;
        }
    }
}