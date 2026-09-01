using Engine;
using Engine.Graphics;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemPickables : Subsystem, IDrawable, IUpdateable {
        public SubsystemAudio m_subsystemAudio;

        public SubsystemPlayers m_subsystemPlayers;

        public SubsystemTerrain m_subsystemTerrain;

        public SubsystemSky m_subsystemSky;

        public SubsystemTime m_subsystemTime;

        public SubsystemGameInfo m_subsystemGameInfo;

        public SubsystemParticles m_subsystemParticles;

        public SubsystemExplosions m_subsystemExplosions;

        public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

        public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

        public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

        [Obsolete("该字段已弃用，掉落物被玩家的拾取逻辑被转移到ComponentPickableGathererPlayer中")]
        public List<ComponentPlayer> m_tmpPlayers = [];

        public List<Pickable> m_pickables = [];

        public List<Pickable> m_pickablesToRemove = [];

        public PrimitivesRenderer3D m_primitivesRenderer = new();

        public Random m_random = new();

        public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new();

        public static int[] m_drawOrders = [10];

        public ReadOnlyList<Pickable> Pickables {
            get {
                lock (m_lock) {
                    return new ReadOnlyList<Pickable>(m_pickables);
                }
            }
        }

        public int[] DrawOrders => m_drawOrders;

        public virtual Action<Pickable> PickableAdded { get; set; }
        public virtual Action<Pickable> PickableRemoved { get; set; }
        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        readonly Lock m_lock = new();

        public virtual Pickable AddPickable(Pickable pickable) {
            if (pickable == null) {
                return null;
            }
            //如果掉落物创建时间没有初始化，就初始化一下
            if (pickable.CreationTime == 0) {
                pickable.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
            }
            ModsManager.HookAction(
                "OnPickableAdded",
                loader => {
                    loader.OnPickableAdded(this, ref pickable, null);
                    return false;
                }
            );
            lock (m_lock) {
                m_pickables.Add(pickable);
            }
            PickableAdded?.Invoke(pickable);
            return pickable;
        }

        public virtual Pickable AddPickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix) =>
            AddPickable(value, count, position, velocity, stuckMatrix, null);

        public virtual Pickable AddPickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner) =>
            AddPickable<Pickable>(value, count, position, velocity, stuckMatrix, owner);

        public virtual Pickable CreatePickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner) =>
            CreatePickable<Pickable>(value, count, position, velocity, stuckMatrix, owner);

        public virtual T CreatePickable<T>(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner)
            where T : Pickable, new() {
            try {
                T pickable = new();
                pickable.InitializeData(
                    () => m_subsystemTerrain.Terrain,
                    () => m_drawBlockEnvironmentData,
                    DefaultCalcVisibilityRange,
                    () => m_subsystemSky.CalculateFog,
                    () => m_primitivesRenderer
                );
                pickable.Initialize(value, count, position, velocity, stuckMatrix, owner);
                pickable.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
                return pickable;
            }
            catch (Exception e) {
                Log.Error($"Pickable create error: {e}");
                return null;
            }
        }

        public virtual T AddPickable<T>(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix, Entity owner)
            where T : Pickable, new() {
            try {
                T pickable = CreatePickable<T>(value, count, position, velocity, stuckMatrix, owner);
                Pickable pickable2 = AddPickable(pickable);
                return pickable2 as T;
            }
            catch (Exception e) {
                Log.Error($"Pickable add error: {e}");
                return null;
            }
        }

        public virtual void Draw(Camera camera, int drawOrder) {
            double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
            m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
            Matrix matrix = Matrix.CreateRotationY((float)MathUtils.Remainder(totalElapsedGameTime, 6.2831854820251465));
            lock (m_lock) {
                foreach (Pickable pickable in m_pickables) {
                    try {
                        pickable.Project = Project;
                        pickable.Draw(camera, drawOrder, totalElapsedGameTime, matrix);
                    }
                    catch (Exception e) {
                        if (pickable.LogDrawError) {
                            Log.Error($"Pickable draw error: {e}");
                            pickable.LogDrawError = false;
                        }
                    }
                }
            }
            m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
        }

        public virtual void Update(float dt) {
            lock (m_lock) {
                foreach (Pickable pickable in m_pickables) {
                    if (pickable.ToRemove) {
                        m_pickablesToRemove.Add(pickable);
                    }
                    else {
                        try {
                            pickable.Project = Project;
                            pickable.Update(dt);
                        }
                        catch (Exception e) {
                            Log.Error($"Pickable update error: {e}");
                            pickable.ToRemove = true;
                        }
                    }
                }
                foreach (Pickable item in m_pickablesToRemove) {
                    m_pickables.Remove(item);
                    PickableRemoved?.Invoke(item);
                }
                m_pickablesToRemove.Clear();
            }
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            m_subsystemAudio = Project.FindSubsystem<SubsystemAudio>(true);
            m_subsystemPlayers = Project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemSky = Project.FindSubsystem<SubsystemSky>(true);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemExplosions = Project.FindSubsystem<SubsystemExplosions>(true);
            m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true);
            m_subsystemFireBlockBehavior = Project.FindSubsystem<SubsystemFireBlockBehavior>(true);
            m_subsystemFluidBlockBehavior = Project.FindSubsystem<SubsystemFluidBlockBehavior>(true);
            foreach (ValuesDictionary item in valuesDictionary.GetValue<ValuesDictionary>("Pickables").Values.Where(v => v is ValuesDictionary)) {
                try {
                    string className = item.GetValue("Class", typeof(Pickable).FullName);
                    Type type = TypeCache.FindType(className, false, true);
#pragma warning disable IL2072
                    if (Activator.CreateInstance(type) is Pickable pickable) {
#pragma warning restore IL2072
                        pickable.Project = Project;
                        pickable.InitializeData(
                            () => m_subsystemTerrain.Terrain,
                            () => m_drawBlockEnvironmentData,
                            DefaultCalcVisibilityRange,
                            () => m_subsystemSky.CalculateFog,
                            () => m_primitivesRenderer
                        );
                        pickable.Load(item);
                        ModsManager.HookAction(
                            "OnPickableAdded",
                            loader => {
                                loader.OnPickableAdded(this, ref pickable, item);
                                return false;
                            }
                        );
                        lock (m_pickables) {
                            m_pickables.Add(pickable);
                        }
                    }
                }
                catch (Exception ex) {
                    Log.Error("Pickable Loaded Error");
                    Log.Error(ex);
                }
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Pickables", valuesDictionary2);
            int num = 0;
            lock (m_lock) {
                foreach (Pickable pickable in m_pickables) {
                    ValuesDictionary valuesDictionary3 = new();
                    pickable.Save(valuesDictionary3);
                    ModsManager.HookAction(
                        "SavePickable",
                        loader => {
                            loader.SavePickable(this, pickable, ref valuesDictionary3);
                            return false;
                        }
                    );
                    valuesDictionary2.SetValue(num.ToString(), valuesDictionary3);
                    num++;
                }
            }
        }

        public float DefaultCalcVisibilityRange() => Math.Min(m_subsystemSky.VisibilityRange, 30);
    }
}