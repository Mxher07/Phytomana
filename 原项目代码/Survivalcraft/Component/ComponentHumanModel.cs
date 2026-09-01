using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentHumanModel : ComponentCreatureModel {
        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemModelsRenderer m_subsystemModelsRenderer;

        public SubsystemNoise m_subsystemNoise;

        public SubsystemAudio m_subsystemAudio;

        public ComponentMiner m_componentMiner;

        public ComponentRider m_componentRider;

        public ComponentSleep m_componentSleep;

        public ComponentPlayer m_componentPlayer;

        public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new();

        public ModelBone m_bodyBone;

        public ModelBone m_headBone;

        public ModelBone m_leg1Bone;

        public ModelBone m_leg2Bone;

        public ModelBone m_hand1Bone;

        public ModelBone m_hand2Bone;

        public float m_sneakFactor;
        public float m_lieDownFactorEye;

        public float m_lieDownFactorModel;

        public float m_walkAnimationSpeed;

        public float m_walkLegsAngle;

        public float m_walkBobHeight;

        public float m_headingOffset;

        public float m_punchFactor;

        public float m_punchPhase;

        public int m_punchCounter;

        public float m_footstepsPhase;

        public bool m_rowLeft;

        public bool m_rowRight;

        public float m_aimHandAngle;

        public Vector3 m_inHandItemOffset;

        public Vector3 m_inHandItemRotation;

        public Vector2 m_headAngles;

        public Vector2 m_handAngles1;

        public Vector2 m_handAngles2;

        public Vector2 m_legAngles1;

        public Vector2 m_legAngles2;

        public Vector3 m_baseInhandItemOffset;

        public Vector3 m_baseInhandItemRotation;

        public override float AttackPhase {
            get => m_punchPhase;
            set => m_punchPhase = value;
        }

        public override float AttackFactor {
            get => m_punchFactor;
            set => m_punchFactor = value;
        }

        public override void Update(float dt) {
            m_sneakFactor = m_componentCreature.ComponentBody.IsCrouching
                ? MathUtils.Min(m_sneakFactor + 2f * dt, 1f)
                : MathUtils.Max(m_sneakFactor - 2f * dt, 0f);
            if ((m_componentSleep != null && m_componentSleep.IsSleeping)
                || m_componentCreature.ComponentHealth.Health <= 0f) {
                m_lieDownFactorEye = MathUtils.Min(m_lieDownFactorEye + 1f * dt, 1f);
                m_lieDownFactorModel = MathUtils.Min(m_lieDownFactorModel + 3f * dt, 1f);
            }
            else {
                m_lieDownFactorEye = MathUtils.Max(m_lieDownFactorEye - 1f * dt, 0f);
                m_lieDownFactorModel = MathUtils.Max(m_lieDownFactorModel - 3f * dt, 0f);
            }
            bool flag = true;
            bool flag2 = true;
            float footstepsPhase = m_footstepsPhase;
            if (m_componentCreature.ComponentLocomotion.LadderValue.HasValue) {
                m_footstepsPhase += 1.5f * m_walkAnimationSpeed * m_componentCreature.ComponentBody.Velocity.Length() * dt;
                flag2 = false;
            }
            else if (!m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled) {
                float num = m_componentCreature.ComponentLocomotion.SlipSpeed
                    ?? (m_componentCreature.ComponentBody.Velocity.XZ - m_componentCreature.ComponentBody.StandingOnVelocity.XZ).Length();
                if (num > 0.5f) {
                    MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
                    m_footstepsPhase += 1f * m_walkAnimationSpeed * num * dt;
                    flag = false;
                    flag2 = false;
                }
            }
            if (flag) {
                float num2 = 0.5f * MathF.Floor(2f * MovementAnimationPhase);
                if (MovementAnimationPhase != num2) {
                    MovementAnimationPhase = MovementAnimationPhase - num2 > 0.25f
                        ? MathUtils.Min(MovementAnimationPhase + 2f * dt, num2 + 0.5f)
                        : MathUtils.Max(MovementAnimationPhase - 2f * dt, num2);
                }
            }
            if (flag2) {
                m_footstepsPhase = 0f;
            }
            float num3 = 0f;
            ComponentMount componentMount = m_componentRider?.Mount;
            if (componentMount != null) {
                ComponentCreatureModel componentCreatureModel = componentMount.Entity.FindComponent<ComponentCreatureModel>();
                if (componentCreatureModel != null) {
                    Bob = componentCreatureModel.Bob;
                    num3 = Bob;
                }
                m_headingOffset = 0f;
            }
            else {
                float x = MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase);
                num3 = m_walkBobHeight * MathUtils.Sqr(x);
                float num4 = 0f;
                if (m_componentCreature.ComponentLocomotion.LastWalkOrder.HasValue
                    && m_componentCreature.ComponentLocomotion.LastWalkOrder != Vector2.Zero) {
                    num4 = Vector2.Angle(Vector2.UnitY, m_componentCreature.ComponentLocomotion.LastWalkOrder.Value);
                }
                m_headingOffset += MathUtils.NormalizeAngle(num4 - m_headingOffset) * MathUtils.Saturate(8f * m_subsystemTime.GameTimeDelta);
                m_headingOffset = MathUtils.NormalizeAngle(m_headingOffset);
            }
            float num5 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
            Bob += num5 * (num3 - Bob);
            IsAttackHitMoment = false;
            if (AttackOrder) {
                m_punchFactor = MathUtils.Min(m_punchFactor + 4f * dt, 1f);
                float punchPhase = m_punchPhase;
                m_punchPhase = MathUtils.Remainder(m_punchPhase + dt * 2f, 1f);
                if (punchPhase < 0.5f
                    && m_punchPhase >= 0.5f) {
                    IsAttackHitMoment = true;
                    m_punchCounter++;
                }
            }
            else {
                m_punchFactor = MathUtils.Max(m_punchFactor - 4f * dt, 0f);
                if (m_punchPhase != 0f) {
                    if (m_punchPhase > 0.5f) {
                        m_punchPhase = MathUtils.Remainder(MathUtils.Min(m_punchPhase + dt * 2f, 1f), 1f);
                    }
                    else if (m_punchPhase > 0f) {
                        m_punchPhase = MathUtils.Max(m_punchPhase - dt * m_punchPhase, 0f);
                    }
                }
            }
            m_rowLeft = RowLeftOrder;
            m_rowRight = RowRightOrder;
            if ((m_rowLeft || m_rowRight)
                && componentMount != null
                && componentMount.ComponentBody.ImmersionFactor > 0f
                && Math.Floor(1.1 * m_subsystemTime.GameTime)
                != Math.Floor(1.1 * (m_subsystemTime.GameTime - m_subsystemTime.GameTimeDelta))) {
                m_subsystemAudio.PlayRandomSound(
                    "Audio/Rowing",
                    m_random.Float(0.4f, 0.6f),
                    m_random.Float(-0.3f, 0.2f),
                    m_componentCreature.ComponentBody.Position,
                    3f,
                    true
                );
            }
            float num6 = MathF.Floor(m_footstepsPhase);
            if (m_footstepsPhase > num6
                && footstepsPhase <= num6) {
                if (m_componentCreature.ComponentBody.CrouchFactor < 1f) {
                    m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 8f);
                }
                if (!m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f)) {
                    m_footstepsPhase = 0f;
                }
            }
            m_aimHandAngle = AimHandAngleOrder;
            m_inHandItemOffset = Vector3.Lerp(m_inHandItemOffset, InHandItemOffsetOrder, 10f * dt);
            m_inHandItemRotation = Vector3.Lerp(m_inHandItemRotation, InHandItemRotationOrder, 10f * dt);
            AttackOrder = false;
            RowLeftOrder = false;
            RowRightOrder = false;
            AimHandAngleOrder = 0f;
            InHandItemOffsetOrder = Vector3.Zero;
            InHandItemRotationOrder = Vector3.Zero;
            base.Update(dt);
        }

        /// <summary>
        /// 同步动画参数到动画控制器
        /// </summary>
        public override void SyncAnimationParameters() {
            base.SyncAnimationParameters();

            var ctrl = AnimationController;
            if (ctrl == null) return;

            // 行走参数
            ctrl.Parameters.SetFloat("WalkLegsAngle", m_walkLegsAngle);
            ctrl.Parameters.SetFloat("WalkBobHeight", m_walkBobHeight);
            ctrl.Parameters.SetFloat("HeadingOffset", m_headingOffset);
            ctrl.Parameters.SetFloat("CrouchFactor", m_componentCreature.ComponentBody.CrouchFactor);
            ctrl.Parameters.SetFloat("Bob", Bob);

            // 蹲下因子
            ctrl.Parameters.SetFloat("SneakFactor", m_sneakFactor);

            // 躺下因子（模型用）
            ctrl.Parameters.SetFloat("LieDownFactor", m_lieDownFactorModel);

            // 攻击参数
            ctrl.Parameters.SetFloat("PunchPhase", m_punchPhase);
            ctrl.Parameters.SetFloat("PunchCounter", m_punchCounter);
            ctrl.Parameters.SetFloat("PunchFactor", m_punchFactor);

            // 瞄准参数
            ctrl.Parameters.SetFloat("AimHandAngle", m_aimHandAngle);

            // 挖掘参数
            float pokingPhase = m_componentMiner?.PokingPhase ?? 0f;
            int activeBlockValue = m_componentMiner?.ActiveBlockValue ?? 0;
            ctrl.Parameters.SetFloat("PokingPhase", pokingPhase);
            ctrl.Parameters.SetFloat("ActiveBlockValue", activeBlockValue);

            // 骑乘参数
            ComponentMount mount = m_componentRider?.Mount;
            bool isRiding = mount != null;
            ctrl.Parameters.SetBool("IsRiding", isRiding);
            ctrl.Parameters.SetBool("IsBoat", isRiding && mount.Entity.ValuesDictionary.DatabaseObject.Name == "Boat");
            ctrl.Parameters.SetBool("RowLeft", m_rowLeft);
            ctrl.Parameters.SetBool("RowRight", m_rowRight);
            ctrl.Parameters.SetFloat("MountBob", isRiding ? Bob : 0f);

            // 身体前向向量（用于躺下动画）
            var bodyMatrix = m_componentCreature.ComponentBody.Matrix;
            var boundingBox = m_componentCreature.ComponentBody.BoundingBox;
            ctrl.Parameters.SetVector3("BodyForward", bodyMatrix.Forward);
            ctrl.Parameters.SetFloat("BodyHeight", boundingBox.Max.Y - boundingBox.Min.Y);
            ctrl.Parameters.SetFloat("BodyDepth", m_componentCreature.ComponentBody.BoxSize.Z);

            // 头部追踪角度（转换为弧度）
            var lookAngles = m_componentCreature.ComponentLocomotion.LookAngles;
            ctrl.Parameters.SetFloat("LookAngleX", lookAngles.X);
            ctrl.Parameters.SetFloat("LookAngleY", lookAngles.Y);

            // 转向指令（用于头部动画）
            var lastTurnOrder = m_componentCreature.ComponentLocomotion.LastTurnOrder;
            ctrl.Parameters.SetFloat("LastTurnOrderX", lastTurnOrder.X);

            // 创造模式飞行
            ctrl.Parameters.SetBool("IsCreativeFly", m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled);
            ctrl.Parameters.SetFloat("VelocityXZ", m_componentCreature.ComponentBody.Velocity.XZ.Length());
            ctrl.Parameters.SetFloat("VelocityY", m_componentCreature.ComponentBody.Velocity.Y);

            // 在梯子上爬行
            ctrl.Parameters.SetBool("IsOnLadder", m_componentCreature.ComponentLocomotion.LadderValue.HasValue);

            // 实体哈希（用于噪声种子）
            ctrl.Parameters.SetFloat("EntityHash", GetHashCode());

            // 时间参数（用于噪声和插值）
            // 原始代码使用 TotalElapsedGameTime 作为噪声时间基准
            ctrl.Parameters.SetFloat("TotalElapsedGameTime", (float)m_subsystemGameInfo.TotalElapsedGameTime);
            ctrl.Parameters.SetFloat("GameTimeDelta", m_subsystemTime.GameTimeDelta);
            ctrl.Parameters.SetFloat("GameTime", (float)m_subsystemTime.GameTime);  // 用于划船动画

            // 手持物品偏移和旋转
            ctrl.Parameters.SetVector3("InHandItemOffset", m_inHandItemOffset);
            ctrl.Parameters.SetVector3("InHandItemRotation", m_inHandItemRotation);
        }

        public override void AnimateCreature() {
            // 检查模组 hook（保持原有实现）
#pragma warning disable CS0618 // 类型或成员已过时
            if (OnAnimate != null && OnAnimate()) {
#pragma warning restore CS0618 // 类型或成员已过时
                return;
            }

            // 如果使用 AnimationController，动画由驱动器处理
            if (AnimationController != null) {
                return;
            }

            // 后备方案：使用硬编码动画（当没有配置 AnimationController 时）
            AnimateCreatureFallback();
        }

        /// <summary>
        /// 硬编码动画后备方案（当没有 AnimationController 时使用）
        /// 保持原有实现，确保向后兼容
        /// </summary>
        public void AnimateCreatureFallback() {
            Vector3 position = m_componentCreature.ComponentBody.Position;
            Vector3 vector = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();

            if (m_lieDownFactorModel == 0f) {
                ComponentMount componentMount = m_componentRider?.Mount;
                float num = MathF.Sin((float)Math.PI * 2f * MovementAnimationPhase);
                position.Y += Bob;
                vector.X += m_headingOffset;
                float num2 = (float)MathUtils.Remainder(0.75 * m_subsystemGameInfo.TotalElapsedGameTime + (GetHashCode() & 0xFFFF), 10000.0);
                float x = Math.Clamp(
                    MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.Noise(1.02f * num2 - 100f))
                    + m_componentCreature.ComponentLocomotion.LookAngles.X
                    + 1f * m_componentCreature.ComponentLocomotion.LastTurnOrder.X
                    + m_headingOffset,
                    0f - MathUtils.DegToRad(80f),
                    MathUtils.DegToRad(80f)
                );
                float y = Math.Clamp(
                    MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.Noise(0.96f * num2 - 200f)) + m_componentCreature.ComponentLocomotion.LookAngles.Y,
                    0f - MathUtils.DegToRad(45f),
                    MathUtils.DegToRad(45f)
                );
                float num3 = 0f;
                float y2 = 0f;
                float x2 = 0f;
                float y3 = 0f;
                float num4 = 0f;
                float num5 = 0f;
                float num6 = 0f;
                float num7 = 0f;
                if (componentMount != null) {
                    if (componentMount.Entity.ValuesDictionary.DatabaseObject.Name == "Boat") {
                        position.Y -= 0.2f;
                        vector.X += (float)Math.PI;
                        num4 = 0.4f;
                        num6 = 0.4f;
                        num5 = 0.2f;
                        num7 = -0.2f;
                        num3 = 1.1f;
                        x2 = 1.1f;
                        y2 = 0.2f;
                        y3 = -0.2f;
                    }
                    else {
                        num4 = 0.5f;
                        num6 = 0.5f;
                        num5 = 0.15f;
                        num7 = -0.15f;
                        y2 = 0.55f;
                        y3 = -0.55f;
                    }
                }
                else if (m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled) {
                    float num8 = m_componentCreature.ComponentLocomotion.LastWalkOrder.HasValue
                        ? MathUtils.Min(0.03f * m_componentCreature.ComponentBody.Velocity.XZ.LengthSquared(), 0.5f)
                        : 0f;
                    num3 = -0.1f - num8;
                    x2 = num3;
                    y2 = MathUtils.Lerp(0f, 0.25f, SimplexNoise.Noise(1.07f * num2 + 400f));
                    y3 = 0f - MathUtils.Lerp(0f, 0.25f, SimplexNoise.Noise(0.93f * num2 + 500f));
                }
                else if (MovementAnimationPhase != 0f) {
                    num4 = -0.5f * num;
                    num6 = 0.5f * num;
                    num3 = m_walkLegsAngle * num;
                    x2 = 0f - num3;
                }
                float num9 = 0f;
                if (m_componentMiner != null) {
                    float num10 = MathF.Sin(MathF.Sqrt(m_componentMiner.PokingPhase) * (float)Math.PI);
                    num9 = m_componentMiner.ActiveBlockValue == 0 ? 1f * num10 : 0.3f + 1f * num10;
                }
                float num11 = m_punchPhase != 0f
                    ? (0f - MathUtils.DegToRad(90f)) * MathF.Sin((float)Math.PI * 2f * MathUtils.Sigmoid(m_punchPhase, 4f))
                    : 0f;
                float num12 = (m_punchCounter & 1) == 0 ? num11 : 0f;
                float num13 = (m_punchCounter & 1) != 0 ? num11 : 0f;
                float num14 = 0f;
                float num15 = 0f;
                float num16 = 0f;
                float num17 = 0f;
                if (m_rowLeft || m_rowRight) {
                    float num18 = 0.6f * (float)Math.Sin(6.91150426864624 * m_subsystemTime.GameTime);
                    float num19 = 0.2f + 0.2f * (float)Math.Cos(6.91150426864624 * (m_subsystemTime.GameTime + 0.5));
                    if (m_rowLeft) {
                        num14 = num18;
                        num15 = num19;
                    }
                    if (m_rowRight) {
                        num16 = num18;
                        num17 = 0f - num19;
                    }
                }
                float num20 = 0f;
                float num21 = 0f;
                float num22 = 0f;
                float num23 = 0f;
                if (m_aimHandAngle != 0f) {
                    num20 = 1.5f;
                    num21 = -0.7f;
                    num22 = m_aimHandAngle * 1f;
                    num23 = 0f;
                }
                float num24 = !m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled ? 1 : 4;
                num4 += MathUtils.Lerp(-0.1f, 0.1f, SimplexNoise.Noise(num2)) + num12 + num14 + num20;
                num5 += MathUtils.Lerp(0f, num24 * 0.15f, SimplexNoise.Noise(1.1f * num2 + 100f)) + num15 + num21;
                num6 += num9 + MathUtils.Lerp(-0.1f, 0.1f, SimplexNoise.Noise(0.9f * num2 + 200f)) + num13 + num16 + num22;
                num7 += 0f - MathUtils.Lerp(0f, num24 * 0.15f, SimplexNoise.Noise(1.05f * num2 + 300f)) + num17 + num23;
                float s = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
                m_headAngles += s * (new Vector2(x, y) - m_headAngles);
                m_handAngles1 += s * (new Vector2(num4, num5) - m_handAngles1);
                m_handAngles2 += s * (new Vector2(num6, num7) - m_handAngles2);
                m_legAngles1 += s * (new Vector2(num3, y2) - m_legAngles1);
                m_legAngles2 += s * (new Vector2(x2, y3) - m_legAngles2);
                if (m_componentCreature.ComponentBody.CrouchFactor == 1) {
                    m_legAngles1 *= 0.5f;
                    m_legAngles2 *= 0.5f;
                }
                float f = MathUtils.Sigmoid(m_componentCreature.ComponentBody.CrouchFactor, 4f);
                Vector3 position3 = new(position.X, position.Y - MathUtils.Lerp(0f, 0.7f, f), position.Z);
                Vector3 bodyBoneScale = m_bodyBone.Transform.Scale;
                Vector3 position4 = new(0f, MathUtils.Lerp(0f, 0.16891f / bodyBoneScale.Y, f), MathUtils.Lerp(0f, 0.67564f / bodyBoneScale.Z, f));
                Vector3 scale = new(1f, 1f, MathUtils.Lerp(1f, 0.5f, f));
                SetBoneTransform(m_bodyBone.Index, Matrix.CreateRotationY(vector.X) * Matrix.CreateTranslation(position3));
                SetBoneTransform(m_headBone.Index, Matrix.CreateRotationX(m_headAngles.Y) * Matrix.CreateRotationZ(0f - m_headAngles.X));
                SetBoneTransform(m_hand1Bone.Index, Matrix.CreateRotationY(m_handAngles1.Y) * Matrix.CreateRotationX(m_handAngles1.X));
                SetBoneTransform(m_hand2Bone.Index, Matrix.CreateRotationY(m_handAngles2.Y) * Matrix.CreateRotationX(m_handAngles2.X));
                SetBoneTransform(
                    m_leg1Bone.Index,
                    Matrix.CreateRotationY(m_legAngles1.Y)
                    * Matrix.CreateRotationX(m_legAngles1.X)
                    * Matrix.CreateTranslation(position4)
                    * Matrix.CreateScale(scale)
                );
                SetBoneTransform(
                    m_leg2Bone.Index,
                    Matrix.CreateRotationY(m_legAngles2.Y)
                    * Matrix.CreateRotationX(m_legAngles2.X)
                    * Matrix.CreateTranslation(position4)
                    * Matrix.CreateScale(scale)
                );
            }
            else {
                float num25 = MathUtils.Max(DeathPhase, m_lieDownFactorModel);
                float num26 = 1f - num25;
                Vector3 position2 = position
                    + num25
                    * 0.5f
                    * m_componentCreature.ComponentBody.BoxSize.Y
                    * Vector3.Normalize(m_componentCreature.ComponentBody.Matrix.Forward * new Vector3(1f, 0f, 1f))
                    + num25 * Vector3.UnitY * m_componentCreature.ComponentBody.BoxSize.Z * 0.1f;
                SetBoneTransform(
                    m_bodyBone.Index,
                    Matrix.CreateFromYawPitchRoll(vector.X, (float)Math.PI / 2f * num25, 0f) * Matrix.CreateTranslation(position2)
                );
                SetBoneTransform(m_headBone.Index, Matrix.Identity);
                SetBoneTransform(
                    m_hand1Bone.Index,
                    Matrix.CreateRotationY(m_handAngles1.Y * num26) * Matrix.CreateRotationX(m_handAngles1.X * num26)
                );
                SetBoneTransform(
                    m_hand2Bone.Index,
                    Matrix.CreateRotationY(m_handAngles2.Y * num26) * Matrix.CreateRotationX(m_handAngles2.X * num26)
                );
                SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationY(m_legAngles1.Y * num26) * Matrix.CreateRotationX(m_legAngles1.X * num26));
                SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationY(m_legAngles2.Y * num26) * Matrix.CreateRotationX(m_legAngles2.X * num26));
            }
        }

        public override void DrawExtras(Camera camera) {
            base.DrawExtras(camera);
            if (IsExtrasDrawn) {
                return;
            }
            if (m_componentCreature.ComponentHealth.Health > 0f
                && m_componentMiner != null
                && m_componentMiner.ActiveBlockValue != 0) {
                int num = Terrain.ExtractContents(m_componentMiner.ActiveBlockValue);
                Block block = BlocksManager.Blocks[num];
                Matrix m = AbsoluteBoneTransformsForCamera[m_hand2Bone.Index];
                m *= camera.InvertedViewMatrix;
                m.Right = Vector3.Normalize(m.Right);
                m.Up = Vector3.Normalize(m.Up);
                m.Forward = Vector3.Normalize(m.Forward);
                Vector3 InhandRotation = block.GetInHandRotation(m_componentMiner.ActiveBlockValue);
                // 偏移经基础旋转对齐手骨朝向：旋转部分由 baseInhandItemRotation 修正几何朝向（在 R_total 内），
                // 但偏移在 R_total 之后、手骨变换之前应用，不经 baseInhandItemRotation，仅经手骨旋转。
                // 当模型手骨朝向与默认人体模型（Hand2）不同时，偏移方向会随手骨局部坐标偏（如 +Y 由握柄方向变为四指方向）。
                // 预乘 baseRotation 使偏移与几何朝向一样对齐。base=0 时 baseRotation=Identity，行为同原版。
                Matrix baseRotation = Matrix.CreateRotationY(MathUtils.DegToRad(m_baseInhandItemRotation.Y))
                    * Matrix.CreateRotationZ(MathUtils.DegToRad(m_baseInhandItemRotation.Z))
                    * Matrix.CreateRotationX(MathUtils.DegToRad(m_baseInhandItemRotation.X));
                Vector3 alignedOffset = Vector3.Transform(
                    block.GetInHandOffset(m_componentMiner.ActiveBlockValue) + m_inHandItemOffset, baseRotation);
                Matrix matrix = Matrix.CreateRotationY(MathUtils.DegToRad(InhandRotation.Y) + m_inHandItemRotation.Y + MathUtils.DegToRad(m_baseInhandItemRotation.Y))
                    * Matrix.CreateRotationZ(MathUtils.DegToRad(InhandRotation.Z) + m_inHandItemRotation.Z + MathUtils.DegToRad(m_baseInhandItemRotation.Z))
                    * Matrix.CreateRotationX(MathUtils.DegToRad(InhandRotation.X) + m_inHandItemRotation.X + MathUtils.DegToRad(m_baseInhandItemRotation.X))
                    * Matrix.CreateTranslation(alignedOffset)
                    * Matrix.CreateTranslation(m_baseInhandItemOffset * (m_componentCreature.ComponentBody.BoxSize.Y / 1.77f))
                    * m;
                int x = Terrain.ToCell(matrix.Translation.X);
                int y = Terrain.ToCell(matrix.Translation.Y);
                int z = Terrain.ToCell(matrix.Translation.Z);
                m_drawBlockEnvironmentData.DrawBlockMode = DrawBlockMode.ThirdPerson;
                m_drawBlockEnvironmentData.InWorldMatrix = matrix;
                m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
                m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z)
                    + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
                m_drawBlockEnvironmentData.Light = m_subsystemTerrain.Terrain.GetCellLight(x, y, z);
                m_drawBlockEnvironmentData.BillboardDirection = -Vector3.UnitZ;
                m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
                m_drawBlockEnvironmentData.Owner = m_entity;
                Matrix matrix2 = matrix * camera.ViewMatrix;
                block.DrawBlock(
                    m_subsystemModelsRenderer.PrimitivesRenderer,
                    m_componentMiner.ActiveBlockValue,
                    Color.White,
                    block.GetInHandScale(m_componentMiner.ActiveBlockValue),
                    ref matrix2,
                    m_drawBlockEnvironmentData
                );
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemModelsRenderer = Project.FindSubsystem<SubsystemModelsRenderer>(true);
            m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_componentMiner = Entity.FindComponent<ComponentMiner>();
            m_componentRider = Entity.FindComponent<ComponentRider>();
            m_componentSleep = Entity.FindComponent<ComponentSleep>();
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>();
            m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
            m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
            m_walkLegsAngle = valuesDictionary.GetValue<float>("WalkLegsAngle");
            m_baseInhandItemOffset = valuesDictionary.GetValue<Vector3>("BaseInhandItemOffset", new Vector3(0.05f, 0.05f, -0.56f));
            m_baseInhandItemRotation = valuesDictionary.GetValue<Vector3>("BaseInhandItemRotation", Vector3.Zero);
        }

        public override void SetModel(Model model) {
            base.SetModel(model);
            if (IsSet) {
                return;
            }
            if (Model != null) {
                m_bodyBone = Model.FindBone("Body", false);
                m_headBone = Model.FindBone("Head", false);
                m_leg1Bone = Model.FindBone("Leg1", false);
                m_leg2Bone = Model.FindBone("Leg2", false);
                m_hand1Bone = Model.FindBone("Hand1", false);
                m_hand2Bone = Model.FindBone("Hand2", false);
            }
            else {
                m_bodyBone = null;
                m_headBone = null;
                m_leg1Bone = null;
                m_leg2Bone = null;
                m_hand1Bone = null;
                m_hand2Bone = null;
            }

            // 配置驱动器参数
            if (AnimationController != null) {
                AnimationController.Parameters.SetFloat("WalkAnimationSpeed", m_walkAnimationSpeed);
                AnimationController.Parameters.SetFloat("WalkBobHeight", m_walkBobHeight);
                AnimationController.Parameters.SetFloat("WalkLegsAngle", m_walkLegsAngle);

                // 运行时参数初始值
                AnimationController.Parameters.SetFloat("MovementPhase", 0f);
                AnimationController.Parameters.SetFloat("Bob", 0f);
                AnimationController.Parameters.SetFloat("HeadingOffset", 0f);
                AnimationController.Parameters.SetFloat("CrouchFactor", 0f);
                AnimationController.Parameters.SetFloat("SneakFactor", 0f);
                AnimationController.Parameters.SetFloat("LieDownFactor", 0f);
                AnimationController.Parameters.SetFloat("PunchPhase", 0f);
                AnimationController.Parameters.SetFloat("PunchCounter", 0);
                AnimationController.Parameters.SetFloat("PunchFactor", 0f);
                AnimationController.Parameters.SetFloat("AimHandAngle", 0f);
                AnimationController.Parameters.SetFloat("PokingPhase", 0f);
                AnimationController.Parameters.SetFloat("ActiveBlockValue", 0);
                AnimationController.Parameters.SetBool("IsRiding", false);
                AnimationController.Parameters.SetBool("IsBoat", false);
                AnimationController.Parameters.SetBool("RowLeft", false);
                AnimationController.Parameters.SetBool("RowRight", false);
                AnimationController.Parameters.SetFloat("MountBob", 0f);
                AnimationController.Parameters.SetFloat("LastTurnOrderX", 0f);
                AnimationController.Parameters.SetFloat("EntityHash", GetHashCode());
                AnimationController.Parameters.SetBool("IsCreativeFly", false);
                AnimationController.Parameters.SetFloat("VelocityXZ", 0f);
            }
        }

        public override Vector3 CalculateEyePosition() {
            float f = MathUtils.Sigmoid(m_lieDownFactorEye, 1f);
            float num = MathUtils.Sigmoid(m_componentCreature.ComponentBody.CrouchFactor, 4f);
            float num2 = 0.875f * m_componentCreature.ComponentBody.BoxSize.Y;
            float num3 = MathUtils.Lerp(MathUtils.Lerp(num2, 0.45f * num2, num), 0.2f * num2, f);
            Matrix matrix = m_componentCreature.ComponentBody.Matrix;
            Vector3 result = m_componentCreature.ComponentBody.Position + matrix.Up * (num3 + 2f * Bob) + matrix.Forward * -0.09f * num;
            ModsManager.HookAction(
                "RecalculateModelEyePosition",
                loader => {
                    loader.RecalculateModelEyePosition(this, ref result);
                    return false;
                }
            );
            return result;
        }

        public override Quaternion CalculateEyeRotation() {
            float num = 0f;
            if (m_lieDownFactorEye != 0f) {
                num += MathUtils.DegToRad(80f) * MathUtils.Sigmoid(MathUtils.Max(m_lieDownFactorEye - 0.2f, 0f) / 0.8f, 4f);
            }
            Quaternion result = m_componentCreature.ComponentBody.Rotation
                * Quaternion.CreateFromYawPitchRoll(
                    0f - m_componentCreature.ComponentLocomotion.LookAngles.X,
                    m_componentCreature.ComponentLocomotion.LookAngles.Y,
                    num
                );
            ModsManager.HookAction(
                "RecalculateModelEyeRotation",
                loader => {
                    loader.RecalculateModelEyeRotation(this, ref result);
                    return false;
                }
            );
            return result;
        }
    }
}
