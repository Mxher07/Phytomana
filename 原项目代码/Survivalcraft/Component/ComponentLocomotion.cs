using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentLocomotion : Component, IUpdateable {
        public SubsystemTime m_subsystemTime;
        public SubsystemNoise m_subsystemNoise;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemGameInfo m_subsystemGameInfo;
        public ComponentCreature m_componentCreature;
        public ComponentPlayer m_componentPlayer;
        public ComponentLevel m_componentLevel;
        public ComponentFactors m_componentFactors;
        public ComponentClothing m_componentClothing;
        public ComponentMount m_componentMount;
        public ComponentRider m_componentRider;

        public Random m_random = new();
        public Vector2? m_walkOrder;
        public Vector3? m_flyOrder;
        public Vector3? m_swimOrder;
        public Vector2 m_turnOrder;
        public Vector2 m_lookOrder;
        public float m_jumpOrder;
        public bool m_lookAutoLevelX;
        public bool m_lookAutoLevelY;
        public double m_shoesWarningTime;
        public float m_walkSpeedWhenTurning;
        public float m_minFrictionFactor;
        public double m_ladderActivationTime;
        public float m_swimBurstRemaining;
        public Vector2 m_lookAngles;
        public Vector3? m_lastPosition;
        public bool m_walking;
        public bool m_falling;
        public bool m_climbing;
        public bool m_jumping;
        public bool m_swimming;
        public bool m_flying;
        public const string fName = "ComponentLocomotion";

        public float AccelerationFactor { get; set; }

        public float WalkSpeed { get; set; }

        public float LadderSpeed { get; set; }

        public float JumpSpeed { get; set; }

        public float FlySpeed { get; set; }

        public float CreativeFlySpeed { get; set; }

        public float SwimSpeed { get; set; }

        public float TurnSpeed { get; set; }

        public float LookSpeed { get; set; }

        public float InAirWalkFactor { get; set; }

        public float? SlipSpeed { get; set; }

        public bool EnableGravityOnDeathOrStun = true;

        public Vector2 LookAngles {
            get => m_lookAngles;
            set {
                value.X = Math.Clamp(value.X, 0f - MathUtils.DegToRad(140f), MathUtils.DegToRad(140f));
                value.Y = Math.Clamp(value.Y, 0f - MathUtils.DegToRad(82f), MathUtils.DegToRad(82f));
                m_lookAngles = value;
            }
        }

        public int? LadderValue { get; set; }

        public Vector2? WalkOrder {
            get => m_walkOrder;
            set {
                m_walkOrder = value;
                if (m_walkOrder.HasValue) {
                    float num = m_walkOrder.Value.LengthSquared();
                    if (num > 1f) {
                        m_walkOrder = m_walkOrder.Value / MathF.Sqrt(num);
                    }
                }
            }
        }

        public Vector3? FlyOrder {
            get => m_flyOrder;
            set {
                m_flyOrder = value;
                if (m_flyOrder.HasValue) {
                    float num = m_flyOrder.Value.LengthSquared();
                    if (num > 1f) {
                        m_flyOrder = m_flyOrder.Value / MathF.Sqrt(num);
                    }
                }
            }
        }

        public Vector3? SwimOrder {
            get => m_swimOrder;
            set {
                m_swimOrder = value;
                if (m_swimOrder.HasValue) {
                    float num = m_swimOrder.Value.LengthSquared();
                    if (num > 1f) {
                        m_swimOrder = m_swimOrder.Value / MathF.Sqrt(num);
                    }
                }
            }
        }

        public Vector2 TurnOrder {
            get => m_turnOrder;
            set => m_turnOrder = value;
        }

        public Vector2 LookOrder {
            get => m_lookOrder;
            set => m_lookOrder = value;
        }

        public float JumpOrder {
            get => m_jumpOrder;
            set => m_jumpOrder = MathUtils.Saturate(value);
        }

        public Vector3? VrMoveOrder { get; set; }

        public Vector2? VrLookOrder { get; set; }

        public float StunTime { get; set; }

        /// <summary>
        /// 禁用输入移动（走路/飞行/跳跃/游泳/梯子）。true 时跳过 Update 主动移动块，
        /// 由 root motion 物理副作用（ComponentModel.ApplyRootMotionPhysics）设置。
        /// 不自减，切出 root motion 时由 API 清 false。
        /// </summary>
        public bool DisableInputMovement;

        public Vector2? LastWalkOrder { get; set; }

        public float LastJumpOrder { get; set; }

        public Vector3? LastFlyOrder { get; set; }

        public Vector3? LastSwimOrder { get; set; }

        public Vector2 LastTurnOrder { get; set; }

        public static bool IsCreativeFlyEnabledSet = false;

        public bool IsCreativeFlyEnabled { get; set; }

        public bool AllowShoesWarning = true;

        public UpdateOrder UpdateOrder => UpdateOrder.Locomotion;

        public virtual void Update(float dt) {
            SlipSpeed = null;
            /* 禁用创造模式飞行的操作只在开始的时候执行一次
             * if (m_subsystemGameInfo.WorldSettings.GameMode != 0)
            {
                IsCreativeFlyEnabled = false;
            }*/
            StunTime = MathUtils.Max(StunTime - dt, 0f);
            if (m_componentCreature.ComponentHealth.Health > 0f
                && StunTime <= 0f
                && !DisableInputMovement) {
                Vector3 position = m_componentCreature.ComponentBody.Position;
                PlayerStats playerStats = m_componentCreature.PlayerStats;
                if (playerStats != null) {
                    float x = m_lastPosition.HasValue ? Vector3.Distance(position, m_lastPosition.Value) : 0f;
                    x = MathUtils.Min(x, 25f * m_subsystemTime.PreviousGameTimeDelta);
                    playerStats.DistanceTravelled += x;
                    if (m_componentRider != null
                        && m_componentRider.Mount != null) {
                        playerStats.DistanceRidden += x;
                    }
                    else {
                        if (m_walking) {
                            playerStats.DistanceWalked += x;
                            m_walking = false;
                        }
                        if (m_falling) {
                            playerStats.DistanceFallen += x;
                            m_falling = false;
                        }
                        if (m_climbing) {
                            playerStats.DistanceClimbed += x;
                            m_climbing = false;
                        }
                        if (m_jumping) {
                            playerStats.Jumps++;
                            m_jumping = false;
                        }
                        if (m_swimming) {
                            playerStats.DistanceSwam += x;
                            m_swimming = false;
                        }
                        if (m_flying) {
                            playerStats.DistanceFlown += x;
                            m_flying = false;
                        }
                    }
                    playerStats.DeepestDive = Math.Max(playerStats.DeepestDive, m_componentCreature.ComponentBody.ImmersionDepth);
                    playerStats.LowestAltitude = Math.Min(playerStats.LowestAltitude, position.Y);
                    playerStats.HighestAltitude = Math.Max(playerStats.HighestAltitude, position.Y);
                    playerStats.EasiestModeUsed = (GameMode)MathUtils.Min(
                        (int)m_subsystemGameInfo.WorldSettings.GameMode,
                        (int)playerStats.EasiestModeUsed
                    );
                }
                m_lastPosition = position;
                m_swimBurstRemaining = MathUtils.Saturate(0.1f * m_swimBurstRemaining + dt);
                int x2 = Terrain.ToCell(position.X);
                int y = Terrain.ToCell(position.Y + 0.2f);
                int z = Terrain.ToCell(position.Z);
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x2, y, z);
                int num = Terrain.ExtractContents(cellValue);
                Block block = BlocksManager.Blocks[num];
                LookAngles += LookSpeed * LookOrder * dt;
                if (VrLookOrder.HasValue) {
                    if (m_componentCreature.ComponentBody.ParentBody != null) {
                        // Mounted: body rotation locked to mount, route VR yaw to eye rotation.
                        // Negated because CalculateEyeRotation applies -LookAngles.X.
                        LookAngles = new Vector2(LookAngles.X - VrLookOrder.Value.X, VrLookOrder.Value.Y);
                    }
                    else {
                        LookAngles = new Vector2(LookAngles.X, VrLookOrder.Value.Y);
                    }
                }
                if (!m_componentCreature.ComponentBody.IsEmbeddedInIce) {
                    Quaternion rotation = m_componentCreature.ComponentBody.Rotation;
                    float num2 = MathF.Atan2(
                        2f * rotation.Y * rotation.W - 2f * rotation.X * rotation.Z,
                        1f - 2f * rotation.Y * rotation.Y - 2f * rotation.Z * rotation.Z
                    );
                    num2 += (0f - TurnSpeed) * TurnOrder.X * dt;
                    // VR yaw only applies to body rotation when unmounted
                    if (VrLookOrder.HasValue && m_componentCreature.ComponentBody.ParentBody == null) {
                        num2 += VrLookOrder.Value.X;
                    }
                    // Snap turn body rotation (TppCamera/FixedCamera)
                    if (ComponentInput.VrSnapBodyRotation != 0f) {
                        num2 += ComponentInput.VrSnapBodyRotation;
                        ComponentInput.VrSnapBodyRotation = 0f;
                    }
                    m_componentCreature.ComponentBody.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num2);
                    if (LadderSpeed > 0f
                        && !LadderValue.HasValue
                        && block is LadderBlock
                        && m_subsystemTime.GameTime >= m_ladderActivationTime
                        && !IsCreativeFlyEnabled
                        && m_componentCreature.ComponentBody.ParentBody == null) {
                        int face = LadderBlock.GetFace(Terrain.ExtractData(cellValue));
                        if ((face == 0 && m_componentCreature.ComponentBody.CollisionVelocityChange.Z > 0f)
                            || (face == 1 && m_componentCreature.ComponentBody.CollisionVelocityChange.X > 0f)
                            || (face == 2 && m_componentCreature.ComponentBody.CollisionVelocityChange.Z < 0f)
                            || (face == 3 && m_componentCreature.ComponentBody.CollisionVelocityChange.X < 0f)
                            || !m_componentCreature.ComponentBody.StandingOnValue.HasValue) {
                            LadderValue = cellValue;
                            m_ladderActivationTime = m_subsystemTime.GameTime + 0.20000000298023224;
                            m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
                        }
                    }
                    if (VrMoveOrder.HasValue) {
                        // When mounted, apply room-scale movement to the mount's body
                        // (player position is locked to mount by ComponentBody)
                        ComponentBody targetBody = m_componentCreature.ComponentBody.ParentBody
                            ?? m_componentCreature.ComponentBody;
                        targetBody.ApplyDirectMove(VrMoveOrder.Value);
                    }
                    if (LadderValue.HasValue) {
                        LadderMovement(dt, cellValue);
                    }
                    else {
                        NormalMovement(dt);
                    }
                }
            }
            else {
                bool fallsOnDeathOrStun = m_componentCreature.ComponentBody.TerrainCollidable && EnableGravityOnDeathOrStun;
                ModsManager.HookActionReverse(
                    "OnLocomotionStopped",
                    loader => {
                        loader.OnLocomotionStopped(this, ref fallsOnDeathOrStun);
                        return false;
                    }
                );
                if (fallsOnDeathOrStun) {
                    m_componentCreature.ComponentBody.IsGravityEnabled = true;
                    m_componentCreature.ComponentBody.IsGroundDragEnabled = true;
                    m_componentCreature.ComponentBody.IsWaterDragEnabled = true;
                }
            }
            LastWalkOrder = WalkOrder;
            LastFlyOrder = FlyOrder;
            LastSwimOrder = SwimOrder;
            LastTurnOrder = TurnOrder;
            LastJumpOrder = JumpOrder;
            WalkOrder = null;
            FlyOrder = null;
            SwimOrder = null;
            TurnOrder = Vector2.Zero;
            JumpOrder = 0f;
            VrMoveOrder = null;
            VrLookOrder = null;
            LookOrder = new Vector2(m_lookAutoLevelX ? -10f * LookAngles.X / LookSpeed : 0f, m_lookAutoLevelY ? -10f * LookAngles.Y / LookSpeed : 0f);
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_componentCreature = Entity.FindComponent<ComponentCreature>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>();
            m_componentLevel = Entity.FindComponent<ComponentLevel>();
            m_componentClothing = Entity.FindComponent<ComponentClothing>();
            m_componentMount = Entity.FindComponent<ComponentMount>();
            m_componentRider = Entity.FindComponent<ComponentRider>();
            m_componentFactors = Entity.FindComponent<ComponentFactors>();
            IsCreativeFlyEnabled = valuesDictionary.GetValue<bool>("IsCreativeFlyEnabled");
            AccelerationFactor = valuesDictionary.GetValue<float>("AccelerationFactor");
            WalkSpeed = valuesDictionary.GetValue<float>("WalkSpeed");
            LadderSpeed = valuesDictionary.GetValue<float>("LadderSpeed");
            JumpSpeed = valuesDictionary.GetValue<float>("JumpSpeed");
            CreativeFlySpeed = valuesDictionary.GetValue<float>("CreativeFlySpeed");
            FlySpeed = valuesDictionary.GetValue<float>("FlySpeed");
            SwimSpeed = valuesDictionary.GetValue<float>("SwimSpeed");
            TurnSpeed = valuesDictionary.GetValue<float>("TurnSpeed");
            LookSpeed = valuesDictionary.GetValue<float>("LookSpeed");
            InAirWalkFactor = valuesDictionary.GetValue<float>("InAirWalkFactor");
            m_walkSpeedWhenTurning = valuesDictionary.GetValue<float>("WalkSpeedWhenTurning");
            m_minFrictionFactor = valuesDictionary.GetValue<float>("MinFrictionFactor");
            m_lookAutoLevelX = valuesDictionary.GetValue<bool>("LookAutoLevelX");
            m_lookAutoLevelY = valuesDictionary.GetValue<bool>("LookAutoLevelY");
            float mobWalkSpeedFactor = m_random.Float(0.85f, 1f);
            float mobFlySpeedFactor = m_random.Float(0.85f, 1f);
            float mobSwimSpeedFactor = m_random.Float(0.85f, 1f);
            bool disableCreativeFlyInSurvivalMode = true;
            ModsManager.HookAction(
                "OnComponentLocomotionLoaded",
                loader => {
                    loader.OnComponentLocomotionLoaded(
                        this,
                        ref mobWalkSpeedFactor,
                        ref mobFlySpeedFactor,
                        ref mobSwimSpeedFactor,
                        ref disableCreativeFlyInSurvivalMode
                    );
                    return false;
                }
            );
            if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Creative && disableCreativeFlyInSurvivalMode) {
                IsCreativeFlyEnabled = false;
            }
            if (Entity.FindComponent<ComponentPlayer>() == null) {
                WalkSpeed *= mobWalkSpeedFactor;
                FlySpeed *= mobFlySpeedFactor;
                SwimSpeed *= mobSwimSpeedFactor;
            }
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("IsCreativeFlyEnabled", IsCreativeFlyEnabled);
        }

        public virtual void NormalMovement(float dt) {
            m_componentCreature.ComponentBody.IsGravityEnabled = true;
            m_componentCreature.ComponentBody.IsGroundDragEnabled = true;
            m_componentCreature.ComponentBody.IsWaterDragEnabled = true;
            Vector3 velocity = m_componentCreature.ComponentBody.Velocity;
            Vector3 right = m_componentCreature.ComponentBody.Matrix.Right;
            Vector3 vector = Vector3.Transform(m_componentCreature.ComponentBody.Matrix.Forward, Quaternion.CreateFromAxisAngle(right, LookAngles.Y));
            if (WalkSpeed > 0f
                && WalkOrder.HasValue) {
                if (IsCreativeFlyEnabled || IsCreativeFlyEnabledSet) {
                    Vector3 v = new(WalkOrder.Value.X, 0f, WalkOrder.Value.Y);
                    if (FlyOrder.HasValue) {
                        v += FlyOrder.Value;
                    }
                    Vector3 v2 = !SettingsManager.HorizontalCreativeFlight
                        || m_componentPlayer == null
                        || m_componentPlayer.ComponentInput.IsControlledByTouch
                            ? Vector3.Normalize(vector + 0.1f * Vector3.UnitY)
                            : Vector3.Normalize(vector * new Vector3(1f, 0f, 1f));
                    Vector3 v3 = CreativeFlySpeed * (right * v.X + Vector3.UnitY * v.Y + v2 * v.Z);
                    float num = v == Vector3.Zero ? 5f : 3f;
                    velocity += MathUtils.Saturate(num * dt) * (v3 - velocity);
                    m_componentCreature.ComponentBody.IsGravityEnabled = false;
                    m_componentCreature.ComponentBody.IsGroundDragEnabled = false;
                    m_flying = true;
                }
                else {
                    Vector2 value = WalkOrder.Value;
                    if (m_walkSpeedWhenTurning > 0f
                        && MathF.Abs(TurnOrder.X) > 0.02f) {
                        value.Y = MathUtils.Max(value.Y, MathUtils.Lerp(0f, m_walkSpeedWhenTurning, MathUtils.Saturate(2f * MathF.Abs(TurnOrder.X))));
                    }
                    float num2 = WalkSpeed;
                    if (m_componentCreature.ComponentBody.ImmersionFactor > 0.2f) {
                        num2 *= 0.66f;
                    }
                    if (value.Y < 0f) {
                        num2 *= 0.6f;
                    }
                    if (m_componentFactors != null) {
                        num2 *= m_componentFactors.SpeedFactor;
                    }
                    if (m_componentMount != null) {
                        ComponentRider rider = m_componentMount.Rider;
                        if (rider != null) {
                            ComponentClothing componentClothing = rider.Entity.FindComponent<ComponentClothing>();
                            if (componentClothing != null) {
                                num2 *= componentClothing.SteedMovementSpeedFactor;
                            }
                        }
                    }
                    Vector3 v4 = value.X * Vector3.Normalize(new Vector3(right.X, 0f, right.Z))
                        + value.Y * Vector3.Normalize(new Vector3(vector.X, 0f, vector.Z));
                    Vector3 vector2 = num2 * v4 + m_componentCreature.ComponentBody.StandingOnVelocity;
                    float num4;
                    if (m_componentCreature.ComponentBody.StandingOnValue.HasValue) {
                        float num3 = MathUtils.Max(
                            BlocksManager.Blocks[Terrain.ExtractContents(m_componentCreature.ComponentBody.StandingOnValue.Value)].FrictionFactor,
                            m_minFrictionFactor
                        );
                        num4 = MathUtils.Saturate(dt * 6f * AccelerationFactor * num3);
                        if (num3 < 0.25f) {
                            SlipSpeed = num2 * value.Length();
                        }
                        m_walking = true;
                    }
                    else {
                        num4 = MathUtils.Saturate(dt * 6f * AccelerationFactor * InAirWalkFactor);
                        if (m_componentCreature.ComponentBody.ImmersionFactor > 0f) {
                            m_swimming = true;
                        }
                        else {
                            m_falling = true;
                        }
                    }
                    velocity.X += num4 * (vector2.X - velocity.X);
                    velocity.Z += num4 * (vector2.Z - velocity.Z);
                    Vector3 vector3 = value.X * right + value.Y * vector;
                    if (m_componentFactors != null) {
                        vector3 *= m_componentFactors.SpeedFactor;
                    }
                    velocity.Y += 10f * AccelerationFactor * vector3.Y * m_componentCreature.ComponentBody.ImmersionFactor * dt;
                    m_componentCreature.ComponentBody.IsGroundDragEnabled = false;
                    if (m_componentPlayer != null
                        && AllowShoesWarning
                        && Time.PeriodicEvent(10.0, 0.0)
                        && (m_shoesWarningTime == 0.0 || Time.FrameStartTime - m_shoesWarningTime > 300.0)
                        && m_componentCreature.ComponentBody.StandingOnValue.HasValue
                        && m_componentCreature.ComponentBody.ImmersionFactor < 0.1f) {
                        bool flag = false;
                        int value2 = m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Feet).LastOrDefault();
                        ClothingData clothingData = BlocksManager.Blocks[Terrain.ExtractContents(value2)].GetClothingData(value2);
                        if (clothingData != null) {
                            flag = clothingData.MovementSpeedFactor > 1f;
                        }
                        if (!flag
                            && vector2.LengthSquared() / velocity.LengthSquared() > 0.99f
                            && WalkOrder.Value.LengthSquared() > 0.99f) {
                            m_componentPlayer.ComponentGui.DisplaySmallMessage(LanguageControl.Get(fName, 1), Color.White, true, true);
                            m_shoesWarningTime = Time.FrameStartTime;
                        }
                    }
                }
            }
            if (FlySpeed > 0f
                && FlyOrder.HasValue) {
                Vector3 value3 = FlyOrder.Value;
                Vector3 v5 = FlySpeed * value3;
                velocity += MathUtils.Saturate(2f * AccelerationFactor * dt) * (v5 - velocity);
                m_componentCreature.ComponentBody.IsGravityEnabled = false;
                m_flying = true;
            }
            if (SwimSpeed > 0f
                && SwimOrder.HasValue
                && m_componentCreature.ComponentBody.ImmersionFactor > 0.5f) {
                Vector3 value4 = SwimOrder.Value;
                Vector3 v6 = SwimSpeed * value4;
                float num5 = 2f;
                if (value4.LengthSquared() >= 0.99f) {
                    v6 *= MathUtils.Lerp(1f, 2f, m_swimBurstRemaining);
                    num5 *= MathUtils.Lerp(1f, 4f, m_swimBurstRemaining);
                    m_swimBurstRemaining -= dt;
                }
                velocity += MathUtils.Saturate(num5 * AccelerationFactor * dt) * (v6 - velocity);
                m_componentCreature.ComponentBody.IsGravityEnabled = MathF.Abs(value4.Y) <= 0.07f;
                m_componentCreature.ComponentBody.IsWaterDragEnabled = false;
                m_componentCreature.ComponentBody.IsGroundDragEnabled = false;
                m_swimming = true;
            }
            if (JumpOrder > 0f
                && (m_componentCreature.ComponentBody.StandingOnValue.HasValue || m_componentCreature.ComponentBody.ImmersionFactor > 0.5f)) {
                m_componentCreature.ComponentBody.TargetCrouchFactor = 0f;
                float num6 = JumpSpeed;
                if (m_componentFactors != null) {
                    num6 *= 0.25f * (m_componentFactors.SpeedFactor - 1f) + 1f;
                }
                velocity.Y = MathUtils.Min(velocity.Y + MathUtils.Saturate(JumpOrder) * num6, num6);
                m_jumping = true;
                m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
                m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 10f);
            }
            if (MathF.Abs(m_componentCreature.ComponentBody.CollisionVelocityChange.Y) > 3f) {
                m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
                m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 10f);
            }
            m_componentCreature.ComponentBody.Velocity = velocity;
        }

        public virtual void LadderMovement(float dt, int value) {
            m_componentCreature.ComponentBody.IsGravityEnabled = false;
            Vector3 position = m_componentCreature.ComponentBody.Position;
            Vector3 velocity = m_componentCreature.ComponentBody.Velocity;
            int num = Terrain.ExtractContents(value);
            if (BlocksManager.Blocks[num] is LadderBlock) {
                LadderValue = value;
                if (WalkOrder.HasValue) {
                    Vector2 value2 = WalkOrder.Value;
                    float num2 = LadderSpeed * value2.Y;
                    velocity.X = 5f * (MathF.Floor(position.X) + 0.5f - position.X);
                    velocity.Z = 5f * (MathF.Floor(position.Z) + 0.5f - position.Z);
                    velocity.Y += MathUtils.Saturate(20f * dt) * (num2 - velocity.Y);
                    m_climbing = true;
                }
                if (m_componentCreature.ComponentBody.StandingOnValue.HasValue
                    && m_subsystemTime.GameTime >= m_ladderActivationTime) {
                    LadderValue = null;
                    m_ladderActivationTime = m_subsystemTime.GameTime + 0.20000000298023224;
                }
            }
            else {
                LadderValue = null;
                m_ladderActivationTime = m_subsystemTime.GameTime + 0.20000000298023224;
            }
            if (JumpOrder > 0f) {
                m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(2f);
                velocity += JumpSpeed * m_componentCreature.ComponentBody.Matrix.Forward;
                m_ladderActivationTime = m_subsystemTime.GameTime + 0.33000001311302185;
                LadderValue = null;
                m_jumping = true;
            }
            if (IsCreativeFlyEnabled) {
                m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f);
                LadderValue = null;
            }
            if (m_componentCreature.ComponentBody.ParentBody != null) {
                LadderValue = null;
            }
            m_componentCreature.ComponentBody.Velocity = velocity;
        }
    }
}