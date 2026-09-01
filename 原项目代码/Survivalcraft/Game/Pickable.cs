using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class Pickable : WorldItem {
        public int Count;

        public Vector3? FlyToPosition;

        public Matrix? StuckMatrix;

        public bool SplashGenerated = true;

        protected double m_timeWaitToAutoPick = 0.5;

        protected float m_distanceToPick = 1f;

        protected float m_distanceToFlyToTarget = 1.75f;

        Random m_random = new();
        public virtual double TimeWaitToAutoPick => m_timeWaitToAutoPick;
        public virtual float DistanceToPick => m_distanceToPick;
        public virtual float DistanceToFlyToTarget => m_distanceToFlyToTarget;

        public bool IsExplosionProof = false;

        #region 必选参数

        public Func<Terrain> CurrnetTerrain;

        public Func<float> CalcVisibilityRange;

        public Func<DrawBlockEnvironmentData> DrawBlockEnvironmentData;

        public Func<SubsystemSky.CalculateFogDelegate> CalculateFog;

        public Func<PrimitivesRenderer3D> PrimitivesRenderer;

        #endregion

        #region 可选

        public Project Project;

        public Entity OwnerEntity;

        public ComponentPickableGatherer FlyToGatherer;

        protected SubsystemPickables m_subsystemPickables;

        public SubsystemPickables SubsystemPickables {
            get {
                if (m_subsystemPickables == null
                    && Project != null) {
                    m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>();
                }
                return m_subsystemPickables;
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

        protected SubsystemExplosions m_subsystemExplosions;

        public SubsystemExplosions SubsystemExplosions {
            get {
                if (m_subsystemExplosions == null
                    && Project != null) {
                    m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>();
                }
                return m_subsystemExplosions;
            }
        }

        protected SubsystemMovingBlocks m_subsystemMovingBlocks;

        public SubsystemMovingBlocks SubsystemMovingBlocks {
            get {
                if (m_subsystemMovingBlocks == null
                    && Project != null) {
                    m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>();
                }
                return m_subsystemMovingBlocks;
            }
        }

        #endregion

        public override void Load(ValuesDictionary valuesDictionary) {
            Value = valuesDictionary.GetValue<int>("Value");
            Count = valuesDictionary.GetValue<int>("Count");
            Position = valuesDictionary.GetValue<Vector3>("Position");
            Velocity = valuesDictionary.GetValue<Vector3>("Velocity");
            CreationTime = valuesDictionary.GetValue("CreationTime", 0.0);
            if (valuesDictionary.ContainsKey("StuckMatrix")) {
                StuckMatrix = valuesDictionary.GetValue<Matrix>("StuckMatrix");
            }
            int ownerEntityID = valuesDictionary.GetValue("OwnerID", 0);
            if (ownerEntityID != 0
                && Project != null) {
                OwnerEntity = Project.FindEntity(ownerEntityID);
            }
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

        public virtual void Initialize(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner) {
            Value = value;
            Count = count;
            Position = position;
            StuckMatrix = stuckMatrix;
            OwnerEntity = owner;
            if (velocity.HasValue) {
                Velocity = velocity.Value;
            }
            else if (Terrain.ExtractContents(value) == 248) {
                Vector2 vector = m_random.Vector2(1.5f, 2f);
                Velocity = new Vector3(vector.X, 3f, vector.Y);
            }
            else {
                Velocity = new Vector3(m_random.Float(-0.5f, 0.5f), m_random.Float(1f, 1.2f), m_random.Float(-0.5f, 0.5f));
            }
        }

        protected TerrainRaycastResult?
            WrappedRaycast(Vector3 start, Vector3 end, bool useInteractionBoxes, bool skipAirBlocks, Func<int, float, bool> action) =>
            m_subsystemTerrain == null
                ? SubsystemTerrain.Raycast(CurrnetTerrain(), start, end, useInteractionBoxes, skipAirBlocks, action)
                : m_subsystemTerrain.Raycast(start, end, useInteractionBoxes, skipAirBlocks, action);

        public virtual void Update(float dt) {
            bool toRemove = UpdateTimeToRemove();
            if (toRemove) {
                ToRemove = true;
            }
            else {
                TerrainChunk chunkAtCell = CurrnetTerrain().GetChunkAtCell(Terrain.ToCell(Position.X), Terrain.ToCell(Position.Z));
                if (chunkAtCell != null
                    && chunkAtCell.State > TerrainChunkState.InvalidContents4) {
                    Vector3 positionAtdt = Position + Velocity * dt;
                    if (FlyToPosition.HasValue) {
                        UpdateMovementWithTarget(FlyToGatherer, dt);
                    }
                    else {
                        UpdateMovement(dt, ref positionAtdt);
                    }
                    Position = positionAtdt;
                }
            }
        }

        public virtual bool UpdateTimeToRemove() //更新移除逻辑
        {
            if (SubsystemPickables == null) {
                return false;
            }
            float maxTimeExist;
            if (MaxTimeExist.HasValue) {
                maxTimeExist = MaxTimeExist.Value;
            }
            else {
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(Value)];
                string category = block.GetCategory(Value);
                int remainPickables = SubsystemPickables.m_pickables.Count - SubsystemPickables.m_pickablesToRemove.Count;
                maxTimeExist = category == "Terrain" ? remainPickables > 80 ? 60 : 120 :
                    category == "Plants" && block.GetNutritionalValue(Value) == 0f ? remainPickables > 80 ? 60 : 120 :
                    !(block is EggBlock) ? remainPickables > 80 ? 120 : 480 : 240f;
            }
            double timeExisted = SubsystemPickables.m_subsystemGameInfo.TotalElapsedGameTime - CreationTime;
            if (timeExisted > maxTimeExist) {
                return true;
            }
            return false;
        }

        public virtual void UpdateMovement(float dt, ref Vector3 positionAtdt) {
            FluidBlock surfaceBlock = null;
            float? surfaceHeight = null;
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(Value)];
            Vector2? vector2 = SubsystemPickables?.m_subsystemFluidBlockBehavior.CalculateFlowSpeed(
                Terrain.ToCell(Position.X),
                Terrain.ToCell(Position.Y + 0.1f),
                Terrain.ToCell(Position.Z),
                out surfaceBlock,
                out surfaceHeight
            );
            if (!StuckMatrix.HasValue) {
                TerrainRaycastResult? terrainRaycastResult = WrappedRaycast(
                    Position,
                    positionAtdt,
                    false,
                    true,
                    (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value)
                );
                MovingBlocksRaycastResult? movingBlocksRaycastResult = SubsystemMovingBlocks?.Raycast(
                    Position + new Vector3(0f, 0.25f, 0f),
                    positionAtdt + new Vector3(0f, 0.25f, 0f),
                    true,
                    (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value)
                );
                bool isMovingRaycastDominant = false;
                int cellValue = 0;
                if (movingBlocksRaycastResult.HasValue
                    && movingBlocksRaycastResult.Value.MovingBlock != null
                    && (!terrainRaycastResult.HasValue || terrainRaycastResult.Value.Distance >= movingBlocksRaycastResult.Value.Distance)) {
                    isMovingRaycastDominant = true;
                    cellValue = movingBlocksRaycastResult.Value.MovingBlock.Value;
                }
                else if (terrainRaycastResult.HasValue
                    && (!movingBlocksRaycastResult.HasValue || terrainRaycastResult.Value.Distance < movingBlocksRaycastResult.Value.Distance)) {
                    //isMovingRaycastDominant = false;
                    cellValue = CurrnetTerrain()
                        .GetCellValue(
                            terrainRaycastResult.Value.CellFace.X,
                            terrainRaycastResult.Value.CellFace.Y,
                            terrainRaycastResult.Value.CellFace.Z
                        );
                }
                if (SubsystemPickables != null) {
                    SubsystemBlockBehavior[] blockBehaviors =
                        SubsystemPickables.m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(cellValue));
                    for (int i = 0; i < blockBehaviors.Length; i++) {
                        if (isMovingRaycastDominant) {
                            blockBehaviors[i].OnHitByProjectile(movingBlocksRaycastResult.Value.MovingBlock, this);
                        }
                        else if (terrainRaycastResult.HasValue) {
                            blockBehaviors[i].OnHitByProjectile(terrainRaycastResult.Value.CellFace, this);
                        }
                    }
                }
                if (terrainRaycastResult.HasValue) {
                    if (WrappedRaycast(
                            Position,
                            Position,
                            false,
                            true,
                            (value2, _) => BlocksManager.Blocks[Terrain.ExtractContents(value2)].IsCollidable_(value2)
                        )
                        .HasValue) {
                        int num8 = Terrain.ToCell(Position.X);
                        int num9 = Terrain.ToCell(Position.Y);
                        int num10 = Terrain.ToCell(Position.Z);
                        int num11 = 0;
                        int num12 = 0;
                        int num13 = 0;
                        int? num14 = null;
                        for (int j = -3; j <= 3; j++) {
                            for (int k = -3; k <= 3; k++) {
                                for (int l = -3; l <= 3; l++) {
                                    int value = CurrnetTerrain().GetCellContents(j + num8, k + num9, l + num10);
                                    if (!BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value)) {
                                        int num15 = j * j + k * k + l * l;
                                        if (!num14.HasValue
                                            || num15 < num14.Value) {
                                            num11 = j + num8;
                                            num12 = k + num9;
                                            num13 = l + num10;
                                            num14 = num15;
                                        }
                                    }
                                }
                            }
                        }
                        if (num14.HasValue) {
                            FlyToPosition = new Vector3(num11, num12, num13) + new Vector3(0.5f);
                        }
                        else {
                            ToRemove = true;
                        }
                    }
                    else {
                        Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
                        bool flag2 = vector2.HasValue && vector2.Value != Vector2.Zero;
                        if (plane.Normal.X != 0f) {
                            float num16 = flag2 || MathF.Sqrt(MathUtils.Sqr(Velocity.Y) + MathUtils.Sqr(Velocity.Z)) > 10f ? 0.95f : 0.25f;
                            Velocity *= new Vector3(0f - num16, num16, num16);
                        }
                        if (plane.Normal.Y != 0f) {
                            float num17 = flag2 || MathF.Sqrt(MathUtils.Sqr(Velocity.X) + MathUtils.Sqr(Velocity.Z)) > 10f ? 0.95f : 0.25f;
                            Velocity *= new Vector3(num17, 0f - num17, num17);
                            if (flag2) {
                                Velocity.Y += 0.1f * plane.Normal.Y;
                            }
                        }
                        if (plane.Normal.Z != 0f) {
                            float num18 = flag2 || MathF.Sqrt(MathUtils.Sqr(Velocity.X) + MathUtils.Sqr(Velocity.Y)) > 10f ? 0.95f : 0.25f;
                            Velocity *= new Vector3(num18, num18, 0f - num18);
                        }
                        positionAtdt = Position;
                    }
                }
            }
            else {
                Vector3 vector3 = StuckMatrix.Value.Translation + StuckMatrix.Value.Up * block.ProjectileTipOffset;
                if (!WrappedRaycast(
                        vector3,
                        vector3,
                        false,
                        true,
                        (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value)
                    )
                    .HasValue) {
                    Position = StuckMatrix.Value.Translation;
                    Velocity = Vector3.Zero;
                    StuckMatrix = null;
                }
            }
            if (surfaceBlock != null
                && !SplashGenerated
                && SubsystemPickables != null) {
                if (surfaceBlock is MagmaBlock) {
                    SubsystemPickables.m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(SubsystemTerrain, Position, false));
                    SubsystemPickables.m_subsystemAudio.PlayRandomSound(
                        "Audio/Sizzles",
                        1f,
                        SubsystemPickables.m_random.Float(-0.2f, 0.2f),
                        Position,
                        3f,
                        true
                    );
                    if (!IsFireProof) {
                        ToRemove = true;
                        SubsystemPickables.m_subsystemExplosions.TryExplodeBlock(
                            Terrain.ToCell(Position.X),
                            Terrain.ToCell(Position.Y),
                            Terrain.ToCell(Position.Z),
                            Value
                        );
                    }
                }
                else {
                    SubsystemPickables.m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(SubsystemTerrain, Position, false));
                    SubsystemPickables.m_subsystemAudio.PlayRandomSound(
                        "Audio/Splashes",
                        1f,
                        SubsystemPickables.m_random.Float(-0.2f, 0.2f),
                        Position,
                        6f,
                        true
                    );
                }
                SplashGenerated = true;
            }
            else if (surfaceBlock == null) {
                SplashGenerated = false;
            }
            //对于火焰的处理
            if (SubsystemPickables != null) {
                if (!IsFireProof
                    && SubsystemPickables.m_subsystemTime.PeriodicGameTimeEvent(1.0, GetHashCode() % 100 / 100.0)
                    && (SubsystemTerrain.Terrain.GetCellContents(
                            Terrain.ToCell(Position.X),
                            Terrain.ToCell(Position.Y + 0.1f),
                            Terrain.ToCell(Position.Z)
                        )
                        == 104
                        || SubsystemPickables.m_subsystemFireBlockBehavior.IsCellOnFire(
                            Terrain.ToCell(Position.X),
                            Terrain.ToCell(Position.Y + 0.1f),
                            Terrain.ToCell(Position.Z)
                        ))) {
                    SubsystemPickables.m_subsystemAudio.PlayRandomSound(
                        "Audio/Sizzles",
                        1f,
                        SubsystemPickables.m_random.Float(-0.2f, 0.2f),
                        Position,
                        3f,
                        true
                    );
                    ToRemove = true;
                    SubsystemPickables.m_subsystemExplosions.TryExplodeBlock(
                        Terrain.ToCell(Position.X),
                        Terrain.ToCell(Position.Y),
                        Terrain.ToCell(Position.Z),
                        Value
                    );
                }
            }
            //掉落物在卡住的时候的更新
            if (!StuckMatrix.HasValue) {
                //TODO:这里的数值改为变量表示
                if (vector2.HasValue
                    && surfaceHeight.HasValue) {
                    float num19 = surfaceHeight.Value - Position.Y;
                    float num20 = MathUtils.Saturate(3f * num19);
                    Velocity.X += 4f * dt * (vector2.Value.X - Velocity.X);
                    Velocity.Y -= 10f * dt;
                    Velocity.Y += 10f * (1f / block.GetDensity(Value) * num20) * dt;
                    Velocity.Z += 4f * dt * (vector2.Value.Y - Velocity.Z);
                    Velocity.Y *= MathF.Pow(0.001f, dt);
                }
                else {
                    Velocity.Y -= 10f * dt;
                    Velocity *= MathF.Pow(0.5f, dt);
                }
            }
        }

        public virtual void UpdateMovementWithTarget(ComponentPickableGatherer targetGatherer, float dt) {
            if (!FlyToPosition.HasValue) {
                return;
            }
            Vector3 v2 = FlyToPosition.Value - Position;
            float num7 = v2.LengthSquared();
            if (num7 >= 0.25f) {
                Velocity = 6f * v2 / MathF.Sqrt(num7);
            }
            else {
                FlyToPosition = null;
                FlyToGatherer = null;
            }
        }

        public override void UnderExplosion(Vector3 impulse, float damage) {
            if (SubsystemExplosions == null) {
                return;
            }
            if (IsExplosionProof) {
                return;
            }
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(Value)];
            if (damage / block.GetExplosionResilience(Value) > 0.1f) {
                SubsystemExplosions.TryExplodeBlock(Terrain.ToCell(Position.X), Terrain.ToCell(Position.Y), Terrain.ToCell(Position.Z), Value);
                ToRemove = true;
            }
            else {
                Vector3 vector = (impulse + new Vector3(0f, 0.1f * impulse.Length(), 0f)) * m_random.Float(0.75f, 1f);
                if (vector.Length() > 10f) {
                    Projectile projectile = SubsystemExplosions.m_subsystemProjectiles.AddProjectile(
                        Value,
                        Position,
                        Velocity + vector,
                        m_random.Vector3(0f, 20f),
                        null
                    );
                    if (m_random.Float(0f, 1f) < 0.33f) {
                        SubsystemExplosions.m_subsystemProjectiles.AddTrail(
                            projectile,
                            Vector3.Zero,
                            new SmokeTrailParticleSystem(15, m_random.Float(0.75f, 1.5f), m_random.Float(1f, 6f), Color.White)
                        );
                    }
                    ToRemove = true;
                }
                else {
                    Velocity += vector;
                }
            }
        }

        public virtual void Draw(Camera camera, int drawOrder, double totalElapsedGameTime, Matrix rotationMatrix) {
            float num = CalcVisibilityRange();
            Vector3 position = Position;
            Vector3 v = position - camera.ViewPosition;
            float num2 = Vector3.Dot(camera.ViewDirection, v);
            if (num2 < -0.5f
                || num2 > num) {
                return;
            }
            float num3 = v.Length();
            if (!(num3 > num)) {
                int num4 = Terrain.ExtractContents(Value);
                Block block = BlocksManager.Blocks[num4];
                float num5 = (float)(totalElapsedGameTime - CreationTime);
                if (!StuckMatrix.HasValue) {
                    position.Y += 0.25f * MathUtils.Saturate(3f * num5);
                }
                int x = Terrain.ToCell(position.X);
                int num6 = Terrain.ToCell(position.Y);
                int z = Terrain.ToCell(position.Z);
                TerrainChunk chunkAtCell = CurrnetTerrain().GetChunkAtCell(x, z);
                if (chunkAtCell != null
                    && chunkAtCell.State >= TerrainChunkState.InvalidVertices1
                    && num6 >= 0
                    && num6 < TerrainChunk.HeightMinusOne) {
                    DrawBlockEnvironmentData().Humidity = CurrnetTerrain().GetSeasonalHumidity(x, z);
                    DrawBlockEnvironmentData().Temperature = CurrnetTerrain().GetSeasonalTemperature(x, z)
                        + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num6);
                    float f = MathUtils.Max(position.Y - num6 - 0.75f, 0f) / 0.25f;
                    Light = (int)MathUtils.Lerp(CurrnetTerrain().GetCellLightFast(x, num6, z), CurrnetTerrain().GetCellLightFast(x, num6 + 1, z), f);
                }
                DrawBlockEnvironmentData().Light = Light;
                DrawBlockEnvironmentData().BillboardDirection = Position - camera.ViewPosition;
                DrawBlockEnvironmentData().InWorldMatrix.Translation = position;
                float num7 = 1f - CalculateFog()(camera.ViewPosition, Position);
                num7 *= MathUtils.Saturate(0.25f * (num - num3));
                Matrix drawMatrix;
                if (StuckMatrix.HasValue) {
                    drawMatrix = StuckMatrix.Value;
                }
                else {
                    rotationMatrix.Translation = position + new Vector3(0f, 0.04f * MathF.Sin(3f * num5), 0f);
                    drawMatrix = rotationMatrix;
                }
                bool shouldDrawBlock = true;
                float drawBlockSize = 0.3f;
                Color drawBlockColor = Color.MultiplyNotSaturated(Color.White, num7);
                if (SubsystemPickables != null) {
                    ModsManager.HookAction(
                        "OnPickableDraw",
                        loader => {
                            loader.OnPickableDraw(
                                this,
                                SubsystemPickables,
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
                    block.DrawBlock(PrimitivesRenderer(), Value, drawBlockColor, drawBlockSize, ref drawMatrix, DrawBlockEnvironmentData());
                }
            }
        }

        public virtual void Save(ValuesDictionary valuesDictionary) {
            valuesDictionary.SetValue("Class", GetType().FullName);
            valuesDictionary.SetValue("Value", Value);
            valuesDictionary.SetValue("Count", Count);
            valuesDictionary.SetValue("Position", Position);
            valuesDictionary.SetValue("Velocity", Velocity);
            valuesDictionary.SetValue("CreationTime", CreationTime);
            if (StuckMatrix.HasValue) {
                valuesDictionary.SetValue("StuckMatrix", StuckMatrix.Value);
            }
            if (OwnerEntity != null
                && OwnerEntity.Id != 0) {
                valuesDictionary.SetValue("OwnerID", OwnerEntity.Id);
            }
        }
    }
}