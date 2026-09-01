namespace Game {
    public static class StringsManager {
        public static string GetString(string name) => LanguageControl.Get("Strings", name);

        public static void LoadStrings() { }
    }
}