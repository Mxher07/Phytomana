using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemExplosions : Subsystem, IUpdateable {
        public class SparseSpatialArray<T> {
            public const int m_bits1 = 4;

            public const int m_bits2 = 4;

            public const int m_mask1 = 15;

            public const int m_mask2 = 15;

            public const int m_diameter = 256;

            public int m_originX;

            public int m_originY;

            public int m_originZ;

            public T[][] m_data;

            public T m_outside;

            public SparseSpatialArray(int centerX, int centerY, int centerZ, T outside) {
                m_data = new T[4096][];
                m_originX = centerX - 128;
                m_originY = centerY - 128;
                m_originZ = centerZ - 128;
                m_outside = outside;
            }

            public T Get(int x, int y, int z) {
                x -= m_originX;
                y -= m_originY;
                z -= m_originZ;
                if (x >= 0
                    && x < 256
                    && y >= 0
                    && y < 256
                    && z >= 0
                    && z < 256) {
                    int num = x >> 4;
                    int explosionPower = y >> 4;
                    int num3 = z >> 4;
                    int num4 = num + (explosionPower << 4) + (num3 << 4 << 4);
                    T[] array = m_data[num4];
                    if (array != null) {
                        int num5 = x & 0xF;
                        int num6 = y & 0xF;
                        int num7 = z & 0xF;
                        int num8 = num5 + (num6 << 4) + (num7 << 4 << 4);
                        return array[num8];
                    }
                    return default;
                }
                return m_outside;
            }

            public void Set(int x, int y, int z, T value) {
                x -= m_originX;
                y -= m_originY;
                z -= m_originZ;
                if (x >= 0
                    && x < 256
                    && y >= 0
                    && y < 256
                    && z >= 0
                    && z < 256) {
                    int num = x >> 4;
                    int explosionPower = y >> 4;
                    int num3 = z >> 4;
                    int num4 = num + (explosionPower << 4) + (num3 << 4 << 4);
                    T[] array = m_data[num4];
                    if (array == null) {
                        array = new T[4096];
                        m_data[num4] = array;
                    }
                    int num5 = x & 0xF;
                    int num6 = y & 0xF;
                    int num7 = z & 0xF;
                    int num8 = num5 + (num6 << 4) + (num7 << 4 << 4);
                    array[num8] = value;
                }
            }

            public void Clear() {
                for (int i = 0; i < m_data.Length; i++) {
                    m_data[i] = null;
                }
            }

            public Dictionary<Point3, T> ToDictionary() {
                Dictionary<Point3, T> dictionary = new();
                for (int i = 0; i < m_data.Length; i++) {
                    T[] array = m_data[i];
                    if (array == null) {
                        continue;
                    }
                    int num = m_originX + ((i & 0xF) << 4);
                    int explosionPower = m_originY + (((i >> 4) & 0xF) << 4);
                    int num3 = m_originZ + (((i >> 8) & 0xF) << 4);
                    for (int j = 0; j < array.Length; j++) {
                        if (!Equals(array[j], default(T))) {
                            int num4 = j & 0xF;
                            int num5 = (j >> 4) & 0xF;
                            int num6 = (j >> 8) & 0xF;
                            dictionary.Add(new Point3(num + num4, explosionPower + num5, num3 + num6), array[j]);
                        }
                    }
                }
                return dictionary;
            }
        }

        public struct ExplosionData {
            public ExplosionData() { }

            public int X;

            public int Y;

            public int Z;

            public float Pressure;

            public bool IsIncendiary;

            public bool NoExplosionSound;

            /// <summary>
            ///     模组如果需要添加或使用额外信息，可以在这个ValuesDictionary读写元素
            /// </summary>
            public ValuesDictionary ValuesDictionaryForMods = new();
        }

        public struct ProcessPoint {
            public int X;

            public int Y;

            public int Z;

            public int Axis;
        }

        public struct SurroundingPressurePoint {
            public float Pressure;

            public bool IsIncendiary;
        }

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemAudio m_subsystemAudio;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemNoise m_subsystemNoise;

        public SubsystemBodies m_subsystemBodies;

        public SubsystemPickables m_subsystemPickables;

        public SubsystemProjectiles m_subsystemProjectiles;

        public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

        public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

        public List<ExplosionData> m_queuedExplosions = [];

        public SparseSpatialArray<float> m_pressureByPoint;

        public SparseSpatialArray<SurroundingPressurePoint> m_surroundingPressureByPoint;

        public int m_projectilesCount;

        public Dictionary<Projectile, bool> m_generatedProjectiles = [];

        public Random m_random = new();

        public ExplosionParticleSystem m_explosionParticleSystem;

        public bool ShowExplosionPressure;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public bool TryExplodeBlock(int x, int y, int z, int value) {
            int num = Terrain.ExtractContents(value);
            Block obj = BlocksManager.Blocks[num];
            float explosionPressure = obj.GetExplosionPressure(value);
            bool explosionIncendiary = obj.GetExplosionIncendiary(value);
            if (explosionPressure > 0f) {
                AddExplosion(x, y, z, explosionPressure, explosionIncendiary, false);
                return true;
            }
            return false;
        }

        public void AddExplosion(int x, int y, int z, float pressure, bool isIncendiary, bool noExplosionSound) {
            if (pressure > 0f) {
                m_queuedExplosions.Add(
                    new ExplosionData { X = x, Y = y, Z = z, Pressure = pressure, IsIncendiary = isIncendiary, NoExplosionSound = noExplosionSound }
                );
                ApplyBodiesShaking(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), pressure);
            }
        }

        public virtual void Update(float dt) {
            if (m_queuedExplosions.Count <= 0) {
                return;
            }
            int x = m_queuedExplosions[0].X;
            int y = m_queuedExplosions[0].Y;
            int z = m_queuedExplosions[0].Z;
            m_pressureByPoint = new SparseSpatialArray<float>(x, y, z, 0f);
            m_surroundingPressureByPoint = new SparseSpatialArray<SurroundingPressurePoint>(
                x,
                y,
                z,
                new SurroundingPressurePoint { IsIncendiary = false, Pressure = 0f }
            );
            m_projectilesCount = 0;
            m_generatedProjectiles.Clear();
            bool flag = false;
            int num = 0;
            while (num < m_queuedExplosions.Count) {
                ExplosionData explosionData = m_queuedExplosions[num];
                if (MathF.Abs(explosionData.X - x) <= 4
                    && MathF.Abs(explosionData.Y - y) <= 4
                    && MathF.Abs(explosionData.Z - z) <= 4) {
                    m_queuedExplosions.RemoveAt(num);
                    //Task.Run(() => SimulateExplosion(explosionData.X, explosionData.Y, explosionData.Z, explosionData.Pressure, explosionData.IsIncendiary));
                    SimulateExplosion(explosionData.X, explosionData.Y, explosionData.Z, explosionData.Pressure, explosionData.IsIncendiary);
                    flag |= !explosionData.NoExplosionSound;
                }
                else {
                    num++;
                }
            }
            /*
            for (int num1 = 0; num1 < m_queuedExplosions.Count;num1++)
            {
                ExplosionData explosionData = m_queuedExplosions[num1];
                while (!(MathF.Abs(explosionData.X - x) <= 4 && MathF.Abs(explosionData.Y - y) <= 4 && MathF.Abs(explosionData.Z - z) <= 4))
                {
                    m_queuedExplosions.RemoveAt(num1);
                    SimulateExplosion(explosionData.X, explosionData.Y, explosionData.Z, explosionData.Pressure, explosionData.IsIncendiary);
                    flag |= !explosionData.NoExplosionSound;
                }
            }
            */ /*
            int m_queuedExplosionsCount = m_queuedExplosions.Count;
            int[] indices = Enumerable.Range(0, m_queuedExplosionsCount).ToArray();

            // 使用 Parallel.For 并行执行循环
            Parallel.For(0, m_queuedExplosionsCount, (i, loopState) =>
            {
                int num1 = indices[i];
                ExplosionData explosionData = m_queuedExplosions[num1];
                if (MathF.Abs(explosionData.X - x) <= 4 && MathF.Abs(explosionData.Y - y) <= 4 && MathF.Abs(explosionData.Z - z) <= 4)
                {
                    m_queuedExplosions.RemoveAt(num1);
                    SimulateExplosion(explosionData.X, explosionData.Y, explosionData.Z, explosionData.Pressure, explosionData.IsIncendiary);
                    flag |= !explosionData.NoExplosionSound;
                }
            });
            */ /*
            int index = 0;
            foreach (var explosionData in m_queuedExplosions)
            {
                if (MathF.Abs(explosionData.X - x) <= 4 && MathF.Abs(explosionData.Y - y) <= 4 && MathF.Abs(explosionData.Z - z) <= 4)
                {
                    m_queuedExplosions.RemoveAt(index);
                    Task.Run(() => SimulateExplosion(explosionData.X, explosionData.Y, explosionData.Z, explosionData.Pressure, explosionData.IsIncendiary));
                    flag |= !explosionData.NoExplosionSound;
                }
                else
                {
                    index++;
                }

            }
            */
            PostprocessExplosions(flag);
            if (!ShowExplosionPressure) {
                m_pressureByPoint = null;
                m_surroundingPressureByPoint = null;
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemNoise = Project.FindSubsystem<SubsystemNoise>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemPickables = Project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemProjectiles = Project.FindSubsystem<SubsystemProjectiles>(true);
            m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true);
            m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(true);
            m_explosionParticleSystem = new ExplosionParticleSystem();
            m_subsystemParticles.AddParticleSystem(m_explosionParticleSystem);
        }

        public void SimulateExplosion(int x, int y, int z, float pressure, bool isIncendiary) {
            int explosionPointValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
            float num = MathUtils.Max(0.13f * MathF.Pow(pressure, 0.5f), 1f);
            m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(0));
            SparseSpatialArray<bool> processed = new(x, y, z, true);
            List<ProcessPoint> list = new();
            List<ProcessPoint> list2 = new();
            List<ProcessPoint> list3 = new();
            TryAddPoint(
                x,
                y,
                z,
                -1,
                pressure,
                isIncendiary,
                list,
                processed
            );
            int explosionPower = 0;
            int num3 = 0;
            if (Terrain.ExtractContents(explosionPointValue) != 0) {
                ModsManager.HookAction(
                    "OnBlockExploded",
                    loader => {
                        loader.OnBlockExploded(m_subsystemTerrain, x, y, z, explosionPointValue);
                        return false;
                    }
                );
            }
            while (list.Count > 0
                || list2.Count > 0) {
                explosionPower += list.Count;
                num3++;
                float num4 = 5f * MathUtils.Max(num3 - 7, 0);
                float num5 = pressure / (MathF.Pow(explosionPower, 0.66f) + num4);
                if (num5 >= num) {
                    foreach (ProcessPoint item in list) {
                        float num6 = m_pressureByPoint.Get(item.X, item.Y, item.Z);
                        float num7 = num5 + num6;
                        m_pressureByPoint.Set(item.X, item.Y, item.Z, num7);
                        if (item.Axis == 0) {
                            TryAddPoint(
                                item.X - 1,
                                item.Y,
                                item.Z,
                                0,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X + 1,
                                item.Y,
                                item.Z,
                                0,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y - 1,
                                item.Z,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y + 1,
                                item.Z,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y,
                                item.Z - 1,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y,
                                item.Z + 1,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                        }
                        else if (item.Axis == 1) {
                            TryAddPoint(
                                item.X - 1,
                                item.Y,
                                item.Z,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X + 1,
                                item.Y,
                                item.Z,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y - 1,
                                item.Z,
                                1,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y + 1,
                                item.Z,
                                1,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y,
                                item.Z - 1,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y,
                                item.Z + 1,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                        }
                        else if (item.Axis == 2) {
                            TryAddPoint(
                                item.X - 1,
                                item.Y,
                                item.Z,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X + 1,
                                item.Y,
                                item.Z,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y - 1,
                                item.Z,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y + 1,
                                item.Z,
                                -1,
                                num7,
                                isIncendiary,
                                list2,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y,
                                item.Z - 1,
                                2,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y,
                                item.Z + 1,
                                2,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                        }
                        else {
                            TryAddPoint(
                                item.X - 1,
                                item.Y,
                                item.Z,
                                0,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X + 1,
                                item.Y,
                                item.Z,
                                0,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y - 1,
                                item.Z,
                                1,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y + 1,
                                item.Z,
                                1,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y,
                                item.Z - 1,
                                2,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                            TryAddPoint(
                                item.X,
                                item.Y,
                                item.Z + 1,
                                2,
                                num7,
                                isIncendiary,
                                list3,
                                processed
                            );
                        }
                    }
                }
                List<ProcessPoint> list4 = list;
                list4.Clear();
                list = list2;
                list2 = list3;
                list3 = list4;
            }
        }

        public void TryAddPoint(int x,
            int y,
            int z,
            int axis,
            float currentPressure,
            bool isIncendiary,
            List<ProcessPoint> toProcess,
            SparseSpatialArray<bool> processed) {
            if (processed.Get(x, y, z)) {
                return;
            }
            int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
            int num = Terrain.ExtractContents(cellValue);
            if (num != 0) {
                int explosionPower = (int)(MathUtils.Hash((uint)(x + 913 * y + 217546 * z)) % 100u);
                float num3 = MathUtils.Lerp(1f, 2f, explosionPower / 100f);
                if (explosionPower % 8 == 0) {
                    num3 *= 3f;
                }
                Block block = BlocksManager.Blocks[num];
                float num4 = m_pressureByPoint.Get(x - 1, y, z)
                    + m_pressureByPoint.Get(x + 1, y, z)
                    + m_pressureByPoint.Get(x, y - 1, z)
                    + m_pressureByPoint.Get(x, y + 1, z)
                    + m_pressureByPoint.Get(x, y, z - 1)
                    + m_pressureByPoint.Get(x, y, z + 1);
                float num5 = MathUtils.Max(block.GetExplosionResilience(cellValue) * num3, 1f);
                float num6 = num4 / num5;
                if (num6 > 1f) {
                    int newValue = Terrain.MakeBlockValue(0);
                    m_subsystemTerrain.DestroyCell(
                        0,
                        x,
                        y,
                        z,
                        newValue,
                        true,
                        true
                    );
                    bool flag = false;
                    float probability = num6 > 5f ? 0.95f : 0.75f;
                    if (m_random.Bool(probability)) {
                        flag = TryExplodeBlock(x, y, z, cellValue);
                    }
                    if (!flag) {
                        CalculateImpulseAndDamage(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), 60f, 2f * num4, out Vector3 impulse, out float _);
                        bool flag2 = false;
                        List<BlockDropValue> list = new();
                        block.GetDropValues(m_subsystemTerrain, cellValue, newValue, 0, list, out bool _);
                        ModsManager.HookAction(
                            "OnBlockExploded",
                            loader => {
                                loader.OnBlockExploded(m_subsystemTerrain, x, y, z, cellValue);
                                return false;
                            }
                        );
                        if (list.Count == 0) {
                            list.Add(new BlockDropValue { Value = cellValue, Count = 1 });
                            flag2 = true;
                        }
                        foreach (BlockDropValue item in list) {
                            int num7 = Terrain.ExtractContents(item.Value);
                            Block block2 = BlocksManager.Blocks[num7];
                            if (block2 is FluidBlock) {
                                continue;
                            }
                            float num8 = m_projectilesCount < 40 || block2.ExplosionKeepsPickables ? 1f :
                                m_projectilesCount < 60 ? 0.5f :
                                m_projectilesCount >= 80 ? 0.125f : 0.25f;
                            if (!(m_random.Float(0f, 1f) < num8)) {
                                continue;
                            }
                            Vector3 velocity = impulse + m_random.Vector3(0.05f * impulse.Length());
                            if (m_projectilesCount >= 1) {
                                velocity *= m_random.Float(0.5f, 1f);
                                velocity += m_random.Vector3(0.2f * velocity.Length());
                            }
                            float num9 = flag2 ? 0f :
                                block2.ExplosionKeepsPickables ? 1f : MathUtils.Lerp(1f, 0f, m_projectilesCount / 25f);
                            Projectile projectile = m_subsystemProjectiles.AddProjectile(
                                item.Value,
                                new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
                                velocity,
                                m_random.Vector3(0f, 20f),
                                null
                            );
                            projectile.ProjectileStoppedAction = !(m_random.Float(0f, 1f) < num9)
                                ? ProjectileStoppedAction.Disappear
                                : ProjectileStoppedAction.TurnIntoPickable;
                            if (m_random.Float(0f, 1f) < 0.5f
                                && m_projectilesCount < 35) {
                                float num10 = num4 > 60f ? m_random.Float(3f, 7f) : m_random.Float(1f, 3f);
                                if (isIncendiary) {
                                    num10 += 10f;
                                }
                                m_subsystemProjectiles.AddTrail(
                                    projectile,
                                    Vector3.Zero,
                                    new SmokeTrailParticleSystem(
                                        15,
                                        m_random.Float(0.75f, 1.5f),
                                        num10,
                                        isIncendiary ? new Color(255, 140, 192) : Color.White
                                    )
                                );
                                projectile.IsIncendiary = isIncendiary;
                            }
                            m_generatedProjectiles.Add(projectile, true);
                            m_projectilesCount++;
                        }
                    }
                }
                else {
                    m_surroundingPressureByPoint.Set(x, y, z, new SurroundingPressurePoint { Pressure = num4, IsIncendiary = isIncendiary });
                    if (block.IsCollidable_(cellValue)) {
                        return;
                    }
                }
            }
            toProcess.Add(new ProcessPoint { X = x, Y = y, Z = z, Axis = axis });
            processed.Set(x, y, z, true);
        }

        public virtual void ApplyBodiesShaking(Vector3 center, float pressure) {
            foreach (ComponentBody body in m_subsystemBodies.Bodies) {
                body.UnderExplosionStart(center, pressure);
            }
        }

        public virtual void PostprocessExplosions(bool playExplosionSound) {
            Point3 point = Point3.Zero;
            float num = float.MaxValue;
            float pressure = 0f;
            foreach (KeyValuePair<Point3, float> item in m_pressureByPoint.ToDictionary()) {
                pressure += item.Value;
                float num3 = m_subsystemAudio.CalculateListenerDistance(new Vector3(item.Key));
                if (num3 < num) {
                    num = num3;
                    point = item.Key;
                }
                float num4 = 0.001f * MathF.Pow(pressure, 0.5f);
                float num5 = MathUtils.Saturate(item.Value / 15f - num4) * m_random.Float(0.2f, 1f);
                if (num5 > 0.1f) {
                    m_explosionParticleSystem.SetExplosionCell(item.Key, num5);
                }
            }
            ModsManager.HookAction(
                "CalculateExplosionPower",
                loader => {
                    loader.CalculateExplosionPower(this, ref pressure);
                    return false;
                }
            );
            foreach (KeyValuePair<Point3, SurroundingPressurePoint> item2 in m_surroundingPressureByPoint.ToDictionary()) {
                int cellValue = m_subsystemTerrain.Terrain.GetCellValue(item2.Key.X, item2.Key.Y, item2.Key.Z);
                int num6 = Terrain.ExtractContents(cellValue);
                SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(cellValue));
                if (blockBehaviors.Length != 0) {
                    for (int i = 0; i < blockBehaviors.Length; i++) {
                        blockBehaviors[i].OnExplosion(cellValue, item2.Key.X, item2.Key.Y, item2.Key.Z, item2.Value.Pressure);
                    }
                }
                float probability = item2.Value.IsIncendiary ? 0.5f : 0.2f;
                Block block = BlocksManager.Blocks[num6];
                if (block.GetFireDuration(cellValue) > 0f
                    && item2.Value.Pressure / block.GetExplosionResilience(cellValue) > 0.2f
                    && m_random.Bool(probability)) {
                    m_subsystemFireBlockBehavior.SetCellOnFire(item2.Key.X, item2.Key.Y, item2.Key.Z, item2.Value.IsIncendiary ? 1f : 0.3f);
                }
            }
            foreach (ComponentBody body in m_subsystemBodies.Bodies) {
                CalculateImpulseAndDamage(body, null, out Vector3 impulse, out float damage);
                body.UnderExplosion(impulse, damage);
            }
            foreach (Pickable pickable in m_subsystemPickables.Pickables) {
                //Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
                CalculateImpulseAndDamage(
                    pickable.Position + new Vector3(0f, 0.5f, 0f),
                    pickable.ExplosionMass,
                    null,
                    out Vector3 impulse2,
                    out float damage2
                );
                pickable.Project = Project;
                pickable.UnderExplosion(impulse2, damage2);
            }
            foreach (Projectile projectile2 in m_subsystemProjectiles.Projectiles) {
                if (!m_generatedProjectiles.ContainsKey(projectile2)) {
                    CalculateImpulseAndDamage(
                        projectile2.Position + new Vector3(0f, 0.5f, 0f),
                        projectile2.ExplosionMass,
                        null,
                        out Vector3 impulse3,
                        out float damage3
                    );
                    projectile2.UnderExplosion(impulse3, damage3);
                }
            }
            Vector3 position = new(point.X, point.Y, point.Z);
            float delay = m_subsystemAudio.CalculateDelay(num);
            if (pressure > 2000000f) {
                if (playExplosionSound) {
                    m_subsystemAudio.PlaySound("Audio/Explosion7", 1f, m_random.Float(-0.1f, 0.1f), position, 53f, delay);
                }
                m_subsystemNoise.MakeNoise(position, 1f, 130f);
            }
            else if (pressure > 400000f) {
                if (playExplosionSound) {
                    m_subsystemAudio.PlaySound("Audio/Explosion6", 1f, m_random.Float(-0.1f, 0.1f), position, 45f, delay);
                }
                m_subsystemNoise.MakeNoise(position, 1f, 100f);
            }
            else if (pressure > 80000f) {
                if (playExplosionSound) {
                    m_subsystemAudio.PlaySound("Audio/Explosion5", 1f, m_random.Float(-0.1f, 0.1f), position, 38f, delay);
                }
                m_subsystemNoise.MakeNoise(position, 1f, 70f);
            }
            else if (pressure > 16000f) {
                if (playExplosionSound) {
                    m_subsystemAudio.PlaySound("Audio/Explosion4", 1f, m_random.Float(-0.1f, 0.1f), position, 32f, delay);
                }
                m_subsystemNoise.MakeNoise(position, 1f, 50f);
            }
            else if (pressure > 3500f) {
                if (playExplosionSound) {
                    m_subsystemAudio.PlaySound("Audio/Explosion3", 1f, m_random.Float(-0.1f, 0.1f), position, 27f, delay);
                }
                m_subsystemNoise.MakeNoise(position, 1f, 40f);
            }
            else if (pressure > 80f) {
                if (playExplosionSound) {
                    m_subsystemAudio.PlaySound("Audio/Explosion2", 1f, m_random.Float(-0.1f, 0.1f), position, 23f, delay);
                }
                m_subsystemNoise.MakeNoise(position, 1f, 35f);
            }
            else if (pressure > 0f) {
                if (playExplosionSound) {
                    m_subsystemAudio.PlaySound("Audio/Explosion1", 1f, m_random.Float(-0.1f, 0.1f), position, 20f, delay);
                }
                m_subsystemNoise.MakeNoise(position, 1f, 30f);
            }
        }

        public virtual void CalculateImpulseAndDamage(ComponentBody componentBody, float? obstaclePressure, out Vector3 impulse, out float damage) {
            CalculateImpulseAndDamage(
                0.5f * (componentBody.BoundingBox.Min + componentBody.BoundingBox.Max),
                componentBody.Mass,
                obstaclePressure,
                out impulse,
                out damage
            );
        }

        public virtual void CalculateImpulseAndDamage(Vector3 position, float mass, float? obstaclePressure, out Vector3 impulse, out float damage) {
            Point3 point = Terrain.ToCell(position);
            obstaclePressure ??= m_pressureByPoint.Get(point.X, point.Y, point.Z);
            float num = 0f;
            Vector3 zero = Vector3.Zero;
            for (int i = -1; i <= 1; i++) {
                for (int j = -1; j <= 1; j++) {
                    for (int k = -1; k <= 1; k++) {
                        int pressure = point.X + i;
                        int num3 = point.Y + j;
                        int num4 = point.Z + k;
                        float num5 = m_subsystemTerrain.Terrain.GetCellContents(pressure, num3, num4) != 0
                            ? obstaclePressure.Value
                            : m_pressureByPoint.Get(pressure, num3, num4);
                        if (i != 0
                            || j != 0
                            || k != 0) {
                            zero += num5 * Vector3.Normalize(new Vector3(point.X - pressure, point.Y - num3, point.Z - num4));
                        }
                        num += num5;
                    }
                }
            }
            float num6 = MathUtils.Max(MathF.Pow(mass, 0.5f), 1f);
            impulse = 5.5555553f * Vector3.Normalize(zero) * MathF.Sqrt(zero.Length()) / num6;
            damage = 2.59259248f * MathF.Sqrt(num) / num6;
        }
    }
}