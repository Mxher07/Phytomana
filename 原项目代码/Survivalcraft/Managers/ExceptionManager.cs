using Engine;
using Engine.Input;

namespace Game {
    public static class ExceptionManager {
        public static Exception m_error;

        public static Exception Error => m_error;

        public static void ReportExceptionToUser(string additionalMessage, Exception e) {
            string arg = MakeFullErrorMessage(additionalMessage, e);
            Log.Error($"{arg}\n{e.StackTrace}");
        }

        public static void DrawExceptionScreen() { }

        public static void UpdateExceptionScreen() { }

        public static string MakeFullErrorMessage(Exception e) => MakeFullErrorMessage(null, e);

        public static string MakeFullErrorMessage(string additionalMessage, Exception e) {
            string text = string.Empty;
            if (!string.IsNullOrEmpty(additionalMessage)) {
                text = additionalMessage;
            }
            for (Exception ex = e; ex != null; ex = ex.InnerException) {
                text = text + (text.Length > 0 ? Environment.NewLine : string.Empty) + ex;
            }
            return text;
        }

        public static bool CheckContinueKey() {
            if (Keyboard.IsKeyDown(Key.F12)
                || Keyboard.IsKeyDown(Key.Back)) {
                return true;
            }
            return false;
        }
    }
}