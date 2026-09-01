namespace Engine.Input {
    public class KeyboardInput {
        public static List<char> Chars = [];
        public static bool _DeletePressed;

        public static bool DeletePressed {
            get {
                bool D = _DeletePressed;
                if (D) {
                    _DeletePressed = false;
                }
                return D;
            }
            set => _DeletePressed = value;
        }

        public static string GetInput() {
            if (Chars.Count > 0) {
                string str = new(Chars.ToArray());
                Chars.Clear();
                return str;
            }
            return string.Empty;
        }
    }
}