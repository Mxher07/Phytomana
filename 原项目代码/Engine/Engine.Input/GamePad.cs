#if ANDROID
#pragma warning disable CA1416
using System.Collections.Concurrent;
using Android.Views;
using Axis = Android.Views.Axis;
#elif BROWSER
using Engine.Browser;
#else
using Silk.NET.Input;
#endif
namespace Engine.Input {
    public static class GamePad {
        internal class State {
            // ReSharper disable MemberHidesStaticFromOuterClass
#pragma warning disable CS0649
            public bool IsConnected;
#pragma warning restore CS0649
            // ReSharper restore MemberHidesStaticFromOuterClass

            public Vector2[] Sticks = new Vector2[2];

            public float[] Triggers = new float[2];

            public float[] LastTriggers = new float[2];

            public bool[] Buttons = new bool[14];

            public bool[] LastButtons = new bool[14];

            public double[] ButtonsRepeat = new double[14];

            public object ModifierKeyOfCurrentCombo; //记录按下的组合键中的修饰键，避免弹起误触
        }
#if ANDROID
        public struct KeyInfo {
            public int GamepadIndex;
            public GamePadButton Button;
            public bool Press;

            public KeyInfo(int gamepadIndex, GamePadButton button, bool press) {
                GamepadIndex = gamepadIndex;
                Button = button;
                Press = press;
            }
        }

        public struct TriggerInfo {
            public int GamepadIndex;
            public GamePadTrigger Trigger;
            public float Value;

            public TriggerInfo(int gamepadIndex, GamePadTrigger trigger, float value) {
                GamepadIndex = gamepadIndex;
                Trigger = trigger;
                Value = value;
            }
        }

        public static Dictionary<int, int> m_deviceToIndex = [];
        public static List<int> m_deviceToRemove = [];
        public static ConcurrentQueue<KeyInfo> m_cachedKeyEvents = [];
        public static ConcurrentQueue<TriggerInfo> m_cachedTriggerEvents = [];
        public static readonly bool[,] m_lastDpadStates = new bool[4, 4];
        static readonly bool[,] m_dpadFromKey = new bool[4, 4];
        static readonly bool[,] m_lastTriggerDown = new bool[4, 2];

        const float TRIGGER_DOWN_THRESHOLD = 0.5f;
        const float TRIGGER_UP_THRESHOLD = 0.4f;
#elif !IOS && !BROWSER
        public static IReadOnlyList<IGamepad> m_gamepads;
#endif
        public static double m_buttonFirstRepeatTime = 0.2;

        public static double m_buttonNextRepeatTime = 0.04;

        internal static State[] m_states = [new(), new(), new(), new()];

        internal static void Initialize() {
#if !MOBILE && !BROWSER
            m_gamepads = Window.m_inputContext.Gamepads;
#endif
        }

        internal static void Dispose() { }

#if ANDROID
        internal static void BeforeFrame() {
            if (Time.PeriodicEvent(2.0, 0.0)) {
                m_deviceToRemove.Clear();
                foreach (int key in m_deviceToIndex.Keys) {
                    if (InputDevice.GetDevice(key) == null) {
                        m_deviceToRemove.Add(key);
                    }
                }
                foreach (int item in m_deviceToRemove) {
                    Disconnect(item);
                }
            }
            while (m_cachedKeyEvents.TryDequeue(out KeyInfo keyInfo)) {
                if (keyInfo.Press) {
                    HandleKeyDown(keyInfo.GamepadIndex, keyInfo.Button);
                }
                else {
                    HandleKeyUp(keyInfo.GamepadIndex, keyInfo.Button);
                }
            }
            while (m_cachedTriggerEvents.TryDequeue(out TriggerInfo info)) {
                int num = TranslateDeviceId(info.GamepadIndex);
                if (num >= 0) {
                    // 在这里更新 Triggers，此时它是当前帧的最新值
                    m_states[num].Triggers[(int)info.Trigger] = info.Value;
                }
            }
        }

        public static void HandleKeyEvent(KeyEvent e) {
            m_cachedKeyEvents.Enqueue(new KeyInfo(TranslateDeviceId(e.DeviceId), TranslateKey(e.KeyCode), e.Action == KeyEventActions.Down));
        }

        internal static void HandleKeyDown(int gamepadIndex, GamePadButton gamePadButton) {
            if (gamepadIndex < 0) {
                return;
            }
            if (gamePadButton >= GamePadButton.A) {
                if (gamePadButton >= GamePadButton.DPadLeft
                    && gamePadButton <= GamePadButton.DPadDown) {
                    int idx = gamePadButton switch {
                        GamePadButton.DPadLeft => 0,
                        GamePadButton.DPadRight => 1,
                        GamePadButton.DPadUp => 2,
                        GamePadButton.DPadDown => 3,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    m_dpadFromKey[gamepadIndex, idx] = true;
                }
                m_states[gamepadIndex].Buttons[(int)gamePadButton] = true;
                switch (gamePadButton) {
                    case GamePadButton.LeftShoulder: m_states[gamepadIndex].Triggers[0] = 1f; break;
                    case GamePadButton.RightShoulder: m_states[gamepadIndex].Triggers[1] = 1f; break;
                }
            }
        }

        internal static void HandleKeyUp(int gamepadIndex, GamePadButton gamePadButton) {
            if (gamepadIndex < 0) {
                return;
            }
            if (gamePadButton >= GamePadButton.A) {
                if (gamePadButton >= GamePadButton.DPadLeft
                    && gamePadButton <= GamePadButton.DPadDown) {
                    int idx = gamePadButton switch {
                        GamePadButton.DPadLeft => 0,
                        GamePadButton.DPadRight => 1,
                        GamePadButton.DPadUp => 2,
                        GamePadButton.DPadDown => 3,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    m_dpadFromKey[gamepadIndex, idx] = false;
                }
                m_states[gamepadIndex].Buttons[(int)gamePadButton] = false;
                switch (gamePadButton) {
                    case GamePadButton.LeftShoulder: m_states[gamepadIndex].Triggers[0] = 0f; break;
                    case GamePadButton.RightShoulder: m_states[gamepadIndex].Triggers[1] = 0f; break;
                }
            }
        }

        internal static void HandleMotionEvent(MotionEvent e) {
            int gamepadIndex = TranslateDeviceId(e.DeviceId);
            if (gamepadIndex >= 0) {
                m_states[gamepadIndex].Sticks[0] = new Vector2(e.GetAxisValue(Axis.X), 0f - e.GetAxisValue(Axis.Y));
                m_states[gamepadIndex].Sticks[1] = new Vector2(e.GetAxisValue(Axis.Z), 0f - e.GetAxisValue(Axis.Rz));
                float l = MathF.Max(e.GetAxisValue(Axis.Ltrigger), e.GetAxisValue(Axis.Brake));
                float r = MathF.Max(e.GetAxisValue(Axis.Rtrigger), e.GetAxisValue(Axis.Gas));
                m_cachedTriggerEvents.Enqueue(new TriggerInfo(TranslateDeviceId(e.DeviceId), GamePadTrigger.Left, l));
                m_cachedTriggerEvents.Enqueue(new TriggerInfo(TranslateDeviceId(e.DeviceId), GamePadTrigger.Right, r));
                float axisX = e.GetAxisValue(Axis.HatX);
                float axisY = e.GetAxisValue(Axis.HatY);
                ProcessDpad(gamepadIndex, gamepadIndex, 0, axisX < -0.5f, GamePadButton.DPadLeft);
                ProcessDpad(gamepadIndex, gamepadIndex, 1, axisX > 0.5f, GamePadButton.DPadRight);
                ProcessDpad(gamepadIndex, gamepadIndex, 2, axisY < -0.5f, GamePadButton.DPadUp);
                ProcessDpad(gamepadIndex, gamepadIndex, 3, axisY > 0.5f, GamePadButton.DPadDown);
            }
        }

        public static void ProcessDpad(int gamepadIndex, int padIndex, int dpadIndex, bool current, GamePadButton button) {
            if (m_dpadFromKey[padIndex, dpadIndex]) {
                return;
            }
            bool last = m_lastDpadStates[padIndex, dpadIndex];
            if (current == last) {
                return;
            }
            m_lastDpadStates[padIndex, dpadIndex] = current;
            m_cachedKeyEvents.Enqueue(new KeyInfo(gamepadIndex, button, current));
        }

        public static int TranslateDeviceId(int deviceId) {
            if (m_deviceToIndex.TryGetValue(deviceId, out int value)) {
                return value;
            }
            for (int i = 0; i < 4; i++) {
                if (!m_deviceToIndex.Values.Contains(i)) {
                    Connect(deviceId, i);
                    return i;
                }
            }
            return -1;
        }

        public static GamePadButton TranslateKey(Keycode keyCode) => keyCode switch {
            Keycode.ButtonA => GamePadButton.A,
            Keycode.ButtonB => GamePadButton.B,
            Keycode.ButtonX => GamePadButton.X,
            Keycode.ButtonY => GamePadButton.Y,
            Keycode.Back => GamePadButton.Back,
            Keycode.ButtonL1 => GamePadButton.LeftShoulder,
            Keycode.ButtonR1 => GamePadButton.RightShoulder,
            Keycode.ButtonThumbl => GamePadButton.LeftThumb,
            Keycode.ButtonThumbr => GamePadButton.RightThumb,
            Keycode.DpadLeft => GamePadButton.DPadLeft,
            Keycode.DpadRight => GamePadButton.DPadRight,
            Keycode.DpadUp => GamePadButton.DPadUp,
            Keycode.DpadDown => GamePadButton.DPadDown,
            Keycode.ButtonSelect => GamePadButton.Back,
            Keycode.ButtonStart => GamePadButton.Start,
            _ => (GamePadButton)(-1)
        };

        public static void Connect(int deviceId, int index) {
            m_deviceToIndex.Add(deviceId, index);
            m_states[index].IsConnected = true;
        }

        public static void Disconnect(int deviceId) {
            if (m_deviceToIndex.Remove(deviceId, out int value)) {
                m_states[value].IsConnected = false;
            }
        }
#elif IOS || BROWSER
        internal static void BeforeFrame() {}
#else
        internal static void BeforeFrame() {
            for (int padIndex = 0; padIndex < 4; padIndex++) {
                if (padIndex >= m_gamepads.Count) {
                    break;
                }
                IGamepad gamepad = m_gamepads[padIndex];
                if (gamepad == null) {
                    continue;
                }
                string name = gamepad.Name;
                if (!name.Contains("Unmapped")) {
                    State state = m_states[padIndex];
                    if (gamepad.IsConnected) {
                        state.IsConnected = true;
                        if (Window.IsActive) {
                            IReadOnlyList<Thumbstick> thumbsticks = gamepad.Thumbsticks;
                            for (int i = 0; i < 2; i++) {
                                state.Sticks[i] = new Vector2(thumbsticks[i].X, -thumbsticks[i].Y);
                            }
                            IReadOnlyList<Trigger> triggers = gamepad.Triggers;
                            for (int i = 0; i < 2; i++) {
                                state.Triggers[i] = triggers[i].Position;
                            }
                            foreach (Button button in gamepad.Buttons) {
                                switch (button.Name) {
                                    case ButtonName.A: state.Buttons[0] = button.Pressed; break;
                                    case ButtonName.B: state.Buttons[1] = button.Pressed; break;
                                    case ButtonName.X: state.Buttons[2] = button.Pressed; break;
                                    case ButtonName.Y: state.Buttons[3] = button.Pressed; break;
                                    case ButtonName.Back: state.Buttons[4] = button.Pressed; break;
                                    case ButtonName.Start: state.Buttons[5] = button.Pressed; break;
                                    case ButtonName.LeftStick: state.Buttons[6] = button.Pressed; break;
                                    case ButtonName.RightStick: state.Buttons[7] = button.Pressed; break;
                                    case ButtonName.LeftBumper: state.Buttons[8] = button.Pressed; break;
                                    case ButtonName.RightBumper: state.Buttons[9] = button.Pressed; break;
                                    case ButtonName.DPadLeft: state.Buttons[10] = button.Pressed; break;
                                    case ButtonName.DPadRight: state.Buttons[12] = button.Pressed; break;
                                    case ButtonName.DPadUp: state.Buttons[11] = button.Pressed; break;
                                    case ButtonName.DPadDown: state.Buttons[13] = button.Pressed; break;
                                }
                            }
                        }
                    }
                    else {
                        state.IsConnected = false;
                    }
                }
            }
        }
#endif

        public static bool IsConnected(int gamePadIndex) => gamePadIndex < 0 || gamePadIndex >= m_states.Length
            ? throw new ArgumentOutOfRangeException(nameof(gamePadIndex))
            : m_states[gamePadIndex].IsConnected;

        public static Vector2 GetStickPosition(int gamePadIndex, GamePadStick stick, float deadZone = 0f) {
            if (deadZone < 0f
                || deadZone >= 1f) {
                throw new ArgumentOutOfRangeException(nameof(deadZone));
            }
            if (IsConnected(gamePadIndex)) {
                Vector2 result = m_states[gamePadIndex].Sticks[(int)stick];
                if (deadZone > 0f) {
                    float num = result.Length();
                    if (num > 0f) {
                        float num2 = ApplyDeadZone(num, deadZone);
                        result *= num2 / num;
                    }
                }
                return result;
            }
            return Vector2.Zero;
        }

        public static float GetTriggerPosition(int gamePadIndex, GamePadTrigger trigger, float deadZone = 0f) => deadZone < 0f || deadZone >= 1f ?
            throw new ArgumentOutOfRangeException(nameof(deadZone)) :
            IsConnected(gamePadIndex) ? ApplyDeadZone(m_states[gamePadIndex].Triggers[(int)trigger], deadZone) : 0f;

        public static bool IsTriggerDown(int gamePadIndex, GamePadTrigger trigger, float deadZone = 0f, float threshold = 0.5f) {
            if (deadZone < 0f
                || deadZone >= 1f) {
                throw new ArgumentOutOfRangeException(nameof(deadZone));
            }
            if (!IsConnected(gamePadIndex)) {
                return false;
            }
            float value = ApplyDeadZone(m_states[gamePadIndex].Triggers[(int)trigger], deadZone);
            return value >= threshold;
        }

        public static bool IsTriggerDownOnce(int gamePadIndex, GamePadTrigger trigger, float deadZone = 0f, float threshold = 0.5f) {
            if (deadZone < 0f
                || deadZone >= 1f) {
                throw new ArgumentOutOfRangeException(nameof(deadZone));
            }
            if (!IsConnected(gamePadIndex)) {
                return false;
            }
            if (m_states[gamePadIndex].ModifierKeyOfCurrentCombo is GamePadTrigger trigger1
                && trigger1 == trigger) {
                return false; //若修饰键按下期间已触发组合键，禁止当前修饰键触发自己的点按行为，避免误触
            }
            bool current = ApplyDeadZone(m_states[gamePadIndex].Triggers[(int)trigger], deadZone) >= threshold;
            bool last = ApplyDeadZone(m_states[gamePadIndex].LastTriggers[(int)trigger], deadZone) >= threshold;
            return !current && last; //扳机必定是修饰键，松开那一刻才算按下一次，避免影响组合键
        }

        public static bool IsButtonDown(int gamePadIndex, GamePadButton button) =>
            IsConnected(gamePadIndex) && m_states[gamePadIndex].Buttons[(int)button];

        public static bool IsButtonDownOnce(int gamePadIndex, GamePadButton button) {
            if (!IsConnected(gamePadIndex)) {
                return false;
            }
            if (m_states[gamePadIndex].ModifierKeyOfCurrentCombo is GamePadButton button1
                && button1 == button) {
                return false; //若修饰键按下期间已触发组合键，禁止当前修饰键触发自己的点按行为，避免误触
            }
            if (IsModifierKey(button)) { //如果是修饰键，松开那一刻才算按下一次，避免影响组合键
                return !m_states[gamePadIndex].Buttons[(int)button] && m_states[gamePadIndex].LastButtons[(int)button];
            }
            //正常按键依然是按下那一刻算按下一次
            return m_states[gamePadIndex].Buttons[(int)button] && !m_states[gamePadIndex].LastButtons[(int)button];
        }

        public static bool IsButtonDownRepeat(int gamePadIndex, GamePadButton button) {
            if (IsConnected(gamePadIndex)) {
                if (m_states[gamePadIndex].Buttons[(int)button]
                    && !m_states[gamePadIndex].LastButtons[(int)button]) {
                    return true;
                }
                double num = m_states[gamePadIndex].ButtonsRepeat[(int)button];
                return num != 0.0 && Time.FrameStartTime >= num;
            }
            return false;
        }

        public static bool IsAnyModifierKeyHolding(int gamePadIndex, float threshold = 0.5f) {
            if (!IsConnected(gamePadIndex)) {
                return false;
            }
            State state = m_states[gamePadIndex];
            return state.Triggers[0] >= threshold
                || state.Triggers[1] >= threshold
                || state.Buttons[(int)GamePadButton.LeftShoulder]
                || state.Buttons[(int)GamePadButton.RightShoulder];
        }

        public static void SetModifierKeyOfCurrentCombo(int gamePadIndex, object modifierKey) {
            if (!IsConnected(gamePadIndex)) {
                return;
            }
            if (IsModifierKey(modifierKey)) {
                m_states[gamePadIndex].ModifierKeyOfCurrentCombo = modifierKey;
            }
        }

        public static bool IsModifierKey(object obj) => obj is GamePadTrigger
            || (obj is GamePadButton button && (button == GamePadButton.LeftShoulder || button == GamePadButton.RightShoulder));

        //        /// <summary>
        //        /// 使指定的手柄的马达震动
        //        /// </summary>
        //        /// <param name="gamePadIndex"></param>
        //        /// <param name="vibration">震动幅度(马达速度)，在0到1之间</param>
        //        /// <param name="durationMs">震动持续时间(毫秒)</param>
        //        public static void MakeVibration(int gamePadIndex, float vibration, float durationMs)
        //        {//由于GLFW不支持手柄震动，所以暂时注释掉这段代码
        //#if !ANDROID
        //            if (IsConnected(gamePadIndex))
        //            {
        //                var gamePad = m_gamepads[gamePadIndex];
        //                foreach(var motor in gamePad.VibrationMotors)
        //                {
        //                    motor.Speed = vibration;
        //                    Task.Delay((int)durationMs).ContinueWith(_ =>
        //                    {
        //                        motor.Speed = 0f;
        //                    });
        //                }
        //            }
        //#endif
        //        }
        public static void Clear() {
            for (int i = 0; i < m_states.Length; i++) {
                for (int j = 0; j < m_states[i].Sticks.Length; j++) {
                    m_states[i].Sticks[j] = Vector2.Zero;
                }
                for (int k = 0; k < m_states[i].Triggers.Length; k++) {
                    m_states[i].Triggers[k] = 0f;
                }
                for (int l = 0; l < m_states[i].Buttons.Length; l++) {
                    m_states[i].Buttons[l] = false;
                    m_states[i].ButtonsRepeat[l] = 0.0;
                }
            }
        }

        internal static void AfterFrame() {
            for (int i = 0; i < m_states.Length; i++) {
                if (Keyboard.BackButtonQuitsApp
                    && IsButtonDownOnce(i, GamePadButton.Back)) {
                    Window.Close();
                }
                State state = m_states[i];
                for (int j = 0; j < state.Buttons.Length; j++) {
                    if (state.Buttons[j]) {
                        if (!state.LastButtons[j]) {
                            state.ButtonsRepeat[j] = Time.FrameStartTime + m_buttonFirstRepeatTime;
                        }
                        else if (Time.FrameStartTime >= state.ButtonsRepeat[j]) {
                            state.ButtonsRepeat[j] = Math.Max(Time.FrameStartTime, state.ButtonsRepeat[j] + m_buttonNextRepeatTime);
                        }
                    }
                    else {
                        state.ButtonsRepeat[j] = 0.0;
                    }
                    state.LastButtons[j] = state.Buttons[j];
                }
                for (int k = 0; k < state.Triggers.Length; k++) {
                    state.LastTriggers[k] = state.Triggers[k];
                }
                if (!IsAnyModifierKeyHolding(i, 0.08f)) { //所有修饰键都松开时，重置组合键触发标记
                    state.ModifierKeyOfCurrentCombo = null;
                }
            }
        }

        public static float ApplyDeadZone(float value, float deadZone) =>
            MathF.Sign(value) * MathF.Max(MathF.Abs(value) - deadZone, 0f) / (1f - deadZone);
#if BROWSER
        public static void GamepadConnectedHandler(int index, string name) {
            if (index < 0 || index >= m_states.Length) {
                return;
            }
            m_states[index].IsConnected = true;
        }

        public static void GamepadDisconnectedHandler(int index) {
            if (index < 0 || index >= m_states.Length) {
                return;
            }
            m_states[index].IsConnected = false;
        }
#endif
    }
}