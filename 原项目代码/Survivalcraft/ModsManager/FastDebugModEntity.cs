using System.Reflection;
using System.Xml.Linq;
using Engine;
using NuGet.Versioning;

namespace Game {
    public class FastDebugModEntity : ModEntity {
        public Dictionary<string, FileInfo> FModFiles = [];

        public FastDebugModEntity() {
            modInfo = new ModInfo { Name = "[Debug]", PackageName = "debug" };
            InitResources();
            modInfo.LoadOrder = (int)LoadOrder.Survivalcraft + 1;
        }

        public override void InitResources() {
            if (SettingsManager.SafeMode) {
                modInfo = GenerateDefaultModInfo();
                return;
            }
            ReadDirResources(ModsManager.ModsPath, "");
            if (!GetFile(
                    "modinfo.json",
                    stream => {
                        modInfo = ModsManager.DeserializeJson(ModsManager.StreamToString(stream));
                        modInfo.Name = $"[FastDebug]{modInfo.Name}";
                    }
                )) {
                modInfo = GenerateDefaultModInfo();
            }
            if(!GetFile("icon.webp", LoadIcon)) {
                GetFile("icon.png", LoadIcon);
            }
        }

        public void ReadDirResources(string basepath, string path) {
            if (string.IsNullOrEmpty(path)) {
                path = basepath;
            }
            foreach (string d in Storage.ListDirectoryNames(path)) {
                ReadDirResources(basepath, $"{path}/{d}");
            }
            foreach (string f in Storage.ListFileNames(path)) {
                if (f.EndsWith(".scmod")) {
                    continue;
                }
                string abpath = $"{path}/{f}";
                string FilenameInZip = abpath.Substring(basepath.Length + 1);
                FModFiles.Add(FilenameInZip, new FileInfo(Storage.GetSystemPath(abpath)));
            }
        }

        public override void CombineContent() {
            foreach ((string path, FileInfo fileInfo) in FModFiles) {
                if (path.StartsWith("Assets/")) {
                    string name = path.Substring(7);
                    ContentInfo contentInfo = new(name);
                    MemoryStream memoryStream = new();
                    using (Stream stream = fileInfo.Open(FileMode.Open)) {
                        stream.CopyTo(memoryStream);
                        contentInfo.SetContentStream(memoryStream);
                        ContentManager.Add(contentInfo);
                    }
                }
            }
        }

        /// <summary>
        ///     获取指定后缀文件列表，带.
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public override void GetFiles(string extension, Action<string, Stream> action) {
            bool skip = false;
            Loader?.GetModFiles(extension, action, out skip);
            if (skip) {
                return;
            }
            foreach (KeyValuePair<string, FileInfo> item in FModFiles) {
                if (item.Key.EndsWith(extension)) {
                    using (Stream fs = item.Value.OpenRead()) {
                        try {
                            action?.Invoke(item.Key, fs);
                        }
                        catch (Exception e) {
                            Log.Error($"GetFile {item.Key} Error:{e}");
                        }
                    }
                }
            }
        }

        public override bool GetFile(string filename, Action<Stream> action) {
            bool skip = false;
            bool loaderReturns = false;
            Loader?.GetModFile(filename, action, out skip, out loaderReturns);
            if (skip) {
                return loaderReturns;
            }
            if (FModFiles.TryGetValue(filename, out FileInfo fileInfo)) {
                using (Stream fs = fileInfo.OpenRead()) {
                    try {
                        action?.Invoke(fs);
                    }
                    catch (Exception e) {
                        Log.Error($"GetFile {filename} Error:{e}");
                    }
                }
                return true;
            }
            return false;
        }

        public override bool GetAssetsFile(string filename, Action<Stream> action) => GetFile($"Assets/{filename}", action);

        public static ModInfo GenerateDefaultModInfo() => new() {
            Name = "FastDebug",
            Version = "1.0.0",
            NuGetVersion = new NuGetVersion(1, 0, 0),
            ApiVersion = ModsManager.APIVersionString,
            ApiVersionRange = new VersionRange(ModsManager.APINuGetVersion),
            Link = "https://gitee.com/SC-SPM/SurvivalcraftApi",
            Author = "SC-SPM",
            Description = "For mod developers to debug uncompressed mod. 用于模组开发者调试未压缩的模组",
            ScVersion = "2.4.0.0",
            PackageName = "fastdebug"
        };
    }
}