using Android;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Android.Window;
using Engine;
using Game;
using Environment = Android.OS.Environment;
using Permission = Android.Content.PM.Permission;
using Resource = _Microsoft.Android.Resource.Designer.Resource;
using Uri = Android.Net.Uri;

#pragma warning disable CA1416
namespace SC4Android {
    [Activity(
         Label = "@string/ShortTitle",
         LaunchMode = LaunchMode.SingleTask,
         Icon = "@mipmap/icon",
         Theme = "@style/MainTheme",
         MainLauncher = true,
         ConfigurationChanges =
             ConfigChanges.ScreenSize
             | ConfigChanges.Orientation
             | ConfigChanges.UiMode
             | ConfigChanges.ScreenLayout
             | ConfigChanges.SmallestScreenSize
             | ConfigChanges.Keyboard
             | ConfigChanges.KeyboardHidden,
         ScreenOrientation = ScreenOrientation.Landscape,
         Exported = true
     ),
     IntentFilter(
         ["android.intent.action.VIEW"],
         Categories = ["android.intent.category.DEFAULT", "android.intent.category.BROWSABLE"]
     ),
     IntentFilter(
         ["android.intent.action.MAIN"],
         Categories = ["android.intent.category.LAUNCHER", "org.khronos.openxr.intent.category.IMMERSIVE_HMD"]
     )]
    public class MainActivity : EngineActivity {
        public static bool GraterThanAndroid11 { get; } = Build.VERSION.SdkInt >= BuildVersionCodes.R;
        public static bool GraterThanAndroid6 { get; } = Build.VERSION.SdkInt >= BuildVersionCodes.M;

        public static Dictionary<string, string> m_startupParameters;

        public static bool CheckAndRequestPermission(Activity activity) {
            bool arePermissionsGranted = true;
            if (GraterThanAndroid11) {
                //当版本大于安卓11时
                if (!Environment.IsExternalStorageManager) {
                    arePermissionsGranted = false;
                    activity.RunOnUiThread(() => Toast.MakeText(activity, activity.Resources?.GetString(Resource.String.NeedPermission), ToastLength.Short)?.Show());
                    activity.StartActivity(new Intent(Settings.ActionManageAppAllFilesAccessPermission, Uri.Parse($"package:{activity.PackageName}")));
                }
                return arePermissionsGranted;
            }
            if (GraterThanAndroid6) {
                //当版本大于安卓6
                List<string> permissionList = [];
                Permission readPermissionStatus = activity.CheckSelfPermission(Manifest.Permission.ReadExternalStorage);
                if (readPermissionStatus != Permission.Granted) {
                    arePermissionsGranted = false;
                    permissionList.Add(Manifest.Permission.ReadExternalStorage);
                }
                Permission writePermissionStatus = activity.CheckSelfPermission(Manifest.Permission.WriteExternalStorage);
                if (writePermissionStatus != Permission.Granted) {
                    arePermissionsGranted = false;
                    permissionList.Add(Manifest.Permission.WriteExternalStorage);
                }
                if (permissionList.Count > 0) {
                    activity.RunOnUiThread(() => Toast.MakeText(activity, activity.Resources?.GetString(Resource.String.NeedPermission), ToastLength.Short)?.Show());
                    activity.RequestPermissions(permissionList.ToArray(), 1);
                }
            }
            return arePermissionsGranted;
        }

        public static bool IsPermissionGranted(Activity activity) {
            if (GraterThanAndroid11) {
                return Environment.IsExternalStorageManager;
            }
            if (GraterThanAndroid6) {
                return activity.CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Granted
                    && activity.CheckSelfPermission(Manifest.Permission.WriteExternalStorage) == Permission.Granted;
            }
            return true;
        }

        protected override void OnRun() {
            base.OnRun();
            if (CheckAndRequestPermission(this)) {
                RunRequired = true;
            }
            else {
                while (true) {
                    Thread.Sleep(100);
                    if (RunRequired) {
                        break;
                    }
                    RunRequired = IsPermissionGranted(this);
                    if (RunRequired) {
                        break;
                    }
                }
            }
            Program.EntryPoint();
        }

        static bool RunRequired { get; set; }

        bool m_isPaused;

        protected override void OnPause() {
            base.OnPause();
            m_isPaused = true;
        }

        protected override void OnResume() {
            base.OnResume();
            if (m_isPaused && !RunRequired) {
                m_isPaused = false;
                RunRequired = CheckAndRequestPermission(this);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);
            Window?.DecorView.ViewTreeObserver?.AddOnPreDrawListener(new ViewTreeObserverListener());
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) {
                SplashScreen.SetOnExitAnimationListener(new SplashScreenOnExitAnimationListener());
            }
            ICollection<string> keySet = Intent?.Extras?.KeySet();
            if (keySet != null && keySet.Count > 0) {
                m_startupParameters = new Dictionary<string, string>(keySet.Count);
                foreach (string key in keySet) {
                    string value = Intent?.Extras?.GetString(key);
                    if (value != null) {
                        m_startupParameters.Add(key, value);
                    }
                }
            }
        }

        public class ViewTreeObserverListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener {
            public bool OnPreDraw() => Program.m_firstFramePrepared;
        }

        public class SplashScreenOnExitAnimationListener : Java.Lang.Object, ISplashScreenOnExitAnimationListener {
            public void OnSplashScreenExit(SplashScreenView view) {
                ObjectAnimator slideUp = ObjectAnimator.OfFloat(view, "alpha", 1f, 0f);
                if (slideUp == null) {
                    return;
                }
                slideUp.SetInterpolator(new AnticipateInterpolator());
                slideUp.SetDuration(800L);
                slideUp.AnimationEnd += (_, _) => { view.Remove(); };
                slideUp.Start();
            }
        }
    }
}