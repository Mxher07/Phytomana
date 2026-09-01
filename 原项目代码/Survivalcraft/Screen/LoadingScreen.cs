using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Game.Animation;
using NCalc;
#if ANDROID
using Android.App;
#elif WINDOWS
//using System.Windows.Forms;
#endif

namespace Game {
    public class LoadingScreen : Screen {
        public enum LogType {
            Info = 0,
            Warning = 1,
            Error = 2,
            Advice = 3
        }

        class LogItem(LogType type, string log) {
            public LogType LogType = type;
            public string Message = log;
        }

        List<Action> LoadingActoins = [];
        List<Action> ModLoadingActoins = [];
        CanvasWidget Canvas = new() { Size = new Vector2(float.PositiveInfinity) };

        RectangleWidget Background = new() {
            FillColor = SettingsManager.DisplayLog ? Color.Black : Color.White, OutlineThickness = 0f, DepthWriteEnabled = true
        };

        static ListPanelWidget LogList;
        public static bool m_isContentLoaded;
        public const string fName = "LoadingScreen";

        static LoadingScreen() {
            if (SettingsManager.DisplayLog) {
                LogList = new ListPanelWidget { Direction = LayoutDirection.Vertical, PlayClickSound = false };
                LogList.ItemWidgetFactory = obj => {
                    if (obj is LogItem logItem) {
                        CanvasWidget canvasWidget = new() {
                            Size = new Vector2(Display.Viewport.Width, 40), Margin = new Vector2(0, 2), HorizontalAlignment = WidgetAlignment.Near
                        };
                        FontTextWidget fontTextWidget = new() {
                            FontScale = 0.6f,
                            Text = logItem.Message,
                            Color = GetColor(logItem.LogType),
                            VerticalAlignment = WidgetAlignment.Center,
                            HorizontalAlignment = WidgetAlignment.Near
                        };
                        canvasWidget.Children.Add(fontTextWidget);
                        canvasWidget.IsVisible = true;
                        return canvasWidget;
                    }
                    return null;
                };
                LogList.ItemSize = 30;
            }
        }

        public static Color GetColor(LogType type) {
            return type switch {
                LogType.Advice => Color.Cyan,
                LogType.Error => Color.Red,
                LogType.Warning => Color.Yellow,
                _ => Color.White
            };
        }

        public LoadingScreen() {
            if (SettingsManager.DisplayLog) {
                Canvas.AddChildren(Background);
                Canvas.AddChildren(LogList);
                AddChildren(Canvas);
            }
            Info($"Initializing Mods Manager. Api Version: {ModsManager.APIVersionString}");
        }

        public void ContentLoaded() {
            if (SettingsManager.DisplayLog) {
                _ = ContentManager.Get<Image>("Fonts/Pericles", ".webp");
                m_isContentLoaded = true;
                return;
            }
            ClearChildren();
            RectangleWidget rectangle1 = new() {
                FillColor = Color.White,
                OutlineColor = Color.Transparent,
                Size = new Vector2(256f),
                VerticalAlignment = WidgetAlignment.Center,
                HorizontalAlignment = WidgetAlignment.Center
            };
            rectangle1.Subtexture = ContentManager.Get<Subtexture>("Textures/Gui/CandyRufusLogo");
            RectangleWidget rectangle2 = new() {
                FillColor = Color.White,
                OutlineColor = Color.Transparent,
                Size = new Vector2(80f, 50f),
                VerticalAlignment = WidgetAlignment.Far,
                HorizontalAlignment = WidgetAlignment.Far,
                Margin = new Vector2(10f)
            };
            rectangle2.Subtexture = ContentManager.Get<Subtexture>("Textures/Gui/EngineLogo");
            BusyBarWidget busyBar = new() {
                VerticalAlignment = WidgetAlignment.Far, HorizontalAlignment = WidgetAlignment.Center, Margin = new Vector2(0, 40)
            };
            Canvas.AddChildren(Background);
            Canvas.AddChildren(rectangle1);
            Canvas.AddChildren(rectangle2);
            Canvas.AddChildren(busyBar);
            AddChildren(Canvas);
            m_isContentLoaded = true;
            Task.Run(() => { _ = ContentManager.Get<Image>("Fonts/Pericles", ".webp"); });
        }

        //日志已经附带状态，不需要添加状态字符串
        public static void Error(string mesg) {
            Add(LogType.Error, mesg);
        }

        public static void Info(string mesg) {
            Add(LogType.Info, mesg);
        }

        public static void Warning(string mesg) {
            Add(LogType.Warning, mesg);
        }

        public static void Advice(string mesg) {
            Add(LogType.Advice, $"[Advice]{mesg}");
        }

        public static void Add(LogType type, string mesg) {
            Dispatcher.Dispatch(
                delegate {
                    switch (type) {
                        case LogType.Info:
                        case LogType.Advice: Log.Information(mesg); break;
                        case LogType.Error: Log.Error(mesg); break;
                        case LogType.Warning: Log.Warning(mesg); break;
                    }
                    if (SettingsManager.DisplayLog && LogList != null) {
                        LogItem item = new(type, mesg);
                        LogList.AddItem(item);
                        LogList.ScrollToItem(item);
                    }
                }
            );
        }

        void InitActions() {
            bool isLoadSucceed = true;
            Exception exception = null;
            AddLoadAction(
                delegate { //将所有的有效的scmod读取为ModEntity，并自动添加SurvivalCraftModEntity
                    ContentManager.Initialize();
                    ModsManager.Initialize();
                }
            );
            AddLoadAction(
                delegate {
#if ANDROID
                    // On Android VR headsets, force enable VR when available
                    if (VrManager.IsVrAvailable && !SettingsManager.UseVr) {
                        SettingsManager.UseVr = true;
                    }
#endif
                    if (SettingsManager.UseVr) {
                        // StartVr waits for the background init (Windows) to finish
                        // and returns false if VR ended up unavailable. Don't gate on
                        // IsVrAvailable here — that flag is still resolving async at
                        // this early load step and would skip VR auto-start.
                        try {
                            VrManager.StartVr();
                            if (!VrManager.IsVrStarted) {
                                SettingsManager.UseVr = false;
                            }
                        }
                        catch (Exception ex) {
                            SettingsManager.UseVr = false;
                            Log.Error($"VR start error: {ex}");
                        }
                    }
                }
            );
            AddLoadAction(
                delegate { //检查所有Mod依赖项
                    //根据加载顺序排序后的结果
                    ModsManager.ModList.Clear();
                    foreach (ModEntity item in ModsManager.ModListAll) {
                        if (item.IsDependencyChecked || item.IsDisabled) {
                            continue;
                        }
                        item.CheckDependencies(ModsManager.ModList);
                    }
                }
            );
            AddLoadAction(() => {
                foreach (ModEntity modEntity in ModsManager.ModList) {
                    modEntity.CombineContent();
                }
                ModsManager.DisposeNotEnabledModsResources();
            });
            AddLoadAction(ContentLoaded);
            AddLoadAction(() => {
                    Dictionary<string, Assembly[]> assemblies = [];
                    ModsManager.ModListAllDo(modEntity => {
                            bool flag = true;
                            assemblies[modEntity.modInfo.PackageName] = modEntity.GetAssemblies();
                            foreach (Assembly assembly in assemblies[modEntity.modInfo.PackageName]) {
                                if (flag) {
                                    Log.Information($"[{modEntity.modInfo.Name}] Getting assemblies.");
                                    flag = false;
                                }
                                AssemblyName assemblyName = assembly.GetName();
                                string fullName = assemblyName.FullName;
                                if (ModsManager.Dlls.TryGetValue(fullName, out Assembly existingAssembly)) {
                                    if (existingAssembly.GetName().Version < assemblyName.Version) {
                                        ModsManager.Dlls[fullName] = assembly;
                                    }
                                }
                                else {
                                    ModsManager.Dlls.Add(fullName, assembly);
                                }
                            }
                        }
                    );
                    //加载 mod 程序集(.dll)文件
                    //但不进行处理操作(如添加block等)
                    ModsManager.ModHooks.Clear();
                    ModsManager.m_tempModHooks.Clear();
                    ModsManager.ModListAllDo(modEntity => {
                            if (!isLoadSucceed) {
                                return;
                            }
                            foreach (Assembly asm in assemblies[modEntity.modInfo.PackageName]) {
                                Log.Information($"[{modEntity.modInfo.Name}] Handling assembly [{asm.FullName}]");
                                try {
                                    modEntity.HandleAssembly(asm);
                                }
                                catch (Exception e) {
                                    exception = e;
                                    string separator = new('-', 10); //生成10个 '-' 连一起的字符串
                                    Log.Error($"{separator}Handle assembly failed{separator}");
                                    Log.Error(
                                        $"Loaded assembly:\n{string.Join("\n", AppDomain.CurrentDomain.GetAssemblies().Select(x => x.FullName ?? x.GetName().FullName))}"
                                    );
                                    Log.Error(separator);
                                    Log.Error($"Error assembly: {asm.FullName}");
#pragma warning disable IL2026
                                    Log.Error($"Dependencies:\n{string.Join("\n", asm.GetReferencedAssemblies().Select(x => x.FullName))}");
#pragma warning restore IL2026
                                    Log.Error(separator);
                                    Log.Error(e);
                                    isLoadSucceed = false;
                                    break;
                                }
                            }
                        }
                    );
                    if (!isLoadSucceed) {
                        ModsManager.ModList.RemoveAll(entity => entity is not SurvivalCraftModEntity && entity is not FastDebugModEntity);
                        LoadingActoins.RemoveRange(1, LoadingActoins.Count - 1);
                        ModLoadingActoins.Clear();
                        ScreensManager.SwitchScreen(
                            new LoadingFailedScreen(
                                "Loading failed 加载失败",
                                ["Exceptions: 异常信息：", ..exception?.ToString().Split('\n') ?? []],
                                [
                                    $"Check the API version required by mod is equal to the current API version ({ModsManager.APIVersionString}). Check and add missing mods. If not solved, please contact the developer of the mods or API with Game.log in the path below.",
                                    $"检查模组是否缺失，并添加所缺失的模组。查看模组所需插件版版本与当前插件版版本（{ModsManager.APIVersionString}）是否一致。若以上方式都无法解决，请联系模组、插件版开发者，并发送下面路径中的 Game.log",
                                    Storage.GetSystemPath(ModsManager.LogPath),
                                    "And you can enable Safe Mode to stop loading any mod.",
                                    "你还可以启用安全模式，停止加载任何模组。"
                                ]
                            )
                        );
                    }
                    //处理程序集
                }
            );
            AddLoadAction(
                delegate { //初始化所有ModEntity的语言包
                    //>>>初始化语言列表
                    LanguageControl.LanguageTypes.Clear();
                    foreach (ContentInfo contentInfo in ContentManager.List("Lang")) {
                        string fileName = Path.GetFileNameWithoutExtension(contentInfo.Filename);
                        if (string.IsNullOrEmpty(fileName)) {
                            continue;
                        }
                        try {
#if BROWSER
                            LanguageControl.LanguageTypes.Add(fileName);
#else
                            CultureInfo cultureInfo = new(fileName.EndsWith("-old") ? fileName.Substring(0, fileName.Length - 4) : fileName, false);
                            LanguageControl.LanguageTypes.TryAdd(fileName, cultureInfo); //第二个参数应为CultureInfo
#endif
                        }
                        catch (Exception) {
                            // ignore
                        }
                    }
                    //<<<结束
#if BROWSER
                    if (ModsManager.Configs.TryGetValue("Language", out string value)
                        && LanguageControl.LanguageTypes.Contains(value)) {
                        LanguageControl.Initialize(value);
                    }
                    else {
                        string systemLanguage = Program.SystemLanguage;
                        if (string.IsNullOrEmpty(systemLanguage)) {
                            //如果不支持系统语言，英语是最佳选择
                            LanguageControl.Initialize("en-US");
                            Log.Information("Language is not specified, and system language is not detected, en-US is loaded instead.");
                        }
                        else if (LanguageControl.LanguageTypes.Contains(systemLanguage)) {
                            LanguageControl.Initialize(systemLanguage);
                            Log.Information($"Language is not specified, system language ({systemLanguage}) is successfully loaded.");
                        }
                        else {
                            bool languageNotLoaded = true;
                            string[] systemLanguageArray = systemLanguage.Split('-');
                            switch (systemLanguageArray.Length) {
                                case 1: {
                                    foreach (string cultureName in LanguageControl.LanguageTypes) {
                                        string[] cultureNameArray = cultureName.Split('-');
                                        if (systemLanguage == cultureNameArray[0]) {
                                            LanguageControl.Initialize(cultureName);
                                            Log.Information(
                                                $"Language is not specified, a language ({cultureName}) closest to system language ({systemLanguage}) is successfully loaded."
                                            );
                                            languageNotLoaded = false;
                                            break;
                                        }
                                    }
                                    break;
                                }
                                case >= 2:
                                    foreach (string cultureName in LanguageControl.LanguageTypes) {
                                        string[] cultureNameArray = cultureName.Split('-');
                                        if (systemLanguageArray[0] == cultureNameArray[0]) {
                                            LanguageControl.Initialize(cultureName);
                                            Log.Information(
                                                $"Language is not specified, a language ({cultureName}) closest to system language ({systemLanguage}) is successfully loaded."
                                            );
                                            languageNotLoaded = false;
                                            break;
                                        }
                                    }
                                    break;
                            }
                            if (languageNotLoaded) {
                                LanguageControl.Initialize("en-US");
                                Log.Information(
                                    $"Language is not specified, and system language ({systemLanguage}) is not supported yet, en-US is loaded instead."
                                );
                            }
                        }
                    }
#else
                    if (ModsManager.Configs.TryGetValue("Language", out string value)
                        && LanguageControl.LanguageTypes.ContainsKey(value)) {
                        LanguageControl.Initialize(value);
                    }
                    else {
                        string systemLanguage = Program.SystemLanguage;
                        if (string.IsNullOrEmpty(systemLanguage)) {
                            //如果不支持系统语言，英语是最佳选择
                            LanguageControl.Initialize("en-US");
                            Log.Information("Language is not specified, and system language is not detected, en-US is loaded instead.");
                        }
                        else if (LanguageControl.LanguageTypes.ContainsKey(systemLanguage)) {
                            LanguageControl.Initialize(systemLanguage);
                            Log.Information($"Language is not specified, system language ({systemLanguage}) is successfully loaded.");
                        }
                        else {
                            bool languageNotLoaded = true;
                            CultureInfo systemCultureInfoParent = new CultureInfo(systemLanguage).Parent;
                            foreach ((string cultureName, CultureInfo cultureInfo) in LanguageControl.LanguageTypes) {
                                bool similar = false;
                                CultureInfo parentCulture = cultureInfo.Parent;
                                string parentCultureName = cultureInfo.Name;
                                if (parentCultureName == systemLanguage
                                    || parentCultureName == systemCultureInfoParent.Name
                                    || parentCultureName == systemCultureInfoParent.Parent.Name) {
                                    similar = true;
                                }
                                else {
                                    string rootCultureName = parentCulture.Parent.Name;
                                    if (rootCultureName.Length > 0
                                        && (rootCultureName == systemCultureInfoParent.Name
                                            || rootCultureName == systemCultureInfoParent.Parent.Name)) {
                                        similar = true;
                                    }
                                }
                                if (similar) {
                                    LanguageControl.Initialize(cultureName);
                                    Log.Information(
                                        $"Language is not specified, a language ({cultureName}) closest to system language ({systemLanguage}) is successfully loaded."
                                    );
                                    languageNotLoaded = false;
                                }
                            }
                            if (languageNotLoaded) {
                                LanguageControl.Initialize("en-US");
                                Log.Information(
                                    $"Language is not specified, and system language ({systemLanguage}) is not supported yet, en-US is loaded instead."
                                );
                            }
                        }
                    }
#endif
                    ModsManager.ModListAllDo(modEntity => { modEntity.LoadLauguage(); });
                    LanguageControl.SetUsual();
#if !MOBILE
                    string title =
                        $"{(SettingsManager.SafeMode ? $"[{LanguageControl.Get("Usual", "safeMode")}]" : "")}{LanguageControl.Get("Usual", "gameName")} {ModsManager.ShortGameVersion} - {LanguageControl.Get("Usual", "api")} {ModsManager.APIVersionString}";
#if DEBUG
                    title = $"[{LanguageControl.Get("Usual", "debug")}]{title}";
#endif
                    Window.TitlePrefix = title;
#endif
                }
            );
            AddLoadAction(ModsManager.ParseAllModSettings); //语言已加载（日志可国际化）+ 模组 dll 已加载（类型可解析）
#if !IOS && !BROWSER
            AddLoadAction(
                delegate { //读取所有的ModEntity的JavaScript
                    JsInterface.Initiate();
                    ModsManager.ModListAllDo(modEntity => { modEntity.LoadJs(); });
                    JsInterface.RegisterEvent();
                }
            );
#endif
            AddLoadAction(ModsManager.DealWithTempModHooks);
            AddLoadAction(
                delegate {
                    Info(LanguageControl.Get(fName, "1"));
                    List<Action> actions = [];
                    ModsManager.HookAction(
                        "OnLoadingStart",
                        loader => {
                            loader.OnLoadingStart(actions);
                            return false;
                        }
                    );
                    foreach (Action ac in actions) {
                        ModLoadingActoins.Add(ac);
                    }
                }
            );
            AddLoadAction(ClothingSlot.Initialize);
            AddLoadAction(
                delegate { //初始化TextureAtlas
                    Info(LanguageControl.Get(fName, "2"));
                    TextureAtlasManager.Initialize();
                }
            );
            AddLoadAction(
                delegate { //初始化Database
                    try {
                        ModsManager.InitImportantDatabaseClasses();
                        DatabaseManager.Initialize();
                        ModsManager.ModListAllDo(modEntity => { modEntity.LoadXdb(ref DatabaseManager.DatabaseNode); });
                        ModsManager.DealWithClassSubstitutes();
                    }
                    catch (Exception e) {
                        Warning(e.ToString());
                    }
                }
            );
            AddLoadAction(
                delegate {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    try {
                        DatabaseManager.LoadDataBaseFromXml(DatabaseManager.DatabaseNode);
                    }
                    catch (Exception e) {
                        Error(e.ToString());
                    }
                    stopwatch.Stop();
                    Info($"{LanguageControl.Get(fName, "3")}({stopwatch.ElapsedMilliseconds}ms)");
                }
            );
            AddLoadAction(
                delegate { //初始化方块管理器
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    BlocksManager.Initialize();
                    stopwatch.Stop();
                    Info($"{LanguageControl.Get(fName, "4")}({stopwatch.ElapsedMilliseconds}ms)");
                }
            );
            AddLoadAction(CraftingRecipesManager.Initialize); //初始化合成谱
            AddLoadAction(
                delegate {
                    Info(LanguageControl.Get(fName, "7"));
                    BlocksTexturesManager.Initialize();
                    CharacterSkinsManager.Initialize();
                    CommunityContentManager.Initialize();
                    OriginalCommunityContentManager.Initialize();
                    FurniturePacksManager.Initialize();
                    LightingManager.Initialize();
                    MotdManager.Initialize();
                    WorldsManager.Initialize();
                }
            );
            AddLoadAction(
                delegate {
                    Info(LanguageControl.Get(fName, "5"));
                    ModSettingsManager.LoadModSettings();
                }
            );
            InitScreens();
            AddLoadAction(FileAssociationManager.Initialize);
            AddLoadAction(() => {
                    //NCalc Warmup
                    Expression expression = new NCalc.Expression("[B] && [N] * 1.0 > 0.5");
                    _ = expression.GetParameterNames();
                    expression.Parameters.Add("B", true);
                    expression.Parameters.Add("N", 0.5f);
                    object expressionResult = expression.Evaluate();
                    _ = Convert.ToBoolean(expressionResult);
                }
            );
            AddLoadAction(() => {
                    AnimationDriverRegistration.Register();
                    AnimationTemplateRegistration.Register();
                }
            );
            AddLoadAction(
                delegate {
                    ModsManager.ModListAllDo(modEntity => {
                        if (modEntity.Loaders.Count > 0) {//modEntity.Loader不为空表示至少设置过一次，即Loaders长度不为0
                            Info($"[{modEntity.modInfo?.Name}] {LanguageControl.Get(fName, "6")}");
                            modEntity.Loaders.ForEach(item => item.OnLoadingFinished(ModLoadingActoins));
                        }
                    });
                }
            );
            AddLoadAction(KeyCompatibleGroupsManager.Initialize); //初始化按键兼容组
            AddLoadAction(
                delegate {
                    if (Program.StartupParameters.TryGetValue("play", out string worldDirectory)) {
                        ScreensManager.SwitchScreen("Play", worldDirectory);
                    }
                    else {
                        ScreensManager.SwitchScreen("MainMenu");
                    }
                }
            );
        }

        void InitScreens() {
            AddLoadAction(delegate { AddScreen("Nag", new NagScreen()); });
            AddLoadAction(delegate { AddScreen("MainMenu", new MainMenuScreen()); });
            AddLoadAction(delegate { AddScreen("Recipaedia", new RecipaediaScreen()); });
            AddLoadAction(delegate { AddScreen("Bestiary", new BestiaryScreen()); });
            AddLoadAction(delegate { AddScreen("BestiaryDescription", new BestiaryDescriptionScreen()); });
            AddLoadAction(delegate { AddScreen("Help", new HelpScreen()); });
            AddLoadAction(delegate { AddScreen("HelpTopic", new HelpTopicScreen()); });
            AddLoadAction(delegate { AddScreen("Settings", new SettingsScreen()); });
            AddLoadAction(delegate { AddScreen("SettingsPerformance", new SettingsPerformanceScreen()); });
            AddLoadAction(delegate { AddScreen("SettingsGraphics", new SettingsGraphicsScreen()); });
            AddLoadAction(delegate { AddScreen("SettingsUi", new SettingsUiScreen()); });
            AddLoadAction(delegate { AddScreen("SettingsCompatibility", new SettingsCompatibilityScreen()); });
            AddLoadAction(delegate { AddScreen("SettingsAudio", new SettingsAudioScreen()); });
            AddLoadAction(delegate { AddScreen("SettingsControls", new SettingsControlsScreen()); });
            AddLoadAction(delegate { AddScreen("Play", new PlayScreen()); });
            AddLoadAction(delegate { AddScreen("NewWorld", new NewWorldScreen()); });
            AddLoadAction(delegate { AddScreen("ModifyWorld", new ModifyWorldScreen()); });
            AddLoadAction(delegate { AddScreen("WorldOptions", new WorldOptionsScreen()); });
            AddLoadAction(delegate { AddScreen("GameLoading", new GameLoadingScreen()); });
            AddLoadAction(delegate { AddScreen("Game", new GameScreen()); });
            AddLoadAction(delegate { AddScreen("TrialEnded", new TrialEndedScreen()); });
            AddLoadAction(delegate { AddScreen("ExternalContent", new ExternalContentScreen()); });
            AddLoadAction(delegate { AddScreen("CommunityContent", new CommunityContentScreen()); });
            AddLoadAction(delegate { AddScreen("OriginalCommunityContent", new OriginalCommunityContentScreen()); });
            AddLoadAction(delegate { AddScreen("Content", new ContentScreen()); });
            AddLoadAction(delegate { AddScreen("ManageContent", new ManageContentScreen()); });
            AddLoadAction(delegate { AddScreen("ModsManageContent", new ModsManageContentScreen()); });
            AddLoadAction(delegate { AddScreen("Releases", new ReleasesScreen()); });
            /*AddLoadAction(delegate
            {
                AddScreen("ManageUser", new ManageUserScreen());
            });*/
            AddLoadAction(delegate { AddScreen("Players", new PlayersScreen()); });
            AddLoadAction(delegate { AddScreen("Player", new PlayerScreen()); });
            AddLoadAction(delegate { AddScreen("KeyboardMapping", new KeyboardMappingScreen()); });
            AddLoadAction(delegate { AddScreen("GamepadMapping", new GamepadMappingScreen()); });
            AddLoadAction(delegate { AddScreen("VrControllerMapping", new VrControllerMappingScreen()); });
            AddLoadAction(delegate { AddScreen("CameraManage", new CameraManageScreen()); });
            AddLoadAction(delegate { AddScreen("ManageClassSubstitutes", new ManageClassSubstitutesScreen()); });
            AddLoadAction(delegate { AddScreen("ModSettings", new ModSettingsScreen()); });
        }

        public void AddScreen(string name, Screen screen) {
            ScreensManager.AddScreen(name, screen);
        }

        void AddLoadAction(Action action) {
            LoadingActoins.Add(action);
        }

        public override void Leave() {
            LogList?.ClearItems();
#if ANDROID
            // 当前Android端SDL不支持半垂直同步
            if (SettingsManager.PresentationInterval > 1) {
                SettingsManager.PresentationInterval = 1;
            }
#endif
            Window.PresentationInterval = SettingsManager.PresentationInterval;
            ContentManager.Dispose("Textures/Gui/CandyRufusLogo");
            ContentManager.Dispose("Textures/Gui/EngineLogo");
        }

        public override void Enter(object[] parameters) {
            Window.PresentationInterval = 1;
            List<string> remove = new();
            foreach (KeyValuePair<string, Screen> screen in ScreensManager.m_screens) {
                if (screen.Value == this) {
                    continue;
                }
                remove.Add(screen.Key);
            }
            foreach (string screen in remove) {
                ScreensManager.m_screens.Remove(screen);
            }
            InitActions();
            base.Enter(parameters);
        }

        public override void Update() {
            if (Input.Back
                || Input.Cancel) {
                ConfirmQuit();
            }
            if (!ModsManager.GetAllowContinue()) {
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            while (!m_isContentLoaded
                || sw.ElapsedMilliseconds < 100) {
                if (ModLoadingActoins.Count > 0) {
                    try {
                        ModLoadingActoins[0].Invoke();
                    }
                    catch (Exception e) {
                        Error(e.ToString());
                        break;
                    }
                    finally {
                        ModLoadingActoins.RemoveAt(0);
                    }
                }
                else if (LoadingActoins.Count > 0) {
                    try {
                        LoadingActoins[0].Invoke();
                    }
                    catch (Exception e) {
                        Error(e.ToString());
                        break;
                    }
                    finally {
                        LoadingActoins.RemoveAt(0);
                    }
                }
                else {
                    break;
                }
            }
            sw.Stop();
        }

        public void ConfirmQuit() {
#if ANDROID
#pragma warning disable CA1416
            if (Android.OS.Build.VERSION.SdkInt < (Android.OS.BuildVersionCodes)21) {
                Window.Close();
                return;
            }
            Window.Activity.RunOnUiThread(() => {
                    new AlertDialog.Builder(Window.Activity).SetMessage("Exit 退出?")
                        ?.SetPositiveButton("Yes 是", (_, _) => { Window.Close(); })
                        ?.SetNegativeButton("No 否", (_, _) => { })
                        ?.Show();
                }
            );
#pragma warning restore CA1416
#elif WINDOWS
            /*
            Task.Run(() =>
            {
                if(MessageBox.Show("Exit 退出?","", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Window.Close();
                }
            });*/
#endif
        }
    }
}