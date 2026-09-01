#if ANDROID
#pragma warning disable CA1416
using Environment = Android.OS.Environment;
using Android.OS;
#elif IOS
using Foundation;
#else
#if BROWSER
using System.Runtime.InteropServices;
using Engine.Browser;
using System.Runtime.InteropServices.JavaScript;
#pragma warning disable CA1416
#elif WINDOWS || LINUX
using System.Diagnostics;
using NativeFileDialogCore;
#if LINUX
using System.Runtime.InteropServices;
#endif
#endif // WINDOWS || LINUX
using System.Reflection;
#endif // !ANDROID
using System.Text;

namespace Engine {
    public static class Storage {
#if !ANDROID
        const bool m_isAndroidPlatform = false;
        static bool m_dataDirectoryCreated;
        static object m_dataDirectoryCreationLock = new();
#else
        const bool m_isAndroidPlatform = true;
#endif

        static Random m_random = new ();

        /// <summary>
        /// Content 加载回调
        /// 参数：ContentManager 中绝对路径
        /// </summary>
        public static Func<string, Stream> LoadContentStreamCallback { get; set; }

        public static void Initialize() {
#if LINUX
            // NativeFileDialogCore on Linux uses DllImport("nfd64"), but the NuGet package
            // provides the native library as libnfd.so. Map the name to resolve correctly.
            NativeLibrary.SetDllImportResolver(
                typeof(Dialog).Assembly,
                (name, assembly, path) => {
                    if (name == "nfd64") {
                        if (NativeLibrary.TryLoad("libnfd", assembly, path, out nint handle)) {
                            return handle;
                        }
                    }
                    return nint.Zero;
                }
            );
#endif
#if BROWSER
            MountOPFS("/__root__");
#endif
        }

        public static long FreeSpace {
            get {
#if ANDROID
                try {
                    StatFs statFs = new(Environment.DataDirectory?.Path);
                    long num = statFs.BlockSizeLong;
                    return statFs.AvailableBlocksLong * num;
                }
                catch (Exception) {
                    return long.MaxValue;
                }
#elif IOS
                try {
                    var paths = NSSearchPath.GetDirectories(NSSearchPathDirectory.DocumentDirectory,
                                              NSSearchPathDomain.User, true);

                    var attributes = NSFileManager.DefaultManager.GetFileSystemAttributes(paths[0]);

                    return (long)attributes.FreeSize;
                }
                catch (Exception) {
                    return long.MaxValue;
                }
#else
                string fullPath = Path.GetFullPath(ProcessPath("data:", false, false));
                if (fullPath.Length > 0) {
                    try {
                        return new DriveInfo(fullPath.Substring(0, 1)).AvailableFreeSpace;
                    }
                    catch {
                        // ignored
                    }
                }
                return long.MaxValue;
#endif
            }
        }

        public static bool FileExists(string path) {
#if ANDROID
            string path2 = ProcessPath(path, false, false, out bool isApp);
            if (isApp) {
                return EngineActivity.m_activity.ApplicationContext?.Assets?.List(GetDirectoryName(path2))?.Contains(GetFileName(path2)) ?? false;
            }
#endif
            return File.Exists(ProcessPath(path, false, m_isAndroidPlatform));
        }

        public static bool DirectoryExists(string path) => Directory.Exists(ProcessPath(path, false, m_isAndroidPlatform));

        public static long GetFileSize(string path) => new FileInfo(ProcessPath(path, false, m_isAndroidPlatform)).Length;

        public static DateTime GetFileLastWriteTime(string path) => File.GetLastWriteTimeUtc(ProcessPath(path, false, m_isAndroidPlatform));

        public static Stream OpenFile(string path, OpenFileMode openFileMode) {
            if (openFileMode != 0
                && openFileMode != OpenFileMode.ReadWrite
                && openFileMode != OpenFileMode.Create
                && openFileMode != OpenFileMode.CreateOrOpen) {
                throw new ArgumentException("openFileMode");
            }
#if ANDROID
            string path2 = ProcessPath(path, openFileMode != OpenFileMode.Read, false, out bool isApp);
            if (isApp) {
                return EngineActivity.m_activity.ApplicationContext?.Assets?.Open(path2);
            }
#else
            string path2 = ProcessPath(path, openFileMode != OpenFileMode.Read, false);
#endif
            FileMode mode;
            switch (openFileMode) {
                case OpenFileMode.Create: mode = FileMode.Create; break;
                case OpenFileMode.CreateOrOpen: mode = FileMode.OpenOrCreate; break;
                default: mode = FileMode.Open; break;
            }
            FileAccess access = openFileMode == OpenFileMode.Read ? FileAccess.Read : FileAccess.ReadWrite;
            return File.Open(path2, mode, access, FileShare.Read);
        }

        public static void DeleteFile(string path) {
            File.Delete(ProcessPath(path, true, m_isAndroidPlatform));
        }

        public static void CopyFile(string sourcePath, string destinationPath) {
            using Stream stream = OpenFile(sourcePath, OpenFileMode.Read);
            using Stream destination = OpenFile(destinationPath, OpenFileMode.Create);
            stream.CopyTo(destination);
        }

        /// <summary>
        /// 移动文件<br/>
        /// 建议改用更加安全的 MoveFileSafely 方法
        /// </summary>
        public static void MoveFile(string sourcePath, string destinationPath) {
            string sourceFileName = ProcessPath(sourcePath, true, m_isAndroidPlatform);
            string text = ProcessPath(destinationPath, true, m_isAndroidPlatform);
            File.Delete(text);
            File.Move(sourceFileName, text);
        }

        public static void MoveFileSafely(string sourcePath, string destinationPath) {
            sourcePath = ProcessPath(sourcePath, true, m_isAndroidPlatform);
            destinationPath = ProcessPath(destinationPath, true, m_isAndroidPlatform);

            string sourceFullPath = Path.GetFullPath(sourcePath);
            string destinationFullPath = Path.GetFullPath(destinationPath);
            StringComparison pathComparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (string.Equals(sourceFullPath, destinationFullPath, pathComparison)
                && File.Exists(sourcePath)) {
                return;
            }

            if (File.Exists(destinationPath)) {
                string backupPath = destinationPath + m_random.UInt();
                while (File.Exists(backupPath)) {
                    backupPath = destinationPath + m_random.UInt();
                }

                File.Move(destinationPath, backupPath);
                try {
                    File.Move(sourcePath, destinationPath);
                }
                catch {
                    try {
                        if (!File.Exists(destinationPath) && File.Exists(backupPath)) {
                            File.Move(backupPath, destinationPath);
                        }
                    }
                    catch {
                        // Preserve the original move exception. The backup remains available for recovery.
                    }
                    throw;
                }

                File.Delete(backupPath);
            }
            else {
                File.Move(sourcePath, destinationPath);
            }
        }

        /// <summary>
        /// 将流追加到文件末尾。文件不存在时创建文件；不会自动创建父目录。
        /// </summary>
        public static void Append(string path, Stream source) {
            ArgumentNullException.ThrowIfNull(source);
            using Stream destination = OpenFile(path, OpenFileMode.CreateOrOpen);
            destination.Seek(0L, SeekOrigin.End);
            source.CopyTo(destination);
        }

        /// <summary>
        /// 将字节追加到文件末尾。文件不存在时创建文件；不会自动创建父目录。
        /// </summary>
        public static void Append(string path, byte[] bytes) {
            ArgumentNullException.ThrowIfNull(bytes);
            using MemoryStream source = new(bytes, writable: false);
            Append(path, source);
        }

        public static void CreateDirectory(string path) {
            Directory.CreateDirectory(ProcessPath(path, true, m_isAndroidPlatform));
        }

        public static void DeleteDirectory(string path) {
            Directory.Delete(ProcessPath(path, true, m_isAndroidPlatform));
        }

        public static void DeleteDirectory(string path, bool recursive) {
            Directory.Delete(ProcessPath(path, true, m_isAndroidPlatform), recursive);
        }

        public static IEnumerable<string> ListFileNames(string path) =>
            from s in Directory.EnumerateFiles(ProcessPath(path, false, m_isAndroidPlatform)) select Path.GetFileName(s);

        public static IEnumerable<string> ListDirectoryNames(string path) {
            return from s in Directory.EnumerateDirectories(ProcessPath(path, false, m_isAndroidPlatform))
#if ANDROID
                select Path.GetFileName(s)
                into s
                where s != ".__override__"
                select s;
#else
                select Path.GetFileName(s);
#endif
        }

        public static string ReadAllText(string path) => ReadAllText(path, Encoding.UTF8);

        public static string ReadAllText(string path, Encoding encoding) {
            using StreamReader streamReader = new(OpenFile(path, OpenFileMode.Read), encoding);
            return streamReader.ReadToEnd();
        }

        public static void WriteAllText(string path, string text) {
            WriteAllText(path, text, Encoding.UTF8);
        }

        public static void WriteAllText(string path, string text, Encoding encoding) {
            using StreamWriter streamWriter = new(OpenFile(path, OpenFileMode.Create), encoding);
            streamWriter.Write(text);
        }

        public static byte[] ReadAllBytes(string path) {
            using BinaryReader binaryReader = new(OpenFile(path, OpenFileMode.Read));
            return binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
        }

        public static void WriteAllBytes(string path, byte[] bytes) {
            using BinaryWriter binaryWriter = new(OpenFile(path, OpenFileMode.Create));
            binaryWriter.Write(bytes);
        }

        public static string GetSystemPath(string path) => ProcessPath(path, false, m_isAndroidPlatform);

        public static string GetExtension(string path) {
            if (string.IsNullOrEmpty(path)) {
                return string.Empty;
            }
            int lastIndexOfPoint = path.LastIndexOf('.');
            int lastIndexOfSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            return lastIndexOfPoint >= 0 && (lastIndexOfSlash == -1 || lastIndexOfSlash < lastIndexOfPoint)
                ? path.Substring(lastIndexOfPoint)
                : string.Empty;
        }

        public static string GetFileName(string path) {
            if (string.IsNullOrEmpty(path)) {
                return string.Empty;
            }
            int num = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            return num >= 0 ? path.Substring(num + 1) : path;
        }

        public static string GetFileNameWithoutExtension(string path) {
            if (string.IsNullOrEmpty(path)) {
                return string.Empty;
            }
            string fileName = GetFileName(path);
            int num = fileName.LastIndexOf('.');
            return num >= 0 ? fileName.Substring(0, num) : fileName;
        }

        public static string GetDirectoryName(string path) {
            if (string.IsNullOrEmpty(path)) {
                return string.Empty;
            }
            int num = path.LastIndexOf('/');
            return num >= 0 ? path.Substring(0, num).TrimEnd('/') : string.Empty;
        }

        public static string CombinePaths(params string[] paths) {
            StringBuilder stringBuilder = new();
            for (int i = 0; i < paths.Length; i++) {
                if (paths[i].Length > 0) {
                    stringBuilder.Append(paths[i]);
                    if (i < paths.Length - 1
                        && (stringBuilder.Length == 0 || stringBuilder[^1] != '/')) {
                        stringBuilder.Append('/');
                    }
                }
            }
            return stringBuilder.ToString();
        }

        public static string ChangeExtension(string path, string extension) =>
            CombinePaths(GetDirectoryName(path), GetFileNameWithoutExtension(path)) + extension;

#if ANDROID
        public static string ProcessPath(string path, bool writeAccess, bool failIfApp) => ProcessPath(path, writeAccess, failIfApp, out _);
        public static string ProcessPath(string path, bool writeAccess, bool failIfApp, out bool isApp) {
            ArgumentNullException.ThrowIfNull(path);
            if (Path.DirectorySeparatorChar != '/') {
                path = path.Replace('/', Path.DirectorySeparatorChar);
            }
            if (Path.DirectorySeparatorChar != '\\') {
                path = path.Replace('\\', Path.DirectorySeparatorChar);
            }
            if (path.StartsWith("app:")) {
                if (failIfApp) {
                    throw new InvalidOperationException($"Access denied to \"{path}\".");
                }
                isApp = true;
                return path.Substring(4).TrimStart(Path.DirectorySeparatorChar);
            }
            if (path.StartsWith("data:")) {
                isApp = false;
                return Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                    path.Substring(5).TrimStart(Path.DirectorySeparatorChar)
                );
            }
            if (path.StartsWith("android:")) {
                isApp = false;
                return Path.Combine(
                    CombinePaths(Environment.ExternalStorageDirectory?.AbsolutePath, path.Substring(8).TrimStart(Path.DirectorySeparatorChar))
                );
            }
            if (path.StartsWith("config:")) {
                isApp = false;
                return Path.Combine(EngineActivity.ConfigPath, path.Substring(8).TrimStart(Path.DirectorySeparatorChar));
            }
            throw new InvalidOperationException($"Invalid path \"{path}\".");
        }
#elif IOS
        public static string ProcessPath(string path, bool writeAccess, bool failIfApp) => ProcessPath(path, writeAccess, failIfApp, out _);
        public static string ProcessPath(string path, bool writeAccess, bool failIfApp, out bool isApp) {
            ArgumentNullException.ThrowIfNull(path);
            if (Path.DirectorySeparatorChar != '/') {
                path = path.Replace('/', Path.DirectorySeparatorChar);
            }
            if (Path.DirectorySeparatorChar != '\\') {
                path = path.Replace('\\', Path.DirectorySeparatorChar);
            }
            if (path.StartsWith("app:")) {
                isApp = false;
                return Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                    path.Substring(4).TrimStart(Path.DirectorySeparatorChar)
                );
            }
            if (path.StartsWith("data:")) {
                isApp = false;
                return Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                    path.Substring(5).TrimStart(Path.DirectorySeparatorChar)
                );
            }
            throw new InvalidOperationException($"Invalid path \"{path}\".");
        }
#else
#if BROWSER
        public static string GetAppDirectory(bool failIfApp) => Path.DirectorySeparatorChar.ToString();
#else
        public static string GetAppDirectory(bool failIfApp) => failIfApp
            ? throw new InvalidOperationException("Access denied.")
#pragma warning disable IL3000
            : Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
#pragma warning restore IL3000
#endif
        public static string GetDataDirectory(bool writeAccess) {
            string text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Assembly.GetEntryAssembly()!.GetName()!.Name!
            );
            if (writeAccess) {
                lock (m_dataDirectoryCreationLock) {
                    if (m_dataDirectoryCreated) {
                        return text;
                    }
                    Directory.CreateDirectory(text);
                    m_dataDirectoryCreated = true;
                    return text;
                }
            }
            return text;
        }

        public static string ProcessPath(string path, bool writeAccess, bool failIfApp) {
            ArgumentNullException.ThrowIfNull(path);
            switch (Path.DirectorySeparatorChar) {
                case '/':
                    path = path.Replace('\\', Path.DirectorySeparatorChar);
                    break;
                case '\\':
                    path = path.Replace('/', Path.DirectorySeparatorChar);
                    break;
            }
            string baseDirectory;
            if (path.StartsWith("app:")) {
                baseDirectory = GetAppDirectory(failIfApp);
                path = path.Substring(4).TrimStart(Path.DirectorySeparatorChar);
            }
            else if (path.StartsWith("data:")) {
                baseDirectory = GetDataDirectory(writeAccess);
                path = path.Substring(5).TrimStart(Path.DirectorySeparatorChar);
            }
            else if (path.StartsWith("system:")) {
                baseDirectory = string.Empty;
                path = path.Substring(7);
            }
            else {
#if BROWSER
                EnsurePathLinked(path);
#endif
                return path;
            }
            string result = string.IsNullOrEmpty(baseDirectory) ? path : Path.Combine(baseDirectory, path);
#if BROWSER
            EnsurePathLinked(result);
#endif
            return result;
        }
#endif
        public static void MoveDirectory(string path, string newPath) => Directory.Move(ProcessPath(path, true, false), ProcessPath(newPath, true, false));

        public static void DeleteDirectoryRecursive(string path) => Directory.Delete(ProcessPath(path, true, false));

        public static DirectoryInfo GetDirectoryInfo(string path) => new(ProcessPath(path, true, false));

        public static FileInfo GetFileInfo(string path) => new(ProcessPath(path, true, false));

        public static char[] InvalidFileNameChars = [
            '\\',
            '/',
            ':',
            '*',
            '?',
            '"',
            '<',
            '>',
            '|',
            '\0'
        ];

        public static string SanitizeFileName(string filename, string replacement = "-") {
            StringBuilder sanitized = new();
            foreach (char c in filename) {
                sanitized.Append(InvalidFileNameChars.Contains(c) ? replacement : c);
            }
            return sanitized.ToString();
        }

        /*
         * <Summary>
         *  使用外部应用打开文件
         * </Summary>
         * <Param name="path">文件路径</Param>
         * <Param name="chooserTitle">（仅安卓）应用选择器标题，留空时使用文件名</Param>
         * <Param name="mimeType">（仅安卓）MIME 类型，留空时自动根据文件后缀推断</Param>
         */
        public static void OpenFileWithExternalApplication(string path, string chooserTitle = null, string mimeType = null) {
            if (!FileExists(path)) {
                throw new FileNotFoundException($"Open {path} failed, because it is not exists.");
            }
            path = ProcessPath(path, false, false);
#if WINDOWS
            Process.Start("explorer.exe", path);
#elif LINUX
            Process.Start("xdg-open", path);
#elif ANDROID
            Window.Activity.OpenFile(path, chooserTitle, mimeType);
#endif
        }

        /*
         * <Summary>
         *  分享文件，当前版本仅支持安卓、浏览器，浏览器上的形式为下载
         * </Summary>
         * <Param name="path">文件路径</Param>
         * <Param name="chooserTitle">（浏览器无效）应用选择器标题，留空时使用文件名</Param>
         * <Param name="mimeType">MIME 类型，留空时自动根据文件后缀推断</Param>
         */
        public static async Task ShareFile(string path, string chooserTitle = null, string mimeType = null) {
            if (!FileExists(path)) {
                throw new FileNotFoundException($"Share {path} failed, because it is not exists.");
            }
#if ANDROID
            path = ProcessPath(path, false, false);
            Window.Activity.ShareFile(path, chooserTitle, mimeType);
#elif BROWSER
            JSObject fileHandle = await BrowserInterop.ShowSaveFilePicker(Storage.GetFileName(path), mimeType);
            Stream stream = OpenFile(path, OpenFileMode.Read);
            byte[] bytes = new byte[stream.Length];
            _ = await stream.ReadAsync(bytes);
            await BrowserInterop.SaveBytesToFileHandle(fileHandle, bytes);
#endif
        }

        /*
         * <Summary>
         *   使用系统文件选择器选择并打开文件
         * </Summary>
         * <Param name="title">文件选择器的标题</Param>
         * <Param name="filters">（安卓无效）过滤器列表。键为名称，例如“图片”；值为通配符列表，例如["*.png", "*.jpg"]</Param>
         * <Param name="defaultPath">（安卓无效）默认路径。其中浏览器只支持"desktop"、"documents"、"downloads"、"music"、"pictures" 或 "videos"</Param>
         * <Param name="mode">（安卓、浏览器只读）文件打开模式</Param>
         */
#pragma warning disable CS1998
        public static async Task<(Stream, string)> ChooseFile(string title = null,
            KeyValuePair<string, string[]>[] filters = null,
            string defaultPath = null,
            OpenFileMode mode = OpenFileMode.Read) {
#pragma warning restore CS1998
            if (mode == OpenFileMode.Create
                || mode == OpenFileMode.CreateOrOpen) {
                throw new ArgumentException("mode");
            }
#if ANDROID
            return await Window.Activity.ChooseFileAsync(title);
#elif  IOS
            throw new Exception("Unsupported Operation");
#elif BROWSER
            List<string> descAndExtArray = [];
            List<int> extCounts = [];
            if (filters != null) {
                foreach (KeyValuePair<string, string[]> filter in filters) {
                    descAndExtArray.Add(filter.Key);
                    extCounts.Add(filter.Value.Length);
                    string[] extensions = filter.Value;
                    foreach (string extension in extensions) {
                        descAndExtArray.Add(extension);
                    }
                }
            }
            JSObject file = null;
            try {
                file = await BrowserInterop.ShowOpenFilePicker(descAndExtArray.ToArray(), extCounts.ToArray(), defaultPath);
            }
            catch {
                // ignore
            }
            if (file == null) {
                return (null, null);
            }
            string fileName = BrowserInterop.GetFileName(file);
            if (string.IsNullOrEmpty(fileName)) {
                return (null, null);
            }
            JSObject bytes = await BrowserInterop.GetFileBytes(file);
            file.Dispose();
            Stream stream = new MemoryStream(BrowserInterop.JSObject2ByteArray(bytes));
            bytes.Dispose();
            stream.Position = 0;
            return (stream, fileName);
#elif WINDOWS || LINUX
            string filtersString = null;
            if (filters != null) {
                StringBuilder sb = new();
                bool firstAdded1 = false;
                foreach (KeyValuePair<string, string[]> filter in filters) {
                    if (firstAdded1) {
                        sb.Append(';');
                    }
                    else {
                        firstAdded1 = true;
                    }
                    sb.Append('[');
                    sb.Append(filter.Key);
                    sb.Append('|');
                    bool firstAdded2 = false;
                    foreach (string ext in filter.Value) {
                        if (firstAdded2) {
                            sb.Append(',');
                        }
                        else {
                            firstAdded2 = true;
                        }
                        sb.Append(ext);
                    }
                    sb.Append(']');
                }
                filtersString = sb.ToString();
            }
            DialogResult result = Dialog.FileOpenEx(
                filtersString,
                defaultPath,
                title,
                null,
                null,
                null,
                Window.Handle
            );
            if (result.IsOk
                && !string.IsNullOrEmpty(result.Path)) {
                try {
                    Stream stream = File.Open(
                        result.Path,
                        FileMode.Open,
                        mode == OpenFileMode.Read ? FileAccess.Read : FileAccess.ReadWrite,
                        FileShare.Read
                    );
                    return (stream, GetFileName(result.Path));
                }
                catch (Exception e) {
                    Log.Error($"Choose file failed. File path: \"{result.Path}\". Reason: {e.Message}");
                    return (null, result.Path);
                }
            }
            return (null, null);
#else
            return (null, null);
#endif
        }

#if BROWSER
        static HashSet<string> m_linkedPaths = ["__root__", "dev", "tmp"];
        /// <summary>
        /// 自动映射路径。By Gemini
        /// </summary>
        static void EnsurePathLinked(string path) {
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            path = path.Replace('\\', '/');
            string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) {
                return;
            }
            string topDir = parts[0];
            // 如果已经是系统目录或挂载根目录，直接跳过
            if (m_linkedPaths.Contains(topDir)) {
                return;
            }
            string linkPath = $"/{topDir}";
            string realPath = $"/__root__/{topDir}";
            if (!Directory.Exists(realPath)) {
                Directory.CreateDirectory(realPath);
            }
            int res = CreateSymlink(realPath, linkPath);
            switch (res) {
                case 0:
                    m_linkedPaths.Add(topDir);
                    break;
                case -17:// EEXIST
                    m_linkedPaths.Add(topDir);
                    // 可以在这里加个校验，确定它是不是指向正确的地方，但通常没必要
                    break;
                default: throw new Exception($"Failed to link \"{linkPath}\" to \"{realPath}\", error code: {res}");
            }
        }

        [DllImport("wasmfsHelper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        static extern int MountOPFS(string mountPath);

        [DllImport("wasmfsHelper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        static extern int CreateSymlink(string target, string linkpath);
#endif
    }
}