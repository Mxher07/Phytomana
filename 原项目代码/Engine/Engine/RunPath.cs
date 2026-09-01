using System.Reflection;

namespace Engine {
    public class RunPath {
        #region //按照游戏格式的路径

        public static string AndroidFilePath = "android:Survivalcraft2.4_API1.9";

        #endregion

        /// <summary>
        ///     获取实际运行路径
        /// </summary>
        public static string GetOperatingPath() => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        ///     获取 EXE 或 dll 所在路径(包含文件自身路径)
        /// </summary>
#pragma warning disable IL3000
        public static string GetExecutablePath() => Assembly.GetExecutingAssembly().Location;
#pragma warning restore IL3000
        /// <summary>
        ///     获取运行入口路径(用命令行或者其他程序调用时调用者目录)
        /// </summary>
        public static string GetEntryPath() => AppContext.BaseDirectory;

        /// <summary>
        ///     获取环境变量 path 多个路径用分号分隔
        /// </summary>
        public static string GetEnvironmentPath() => Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
    }
}
//跑路[doge]