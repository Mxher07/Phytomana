using System.Globalization;
using System.Text;
using Engine;
using Engine.Graphics;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemMovingBlocks : Subsystem, IUpdateable, IDrawable {
        public class MovingBlockSet : IMovingBlockSet, IDisposable {
            public string Id;

            public object Tag;

            public bool Stop;

            public int RemainCounter;

            public Vector3 Position;

            public Vector3 StartPosition;

            public Vector3 TargetPosition;

            public float Speed;

            public float Acceleration;

            public float Drag;

            public Vector2 Smoothness;

            public List<MovingBlock> Blocks;

            public Box Box;

            public Vector3 CurrentVelocity;

            public Vector3 GeometryOffset;

            public Point3 GeometryGenerationPosition = new(int.MaxValue);

            Vector3 IMovingBlockSet.Position => Position;

            string IMovingBlockSet.Id => Id;

            object IMovingBlockSet.Tag => Tag;

            Vector3 IMovingBlockSet.CurrentVelocity => CurrentVelocity;
            bool IMovingBlockSet.Stopped => Stop;
            List<MovingBlock> IMovingBlockSet.Blocks => Blocks;

            public TerrainGeometry Geometry;

            public void Dispose() {
                Geometry.ClearGeometry();
            }

            public virtual void UpdateBox() {
                Point3? point = null;
                Point3? point2 = null;
                foreach (MovingBlock block in Blocks) {
                    point = point.HasValue ? Point3.Min(point.Value, block.Offset) : block.Offset;
                    point2 = point2.HasValue ? Point3.Max(point2.Value, block.Offset) : block.Offset;
                }
                if (point.HasValue) {
                    Box = new Box(
                        point.Value.X,
                        point.Value.Y,
                        point.Value.Z,
                        point2.Value.X - point.Value.X + 1,
                        point2.Value.Y - point.Value.Y + 1,
                        point2.Value.Z - point.Value.Z + 1
                    );
                }
                else {
                    Box = default;
                }
            }

            public BoundingBox BoundingBox(bool extendToFillCells) {
                Vector3 min = new(Position.X + Box.Left, Position.Y + Box.Top, Position.Z + Box.Near);
                Vector3 max = new(Position.X + Box.Right, Position.Y + Box.Bottom, Position.Z + Box.Far);
                if (extendToFillCells) {
                    min.X = MathF.Floor(min.X);
                    min.Y = MathF.Floor(min.Y);
                    min.Z = MathF.Floor(min.Z);
                    max.X = MathF.Ceiling(max.X);
                    max.Y = MathF.Ceiling(max.Y);
                    max.Z = MathF.Ceiling(max.Z);
                }
                return new BoundingBox(min, max);
            }

            void IMovingBlockSet.SetBlock(Point3 offset, int value) {
                Blocks.RemoveAll(b => b.Offset == offset);
                if (value != 0) {
                    Blocks.Add(new MovingBlock { Offset = offset, Value = value });
                }
                UpdateBox();
                GeometryGenerationPosition = new Point3(int.MaxValue);
            }

            void IMovingBlockSet.Stop() {
                Stop = true;
            }
        }

        public SubsystemTime m_subsystemTime;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemSky m_subsystemSky;

        public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

        public List<MovingBlockSet> m_movingBlockSets = [];

        public List<MovingBlockSet> m_stopped = [];

        public List<MovingBlockSet> m_removing = [];

        public DynamicArray<TerrainChunkGeometry.Buffer> Buffers;

        public DynamicArray<IMovingBlockSet> m_result = [];
        public static DynamicArray<int> m_tmpIndices = [];
        public static DynamicArray<TerrainVertex> m_vertexList = [];
        public Shader m_shader;

        public BlockGeometryGenerator m_blockGeometryGenerator;

        public bool m_canGenerateGeometry;

        public static int[] m_drawOrders = [10];

        [Obsolete("Use MovingBlockSetEnumerable instead. This is slow and uses more memory.")]
        public List<IMovingBlockSet> MovingBlockSets => [..m_movingBlockSets];

        public IEnumerable<IMovingBlockSet> MovingBlockSetEnumerable => m_movingBlockSets;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public int[] DrawOrders => m_drawOrders;

        public event Action<IMovingBlockSet, Point3> CollidedWithTerrain;

        public event Action<IMovingBlockSet> Stopped;

        public bool m_noDropOnMovingBlockStopped = false;

        public IMovingBlockSet AddMovingBlockSet(Vector3 position,
            Vector3 targetPosition,
            float speed,
            float acceleration,
            float drag,
            Vector2 smoothness,
            IEnumerable<MovingBlock> blocks,
            string id,
            object tag,
            bool testCollision) {
            MovingBlockSet movingBlockSet = new() {
                Position = position,
                StartPosition = position,
                TargetPosition = targetPosition,
                Speed = speed,
                Acceleration = acceleration,
                Drag = drag,
                Smoothness = smoothness,
                Id = id,
                Tag = tag,
                Blocks = blocks.ToList(),
                Geometry = new TerrainGeometry(m_subsystemAnimatedTextures.AnimatedBlocksTexture)
            };
            for (int i = 0; i < movingBlockSet.Blocks.Count; i++) {
                movingBlockSet.Blocks[i].MovingBlockSet = movingBlockSet;
            }
            movingBlockSet.UpdateBox();
            bool canAdd = true;
            ModsManager.HookAction(
                "OnMovingBlockSetAdded",
                loader => {
                    loader.OnMovingBlockSetAdded(ref movingBlockSet, this, ref testCollision, out bool doNotAdd);
                    canAdd &= !doNotAdd;
                    return false;
                }
            );
            if (!canAdd) {
                return null;
            }
            if (testCollision) {
                MovingBlocksCollision(movingBlockSet);
                if (movingBlockSet.Stop) {
                    return null;
                }
            }
            if (m_canGenerateGeometry) {
                GenerateGeometry(movingBlockSet);
            }
            m_movingBlockSets.Add(movingBlockSet);
            return movingBlockSet;
        }

        public void RemoveMovingBlockSet(IMovingBlockSet movingBlockSet) {
            MovingBlockSet movingBlockSet2 = (MovingBlockSet)movingBlockSet;
            if (m_movingBlockSets.Remove(movingBlockSet2)) {
                m_removing.Add(movingBlockSet2);
                ModsManager.HookAction(
                    "OnMovingBlockSetRemoved",
                    loader => {
                        loader.OnMovingBlockSetRemoved(movingBlockSet2, this);
                        return false;
                    }
                );
                movingBlockSet2.RemainCounter = 4;
            }
        }

        public void FindMovingBlocks(BoundingBox boundingBox, bool extendToFillCells, DynamicArray<IMovingBlockSet> result) {
            foreach (MovingBlockSet movingBlockSet in m_movingBlockSets) {
                if (!movingBlockSet.Stop
                    && ExclusiveBoxIntersection(boundingBox, movingBlockSet.BoundingBox(extendToFillCells))) {
                    result.Add(movingBlockSet);
                }
            }
        }

        public IMovingBlockSet FindMovingBlocks(string id, object tag) {
            foreach (MovingBlockSet movingBlockSet in m_movingBlockSets) {
                if (movingBlockSet.Id == id
                    && Equals(movingBlockSet.Tag, tag)) {
                    return movingBlockSet;
                }
            }
            return null;
        }

        public MovingBlocksRaycastResult? Raycast(Vector3 start, Vector3 end, bool extendToFillCells, Func<int, float, bool> action = null) {
            Ray3 ray = new(start, Vector3.Normalize(end - start));
            BoundingBox boundingBox = new(Vector3.Min(start, end), Vector3.Max(start, end));
            m_result.Clear();
            FindMovingBlocks(boundingBox, extendToFillCells, m_result);
            float num = float.MaxValue;
            MovingBlockSet movingBlockSet = null;
            try {
                foreach (IMovingBlockSet item in m_result.Array) {
                    if (item is not MovingBlockSet item1
                        || item1.Stop) {
                        continue;
                    }
                    BoundingBox box = item.BoundingBox(extendToFillCells);
                    float? num2 = ray.Intersection(box);
                    if (num2.HasValue
                        && num2.Value < num) {
                        num = num2.Value;
                        movingBlockSet = item1;
                    }
                }
            }
            catch (Exception e) {
                Log.Error($"Moving Blocks raycast error{e}");
            }
            if (movingBlockSet != null) {
                float distance = float.MaxValue;
                MovingBlock rightMovingBlock = null;
                int rightCollisionBoxIndex = -1;
                BoundingBox? rightNearestBox = null;
                foreach (MovingBlock movingBlock in movingBlockSet.Blocks) {
                    int blockValue = movingBlock.Value;
                    Block block = BlocksManager.Blocks[Terrain.ExtractContents(blockValue)];
                    Ray3 equalRay = new(
                        ray.Position - movingBlockSet.Position - new Vector3(movingBlock.Offset.X, movingBlock.Offset.Y, movingBlock.Offset.Z),
                        ray.Direction
                    );
                    float? dist = block.Raycast(
                        equalRay,
                        m_subsystemTerrain,
                        blockValue,
                        true,
                        out int collisionBoxIndex,
                        out BoundingBox nearestBox
                    );
                    if (dist.HasValue
                        && dist.Value < distance
                        && (action == null || action(blockValue, dist.Value))) {
                        distance = dist.Value;
                        rightMovingBlock = movingBlock;
                        rightCollisionBoxIndex = collisionBoxIndex;
                        rightNearestBox = nearestBox;
                    }
                }
                MovingBlocksRaycastResult value = default;
                value.Ray = ray;
                value.Distance = distance == float.MaxValue ? num : distance;
                value.MovingBlockSet = movingBlockSet;
                value.MovingBlock = rightMovingBlock;
                value.CollisionBoxIndex = rightCollisionBoxIndex;
                value.BlockBoundingBox = rightNearestBox;
                return value;
            }
            return null;
        }

        public virtual void Update(float dt) {
            m_canGenerateGeometry = true;
            foreach (MovingBlockSet movingBlockSet in m_movingBlockSets) {
                bool pass = false;
                ModsManager.HookAction(
                    "OnMovingBlockSetUpdate",
                    loader => {
                        loader.OnMovingBlockSetUpdate(movingBlockSet, this, pass, out bool skipVanilla);
                        pass |= skipVanilla;
                        return false;
                    }
                );
                if (pass) {
                    continue;
                }
                TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(
                    Terrain.ToCell(movingBlockSet.Position.X),
                    Terrain.ToCell(movingBlockSet.Position.Z)
                );
                if (chunkAtCell == null
                    || chunkAtCell.State <= TerrainChunkState.InvalidContents4) {
                    continue;
                }
                movingBlockSet.Speed += movingBlockSet.Acceleration * m_subsystemTime.GameTimeDelta;
                if (movingBlockSet.Drag != 0f) {
                    movingBlockSet.Speed *= MathF.Pow(1f - movingBlockSet.Drag, m_subsystemTime.GameTimeDelta);
                }
                float x = Vector3.Distance(movingBlockSet.StartPosition, movingBlockSet.Position);
                float num = Vector3.Distance(movingBlockSet.TargetPosition, movingBlockSet.Position);
                float num2 = movingBlockSet.Smoothness.X > 0f ? MathUtils.Saturate((MathF.Sqrt(x) + 0.05f) / movingBlockSet.Smoothness.X) : 1f;
                float num3 = movingBlockSet.Smoothness.Y > 0f ? MathUtils.Saturate((num + 0.05f) / movingBlockSet.Smoothness.Y) : 1f;
                float num4 = num2 * num3;
                bool flag = false;
                Vector3 vector = num > 0f ? (movingBlockSet.TargetPosition - movingBlockSet.Position) / num : Vector3.Zero;
                float x2 = m_subsystemTime.GameTimeDelta > 0f ? 0.95f / m_subsystemTime.GameTimeDelta : 0f;
                float num5 = MathUtils.Min(movingBlockSet.Speed * num4, x2);
                if (num5 * m_subsystemTime.GameTimeDelta >= num) {
                    movingBlockSet.Position = movingBlockSet.TargetPosition;
                    movingBlockSet.CurrentVelocity = Vector3.Zero;
                    flag = true;
                }
                else {
                    movingBlockSet.CurrentVelocity = num5 / num * (movingBlockSet.TargetPosition - movingBlockSet.Position);
                    movingBlockSet.Position += movingBlockSet.CurrentVelocity * m_subsystemTime.GameTimeDelta;
                }
                movingBlockSet.Stop = false;
                MovingBlocksCollision(movingBlockSet);
                TerrainCollision(movingBlockSet);
                if (movingBlockSet.Stop) {
                    if (vector.X < 0f) {
                        movingBlockSet.Position.X = MathF.Ceiling(movingBlockSet.Position.X);
                    }
                    else if (vector.X > 0f) {
                        movingBlockSet.Position.X = MathF.Floor(movingBlockSet.Position.X);
                    }
                    if (vector.Y < 0f) {
                        movingBlockSet.Position.Y = MathF.Ceiling(movingBlockSet.Position.Y);
                    }
                    else if (vector.Y > 0f) {
                        movingBlockSet.Position.Y = MathF.Floor(movingBlockSet.Position.Y);
                    }
                    if (vector.Z < 0f) {
                        movingBlockSet.Position.Z = MathF.Ceiling(movingBlockSet.Position.Z);
                    }
                    else if (vector.Z > 0f) {
                        movingBlockSet.Position.Z = MathF.Floor(movingBlockSet.Position.Z);
                    }
                }
                if (movingBlockSet.Stop || flag) {
                    m_stopped.Add(movingBlockSet);
                }
            }
            foreach (MovingBlockSet item in m_stopped) {
                Stopped?.Invoke(item);
            }
            m_stopped.Clear();
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            foreach (TerrainChunkGeometry.Buffer buffer in Buffers) {
                buffer.Dispose();
            }
            Buffers.Clear();
            foreach (MovingBlockSet movingBlockSet2 in m_movingBlockSets) {
                DrawMovingBlockSet(camera, movingBlockSet2);
            }
            int num = 0;
            while (num < m_removing.Count) {
                MovingBlockSet movingBlockSet = m_removing[num];
                if (movingBlockSet.RemainCounter-- > 0) {
                    DrawMovingBlockSet(camera, movingBlockSet);
                    num++;
                }
                else {
                    m_removing.RemoveAt(num);
                    movingBlockSet.Dispose();
                }
            }
            for (int i = 0; i < Buffers.Count; i++) {
                TerrainChunkGeometry.Buffer buffer = Buffers[i];
                Vector3 viewPosition = camera.ViewPosition;
                Vector3 vector = new(MathF.Floor(viewPosition.X), 0f, MathF.Floor(viewPosition.Z));
                Matrix value = Matrix.CreateTranslation(vector - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
                Display.BlendState = BlendState.AlphaBlend;
                Display.DepthStencilState = DepthStencilState.Default;
                Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
                m_shader.GetParameter("u_origin").SetValue(vector.XZ);
                m_shader.GetParameter("u_viewProjectionMatrix").SetValue(value);
                m_shader.GetParameter("u_viewPosition").SetValue(camera.ViewPosition);
                m_shader.GetParameter("u_texture").SetValue(buffer.Texture);
                m_shader.GetParameter("u_samplerState").SetValue(SamplerState.PointClamp);
                m_shader.GetParameter("u_fogYMultiplier").SetValue(m_subsystemSky.VisibilityRangeYMultiplier);
                m_shader.GetParameter("u_fogColor").SetValue(new Vector3(m_subsystemSky.ViewFogColor));
                m_shader.GetParameter("u_fogBottomTopDensity")
                    .SetValue(new Vector3(m_subsystemSky.ViewFogBottom, m_subsystemSky.ViewFogTop, m_subsystemSky.ViewFogDensity));
                m_shader.GetParameter("u_hazeStartDensity").SetValue(new Vector2(m_subsystemSky.ViewHazeStart, m_subsystemSky.ViewHazeDensity));
                m_shader.GetParameter("u_alphaThreshold").SetValue(0.5f);
                Display.DrawIndexed(
                    PrimitiveType.TriangleList,
                    m_shader,
                    buffer.VertexBuffer,
                    buffer.IndexBuffer,
                    0,
                    buffer.IndexBuffer.IndicesCount
                );
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemAnimatedTextures = Project.FindSubsystem<SubsystemAnimatedTextures>(true);
            m_shader = ContentManager.Get<Shader>("Shaders/AlphaTested");
            Buffers = [];
            foreach (ValuesDictionary value9 in valuesDictionary.GetValue<ValuesDictionary>("MovingBlockSets").Values) {
                Vector3 value = value9.GetValue<Vector3>("Position");
                Vector3 value2 = value9.GetValue<Vector3>("TargetPosition");
                float value3 = value9.GetValue<float>("Speed");
                float value4 = value9.GetValue<float>("Acceleration");
                float value5 = value9.GetValue<float>("Drag");
                Vector2 value6 = value9.GetValue("Smoothness", Vector2.Zero);
                string value7 = value9.GetValue<string>("Id", null);
                object value8 = value9.GetValue<object>("Tag", null);
                List<MovingBlock> list = [];
                string[] array = value9.GetValue<string>("Blocks").Split([';'], StringSplitOptions.RemoveEmptyEntries);
                foreach (string obj2 in array) {
                    MovingBlock item = new();
                    string[] array2 = obj2.Split([','], StringSplitOptions.RemoveEmptyEntries);
                    item.Value = HumanReadableConverter.ConvertFromString<int>(array2[0]);
                    item.Offset.X = HumanReadableConverter.ConvertFromString<int>(array2[1]);
                    item.Offset.Y = HumanReadableConverter.ConvertFromString<int>(array2[2]);
                    item.Offset.Z = HumanReadableConverter.ConvertFromString<int>(array2[3]);
                    list.Add(item);
                }
                AddMovingBlockSet(
                    value,
                    value2,
                    value3,
                    value4,
                    value5,
                    value6,
                    list,
                    value7,
                    value8,
                    false
                );
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            ValuesDictionary valuesDictionary2 = [];
            valuesDictionary.SetValue("MovingBlockSets", valuesDictionary2);
            int num = 0;
            foreach (MovingBlockSet movingBlockSet in m_movingBlockSets) {
                ValuesDictionary valuesDictionary3 = [];
                valuesDictionary2.SetValue(num.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
                valuesDictionary3.SetValue("Position", movingBlockSet.Position);
                valuesDictionary3.SetValue("TargetPosition", movingBlockSet.TargetPosition);
                valuesDictionary3.SetValue("Speed", movingBlockSet.Speed);
                valuesDictionary3.SetValue("Acceleration", movingBlockSet.Acceleration);
                valuesDictionary3.SetValue("Drag", movingBlockSet.Drag);
                if (movingBlockSet.Smoothness != Vector2.Zero) {
                    valuesDictionary3.SetValue("Smoothness", movingBlockSet.Smoothness);
                }
                if (movingBlockSet.Id != null) {
                    valuesDictionary3.SetValue("Id", movingBlockSet.Id);
                }
                if (movingBlockSet.Tag != null) {
                    valuesDictionary3.SetValue("Tag", movingBlockSet.Tag);
                }
                StringBuilder stringBuilder = new();
                foreach (MovingBlock block in movingBlockSet.Blocks) {
                    stringBuilder.Append(HumanReadableConverter.ConvertToString(block.Value));
                    stringBuilder.Append(',');
                    stringBuilder.Append(HumanReadableConverter.ConvertToString(block.Offset.X));
                    stringBuilder.Append(',');
                    stringBuilder.Append(HumanReadableConverter.ConvertToString(block.Offset.Y));
                    stringBuilder.Append(',');
                    stringBuilder.Append(HumanReadableConverter.ConvertToString(block.Offset.Z));
                    stringBuilder.Append(';');
                }
                valuesDictionary3.SetValue("Blocks", stringBuilder.ToString());
                num++;
            }
        }

        public override void Dispose() {
            if (m_blockGeometryGenerator != null
                && m_blockGeometryGenerator.Terrain != null) {
                m_blockGeometryGenerator.Terrain.Dispose();
            }
            foreach (MovingBlockSet movingBlockSet in m_movingBlockSets) {
                movingBlockSet.Dispose();
            }
            foreach (MovingBlockSet item in m_removing) {
                item.Dispose();
            }
        }

        public void MovingBlocksCollision(MovingBlockSet movingBlockSet) {
            BoundingBox boundingBox = movingBlockSet.BoundingBox(true);
            m_result.Clear();
            FindMovingBlocks(boundingBox, true, m_result);
            for (int i = 0; i < m_result.Count; i++) {
                if (m_result.Array[i] != movingBlockSet) {
                    movingBlockSet.Stop = true;
                    break;
                }
            }
        }

        public void TerrainCollision(MovingBlockSet movingBlockSet) {
            Point3 point = default;
            point.X = (int)MathF.Floor(movingBlockSet.Box.Left + movingBlockSet.Position.X);
            point.Y = (int)MathF.Floor(movingBlockSet.Box.Top + movingBlockSet.Position.Y);
            point.Z = (int)MathF.Floor(movingBlockSet.Box.Near + movingBlockSet.Position.Z);
            Point3 point2 = default;
            point2.X = (int)MathF.Ceiling(movingBlockSet.Box.Right + movingBlockSet.Position.X);
            point2.Y = (int)MathF.Ceiling(movingBlockSet.Box.Bottom + movingBlockSet.Position.Y);
            point2.Z = (int)MathF.Ceiling(movingBlockSet.Box.Far + movingBlockSet.Position.Z);
            for (int i = point.X; i < point2.X; i++) {
                for (int j = point.Z; j < point2.Z; j++) {
                    for (int k = point.Y; k < point2.Y; k++) {
                        if (Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(i, k, j)) != 0) {
                            CollidedWithTerrain?.Invoke(movingBlockSet, new Point3(i, k, j));
                        }
                    }
                }
            }
        }

        public void GenerateGeometry(MovingBlockSet movingBlockSet) {
            Point3 point = default;
            point.X = movingBlockSet.CurrentVelocity.X > 0f
                ? (int)MathF.Floor(movingBlockSet.Position.X)
                : (int)MathF.Ceiling(movingBlockSet.Position.X);
            point.Y = movingBlockSet.CurrentVelocity.Y > 0f
                ? (int)MathF.Floor(movingBlockSet.Position.Y)
                : (int)MathF.Ceiling(movingBlockSet.Position.Y);
            point.Z = movingBlockSet.CurrentVelocity.Z > 0f
                ? (int)MathF.Floor(movingBlockSet.Position.Z)
                : (int)MathF.Ceiling(movingBlockSet.Position.Z);
            if (!(point != movingBlockSet.GeometryGenerationPosition)) {
                return;
            }
            Point3 p = new(movingBlockSet.Box.Left, movingBlockSet.Box.Top, movingBlockSet.Box.Near);
            Point3 point2 = new(movingBlockSet.Box.Width, movingBlockSet.Box.Height, movingBlockSet.Box.Depth);
            int num = point.Y + p.Y;
            point2.Y = MathUtils.Min(point2.Y, 254);
            if (m_blockGeometryGenerator == null) {
                int x = 2;
                x = (int)MathUtils.NextPowerOf2((uint)x);
                m_blockGeometryGenerator = new BlockGeometryGenerator(
                    new Terrain(),
                    m_subsystemTerrain,
                    null,
                    Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(true),
                    null,
                    Project.FindSubsystem<SubsystemPalette>(true)
                );
                for (int i = 0; i < x; i++) {
                    for (int j = 0; j < x; j++) {
                        m_blockGeometryGenerator.Terrain.AllocateChunk(i, j);
                    }
                }
            }
            Terrain terrain = m_subsystemTerrain.Terrain;
            for (int k = 0; k < point2.X + 2; k++) {
                for (int l = 0; l < point2.Z + 2; l++) {
                    int x2 = k + p.X + point.X - 1;
                    int z = l + p.Z + point.Z - 1;
                    int shaftValue = terrain.GetShaftValue(x2, z);
                    m_blockGeometryGenerator.Terrain.SetTemperature(k, l, Terrain.ExtractTemperature(shaftValue));
                    m_blockGeometryGenerator.Terrain.SetHumidity(k, l, Terrain.ExtractHumidity(shaftValue));
                    for (int m = 0; m < point2.Y + 2; m++) {
                        if (m_blockGeometryGenerator.Terrain.IsCellValid(k, m + num, l)) {
                            int y = m + p.Y + point.Y - 1;
                            int cellValue = terrain.GetCellValue(x2, y, z);
                            int num2 = Terrain.ExtractContents(cellValue);
                            int light = Terrain.ExtractLight(cellValue);
                            int shadowStrength = BlocksManager.Blocks[num2].GetShadowStrength(cellValue);
                            int value = Terrain.MakeBlockValue(257, light, ShadowBlock.SetShadowStrength(0, shadowStrength));
                            m_blockGeometryGenerator.Terrain.SetCellValueFast(k, m + num, l, value);
                        }
                    }
                }
            }
            m_blockGeometryGenerator.Terrain.SeasonTemperature = terrain.SeasonTemperature;
            m_blockGeometryGenerator.Terrain.SeasonHumidity = terrain.SeasonHumidity;
            foreach (MovingBlock block in movingBlockSet.Blocks) {
                int x3 = block.Offset.X - p.X + 1;
                int num3 = block.Offset.Y - p.Y + 1;
                int z2 = block.Offset.Z - p.Z + 1;
                if (m_blockGeometryGenerator.Terrain.IsCellValid(x3, num3 + num, z2)) {
                    int cellLightFast = m_blockGeometryGenerator.Terrain.GetCellLightFast(x3, num3 + num, z2);
                    int value2 = Terrain.ReplaceLight(block.Value, cellLightFast);
                    m_blockGeometryGenerator.Terrain.SetCellValueFast(x3, num3 + num, z2, value2);
                }
            }
            m_blockGeometryGenerator.ResetCache();
            movingBlockSet.Geometry.ClearGeometry();
            for (int n = 1; n < point2.X + 1; n++) {
                for (int num4 = 1; num4 < point2.Y + 1; num4++) {
                    for (int num5 = 1; num5 < point2.Z + 1; num5++) {
                        if (num4 + num > 0
                            && num4 + num < TerrainChunk.HeightMinusOne) {
                            int cellValueFast = m_blockGeometryGenerator.Terrain.GetCellValueFast(n, num4 + num, num5);
                            int num6 = Terrain.ExtractContents(cellValueFast);
                            if (num6 != 0) {
                                if (m_blockGeometryGenerator.Terrain.GetChunkAtCell(n + 1, num5) == null) {
                                    m_blockGeometryGenerator.Terrain.AllocateChunk((n + 1) >> 4, num5 >> 4);
                                }
                                if (m_blockGeometryGenerator.Terrain.GetChunkAtCell(n, num5 + 1) == null) {
                                    m_blockGeometryGenerator.Terrain.AllocateChunk(n >> 4, (num5 + 1) >> 4);
                                }
                                BlocksManager.Blocks[num6]
                                    .GenerateTerrainVertices(m_blockGeometryGenerator, movingBlockSet.Geometry, cellValueFast, n, num4 + num, num5);
                            }
                        }
                    }
                }
            }
            movingBlockSet.GeometryOffset = new Vector3(p) + new Vector3(0f, -num, 0f) - new Vector3(1f);
            movingBlockSet.GeometryGenerationPosition = point;
        }

        public virtual void DrawMovingBlockSet(Camera camera, MovingBlockSet movingBlockSet) {
            if (camera.ViewFrustum.Intersection(movingBlockSet.BoundingBox(false))) {
                GenerateGeometry(movingBlockSet);
                Vector3 vector = movingBlockSet.Position + movingBlockSet.GeometryOffset;
                TerrainRenderer.CompileDrawSubsets(
                    [movingBlockSet.Geometry],
                    Buffers,
                    item => {
                        item.X += vector.X;
                        item.Y += vector.Y;
                        item.Z += vector.Z;
                        return item;
                    }
                );
            }
        }

        public static bool ExclusiveBoxIntersection(BoundingBox b1, BoundingBox b2) {
            if (b1.Max.X > b2.Min.X
                && b1.Min.X < b2.Max.X
                && b1.Max.Y > b2.Min.Y
                && b1.Min.Y < b2.Max.Y
                && b1.Max.Z > b2.Min.Z) {
                return b1.Min.Z < b2.Max.Z;
            }
            return false;
        }

        public virtual void AddTerrainBlock(int x, int y, int z, int value, MovingBlock movingBlock) {
            try {
                if (movingBlock == null) {
                    throw new NullReferenceException("Moving Block Set cannot be null when stop block movement!");
                }
                movingBlock?.MovingBlockSet?.Stop();
                m_subsystemTerrain.DestroyCell(
                    0,
                    x,
                    y,
                    z,
                    value,
                    m_noDropOnMovingBlockStopped,
                    false,
                    movingBlock
                );
                //m_subsystemTerrain.ChangeCell(x,y,z,value,true,movingBlock);
            }
            catch (Exception ex) {
                Log.Error($"Add terrain block when moving blocks stop error: {ex}");
            }
        }
    }
}