using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Input;
using TemplatesDatabase;
using XmlUtilities;

namespace Game {
    public static class SettingsManager {
        public static float m_soundsVolume;

        public static float m_musicVolume;

        public static float m_brightness;

        public static ResolutionMode m_resolutionMode;

        public static WindowMode m_windowMode;

        public static Point2 m_resizableWindowPosition;

        public static Point2 m_resizableWindowSize;

        public static bool UsePrimaryMemoryBank { get; set; }

        public static bool AllowInitialIntro { get; set; }
        public static bool DeleteWorldNeedToText { get; set; }

        public static bool CreativeDragMaxStacking { get; set; }

        public static float Touchoffset { get; set; }

        public const string fName = "SettingsManager";

        public static float SoundsVolume {
            get => m_soundsVolume;
            set => m_soundsVolume = MathUtils.Saturate(value);
        }

        public static float MusicVolume {
            get => m_musicVolume;
            set => m_musicVolume = MathUtils.Saturate(value);
        }

        public static int VisibilityRange { get; set; }

        public static bool UseVr { get; set; }

        public static float UIScale { get; set; }

        public static float VrGuiSize { get; set; }

        public static bool VrGuiDockLeftHand { get; set; }

        public static float VrEyeHeightOffset { get; set; }

        public static VrLookControlMode VrLookControlMode { get; set; }

        public static VrMoveControlMode VrMoveControlMode { get; set; }

        public static ResolutionMode ResolutionMode {
            get => m_resolutionMode;
            set {
                if (value != m_resolutionMode) {
                    m_resolutionMode = value;
                    SettingChanged?.Invoke("ResolutionMode");
                }
            }
        }

        public static float ViewAngle { get; set; }

        public static SkyRenderingMode SkyRenderingMode { get; set; }

        public static bool TerrainMipmapsEnabled { get; set; }

        public static bool ObjectsShadowsEnabled { get; set; }

        public static float Brightness {
            get => m_brightness;
            set {
                value = Math.Clamp(value, 0f, 1f);
                if (value != m_brightness) {
                    m_brightness = value;
                    SettingChanged?.Invoke("Brightness");
                }
            }
        }

        public static int PresentationInterval { get; set; }

        public static bool ShowGuiInScreenshots { get; set; }

        public static bool ShowLogoInScreenshots { get; set; }

        public static ScreenshotSize ScreenshotSize { get; set; }
#pragma warning disable CS0649
        private static Point2 m_screenshotSizeCustom;
#pragma warning restore CS0649

        public static Point2 ScreenshotSizeCustom {
            get { return m_screenshotSizeCustom; }
            set {
                int max = Math.Min(Display.MaxTextureSize, 16384);
                int width = MathUtils.Clamp(value.X, 120, max);
                int height = MathUtils.Clamp(value.Y, 120, max);
                value = new Point2(width, height);
            }
        }
        public static int[] ScreenshotSizeCustomWidths = [
            80,
            160,
            320,
            480,
            640,
            800,
            960,
            1280,
            1600,
            1920, //default
            2560,
            3840,
            5120,
            6144,
            7680,
            10240,
            12288,
            15360
        ];
        private static int m_screenshotSizeCustomWidthIndex;

        public static int ScreenshotSizeCustomWidthIndex {
            get => m_screenshotSizeCustomWidthIndex;
            set {
                m_screenshotSizeCustomWidthIndex = MathUtils.Clamp(value, 0, ScreenshotSizeCustomWidths.Length - 1);
                int widthMax = Math.Min(Display.MaxTextureSize, 16384);
                if (widthMax <= ScreenshotSizeCustomWidths[m_screenshotSizeCustomWidthIndex]) {
                    value = ScreenshotSizeCustomWidths.GetLastIndexOfAnyInRange(0,widthMax);
                }
                m_screenshotSizeCustomWidthIndex = value;
            }
        }

        private static int GetLastIndexOfAnyInRange(this int[] array,int minValue,int maxValue) {
            for (int i = array.Length - 1; i >= 0; i--) {
                if (array[i] >= minValue && array[i] <= maxValue)
                    return i;
            }
            return 0;
        }

        public static float[] ScreenshotSizeCustomAspectRatios = [
            1f,
            4f / 5f,
            3f / 4f,
            2f / 3f,
            10f / 16f,
            9f / 16f, //default
            0.5f,
            27f / 64f,
            9f / 32f,
            27f / 128f
        ];

        public static string[] ScreenshotSizeCustomAspectRatiosNames = [
            "1:1",
            "5:4",
            "4:3",
            "3:2",
            "16:10",
            "16:9",
            "18:9",
            "21:9",
            "32:9",
            "42:9"
        ];
        private static int m_screenshotSizeCustomAspectRatioIndex;
        public static int ScreenshotSizeCustomAspectRatioIndex {
            get {  return m_screenshotSizeCustomAspectRatioIndex; }
            set {
                m_screenshotSizeCustomAspectRatioIndex = MathUtils.Clamp(value, 0, ScreenshotSizeCustomAspectRatios.Length - 1);
            }
        }

        public static WindowMode WindowMode {
#if BROWSER
            get => Window.WindowMode;
            set {
                value = value == WindowMode.Fullscreen ? WindowMode.Fullscreen : WindowMode.Fixed;
                Window.WindowMode = value;
                m_windowMode = value;
            }
#else
            get => m_windowMode;
            set {
                if (value != m_windowMode) {
                    if (value == WindowMode.Borderless) {
                        m_resizableWindowSize = Window.Size;
                        m_resizableWindowPosition = Window.Position;
                        Window.Position = Point2.Zero;
                        Window.Size = Window.ScreenSize;
                    }
                    else if (value == WindowMode.Fullscreen
                        && m_windowMode != WindowMode.Borderless) {
                        m_resizableWindowSize = Window.Size;
                        m_resizableWindowPosition = Window.Position;
                    }
                    Window.WindowMode = value;
                    m_windowMode = value;
                    if (value == WindowMode.Resizable) {
                        Window.Position = m_resizableWindowPosition;
                        Window.Size = m_resizableWindowSize;
                    }
                }
                ModsManager.HookAction(
                    "WindowModeChanged",
                    loader => {
                        loader.WindowModeChanged(value);
                        return false;
                    }
                );
            }
#endif
        }

        #region 简单设置项

        public static GuiSize GuiSize { get; set; }

        public static bool HideMoveLookPads { get; set; }

        public static bool HideCrosshair { get; set; }

        public static string BlocksTextureFileName { get; set; }

        public static MoveControlMode MoveControlMode { get; set; }

        public static LookControlMode LookControlMode { get; set; }

        public static bool LeftHandedLayout { get; set; }

        public static bool FlipVerticalAxis { get; set; }

        public static float MoveSensitivity { get; set; }

        public static float LookSensitivity { get; set; }

        public static float GamepadDeadZone { get; set; }

        public static float GamepadCursorSpeed { get; set; }

        /// <summary>
        ///     手柄扳机触发阈值，范围0~1，默认0.5。扳机的按压幅度只有超过这个数时才会被视为“按下”状态，越小则越容易触发。
        /// </summary>
        public static float GamepadTriggerThreshold { get; set; }

        public static float CreativeDigTime { get; set; }

        public static float CreativeReach { get; set; }

        public static float MinimumHoldDuration { get; set; }

        public static float MinimumDragDistance { get; set; }

        public static bool AutoJump { get; set; }

        public static bool HorizontalCreativeFlight { get; set; }

        public static string DropboxAccessToken { get; set; }

        public static string MotdUpdateUrl { get; set; }

        public static string MotdUpdateCheckUrl { get; set; }
        public static string ScpboxAccessToken { get; set; }

        public static string ScpboxUserInfo { get; set; }

        public static bool MotdUseBackupUrl { get; set; }

        public static double MotdUpdatePeriodHours { get; set; }

        public static DateTime MotdLastUpdateTime { get; set; }

        public static string MotdLastDownloadedData { get; set; }

        public static string UserId { get; set; }

        public static string LastLaunchedVersion { get; set; }

        public static CommunityContentMode CommunityContentMode { get; set; }

        public static CommunityContentMode OriginalCommunityContentMode { get; set; }

        public static bool MultithreadedTerrainUpdate { get; set; }

        public static int IsolatedStorageMigrationCounter { get; set; }

        public static bool DisplayFpsCounter { get; set; }

        public static bool DisplayFpsRibbon { get; set; }

        public static int NewYearCelebrationLastYear { get; set; }

        public static ScreenLayout ScreenLayout1 { get; set; }

        public static ScreenLayout ScreenLayout2 { get; set; }

        public static ScreenLayout ScreenLayout3 { get; set; }

        public static ScreenLayout ScreenLayout4 { get; set; }

        public static bool UpsideDownLayout { get; set; }

        #endregion

        public static bool FullScreenMode {
            get => Window.WindowMode == WindowMode.Fullscreen;
            set {
                if (value && Window.WindowMode != WindowMode.Fullscreen) {
                    Window.WindowMode = WindowMode.Fullscreen;
                }
                else if (!value
                    && Window.WindowMode == WindowMode.Fullscreen) {
                    Window.WindowMode = WindowMode.Resizable;
                }
#if !BROWSER
                ModsManager.HookAction(
                    "WindowModeChanged",
                    loader => {
                        loader.WindowModeChanged(WindowMode);
                        return false;
                    }
                );
#endif
            }
        }

        public static bool DisplayLog { get; set; }

        public static string BulletinTime { get; set; }

        public static bool DragHalfInSplit { get; set; }

        public static bool ShortInventoryLooping { get; set; }

        public static float LowFPSToTimeDeceleration { get; set; }

        public static bool UseAPISleepTimeAcceleration { get; set; }

        public static float MoveWidgetMarginX { get; set; }
        public static float MoveWidgetMarginY { get; set; }

        [Obsolete("该变量目前尚未使用，有待后续API版本完善。后续完善后模组可能用到，为了向未来兼容别删")]
        public static float MoveWidgetSize { get; set; }

        public static int AnimatedTextureRefreshLimit { get; set; }

        public static bool FileAssociationEnabled {
            get {
#if WINDOWS
                return field;
#elif ANDROID
                return true;
#else
                return false;
#endif
            }
            set {
#if WINDOWS
                field = value;
#endif
            }
        }

        public static string DisabledMods {
            get {
                List<string> result = [];
                foreach ((string packageName, HashSet<string> versions) in ModsManager.DisabledMods) {
                    if (versions.Count == 0) {
                        continue;
                    }
                    result.Add(packageName);
                    result.Add(versions.Count.ToString());
                    foreach (string version in versions) {
                        result.Add(version);
                    }
                }
                return string.Join(";", result);
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    return;
                }
                string[] array = value.Split(';');
                Dictionary<string, HashSet<string>> result = [];
                int i = 0;
                while (i < array.Length) {
                    string packageName = array[i++];
                    if (int.TryParse(array[i++], out int count)) {
                        HashSet<string> versions = new(count);
                        int end = i + count;
                        while (i < end) {
                            versions.Add(array[i++]);
                        }
                        result.Add(packageName, versions);
                    }
                }
                ModsManager.DisabledMods = result;
            }
        }

        //因为赋值时ModsManager还没准备好，所以不在此处理get和set
        public static string ModLoadAfters { get; set; }

        public static bool SafeMode { get; set; }

        public static bool AdaptEdgeToEdgeDisplay { get; set; }

        public static event Action<string> SettingChanged;
        public static ValuesDictionary KeyboardMappingSettings { get; set; }
        public static ValuesDictionary GamepadMappingSettings { get; set; }
        public static ValuesDictionary CameraManageSettings { get; set; }
        public static ValuesDictionary VrMappingSettings { get; set; }
        public static VrControllerType VrActiveControllerType { get; set; } = VrControllerType.Unknown;

        public static string CommunityServerUserInfos {
            get {
                if (CommunityServerManager.UserInfos.Count == 0) {
                    return string.Empty;
                }
                List<string> results = [];
                foreach (CommunityServerManager.Info info in CommunityServerManager.UserInfos) {
                    results.Add(info.ToString());
                }
                return string.Join("||", results);
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    return;
                }
                string[] array1 = value.Split("||", StringSplitOptions.RemoveEmptyEntries);
                foreach (string str in array1) {
                    CommunityServerManager.Info info = CommunityServerManager.Info.FromString(str);
                    if (info != null) {
                        CommunityServerManager.UserInfos.Add(info);
                    }
                }
            }
        }

        public static string LastSelectedOriginalCommunityInfo {
            get => CommunityServerManager.CurrentOriginalInfo.ToString();
            set => CommunityServerManager.Info.FromString(value);
        }

        public static string LastSelectedChineseCommunityInfo {
            get => CommunityServerManager.CurrentChineseInfo.ToString();
            set => CommunityServerManager.Info.FromString(value);
        }

        static string m_databaseClassSubstitutes;

        public static string DatabaseClassSubstitutes {
            get {
                if (m_databaseClassSubstitutes == null) {
                    if (ModsManager.ClassSubstitutes.Count > 0) {
                        StringBuilder sb = new();
                        foreach ((string guid, List<ModsManager.ClassSubstitute> substitutes) in ModsManager.ClassSubstitutes) {
                            if (substitutes.Count == 0) {
                                continue;
                            }
                            sb.Append($"{guid},");
                            foreach (ModsManager.ClassSubstitute substitute in substitutes) {
                                sb.Append($"{substitute.PackageName},{substitute.ClassName},");
                            }
                            sb.Length--;
                            sb.Append(";");
                        }
                        m_databaseClassSubstitutes = sb.ToString();
                    }
                    else {
                        m_databaseClassSubstitutes = string.Empty;
                    }
                }
                return m_databaseClassSubstitutes;
            }
            set {
                ModsManager.OldClassSubstitutes.Clear();
                if (string.IsNullOrEmpty(value)) {
                    return;
                }
                string[] array1 = value.Split(';');
                foreach (string str in array1) {
                    string[] array2 = str.Split(',');
                    if (array2.Length < 5) {
                        continue;
                    }
                    string guid = array2[0];
                    List<ModsManager.ClassSubstitute> substitutes = [];
                    for (int i = 1; i < array2.Length; i += 2) {
                        substitutes.Add(new ModsManager.ClassSubstitute(array2[i], array2[i + 1]));
                    }
                    ModsManager.OldClassSubstitutes.Add(guid, substitutes);
                }
            }
        }

        public static string DatabaseSelectedClassSubstitutes {
            get {
                if (ModsManager.SelectedClassSubstitutes.Count > 0) {
                    StringBuilder sb = new();
                    foreach ((string guid, ModsManager.ClassSubstitute substitute) in ModsManager.SelectedClassSubstitutes) {
                        sb.Append($"{guid},{substitute.PackageName},{substitute.ClassName};");
                    }
                    sb.Length--;
                    return sb.ToString();
                }
                return string.Empty;
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    return;
                }
                string[] array = value.Split(';');
                foreach (string str in array) {
                    string[] array2 = str.Split(',');
                    if (array2.Length == 3) {
                        ModsManager.SelectedClassSubstitutes.Add(array2[0], new ModsManager.ClassSubstitute(array2[1], array2[2]));
                    }
                }
            }
        }

        static readonly Lock m_saveLock = new();

        public static void Initialize() {
            {
                DisplayLog = false;
                DragHalfInSplit = true;
                ShortInventoryLooping = false;
                m_resolutionMode = ResolutionMode.High;
                VisibilityRange = 128;
                ViewAngle = 1f;
                TerrainMipmapsEnabled = false;
                SkyRenderingMode = SkyRenderingMode.Full;
                ObjectsShadowsEnabled = true;
                PresentationInterval = 1;
                m_soundsVolume = 1.0f;
                m_musicVolume = 0.2f;
                m_brightness = 0.8f;
                ShowGuiInScreenshots = false;
                ShowLogoInScreenshots = true;
                ScreenshotSize = ScreenshotSize.ScreenSize;
                ScreenshotSizeCustomWidthIndex = 9;
                ScreenshotSizeCustomAspectRatioIndex = 5;
                MoveControlMode = MoveControlMode.Buttons;
                HideMoveLookPads = false;
                HideCrosshair = false;
                AllowInitialIntro = true;
                DeleteWorldNeedToText = false;
                BlocksTextureFileName = string.Empty;
                LookControlMode = LookControlMode.EntireScreen;
                FlipVerticalAxis = false;
#if ANDROID || BROWSER
                UIScale = 0.9f;
                AutoJump = true;
#else
                UIScale = 0.75f;
                AutoJump = false;
#endif
                VrGuiSize = 1f;
                VrEyeHeightOffset = 0f;
                MoveSensitivity = 0.5f;
                LookSensitivity = 0.5f;
                GamepadDeadZone = 0.16f;
                GamepadCursorSpeed = 1f;
                GamepadTriggerThreshold = 0.5f;
                CreativeDigTime = 0.33f;
                CreativeReach = 7.5f;
                MinimumHoldDuration = 0.25f;
                MinimumDragDistance = 10f;
                HorizontalCreativeFlight = false;
                DropboxAccessToken = string.Empty;
                ScpboxAccessToken = string.Empty;
                MotdUpdateUrl = $"{CommunityServerManager.CurrentChineseInfo.ApiUrl}com/motd?v={0}&l={1}";
                MotdUpdateCheckUrl = $"{CommunityServerManager.CurrentChineseInfo.ApiUrl}com/motd?v={0}&cmd=version_check&platform={1}&apiv={2}&l={3}";
                MotdUpdatePeriodHours = 12.0;
                MotdLastUpdateTime = DateTime.MinValue;
                MotdLastDownloadedData = string.Empty;
                UserId = string.Empty;
                LastLaunchedVersion = string.Empty;
                CommunityContentMode = CommunityContentMode.Normal;
                OriginalCommunityContentMode = CommunityContentMode.Normal;
                MultithreadedTerrainUpdate = true;
                NewYearCelebrationLastYear = 2025;
                ScreenLayout1 = ScreenLayout.Single;
                ScreenLayout2 = Window.ScreenSize.X / (float)Window.ScreenSize.Y > 1.33333337f
                    ? ScreenLayout.DoubleVertical
                    : ScreenLayout.DoubleHorizontal;
                ScreenLayout3 = Window.ScreenSize.X / (float)Window.ScreenSize.Y > 1.33333337f
                    ? ScreenLayout.TripleVertical
                    : ScreenLayout.TripleHorizontal;
                ScreenLayout4 = ScreenLayout.Quadruple;
                BulletinTime = string.Empty;
                ScpboxUserInfo = string.Empty;
                HorizontalCreativeFlight = true;
                CreativeDragMaxStacking = true;
                LowFPSToTimeDeceleration = 10;
                UseAPISleepTimeAcceleration = false;
                //MoveWidgetSize = 1f;
                MoveWidgetMarginX = 0f;
                MoveWidgetMarginY = 0f;
#if BROWSER
                AnimatedTextureRefreshLimit = 2;
#else
                AnimatedTextureRefreshLimit = 7;
#endif
                FileAssociationEnabled = true;
                SafeMode = false;
                AdaptEdgeToEdgeDisplay = Window.HasWideNotch;
                InitializeKeyboardMappingSettings();
                InitializeGamepadMappingSettings();
                InitializeVrMappingSettings();
                InitializeCameraManageSettings();
            }
            LoadSettings();
            TextBoxWidget.ShowCandidatesWindow = FullScreenMode;
            Window.Deactivated += delegate { SaveSettings(); };
        }

        public static void InitializeKeyboardMappingSettings() {
            KeyboardMappingSettings = new ValuesDictionary();
            KeyboardMappingSettings.SetValue("MoveLeft", Key.A);
            KeyboardMappingSettings.SetValue("MoveRight", Key.D);
            KeyboardMappingSettings.SetValue("MoveFront", Key.W);
            KeyboardMappingSettings.SetValue("MoveBack", Key.S);
            KeyboardMappingSettings.SetValue("MoveUp", Key.Space);
            KeyboardMappingSettings.SetValue("MoveDown", Key.Shift);
            KeyboardMappingSettings.SetValue("Jump", Key.Space);
            KeyboardMappingSettings.SetValue("Dig", MouseButton.Left);
            KeyboardMappingSettings.SetValue("Hit", MouseButton.Left);
            KeyboardMappingSettings.SetValue("Interact", MouseButton.Right);
            KeyboardMappingSettings.SetValue("Aim", MouseButton.Right);
            KeyboardMappingSettings.SetValue("ToggleCrouch", Key.Shift);
            KeyboardMappingSettings.SetValue("ToggleMount", Key.R);
            KeyboardMappingSettings.SetValue("ToggleFly", Key.F);
            KeyboardMappingSettings.SetValue("PickBlockType", MouseButton.Middle);
            KeyboardMappingSettings.SetValue("ToggleInventory", Key.E);
            KeyboardMappingSettings.SetValue("ToggleClothing", Key.C);
            KeyboardMappingSettings.SetValue("TakeScreenshot", Key.P);
            KeyboardMappingSettings.SetValue("SwitchCameraMode", Key.V);
            KeyboardMappingSettings.SetValue("TimeOfDay", Key.T);
            KeyboardMappingSettings.SetValue("Lightning", Key.L);
            KeyboardMappingSettings.SetValue("Precipitation", Key.K);
            KeyboardMappingSettings.SetValue("Fog", Key.J);
            KeyboardMappingSettings.SetValue("Drop", Key.Q);
            KeyboardMappingSettings.SetValue("EditItem", Key.G);
            KeyboardMappingSettings.SetValue("KeyboardHelp", Key.H);
        }

        public static void InitializeGamepadMappingSettings() {
            GamepadMappingSettings = new ValuesDictionary();
            GamepadMappingSettings.SetValue("MoveUp", GamePadButton.A);
            GamepadMappingSettings.SetValue("MoveDown", GamePadButton.RightShoulder);
            GamepadMappingSettings.SetValue("Jump", GamePadButton.A);
            GamepadMappingSettings.SetValue("Dig", GamePadTrigger.Right);
            GamepadMappingSettings.SetValue("Hit", GamePadTrigger.Right);
            GamepadMappingSettings.SetValue("Interact", GamePadTrigger.Left);
            GamepadMappingSettings.SetValue("Aim", GamePadTrigger.Left);
            GamepadMappingSettings.SetValue("ToggleCrouch", GamePadButton.RightShoulder);
            GamepadMappingSettings.SetValue("ToggleMount", GamePadButton.DPadUp);
            GamepadMappingSettings.SetValue("ToggleFly", GamePadButton.Null);
            GamepadMappingSettings.SetValue("PickBlockType", GamePadButton.Null);
            GamepadMappingSettings.SetValue("ToggleInventory", GamePadButton.X);
            GamepadMappingSettings.SetValue("ToggleClothing", GamePadButton.Y);
            GamepadMappingSettings.SetValue("TakeScreenshot", GamePadButton.Null);
            GamepadMappingSettings.SetValue("SwitchCameraMode", GamePadButton.DPadDown);
            GamepadMappingSettings.SetValue("TimeOfDay", GamePadButton.Null);
            GamepadMappingSettings.SetValue("Lightning", GamePadButton.Null);
            GamepadMappingSettings.SetValue("Precipitation", GamePadButton.Null);
            GamepadMappingSettings.SetValue("Fog", GamePadButton.Null);
            GamepadMappingSettings.SetValue("Drop", GamePadButton.B);
            GamepadMappingSettings.SetValue("EditItem", GamePadButton.LeftShoulder);
            GamepadMappingSettings.SetValue("GamepadHelp", GamePadButton.Start);
            GamepadMappingSettings.SetValue("Back", GamePadButton.Back);
        }

        public static void InitializeCameraManageSettings() { //键表示摄像机的类名，值表示摄像机的排序（小于0则禁用）
            CameraManageSettings = new ValuesDictionary();
            CameraManageSettings.SetValue("Game.FppCamera", 0);
            CameraManageSettings.SetValue("Game.TppCamera", 1);
            CameraManageSettings.SetValue("Game.OrbitCamera", 2);
            CameraManageSettings.SetValue("Game.FixedCamera", 3);
        }

        public static void InitializeVrMappingSettings() {
            VrMappingSettings = new ValuesDictionary();
        }

        static ValuesDictionary GetActiveVrMappingDict() {
            VrControllerType type = VrActiveControllerType;
            if (type == VrControllerType.Unknown) type = VrControllerType.MetaQuestTouch;
            string typeName = type.ToString();
            if (!VrMappingSettings.TryGetValue(typeName, out object val) || val is not ValuesDictionary dict) {
                dict = new ValuesDictionary();
                VrMappingSettings.SetValue(typeName, dict);
            }
            return dict;
        }

        static void SetVrBinding(string name, VrController ctrl, VrControllerButton btn) {
            var dict = GetActiveVrMappingDict();
            if (btn == VrControllerButton.Null) {
                dict.SetValue(name, VrControllerButton.Null);
                return;
            }
            ValuesDictionary vd = [];
            vd.SetValue("Controller", ctrl);
            vd.SetValue("Button", btn);
            dict.SetValue(name, vd);
        }

        public static (VrController Controller, VrControllerButton Button) GetVrMapping(string name) {
            var dict = GetActiveVrMappingDict();
            if (dict.TryGetValue(name, out object val)) {
                if (val is VrControllerButton btn) return (VrController.Left, btn); // Null case
                if (val is ValuesDictionary vd)
                    return (vd.GetValue<VrController>("Controller"), vd.GetValue<VrControllerButton>("Button"));
            }
            return (VrController.Left, VrControllerButton.Null);
        }

        public static void SetVrMapping(string name, VrController ctrl, VrControllerButton btn) {
            SetVrBinding(name, ctrl, btn);
        }

        public static void EnsureVrMappingDefaults(VrControllerType type) {
            if (type == VrControllerType.Unknown) {
                type = VrControllerType.MetaQuestTouch;
            }
            VrActiveControllerType = type;
            string typeName = type.ToString();
            if (VrMappingSettings.TryGetValue(typeName, out object val) && val is ValuesDictionary vd && vd.Count > 0) {
                return; // Already has saved settings for this controller type
            }
            SetDefaultVrBindings();
            ApplyControllerSpecificDefaults(type);
        }

        public static void ResetVrMappingDefaults() {
            // Clear and recreate defaults for the active controller type
            VrControllerType type = VrActiveControllerType;
            if (type == VrControllerType.Unknown) type = VrControllerType.MetaQuestTouch;
            string typeName = type.ToString();
            // Replace sub-dict entirely
            VrMappingSettings.SetValue(typeName, new ValuesDictionary());
            SetDefaultVrBindings();
            ApplyControllerSpecificDefaults(type);
        }

        static void SetDefaultVrBindings() {
            // Left controller defaults (Quest-style)
            SetVrBinding("VrInteract", VrController.Left, VrControllerButton.Trigger);
            SetVrBinding("VrAim", VrController.Left, VrControllerButton.Trigger);
            SetVrBinding("VrEditItem", VrController.Left, VrControllerButton.Grip);
            SetVrBinding("VrToggleMount", VrController.Left, VrControllerButton.Secondary);
            SetVrBinding("VrToggleCrouch", VrController.Left, VrControllerButton.Primary);
            SetVrBinding("VrToggleInventory", VrController.Left, VrControllerButton.Thumbrest);
            SetVrBinding("VrToggleClothing", VrController.Left, VrControllerButton.Null);
            SetVrBinding("VrJump", VrController.Left, VrControllerButton.Trackpad);
            // Right controller defaults
            SetVrBinding("VrHit", VrController.Right, VrControllerButton.Trigger);
            SetVrBinding("VrDig", VrController.Right, VrControllerButton.Trigger);
            SetVrBinding("VrDrop", VrController.Right, VrControllerButton.Grip);
            SetVrBinding("VrScrollLeft", VrController.Right, VrControllerButton.Secondary);
            SetVrBinding("VrScrollRight", VrController.Right, VrControllerButton.Primary);
            SetVrBinding("VrToggleFly", VrController.Right, VrControllerButton.Thumbrest);
            SetVrBinding("VrSwitchCameraMode", VrController.Right, VrControllerButton.Null);
        }

        static readonly Dictionary<string, VrControllerButton> VrActionAlternatives = new() {
            ["VrToggleMount"] = VrControllerButton.TrackpadUp,
            ["VrToggleCrouch"] = VrControllerButton.TrackpadDown,
            ["VrToggleInventory"] = VrControllerButton.Thumbrest,
            ["VrScrollLeft"] = VrControllerButton.TrackpadLeft,
            ["VrScrollRight"] = VrControllerButton.TrackpadRight,
            ["VrToggleFly"] = VrControllerButton.TrackpadUp,
        };

        public static VrControllerButton? GetVrActionAlternative(string name) {
            return VrActionAlternatives.TryGetValue(name, out var alt) ? alt : null;
        }

        public static void ApplyControllerSpecificDefaults(VrControllerType type) {
            switch (type) {
            case VrControllerType.HtcVive:
            case VrControllerType.MicrosoftMRMotion:
                SetVrBinding("VrToggleMount", VrController.Left, VrControllerButton.TrackpadUp);
                SetVrBinding("VrToggleCrouch", VrController.Left, VrControllerButton.TrackpadDown);
                SetVrBinding("VrToggleInventory", VrController.Left, VrControllerButton.TrackpadLeft);
                SetVrBinding("VrToggleClothing", VrController.Left, VrControllerButton.TrackpadRight);
                SetVrBinding("VrToggleFly", VrController.Right, VrControllerButton.TrackpadUp);
                SetVrBinding("VrSwitchCameraMode", VrController.Right, VrControllerButton.TrackpadDown);
                break;
            case VrControllerType.ValveIndex:
                SetVrBinding("VrToggleMount", VrController.Left, VrControllerButton.Primary);
                SetVrBinding("VrToggleCrouch", VrController.Left, VrControllerButton.TrackpadDown);
                SetVrBinding("VrToggleInventory", VrController.Left, VrControllerButton.TrackpadLeft);
                SetVrBinding("VrToggleClothing", VrController.Left, VrControllerButton.TrackpadRight);
                SetVrBinding("VrToggleFly", VrController.Right, VrControllerButton.TrackpadUp);
                SetVrBinding("VrSwitchCameraMode", VrController.Right, VrControllerButton.TrackpadDown);
                break;
            case VrControllerType.PICO:
                // PICO has no Thumbrest — disable actions that use it
                SetVrBinding("VrToggleInventory", VrController.Left, VrControllerButton.Null);
                SetVrBinding("VrToggleFly", VrController.Right, VrControllerButton.Null);
                break;
            // QuestTouch/ViveFocus3 use SetDefaultVrBindings defaults
            }
        }

        public static object GetKeyboardMapping(string keyName, bool throwIfNotFound = true) {
            if (KeyboardMappingSettings.TryGetValue(keyName, out object result)) { //原版设置
                return result;
            }
            foreach (ValuesDictionary item in ModSettingsManager.ModKeyboardMapSettings.Values) { //模组设置
                if (item.TryGetValue(keyName, out object result2)) {
                    return result2;
                }
            }
            return throwIfNotFound ? throw new ArgumentException(string.Format(LanguageControl.Get(fName, "1"), keyName)) : null;
        }

        public static object GetGamepadMapping(string keyName, bool throwIfNotFound = true) {
            if (GamepadMappingSettings.TryGetValue(keyName, out object result)) { //原版设置
                return result;
            }
            foreach (ValuesDictionary item in ModSettingsManager.ModGamepadMapSettings.Values) { //模组设置
                if (item.TryGetValue(keyName, out object result2)) {
                    return result2;
                }
            }
            return throwIfNotFound ? throw new ArgumentException(string.Format(LanguageControl.Get(fName, "1"), keyName)) : null;
        }

        public static int GetCameraManageSetting(string keyName, bool throwIfNotFound = true) {
            if (CameraManageSettings.TryGetValue(keyName, out object result)) { //原版设置
                return Convert.ToInt32(result);
            }
            foreach (ValuesDictionary item in ModSettingsManager.ModCameraManageSettings.Values) { //模组设置
                if (item.TryGetValue(keyName, out object result2)) {
                    return Convert.ToInt32(result2);
                }
            }
            return throwIfNotFound ? throw new ArgumentException(string.Format(LanguageControl.Get(fName, "2"), keyName)) : -1;
        }

        /// <summary>
        ///     仅用于修改现有键盘鼠标键位，添加键位请使用<see cref="ModLoader.GetKeyboardMappings" />
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        public static void SetKeyboardMapping(string keyName, object value) {
            if (KeyboardMappingSettings.ContainsKey(keyName)) { //原版设置
                KeyboardMappingSettings[keyName] = value;
            }
            else {
                foreach (ValuesDictionary item in ModSettingsManager.ModKeyboardMapSettings.Values) { //模组设置
                    if (item.ContainsKey(keyName)) {
                        item[keyName] = value;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     仅用于修改现有手柄键位，添加键位请使用<see cref="ModLoader.GetGamepadMappings" />
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        public static void SetGamepadMapping(string keyName, object value) {
            if (GamepadMappingSettings.ContainsKey(keyName)) { //原版设置
                GamepadMappingSettings[keyName] = value;
            }
            else {
                foreach (ValuesDictionary item in ModSettingsManager.ModGamepadMapSettings.Values) { //模组设置
                    if (item.ContainsKey(keyName)) {
                        item[keyName] = value;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     仅用于修改现有相机配置，添加相机配置请使用<see cref="ModLoader.GetCameraList" />
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="value"></param>
        public static void SetCameraManageSetting(string keyName, int value) {
            if (CameraManageSettings.ContainsKey(keyName)) { //原版设置
                CameraManageSettings[keyName] = value;
            }
            else {
                foreach (ValuesDictionary item in ModSettingsManager.ModCameraManageSettings.Values) { //模组设置
                    if (item.ContainsKey(keyName)) {
                        item[keyName] = value;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     文件存在则读取并返回真否则返回假
        /// </summary>
        public static bool LoadSettings() {
            ModsManager.LoadConfigs();
            try {
                //加载原生设置
                if (Storage.FileExists(ModsManager.SettingPath)) {
                    using (Stream stream = Storage.OpenFile(ModsManager.SettingPath, OpenFileMode.Read)) {
                        XElement xElement = XmlUtils.LoadXmlFromStream(stream, null, true);
                        if (xElement.Elements("Configs").Any()) //往下适配低版本Settings.xml
                        {
                            ModsManager.LoadConfigsFromXml(xElement);
                        }
                        else {
                            ValuesDictionary valuesDictionary = new();
                            valuesDictionary.ApplyOverrides(xElement);
                            foreach (string name in valuesDictionary.Keys) {
                                try {
                                    PropertyInfo propertyInfo = (from pi in typeof(SettingsManager).GetRuntimeProperties()
                                        where pi.Name == name && pi.GetMethod != null && pi.GetMethod.IsStatic && pi.GetMethod.IsPublic && pi.SetMethod != null && pi.SetMethod.IsPublic
                                        select pi).FirstOrDefault();
                                    if (propertyInfo is not null) {
                                        object value = valuesDictionary.GetValue<object>(name);
                                        if (propertyInfo.PropertyType == typeof(ValuesDictionary)
                                            && value is ValuesDictionary vd2) {
                                            ValuesDictionary vd3 = propertyInfo.GetValue(null) as ValuesDictionary;
                                            vd3.ApplyOverrides(vd2);
                                        }
                                        else {
                                            propertyInfo.SetValue(null, value, null);
                                        }
                                    }
                                }
                                catch (Exception ex) {
                                    if (!LanguageControl.TryGet(out string str, fName, "3")) {
                                        str = "Setting \"{0}\" could not be loaded. Reason: {1}";
                                    }
                                    Log.Warning(string.Format(str, name, ex));
                                }
                            }
                        }
                    }
                    if (!LanguageControl.TryGet(out string info, fName, "4")) {
                        info = "Loaded settings.";
                    }
                    Log.Information(info);
                    return true;
                }
                return false;
            }
            catch (Exception e) {
                if (!LanguageControl.TryGet(out string str, fName, "5")) {
                    str = "Loading settings failed.";
                }
                ExceptionManager.ReportExceptionToUser(str, e);
                return false;
            }
        }

        public static void SaveSettings() {
            if (!m_saveLock.TryEnter(0)) {
                return;
            }
            try {
                try {
                    ModsManager.SaveConfigs();
                    ModSettingsManager.SaveModSettings();
                }
                catch (Exception) {
                    //ignore
                }
                ValuesDictionary settingsValuesDictionary = new();
                //原生设置
                XElement xElement = new("Settings");
                foreach (PropertyInfo item in from pi in typeof(SettingsManager).GetRuntimeProperties()
                    where pi.GetMethod.IsStatic && pi.GetMethod.IsPublic && pi.SetMethod.IsPublic
                    select pi) {
                    try {
                        object value = item.GetValue(null, null);
                        settingsValuesDictionary.SetValue(item.Name, value);
                    }
                    catch (Exception ex) {
                        if (!LanguageControl.TryGet(out string str, fName, "6")) {
                            str = "Setting \"{0}\" could not be saved. Reason: {1}";
                        }
                        Log.Warning(string.Format(str, item.Name, ex));
                    }
                }
                settingsValuesDictionary.Save(xElement);
                if (!Storage.DirectoryExists(ModsManager.DocPath)) {
                    Storage.CreateDirectory(ModsManager.DocPath);
                }
                //保存
                using (Stream stream = Storage.OpenFile(ModsManager.SettingPath, OpenFileMode.Create)) {
                    XmlUtils.SaveXmlToStream(xElement, stream, Encoding.UTF8, true);
                }
                if (!LanguageControl.TryGet(out string info, fName, "7")) {
                    info = "Saved settings.";
                }
                Log.Information(info);
            }
            catch (Exception e) {
                if (!LanguageControl.TryGet(out string str, fName, "8")) {
                    str = "Saving settings failed.";
                }
                ExceptionManager.ReportExceptionToUser(str, e);
            }
            finally {
                m_saveLock.Exit();
            }
        }
    }
}