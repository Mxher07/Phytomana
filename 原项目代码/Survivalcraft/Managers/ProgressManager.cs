using Engine;

namespace Game {
    public static class ProgressManager {
        public static string OperationName { get; set; }

        public static float Progress { get; set; }

        public static void UpdateProgress(string operationName, float progress) {
            OperationName = operationName;
            Progress = MathUtils.Saturate(progress);
        }
    }
}