using Engine.Graphics;

namespace Engine.Animation {
    /// <summary>
    /// 动画来源对象池 - 复用 IAnimationSource 实例避免 GC 压力
    /// </summary>
    public static class AnimationSourcePool {
        public static readonly Stack<ClipAnimationSource> m_clipPool = new();
        public static readonly object m_lock = new();
        public static int m_maxPoolSize = 64;

        /// <summary>
        /// 设置池最大大小
        /// </summary>
        public static void SetMaxPoolSize(int size) {
            m_maxPoolSize = Math.Max(0, size);
        }

        /// <summary>
        /// 租用关键帧动画源
        /// </summary>
        public static ClipAnimationSource RentClip(Model model, ModelAnimation animation, AnimationSourceConfig config = null) {
            lock (m_lock) {
                if (m_clipPool.Count > 0) {
                    ClipAnimationSource source = m_clipPool.Pop();
                    // 重新初始化
                    source = new ClipAnimationSource(model, animation, config);
                    return source;
                }
            }
            return new ClipAnimationSource(model, animation, config);
        }

        /// <summary>
        /// 归还动画源到池
        /// </summary>
        public static void Return(IAnimationSource source) {
            if (source == null) {
                return;
            }
            lock (m_lock) {
                if (m_clipPool.Count >= m_maxPoolSize) {
                    return;
                }
                if (source is ClipAnimationSource clipSource) {
                    clipSource.Reset();
                    m_clipPool.Push(clipSource);
                }
                // DriverAnimationSource 通常绑定到层，不需要池化
            }
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public static void Clear() {
            lock (m_lock) {
                m_clipPool.Clear();
            }
        }

        /// <summary>
        /// 获取当前池中对象数量
        /// </summary>
        public static int Count {
            get {
                lock (m_lock) {
                    return m_clipPool.Count;
                }
            }
        }
    }

    /// <summary>
    /// 外部动画缓存 - 共享动画数据避免重复加载
    /// </summary>
    public static class AnimationCache {
        public static readonly Dictionary<string, WeakReference<LoadedAnimationData>> _cache = new();
        public static readonly object _lock = new();

        /// <summary>
        /// 获取或加载动画
        /// </summary>
        public static LoadedAnimationData GetOrLoad(string path, string animationName, Func<string, LoadedAnimationData> loader) {
            string cacheKey = string.IsNullOrEmpty(animationName) ? path : $"{path}#{animationName}";
            lock (_lock) {
                // 检查缓存
                if (_cache.TryGetValue(cacheKey, out WeakReference<LoadedAnimationData> weakRef)
                    && weakRef.TryGetTarget(out LoadedAnimationData cached)) {
                    return cached;
                }

                // 加载并缓存
                LoadedAnimationData loaded = loader(path);
                if (loaded != null) {
                    _cache[cacheKey] = new WeakReference<LoadedAnimationData>(loaded);
                }
                return loaded;
            }
        }

        /// <summary>
        /// 尝试从缓存获取
        /// </summary>
        public static bool TryGet(string path, string animationName, out LoadedAnimationData data) {
            string cacheKey = string.IsNullOrEmpty(animationName) ? path : $"{path}#{animationName}";
            data = null;
            lock (_lock) {
                if (_cache.TryGetValue(cacheKey, out WeakReference<LoadedAnimationData> weakRef)) {
                    return weakRef.TryGetTarget(out data);
                }
            }
            return false;
        }

        /// <summary>
        /// 添加到缓存
        /// </summary>
        public static void Add(string path, string animationName, LoadedAnimationData data) {
            if (data == null) {
                return;
            }
            string cacheKey = string.IsNullOrEmpty(animationName) ? path : $"{path}#{animationName}";
            lock (_lock) {
                _cache[cacheKey] = new WeakReference<LoadedAnimationData>(data);
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public static void Clear() {
            lock (_lock) {
                _cache.Clear();
            }
        }

        /// <summary>
        /// 清理已被 GC 回收的缓存项
        /// </summary>
        public static void Compact() {
            lock (_lock) {
                List<string> deadKeys = _cache.Where(kvp => !kvp.Value.TryGetTarget(out _)).Select(kvp => kvp.Key).ToList();
                foreach (string key in deadKeys) {
                    _cache.Remove(key);
                }
            }
        }

        /// <summary>
        /// 获取缓存项数量
        /// </summary>
        public static int Count {
            get {
                lock (_lock) {
                    return _cache.Count;
                }
            }
        }
    }

    /// <summary>
    /// 已加载的动画数据
    /// </summary>
    public class LoadedAnimationData {
        public string Path { get; }
        public object Data { get; }
        public List<ModelAnimation> Animations { get; }

        public LoadedAnimationData(string path, object data, List<ModelAnimation> animations = null) {
            Path = path;
            Data = data;
            Animations = animations ?? new List<ModelAnimation>();
        }

        /// <summary>
        /// 获取指定名称的动画
        /// </summary>
        public ModelAnimation GetAnimation(string name) {
            return Animations?.FirstOrDefault(a => a.Name == name);
        }
    }
}