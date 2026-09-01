// Engine, Version=1.0.8083.41417, Culture=neutral, PublicKeyToken=null
// Engine.Input.MultiMouse

using Engine;
using Engine.Input;

public static class MultiMouse {
    class MouseData {
        // ReSharper disable MemberHidesStaticFromOuterClass
        public bool IsConnected;

        public Point2 MousePosition;

        public Point2 MouseMovement;

        public int MouseWheelMovement;
        // ReSharper restore MemberHidesStaticFromOuterClass

        public bool[] MouseButtonsDownArray = new bool[Enum.GetValues<MouseButton>().Length];

        public bool[] MouseButtonsDownOnceArray = new bool[Enum.GetValues<MouseButton>().Length];

        public bool[] MouseButtonsUpOnceArray = new bool[Enum.GetValues<MouseButton>().Length];
    }

    static MouseData[] _MouseData = [new(), new(), new(), new()];

    public static event Action<MultiMouseEvent> MouseMove;

    public static event Action<MultiMouseButtonEvent> MouseDown;

    public static event Action<MultiMouseButtonEvent> MouseUp;

    public static bool IsConnected(int mouseIndex) => _MouseData[mouseIndex].IsConnected;

    public static Point2 MouseMovement(int mouseIndex) => _MouseData[mouseIndex].MouseMovement;

    public static int MouseWheelMovement(int mouseIndex) => _MouseData[mouseIndex].MouseWheelMovement;

    public static Point2 MousePosition(int mouseIndex) => _MouseData[mouseIndex].MousePosition;

    public static bool IsMouseButtonDown(int mouseIndex, MouseButton mouseButton) => _MouseData[mouseIndex].MouseButtonsDownArray[(int)mouseButton];

    public static bool IsMouseButtonDownOnce(int mouseIndex, MouseButton mouseButton) =>
        _MouseData[mouseIndex].MouseButtonsDownOnceArray[(int)mouseButton];

    public static bool IsMouseButtonUpOnce(int mouseIndex, MouseButton mouseButton) =>
        _MouseData[mouseIndex].MouseButtonsUpOnceArray[(int)mouseButton];

    public static void Clear() {
        for (int i = 0; i < _MouseData.Length; i++) {
            for (int j = 0; j < _MouseData[i].MouseButtonsDownArray.Length; j++) {
                _MouseData[i].MouseButtonsDownArray[j] = false;
                _MouseData[i].MouseButtonsDownOnceArray[j] = false;
                _MouseData[i].MouseButtonsUpOnceArray[j] = false;
            }
        }
    }

    internal static void AfterFrame() {
        for (int i = 0; i < _MouseData.Length; i++) {
            for (int j = 0; j < _MouseData[i].MouseButtonsDownOnceArray.Length; j++) {
                _MouseData[i].MouseButtonsDownOnceArray[j] = false;
                _MouseData[i].MouseButtonsUpOnceArray[j] = false;
            }
        }
    }

    // ReSharper disable UnusedMember.Local
    static void ProcessMouseDown(int mouseIndex, MouseButton mouseButton, Point2 position) {
        if (Window.IsActive
            && !Keyboard.IsKeyboardVisible) {
            MouseData obj = _MouseData[mouseIndex];
            obj.MouseButtonsDownArray[(int)mouseButton] = true;
            obj.MouseButtonsDownOnceArray[(int)mouseButton] = true;
            if (MouseDown != null) {
                MouseDown(new MultiMouseButtonEvent { MouseIndex = mouseIndex, Button = mouseButton, Position = position });
            }
        }
    }

    static void ProcessMouseUp(int mouseIndex, MouseButton mouseButton, Point2 position) {
        if (Window.IsActive
            && !Keyboard.IsKeyboardVisible) {
            MouseData obj = _MouseData[mouseIndex];
            obj.MouseButtonsDownArray[(int)mouseButton] = false;
            obj.MouseButtonsUpOnceArray[(int)mouseButton] = true;
            if (MouseUp != null) {
                MouseUp(new MultiMouseButtonEvent { MouseIndex = mouseIndex, Button = mouseButton, Position = position });
            }
        }
    }

    static void ProcessMouseMove(int mouseIndex, Point2 move) {
        if (Window.IsActive
            && !Keyboard.IsKeyboardVisible) {
            MouseData mouseData = _MouseData[mouseIndex];
            int x = (int)Math.Round(1f * MathUtils.PowSign(move.X, 1.25f));
            int y = (int)Math.Round(1f * MathUtils.PowSign(move.Y, 1.25f));
            mouseData.MousePosition += new Point2(x, y);
            mouseData.MousePosition.X = Math.Clamp(mouseData.MousePosition.X, 0, Window.Size.X - 1);
            mouseData.MousePosition.Y = Math.Clamp(mouseData.MousePosition.Y, 0, Window.Size.Y - 1);
            if (MouseMove != null) {
                MouseMove(new MultiMouseEvent { MouseIndex = mouseIndex, Position = mouseData.MousePosition });
            }
        }
    }
    // ReSharper restore UnusedMember.Local

    internal static void IsConnected(int mouseIndex, bool value) {
        _MouseData[mouseIndex].IsConnected = value;
    }

    internal static void MouseMovement(int mouseIndex, Point2 value) {
        _MouseData[mouseIndex].MouseMovement = value;
    }

    internal static void MouseWheelMovement(int mouseIndex, int value) {
        _MouseData[mouseIndex].MouseWheelMovement = value;
    }

    internal static void Initialize() { }

    internal static void Dispose() { }

    internal static void BeforeFrame() { }
}