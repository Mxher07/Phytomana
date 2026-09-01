namespace Engine.Animation {
    /// <summary>
    /// 关节限制 - 定义骨骼旋转的角度范围
    /// </summary>
    public class JointLimit {
        /// <summary>
        /// 最小角度（X=Yaw, Y=Pitch, Z=Roll，弧度）
        /// </summary>
        public Vector3 Min { get; set; }

        /// <summary>
        /// 最大角度（弧度）
        /// </summary>
        public Vector3 Max { get; set; }

        /// <summary>
        /// 欧拉角旋转顺序（默认 YXZ，适用于大多数骨骼）
        /// </summary>
        public EulerRotationOrder RotationOrder { get; set; } = EulerRotationOrder.YXZ;

        /// <summary>
        /// 从角度创建关节限制（配置文件加载时调用）
        /// </summary>
        /// <param name="minDeg">最小角度（度）</param>
        /// <param name="maxDeg">最大角度（度）</param>
        /// <param name="order">旋转顺序</param>
        public static JointLimit FromDegrees(Vector3 minDeg, Vector3 maxDeg, EulerRotationOrder order = EulerRotationOrder.YXZ) => new() {
            Min = new Vector3(MathUtils.DegToRad(minDeg.X), MathUtils.DegToRad(minDeg.Y), MathUtils.DegToRad(minDeg.Z)),
            Max = new Vector3(MathUtils.DegToRad(maxDeg.X), MathUtils.DegToRad(maxDeg.Y), MathUtils.DegToRad(maxDeg.Z)),
            RotationOrder = order
        };

        /// <summary>
        /// 应用关节限制到旋转矩阵
        /// </summary>
        public Matrix ApplyLimit(Matrix rotation) {
            // 分解欧拉角
            Vector3 eulerAngles = DecomposeToEulerAngles(rotation, RotationOrder);

            // 应用限制
            eulerAngles.X = Math.Clamp(eulerAngles.X, Min.X, Max.X);
            eulerAngles.Y = Math.Clamp(eulerAngles.Y, Min.Y, Max.Y);
            eulerAngles.Z = Math.Clamp(eulerAngles.Z, Min.Z, Max.Z);

            // 重建旋转矩阵
            return ComposeFromEulerAngles(eulerAngles, RotationOrder);
        }

        /// <summary>
        /// 从旋转矩阵分解欧拉角
        /// </summary>
        static Vector3 DecomposeToEulerAngles(Matrix matrix, EulerRotationOrder order) {
            // 提取旋转部分（忽略位移和缩放）
            if (!matrix.Decompose(out _, out Quaternion quaternion, out _)) {
                return Vector3.Zero;
            }
            return QuaternionToEulerAngles(quaternion, order);
        }

        /// <summary>
        /// 四元数转欧拉角
        /// </summary>
        static Vector3 QuaternionToEulerAngles(Quaternion q, EulerRotationOrder order) {
            float yaw, pitch, roll;
            switch (order) {
                case EulerRotationOrder.YXZ:
                    // YXZ 顺序：先 Y 后 X 再 Z
                    yaw = MathF.Atan2(2f * (q.W * q.Y + q.X * q.Z), 1f - 2f * (q.Y * q.Y + q.X * q.X));
                    pitch = MathF.Asin(Math.Clamp(2f * (q.W * q.X - q.Y * q.Z), -1f, 1f));
                    roll = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.X * q.X + q.Z * q.Z));
                    break;
                case EulerRotationOrder.XYZ:
                    yaw = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));
                    pitch = MathF.Asin(Math.Clamp(2f * (q.W * q.X - q.Y * q.Z), -1f, 1f));
                    roll = MathF.Atan2(2f * (q.W * q.Y + q.X * q.Z), 1f - 2f * (q.X * q.X + q.Z * q.Z));
                    break;
                case EulerRotationOrder.ZXY:
                    yaw = MathF.Atan2(2f * (q.W * q.Y - q.X * q.Z), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));
                    pitch = MathF.Atan2(2f * (q.W * q.X + q.Y * q.Z), 1f - 2f * (q.X * q.X + q.Z * q.Z));
                    roll = MathF.Asin(Math.Clamp(2f * (q.W * q.Z - q.X * q.Y), -1f, 1f));
                    break;
                case EulerRotationOrder.ZYX:
                    yaw = MathF.Atan2(2f * (q.W * q.X + q.Y * q.Z), 1f - 2f * (q.X * q.X + q.Y * q.Y));
                    pitch = MathF.Asin(Math.Clamp(2f * (q.W * q.Y - q.X * q.Z), -1f, 1f));
                    roll = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));
                    break;
                default: yaw = pitch = roll = 0f; break;
            }
            return new Vector3(yaw, pitch, roll);
        }

        /// <summary>
        /// 欧拉角构建旋转矩阵
        /// </summary>
        static Matrix ComposeFromEulerAngles(Vector3 angles, EulerRotationOrder order) {
            float yaw = angles.X;
            float pitch = angles.Y;
            float roll = angles.Z;
            switch (order) {
                case EulerRotationOrder.YXZ: return Matrix.CreateRotationY(yaw) * Matrix.CreateRotationX(pitch) * Matrix.CreateRotationZ(roll);
                case EulerRotationOrder.XYZ: return Matrix.CreateRotationX(pitch) * Matrix.CreateRotationY(yaw) * Matrix.CreateRotationZ(roll);
                case EulerRotationOrder.ZXY: return Matrix.CreateRotationZ(roll) * Matrix.CreateRotationX(pitch) * Matrix.CreateRotationY(yaw);
                case EulerRotationOrder.ZYX: return Matrix.CreateRotationZ(roll) * Matrix.CreateRotationY(yaw) * Matrix.CreateRotationX(pitch);
                default: return Matrix.Identity;
            }
        }
    }
}