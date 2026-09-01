#if WINDOWS
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine;
using Microsoft.Win32;

#pragma warning disable CA1416
#endif

namespace Game {
    public static class FileAssociationManager {
        public static string ProgIdBase = $"SurvivalcraftApi_{ModsManager.ShortAPIVersionString}";
        public static string VersionedExeName = $"Survivalcraft_{ModsManager.ShortAPIVersionString}.exe";
        public static string[] SupportedExtensions = [".scworld", ".scbtex", ".scskin", ".scfpack", ".scmod" /*, ".scmodList"*/];
        public const string fName = "FileAssociationManager";

        public static void Initialize() {
#if WINDOWS
            if (SettingsManager.FileAssociationEnabled) {
                Task.Run(() => {
                        if (!IsRegistered()) {
                            Register();
                        }
                    }
                );
            }
#endif
        }

        public static bool Register() {
#if WINDOWS
            try {
                string appPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(appPath)) {
                    Log.Error(LanguageControl.Get(fName, "1"));
                    return false;
                }
                string friendlyAppName =
                    $"{LanguageControl.Get("Usual", "gameName")} {ModsManager.ShortGameVersion} - API {ModsManager.APIVersionString}";
                string command = $"\"{appPath}\" \"%1\"";
                foreach (string extension in SupportedExtensions) {
                    using (RegistryKey progKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgIdBase}{extension}")) {
                        if (progKey == null) {
                            Log.Error(LanguageControl.Get(fName, "2"));
                            return false;
                        }
                        progKey.SetValue("", LanguageControl.Get(out _, LanguageControl.englishJsonNode, fName, "ExtensionDescription", extension));
                        progKey.SetValue("FriendlyAppName", friendlyAppName);
                        progKey.SetValue("FriendlyTypeName", LanguageControl.Get(fName, "ExtensionDescription", extension));
                        using (RegistryKey iconKey = progKey.CreateSubKey("DefaultIcon")) {
                            iconKey?.SetValue("", $"{appPath},0");
                        }
                        using (RegistryKey commandKey = progKey.CreateSubKey(@"shell\open\command")) {
                            if (commandKey == null) {
                                Log.Error(LanguageControl.Get(fName, "3"));
                                return false;
                            }
                            commandKey.SetValue("", command);
                        }
                    }
                    using (RegistryKey openWithKey = Registry.CurrentUser.CreateSubKey(
                            $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithProgids"
                        )) {
                        if (openWithKey == null) {
                            Log.Error(string.Format(LanguageControl.Get(fName, "4"), extension));
                            return false;
                        }
                        openWithKey.SetValue($"{ProgIdBase}{extension}", Array.Empty<byte>(), RegistryValueKind.None);
                    }
                }
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception e) {
                Log.Error($"{LanguageControl.Get(fName, "5")}\n{e.Message}");
                return false;
            }
            Log.Information(LanguageControl.Get(fName, "6"));
#endif
            return true;
        }

        public static void Unregister() {
#if WINDOWS
            try {
                foreach (string extension in SupportedExtensions) {
                    Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgIdBase}", false);
                    Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithProgids", true)
                        ?.DeleteValue($"{ProgIdBase}{extension}", false);
                }
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception e) {
                Log.Error($"{LanguageControl.Get(fName, "7")}\n{e.Message}");
            }
            Log.Information(LanguageControl.Get(fName, "8"));
#endif
        }

        public static bool IsRegistered() {
#if WINDOWS
            try {
                string appPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (appPath == null) {
                    return false;
                }
                string friendlyAppName =
                    $"{LanguageControl.Get("Usual", "gameName")} {ModsManager.ShortGameVersion} - API {ModsManager.APIVersionString}";
                string command = $"\"{appPath}\" \"%1\"";
                foreach (string extension in SupportedExtensions) {
                    using (RegistryKey progKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProgIdBase}{extension}", false)) {
                        if (progKey == null) {
                            return false;
                        }
                        if (progKey.GetValue("FriendlyAppName")?.ToString() != friendlyAppName) {
                            return false;
                        }
                        if ((string)progKey.OpenSubKey($@"shell\open\command", false)?.GetValue("")
                            != command) {
                            return false;
                        }
                    }
                    if (Registry.CurrentUser.OpenSubKey(
                                $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithProgids",
                                false
                            )
                            ?.GetValue($"{ProgIdBase}{extension}")
                        == null) {
                        return false;
                    }
                }
            }
            catch {
                return false;
            }
#endif
            return true;
        }
#if WINDOWS
        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
#endif
    }
}