using System.Collections.Concurrent;
using Engine;
using Engine.Graphics;
using Engine.Media;
// ReSharper disable MethodOverloadWithOptionalParameter

namespace Game {
    public class ContentInfo {
        public MemoryStream ContentStream;
        public string AbsolutePath;
        public string ContentPath;
        public string ContentSuffix;
        public string Filename;
        public Lock InUse = new();

        public ContentInfo(string AbsolutePath_) {
            AbsolutePath = AbsolutePath_;
            int pos = AbsolutePath_.LastIndexOf('.');
            ContentPath = pos > -1 ? AbsolutePath_.Substring(0, pos) : AbsolutePath_;
            ContentSuffix = pos > -1 ? AbsolutePath.Substring(pos) : null;
            Filename = Path.GetFileName(AbsolutePath);
        }

        public void SetContentStream(Stream stream) {
            if (stream is MemoryStream memoryStream) {
                ContentStream = memoryStream;
                ContentStream.Position = 0L;
            }
            else {
                throw new Exception($"Can't set ContentStream width type {stream.GetType().Name}");
            }
        }

        // 确认不需要此步也能正常运行，现仅作检测和设置Position为0用
        public Stream Duplicate() {
            if (ContentStream == null
                || !ContentStream.CanRead
                || !ContentStream.CanWrite) {
                throw new Exception("ContentStream has been disposed");
            }
            ContentStream.Position = 0L;
            return ContentStream;
        }

        public void Dispose() {
            ContentStream?.Dispose();
        }
    }

    public static class ContentManager {
        internal static ConcurrentDictionary<string, ContentInfo> Resources = [];
        internal static Dictionary<string, IContentReader.IContentReader> ReaderList = [];
        internal static ConcurrentDictionary<string, List<object>> Caches = [];
        internal static object syncObj = new();

        public static void Initialize() {
            ReaderList.Clear();
            Resources.Clear();
            Caches.Clear();
            Display.DeviceReset += Display_DeviceReset;
            Storage.LoadContentStreamCallback = GetStream;
            GltfLoader.LoadExternalStreamCallback = GetStream;
        }

        public static T Get<T>(string name) where T : class => Get(typeof(T), name, null, true) as T;

        public static T Get<T>(string name, string suffix = null) where T : class => Get(typeof(T), name, suffix, true) as T;

        public static T Get<T>(string name, string suffix = null, bool throwOnNotFound = true) where T : class =>
            Get(typeof(T), name, suffix, throwOnNotFound) as T;

        public static object Get(Type type, string name) => Get(type, name, null, true);

        public static object Get(Type type, string name, string suffix = null) => Get(type, name, suffix, true);

        public static object Get(Type type, string name, string suffix = null, bool throwOnNotFound = true) {
            ArgumentNullException.ThrowIfNull(type);
            object obj = null;
            string key = suffix == null ? name : name + (suffix.StartsWith('.') ? suffix : $".{suffix}");
            if (type == typeof(Subtexture)) {
                return TextureAtlasManager.GetSubtexture(name, throwOnNotFound);
            }
            if (Caches.TryGetValue(key, out List<object> cacheList)) {
                obj = cacheList.Find(f => f.GetType() == type);
            }
            if (obj != null) {
                return obj;
            }
            if (ReaderList.TryGetValue(type.FullName ?? type.Name, out IContentReader.IContentReader reader)) {
                List<ContentInfo> contents = [];
                string p;
                if (suffix == null) {
                    foreach (string suffix1 in reader.DefaultSuffix) {
                        p = $"{name}.{suffix1}";
                        if (Caches.TryGetValue(p, out List<object> cacheList2)) {
                            obj = cacheList2.Find(f => f.GetType() == type);
                        }
                        if (obj != null) {
                            if (cacheList == null) {
                                cacheList = [];
                                Caches.AddOrUpdate(key, cacheList, (_, _) => cacheList);
                            }
                            cacheList.Add(obj);
                            return obj;
                        }
                        if (Resources.TryGetValue(p, out ContentInfo contentInfo)) {
                            contents.Add(contentInfo);
                        }
                    }
                }
                else {
                    if (Resources.TryGetValue(key, out ContentInfo contentInfo)) {
                        contents.Add(contentInfo);
                    }
                }
                if (contents.Count == 0) {
                    //没有找到对应资源
                    return throwOnNotFound ? throw new Exception($"Not Found Res [{key}][{type.FullName}]") : null;
                }
                obj = reader.Get(contents.ToArray());
            }
            if (cacheList == null
                && !Caches.TryGetValue(key, out cacheList)) {
                cacheList = [];
                Caches.AddOrUpdate(key, cacheList, (_, _) => cacheList);
            }
            cacheList.Add(obj);
            return obj;
        }

        public static Stream GetStream(string path) => Resources.TryGetValue(path, out ContentInfo contentInfo) ? contentInfo.Duplicate() : null;

        public static void Add(ContentInfo contentInfo) {
            Resources.AddOrUpdate(contentInfo.AbsolutePath, contentInfo, (_, _) => contentInfo);
        }

        /// <summary>
        ///     可能需要带上文件后缀，即获取名字+获取的后缀
        /// </summary>
        /// <param name="name"></param>
        public static void Dispose(string name) {
            lock (syncObj) {
                if (Caches.TryGetValue(name, out List<object> list)) {
                    List<object> toRemove = new();
                    foreach (object t in list) {
                        if (t is IDisposable d) {
                            d.Dispose();
                        }
                        toRemove.Add(t);
                    }
                    foreach (object t in toRemove) {
                        list.Remove(t);
                    }
                }
            }
        }

        /// <param name="key">全路径，需要带后缀</param>
        public static bool ContainsKey(string key) => Resources.ContainsKey(key);

        public static bool IsContent(object content) {
            foreach (List<object> l in Caches.Values) {
                foreach (object d in l) {
                    if (d == content) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void Display_DeviceReset() {
            foreach (KeyValuePair<string, List<object>> i in Caches) {
                string k = i.Key;
                for (int j = 0; j < i.Value.Count; j++) {
                    object t = i.Value[j];
                    if (t is Texture2D
                        || t is Model
                        || t is BitmapFont) {
                        i.Value[j] = Get(t.GetType(), k);
                    }
                }
            }
        }

        public static ReadOnlyList<ContentInfo> List() => new(Resources.Values.ToDynamicArray());

        public static ReadOnlyList<ContentInfo> List(string directory) {
            List<ContentInfo> contents = [];
            if (!directory.EndsWith('/')) {
                directory += "/";
            }
            foreach (ContentInfo content in Resources.Values) {
                if (content.ContentPath.StartsWith(directory)) {
                    contents.Add(content);
                }
            }
            return new ReadOnlyList<ContentInfo>(contents);
        }

        public static void AddContentReader(IContentReader.IContentReader reader) {
            ReaderList.TryAdd(reader.Type, reader);
        }
    }
}