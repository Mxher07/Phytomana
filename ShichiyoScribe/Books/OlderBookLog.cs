using Engine;

namespace Game {
    /// <summary>
    ///     OlderBook 日志入口。
    ///     Debug 编译会额外输出各步骤详细信息；Release 只保留主要进度与报错日志。
    /// </summary>
    public static class OlderBookLog {
        public const string Tag = "[ShichiyoScribe]";

        /// <summary>主要进度信息（Release 也输出）。</summary>
        public static void Info(string message) => Log.Information($"{Tag} {message}");

        /// <summary>详细调试信息，仅 DEBUG 编译输出。</summary>
        public static void Debug(string message) {
#if DEBUG
            Log.Information($"{Tag}[Debug] {message}");
#endif
        }

        public static void Warning(string message) => Log.Warning($"{Tag} {message}");

        public static void Error(string message) => Log.Error($"{Tag} {message}");

        public static void Error(string message, System.Exception e) => Log.Error($"{Tag} {message}\n{e}");
    }
}