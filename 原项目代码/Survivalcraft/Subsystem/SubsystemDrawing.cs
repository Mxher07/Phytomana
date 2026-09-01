using System.Diagnostics;
using System.Text;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class SubsystemDrawing : Subsystem {
        public Dictionary<IDrawable, bool> m_drawables = [];

        public SortedMultiCollection<int, IDrawable> m_sortedDrawables = [];

        public Dictionary<Type, DebugInfo> m_debugInfos = [];
        public Stopwatch m_debugStopwatch = new();
        public bool UpdateTimeDebug = false;

        public int DrawablesCount {
            get {
                lock (m_drawables) {
                    return m_drawables.Count;
                }
            }
        }

        public void AddDrawable(IDrawable drawable) {
            lock (m_drawables) {
                if (!m_drawables.TryAdd(drawable, true)) {
                    Log.Error($"SubsystemDrawing: Drawable [{drawable.GetType().ToString()}] already added.");
                }
            }
        }

        public void RemoveDrawable(IDrawable drawable) {
            lock (m_drawables) {
                m_drawables.Remove(drawable);
            }
        }

        public virtual void Draw(Camera camera) {
            m_sortedDrawables.Clear();
            lock (m_drawables) {
                foreach (IDrawable key2 in m_drawables.Keys) {
                    int[] drawOrders = key2.DrawOrders;
                    foreach (int key in drawOrders) {
                        m_sortedDrawables.Add(key, key2);
                    }
                }
            }
            if (UpdateTimeDebug) {
                m_debugStopwatch.Start();
            }
            foreach ((int drawOrder, IDrawable sortedDrawable) in m_sortedDrawables) {
                long ticks = UpdateTimeDebug ? m_debugStopwatch.ElapsedTicks : 0;
                try {
                    sortedDrawable.Draw(camera, drawOrder);
                }
                catch (Exception) {
                    // ignored
                }
                finally {
                    if (UpdateTimeDebug) {
                        Type type = sortedDrawable.GetType();
                        long ticksCosted = m_debugStopwatch.ElapsedTicks - ticks;
                        if (!m_debugInfos.TryGetValue(type, out DebugInfo info)) {
                            info = new DebugInfo();
                            m_debugInfos.Add(type, info);
                        }
                        info.Counter++;
                        info.TotalTicksCosted += ticksCosted;
                        if (ticksCosted > info.MaxTicksCosted1) {
                            info.MaxTicksCosted1 = ticksCosted;
                        }
                        else if (ticksCosted > info.MaxTicksCosted2) {
                            info.MaxTicksCosted2 = ticksCosted;
                        }
                    }
                }
            }
            m_debugStopwatch.Reset();
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            foreach (IDrawable item in Project.FindSubsystems<IDrawable>()) {
                AddDrawable(item);
            }
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            if (UpdateTimeDebug) {
                int maxTypeNameLength = 1;
                if (m_debugInfos.Keys.Count > 0) {
                    maxTypeNameLength = m_debugInfos.Keys.Max(type => type.FullName?.Length ?? 0) + 1;
                }
                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine("====== SubsystemDrawing Performance Analyze ======");
                stringBuilder.Append("TypeName".PadRight(maxTypeNameLength));
                stringBuilder.Append("    Counter   TotalTime AverageTime    MaxTime1    MaxTime2");
                foreach ((Type type, DebugInfo info) in m_debugInfos.OrderByDescending(pair => pair.Value.TotalTicksCosted)) {
                    stringBuilder.AppendLine();
                    stringBuilder.Append(type.FullName?.PadRight(maxTypeNameLength));
                    stringBuilder.Append(info.Counter.ToString().PadLeft(11));
                    stringBuilder.Append($"{(float)info.TotalTicksCosted / Stopwatch.Frequency * 1000:F}ms".PadLeft(12));
                    stringBuilder.Append($"{(float)info.TotalTicksCosted / info.Counter / Stopwatch.Frequency * 1000000f:F}μs".PadLeft(12));
                    stringBuilder.Append($"{(float)info.MaxTicksCosted1 / Stopwatch.Frequency * 1000000f:F}μs".PadLeft(12));
                    stringBuilder.Append($"{(float)info.MaxTicksCosted2 / Stopwatch.Frequency * 1000000f:F}μs".PadLeft(12));
                }
                Log.Information(stringBuilder.ToString());
                m_debugInfos.Clear();
            }
        }

        public override void OnEntityAdded(Entity entity) {
            foreach (IDrawable item in entity.FindComponents<IDrawable>()) {
                bool skipVanilla = false;
                ModsManager.HookAction(
                    "OnIDrawableAdded",
                    loader => {
                        loader.OnIDrawableAdded(this, item, skipVanilla, out bool skip);
                        skipVanilla |= skip;
                        return false;
                    }
                );
                if (!skipVanilla) {
                    AddDrawable(item);
                }
            }
        }

        public override void OnEntityRemoved(Entity entity) {
            foreach (IDrawable item in entity.FindComponents<IDrawable>()) {
                RemoveDrawable(item);
            }
        }
    }
}