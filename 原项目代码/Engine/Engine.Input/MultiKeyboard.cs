using Engine;
using Engine.Input;

public static class MultiKeyboard {
    class KeyboardData {
        // ReSharper disable MemberHidesStaticFromOuterClass
        public bool IsConnected;

        public Key? LastKey;

        public char? LastChar;
        // ReSharper restore MemberHidesStaticFromOuterClass

        public bool[] KeysDownArray = new bool[Enum.GetValues<Key>().Length];

        public bool[] KeysDownOnceArray = new bool[Enum.GetValues<Key>().Length];

        public double[] KeysDownRepeatArray = new double[Enum.GetValues<Key>().Length];
    }

    static double KeyFirstRepeatTime = 0.3;

    static double KeyNextRepeatTime = 0.04;

    static KeyboardData[] _KeyboardData = [new(), new(), new(), new()];

    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public static bool BackButtonQuitsApp { get; set; }
    // ReSharper restore UnusedAutoPropertyAccessor.Global

    public static event Action<int, Key> KeyDown;

    public static event Action<int, Key> KeyUp;

    public static event Action<int, char> CharacterEntered;

    public static bool IsConnected(int keyboardIndex) => _KeyboardData[keyboardIndex].IsConnected;

    public static bool IsKeyDown(int keyboardIndex, Key key) => _KeyboardData[keyboardIndex].KeysDownArray[(int)key];

    public static bool IsKeyDownOnce(int keyboardIndex, Key key) => _KeyboardData[keyboardIndex].KeysDownOnceArray[(int)key];

    public static bool IsKeyDownRepeat(int keyboardIndex, Key key) {
        double num = _KeyboardData[keyboardIndex].KeysDownRepeatArray[(int)key];
        return num < 0.0 || (num != 0.0 && Time.FrameStartTime >= num);
    }

    public static Key? LastKey(int keyboardIndex) => _KeyboardData[keyboardIndex].LastKey;

    public static char? LastChar(int keyboardIndex) => _KeyboardData[keyboardIndex].LastChar;

    public static void Clear() {
        for (int i = 0; i < 4; i++) {
            _KeyboardData[i].LastKey = null;
            _KeyboardData[i].LastChar = null;
            for (int j = 0; j < _KeyboardData[i].KeysDownArray.Length; j++) {
                _KeyboardData[i].KeysDownArray[j] = false;
                _KeyboardData[i].KeysDownOnceArray[j] = false;
                _KeyboardData[i].KeysDownRepeatArray[j] = 0.0;
            }
        }
    }

    internal static void BeforeFrame() { }

    internal static void AfterFrame() {
        for (int i = 0; i < 4; i++) {
            if (BackButtonQuitsApp && IsKeyDownOnce(i, Key.Back)) {
                Window.Close();
            }
            _KeyboardData[i].LastKey = null;
            _KeyboardData[i].LastChar = null;
            for (int j = 0; j < _KeyboardData[i].KeysDownOnceArray.Length; j++) {
                _KeyboardData[i].KeysDownOnceArray[j] = false;
            }
            for (int k = 0; k < _KeyboardData[i].KeysDownRepeatArray.Length; k++) {
                if (_KeyboardData[i].KeysDownArray[k]) {
                    if (_KeyboardData[i].KeysDownRepeatArray[k] < 0.0) {
                        _KeyboardData[i].KeysDownRepeatArray[k] = Time.FrameStartTime + KeyFirstRepeatTime;
                    }
                    else if (Time.FrameStartTime >= _KeyboardData[i].KeysDownRepeatArray[k]) {
                        _KeyboardData[i].KeysDownRepeatArray[k] = Math.Max(
                            Time.FrameStartTime,
                            _KeyboardData[i].KeysDownRepeatArray[k] + KeyNextRepeatTime
                        );
                    }
                }
                else {
                    _KeyboardData[i].KeysDownRepeatArray[k] = 0.0;
                }
            }
        }
    }

    // ReSharper disable UnusedMember.Local
    static void SetIsConnected(int keyboardIndex, bool value) {
        _KeyboardData[keyboardIndex].IsConnected = value;
    }

    static bool ProcessKeyDown(int keyboardIndex, Key key) {
        if (!Window.IsActive) {
            return false;
        }
        _KeyboardData[keyboardIndex].LastKey = key;
        if (!_KeyboardData[keyboardIndex].KeysDownArray[(int)key]) {
            _KeyboardData[keyboardIndex].KeysDownArray[(int)key] = true;
            _KeyboardData[keyboardIndex].KeysDownOnceArray[(int)key] = true;
            _KeyboardData[keyboardIndex].KeysDownRepeatArray[(int)key] = -1.0;
        }
        KeyDown?.Invoke(keyboardIndex, key);
        return true;
    }

    static bool ProcessKeyUp(int keyboardIndex, Key key) {
        if (!Window.IsActive) {
            return false;
        }
        if (_KeyboardData[keyboardIndex].KeysDownArray[(int)key]) {
            _KeyboardData[keyboardIndex].KeysDownArray[(int)key] = false;
        }
        KeyUp?.Invoke(keyboardIndex, key);
        return true;
    }

    static bool ProcessCharacterEntered(int keyboardIndex, char ch) {
        if (!Window.IsActive) {
            return false;
        }
        _KeyboardData[keyboardIndex].LastChar = ch;
        CharacterEntered?.Invoke(keyboardIndex, ch);
        return true;
    }
    // ReSharper restore UnusedMember.Local

    internal static void Initialize() { }

    internal static void Dispose() { }
}