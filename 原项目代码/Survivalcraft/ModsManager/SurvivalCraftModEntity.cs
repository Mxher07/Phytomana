using System.Reflection;
using System.Xml.Linq;
using Game.IContentReader;
using StringReader = Game.IContentReader.StringReader;
#if !BROWSER
using Engine;
#endif

namespace Game {
    public class SurvivalCraftModEntity : ModEntity {
        public new const string fName = "SurvivalCraftModEntity";

        public SurvivalCraftModEntity() {
            List<IContentReader.IContentReader> readers = new();
            readers.AddRange(
                [
                    new AssemblyReader(),
                    new BitmapFontReader(),
                    new DaeModelReader(),
                    new ImageReader(),
                    new JsonArrayReader(),
                    new JsonObjectReader(),
                    new JsonDocumentReader(),
                    new IContentReader.JsonModelReader(),
                    new MtllibStructReader(),
                    new IContentReader.ObjModelReader(),
                    new ShaderReader(),
                    new SoundBufferReader(),
                    new StreamingSourceReader(),
                    new StringReader(),
                    new SubtextureReader(),
                    new Texture2DReader(),
                    new XmlReader(),
                    new ContentStreamReader()
                ]
            );
            for (int i = 0; i < readers.Count; i++) {
                ContentManager.ReaderList.Add(readers[i].Type, readers[i]);
            }
#if BROWSER
            Engine.Browser.BrowserInterop.SetContentPtr(ContentFileBridge.Initialize());
            while (!ContentFileBridge.GetIsDownloaded()) {
                Thread.Sleep(100);
            }
            UnmanagedMemoryStream memoryStream = ContentFileBridge.GetStream();
            Size = memoryStream.Length;
            ModArchive = ZipArchive.Open(memoryStream);
#else
            MemoryStream memoryStream = new();
            const string ContentPath = "app:/Content.zip";
            if (Storage.FileExists(ContentPath)) //检测外置资源是否存在，如果不存在就使用内置资源
            {
                Storage.OpenFile(ContentPath, OpenFileMode.Read).CopyTo(memoryStream);
            }
            else {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("Game.Content.zip")?.CopyTo(memoryStream);
            }
            if (memoryStream == null) {
                throw new Exception("Unable to load Content.zip file.");
            }
            Size = memoryStream.Length;
            memoryStream.Position = 0L;
            ModArchive = ZipArchive.Open(memoryStream);
#endif
            InitResources();
            if (modInfo != null) {
                modInfo.LoadOrder = (int)LoadOrder.Survivalcraft;
            }
        }

        public override void LoadBlocksData() {
            LoadingScreen.Info($"[{modInfo?.Name}] {LanguageControl.Get(fName, "1")}");
            BlocksManager.LoadBlocksData(ContentManager.Get<string>("BlocksData"));
            ContentManager.Dispose("BlocksData");
        }

        public override Assembly[] GetAssemblies() => [typeof(BlocksManager).Assembly];

        public override void HandleAssembly(Assembly assembly) {
#pragma warning disable IL2026
            Type[] types = assembly.GetTypes();
#pragma warning restore IL2026
            foreach (Type type in types) {
                if (type.IsSubclassOf(typeof(ModLoader))
                    && !type.IsAbstract) {
#pragma warning disable IL2072
                    if (Activator.CreateInstance(type) is not ModLoader modLoader) {
#pragma warning restore IL2072
                        continue;
                    }
                    modLoader.Entity = this;
                    modLoader.__ModInitialize();
                    Loaders.Add(modLoader);
                    if (modLoader is SurvivalCraftModLoader) {
                        Loader = modLoader;
                    }
                    ModsManager.ModLoaders.Add(modLoader);
                }
                else if (type.IsSubclassOf(typeof(Block))
                    && !type.IsAbstract) {
#pragma warning disable IL2072
                    FieldInfo fieldInfo = type.GetRuntimeFields().FirstOrDefault(p => p.Name == "Index" && p.IsPublic && p.IsStatic);
#pragma warning restore IL2072
                    if (fieldInfo == null
                        || fieldInfo.FieldType != typeof(int)) {
                        ModsManager.AddException(
                            new InvalidOperationException($"Block type \"{type.FullName}\" does not have static field Index of type int.")
                        );
                    }
                    else {
                        BlockTypes.Add(type);
                    }
                }
                else if (type.IsSubclassOf(typeof(SubsystemCreatureSpawn.CreatureType))
                    && !type.IsAbstract) {
                    SubsystemCreatureSpawn.m_creatureSpawnRules.TryAdd(type.FullName, type);
                }
            }
        }

        public override void LoadXdb(ref XElement xElement) {
            LoadingScreen.Info($"[{modInfo?.Name}] {LanguageControl.Get(fName, "2")}");
            xElement = ContentManager.Get<XElement>("Database");
            ContentManager.Dispose("Database");
        }

        public override void LoadCr(ref XElement xElement) {
            LoadingScreen.Info($"[{modInfo?.Name}] {LanguageControl.Get(fName, "3")}");
            xElement = ContentManager.Get<XElement>("CraftingRecipes");
            ContentManager.Dispose("CraftingRecipes");
        }

        public override void LoadClo(ClothingBlock block, ref XElement xElement) {
            LoadingScreen.Info($"[{modInfo?.Name}] {LanguageControl.Get(fName, "4")}");
            xElement = ContentManager.Get<XElement>("Clothes");
            ContentManager.Dispose("Clothes");
        }

        public override void SaveSettings(XElement xElement) { }
        public override void LoadSettings(XElement xElement) { }

        public override void OnBlocksInitalized() {
            BlocksManager.AddCategory("Terrain");
            BlocksManager.AddCategory("Minerals");
            BlocksManager.AddCategory("Plants");
            BlocksManager.AddCategory("Construction");
            BlocksManager.AddCategory("Items");
            BlocksManager.AddCategory("Tools");
            BlocksManager.AddCategory("Weapons");
            BlocksManager.AddCategory("Clothes");
            BlocksManager.AddCategory("Electrics");
            BlocksManager.AddCategory("Food");
            BlocksManager.AddCategory("Spawner Eggs");
            BlocksManager.AddCategory("Painted");
            BlocksManager.AddCategory("Dyed");
            BlocksManager.AddCategory("Fireworks");
        }
    }
}