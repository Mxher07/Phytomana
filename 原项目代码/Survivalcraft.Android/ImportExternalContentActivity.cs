using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Views;
using Android.Widget;
using Engine;
using Game;
using Resource = _Microsoft.Android.Resource.Designer.Resource;
using Uri = Android.Net.Uri;

#pragma warning disable CA1416
namespace SC4Android {
    [Activity(
         Label = "@string/ImportExternalContentActivityLabel",
         LaunchMode = LaunchMode.SingleTop,
         Icon = "@mipmap/icon",
         Theme = "@style/ImportExternalContentDialog",
         Exported = true
     ),
     IntentFilter(
         ["android.intent.action.VIEW"],
         DataSchemes = ["file", "content"],
         DataMimeTypes = ["*/*", "image/png", "image/webp"],
         DataPathPatterns = [
             @".*\.scworld",
             @".*\.scbtex",
             @".*\.scskin",
             @".*\.scfpack",
             @".*\.scmod",
             @".*\.png",
             @".*\.webp",
             @".*\.astc",
             @".*\.astcsrgb"
         ],
         Categories = ["android.intent.category.DEFAULT", "android.intent.category.BROWSABLE"]
     ), IntentFilter(["android.intent.action.SEND"], DataMimeType = "*/*", Categories = ["android.intent.category.DEFAULT"])]
    public class ImportExternalContentActivity : Activity {
        bool permissionGranted;

        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);
            Intent intent = Intent;
            if (intent == null) {
                Toast.MakeText(this, Resources?.GetString(Resource.String.FileNotFound), ToastLength.Short)?.Show();
                FinishAndRemoveTask();
                return;
            }
            Uri uri = null;
            if (intent.Action == Intent.ActionView) {
                uri = intent.Data;
            }
            else if (intent.Action == Intent.ActionSend) {
                if (Build.VERSION.SdkInt >= (BuildVersionCodes)33) {
                    uri = intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(Uri))) as Uri;
                }
                else {
#pragma warning disable CA1422
                    uri = intent.GetParcelableExtra(Intent.ExtraStream) as Uri;
#pragma warning restore CA1422
                }
            }
            if (uri == null) {
                Toast.MakeText(this, Resources?.GetString(Resource.String.FileNotFound), ToastLength.Short)?.Show();
                FinishAndRemoveTask();
                return;
            }
            Stream fileStream = GetStreamAndInfosFromUri(uri, out string fileName, out long fileSize);
            if (fileSize == 0
                || fileStream == null) {
                Toast.MakeText(this, Resources?.GetString(Resource.String.FileNotFound), ToastLength.Short)?.Show();
                FinishAndRemoveTask();
                return;
            }
            string extension = Storage.GetExtension(fileName)?.ToLowerInvariant();
            ExternalContentType type = ExternalContentManager.ExtensionToType(extension);
            if (ExternalContentManager.IsEntryTypeDownloadSupported(type)) {
                if (MainActivity.CheckAndRequestPermission(this)) {
                    permissionGranted = true;
                }
                else {
                    while (true) {
                        Thread.Sleep(100);
                        if (permissionGranted) {
                            break;
                        }
                        permissionGranted = MainActivity.IsPermissionGranted(this);
                        if (permissionGranted) {
                            break;
                        }
                    }
                }
                new AlertDialog.Builder(this).SetTitle(Resources?.GetString(Resource.String.Import))
                    ?.SetMessage(string.Format(Resources?.GetString(Resource.String.InsureImporting)!, fileName))
                    ?.SetPositiveButton(
                        Resources?.GetString(Resource.String.Yes)!,
                        async void (_, _) => await ImportFileAsync(fileName, fileSize, fileStream)
                    )
                    ?.SetNegativeButton(Resources?.GetString(Resource.String.No)!, (_, _) => FinishAndRemoveTask())
                    ?.Show();
            }
            else {
                new AlertDialog.Builder(this).SetTitle(Resources?.GetString(Resource.String.NotSupportedType))
                    ?.SetMessage(string.Format(Resources?.GetString(Resource.String.FileTypeIsNotSupported)!, extension))
                    ?.SetPositiveButton(Resources?.GetString(Resource.String.Ok)!, (_, _) => FinishAndRemoveTask())
                    ?.SetOnCancelListener(new DialogInterfaceOnCancelListener(FinishAndRemoveTask))
                    ?.Show();
            }
        }

        public async Task ImportFileAsync(string name, long size, Stream stream) {
            AlertDialog importingDialog = null;
            try {
                ExternalContentType type = ExternalContentManager.ExtensionToType(Storage.GetExtension(name)?.ToLowerInvariant());
                if (size > 10L * 1024 * 1024) {
                    RunOnUiThread(() => {
                            LinearLayout layout = new(this) { Orientation = Orientation.Vertical };
                            int margin = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 24, Resources?.DisplayMetrics);
                            layout.SetPadding(0, margin, 0, margin);
                            layout.SetGravity(GravityFlags.Center);
                            ProgressBar progressBar = new(this) { Indeterminate = true };
                            layout.AddView(progressBar);
                            importingDialog = new AlertDialog.Builder(this).SetTitle(Resources?.GetString(Resource.String.Importing))
                                ?.SetView(layout)
                                ?.SetCancelable(false)
                                ?.Show();
                        }
                    );
                }
                await Task.Run(() => {
                        switch (type) {
                            case ExternalContentType.World: WorldsManager.ImportWorld(stream); break;
                            case ExternalContentType.BlocksTexture: BlocksTexturesManager.ImportBlocksTexture(name, stream); break;
                            case ExternalContentType.CharacterSkin: CharacterSkinsManager.ImportCharacterSkin(name, stream); break;
                            case ExternalContentType.FurniturePack: FurniturePacksManager.ImportFurniturePack(name, stream); break;
                            case ExternalContentType.Mod: ModsManager.ImportMod(name, stream ); break;
                        }
                    }
                );
                stream.Close();
                await stream.DisposeAsync();
                RunOnUiThread(() => {
                        importingDialog?.Dismiss();
                        AlertDialog.Builder builder = new AlertDialog.Builder(this)
                            .SetTitle(Resources?.GetString(Resource.String.ImportedSuccessfully))
                            ?.SetPositiveButton(
                                Resources?.GetString(Resource.String.Yes)!,
                                (_, _) => {
                                    Intent intent = new(this, typeof(MainActivity));
                                    intent.SetFlags(ActivityFlags.ReorderToFront);
                                    StartActivity(intent);
                                    FinishAndRemoveTask();
                                }
                            )
                            ?.SetNegativeButton(Resources?.GetString(Resource.String.No)!, (_, _) => FinishAndRemoveTask());
                        builder?.SetMessage(Resources?.GetString(Resource.String.InsureLaunchingGame));
                        builder?.Show();
                    }
                );
            }
            catch (Exception e) {
                RunOnUiThread(() => {
                        importingDialog?.Dismiss();
                        new AlertDialog.Builder(this).SetTitle(Resources?.GetString(Resource.String.FailedToImport))
                            ?.SetMessage(e.Message)
                            ?.SetPositiveButton(Resources?.GetString(Resource.String.Ok)!, (_, _) => FinishAndRemoveTask())
                            ?.SetOnCancelListener(new DialogInterfaceOnCancelListener(FinishAndRemoveTask))
                            ?.Show();
                    }
                );
            }
        }

        public Stream GetStreamAndInfosFromUri(Uri uri, out string name, out long size) {
            name = null;
            size = 0L;
            Stream stream = null;
            try {
                using (ICursor cursor = ContentResolver?.Query(uri, null, null, null, null)) {
                    if (cursor != null
                        && cursor.MoveToFirst()) {
                        int nameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
                        if (nameIndex >= 0) {
                            name = cursor.GetString(nameIndex);
                        }
                        int sizeIndex = cursor.GetColumnIndex(IOpenableColumns.Size);
                        if (sizeIndex >= 0) {
                            size = cursor.GetLong(sizeIndex);
                        }
                    }
                }
                stream = ContentResolver?.OpenInputStream(uri);
            }
            catch {
                // ignored
            }
            if (string.IsNullOrEmpty(name)) {
                name = Path.GetFileName(uri.Path);
            }
            return stream;
        }

        protected override void OnResume() {
            base.OnResume();
            if (!permissionGranted) {
                permissionGranted = MainActivity.CheckAndRequestPermission(this);
            }
        }

        public class DialogInterfaceOnCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener {
            readonly Action _onCancel;
            public DialogInterfaceOnCancelListener(Action onCancel) => _onCancel = onCancel;
            public void OnCancel(IDialogInterface dialog) => _onCancel?.Invoke();
        }
    }
}