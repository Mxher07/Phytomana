using System.Globalization;
using Engine;
#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace Game {
    public class GameLogSink : ILogSink, IDisposable {
        public static Stream m_stream;

        public static StreamWriter m_writer;

        public static string errorOfInstantiation;

        public const string fName = "GameLogSink";
        public GameLogSink() {
            try {
                if (m_stream != null) {
                    throw new InvalidOperationException("GameLogSink already created.");
                }
                if (!Storage.DirectoryExists(ModsManager.LogPath)) {
                    Storage.CreateDirectory(ModsManager.LogPath);
                }
                string path = Storage.CombinePaths(ModsManager.LogPath, "Game.log");
                FileInfo fileInfo = Storage.GetFileInfo(path);
                if (!fileInfo.Exists) {
                    m_stream = fileInfo.Create();
                }
                else {
                    if (fileInfo.Length > 2097152) //2MiB
                    {
                        CultureInfo cultureInfo = Program.SystemLanguage == null
                            ? CultureInfo.CurrentCulture
                            : new CultureInfo(Program.SystemLanguage);
                        string destination = Storage.ProcessPath(
                            Storage.CombinePaths(ModsManager.LogPath, Storage.SanitizeFileName($"Game {DateTime.Now.ToString(cultureInfo)}.log")),
                            true,
                            false
                        );
                        try {
                            fileInfo.MoveTo(destination, true);
                        }
                        catch {
                            // ignore
                        }
                        m_stream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
                    }
                    else {
                        m_stream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                    }
                }
                m_stream.Position = m_stream.Length;
                m_writer = new StreamWriter(m_stream);
            }
            catch (Exception ex) {
#if !MOBILE && !BROWSER
#if WINDOWS
                AllocConsole();
                Window.Closed += () => FreeConsole();
#endif
#pragma warning disable CA1416
                Console.Title = "Logs of Survivalcraft API";
#pragma warning restore CA1416
                errorOfInstantiation = $"Error creating GameLogSink, and a console window for viewing logs is created. Reason: {ex.Message}";
                Engine.Log.Information(errorOfInstantiation);
#else
                errorOfInstantiation = $"Error creating GameLogSink. Reason: {ex.Message}";
                Engine.Log.Error(errorOfInstantiation);
#endif
            }
        }

        public static string GetRecentLog(int bytesCount) {
            if (m_stream == null) {
                return LanguageControl.Get(fName, "1");
            }
            lock (m_stream) {
                try {
                    m_stream.Position = Math.Max(m_stream.Position - bytesCount, 0L);
                    return new StreamReader(m_stream).ReadToEnd();
                }
                finally {
                    m_stream.Position = m_stream.Length;
                }
            }
        }

        public static List<string> GetRecentLogLines(int bytesCount) {
            if (m_stream == null) {
                return [errorOfInstantiation, LanguageControl.Get(fName, "1")];
            }
            lock (m_stream) {
                try {
                    m_stream.Position = Math.Max(m_stream.Position - bytesCount, 0L);
                    StreamReader streamReader = new(m_stream);
                    List<string> list = new();
                    while (true) {
                        string text = streamReader.ReadLine();
                        if (text == null) {
                            break;
                        }
                        list.Add(text);
                    }
                    return list;
                }
                finally {
                    m_stream.Position = m_stream.Length;
                }
            }
        }

        public void Log(LogType type, string message) {
            if (m_stream != null) {
                lock (m_stream) {
                    string value;
                    switch (type) {
                        case LogType.Debug: value = "DEBUG: "; break;
                        case LogType.Verbose: value = "INFO: "; break;
                        case LogType.Information: value = "INFO: "; break;
                        case LogType.Warning: value = "WARNING: "; break;
                        case LogType.Error: value = "ERROR: "; break;
                        default: value = string.Empty; break;
                    }
                    m_writer.Write(DateTime.Now.ToString("HH:mm:ss.fff"));
                    m_writer.Write(" ");
                    m_writer.Write(value);
                    m_writer.WriteLine(message);
                    m_writer.Flush();
                }
            }
        }

        public void Dispose() {
            if (m_writer != null) {
                m_writer.Dispose();
                m_writer = null;
            }
            if (m_stream != null) {
                m_stream.Dispose();
                m_stream = null;
            }
        }

#if WINDOWS
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();
#endif
    }
}