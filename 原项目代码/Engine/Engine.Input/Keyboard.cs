#if ANDROID
#pragma warning disable CA1416
using System.Collections.Concurrent;
using Android.App;
using Android.Views;
using Android.Widget;
#elif BROWSER
using Engine.Browser;
#else
using Silk.NET.Input;
#endif

namespace Engine.Input {
    public static class Keyboard {
#if ANDROID
        public struct KeyInfo {
            public Key Key;
            public bool Press;//true：按下，false: 抬起
            public int? UnicodeChar;

            public KeyInfo(Key key, bool press, int? unicodeChar) {
                Key = key;
                Press = press;
                UnicodeChar = unicodeChar;
            }
        }

        public static ConcurrentQueue<KeyInfo> m_cachedKeyEvents = [];
#elif !BROWSER
        public static IKeyboard m_keyboard;
#endif

        public static double m_keyFirstRepeatTime = 0.3;

        public static double m_keyNextRepeatTime = 0.04;

        static bool[] m_keysDownArray = new bool[Enum.GetValues<Key>().Length];

        static bool[] m_keysDownOnceArray = new bool[Enum.GetValues<Key>().Length];

        static double[] m_keysDownRepeatArray = new double[Enum.GetValues<Key>().Length];

        static Key? m_lastKey;

        static char? m_lastChar;

        public static Key? LastKey => m_lastKey;

        public static char? LastChar => m_lastChar;

        public static bool IsKeyboardVisible { get; private set; }

#if ANDROID
        /// <summary>
        /// Whether the native Android AlertDialog is safe to show.
        /// False when VR is active (AlertDialog causes hang on VR headsets).
        /// </summary>
        public static bool IsAndroidDialogAvailable { get; set; } = true;
#endif

        public static bool BackButtonQuitsApp { get; set; }

        public static event Action<Key> KeyDown;

        public static event Action<Key> KeyUp;

        public static event Action<char> CharacterEntered;

        public static bool IsKeyDown(Key key) => m_keysDownArray[(int)key];

        public static bool IsKeyDownOnce(Key key) => m_keysDownOnceArray[(int)key];

        public static bool IsKeyDownRepeat(Key key) {
            double num = m_keysDownRepeatArray[(int)key];
            return num < 0.0 || (num != 0.0 && Time.FrameStartTime >= num);
        }

        public static void ShowKeyboard(string title,
            string description,
            string defaultText,
            bool passwordMode,
            Action<string> enter,
            Action cancel) {
            ArgumentNullException.ThrowIfNull(title);
            ArgumentNullException.ThrowIfNull(description);
            ArgumentNullException.ThrowIfNull(defaultText);
#if ANDROID
            // VR headsets on Android cannot show native AlertDialog (causes hang).
            if (!IsAndroidDialogAvailable) {
                cancel?.Invoke();
                return;
            }
#endif
            if (!IsKeyboardVisible) {
                Clear();
                Touch.Clear();
                Mouse.Clear();
                IsKeyboardVisible = true;
                try {
                    ShowKeyboardInternal(
                        title,
                        description,
                        defaultText,
                        passwordMode,
                        delegate(string text) {
                            Dispatcher.Dispatch(
                                delegate {
                                    IsKeyboardVisible = false;
                                    enter?.Invoke(text ?? string.Empty);
                                }
                            );
                        },
                        delegate {
                            Dispatcher.Dispatch(
                                delegate {
                                    IsKeyboardVisible = false;
                                    cancel?.Invoke();
                                }
                            );
                        }
                    );
                }
                catch {
                    IsKeyboardVisible = false;
                    throw;
                }
            }
        }

        public static void Clear() {
            m_lastKey = null;
            m_lastChar = null;
            for (int i = 0; i < m_keysDownArray.Length; i++) {
                m_keysDownArray[i] = false;
                m_keysDownOnceArray[i] = false;
                m_keysDownRepeatArray[i] = 0.0;
            }
        }

        internal static void BeforeFrame() {
#if ANDROID
            while (!m_cachedKeyEvents.IsEmpty) {
                if (m_cachedKeyEvents.TryDequeue(out KeyInfo keyInfo)) {
                    if (keyInfo.Press) {
                        if (keyInfo.Key != Key.Null) {
                            ProcessKeyDown(keyInfo.Key);
                        }
                        if (keyInfo.UnicodeChar.HasValue) {
                            ProcessCharacterEntered((char)keyInfo.UnicodeChar.Value);
                        }
                    }
                    else if (keyInfo.Key != Key.Null) {
                        ProcessKeyUp(keyInfo.Key);
                    }
                }
                else {
                    Thread.Yield();
                }
            }
#endif
        }

        internal static void AfterFrame() {
            if (BackButtonQuitsApp && (IsKeyDownOnce(Key.Back) || IsKeyDownOnce(Key.Escape))) {
                Window.Close();
            }
            m_lastKey = null;
            m_lastChar = null;
            for (int i = 0; i < m_keysDownOnceArray.Length; i++) {
                m_keysDownOnceArray[i] = false;
            }
            for (int j = 0; j < m_keysDownRepeatArray.Length; j++) {
                if (m_keysDownArray[j]) {
                    if (m_keysDownRepeatArray[j] < 0.0) {
                        m_keysDownRepeatArray[j] = Time.FrameStartTime + m_keyFirstRepeatTime;
                    }
                    else if (Time.FrameStartTime >= m_keysDownRepeatArray[j]) {
                        m_keysDownRepeatArray[j] = Math.Max(Time.FrameStartTime, m_keysDownRepeatArray[j] + m_keyNextRepeatTime);
                    }
                }
                else {
                    m_keysDownRepeatArray[j] = 0.0;
                }
            }
        }

#if ANDROID
        public static void EnqueueMouseButtonEvent(Key key, bool press, int? unicodeChar) {
            m_cachedKeyEvents.Enqueue(new KeyInfo(key, press, unicodeChar));
        }
#endif

        public static bool ProcessKeyDown(Key key) {
            if (!Window.IsActive || IsKeyboardVisible) {
                return false;
            }
            m_lastKey = key;
            if (!m_keysDownArray[(int)key]) {
                m_keysDownArray[(int)key] = true;
                m_keysDownOnceArray[(int)key] = true;
                m_keysDownRepeatArray[(int)key] = -1.0;
            }
            KeyDown?.Invoke(key);
            return true;
        }

        public static bool ProcessKeyUp(Key key) {
            if (!Window.IsActive || IsKeyboardVisible) {
                return false;
            }
            if (m_keysDownArray[(int)key]) {
                m_keysDownArray[(int)key] = false;
            }
            KeyUp?.Invoke(key);
            return true;
        }

        public static bool ProcessCharacterEntered(char ch) {
            if (!Window.IsActive || IsKeyboardVisible) {
                return false;
            }
            m_lastChar = ch;
            CharacterEntered?.Invoke(ch);
            return true;
        }

        internal static void Initialize() {
#if !MOBILE && !BROWSER
            m_keyboard = Window.m_inputContext.Keyboards[0];
            m_keyboard.KeyDown += KeyDownHandler;
            m_keyboard.KeyUp += KeyUpHandler;
            m_keyboard.KeyChar += KeyPressHandler;
#endif
        }

        internal static void Dispose() { }
#if ANDROID
        public static void HandleKeyEvent(KeyEvent keyEvent) {
            EnqueueMouseButtonEvent(TranslateKey(keyEvent.KeyCode), keyEvent.Action == KeyEventActions.Down, keyEvent.UnicodeChar);
        }
#elif !BROWSER
        static void KeyDownHandler(IKeyboard keyboard, Silk.NET.Input.Key key, int scancode) {
            if (scancode == 270
                || key == Silk.NET.Input.Key.Delete) {
                KeyboardInput.DeletePressed = true;
            }
            Key translatedKey = TranslateKey(key);
            if (translatedKey != Key.Null) {
                ProcessKeyDown(translatedKey);
            }
            else if (scancode == 270) {
                ProcessKeyDown(Key.Back);
            }
        }

        static void KeyUpHandler(IKeyboard keyboard, Silk.NET.Input.Key key, int scancode) {
            Key translatedKey = TranslateKey(key);
            if (translatedKey != Key.Null) {
                ProcessKeyUp(translatedKey);
            }
            else if (scancode == 270) {
                ProcessKeyUp(Key.Back);
            }
        }

        static void KeyPressHandler(IKeyboard keyboard, char c) {
            KeyboardInput.Chars.Add(c);
            ProcessCharacterEntered(c);
        }
#endif
#if ANDROID
        public static Key TranslateKey(Keycode keyCode) => keyCode switch {
            Keycode.Home => Key.Home,
            Keycode.Back => Key.Back,
            Keycode.Num0 or Keycode.Numpad0 => Key.Number0,
            Keycode.Num1 or Keycode.Numpad1 => Key.Number1,
            Keycode.Num2 or Keycode.Numpad2 => Key.Number2,
            Keycode.Num3 or Keycode.Numpad3 => Key.Number3,
            Keycode.Num4 or Keycode.Numpad4 => Key.Number4,
            Keycode.Num5 or Keycode.Numpad5 => Key.Number5,
            Keycode.Num6 or Keycode.Numpad6 => Key.Number6,
            Keycode.Num7 or Keycode.Numpad7 => Key.Number7,
            Keycode.Num8 or Keycode.Numpad8 => Key.Number8,
            Keycode.Num9 or Keycode.Numpad9 => Key.Number9,
            Keycode.A => Key.A,
            Keycode.B => Key.B,
            Keycode.C => Key.C,
            Keycode.D => Key.D,
            Keycode.E => Key.E,
            Keycode.F => Key.F,
            Keycode.G => Key.G,
            Keycode.H => Key.H,
            Keycode.I => Key.I,
            Keycode.J => Key.J,
            Keycode.K => Key.K,
            Keycode.L => Key.L,
            Keycode.M => Key.M,
            Keycode.N => Key.N,
            Keycode.O => Key.O,
            Keycode.P => Key.P,
            Keycode.Q => Key.Q,
            Keycode.R => Key.R,
            Keycode.S => Key.S,
            Keycode.T => Key.T,
            Keycode.U => Key.U,
            Keycode.V => Key.V,
            Keycode.W => Key.W,
            Keycode.X => Key.X,
            Keycode.Y => Key.Y,
            Keycode.Z => Key.Z,
            Keycode.Comma => Key.Comma,
            Keycode.Period or Keycode.NumpadDot => Key.Period,
            Keycode.ShiftLeft or Keycode.ShiftRight => Key.Shift,
            Keycode.Tab => Key.Tab,
            Keycode.Space => Key.Space,
            Keycode.Enter or Keycode.NumpadEnter => Key.Enter,
            Keycode.Del => Key.Delete,
            Keycode.Minus or Keycode.NumpadSubtract => Key.Minus,
            Keycode.LeftBracket => Key.LeftBracket,
            Keycode.RightBracket => Key.RightBracket,
            Keycode.Semicolon => Key.Semicolon,
            Keycode.Slash or Keycode.NumpadDivide => Key.Slash,
            Keycode.Backslash => Key.BackSlash,
            Keycode.Equals or Keycode.Plus => Key.Plus,
            Keycode.PageUp => Key.PageUp,
            Keycode.PageDown => Key.PageDown,
            Keycode.Escape => Key.Escape,
            Keycode.ForwardDel => Key.Delete,
            Keycode.CtrlLeft or Keycode.CtrlRight => Key.Control,
            Keycode.CapsLock => Key.CapsLock,
            Keycode.Insert => Key.Insert,
            Keycode.F1 => Key.F1,
            Keycode.F2 => Key.F2,
            Keycode.F3 => Key.F3,
            Keycode.F4 => Key.F4,
            Keycode.F5 => Key.F5,
            Keycode.F6 => Key.F6,
            Keycode.F7 => Key.F7,
            Keycode.F8 => Key.F8,
            Keycode.F9 => Key.F9,
            Keycode.F10 => Key.F10,
            Keycode.F11 => Key.F11,
            Keycode.F12 => Key.F12,
            Keycode.AltLeft or Keycode.AltRight => Key.Alt,
            _ => Key.Null
        };
#elif BROWSER
        public static Key TranslateKey(string code) => code switch {
            "ShiftLeft" or "ShiftRight" => Key.Shift,
            "ControlLeft" or "ControlRight" => Key.Control,
            "F1" => Key.F1,
            "F2" => Key.F2,
            "F3" => Key.F3,
            "F4" => Key.F4,
            "F5" => Key.F5,
            "F6" => Key.F6,
            "F7" => Key.F7,
            "F8" => Key.F8,
            "F9" => Key.F9,
            "F10" => Key.F10,
            "F11" => Key.F11,
            "F12" => Key.F12,
            "ArrowLeft" => Key.LeftArrow,
            "ArrowRight" => Key.RightArrow,
            "ArrowUp" => Key.UpArrow,
            "ArrowDown" => Key.DownArrow,
            "Enter" or "NumpadEnter" => Key.Enter,
            "Escape" => Key.Escape,
            "Space" => Key.Space,
            "Tab" => Key.Tab,
            "Backspace" => Key.BackSpace,
            "Insert" => Key.Insert,
            "Delete" => Key.Delete,
            "PageUp" => Key.PageUp,
            "PageDown" => Key.PageDown,
            "Home" => Key.Home,
            "End" => Key.End,
            "CapsLock" => Key.CapsLock,
            "KeyA" => Key.A,
            "KeyB" => Key.B,
            "KeyC" => Key.C,
            "KeyD" => Key.D,
            "KeyE" => Key.E,
            "KeyF" => Key.F,
            "KeyG" => Key.G,
            "KeyH" => Key.H,
            "KeyI" => Key.I,
            "KeyJ" => Key.J,
            "KeyK" => Key.K,
            "KeyL" => Key.L,
            "KeyM" => Key.M,
            "KeyN" => Key.N,
            "KeyO" => Key.O,
            "KeyP" => Key.P,
            "KeyQ" => Key.Q,
            "KeyR" => Key.R,
            "KeyS" => Key.S,
            "KeyT" => Key.T,
            "KeyU" => Key.U,
            "KeyV" => Key.V,
            "KeyW" => Key.W,
            "KeyX" => Key.X,
            "KeyY" => Key.Y,
            "KeyZ" => Key.Z,
            "Numpad0" or "Digit0" => Key.Number0,
            "Numpad1" or "Digit1" => Key.Number1,
            "Numpad2" or "Digit2" => Key.Number2,
            "Numpad3" or "Digit3" => Key.Number3,
            "Numpad4" or "Digit4" => Key.Number4,
            "Numpad5" or "Digit5" => Key.Number5,
            "Numpad6" or "Digit6" => Key.Number6,
            "Numpad7" or "Digit7" => Key.Number7,
            "Numpad8" or "Digit8" => Key.Number8,
            "Numpad9" or "Digit9" => Key.Number9,
            "Backquote" => Key.Tilde,
            "Minus" or "NumpadSubtract" => Key.Minus,
            "Equal" or "NumpadAdd" => Key.Plus,
            "BracketLeft" => Key.LeftBracket,
            "BracketRight" => Key.RightBracket,
            "Semicolon" => Key.Semicolon,
            "Quote" => Key.Quote,
            "Comma" => Key.Comma,
            "Period" or "NumpadDecimal" => Key.Period,
            "Slash" or "NumpadDivide" => Key.Slash,
            "AltLeft" or "AltRight" => Key.Alt,
            "Backslash" => Key.BackSlash,
            _ => Key.Null
        };
#else
        public static Key TranslateKey(Silk.NET.Input.Key key) => key switch {
            Silk.NET.Input.Key.ShiftLeft or Silk.NET.Input.Key.ShiftRight => Key.Shift,
            Silk.NET.Input.Key.ControlLeft or Silk.NET.Input.Key.ControlRight => Key.Control,
            Silk.NET.Input.Key.F1 => Key.F1,
            Silk.NET.Input.Key.F2 => Key.F2,
            Silk.NET.Input.Key.F3 => Key.F3,
            Silk.NET.Input.Key.F4 => Key.F4,
            Silk.NET.Input.Key.F5 => Key.F5,
            Silk.NET.Input.Key.F6 => Key.F6,
            Silk.NET.Input.Key.F7 => Key.F7,
            Silk.NET.Input.Key.F8 => Key.F8,
            Silk.NET.Input.Key.F9 => Key.F9,
            Silk.NET.Input.Key.F10 => Key.F10,
            Silk.NET.Input.Key.F11 => Key.F11,
            Silk.NET.Input.Key.F12 => Key.F12,
            Silk.NET.Input.Key.Up => Key.UpArrow,
            Silk.NET.Input.Key.Down => Key.DownArrow,
            Silk.NET.Input.Key.Left => Key.LeftArrow,
            Silk.NET.Input.Key.Right => Key.RightArrow,
            Silk.NET.Input.Key.Enter or Silk.NET.Input.Key.KeypadEnter => Key.Enter,
            Silk.NET.Input.Key.Escape => Key.Escape,
            Silk.NET.Input.Key.Space => Key.Space,
            Silk.NET.Input.Key.Tab => Key.Tab,
            Silk.NET.Input.Key.Backspace => Key.BackSpace,
            Silk.NET.Input.Key.Insert => Key.Insert,
            Silk.NET.Input.Key.Delete => Key.Delete,
            Silk.NET.Input.Key.PageUp => Key.PageUp,
            Silk.NET.Input.Key.PageDown => Key.PageDown,
            Silk.NET.Input.Key.Home => Key.Home,
            Silk.NET.Input.Key.End => Key.End,
            Silk.NET.Input.Key.CapsLock => Key.CapsLock,
            Silk.NET.Input.Key.A => Key.A,
            Silk.NET.Input.Key.B => Key.B,
            Silk.NET.Input.Key.C => Key.C,
            Silk.NET.Input.Key.D => Key.D,
            Silk.NET.Input.Key.E => Key.E,
            Silk.NET.Input.Key.F => Key.F,
            Silk.NET.Input.Key.G => Key.G,
            Silk.NET.Input.Key.H => Key.H,
            Silk.NET.Input.Key.I => Key.I,
            Silk.NET.Input.Key.J => Key.J,
            Silk.NET.Input.Key.K => Key.K,
            Silk.NET.Input.Key.L => Key.L,
            Silk.NET.Input.Key.M => Key.M,
            Silk.NET.Input.Key.N => Key.N,
            Silk.NET.Input.Key.O => Key.O,
            Silk.NET.Input.Key.P => Key.P,
            Silk.NET.Input.Key.Q => Key.Q,
            Silk.NET.Input.Key.R => Key.R,
            Silk.NET.Input.Key.S => Key.S,
            Silk.NET.Input.Key.T => Key.T,
            Silk.NET.Input.Key.U => Key.U,
            Silk.NET.Input.Key.V => Key.V,
            Silk.NET.Input.Key.W => Key.W,
            Silk.NET.Input.Key.X => Key.X,
            Silk.NET.Input.Key.Y => Key.Y,
            Silk.NET.Input.Key.Z => Key.Z,
            Silk.NET.Input.Key.Number0 or Silk.NET.Input.Key.Keypad0 => Key.Number0,
            Silk.NET.Input.Key.Number1 or Silk.NET.Input.Key.Keypad1 => Key.Number1,
            Silk.NET.Input.Key.Number2 or Silk.NET.Input.Key.Keypad2 => Key.Number2,
            Silk.NET.Input.Key.Number3 or Silk.NET.Input.Key.Keypad3 => Key.Number3,
            Silk.NET.Input.Key.Number4 or Silk.NET.Input.Key.Keypad4 => Key.Number4,
            Silk.NET.Input.Key.Number5 or Silk.NET.Input.Key.Keypad5 => Key.Number5,
            Silk.NET.Input.Key.Number6 or Silk.NET.Input.Key.Keypad6 => Key.Number6,
            Silk.NET.Input.Key.Number7 or Silk.NET.Input.Key.Keypad7 => Key.Number7,
            Silk.NET.Input.Key.Number8 or Silk.NET.Input.Key.Keypad8 => Key.Number8,
            Silk.NET.Input.Key.Number9 or Silk.NET.Input.Key.Keypad9 => Key.Number9,
            Silk.NET.Input.Key.GraveAccent => Key.Tilde,
            Silk.NET.Input.Key.Minus or Silk.NET.Input.Key.KeypadSubtract => Key.Minus,
            Silk.NET.Input.Key.Equal or Silk.NET.Input.Key.KeypadAdd => Key.Plus,
            Silk.NET.Input.Key.LeftBracket => Key.LeftBracket,
            Silk.NET.Input.Key.RightBracket => Key.RightBracket,
            Silk.NET.Input.Key.Semicolon => Key.Semicolon,
            Silk.NET.Input.Key.Apostrophe => Key.Quote,
            Silk.NET.Input.Key.Comma => Key.Comma,
            Silk.NET.Input.Key.Period or Silk.NET.Input.Key.KeypadDecimal => Key.Period,
            Silk.NET.Input.Key.Slash or Silk.NET.Input.Key.KeypadDivide => Key.Slash,
            Silk.NET.Input.Key.BackSlash => Key.BackSlash,
            Silk.NET.Input.Key.AltLeft or Silk.NET.Input.Key.AltRight => Key.Alt,
            _ => Key.Null
        };
#endif
        // ReSharper disable UnusedParameter.Local
        public static void ShowKeyboardInternal(string title,
            string description,
            string defaultText,
            bool passwordMode,
            Action<string> enter,
            Action cancel) {
        // ReSharper restore UnusedParameter.Local
#if ANDROID
            AlertDialog.Builder builder = new(Window.Activity);
            builder.SetTitle(title);
            builder.SetMessage(description);
            EditText editText = new(Window.Activity);
            editText.Text = defaultText;
            if (passwordMode) {
                editText.InputType = Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationPassword;
            }
            builder.SetView(editText);
            builder.SetPositiveButton("Ok", delegate { enter(editText.Text); });
            builder.SetNegativeButton("Cancel", delegate { cancel(); });
            Window.Activity.RunOnUiThread(() => {
                    AlertDialog alertDialog = builder.Create();
                    if (alertDialog == null) {
                        return;
                    }
                    alertDialog.Window?.SetSoftInputMode(SoftInput.StateVisible);
                    alertDialog.DismissEvent += delegate { cancel(); };
                    alertDialog.CancelEvent += delegate { cancel(); };
                    alertDialog.Window?.Attributes?.Gravity = GravityFlags.Center;
                    alertDialog.Show();
                    editText.RequestFocus();
                }
            );
#elif BROWSER
            Task.Run(() => {
                string input = BrowserInterop.ShowKeyboard(title, defaultText);
                if (input == null) {
                    cancel();
                }
                else {
                    enter(input);
                }
            });
#else
            cancel();
#endif
        }
    }
}