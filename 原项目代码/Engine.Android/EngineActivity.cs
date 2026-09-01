#pragma warning disable CA1416
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.View;
using Engine.Input;
using Silk.NET.Windowing.Sdl.Android;
using Environment = System.Environment;
using Insets = AndroidX.Core.Graphics.Insets;
using Path = System.IO.Path;
using Stream = Android.Media.Stream;
using Uri = Android.Net.Uri;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace Engine {
    public class EngineActivity : SilkActivity {
        internal static EngineActivity m_activity;

        public event Action Paused;

        public event Action Resumed;

        public event Action Destroyed;

        public event Action<Intent> NewIntent;

        public event Func<KeyEvent, bool> OnDispatchKeyEvent;

        public static string BasePath = RunPath.AndroidFilePath;
        public static string ConfigPath = RunPath.AndroidFilePath;

        const int PickFileRequestCode = 1001;
        TaskCompletionSource<(System.IO.Stream Stream, string FileName)> filePickTcs;

        AudioManager AudioManager {
            get {
                if (field == null) {
                    field = GetAudioManager();
                }
                return field;
            }
        }

        public EngineActivity() => m_activity = this;

        protected override void OnCreate(Bundle savedInstanceState) {
            RequestWindowFeature(WindowFeatures.NoTitle);
            base.OnCreate(savedInstanceState);
            Window?.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
            EnableImmersiveMode();
            VolumeControlStream = Stream.Music;
            RequestedOrientation = ScreenOrientation.SensorLandscape;
            if (Build.VERSION.SdkInt >= (BuildVersionCodes)28 && Window != null) {
                ViewCompat.SetOnApplyWindowInsetsListener(Window.DecorView, new ApplyWindowInsetsListener());
            }
        }

        public void Vibrate(long ms) {
            if (Build.VERSION.SdkInt >= (BuildVersionCodes)26) {
                (GetSystemService("vibrator") as Vibrator)?.Vibrate(VibrationEffect.CreateOneShot(ms, VibrationEffect.DefaultAmplitude));
            }
        }

        public void OpenLink(string link) {
            StartActivity(new Intent(Intent.ActionView, Uri.Parse(link)));
        }

        public void OpenFile(string path, string chooserTitle = null, string mimeType = null) {
            string processedAndroidFilePath = Storage.ProcessPath(RunPath.AndroidFilePath, false, false);
            if (!path.StartsWith(processedAndroidFilePath)) {
                throw new ArgumentException($"Open {path} failed, because it is not in {processedAndroidFilePath}.");
            }
            Java.IO.File file = new(path);
            if (!file.Exists()) {
                throw new FileNotFoundException($"Open {path} failed, because it is not exists.");
            }
            Uri uri = Build.VERSION.SdkInt >= BuildVersionCodes.N
                ? AndroidX.Core.Content.FileProvider.GetUriForFile(this, $"{PackageName}.fileprovider", file)
                : Uri.FromFile(file);
            Intent intent = new(Intent.ActionView);
            mimeType ??= Android.Webkit.MimeTypeMap.Singleton?.GetMimeTypeFromExtension(Storage.GetExtension(path));
            if (mimeType == null) {
                intent.SetData(uri);
            }
            else {
                intent.SetDataAndType(uri, mimeType);
            }
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
            if (Application.Context.PackageManager?.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly).Any() ?? false) {
                StartActivity(Intent.CreateChooser(intent, chooserTitle ?? Storage.GetFileName(path)));
            }
            else {
                throw new InvalidOperationException($"Open {path} failed, because no app can open it.");
            }
        }

        public void ShareFile(string path, string chooserTitle = null, string mimeType = null) {
            string processedAndroidFilePath = Storage.ProcessPath(RunPath.AndroidFilePath, false, false);
            if (!path.StartsWith(processedAndroidFilePath)) {
                throw new ArgumentException($"Share {path} failed, because it is not in {processedAndroidFilePath}.");
            }
            Java.IO.File file = new(path);
            if (!file.Exists()) {
                throw new FileNotFoundException($"Share {path} failed, because it does not exist.");
            }
            Uri uri = Build.VERSION.SdkInt >= BuildVersionCodes.N
                ? AndroidX.Core.Content.FileProvider.GetUriForFile(this, $"{PackageName}.fileprovider", file)
                : Uri.FromFile(file);
            Intent intent = new(Intent.ActionSend);
            mimeType ??= Android.Webkit.MimeTypeMap.Singleton?.GetMimeTypeFromExtension(Storage.GetExtension(path)) ?? "*/*";
            intent.SetType(mimeType);
            intent.PutExtra(Intent.ExtraStream, uri);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
            StartActivity(Intent.CreateChooser(intent, chooserTitle ?? Storage.GetFileName(path)));
        }

        public Task<(System.IO.Stream Stream, string FileName)> ChooseFileAsync(string chooserTitle = null) {
            Intent intent = new(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            filePickTcs = new TaskCompletionSource<(System.IO.Stream, string)>();
            StartActivityForResult(string.IsNullOrEmpty(chooserTitle) ? intent : Intent.CreateChooser(intent, chooserTitle), PickFileRequestCode);
            return filePickTcs.Task;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data) {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PickFileRequestCode) {
                if (resultCode == Result.Ok
                    && data != null) {
                    try {
                        System.IO.Stream stream = GetStreamFromUri(data.Data, out string fileName);
                        filePickTcs?.TrySetResult((stream, fileName));
                    }
                    catch (Exception ex) {
                        filePickTcs?.TrySetException(ex);
                    }
                }
                else {
                    filePickTcs?.TrySetResult((null, null));
                }
            }
        }

        protected override void OnPause() {
            base.OnPause();
            Paused?.Invoke();
        }

        protected override void OnResume() {
            base.OnResume();
            Resumed?.Invoke();
        }

        protected override void OnNewIntent(Intent intent) {
            base.OnNewIntent(intent);
            NewIntent?.Invoke(intent);
        }

        protected override void OnRun() { }

        protected override void OnDestroy() {
            try {
                base.OnDestroy();
                Destroyed?.Invoke();
            }
            finally {
                Thread.Sleep(250);
                Environment.Exit(0);
            }
        }

        public override bool DispatchTouchEvent(MotionEvent e) {
            if (e == null) {
                return true;
            }
            if ((e.Source & InputSourceType.Touchscreen) == InputSourceType.Touchscreen) {
                Touch.HandleTouchEvent(e);
            }
            else if ((e.Source & InputSourceType.Mouse) == InputSourceType.Mouse
                || (e.Source & InputSourceType.ClassPointer) == InputSourceType.ClassPointer
                || (e.Source & InputSourceType.MouseRelative) == InputSourceType.MouseRelative) {
                Mouse.HandleMotionEvent(e);
            }
            return true;
        }

        public override bool DispatchKeyEvent(KeyEvent e) {
            if (e == null) {
                return true;
            }
            /*Debug.WriteLine(
                $"[DispatchKeyEvent]action:{e.Action} keyCode:{e.KeyCode} unicodeChar:{e.UnicodeChar} flags:{e.Flags} metaState:{e.MetaState} source:{e.Source} deviceId:{e.DeviceId}"
            );*/
            bool handled = false;
            Delegate[] invocationList = OnDispatchKeyEvent?.GetInvocationList();
            if (invocationList != null) {
                foreach (Delegate invocation in invocationList) {
                    handled |= (bool)invocation.DynamicInvoke(e)!;
                }
            }
            if (!handled) {
                _ = e.Action switch {
                    KeyEventActions.Down => OnKeyDown(e.KeyCode, e),
                    KeyEventActions.Up => OnKeyUp(e.KeyCode, e),
                    _ => false
                };
            }
            return true;
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e) {
            switch (keyCode) {
                case Keycode.VolumeUp:
                    AudioManager?.AdjustStreamVolume(Stream.Music, Adjust.Raise, VolumeNotificationFlags.ShowUi);
                    EnableImmersiveMode();
                    break;
                case Keycode.VolumeDown:
                    AudioManager?.AdjustStreamVolume(Stream.Music, Adjust.Lower, VolumeNotificationFlags.ShowUi);
                    EnableImmersiveMode();
                    break;
            }
            if (e == null) {
                return true;
            }
            if ((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad
                || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick) {
                GamePad.HandleKeyEvent(e);
            }
            else {
                Keyboard.HandleKeyEvent(e);
            }
            return true;
        }

        AudioManager GetAudioManager() => Build.VERSION.SdkInt >= (BuildVersionCodes)21 ? GetSystemService("audio") as AudioManager : null;

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e) {
            if (e == null) {
                return true;
            }
            if ((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad
                || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick) {
                GamePad.HandleKeyEvent(e);
            }
            else {
                Keyboard.HandleKeyEvent(e);
            }
            return true;
        }

        public override bool DispatchGenericMotionEvent(MotionEvent e) {
            if (e == null) {
                return true;
            }
            //Debug.WriteLine($"[OnGenericMotionEvent]source:{e.Source} action:{e.Action}");
            if (((e.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad || (e.Source & InputSourceType.Joystick) == InputSourceType.Joystick)
                && e.Action == MotionEventActions.Move) {
                GamePad.HandleMotionEvent(e);
            }
            if ((e.Source & InputSourceType.Mouse) == InputSourceType.Mouse
                || (e.Source & InputSourceType.ClassPointer) == InputSourceType.ClassPointer
                || (e.Source & InputSourceType.MouseRelative) == InputSourceType.MouseRelative) {
                Mouse.HandleMotionEvent(e);
            }
            return true;
        }

        public void EnableImmersiveMode() {
            if (Window != null) {
                switch (Build.VERSION.SdkInt) {
                    case >= (BuildVersionCodes)30:
                        IWindowInsetsController insetsController = Window.InsetsController;
                        if (insetsController != null) {
                            insetsController.Hide(WindowInsets.Type.SystemBars());
                            insetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                        }
                        break;
#pragma warning disable CA1422
                    case > (BuildVersionCodes)19:
                        Window.DecorView.SystemUiFlags = SystemUiFlags.Fullscreen
                            | SystemUiFlags.HideNavigation
                            | SystemUiFlags.Immersive
                            | SystemUiFlags.ImmersiveSticky; break;
#pragma warning restore CA1422
                }
            }
        }

        public void GetGlEsVersion(out int major, out int minor) {
            try {
                int reqGlEsVersion = ((ActivityManager)GetSystemService(ActivityService))?.DeviceConfigurationInfo?.ReqGlEsVersion ?? 0x20000;
                major = reqGlEsVersion >> 16;
                minor = reqGlEsVersion & 0xFFFF;
            }
            catch {
                major = 2;
                minor = 0;
            }
        }

        public int GetScreenRefreshRate() => (int)Display.RefreshRate;

        public System.IO.Stream GetStreamFromUri(Uri uri, out string fileName) {
            System.IO.Stream stream = null;
            fileName = null;
            try {
                using (ICursor cursor = ContentResolver?.Query(uri, null, null, null, null)) {
                    if (cursor != null
                        && cursor.MoveToFirst()) {
                        int nameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
                        if (nameIndex >= 0) {
                            fileName = cursor.GetString(nameIndex);
                        }
                    }
                }
                stream = ContentResolver?.OpenInputStream(uri);
            }
            catch {
                // ignored
            }
            if (string.IsNullOrEmpty(fileName)) {
                fileName = Path.GetFileName(uri.Path);
            }
            return stream;
        }

        public class ApplyWindowInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener {
            public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets) {
                IList<Rect> boundingRects = insets?.DisplayCutout?.BoundingRects;
                if (boundingRects == null
                    || boundingRects.Count == 0) {
                    return WindowInsetsCompat.Consumed;
                }
                bool hasWideNotch = false;
                if (boundingRects.Count >= 2) {
                    hasWideNotch = true;
                }
                else {
                    Rect rect = boundingRects[0];
                    if (Math.Max(rect.Width(), rect.Height()) > 200) {
                        hasWideNotch = true;
                    }
                }
                Insets cutoutInsets = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
                if (cutoutInsets != null) {
                    Engine.Window.DisplayCutoutInsetsChangedHandler(
                        new Vector4(cutoutInsets.Left, cutoutInsets.Top, cutoutInsets.Right, cutoutInsets.Bottom),
                        hasWideNotch
                    );
                }
                return WindowInsetsCompat.Consumed;
            }
        }
    }
}