using Engine;
using GameEntitySystem;
using TemplatesDatabase;
// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace Game {
    /// <summary>
    /// 物理效果禁用位掩码。在物理应用点（非设定点）查询，
    /// 故 NormalMovement 每帧重置 IsGravityEnabled=true 不影响（flag 独立于 boolean 属性）。
    /// </summary>
    [System.Flags]
    public enum PhysicalEffects {
        None = 0,
        /// <summary>禁用重力积分（ComponentBody 重力应用点）</summary>
        Gravity = 1,
        /// <summary>禁用空气阻力（ComponentBody AirDrag 应用点）</summary>
        AirDrag = 2,
        /// <summary>禁用水阻力（ComponentBody WaterDrag 应用点）</summary>
        WaterDrag = 4,
        /// <summary>禁用地面阻力（ComponentBody GroundDrag 应用点）</summary>
        GroundDrag = 8,
    }

    public class ComponentBody : ComponentFrame, IUpdateable {
        public struct CollisionBox {
            public CollisionBox() { }

            public int BlockValue;

            public Vector3 BlockVelocity;

            public ComponentBody ComponentBody;

            public BoundingBox Box;

            /// <summary>
            ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
            /// </summary>
            public ValuesDictionary ValuesDictionaryForMods = new();
        }

        public SubsystemTime m_subsystemTime;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemMovingBlocks m_subsystemMovingBlocks;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

        public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

        public SubsystemPlayers m_subsystemPlayers;

        public Random m_random = new();

        public ComponentHealth m_componentHealth;

        public DynamicArray<CollisionBox> m_collisionBoxes = [];

        public DynamicArray<ComponentBody> m_componentBodies = [];

        public DynamicArray<IMovingBlockSet> m_movingBlockSets = [];

        public DynamicArray<CollisionBox> m_bodiesCollisionBoxes = [];

        public DynamicArray<CollisionBox> m_movingBlocksCollisionBoxes = [];

        public ComponentBody m_parentBody;

        public List<ComponentBody> m_childBodies = [];

        public Vector3 m_velocity;

        public float m_crouchFactor;

        public int m_embeddedInIceCounter;

        public float m_shakingStrength;

        public float m_targetCrouchFactor;

        public Vector3 m_totalImpulse;

        public Vector3 m_directMove;

        public bool m_fluidEffectsPlayed;

        public float m_stoppedTime;

        public static Vector3[] m_freeSpaceOffsets;

        public static bool DrawBodiesBounds;

        public const float SleepThresholdSpeed = 1E-05f;

        public float MaxSpeed = 25f;

        public bool CanCrouch;

        public bool TerrainCollidable = true;

        /// <summary>
        /// 被禁用的物理效果位掩码。由 root motion 物理副作用（ComponentModel.ApplyRootMotionPhysics）设置，
        /// 切出 RM 时由 RestoreRootMotionPhysics 清除全部 RM 相关位（Gravity/AirDrag/WaterDrag/GroundDrag）。
        /// 当前为 RM 物理副作用专用 bitmask；其他特性（buff/状态效果）请用独立机制，勿共享此字段以免 Restore 误清。
        /// </summary>
        public PhysicalEffects DisabledPhysicalEffects;

        public bool BodyCollidable = true;

        public bool FluidCollidable = true;

        public bool IsRaycastTransparent = false; //不可选中

        public static bool ResetVelocityOnProjectLoad = true;
        public virtual Vector3 StanceBoxSize => new(BoxSize.X, (CrouchFactor >= 0.8f ? 0.5f : 1f) * BoxSize.Y, BoxSize.Z);

        public virtual bool CanBePushedByOtherBodies { get; set; } = true;
        public virtual Vector3 BoxSize { get; set; }

        public virtual float Mass { get; set; }

        public virtual float Density { get; set; }

        public virtual Vector2 AirDrag { get; set; }

        public virtual Vector2 WaterDrag { get; set; }

        public virtual float WaterSwayAngle { get; set; }

        public virtual float WaterTurnSpeed { get; set; }

        /// <summary>
        /// 水中摇摆轴
        /// </summary>
        public virtual Vector3 WaterSwayAxis { get; set; }

        /// <summary>
        /// 水中转向轴
        /// </summary>
        public virtual Vector3 WaterTurnAxis { get; set; }

        public bool CanEmbedInIce { get; set; }

        public virtual float ImmersionDepth { get; set; }

        public virtual float ImmersionFactor { get; set; }

        public virtual FluidBlock ImmersionFluidBlock { get; set; }

        public bool IsEmbeddedInIce => m_embeddedInIceCounter >= 2;

        public virtual int? StandingOnValue { get; set; }

        public virtual ComponentBody StandingOnBody { get; set; }

        public virtual Vector3 StandingOnVelocity { get; set; }

        [Obsolete("Use IsCrouching")]
        public virtual bool IsSneaking {
            get => IsCrouching;
            set => IsCrouching = value;
        }

        public virtual bool IsCrouching {
            get => CrouchFactor > 0;
            set => TargetCrouchFactor = value ? 1 : 0;
        }

        public virtual Vector3 Velocity {
            get => m_velocity;
            set {
                if (value.LengthSquared() > MaxSpeed * MaxSpeed) {
                    m_velocity = MaxSpeed * Vector3.Normalize(value);
                }
                else {
                    m_velocity = value;
                }
            }
        }

        public virtual float TargetCrouchFactor {
            get => m_targetCrouchFactor;
            set {
                if (!CanCrouch) {
                    value = 0f;
                }
                m_targetCrouchFactor = value;
            }
        }

        public virtual float CrouchFactor {
            get => m_crouchFactor;
            set {
                if (!CanCrouch) {
                    value = 0f;
                }
                m_crouchFactor = value;
            }
        }

        public float CrushedTime { get; set; }

        public virtual bool IsGravityEnabled { get; set; }

        public virtual bool IsGroundDragEnabled { get; set; }

        public virtual bool IsWaterDragEnabled { get; set; }

        public virtual bool IsSmoothRiseEnabled { get; set; }

        public virtual float MaxSmoothRiseHeight { get; set; }

        public virtual Vector3 CollisionVelocityChange { get; set; }
        public virtual bool CrouchPreventsFalling { get; set; } = true;
        public virtual bool FixCollisionOnRidingBug { get; set; } = true;

        public virtual BoundingBox BoundingBox {
            get {
                Vector3 stanceBoxSize = StanceBoxSize;
                Vector3 position = Position;
                return new BoundingBox(
                    position - new Vector3(stanceBoxSize.X / 2f, 0f, stanceBoxSize.Z / 2f),
                    position + new Vector3(stanceBoxSize.X / 2f, stanceBoxSize.Y, stanceBoxSize.Z / 2f)
                );
            }
        }

        public virtual ReadOnlyList<ComponentBody> ChildBodies => new(m_childBodies);

        public virtual ComponentBody ParentBody {
            get => m_parentBody;
            set {
                if (value != m_parentBody) {
                    if (m_parentBody != null) {
                        m_parentBody.m_childBodies.Remove(this);
                    }
                    m_parentBody = value;
                    if (m_parentBody != null) {
                        m_parentBody.m_childBodies.Add(this);
                    }
                }
            }
        }

        public virtual Vector3 ParentBodyPositionOffset { get; set; }

        public virtual Quaternion ParentBodyRotationOffset { get; set; }

        public virtual float FloatUpdateOrder {
            get {
                if (m_parentBody == null) {
                    return (float)UpdateOrder.Body;
                }
                return m_parentBody.FloatUpdateOrder + 0.01f;
            }
        }

        public virtual Action<ComponentBody> CollidedWithBody { get; set; }

        public virtual Action<IMovingBlockSet> CollidedWithMovingBlock { get; set; }
        public virtual Action<Attackment> Attacked { get; set; }

        static ComponentBody() {
            List<Vector3> list = [];
            for (int i = -2; i <= 2; i++) {
                for (int j = -2; j <= 2; j++) {
                    for (int k = -2; k <= 2; k++) {
                        Vector3 item = new(0.25f * i, 0.25f * j, 0.25f * k);
                        list.Add(item);
                    }
                }
            }
            list.Sort((o1, o2) => Comparer<float>.Default.Compare(o1.LengthSquared(), o2.LengthSquared()));
            m_freeSpaceOffsets = list.ToArray();
        }

        public virtual void ApplyImpulse(Vector3 impulse) {
            m_totalImpulse += impulse;
        }

        public virtual void ApplyDirectMove(Vector3 directMove) {
            m_directMove += directMove;
        }

        public void ApplyShaking(float strength) {
            m_shakingStrength += strength;
        }

        public virtual bool IsChildOfBody(ComponentBody componentBody) {
            if (ParentBody != componentBody) {
                if (ParentBody != null) {
                    return ParentBody.IsChildOfBody(componentBody);
                }
                return false;
            }
            return true;
        }

        public virtual void UnderExplosionStart(Vector3 explosionCenter, float explosionPressure) {
            float num = Vector3.Distance(Position, explosionCenter);
            float num2 = 5f * MathF.Sqrt(explosionPressure);
            float num3 = 1f * MathF.Sqrt(explosionPressure);
            float strength = num2 / (num / num3 + 1f);
            ModsManager.HookAction(
                "OnComponentBodyExplodedStart",
                loader => {
                    loader.OnComponentBodyExplodedStart(this, explosionCenter, explosionPressure, ref strength);
                    return false;
                }
            );
            if (strength > 0) {
                ApplyShaking(strength);
            }
        }

        public virtual void UnderExplosion(Vector3 impulse, float damage) {
            bool setOnFire = true;
            float fluctuation = 0.5f;
            float explosionResilience = m_componentHealth?.ExplosionResilience ?? 1f;
            damage /= explosionResilience;
            impulse /= explosionResilience;
            Injury explosionInjury = new ExplosionInjury(damage);
            ModsManager.HookAction(
                "OnComponentBodyExploded",
                loader => {
                    // ReSharper disable AccessToModifiedClosure
                    loader.OnComponentBodyExploded(this, ref explosionInjury, ref impulse, ref setOnFire, ref fluctuation);
                    // ReSharper restore AccessToModifiedClosure
                    return false;
                }
            );
            impulse *= m_random.Float(1f - fluctuation, 1f + fluctuation);
            damage *= m_random.Float(1f - fluctuation, 1f + fluctuation);
            ApplyImpulse(impulse);
            if (damage > 0f) {
                Entity.FindComponent<ComponentDamage>()?.Damage(damage);
            }
            if (explosionInjury.Amount > 0f) {
                Entity.FindComponent<ComponentHealth>()?.Injure(explosionInjury);
            }
            if (setOnFire) {
                ComponentOnFire componentOnFire = Entity.FindComponent<ComponentOnFire>();
                if (componentOnFire != null
                    && m_random.Float(0f, 1f) < MathUtils.Min(damage - 0.1f, 0.5f)) {
                    componentOnFire.SetOnFire(null, m_random.Float(6f, 8f));
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(true);
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true);
            m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(true);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_componentHealth = Entity.FindComponent<ComponentHealth>();
            CanCrouch = Entity.FindComponent<ComponentPlayer>() != null;
            BoxSize = valuesDictionary.GetValue<Vector3>("BoxSize");
            Mass = valuesDictionary.GetValue<float>("Mass");
            Density = valuesDictionary.GetValue<float>("Density");
            AirDrag = valuesDictionary.GetValue<Vector2>("AirDrag");
            WaterDrag = valuesDictionary.GetValue<Vector2>("WaterDrag");
            WaterSwayAngle = valuesDictionary.GetValue<float>("WaterSwayAngle");
            WaterTurnSpeed = valuesDictionary.GetValue<float>("WaterTurnSpeed");
            WaterSwayAxis = valuesDictionary.GetValue<Vector3>("WaterSwayAxis", Vector3.UnitX);
            WaterTurnAxis = valuesDictionary.GetValue<Vector3>("WaterTurnAxis", Vector3.UnitY);
            CanEmbedInIce = valuesDictionary.GetValue<bool>("CanEmbedInIce");
            Velocity = valuesDictionary.GetValue<Vector3>("Velocity").FixNaN();
            m_embeddedInIceCounter = valuesDictionary.GetValue("EmbeddedInIceCounter", 0);
            if (ResetVelocityOnProjectLoad) {
                Velocity = Vector3.Zero;
            }
            MaxSmoothRiseHeight = valuesDictionary.GetValue<float>("MaxSmoothRiseHeight");
            ParentBody = valuesDictionary.GetValue<EntityReference>("ParentBody").GetComponent<ComponentBody>(Entity, idToEntityMap, false);
            ParentBodyPositionOffset = valuesDictionary.GetValue<Vector3>("ParentBodyPositionOffset");
            ParentBodyRotationOffset = valuesDictionary.GetValue<Quaternion>("ParentBodyRotationOffset");
            IsSmoothRiseEnabled = true;
            IsGravityEnabled = true;
            IsGroundDragEnabled = true;
            IsWaterDragEnabled = true;
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            base.Save(valuesDictionary, entityToIdMap);
            if (Velocity != Vector3.Zero) {
                valuesDictionary.SetValue("Velocity", Velocity.FixNaN());
            }
            valuesDictionary.SetValue("EmbeddedInIceCounter", m_embeddedInIceCounter);
            EntityReference value = EntityReference.FromId(ParentBody);
            if (!value.IsNullOrEmpty()) {
                valuesDictionary.SetValue("ParentBody", value);
                valuesDictionary.SetValue("ParentBodyPositionOffset", ParentBodyPositionOffset);
                valuesDictionary.SetValue("ParentBodyRotationOffset", ParentBodyRotationOffset);
            }
        }

        public override void OnEntityRemoved() {
            ParentBody = null;
            ComponentBody[] array = ChildBodies.ToArray();
            for (int i = 0; i < array.Length; i++) {
                array[i].ParentBody = null;
            }
        }

        public virtual void Update(float dt) {
            bool skipVanilla_ = false;
            ModsManager.HookAction(
                "UpdateComponentBody",
                loader => {
                    loader.UpdateComponentBody(this, dt, skipVanilla_, out bool skipVanilla);
                    skipVanilla_ |= skipVanilla;
                    return false;
                }
            );
            if (skipVanilla_) {
                return;
            }
            CollisionVelocityChange = Vector3.Zero;
            if (m_shakingStrength > 1f) {
                Vector3 vector = default;
                vector.X = m_shakingStrength * MathF.Sin((float)MathUtils.Remainder(31.0 * m_subsystemTime.GameTime, Math.PI * 2));
                vector.Y = 0.3f * m_shakingStrength * MathF.Sin((float)MathUtils.Remainder(23.3 * m_subsystemTime.GameTime, Math.PI * 2));
                vector.Z = m_shakingStrength * MathF.Sin((float)MathUtils.Remainder(27.6 * m_subsystemTime.GameTime, Math.PI * 2));
                Velocity += vector * dt;
                m_shakingStrength *= MathUtils.Saturate(1f - 3.5f * dt);
            }
            else {
                m_shakingStrength = 0f;
            }
            Velocity += m_totalImpulse;
            m_totalImpulse = Vector3.Zero;
            if (m_parentBody != null
                || m_velocity.LengthSquared() > 9.99999944E-11f
                || m_directMove != Vector3.Zero
                || m_targetCrouchFactor != m_crouchFactor) {
                m_stoppedTime = 0f;
            }
            else {
                m_stoppedTime += dt;
                if (m_stoppedTime > 0.5f
                    && !Time.PeriodicEvent(0.25, 0.0)) {
                    return;
                }
            }
            if (m_targetCrouchFactor > m_crouchFactor) {
                m_crouchFactor = MathUtils.Min(m_crouchFactor + 2f * dt, m_targetCrouchFactor);
            }
            if (m_targetCrouchFactor < m_crouchFactor) {
                if (Entity.FindComponent<ComponentRider>().Mount == null) {
                    m_crouchFactor = MathUtils.Max(m_crouchFactor - 2f * dt, m_targetCrouchFactor);
                }
            }
            Vector3 position = Position;
            TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(position.X), Terrain.ToCell(position.Z));
            if (chunkAtCell == null
                || chunkAtCell.State <= TerrainChunkState.InvalidContents4) {
                if (m_subsystemPlayers.PlayerStartedPlaying) {
                    Velocity = Vector3.Zero;
                }
                return;
            }
            if (CanEmbedInIce) {
                BoundingBox boundingBox = BoundingBox;
                m_collisionBoxes.Clear();
                FindTerrainCollisionBoxes(boundingBox, m_collisionBoxes);
                if (IsCollidingWithIce(boundingBox, m_collisionBoxes)) {
                    m_embeddedInIceCounter++;
                }
                else {
                    m_embeddedInIceCounter = 0;
                }
                if (IsEmbeddedInIce) {
                    Velocity = Vector3.Zero;
                    return;
                }
            }
            m_bodiesCollisionBoxes.Clear();
            if (BodyCollidable) {
                FindBodiesCollisionBoxes(position, m_bodiesCollisionBoxes);
            }
            m_movingBlocksCollisionBoxes.Clear();
            if (TerrainCollidable) {
                FindMovingBlocksCollisionBoxes(position, m_movingBlocksCollisionBoxes);
            }
            MoveToFreeSpace(dt);
            if (IsGravityEnabled && (DisabledPhysicalEffects & PhysicalEffects.Gravity) == 0) {
                m_velocity.Y -= 10f * dt;
                if (ImmersionFactor > 0f && FluidCollidable) {
                    float num = ImmersionFactor
                        * (1f + 0.03f * MathF.Sin((float)MathUtils.Remainder(2.0 * m_subsystemTime.GameTime, 6.2831854820251465)));
                    m_velocity.Y += 10f * (1f / Density * num) * dt;
                }
            }
            if ((DisabledPhysicalEffects & PhysicalEffects.AirDrag) == 0) {
                float num2 = MathUtils.Saturate(AirDrag.X * dt);
                float num3 = MathUtils.Saturate(AirDrag.Y * dt);
                m_velocity.X *= 1f - num2;
                m_velocity.Y *= 1f - num3;
                m_velocity.Z *= 1f - num2;
            }
            if (IsWaterDragEnabled
                && (DisabledPhysicalEffects & PhysicalEffects.WaterDrag) == 0
                && ImmersionFactor > 0f
                && ImmersionFluidBlock != null
                && FluidCollidable) {
                Vector2? vector = m_subsystemFluidBlockBehavior.CalculateFlowSpeed(
                    Terrain.ToCell(position.X),
                    Terrain.ToCell(position.Y),
                    Terrain.ToCell(position.Z)
                );
                Vector3 vector2 = vector.HasValue ? new Vector3(vector.Value.X, 0f, vector.Value.Y) : Vector3.Zero;
                float num4 = 1f;
                if (ImmersionFluidBlock.FrictionFactor != 1f) {
                    num4 = SimplexNoise.Noise((float)MathUtils.Remainder(6.0 * Time.FrameStartTime + GetHashCode() % 1000, 1000.0)) > 0.5f
                        ? ImmersionFluidBlock.FrictionFactor
                        : 1f;
                }
                float f = MathUtils.Saturate(WaterDrag.X * num4 * ImmersionFactor * dt);
                float f2 = MathUtils.Saturate(WaterDrag.Y * num4 * dt);
                m_velocity.X = MathUtils.Lerp(m_velocity.X, vector2.X, f);
                m_velocity.Y = MathUtils.Lerp(m_velocity.Y, vector2.Y, f2);
                m_velocity.Z = MathUtils.Lerp(m_velocity.Z, vector2.Z, f);
                if (m_parentBody == null
                    && vector.HasValue
                    && !StandingOnValue.HasValue) {
                    if (WaterTurnSpeed > 0f) {
                        float num5 = MathUtils.Saturate(MathUtils.Lerp(1f, 0f, m_velocity.Length()));
                        Vector2 vector3 = Vector2.Normalize(vector.Value) * num5;
                        Rotation *= Quaternion.CreateFromAxisAngle(WaterTurnAxis, WaterTurnSpeed * (-1f * vector3.X + 0.71f * vector3.Y) * dt);
                    }
                    if (WaterSwayAngle > 0f) {
                        Rotation *= Quaternion.CreateFromAxisAngle(
                            WaterSwayAxis,
                            WaterSwayAngle * (float)Math.Sin(200f / Mass * m_subsystemTime.GameTime)
                        );
                    }
                }
            }
            if (m_parentBody != null) {
                Vector3 vector4 = Vector3.Transform(ParentBodyPositionOffset, m_parentBody.Rotation) + m_parentBody.Position - position;
                m_velocity = dt > 0f ? vector4 / dt : Vector3.Zero;
                Rotation = ParentBodyRotationOffset * m_parentBody.Rotation;
            }
            StandingOnValue = null;
            StandingOnBody = null;
            StandingOnVelocity = Vector3.Zero;
            Vector3 velocity = m_velocity;
            float num6 = m_velocity.Length();
            if ((num6 > 0f || m_directMove != Vector3.Zero)
                && CrushedTime == 0f) {
                Vector3 stanceBoxSize = StanceBoxSize;
                float x = num6 > 0f
                    ? 0.45f * MathUtils.Min(stanceBoxSize.X, stanceBoxSize.Y, stanceBoxSize.Z) / num6
                    : dt;
                float num7 = dt;
                while (num7 > 0f) {
                    float num8 = MathUtils.Min(num7, x);
                    MoveWithCollision(num8, m_velocity * num8 + m_directMove);
                    m_directMove = Vector3.Zero;
                    num7 -= num8;
                }
            }
            if (ParentBody != null && FixCollisionOnRidingBug) {
                CollisionVelocityChange = ParentBody.CollisionVelocityChange;
            }
            else {
                CollisionVelocityChange = m_velocity - velocity;
            }
            if (IsGroundDragEnabled && StandingOnValue.HasValue && (DisabledPhysicalEffects & PhysicalEffects.GroundDrag) == 0) {
                m_velocity = Vector3.Lerp(m_velocity, StandingOnVelocity, 6f * dt);
            }
            UpdateImmersionData();
            if (ImmersionFluidBlock is WaterBlock
                && ImmersionDepth > 0.3f
                && !m_fluidEffectsPlayed) {
                m_fluidEffectsPlayed = true;
                m_subsystemAudio.PlayRandomSound("Audio/WaterFallIn", m_random.Float(0.75f, 1f), m_random.Float(-0.3f, 0f), position, 4f, true);
                m_subsystemParticles.AddParticleSystem(
                    new WaterSplashParticleSystem(m_subsystemTerrain, position, (BoundingBox.Max - BoundingBox.Min).Length() > 0.8f)
                );
            }
            else if (ImmersionFluidBlock is MagmaBlock
                && ImmersionDepth > 0f
                && !m_fluidEffectsPlayed) {
                m_fluidEffectsPlayed = true;
                m_subsystemAudio.PlaySound("Audio/SizzleLong", 1f, 0f, position, 4f, true);
                m_subsystemParticles.AddParticleSystem(
                    new MagmaSplashParticleSystem(m_subsystemTerrain, position, (BoundingBox.Max - BoundingBox.Min).Length() > 0.8f)
                );
            }
            else if (ImmersionFluidBlock == null) {
                m_fluidEffectsPlayed = false;
            }
        }

        public virtual void UpdateImmersionData() {
            Vector3 position = Position;
            int x = Terrain.ToCell(position.X);
            int y = Terrain.ToCell(position.Y + 0.01f);
            int z = Terrain.ToCell(position.Z);
            float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, y, z, out _);
            if (surfaceHeight.HasValue) {
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
                ImmersionDepth = MathUtils.Max(surfaceHeight.Value - position.Y, 0f);
                ImmersionFactor = MathUtils.Saturate(MathF.Pow(ImmersionDepth / StanceBoxSize.Y, 0.7f));
                ImmersionFluidBlock = BlocksManager.FluidBlocks[Terrain.ExtractContents(cellValue)];
            }
            else {
                ImmersionDepth = 0f;
                ImmersionFactor = 0f;
                ImmersionFluidBlock = null;
            }
        }

        public virtual void MoveToFreeSpace(float dt) {
            if (MoveToFreeSpaceHelper(0.5f)) {
                CrushedTime = 0f;
                return;
            }
            if (CrushedTime == 0f) {
                m_subsystemAudio.PlaySound("Audio/Crushed", 1.5f, m_random.Float(-0.2f, 0.2f), Position, 1f, false);
            }
            float targetCrouchFactor = TargetCrouchFactor;
            float crouchFactor = CrouchFactor;
            if (CanCrouch && (TargetCrouchFactor != 1f || CrouchFactor != 1f)) {
                TargetCrouchFactor = 1f;
                CrouchFactor = 1f;
                if (MoveToFreeSpaceHelper(0.5f)) {
                    CrushedTime = 0f;
                    return;
                }
            }
            TargetCrouchFactor = targetCrouchFactor;
            CrouchFactor = crouchFactor;
            if (MoveToFreeSpaceHelper(1f)) {
                CrushedTime = 0f;
                return;
            }
            if (CanCrouch && (TargetCrouchFactor != 1f || CrouchFactor != 1f)) {
                TargetCrouchFactor = 1f;
                CrouchFactor = 1f;
                if (MoveToFreeSpaceHelper(1f)) {
                    CrushedTime = 0f;
                    return;
                }
            }
            CrushedTime += dt;
        }

        public bool IsSpaceFreeToMove(float maxMoveFraction, out Vector3? freePosition, out bool needToTeleport) {
            needToTeleport = false;
            freePosition = null;
            Vector3 stanceBoxSize = StanceBoxSize;
            Vector3 position = Position;
            for (int i = 0; i < m_freeSpaceOffsets.Length; i++) {
                Vector3? vector = null;
                Vector3 positionToDetect = position + m_freeSpaceOffsets[i];
                if (Terrain.ToCell(positionToDetect) != Terrain.ToCell(position)) //不能瞬移到不同的格子，只能在相同的格子中瞬移
                {
                    continue;
                }
                BoundingBox boxThere = new(
                    positionToDetect - new Vector3(stanceBoxSize.X / 2f, 0f, stanceBoxSize.Z / 2f),
                    positionToDetect + new Vector3(stanceBoxSize.X / 2f, stanceBoxSize.Y, stanceBoxSize.Z / 2f)
                );
                //在检测点处玩家的碰撞箱
                boxThere.Min += new Vector3(0.01f, MaxSmoothRiseHeight + 0.01f, 0.01f);
                boxThere.Max -= new Vector3(0.01f);
                m_collisionBoxes.Clear();
                FindTerrainCollisionBoxes(boxThere, m_collisionBoxes);
                m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
                m_collisionBoxes.AddRange(m_bodiesCollisionBoxes);
                if (IsColliding(boxThere, m_collisionBoxes)) //目标碰撞箱存在碰撞
                {
                    m_stoppedTime = 0f;
                    float pushBack_X = CalculatePushBack(boxThere, 0, m_collisionBoxes, out _);
                    float pushBack_Y = CalculatePushBack(boxThere, 1, m_collisionBoxes, out _);
                    float pushBack_Z = CalculatePushBack(boxThere, 2, m_collisionBoxes, out _);
                    float pushBackXLength = pushBack_X * pushBack_X;
                    float pushBackYLength = pushBack_Y * pushBack_Y;
                    float pushBackZLength = pushBack_Z * pushBack_Z;
                    List<Vector3> list = new();
                    //将pushBack后的点，加入待检查的点当中
                    if (pushBackXLength <= pushBackYLength
                        && pushBackXLength <= pushBackZLength) {
                        list.Add(positionToDetect + new Vector3(pushBack_X, 0f, 0f));
                        if (pushBackYLength <= pushBackZLength) {
                            list.Add(positionToDetect + new Vector3(0f, pushBack_Y, 0f));
                            list.Add(positionToDetect + new Vector3(0f, 0f, pushBack_Z));
                        }
                        else {
                            list.Add(positionToDetect + new Vector3(0f, 0f, pushBack_Z));
                            list.Add(positionToDetect + new Vector3(0f, pushBack_Y, 0f));
                        }
                    }
                    else if (pushBackYLength <= pushBackXLength
                        && pushBackYLength <= pushBackZLength) {
                        list.Add(positionToDetect + new Vector3(0f, pushBack_Y, 0f));
                        if (pushBackXLength <= pushBackZLength) {
                            list.Add(positionToDetect + new Vector3(pushBack_X, 0f, 0f));
                            list.Add(positionToDetect + new Vector3(0f, 0f, pushBack_Z));
                        }
                        else {
                            list.Add(positionToDetect + new Vector3(0f, 0f, pushBack_Z));
                            list.Add(positionToDetect + new Vector3(pushBack_X, 0f, 0f));
                        }
                    }
                    else {
                        list.Add(positionToDetect + new Vector3(0f, 0f, pushBack_Z));
                        if (pushBackXLength <= pushBackYLength) {
                            list.Add(positionToDetect + new Vector3(pushBack_X, 0f, 0f));
                            list.Add(positionToDetect + new Vector3(0f, pushBack_Y, 0f));
                        }
                        else {
                            list.Add(positionToDetect + new Vector3(0f, pushBack_Y, 0f));
                            list.Add(positionToDetect + new Vector3(pushBack_X, 0f, 0f));
                        }
                    }
                    foreach (Vector3 item in list) {
                        if (!(MathF.Abs(item.X - position.X) > stanceBoxSize.X * maxMoveFraction)
                            && !(MathF.Abs(item.Y - position.Y) > stanceBoxSize.Y * maxMoveFraction)
                            && !(MathF.Abs(item.Z - position.Z) > stanceBoxSize.Z * maxMoveFraction)) {
                            boxThere = new BoundingBox(
                                item - new Vector3(stanceBoxSize.X / 2f, 0f, stanceBoxSize.Z / 2f),
                                item + new Vector3(stanceBoxSize.X / 2f, stanceBoxSize.Y, stanceBoxSize.Z / 2f)
                            );
                            boxThere.Min += new Vector3(0.02f, MaxSmoothRiseHeight + 0.02f, 0.02f);
                            boxThere.Max -= new Vector3(0.02f);
                            m_collisionBoxes.Clear();
                            FindTerrainCollisionBoxes(boxThere, m_collisionBoxes);
                            m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
                            m_collisionBoxes.AddRange(m_bodiesCollisionBoxes);
                            if (!IsColliding(boxThere, m_collisionBoxes)) {
                                vector = item;
                                needToTeleport = true;
                                break;
                            }
                        }
                    }
                }
                else {
                    //和目标位置没有碰撞，说明目标位置可以装人
                    vector = positionToDetect;
                }
                if (vector.HasValue) {
                    freePosition = vector.Value;
                    return true;
                }
            }
            return false;
        }

        public bool MoveToFreeSpaceHelper(float maxMoveFraction) {
            IsSpaceFreeToMove(maxMoveFraction, out Vector3? freePosition, out bool _);
            if (freePosition.HasValue) {
                Position = freePosition.Value;
                return true;
            }
            return false;
        }

        public virtual void MoveWithCollision(float dt, Vector3 move) {
            Vector3 position = Position;
            bool isSmoothRising = IsSmoothRiseEnabled && MaxSmoothRiseHeight > 0f && HandleSmoothRise(ref move, position, dt);
            HandleAxisCollision(1, move.Y, ref position, isSmoothRising);
            HandleAxisCollision(0, move.X, ref position, isSmoothRising);
            HandleAxisCollision(2, move.Z, ref position, isSmoothRising);
            Position = position;
        }

        public virtual bool HandleSmoothRise(ref Vector3 move, Vector3 position, float dt) {
            Vector3 stanceBoxSize = StanceBoxSize;
            BoundingBox box = new(
                position - new Vector3(stanceBoxSize.X / 2f, 0f, stanceBoxSize.Z / 2f),
                position + new Vector3(stanceBoxSize.X / 2f, stanceBoxSize.Y, stanceBoxSize.Z / 2f)
            );
            box.Min += new Vector3(0.04f, 0f, 0.04f);
            box.Max -= new Vector3(0.04f, 0f, 0.04f);
            m_collisionBoxes.Clear();
            FindTerrainCollisionBoxes(box, m_collisionBoxes);
            m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
            float num = MathUtils.Max(CalculatePushBack(box, 1, m_collisionBoxes, out CollisionBox pushingCollisionBox), 0f);
            if (!BlocksManager.Blocks[Terrain.ExtractContents(pushingCollisionBox.BlockValue)].NoSmoothRise
                && num > 0.04f) {
                float x = MathUtils.Min(4.5f * dt, num);
                move.Y = MathUtils.Max(move.Y, x);
                m_velocity.Y = MathUtils.Max(m_velocity.Y, 0f);
                StandingOnValue = pushingCollisionBox.BlockValue;
                StandingOnBody = pushingCollisionBox.ComponentBody;
                m_stoppedTime = 0f;
                return true;
            }
            return false;
        }

        public virtual void HandleAxisCollision(int axis, float move, ref Vector3 position, bool isSmoothRising) {
            Vector3 stanceBoxSize = StanceBoxSize;
            m_collisionBoxes.Clear();
            if (CrouchPreventsFalling
                && m_crouchFactor >= 1f
                && axis != 1) {
                FindCrouchCollisionBoxes(position, new Vector2(stanceBoxSize.X - 0.08f, stanceBoxSize.Z - 0.08f), m_collisionBoxes);
            }
            Vector3 vector;
            switch (axis) {
                case 0:
                    position.X += move;
                    vector = new Vector3(0f, 0.04f, 0.04f);
                    break;
                case 1:
                    position.Y += move;
                    vector = new Vector3(0.04f, 0f, 0.04f);
                    break;
                default:
                    position.Z += move;
                    vector = new Vector3(0.04f, 0.04f, 0f);
                    break;
            }
            BoundingBox boundingBox = new(
                position - new Vector3(stanceBoxSize.X / 2f, 0f, stanceBoxSize.Z / 2f) + vector,
                position + new Vector3(stanceBoxSize.X / 2f, stanceBoxSize.Y, stanceBoxSize.Z / 2f) - vector
            );
            FindTerrainCollisionBoxes(boundingBox, m_collisionBoxes);
            m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
            float num;
            CollisionBox pushingCollisionBox;
            if (axis != 1 || isSmoothRising) {
                BoundingBox smoothRiseBox = boundingBox;
                smoothRiseBox.Min.Y += MaxSmoothRiseHeight;
                num = CalculateSmoothRisePushBack(boundingBox, smoothRiseBox, axis, m_collisionBoxes, out pushingCollisionBox);
            }
            else {
                num = CalculatePushBack(boundingBox, axis, m_collisionBoxes, out pushingCollisionBox);
            }
            BoundingBox box = new(
                position - new Vector3(stanceBoxSize.X / 2f, 0f, stanceBoxSize.Z / 2f) + vector,
                position + new Vector3(stanceBoxSize.X / 2f, stanceBoxSize.Y, stanceBoxSize.Z / 2f) - vector
            );
            float num2 = CalculatePushBack(box, axis, m_bodiesCollisionBoxes, out CollisionBox pushingCollisionBox2);
            if (MathF.Abs(num) > MathF.Abs(num2)) {
                if (num == 0f) {
                    return;
                }
                int num3 = Terrain.ExtractContents(pushingCollisionBox.BlockValue);
                if (BlocksManager.Blocks[num3].HasCollisionBehavior) {
                    SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num3);
                    for (int i = 0; i < blockBehaviors.Length; i++) {
                        Vector3 vector2 = (pushingCollisionBox.Box.Min + pushingCollisionBox.Box.Max) / 2f;
                        CellFace cellFace = CellFace.FromAxisAndDirection(
                            Terrain.ToCell(vector2.X),
                            Terrain.ToCell(vector2.Y),
                            Terrain.ToCell(vector2.Z),
                            axis,
                            0f - GetVectorComponent(m_velocity, axis)
                        );
                        blockBehaviors[i].OnCollide(cellFace, GetVectorComponent(m_velocity, axis), this);
                    }
                }
                switch (axis) {
                    case 0:
                        position.X += num;
                        m_velocity.X = pushingCollisionBox.BlockVelocity.X;
                        break;
                    case 1:
                        position.Y += num;
                        m_velocity.Y = pushingCollisionBox.BlockVelocity.Y;
                        if (move < 0f) {
                            StandingOnValue = pushingCollisionBox.BlockValue;
                            StandingOnBody = pushingCollisionBox.ComponentBody;
                            StandingOnVelocity = pushingCollisionBox.BlockVelocity;
                        }
                        break;
                    default:
                        position.Z += num;
                        m_velocity.Z = pushingCollisionBox.BlockVelocity.Z;
                        break;
                }
            }
            else {
                if (num2 == 0f) {
                    return;
                }
                ComponentBody componentBody = pushingCollisionBox2.ComponentBody;
                float targetMass = componentBody.CanBePushedByOtherBodies ? componentBody.Mass : 1e9f;
                switch (axis) {
                    case 0:
                        InelasticCollision(
                            m_velocity.X,
                            componentBody.m_velocity.X,
                            Mass,
                            targetMass,
                            0.5f,
                            out m_velocity.X,
                            out componentBody.m_velocity.X
                        );
                        position.X += num2;
                        break;
                    case 1:
                        InelasticCollision(
                            m_velocity.Y,
                            componentBody.m_velocity.Y,
                            Mass,
                            targetMass,
                            0.5f,
                            out m_velocity.Y,
                            out componentBody.m_velocity.Y
                        );
                        position.Y += num2;
                        if (move < 0f) {
                            StandingOnValue = pushingCollisionBox2.BlockValue;
                            StandingOnBody = pushingCollisionBox2.ComponentBody;
                            StandingOnVelocity = new Vector3(componentBody.m_velocity.X, 0f, componentBody.m_velocity.Z);
                        }
                        break;
                    default:
                        InelasticCollision(
                            m_velocity.Z,
                            componentBody.m_velocity.Z,
                            Mass,
                            targetMass,
                            0.5f,
                            out m_velocity.Z,
                            out componentBody.m_velocity.Z
                        );
                        position.Z += num2;
                        break;
                }
                CollidedWithBody?.Invoke(componentBody);
                componentBody.CollidedWithBody?.Invoke(this);
            }
        }

        public virtual void FindBodiesCollisionBoxes(Vector3 position, DynamicArray<CollisionBox> result) {
            m_componentBodies.Clear();
            m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), 4f, m_componentBodies);
            for (int i = 0; i < m_componentBodies.Count; i++) {
                ComponentBody componentBody = m_componentBodies.Array[i];
                if (!componentBody.BodyCollidable) {
                    continue;
                }
                if (componentBody != this
                    && componentBody != m_parentBody
                    && componentBody.m_parentBody != this) {
                    result.Add(new CollisionBox { Box = componentBody.BoundingBox, ComponentBody = componentBody });
                }
            }
        }

        public virtual void FindMovingBlocksCollisionBoxes(Vector3 position, DynamicArray<CollisionBox> result) {
            Vector3 stanceBoxSize = StanceBoxSize;
            BoundingBox boundingBox = new(
                position - new Vector3(stanceBoxSize.X / 2f, 0f, stanceBoxSize.Z / 2f),
                position + new Vector3(stanceBoxSize.X / 2f, stanceBoxSize.Y, stanceBoxSize.Z / 2f)
            );
            boundingBox.Min -= new Vector3(1f);
            boundingBox.Max += new Vector3(1f);
            m_movingBlockSets.Clear();
            m_subsystemMovingBlocks.FindMovingBlocks(boundingBox, false, m_movingBlockSets);
            for (int i = 0; i < m_movingBlockSets.Count; i++) {
                CollidedWithMovingBlock?.Invoke(m_movingBlockSets[i]);
                IMovingBlockSet movingBlockSet = m_movingBlockSets.Array[i];
                for (int j = 0; j < movingBlockSet.Blocks.Count; j++) {
                    MovingBlock movingBlock = movingBlockSet.Blocks[j];
                    int num = Terrain.ExtractContents(movingBlock.Value);
                    Block block = BlocksManager.Blocks[num];
                    if (block.GetIsCollidable(this, movingBlock.Value)) {
                        BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, movingBlock.Value);
                        Vector3 vector = new Vector3(movingBlock.Offset) + movingBlockSet.Position;
                        for (int k = 0; k < customCollisionBoxes.Length; k++) {
                            result.Add(
                                new CollisionBox {
                                    Box = new BoundingBox(vector + customCollisionBoxes[k].Min, vector + customCollisionBoxes[k].Max),
                                    BlockValue = movingBlock.Value,
                                    BlockVelocity = movingBlockSet.CurrentVelocity
                                }
                            );
                        }
                    }
                }
            }
        }

        public virtual void FindTerrainCollisionBoxes(BoundingBox box, DynamicArray<CollisionBox> result) {
            if (!TerrainCollidable) {
                return;
            }
            Point3 point = Terrain.ToCell(box.Min);
            Point3 point2 = Terrain.ToCell(box.Max);
            point.Y = MathUtils.Max(point.Y, 0);
            point2.Y = MathUtils.Min(point2.Y, 255);
            if (point.Y > point2.Y) {
                return;
            }
            for (int i = point.X; i <= point2.X; i++) {
                for (int j = point.Z; j <= point2.Z; j++) {
                    TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(i, j);
                    if (chunkAtCell == null) {
                        continue;
                    }
                    int num = TerrainChunk.CalculateCellIndex(i & 0xF, point.Y, j & 0xF);
                    int num2 = point.Y;
                    while (num2 <= point2.Y) {
                        int cellValueFast = chunkAtCell.GetCellValueFast(num);
                        int num3 = Terrain.ExtractContents(cellValueFast);
                        if (num3 != 0) {
                            Block block = BlocksManager.Blocks[num3];
                            if (block.GetIsCollidable(this, cellValueFast) && TerrainCollidable) {
                                BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, cellValueFast);
                                Vector3 vector = new(i, num2, j);
                                for (int k = 0; k < customCollisionBoxes.Length; k++) {
                                    result.Add(
                                        new CollisionBox {
                                            Box = new BoundingBox(vector + customCollisionBoxes[k].Min, vector + customCollisionBoxes[k].Max),
                                            BlockValue = cellValueFast
                                        }
                                    );
                                }
                            }
                        }
                        num2++;
                        num++;
                    }
                }
            }
        }

        public virtual void FindCrouchCollisionBoxes(Vector3 position, Vector2 overhang, DynamicArray<CollisionBox> result) {
            int num = Terrain.ToCell(position.X);
            int num2 = Terrain.ToCell(position.Y);
            int num3 = Terrain.ToCell(position.Z);
            if (BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3)]
                .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3))) {
                return;
            }
            bool num4 = position.X < num + 0.5f;
            bool flag = position.Z < num3 + 0.5f;
            CollisionBox item;
            if (num4) {
                if (flag) {
                    bool isCollidable = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 - 1)]
                        .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3 - 1));
                    bool isCollidable2 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3)]
                        .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num - 1, num2 - 1, num3));
                    bool isCollidable3 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3 - 1)]
                        .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num - 1, num2 - 1, num3 - 1));
                    if ((isCollidable && !isCollidable2)
                        || (!isCollidable && !isCollidable2 && isCollidable3)) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num, num2, num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)), BlockValue = 0
                        };
                        result.Add(item);
                    }
                    if ((!isCollidable && isCollidable2)
                        || (!isCollidable && !isCollidable2 && isCollidable3)) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1)), BlockValue = 0
                        };
                        result.Add(item);
                    }
                    if (isCollidable && isCollidable2) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num + overhang.X, num2, num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)),
                            BlockValue = 0
                        };
                        result.Add(item);
                    }
                }
                else {
                    bool isCollidable4 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 + 1)]
                        .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3 + 1));
                    bool isCollidable5 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3)]
                        .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num - 1, num2 - 1, num3));
                    bool isCollidable6 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3 + 1)]
                        .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num - 1, num2 - 1, num3 + 1));
                    if ((isCollidable4 && !isCollidable5)
                        || (!isCollidable4 && !isCollidable5 && isCollidable6)) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1 - overhang.Y)), BlockValue = 0
                        };
                        result.Add(item);
                    }
                    if ((!isCollidable4 && isCollidable5)
                        || (!isCollidable4 && !isCollidable5 && isCollidable6)) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1)), BlockValue = 0
                        };
                        result.Add(item);
                    }
                    if (isCollidable4 && isCollidable5) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1 - overhang.Y)),
                            BlockValue = 0
                        };
                        result.Add(item);
                    }
                }
            }
            else if (flag) {
                bool isCollidable7 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 - 1)]
                    .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3 - 1));
                bool isCollidable8 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3)]
                    .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num + 1, num2 - 1, num3));
                bool isCollidable9 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3 - 1)]
                    .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num + 1, num2 - 1, num3 - 1));
                if ((isCollidable7 && !isCollidable8)
                    || (!isCollidable7 && !isCollidable8 && isCollidable9)) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)), BlockValue = 0
                    };
                    result.Add(item);
                }
                if ((!isCollidable7 && isCollidable8)
                    || (!isCollidable7 && !isCollidable8 && isCollidable9)) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1 - overhang.X, num2 + 1, num3 + 1)), BlockValue = 0
                    };
                    result.Add(item);
                }
                if (isCollidable7 && isCollidable8) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3 + overhang.Y), new Vector3(num + 1 - overhang.X, num2 + 1, num3 + 1)),
                        BlockValue = 0
                    };
                    result.Add(item);
                }
            }
            else {
                bool isCollidable10 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 + 1)]
                    .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3 + 1));
                bool isCollidable11 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3)]
                    .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num + 1, num2 - 1, num3));
                bool isCollidable12 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3 + 1)]
                    .GetIsCollidable(this, m_subsystemTerrain.Terrain.GetCellValue(num + 1, num2 - 1, num3 + 1));
                if ((isCollidable10 && !isCollidable11)
                    || (!isCollidable10 && !isCollidable11 && isCollidable12)) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1 - overhang.Y)), BlockValue = 0
                    };
                    result.Add(item);
                }
                if ((!isCollidable10 && isCollidable11)
                    || (!isCollidable10 && !isCollidable11 && isCollidable12)) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1 - overhang.X, num2 + 1, num3 + 1)), BlockValue = 0
                    };
                    result.Add(item);
                }
                if (isCollidable10 && isCollidable11) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1 - overhang.X, num2 + 1, num3 + 1 - overhang.Y)),
                        BlockValue = 0
                    };
                    result.Add(item);
                }
            }
        }

        public virtual bool IsColliding(BoundingBox box, DynamicArray<CollisionBox> collisionBoxes) {
            for (int i = 0; i < collisionBoxes.Count; i++) {
                if (box.Intersection(collisionBoxes.Array[i].Box)) {
                    return true;
                }
            }
            return false;
        }

        public virtual bool IsCollidingWithIce(BoundingBox box, DynamicArray<CollisionBox> collisionBoxes) {
            for (int i = 0; i < collisionBoxes.Count; i++) {
                CollisionBox collisionBox = collisionBoxes.Array[i];
                if (box.Intersection(collisionBox.Box)
                    && Terrain.ExtractContents(collisionBox.BlockValue) == 62) {
                    return true;
                }
            }
            return false;
        }

        public virtual float CalculatePushBack(BoundingBox box,
            int axis,
            DynamicArray<CollisionBox> collisionBoxes,
            out CollisionBox pushingCollisionBox) {
            pushingCollisionBox = default;
            float num = 0f;
            for (int i = 0; i < collisionBoxes.Count; i++) {
                float num2 = CalculateBoxBoxOverlap(ref box, ref collisionBoxes.Array[i].Box, axis);
                if (MathF.Abs(num2) > MathF.Abs(num)) {
                    num = num2;
                    pushingCollisionBox = collisionBoxes.Array[i];
                }
            }
            return num;
        }

        public virtual float CalculateSmoothRisePushBack(BoundingBox normalBox,
            BoundingBox smoothRiseBox,
            int axis,
            DynamicArray<CollisionBox> collisionBoxes,
            out CollisionBox pushingCollisionBox) {
            pushingCollisionBox = default;
            float num = 0f;
            for (int i = 0; i < collisionBoxes.Count; i++) {
                float num2 = !BlocksManager.Blocks[Terrain.ExtractContents(collisionBoxes.Array[i].BlockValue)].NoSmoothRise
                    ? CalculateBoxBoxOverlap(ref smoothRiseBox, ref collisionBoxes.Array[i].Box, axis)
                    : CalculateBoxBoxOverlap(ref normalBox, ref collisionBoxes.Array[i].Box, axis);
                if (MathF.Abs(num2) > MathF.Abs(num)) {
                    num = num2;
                    pushingCollisionBox = collisionBoxes.Array[i];
                }
            }
            return num;
        }

        public static float CalculateBoxBoxOverlap(ref BoundingBox b1, ref BoundingBox b2, int axis) {
            if (b1.Max.X <= b2.Min.X
                || b1.Min.X >= b2.Max.X
                || b1.Max.Y <= b2.Min.Y
                || b1.Min.Y >= b2.Max.Y
                || b1.Max.Z <= b2.Min.Z
                || b1.Min.Z >= b2.Max.Z) {
                return 0f;
            }
            switch (axis) {
                case 0: {
                    float num13 = b1.Min.X + b1.Max.X;
                    float num14 = b2.Min.X + b2.Max.X;
                    float num15 = b1.Max.X - b1.Min.X;
                    float num16 = b2.Max.X - b2.Min.X;
                    float num17 = num14 - num13;
                    float num18 = num15 + num16;
                    return 0.5f * (num17 > 0f ? num17 - num18 : num17 + num18);
                }
                case 1: {
                    float num7 = b1.Min.Y + b1.Max.Y;
                    float num8 = b2.Min.Y + b2.Max.Y;
                    float num9 = b1.Max.Y - b1.Min.Y;
                    float num10 = b2.Max.Y - b2.Min.Y;
                    float num11 = num8 - num7;
                    float num12 = num9 + num10;
                    return 0.5f * (num11 > 0f ? num11 - num12 : num11 + num12);
                }
                default: {
                    float num = b1.Min.Z + b1.Max.Z;
                    float num2 = b2.Min.Z + b2.Max.Z;
                    float num3 = b1.Max.Z - b1.Min.Z;
                    float num4 = b2.Max.Z - b2.Min.Z;
                    float num5 = num2 - num;
                    float num6 = num3 + num4;
                    return 0.5f * (num5 > 0f ? num5 - num6 : num5 + num6);
                }
            }
        }

        public static float GetVectorComponent(Vector3 v, int axis) {
            switch (axis) {
                case 0: return v.X;
                case 1: return v.Y;
                default: return v.Z;
            }
        }

        public static void InelasticCollision(float v1,
            float v2,
            float m1,
            float m2,
            float cr,
            out float result1,
            out float result2) {
            float num = 1f / (m1 + m2);
            result1 = (cr * m2 * (v2 - v1) + m1 * v1 + m2 * v2) * num;
            result2 = (cr * m1 * (v1 - v2) + m1 * v1 + m2 * v2) * num;
        }

        public virtual bool MoveToFreeSpace() {
            Vector3 boxSize = BoxSize;
            Vector3 position = Position;
            for (int i = 0; i < m_freeSpaceOffsets.Length; i++) {
                Vector3? vector = null;
                Vector3 vector2 = position + m_freeSpaceOffsets[i];
                if (Terrain.ToCell(vector2) != Terrain.ToCell(position)) {
                    continue;
                }
                BoundingBox box = new(
                    vector2 - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f),
                    vector2 + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f)
                );
                box.Min += new Vector3(0.01f, MaxSmoothRiseHeight + 0.01f, 0.01f);
                box.Max -= new Vector3(0.01f);
                m_collisionBoxes.Clear();
                FindTerrainCollisionBoxes(box, m_collisionBoxes);
                m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
                m_collisionBoxes.AddRange(m_bodiesCollisionBoxes);
                if (!IsColliding(box, m_collisionBoxes)) {
                    vector = vector2;
                }
                else {
                    m_stoppedTime = 0f;
                    float num = CalculatePushBack(box, 0, m_collisionBoxes, out CollisionBox _);
                    float num2 = CalculatePushBack(box, 1, m_collisionBoxes, out CollisionBox _);
                    float num3 = CalculatePushBack(box, 2, m_collisionBoxes, out CollisionBox _);
                    float num4 = num * num;
                    float num5 = num2 * num2;
                    float num6 = num3 * num3;
                    List<Vector3> list = new();
                    if (num4 <= num5
                        && num4 <= num6) {
                        list.Add(vector2 + new Vector3(num, 0f, 0f));
                        if (num5 <= num6) {
                            list.Add(vector2 + new Vector3(0f, num2, 0f));
                            list.Add(vector2 + new Vector3(0f, 0f, num3));
                        }
                        else {
                            list.Add(vector2 + new Vector3(0f, 0f, num3));
                            list.Add(vector2 + new Vector3(0f, num2, 0f));
                        }
                    }
                    else if (num5 <= num4
                        && num5 <= num6) {
                        list.Add(vector2 + new Vector3(0f, num2, 0f));
                        if (num4 <= num6) {
                            list.Add(vector2 + new Vector3(num, 0f, 0f));
                            list.Add(vector2 + new Vector3(0f, 0f, num3));
                        }
                        else {
                            list.Add(vector2 + new Vector3(0f, 0f, num3));
                            list.Add(vector2 + new Vector3(num, 0f, 0f));
                        }
                    }
                    else {
                        list.Add(vector2 + new Vector3(0f, 0f, num3));
                        if (num4 <= num5) {
                            list.Add(vector2 + new Vector3(num, 0f, 0f));
                            list.Add(vector2 + new Vector3(0f, num2, 0f));
                        }
                        else {
                            list.Add(vector2 + new Vector3(0f, num2, 0f));
                            list.Add(vector2 + new Vector3(num, 0f, 0f));
                        }
                    }
                    foreach (Vector3 item in list) {
                        box = new BoundingBox(
                            item - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f),
                            item + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f)
                        );
                        box.Min += new Vector3(0.02f, MaxSmoothRiseHeight + 0.02f, 0.02f);
                        box.Max -= new Vector3(0.02f);
                        m_collisionBoxes.Clear();
                        FindTerrainCollisionBoxes(box, m_collisionBoxes);
                        m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
                        m_collisionBoxes.AddRange(m_bodiesCollisionBoxes);
                        if (!IsColliding(box, m_collisionBoxes)) {
                            vector = item;
                            break;
                        }
                    }
                }
                if (vector.HasValue) {
                    Position = vector.Value;
                    return true;
                }
            }
            return false;
        }

        public virtual void FindSneakCollisionBoxes(Vector3 position, Vector2 overhang, DynamicArray<CollisionBox> result) {
            int num = Terrain.ToCell(position.X);
            int num2 = Terrain.ToCell(position.Y);
            int num3 = Terrain.ToCell(position.Z);
            int value = m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3);
            if (BlocksManager.Blocks[Terrain.ExtractContents(value)].GetIsCollidable(this, value)) {
                return;
            }
            bool num4 = position.X < num + 0.5f;
            bool flag = position.Z < num3 + 0.5f;
            CollisionBox item;
            if (num4) {
                if (flag) {
                    int value1 = m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3 - 1);
                    int value2 = m_subsystemTerrain.Terrain.GetCellValue(num - 1, num2 - 1, num3);
                    int value3 = m_subsystemTerrain.Terrain.GetCellValue(num - 1, num2 - 1, num3 - 1);
                    bool isCollidable = BlocksManager.Blocks[Terrain.ExtractContents(value1)].GetIsCollidable(this, value1);
                    bool isCollidable2 = BlocksManager.Blocks[Terrain.ExtractContents(value2)].GetIsCollidable(this, value2);
                    bool isCollidable3 = BlocksManager.Blocks[Terrain.ExtractContents(value3)].GetIsCollidable(this, value3);
                    if ((isCollidable && !isCollidable2)
                        || (!isCollidable && !isCollidable2) & isCollidable3) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num, num2, num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)), BlockValue = 0
                        };
                        result.Add(item);
                    }
                    if ((!isCollidable && isCollidable2)
                        || (!isCollidable && !isCollidable2) & isCollidable3) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1)), BlockValue = 0
                        };
                        result.Add(item);
                    }
                    if (isCollidable && isCollidable2) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num + overhang.X, num2, num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)),
                            BlockValue = 0
                        };
                        result.Add(item);
                    }
                }
                else {
                    int value4 = m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3 + 1);
                    int value5 = m_subsystemTerrain.Terrain.GetCellValue(num - 1, num2 - 1, num3);
                    int value6 = m_subsystemTerrain.Terrain.GetCellValue(num - 1, num2 - 1, num3 + 1);
                    bool isCollidable4 = BlocksManager.Blocks[Terrain.ExtractContents(value4)].GetIsCollidable(this, value4);
                    bool isCollidable5 = BlocksManager.Blocks[Terrain.ExtractContents(value5)].GetIsCollidable(this, value5);
                    bool isCollidable6 = BlocksManager.Blocks[Terrain.ExtractContents(value6)].GetIsCollidable(this, value6);
                    if ((isCollidable4 && !isCollidable5)
                        || (!isCollidable4 && !isCollidable5) & isCollidable6) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1 - overhang.Y)), BlockValue = 0
                        };
                        result.Add(item);
                    }
                    if ((!isCollidable4 && isCollidable5)
                        || (!isCollidable4 && !isCollidable5) & isCollidable6) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1)), BlockValue = 0
                        };
                        result.Add(item);
                    }
                    if (isCollidable4 && isCollidable5) {
                        item = new CollisionBox {
                            Box = new BoundingBox(new Vector3(num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1 - overhang.Y)),
                            BlockValue = 0
                        };
                        result.Add(item);
                    }
                }
            }
            else if (flag) {
                int value7 = m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3 - 1);
                int value8 = m_subsystemTerrain.Terrain.GetCellValue(num + 1, num2 - 1, num3);
                int value9 = m_subsystemTerrain.Terrain.GetCellValue(num + 1, num2 - 1, num3 - 1);
                bool isCollidable7 = BlocksManager.Blocks[Terrain.ExtractContents(value7)].GetIsCollidable(this, value7);
                bool isCollidable8 = BlocksManager.Blocks[Terrain.ExtractContents(value8)].GetIsCollidable(this, value8);
                bool isCollidable9 = BlocksManager.Blocks[Terrain.ExtractContents(value9)].GetIsCollidable(this, value9);
                if ((isCollidable7 && !isCollidable8)
                    || (!isCollidable7 && !isCollidable8) & isCollidable9) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)), BlockValue = 0
                    };
                    result.Add(item);
                }
                if ((!isCollidable7 && isCollidable8)
                    || (!isCollidable7 && !isCollidable8) & isCollidable9) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1 - overhang.X, num2 + 1, num3 + 1)), BlockValue = 0
                    };
                    result.Add(item);
                }
                if (isCollidable7 && isCollidable8) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3 + overhang.Y), new Vector3(num + 1 - overhang.X, num2 + 1, num3 + 1)),
                        BlockValue = 0
                    };
                    result.Add(item);
                }
            }
            else {
                int value10 = m_subsystemTerrain.Terrain.GetCellValue(num, num2 - 1, num3 + 1);
                int value11 = m_subsystemTerrain.Terrain.GetCellValue(num + 1, num2 - 1, num3);
                int value12 = m_subsystemTerrain.Terrain.GetCellValue(num + 1, num2 - 1, num3 + 1);
                bool isCollidable10 = BlocksManager.Blocks[Terrain.ExtractContents(value10)].GetIsCollidable(this, value10);
                bool isCollidable11 = BlocksManager.Blocks[Terrain.ExtractContents(value11)].GetIsCollidable(this, value11);
                bool isCollidable12 = BlocksManager.Blocks[Terrain.ExtractContents(value12)].GetIsCollidable(this, value12);
                if ((isCollidable10 && !isCollidable11)
                    || (!isCollidable10 && !isCollidable11) & isCollidable12) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1 - overhang.Y)), BlockValue = 0
                    };
                    result.Add(item);
                }
                if ((!isCollidable10 && isCollidable11)
                    || (!isCollidable10 && !isCollidable11) & isCollidable12) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1 - overhang.X, num2 + 1, num3 + 1)), BlockValue = 0
                    };
                    result.Add(item);
                }
                if (isCollidable10 && isCollidable11) {
                    item = new CollisionBox {
                        Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1 - overhang.X, num2 + 1, num3 + 1 - overhang.Y)),
                        BlockValue = 0
                    };
                    result.Add(item);
                }
            }
        }
    }
}