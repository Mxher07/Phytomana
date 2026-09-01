using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class Projectile : WorldItem {
        public Vector3 Rotation;

        public Vector3 AngularVelocity;

        public bool IsInFluid;

        [Obsolete("Use IsInFluid instead.")]
        public bool IsInWater {
            get => IsInFluid;
            set => IsInFluid = value;
        }

        public double LastNoiseTime;

        public ProjectileStoppedAction ProjectileStoppedAction;

        public ITrailParticleSystem TrailParticleSystem;

        public Vector3 TrailOffset;

        public bool NoChunk;

        public bool IsIncendiary;

        public Action OnRemove;

        public float Damping = -1f;

        public float DampingInFluid = 0.001f;

        public float Gravity = 10f;

        public float TerrainKnockBack = 0.3f;

        public bool StopTrailParticleInFluid = true;

        /// <summary>
        ///     弹射物结算时掉的耐久
        /// </summary>
        public int DamageToPickable = 1;

        public int? TurnIntoPickableBlockValue = null;

        public bool TerrainCollidable = true;

        public bool BodyCollidable = true;

        public float? m_attackPower;
        public virtual float MinVelocityToAttack { get; set; } = 10f;
        Random m_random = new();

        public delegate float CalcVisibilityRangeDelegate();

        #region 必选参数

        public Func<Terrain> CurrnetTerrain;

        public Func<float> CalcVisibilityRange;

        public Func<DrawBlockEnvironmentData> DrawBlockEnvironmentData;

        public Func<SubsystemSky.CalculateFogDelegate> CalculateFog;

        public Func<PrimitivesRenderer3D> PrimitivesRenderer;

        #endregion

        #region 可选参数

        public Project Project;

        public Entity OwnerEntity;

        public ComponentCreature Owner {
            get => OwnerEntity?.FindComponent<ComponentCreature>();
            set => OwnerEntity = value?.Entity;
        }

        /// <summary>
        ///     弹射物飞行的时候会忽略List中的ComponentBody
        /// </summary>
        public List<ComponentBody> BodiesToIgnore = new();

        protected SubsystemProjectiles m_subsystemProjectiles;

        public SubsystemProjectiles SubsystemProjectiles {
            get {
                if (m_subsystemProjectiles == null
                    && Project != null) {
                    m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>();
                }
                return m_subsystemProjectiles;
            }
        }

        protected SubsystemTerrain m_subsystemTerrain;

        public SubsystemTerrain SubsystemTerrain {
            get {
                if (m_subsystemTerrain == null
                    && Project != null) {
                    m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>();
                }
                return m_subsystemTerrain;
            }
        }

        protected SubsystemPickables SubsystemPickables => SubsystemProjectiles?.m_subsystemPickables;
        protected SubsystemParticles SubsystemParticles => SubsystemProjectiles?.m_subsystemParticles;
        protected SubsystemAudio SubsystemAudio => SubsystemProjectiles?.m_subsystemAudio;

        #endregion

        /// <summary>
        ///     在进入加载存档时执行
        /// </summary>
        public override void Load(ValuesDictionary valuesDictionary) {
            Value = valuesDictionary.GetValue<int>("Value");
            Position = valuesDictionary.GetValue<Vector3>("Position");
            Velocity = valuesDictionary.GetValue<Vector3>("Velocity");
            CreationTime = valuesDictionary.GetValue<double>("CreationTime");
            ProjectileStoppedAction = valuesDictionary.GetValue("ProjectileStoppedAction", ProjectileStoppedAction);
            int ownerEntityID = valuesDictionary.GetValue("OwnerID", 0);
            if (ownerEntityID != 0
                && Project != null) {
                OwnerEntity = Project.FindEntity(ownerEntityID);
            }
        }

        public virtual void Save(SubsystemProjectiles subsystemProjectiles, ValuesDictionary valuesDictionary) {
            valuesDictionary.SetValue("Class", GetType().FullName);
            valuesDictionary.SetValue("Value", Value);
            valuesDictionary.SetValue("Position", Position);
            valuesDictionary.SetValue("Velocity", Velocity);
            valuesDictionary.SetValue("CreationTime", CreationTime);
            valuesDictionary.SetValue("ProjectileStoppedAction", ProjectileStoppedAction);
            if (OwnerEntity != null
                && OwnerEntity.Id != 0) {
                valuesDictionary.SetValue("OwnerID", OwnerEntity.Id);
            }
            ModsManager.HookAction(
                "SaveProjectile",
                loader => {
                    loader.SaveProjectile(subsystemProjectiles, this, ref valuesDictionary);
                    return false;
                }
            );
        }

        public virtual float AttackPower {
            get => m_attackPower ?? BlocksManager.Blocks[Terrain.ExtractContents(Value)].GetProjectilePower(Value);
            set => m_attackPower = value;
        }

        public virtual void InitializeData(Func<Terrain> terrain,
            Func<DrawBlockEnvironmentData> drawBlockEnvironmentData,
            Func<float> calcVisibilityRange,
            Func<SubsystemSky.CalculateFogDelegate> calculateFog,
            Func<PrimitivesRenderer3D> primitivesRenderer) {
            CurrnetTerrain = terrain;
            DrawBlockEnvironmentData = drawBlockEnvironmentData;
            CalculateFog = calculateFog;
            CalcVisibilityRange = calcVisibilityRange;
            PrimitivesRenderer = primitivesRenderer;
        }

        public virtual void Initialize(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, Entity owner) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            Value = value;
            Position = position;
            Velocity = velocity;
            Rotation = Vector3.Zero;
            AngularVelocity = angularVelocity;
            OwnerEntity = owner;
            Damping = block.GetProjectileDamping(value);
            ProjectileStoppedAction = ProjectileStoppedAction.TurnIntoPickable;
        }

        public virtual void Initialize(int value, Vector3 position, Vector3 velocity, Vector3 angularVelocity, ComponentCreature owner) {
            Initialize(value, position, velocity, angularVelocity, owner?.Entity);
        }

        public virtual void Raycast(float dt, out BodyRaycastResult? bodyRaycastResult, out TerrainRaycastResult? terrainRaycastResult) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(Value)];
            Vector3 position = Position;
            Vector3 positionAtdt = position + Velocity * dt;
            Vector3 v = block.ProjectileTipOffset * Vector3.Normalize(Velocity);
            if (TerrainCollidable) {
                terrainRaycastResult = SubsystemTerrain == null
                    ? SubsystemTerrain.Raycast(
                        CurrnetTerrain(),
                        position + v,
                        positionAtdt + v,
                        false,
                        true,
                        (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value)
                    )
                    : SubsystemTerrain.Raycast(
                        position + v,
                        positionAtdt + v,
                        false,
                        true,
                        (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value)
                    );
            }
            else {
                terrainRaycastResult = null;
            }
            if (BodyCollidable && Project != null) {
                bodyRaycastResult = SubsystemProjectiles?.m_subsystemBodies.Raycast(
                    position + v,
                    positionAtdt + v,
                    0.2f,
                    (body, distance) => {
                        bool ignore = false;
                        ModsManager.HookAction(
                            "OnProjectileRaycastBody",
                            loader => {
                                loader.OnProjectileRaycastBody(body, this, distance, out bool ignoreByThisMod);
                                ignore |= ignoreByThisMod;
                                return false;
                            }
                        );
                        if (BodiesToIgnore.Contains(body) || ignore) {
                            return false;
                        }
                        return true;
                    }
                );
            }
            else {
                bodyRaycastResult = null;
            }
        }

        public virtual void Update(float dt) {
            if (Project != null) {
                UpdateTimeToRemove();
            }
            TerrainChunk chunkAtCell = CurrnetTerrain().GetChunkAtCell(Terrain.ToCell(Position.X), Terrain.ToCell(Position.Z));
            if (chunkAtCell == null
                || chunkAtCell.State <= TerrainChunkState.InvalidContents4) {
                NoChunk = true;
                if (TrailParticleSystem != null) {
                    TrailParticleSystem.IsStopped = true;
                }
                if (Project != null) {
                    OnProjectileFlyOutOfLoadedChunks();
                }
            }
            else {
                NoChunk = false;
                UpdateInChunk(dt);
            }
        }

        public virtual void UpdateTimeToRemove() {
            if (SubsystemProjectiles == null) {
                return;
            }
            double totalElapsedGameTime = SubsystemProjectiles.m_subsystemGameInfo.TotalElapsedGameTime;
            if (totalElapsedGameTime - CreationTime > (MaxTimeExist ?? 40f)) {
                ToRemove = true;
            }
        }

        public virtual void OnProjectileFlyOutOfLoadedChunks() {
            if (Project == null) {
                return;
            }
            ModsManager.HookAction(
                "OnProjectileFlyOutOfLoadedChunks",
                loader => {
                    loader.OnProjectileFlyOutOfLoadedChunks(this);
                    return false;
                }
            );
        }

        public virtual bool ProcessOnHitAsProjectileBlockBehavior(CellFace? cellFace, ComponentBody componentBody, float dt) {
            if (SubsystemProjectiles == null) {
                return false;
            }
            bool flag = false;
            SubsystemBlockBehavior[]
                blockBehaviors = SubsystemProjectiles.m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(Value));
            for (int i = 0; i < blockBehaviors.Length; i++) {
                flag |= blockBehaviors[i].OnHitAsProjectile(cellFace, componentBody, this);
            }
            return flag;
        }

        public virtual void HitBody(BodyRaycastResult bodyRaycastResult, ref Vector3 positionAtdt) {
            float attackPower = Velocity.Length() > MinVelocityToAttack ? AttackPower : 0;
            Vector3 velocityAfterAttack = Velocity * -0.05f + m_random.Vector3(-0.0166f * Velocity.Length());
            Vector3 angularVelocityAfterAttack = AngularVelocity * 0.05f;
            bool ignoreBody = false;
            Attackment attackment = new ProjectileAttackment(
                bodyRaycastResult.ComponentBody.Entity,
                OwnerEntity,
                bodyRaycastResult.HitPoint(),
                Vector3.Normalize(Velocity),
                attackPower,
                this
            );
            ModsManager.HookAction(
                "OnProjectileHitBody",
                loader => {
                    loader.OnProjectileHitBody(
                        this,
                        bodyRaycastResult,
                        ref attackment,
                        ref velocityAfterAttack,
                        ref angularVelocityAfterAttack,
                        ref ignoreBody
                    );
                    return false;
                }
            );
            if (ignoreBody) {
                BodiesToIgnore.Add(bodyRaycastResult.ComponentBody);
            }
            if (attackPower > 0f) {
                ComponentMiner.AttackBody(attackment);
                if (Owner is { PlayerStats: not null }) {
                    Owner.PlayerStats.RangedHits++;
                }
            }
            if (IsIncendiary) {
                bodyRaycastResult.ComponentBody.Entity.FindComponent<ComponentOnFire>()?.SetOnFire(Owner, m_random.Float(6f, 8f));
            }
            if (!ignoreBody) {
                positionAtdt = Position;
            }
            Velocity = velocityAfterAttack;
            AngularVelocity = angularVelocityAfterAttack;
        }

        public virtual void HitTerrain(TerrainRaycastResult terrainRaycastResult,
            CellFace cellFace,
            ref Vector3 positionAtdt,
            ref Vector3? pickableStuckMatrix) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(Value)];
            int cellValue = CurrnetTerrain().GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
            Block blockHitted = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
            float velocityLength = Velocity.Length();
            Vector3 velocityAfterHit = Velocity;
            Vector3 angularVelocityAfterHit = AngularVelocity * -0.3f;
            Plane plane = cellFace.CalculatePlane();
            if (plane.Normal.X != 0f) {
                velocityAfterHit *= new Vector3(-TerrainKnockBack, TerrainKnockBack, TerrainKnockBack);
            }
            if (plane.Normal.Y != 0f) {
                velocityAfterHit *= new Vector3(TerrainKnockBack, -TerrainKnockBack, TerrainKnockBack);
            }
            if (plane.Normal.Z != 0f) {
                velocityAfterHit *= new Vector3(TerrainKnockBack, TerrainKnockBack, -TerrainKnockBack);
            }
            float num3 = velocityAfterHit.Length();
            velocityAfterHit = num3 * Vector3.Normalize(velocityAfterHit + m_random.Vector3(num3 / 6f, num3 / 3f));
            bool triggerBlocksBehavior = true;
            bool destroyCell = velocityLength > 10f && m_random.Float(0f, 1f) > blockHitted.GetProjectileResilience(cellValue);
            float impactSoundLoudness = velocityLength > 5f ? 1f : 0f;
            bool projectileGetStuck = block.IsStickable_(Value)
                && velocityLength > 10f
                && m_random.Bool(blockHitted.GetProjectileStickProbability(Value));
            ModsManager.HookAction(
                "OnProjectileHitTerrain",
                loader => {
                    loader.OnProjectileHitTerrain(
                        this,
                        terrainRaycastResult,
                        ref triggerBlocksBehavior,
                        ref destroyCell,
                        ref impactSoundLoudness,
                        ref projectileGetStuck,
                        ref velocityAfterHit,
                        ref angularVelocityAfterHit
                    );
                    return false;
                }
            );
            //以上为ModLoader接口和ref变量
            if (triggerBlocksBehavior && SubsystemProjectiles != null) {
                SubsystemBlockBehavior[] blockBehaviors2 =
                    SubsystemProjectiles.m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(cellValue));
                for (int j = 0; j < blockBehaviors2.Length; j++) {
                    blockBehaviors2[j].OnHitByProjectile(cellFace, this);
                }
            }
            if (destroyCell
                && SubsystemTerrain != null
                && SubsystemProjectiles != null) {
                SubsystemTerrain.DestroyCell(
                    0,
                    cellFace.X,
                    cellFace.Y,
                    cellFace.Z,
                    0,
                    true,
                    false
                );
                SubsystemProjectiles.m_subsystemSoundMaterials.PlayImpactSound(cellValue, Position, 1f);
            }
            if (IsIncendiary
                && SubsystemTerrain != null
                && SubsystemProjectiles != null) {
                SubsystemProjectiles.m_subsystemFireBlockBehavior.SetCellOnFire(cellFace.X, cellFace.Y, cellFace.Z, 1f);
                Vector3 vector3 = Position - 0.75f * Vector3.Normalize(Velocity);
                for (int k = 0; k < 8; k++) {
                    Vector3 v2 = k == 0 ? Vector3.Normalize(Velocity) : m_random.Vector3(1.5f);
                    TerrainRaycastResult? terrainRaycastResult2 = SubsystemTerrain.Raycast(vector3, vector3 + v2, false, true, (_, _) => true);
                    if (terrainRaycastResult2.HasValue) {
                        SubsystemProjectiles.m_subsystemFireBlockBehavior.SetCellOnFire(
                            terrainRaycastResult2.Value.CellFace.X,
                            terrainRaycastResult2.Value.CellFace.Y,
                            terrainRaycastResult2.Value.CellFace.Z,
                            1f
                        );
                    }
                }
            }
            if (impactSoundLoudness > 0
                && SubsystemProjectiles != null) {
                SubsystemProjectiles.m_subsystemSoundMaterials.PlayImpactSound(cellValue, Position, impactSoundLoudness);
            }
            if (projectileGetStuck) {
                Vector3 v3 = Vector3.Normalize(Velocity);
                float s = MathUtils.Lerp(0.1f, 0.2f, MathUtils.Saturate((velocityLength - 15f) / 20f));
                pickableStuckMatrix = Position + terrainRaycastResult.Distance * Vector3.Normalize(Velocity) + v3 * s;
            }
            else {
                positionAtdt = Position;
                AngularVelocity = angularVelocityAfterHit;
                Velocity = velocityAfterHit;
            }
            MakeNoise();
        }

        public virtual void TurnIntoPickable(Vector3? pickableStuckMatrix) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(Value)];
            int damagedBlockValue = BlocksManager.DamageItem(Value, DamageToPickable, OwnerEntity);
            if (TurnIntoPickableBlockValue.HasValue) {
                damagedBlockValue = TurnIntoPickableBlockValue.Value;
            }
            if (damagedBlockValue != 0
                && SubsystemPickables != null) {
                Pickable pickable;
                if (pickableStuckMatrix.HasValue) {
                    SubsystemProjectiles.CalculateVelocityAlignMatrix(block, pickableStuckMatrix.Value, Velocity, out Matrix matrix);
                    pickable = SubsystemPickables.CreatePickable(damagedBlockValue, 1, Position, Vector3.Zero, matrix, OwnerEntity);
                }
                else {
                    pickable = SubsystemPickables.CreatePickable(damagedBlockValue, 1, Position, Vector3.Zero, null, OwnerEntity);
                }
                ModsManager.HookAction(
                    "OnProjectileTurnIntoPickable",
                    loader => {
                        loader.OnProjectileTurnIntoPickable(this, ref pickable);
                        return false;
                    }
                );
                if (pickable != null) {
                    SubsystemPickables.AddPickable(pickable);
                }
            }
            else if (SubsystemParticles != null) {
                SubsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(SubsystemTerrain, Position, Value, 1f));
            }
            ToRemove = true;
        }

        public virtual void UpdateInChunk(float dt) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(Value)];
            Vector3 position = Position;
            Vector3 positionAtdt = position + Velocity * dt;
            Vector3? pickableStuckMatrix = null;
            Raycast(dt, out BodyRaycastResult? bodyRaycastResult, out TerrainRaycastResult? terrainRaycastResult);
            CellFace? nullableCellFace = terrainRaycastResult.HasValue ? new CellFace?(terrainRaycastResult.Value.CellFace) : null;
            ComponentBody componentBody = bodyRaycastResult?.ComponentBody;
            //这里增加：忽略哪些Body、是否忽略地形
            bool disintegrate = block.DisintegratesOnHit;
            //执行各方块的OnHitAsProjectile。
            if (terrainRaycastResult.HasValue
                || bodyRaycastResult.HasValue) {
                disintegrate |= ProcessOnHitAsProjectileBlockBehavior(nullableCellFace, componentBody, dt);
                ToRemove |= disintegrate;
            }
            //如果弹射物命中了Body，进行攻击，并改变速度。
            if (bodyRaycastResult.HasValue
                && (!terrainRaycastResult.HasValue || bodyRaycastResult.Value.Distance < terrainRaycastResult.Value.Distance)) {
                HitBody(bodyRaycastResult.Value, ref positionAtdt);
            }
            //如果弹射物命中了地形，进行处理。破坏方块、点燃方块、撞到地形的移动效果。
            else if (terrainRaycastResult.HasValue) {
                CellFace cellFace = nullableCellFace.Value;
                HitTerrain(terrainRaycastResult.Value, cellFace, ref positionAtdt, ref pickableStuckMatrix);
            }
            //弹射物转化为掉落物
            if (terrainRaycastResult.HasValue
                || bodyRaycastResult.HasValue) {
                if (disintegrate && SubsystemParticles != null) {
                    SubsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(SubsystemTerrain, Position, Value, 1f));
                }
                else if (!ToRemove
                    && (pickableStuckMatrix.HasValue || Velocity.Length() < 1f)) {
                    if (ProjectileStoppedAction == ProjectileStoppedAction.TurnIntoPickable) {
                        TurnIntoPickable(pickableStuckMatrix);
                    }
                    else if (ProjectileStoppedAction == ProjectileStoppedAction.Disappear) {
                        ToRemove = true;
                    }
                }
            }
            UpdateMovement(dt, ref positionAtdt);
        }

        public virtual void UpdateMovement(float dt, ref Vector3 positionAtdt) {
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(Value)];
            if (Damping < 0f) {
                Damping = block.GetProjectileDamping(Value);
            }
            float friction = IsInFluid ? MathF.Pow(DampingInFluid, dt) : MathF.Pow(Damping, dt);
            Velocity.Y += -Gravity * dt;
            Velocity *= friction;
            AngularVelocity *= friction;
            Position = positionAtdt;
            Rotation += AngularVelocity * dt;
            int cellContents = CurrnetTerrain().GetCellContents(Terrain.ToCell(Position.X), Terrain.ToCell(Position.Y), Terrain.ToCell(Position.Z));
            Block blockTheProjectileIn = BlocksManager.Blocks[cellContents];
            bool isProjectileInFluid = blockTheProjectileIn is FluidBlock;
            if (TrailParticleSystem != null) {
                UpdateTrailParticleSystem(dt);
            }
            if (isProjectileInFluid && !IsInFluid) {
                if (DampingInFluid <= 0.001f) {
                    float horizontalSpeed = new Vector2(Velocity.X + Velocity.Z).Length();
                    if (horizontalSpeed > 6f
                        && horizontalSpeed > 4f * MathF.Abs(Velocity.Y)) {
                        Velocity *= 0.5f;
                        Velocity.Y *= -1f;
                        isProjectileInFluid = false;
                    }
                    else {
                        Velocity *= 0.2f;
                    }
                }
                float? surfaceHeight = SubsystemProjectiles?.m_subsystemFluidBlockBehavior.GetSurfaceHeight(
                    Terrain.ToCell(Position.X),
                    Terrain.ToCell(Position.Y),
                    Terrain.ToCell(Position.Z)
                );
                if (surfaceHeight.HasValue
                    && SubsystemParticles != null
                    && SubsystemAudio != null) {
                    if (blockTheProjectileIn is MagmaBlock) {
                        SubsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(SubsystemTerrain, Position, false));
                        SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), Position, 3f, true);
                        if (!IsFireProof) {
                            ToRemove = true;
                            SubsystemProjectiles?.m_subsystemExplosions.TryExplodeBlock(
                                Terrain.ToCell(Position.X),
                                Terrain.ToCell(Position.Y),
                                Terrain.ToCell(Position.Z),
                                Value
                            );
                        }
                    }
                    else {
                        SubsystemParticles.AddParticleSystem(
                            new WaterSplashParticleSystem(SubsystemTerrain, new Vector3(Position.X, surfaceHeight.Value, Position.Z), false)
                        );
                        SubsystemAudio.PlayRandomSound("Audio/Splashes", 1f, m_random.Float(-0.2f, 0.2f), Position, 6f, true);
                    }
                    MakeNoise();
                }
            }
            IsInFluid = isProjectileInFluid;
            if (SubsystemProjectiles != null
                && SubsystemAudio != null) {
                if (!IsFireProof
                    && SubsystemProjectiles.m_subsystemTime.PeriodicGameTimeEvent(1.0, GetHashCode() % 100 / 100.0)
                    && (SubsystemProjectiles.m_subsystemFireBlockBehavior.IsCellOnFire(
                            Terrain.ToCell(Position.X),
                            Terrain.ToCell(Position.Y + 0.1f),
                            Terrain.ToCell(Position.Z)
                        )
                        || SubsystemProjectiles.m_subsystemFireBlockBehavior.IsCellOnFire(
                            Terrain.ToCell(Position.X),
                            Terrain.ToCell(Position.Y + 0.1f) - 1,
                            Terrain.ToCell(Position.Z)
                        ))) {
                    SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), Position, 3f, true);
                    ToRemove = true;
                    SubsystemProjectiles.m_subsystemExplosions.TryExplodeBlock(
                        Terrain.ToCell(Position.X),
                        Terrain.ToCell(Position.Y),
                        Terrain.ToCell(Position.Z),
                        Value
                    );
                }
            }
        }

        public virtual void UpdateTrailParticleSystem(float dt) {
            if (SubsystemParticles == null) {
                return;
            }
            if (!SubsystemParticles.ContainsParticleSystem((ParticleSystemBase)TrailParticleSystem)) {
                SubsystemParticles.AddParticleSystem((ParticleSystemBase)TrailParticleSystem);
            }
            Vector3 v4 = TrailOffset != Vector3.Zero
                ? Vector3.TransformNormal(TrailOffset, Matrix.CreateFromAxisAngle(Vector3.Normalize(Rotation), Rotation.Length()))
                : Vector3.Zero;
            TrailParticleSystem.Position = Position + v4;
            if (IsInFluid && StopTrailParticleInFluid) {
                TrailParticleSystem.IsStopped = true;
            }
        }

        public virtual void MakeNoise() {
            if (SubsystemProjectiles == null) {
                return;
            }
            if (SubsystemProjectiles.m_subsystemTime.GameTime - LastNoiseTime > 0.5) {
                SubsystemProjectiles.m_subsystemNoise.MakeNoise(Position, 0.25f, 6f);
                LastNoiseTime = SubsystemProjectiles.m_subsystemTime.GameTime;
            }
        }

        public override void UnderExplosion(Vector3 impulse, float damage) {
            Velocity += (impulse + new Vector3(0f, 0.1f * impulse.Length(), 0f)) * m_random.Float(0.75f, 1f);
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            float num = MathUtils.Sqr(CalcVisibilityRange());
            Vector3 position = Position;
            if (!NoChunk
                && Vector3.DistanceSquared(camera.ViewPosition, position) < num
                && camera.ViewFrustum.Intersection(position)) {
                int x = Terrain.ToCell(position.X);
                int num2 = Terrain.ToCell(position.Y);
                int z = Terrain.ToCell(position.Z);
                int num3 = Terrain.ExtractContents(Value);
                Block block = BlocksManager.Blocks[num3];
                TerrainChunk chunkAtCell = CurrnetTerrain().GetChunkAtCell(x, z);
                if (chunkAtCell != null
                    && chunkAtCell.State >= TerrainChunkState.InvalidVertices1
                    && num2 >= 0
                    && num2 < 255) {
                    DrawBlockEnvironmentData().Humidity = CurrnetTerrain().GetSeasonalHumidity(x, z);
                    DrawBlockEnvironmentData().Temperature = CurrnetTerrain().GetSeasonalTemperature(x, z)
                        + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num2);
                    Light = CurrnetTerrain().GetCellLightFast(x, num2, z);
                }
                DrawBlockEnvironmentData().Light = Light;
                DrawBlockEnvironmentData().BillboardDirection = block.GetAlignToVelocity(Value) ? null : new Vector3?(camera.ViewDirection);
                DrawBlockEnvironmentData().InWorldMatrix.Translation = position;
                Matrix matrix;
                if (block.GetAlignToVelocity(Value)) {
                    SubsystemProjectiles.CalculateVelocityAlignMatrix(block, position, Velocity, out matrix);
                }
                else if (Rotation != Vector3.Zero) {
                    matrix = Matrix.CreateFromAxisAngle(Vector3.Normalize(Rotation), Rotation.Length());
                    matrix.Translation = Position;
                }
                else {
                    matrix = Matrix.CreateTranslation(Position);
                }
                bool shouldDrawBlock = true;
                float drawBlockSize = 0.3f;
                Color drawBlockColor = Color.MultiplyNotSaturated(Color.White, 1f - CalculateFog()(camera.ViewPosition, Position));
                if (SubsystemProjectiles != null) {
                    ModsManager.HookAction(
                        "OnProjectileDraw",
                        loader => {
                            loader.OnProjectileDraw(
                                this,
                                SubsystemProjectiles,
                                camera,
                                drawOrder,
                                ref shouldDrawBlock,
                                ref drawBlockSize,
                                ref drawBlockColor
                            );
                            return false;
                        }
                    );
                }
                if (shouldDrawBlock) {
                    block.DrawBlock(PrimitivesRenderer(), Value, drawBlockColor, drawBlockSize, ref matrix, DrawBlockEnvironmentData());
                }
            }
        }
    }
}