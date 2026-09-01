namespace Game {
    [Flags]
    public enum WidgetInputDevice {
        None = 0,
        Keyboard = 1,
        MultiKeyboard1 = 1 << 1,
        MultiKeyboard2 = 1 << 2,
        MultiKeyboard3 = 1 << 3,
        MultiKeyboard4 = 1 << 4,
        Mouse = 1 << 5,
        MultiMouse1 = 1 << 6,
        MultiMouse2 = 1 << 7,
        MultiMouse3 = 1 << 8,
        MultiMouse4 = 1 << 9,
        Touch = 1 << 10,
        GamePad1 = 1 << 11,
        GamePad2 = 1 << 12,
        GamePad3 = 1 << 13,
        GamePad4 = 1 << 14,
        VrControllers = 1 << 15,
        MultiKeyboards = MultiKeyboard1 | MultiKeyboard2 | MultiKeyboard3 | MultiKeyboard4,
        MultiMice = MultiMouse1 | MultiMouse2 | MultiMouse3 | MultiMouse4,
        Gamepads = GamePad1 | GamePad2 | GamePad3 | GamePad4,
        All = Keyboard | Mouse | Touch | Gamepads | VrControllers
    }
}