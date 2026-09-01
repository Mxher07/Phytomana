namespace Engine.Browser {
    public enum InputEventType : byte {
        None = 0,
        // --- 4 字节事件 ---
        // Param: Code
        KeyDown = 1, // Payload: Char
        KeyUp = 2,
        // Param: ButtonIndex, Payload: GamepadIndex
        GamepadButtonDown = 3,
        GamepadButtonUp = 4,
        GamepadConnected = 5, // 未使用，还是走 BrowserInterop
        GamepadDisconnected = 6,
        //其他
        FocusChange = 64, // Param: 0 不可见，1 可见
        FullscreenChange = 65, // Param: 0 退出全屏，1 进入全屏


        // --- 12 字节事件 (有 float X, Y) ---
        // Param: ButtonIndex
        MouseDown = 128,
        MouseUp = 129,
        MouseMove = 130, // X、Y 是移动距离
        MouseWheel = 131,
        // Param: pointerId
        TouchDown = 132,
        TouchUp = 133,
        TouchMove = 134,
    }
}