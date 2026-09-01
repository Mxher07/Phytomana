#if ANDROID
using Android.Content;
using SC4Android;
#elif BROWSER
using System.Runtime.Versioning;
using Engine.Browser;
#else
using Engine.Input;
using System.Diagnostics;
#if WINDOWS
using ImeSharp;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
#endif
#endif
using System.Globalization;
using Engine;
using Engine.Graphics;
using Silk.NET.OpenGLES;

#if BROWSER
[assembly: SupportedOSPlatform("browser")]
#endif
namespace Game {
    public static class Program {
        public static double m_frameBeginTime;
        public static double m_cpuEndTime;
        public static List<Uri> m_urisToHandle = [];
#if WINDOWS
        public static Mutex m_mutex;
        public static bool m_mutexHandled;
#endif
        public static string SystemLanguage { get; set; }
        public static float LastFrameTime { get; set; }

        public static float LastCpuFrameTime { get; set; }

        public static Dictionary<string, string> StartupParameters = [];

        public static event Action<Uri> HandleUri;
#if ANDROID || BROWSER
        public static bool m_firstFramePrepared;
#endif

#if !ANDROID
        // ReSharper disable UnusedMember.Local
#if BROWSER
        //浏览器运行最小测试
        public static async Task Main2(string[] args) {
            Display.Initialize();
            BrowserInterop.Initialize(InputBridge.Initialize());
            Display.Resize();
            unsafe {
                Emscripten.RequestAnimationFrameLoop((delegate* unmanaged<double, nint, int>)&Frame, nint.Zero);
            }
        }

        public static int Counter;
        [System.Runtime.InteropServices.UnmanagedCallersOnly]
        public static int Frame(double time, nint userData)
        {
            InputBridge.BeforeFrame();
            Display.Clear(Color.White);
            PrimitivesRenderer2D primitivesRenderer2D = new PrimitivesRenderer2D();
            FlatBatch2D flatBatch2D = primitivesRenderer2D.FlatBatch();
            Point2 size = Display.BackbufferSize;
            flatBatch2D.QueueLine(new Vector2(Counter, 0f), new Vector2(size.X - Counter, size.Y), 0f, Color.Black);
            primitivesRenderer2D.Flush();
            if (++Counter >= size.X) {
                Counter = 0;
            }
            return 1;
        }

        public static async Task Main(string[] args) {
#else
        static void Main(string[] args) {
#endif
            // ReSharper restore UnusedMember.Local
#if WINDOWS
            if (args != null
                && args.Length > 0) {
                switch (args.Length) {
                    case 1: {
                        string path = args[0];
                        ExternalContentType type = ExternalContentManager.ExtensionToType(Storage.GetExtension(path));
                        if (ExternalContentManager.IsEntryTypeDownloadSupported(type)
                            && File.Exists(path)) {
                            using (FileStream fileStream = File.OpenRead(path)) {
                                string fileName = Storage.GetFileName(path);
                                try {
                                    ExternalContentManager.ImportExternalContentSync(fileStream, type, fileName);
                                    Window.MessageBox(IntPtr.Zero, $"Successfully imported {fileName}.\n导入 {fileName} 成功", "Success 成功", 0x40u);
                                }
                                catch (Exception e) {
                                    Window.MessageBox(IntPtr.Zero, $"Failed to import {fileName}, reason:\n导入 {fileName} 失败，原因：\n{e}", null, 0x10u);
                                }
                            }
                        }
                        return;
                    }
                    case 2:
                        switch (args[0]) {
                            case "--wait":
                                if (int.TryParse(args[1], out int pid)) {
                                    try {
                                        Process.GetProcessById(pid)?.WaitForExit();
                                    }
                                    catch {
                                        // ignored
                                    }
                                }
                                break;
                            default:
                                if (args[0].StartsWith('-') && !args[1].StartsWith('-')) {
                                    StartupParameters.TryAdd(args[0].TrimStart('-'), args[1]);
                                }
                                break;
                        }
                        break;
                    default:
                        string key = null;
                        string value = null;
                        foreach (string arg in args) {
                            if (arg.StartsWith('-')) {
                                if (key != null) {
                                    StartupParameters.TryAdd(key, value ?? string.Empty);
                                }
                                key = arg.TrimStart('-');
                                if (key.Length == 0) {
                                    key = null;
                                }
                            }
                            else if (key != null) {
                                value = arg;
                            }
                        }
                        if (key != null) {
                            StartupParameters.TryAdd(key, value ?? string.Empty);
                        }
                        break;
                }
            }
            string mutexName;
            using (SHA256 sha256 = SHA256.Create()) {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetEntryAssembly()?.Location ?? "SurvivalcraftApi"));
                mutexName = $"Global\\{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}";
            }
            m_mutex = new Mutex(true, mutexName, out m_mutexHandled);
            if (!m_mutexHandled) {

                string str =
                    "This game is already running! If you cannot find the window, please stop it from the Task Manager, and check the log file in Bugs directory.\n游戏已经在运行！如果找不到游戏窗口，请从任务管理器终止它，并检查 Bugs 目录中的日志文件。";
                Window.MessageBox(IntPtr.Zero, str, null, 0x10u);
                return;
            }
#endif

#if WINDOWS
            Window.Created += () => {
                InputMethod.Initialize(Process.GetCurrentProcess().MainWindowHandle);
                InputMethod.Enabled = false;
            };
#endif
            EntryPoint();
#if WINDOWS
            m_mutex.ReleaseMutex();
            m_mutex.Dispose();
#endif
            /*AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
                //在程序目录下面寻找dll,解决部分设备找不到目录下程序集的问题
                string location = new FileInfo(typeof(Program).Assembly.Location).Directory!.FullName;
                return Assembly.LoadFrom(Path.Combine(location, e.Name));
            };*/

            //RootCommand rootCommand =
            //[
            //    new Option<string>(["-m", "--mod-import"], ""),
            //    new Option<string>(["-l", "--language"], "")
            //];
        }
#endif // !ANDROID

        public static void DisableVrCameras() {
            if (GameManager.Project == null) return;
            SubsystemGameWidgets subsystemGameWidgets = GameManager.Project.FindSubsystem<SubsystemGameWidgets>(true);
            foreach (GameWidget gameWidget in subsystemGameWidgets.GameWidgets) {
                if (gameWidget.ActiveCamera is BasePerspectiveCamera camera) {
                    camera.Eye = null;
                }
            }
        }

        [STAThread]
        public static void EntryPoint() {
#if BROWSER
            SystemLanguage = BrowserInterop.GetLanguage();
#else
            SystemLanguage = CultureInfo.CurrentUICulture.Name;
#endif
#if ANDROID
            if (MainActivity.m_startupParameters != null) {
                foreach ((string key, string value) in MainActivity.m_startupParameters) {
                    StartupParameters.Add(key, value);
                }
            }
#endif
            if (string.IsNullOrEmpty(SystemLanguage)) {
                SystemLanguage = RegionInfo.CurrentRegion.DisplayName != "United States" ? "zh-CN" : "en-US";
            }
            //预加载
            Storage.Initialize();
            VersionsManager.Initialize();
            Window.HandleUri += HandleUriHandler;
            Window.Deactivated += DeactivatedHandler;
            Window.Frame += FrameHandler;
            Window.ToRestart += ToRestartHandler;
            Window.FileDropped += FileDropHandler;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            string title = $"Survivalcraft {ModsManager.ShortGameVersion} - API {ModsManager.APIVersionString}";
            Log.AddLogSink(new GameLogSink());
#if DEBUG
            title = $"[DEBUG]{title}";
#endif
            Window.UnhandledException += delegate (UnhandledExceptionInfo e) {
                ExceptionManager.ReportExceptionToUser("Unhandled exception.", e.Exception);
                e.IsHandled = true;
            };
            Window.Run(0, 0, WindowMode.Resizable, title);
        }

        public static void HandleUriHandler(Uri uri) {
            m_urisToHandle.Add(uri);
        }

        public static void DeactivatedHandler() {
            GC.Collect();
        }

        public static void FrameHandler() {
            if (Time.FrameIndex < 0) {
                Display.Clear(Color.White, 1f);
            }
            else if (Time.FrameIndex == 0) {
                Display.Clear(Color.White, 1f);
                Initialize();
            }
            else {
                Run();
            }
        }

        public static void Initialize() {
            Log.Information(
                $"Survivalcraft starting up at {DateTime.Now}, GameVersion={VersionsManager.Version}, BuildConfiguration={VersionsManager.BuildConfiguration}, Platform={VersionsManager.PlatformString}, Storage.AvailableFreeSpace={Storage.FreeSpace / 1024 / 1024}MB, ApproximateScreenDpi={ScreenResolutionManager.ApproximateScreenDpi:0.0}, ApproxScreenInches={ScreenResolutionManager.ApproximateScreenInches:0.0}, ScreenResolution={Window.Size}, ProcessorsCount={Environment.ProcessorCount}, APIVersion={ModsManager.APIVersionString}, 64bit={Environment.Is64BitProcess}"
            );
            try {
                SettingsManager.Initialize();
                ExternalContentManager.Initialize();
                MusicManager.Initialize();
                ScreensManager.Initialize();
                APIUpdateManager.Initialize();
#if WINDOWS
                VrManager.SetBackend(new Engine.VR.WindowsOpenXrVrBackend());
#elif ANDROID
                VrManager.SetBackend(new Engine.VR.AndroidOpenXrVrBackend());
#endif
                VrManager.Initialize();
                Log.Information("Program Initialize Success");
            }
            catch (Exception e) {
                Log.Error(e.ToString());
            }
        }

        public static void Run() {
            LastFrameTime = (float)(Time.RealTime - m_frameBeginTime);
            LastCpuFrameTime = (float)(m_cpuEndTime - m_frameBeginTime);
            m_frameBeginTime = Time.RealTime;
            bool vrFrame = VrManager.IsVrStarted && VrManager.BeginFrame();
#if !MOBILE && !BROWSER
            if (Keyboard.IsKeyDownOnce(Key.F11)) {
                SettingsManager.WindowMode = SettingsManager.WindowMode == WindowMode.Fullscreen ? WindowMode.Resizable : WindowMode.Fullscreen;
                Mouse.m_lastMousePosition = null;
            }
#endif
            try {
                if (ExceptionManager.Error == null) {
                    foreach (Uri obj in m_urisToHandle) {
                        HandleUri?.Invoke(obj);
                    }
                    m_urisToHandle.Clear();
                    PerformanceManager.Update();
                    MotdManager.Update();
                    MusicManager.Update();
                    ScreensManager.Update();
                    DialogsManager.Update();
#if !IOS && !BROWSER
                    JsInterface.Update();
#endif
                }
                else {
                    ExceptionManager.UpdateExceptionScreen();
                }
            }
            catch (Exception e) {
                //ModsManager.AddException(e);
                Log.Error("Game Running Error!");
                Log.Error(e);
                try {
                    ScreensManager.SwitchScreen("MainMenu");
                    ViewGameLogDialog dialog = new();
                    dialog.SetErrorHead(9, 10);
                    DialogsManager.ShowDialog(null, dialog);
                    GameManager.DisposeProject();
                }
                catch (Exception e3) {
                    Log.Error(e3);
                }
            }
            m_cpuEndTime = Time.RealTime;
            try {
                Display.RenderTarget = null;
                if (ExceptionManager.Error == null) {
                    if (LoadingScreen.m_isContentLoaded) {
                        ScreensManager.Draw();
                        PerformanceManager.Draw();
                        ScreenCaptureManager.Run();
                    }
                    else {
                        Display.Clear(Color.White, 1f);
                    }
                }
                else {
                    ExceptionManager.DrawExceptionScreen();
                }
            }
            catch (Exception e2) {
                if (GameManager.Project != null) {
                    GameManager.DisposeProject();
                }
                Log.Error(e2);
                ExceptionManager.ReportExceptionToUser(null, e2);
                ScreensManager.SwitchScreen("MainMenu");
            }
            finally {
#if ANDROID
                if (LoadingScreen.m_isContentLoaded) {
                    m_firstFramePrepared = true;
                }
#elif BROWSER
                if (!m_firstFramePrepared && LoadingScreen.m_isContentLoaded) {
                    m_firstFramePrepared = true;
                    BrowserInterop.FirstFramePrepared();
                }
#endif
                if (vrFrame) {
                    VrManager.EndFrame();
                    VrManager.Update();
                }
            }
        }

        public static void ToRestartHandler() {
#if ANDROID
#pragma warning disable CA1416
            Intent intent = new Intent(Window.Activity, Window.Activity.Class);
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            Window.Activity.ApplicationContext.StartActivity(intent);
            Java.Lang.JavaSystem.Exit(0);
#elif !BROWSER
            Process current = Process.GetCurrentProcess();
            Process.Start(new ProcessStartInfo {
                FileName = current.MainModule!.FileName!,
                Arguments = $"--wait {current.Id}",
                UseShellExecute = false
            });
#endif
        }

        public static void FileDropHandler(List<(Stream stream, string fileName)> files) {
            if (ScreensManager.CurrentScreen is LoadingScreen) {
                return;
            }
            _ = ExternalContentManager.ImportExternalContentsAsync(files, true);
        }
    }
}