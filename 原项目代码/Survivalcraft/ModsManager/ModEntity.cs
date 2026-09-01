using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using NuGet.Versioning;

namespace Game {
    public class ModEntity {
        public ModInfo modInfo;
        public Texture2D Icon;
        public ZipArchive ModArchive;
        public Dictionary<string, ZipArchiveEntry> ModFiles = [];
        public List<Type> BlockTypes = [];
        public string ModFilePath;
        public bool IsDisabled;
        public ModDisableReason DisableReason = ModDisableReason.Unknown;
        public long Size;
        public bool IsDependencyChecked;
        public string LoadAfter;
        public static HashSet<string> InvalidDllNames = ["Survivalcraft.dll", "Engine.dll", "EntitySystem.dll"];
        public const string fName = "ModEntity";

        /// <summary>
        /// 获取和设置模组的主要 ModLoader
        /// </summary>
        public ModLoader Loader {
            get => ModLoader_;
            set => ModLoader_ = value;
        }
        public List<ModLoader> Loaders {
            get; set;
        } = [];

        ModLoader ModLoader_;

        public ModEntity() { }

        public ModEntity(ZipArchive zipArchive) {
            ModFilePath = ModsManager.ModsPath;
            ModArchive = zipArchive;
            InitResources();
        }

        public ModEntity(string FileName, ZipArchive zipArchive) {
            ModFilePath = FileName;
            Size = Storage.GetFileSize(FileName);
            ModArchive = zipArchive;
            InitResources();
        }

        public virtual void LoadIcon(Stream stream) {
            Icon = Texture2D.Load(stream);
            stream.Close();
        }

        /// <summary>
        ///     获取模组的文件时调用。
        /// </summary>
        /// <param name="extension">文件扩展名</param>
        /// <param name="action">参数1文件名参数，2打开的文件流</param>
        public virtual void GetFiles(string extension, Action<string, Stream> action) {
            //将每个zip里面的文件读进内存中
            bool skip = false;
            Loader?.GetModFiles(extension, action, out skip);
            if (skip || ModArchive == null) {
                return;
            }
            foreach ((string filename, ZipArchiveEntry zipArchiveEntry) in ModFiles) {
                if (Storage.GetExtension(filename) == extension) {
                    MemoryStream stream = new();
                    ModArchive.ExtractFile(zipArchiveEntry, stream);
                    stream.Position = 0L;
                    try {
                        action.Invoke(zipArchiveEntry.FilenameInZip, stream);
                    }
                    catch (Exception e) {
                        Log.Error($"Get file [{zipArchiveEntry.FilenameInZip}] failed: {e}");
                    }
                    finally {
                        stream.Dispose();
                    }
                }
            }
        }

        /// <param name="extension">文件扩展名</param>
        /// <param name="action">参数1文件名参数，2打开的文件流</param>
        /// <return>列表是否为空</return>
        public virtual bool GetFilesAndExist(string extension, Action<string, Stream> action) {
            if (ModArchive?.ReadCentralDir().Count != 0) {
                GetFiles(extension, action);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     获取指定文件
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="action">参数1打开的文件流</param>
        /// <returns></returns>
        public virtual bool GetFile(string filename, Action<Stream> action) {
            bool skip = false;
            bool loaderReturns = false;
            Loader?.GetModFile(filename, action, out skip, out loaderReturns);
            if (skip || ModArchive == null) {
                return loaderReturns;
            }
            if (ModFiles.TryGetValue(filename, out ZipArchiveEntry entry)) {
                using MemoryStream ms = new();
                ModArchive.ExtractFile(entry, ms);
                ms.Position = 0L;
                try {
                    action?.Invoke(ms);
                }
                catch (Exception e) {
                    LoadingScreen.Error($"[{modInfo.Name}] Get file [{filename}] failed: {e}");
                }
                return false;
            }
            return true;
        }

        public virtual bool GetAssetsFile(string filename, Action<Stream> action) => GetFile($"Assets/{filename}", action);

        /// <summary>
        ///     初始化语言包
        /// </summary>
        public virtual void LoadLauguage() {
            GetAssetsFile(
                "Lang/en-US.json",
                stream => {
                    LoadingScreen.Info($"[{modInfo.Name}] Loading English Language file");
                    LanguageControl.LoadEnglishJson(stream);
                }
            );
            string language = ModsManager.Configs["Language"];
            if (language == "en-US") {
                return;
            }
            GetAssetsFile(
                $"Lang/{language}.json",
                stream => {
                    LoadingScreen.Info($"[{modInfo.Name}] Loading Current Language file");
                    LanguageControl.loadJson(stream);
                }
            );
        }

        /// <summary>
        ///     初始化Content资源
        /// </summary>
        public virtual void InitResources() {
            ModFiles.Clear();
            if (ModArchive == null) {
                return;
            }
            List<ZipArchiveEntry> entries = ModArchive.ReadCentralDir();
            foreach (ZipArchiveEntry zipArchiveEntry in entries) {
                if (zipArchiveEntry.FileSize > 0) {
                    ModFiles.Add(zipArchiveEntry.FilenameInZip, zipArchiveEntry);
                }
            }
            if (GetFile("icon.webp", LoadIcon)) {
                GetFile("icon.png", LoadIcon);
            }
            GetFile("modinfo.json",
                stream => {
                    try {
                        modInfo = ModsManager.DeserializeJson(ModsManager.StreamToString(stream));
                    }
                    catch (Exception e) {
                        Log.Error($"Deserialize modinfo.json from [{Storage.GetFileName(ModFilePath)}] failed: {e}");
                    }
                });
            // modsettings.json 优先级高于 modinfo.json 的 Settings 字段；不存在或不合法时沿用 modinfo.json 的 RawSettings。
            // 必须在 SafeMode 释放 ModArchive 之前读取（ParseAllModSettings 阶段归档可能已释放）。
            GetFile("modsettings.json",
                stream => {
                    try {
                        JsonElement settings = ModsManager.TryDeserializeSettingsArray(ModsManager.StreamToString(stream));
                        if (settings.ValueKind == JsonValueKind.Array) {
                            modInfo.RawSettings = settings;
                        }
                    }
                    catch (Exception e) {
                        Log.Error($"Deserialize modsettings.json from [{Storage.GetFileName(ModFilePath)}] failed: {e}");
                    }
                });
            if (SettingsManager.SafeMode && this is not SurvivalCraftModEntity) {
                ModFiles.Clear();
                ModArchive?.ZipFileStream.Dispose();
                ModArchive = null;
            }
            if (modInfo == null) {
                IsDisabled = true;
                DisableReason = ModDisableReason.NoModInfo;
                return;
            }
            if (string.IsNullOrEmpty(modInfo.PackageName)
                || (ModsManager.ModListAll.Count >= 2 && (modInfo.PackageName == "survivalcraft" || modInfo.PackageName == "fastdebug"))
                || modInfo.PackageName.Contains(';')
                || modInfo.PackageName.Contains('\n')) {
                IsDisabled = true;
                DisableReason = ModDisableReason.InvalidPackageName;
                return;
            }
            ModEntity temp = ModsManager.ModListAll.FirstOrDefault(x => x.modInfo?.PackageName == modInfo.PackageName);
            if (temp != null) {
                IsDisabled = true;
                DisableReason = ModDisableReason.Duplicated;
                temp.IsDisabled = true;
                temp.DisableReason = ModDisableReason.Duplicated;
                return;
            }
            if (ModsManager.DisabledMods.TryGetValue(modInfo.PackageName, out HashSet<string> disabledVersions)
                && disabledVersions.Contains(modInfo.Version)) {
                IsDisabled = true;
                DisableReason = ModDisableReason.Manually;
                return;
            }
            LoadingScreen.Info($"[{modInfo.Name}](Version: {modInfo.Version}) Loaded {ModFiles.Count} resource files.");
        }

        public virtual void CombineContent() {
            foreach (KeyValuePair<string, ZipArchiveEntry> c in ModFiles) {
                ZipArchiveEntry zipArchiveEntry = c.Value;
                string filename = zipArchiveEntry.FilenameInZip;
                if (!zipArchiveEntry.IsFilenameUtf8) {
                    ModsManager.AddException(
                        new Exception(
                            $"[{modInfo.Name}] The file name [{zipArchiveEntry.FilenameInZip}] is not encoded in UTF-8, need to be corrected."
                        )
                    );
                }
                if (filename.StartsWith("Assets/")) {
                    MemoryStream memoryStream = new();
                    ContentInfo contentInfo = new(filename.Substring(7));
                    ModArchive.ExtractFile(zipArchiveEntry, memoryStream);
                    contentInfo.SetContentStream(memoryStream);
                    ContentManager.Add(contentInfo);
                }
            }
        }

        /// <summary>
        ///     初始化BlocksData资源
        /// </summary>
        public virtual void LoadBlocksData() {
            bool flag = true;
            GetFiles(
                ".csv",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "1")}");
                        flag = false;
                    }
                    BlocksManager.LoadBlocksData(ModsManager.StreamToString(stream));
                }
            );
        }

        /// <summary>
        ///     初始化Database数据
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void LoadXdb(ref XElement xElement) {
            bool flag = true;
            XElement element = xElement;
            GetFiles(
                ".xdb",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "2")}");
                        flag = false;
                    }
                    ModsManager.CombineDataBase(element, stream, modInfo.PackageName);
                }
            );
            foreach(ModLoader item in Loaders)
                item.OnXdbLoad(xElement);
        }

        /// <summary>
        ///     初始化Clothing数据
        /// </summary>
        /// <param name="block"></param>
        /// <param name="xElement"></param>
        // ReSharper disable UnusedParameter.Global
        public virtual void LoadClo(ClothingBlock block, ref XElement xElement)
            // ReSharper restore UnusedParameter.Global
        {
            bool flag = true;
            XElement element = xElement;
            GetFiles(
                ".clo",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "3")}");
                        flag = false;
                    }
                    ModsManager.CombineClo(element, stream);
                }
            );
        }

        /// <summary>
        ///     初始化CraftingRecipe
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void LoadCr(ref XElement xElement) {
            bool flag = true;
            XElement element = xElement;
            GetFiles(
                ".cr",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "4")}");
                        flag = false;
                    }
                    ModsManager.CombineCr(element, stream);
                }
            );
        }

        /// <summary>
        ///     加载mod程序集
        /// </summary>
        public virtual Assembly[] GetAssemblies() {
            bool flag = true;
            List<Assembly> assemblies = new();
            GetFiles(
                ".dll",
                (filename, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] Loading .dll assembly files.");
                        flag = false;
                    }
                    if (!filename.StartsWith("Assets/")) {
                        string fileNameWithoutDirectory = Storage.GetFileName(filename);
                        if (!InvalidDllNames.Contains(fileNameWithoutDirectory)) {
#pragma warning disable IL2026
                            assemblies.Add(Assembly.Load(ModsManager.StreamToBytes(stream)));
#pragma warning restore IL2026
                        }
                    }
                }
            ); //获取mod文件内的dll文件（不包括Assets目录内的dll）
            return assemblies.ToArray();
        }

        public virtual void HandleAssembly(Assembly assembly) {
            List<Type> blockTypes = [];
#pragma warning disable IL2026
            Type[] types = assembly.GetTypes();
#pragma warning restore IL2026
            foreach (Type type in types) {
                if (type.IsSubclassOf(typeof(ModLoader))
                    && !type.IsAbstract) {
#pragma warning disable IL2062
                    if (Activator.CreateInstance(type) is ModLoader modLoader) {
#pragma warning disable IL2062
                        modLoader.Entity = this;
                        modLoader.__ModInitialize();
                        ModsManager.ModLoaders.Add(modLoader);
                        Loaders.Add(modLoader);
                    }
                }
                else if (type.IsSubclassOf(typeof(IContentReader.IContentReader))
                    && !type.IsAbstract
#pragma warning disable IL2062
                    && Activator.CreateInstance(type) is IContentReader.IContentReader reader) {
#pragma warning restore IL2062
                    ContentManager.ReaderList.TryAdd(reader.Type, reader);
                }
                else if (type.IsSubclassOf(typeof(Block))
                    && !type.IsAbstract) {
                    blockTypes.Add(type);
                }
                else if (type.IsSubclassOf(typeof(SubsystemCreatureSpawn.CreatureType))
                    && !type.IsAbstract) {
                    SubsystemCreatureSpawn.m_creatureSpawnRules.TryAdd(type.FullName, type);
                }
            }
            BlockTypes.AddRange(blockTypes);
            Loaders.Sort((x, y) => x.Priority.CompareTo(y.Priority));
            Loader = Loaders.FirstOrDefault();
        }

        public virtual void LoadJs() {
#if !IOS && !BROWSER
            bool flag = true;
            GetFiles(
                ".js",
                (_, stream) => {
                    if (flag) {
                        LoadingScreen.Info($"[{modInfo.Name}] {LanguageControl.Get(fName, "5")}");
                        flag = false;
                    }
                    JsInterface.Execute(new StreamReader(stream).ReadToEnd());
                }
            );
#endif
        }

        /// <summary>
        ///     检查依赖项
        /// </summary>
        public virtual void CheckDependencies(List<ModEntity> modEntities = null) {
            if (IsDisabled || modInfo == null) {
                return;
            }
            modEntities ??= ModsManager.ModList;
            if (modInfo.DependencyRanges.Count == 0) {
                IsDependencyChecked = true;
                modEntities.Add(this);
                ModsManager.PackageNameToModEntity.TryAdd(modInfo.PackageName, this);
                return;
            }
            LoadingScreen.Info($"[{modInfo.Name}] Checking dependencies.");
            foreach ((string name, VersionRange range) in modInfo.DependencyRanges) {
                ModEntity entity = ModsManager.ModListAll.Find(px => !px.IsDisabled
                    && px.modInfo != null
                    && px.modInfo.PackageName == name
                    && ((px.modInfo.NuGetVersion != null && range.Satisfies(px.modInfo.NuGetVersion)) || range.Equals(VersionRange.All) || px.modInfo.Version == range.OriginalString)
                );
                if (entity != null) {
                    if (!entity.IsDependencyChecked) {
                        if (entity.modInfo.DependencyRanges.ContainsKey(modInfo.PackageName)) {
                            IsDisabled = true;
                            DisableReason = ModDisableReason.DependencyError;
                            entity.IsDisabled = true;
                            entity.DisableReason = ModDisableReason.DependencyError;
                            Log.Error($"[{modInfo.Name}] Dependency {name} is dependent on {modInfo.Name}, which is not allowed.");
                            return;
                        }
                        entity.CheckDependencies(modEntities);
                    }
                }
                else {
                    IsDisabled = true;
                    DisableReason = ModDisableReason.DependencyError;
                    string versionStr = string.IsNullOrEmpty(range.OriginalString) ? string.Empty : $"Required version: {range}";
                    Log.Error($"[{modInfo.Name}] Failed to find dependency: Package Name \"{name}\", {versionStr}");
                    return;
                }
            }
            IsDependencyChecked = true;
            modEntities.Add(this);
            ModsManager.PackageNameToModEntity.TryAdd(modInfo.PackageName, this);
        }

        /// <summary>
        ///     保存设置
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void SaveSettings(XElement xElement) {
            Loaders.ForEach(item => item.SaveSettings(xElement));
        }

        /// <summary>
        ///     加载设置
        /// </summary>
        /// <param name="xElement"></param>
        public virtual void LoadSettings(XElement xElement) {
            Loaders.ForEach(item => item.LoadSettings(xElement));
        }

        /// <summary>
        ///     BlocksManager初始化完毕
        /// </summary>
        // <param name="categories"></param>
        public virtual void OnBlocksInitalized() {
            Loaders.ForEach(item => item.BlocksInitalized());
        }

        //释放资源
        public virtual void Dispose() {
            try {
                Loaders.ForEach(item => item.ModDispose());
            }
            catch {
                // ignored
            }
            ModArchive?.ZipFileStream.Dispose();
        }

        public override bool Equals(object obj) {
            if (obj is ModEntity px) {
                return px.modInfo.PackageName == modInfo.PackageName
                    && (px.modInfo.NuGetVersion == null
                        ? px.modInfo.Version == modInfo.Version
                        : px.modInfo.NuGetVersion.Equals(modInfo.NuGetVersion));
            }
            return false;
        }

        public override int GetHashCode() =>
            // ReSharper disable NonReadonlyMemberInGetHashCode
            modInfo.GetHashCode();
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}