#if ANDROID
using System.Collections.Concurrent;
using Android.App;
using Android.OS;
using Android.Views;
#pragma warning disable CA1416
#elif BROWSER
using Engine.Browser;
#elif !IOS
using Silk.NET.Input;
#endif

namespace Engine.Input {
    public static class Mouse {
#if ANDROID
        public struct MouseButtonInfo {
            public MouseButton Button;
            public bool Press;//true：按下，false: 抬起
            public Point2 Position;

            public MouseButtonInfo(MouseButton button, bool press, Point2 position) {
                Button = button;
                Press = press;
                Position = position;
            }
        }

        public static ConcurrentQueue<MouseButtonInfo> m_cachedMouseButtonEvents = [];
        static Vector2 m_queuedMouseMovement;
        static float m_queuedMouseWheelMovement;
        static float m_queuedMouseWheelMovementX;
        static bool m_pointerCaptureRequested;
#elif BROWSER
        internal static Point2 m_currentMousePosition;
        internal static Vector2 m_queuedMouseMovement;
        internal static float m_queuedMouseWheelMovement;
        internal static float m_queuedMouseWheelMovementX;
        static bool m_pointerCaptureRequested;
#elif !IOS
        public static IMouse m_mouse;
#endif
        public static Point2? m_lastMousePosition;

        static bool[] m_mouseButtonsDownArray;

        static int[] m_mouseButtonsDownFrameArray;

        static bool[] m_mouseButtonsDelayedUpArray;

        static bool[] m_mouseButtonsDownOnceArray;

        static bool[] m_mouseButtonsUpOnceArray;

        public static Point2 MouseMovement { get; private set; }

        /// <summary>
        /// 鼠标滚轮纵向滚动
        /// </summary>
        public static int MouseWheelMovement { get; private set; }

        /// <summary>
        /// 鼠标滚轮横向滚动
        /// </summary>
        public static int MouseWheelMovementX { get; private set; }

        public static Point2? MousePosition { get; private set; }

        public static bool IsMouseVisible { get; set; }

        public static event Action<MouseEvent> MouseMove;

        public static event Action<MouseButtonEvent> MouseDown;

        public static event Action<MouseButtonEvent> MouseUp;

        public static void SetMousePosition(int x, int y) {
#if !MOBILE && !BROWSER
            m_mouse.Position = new System.Numerics.Vector2(x, y);
#endif
        }

        internal static void Initialize() {
#if ANDROID
            if (Build.VERSION.SdkInt >= (BuildVersionCodes)26) {
                Window.m_surface.SetOnCapturedPointerListener(new OnCapturedPointerListener());
            }
#elif !IOS && !BROWSER
            m_mouse = Window.m_inputContext.Mice[0];
            m_mouse.MouseDown += MouseDownHandler;
            m_mouse.MouseUp += MouseUpHandler;
            //m_mouse.MouseMove += MouseMoveHandler;
            m_mouse.Scroll += MouseWheelHandler;
#endif
        }

        internal static void Dispose() { }

        internal static void BeforeFrame() {
#if ANDROID
            if (IsMouseVisible) {
                if (m_pointerCaptureRequested) {
                    m_pointerCaptureRequested = false;
                    if (Build.VERSION.SdkInt >= (BuildVersionCodes)26) {
                        Window.m_surface?.ReleasePointerCapture();
                    }
                    Clear();
                }
                MouseMovement = Point2.Zero;
                m_lastMousePosition = null;
            }
            else {
                if (!m_pointerCaptureRequested) {
                    m_pointerCaptureRequested = true;
                    if (Build.VERSION.SdkInt >= (BuildVersionCodes)26) {
                        Window.m_surface?.RequestPointerCapture();
                    }
                }
                if (m_lastMousePosition.HasValue) {
                    MouseMovement = Point2.Round(m_queuedMouseMovement.X, m_queuedMouseMovement.Y);
                }
                //安卓端m_lastMousePosition只用来表示是不是鼠标不可见后的第一帧
                m_lastMousePosition = Point2.Zero;
                m_queuedMouseMovement = Vector2.Zero;
            }
            MouseWheelMovement = (int)MathUtils.Round(m_queuedMouseWheelMovement) * 120;
            m_queuedMouseWheelMovement = 0f;
            MouseWheelMovementX = (int)MathUtils.Round(m_queuedMouseWheelMovementX) * 120;
            m_queuedMouseWheelMovementX = 0f;
            while (!m_cachedMouseButtonEvents.IsEmpty) {
                if (m_cachedMouseButtonEvents.TryDequeue(out MouseButtonInfo buttonInfo)) {
                    if (buttonInfo.Press) {
                        ProcessMouseDown(buttonInfo.Button, buttonInfo.Position);
                    }
                    else {
                        ProcessMouseUp(buttonInfo.Button, buttonInfo.Position);
                    }
                }
                else {
                    Thread.Yield();
                }
            }
#elif BROWSER
            if (Window.IsActive) {
                ProcessMouseMove(m_currentMousePosition);
                if (IsMouseVisible) {
                    if (m_pointerCaptureRequested) {
                        m_pointerCaptureRequested = false;
                        BrowserInterop.SetNeedPointerLock(false);
                    }
                    MouseMovement = Point2.Zero;
                    m_lastMousePosition = null;
                }
                else {
                    if (!m_pointerCaptureRequested) {
                        m_pointerCaptureRequested = true;
                        BrowserInterop.SetNeedPointerLock(true);
                    }
                    if (m_lastMousePosition.HasValue) {
                        MouseMovement = Point2.Round(m_queuedMouseMovement.X, m_queuedMouseMovement.Y);
                    }
                    m_lastMousePosition = m_currentMousePosition;
                    m_queuedMouseMovement = Vector2.Zero;
                }
            }
            else {
                m_lastMousePosition = null;
            }
            MouseWheelMovement = (int)MathUtils.Round(m_queuedMouseWheelMovement) * 120;
            m_queuedMouseWheelMovement = 0f;
            MouseWheelMovementX = (int)MathUtils.Round(m_queuedMouseWheelMovementX) * 120;
            m_queuedMouseWheelMovementX = 0f;
#elif !IOS
            if (Window.IsActive) {
                Point2 position = new((int)m_mouse.Position.X, (int)m_mouse.Position.Y);
                ProcessMouseMove(position);
                if (IsMouseVisible) {
                    m_mouse.Cursor.CursorMode = CursorMode.Normal;
                    MouseMovement = Point2.Zero;
                    m_lastMousePosition = null;
                }
                else {
                    m_mouse.Cursor.CursorMode = CursorMode.Raw;
                    if (m_lastMousePosition.HasValue) {
                        MouseMovement = new Point2(position.X - m_lastMousePosition.Value.X, position.Y - m_lastMousePosition.Value.Y);
                    }
                    Point2 windowSize = Window.Size;
                    if (position.X < 0
                        || position.X >= windowSize.X
                        || position.Y < 0
                        || position.Y >= windowSize.Y) {
                        position = new Point2(windowSize.X / 2, windowSize.Y / 2);
                        SetMousePosition(position.X, position.Y);
                    }
                    m_lastMousePosition = position;
                }
            }
            else {
                m_lastMousePosition = null;
            }
#endif
        }
#if ANDROID
        public static void EnqueueMouseButtonEvent(MouseButton button, bool press, Point2 position) => m_cachedMouseButtonEvents.Enqueue(new MouseButtonInfo(button, press, position));
#endif
#if ANDROID
        internal static void HandleMotionEvent(MotionEvent e) {
            switch (e.Action) {
                case MotionEventActions.Move: {
                    for (int num = e.HistorySize - 1; num >= 0; num--) {
                        m_queuedMouseMovement += new Vector2(e.GetHistoricalX(num), e.GetHistoricalY(num));
                    }
                    MousePosition = Point2.Round(e.GetX(), e.GetY());
                    break;
                }
                case MotionEventActions.HoverMove: MousePosition = Point2.Round(e.GetX(), e.GetY()); break;
                case MotionEventActions.ButtonPress:
                    EnqueueMouseButtonEvent(TranslateMouseButton(e.ActionButton), true, Point2.Round(e.GetX(), e.GetY())); break;
                case MotionEventActions.ButtonRelease:
                    EnqueueMouseButtonEvent(TranslateMouseButton(e.ActionButton), false, Point2.Round(e.GetX(), e.GetY())); break;
                case MotionEventActions.PointerIdShift: {
                    for (int num2 = e.HistorySize - 1; num2 >= 0; num2--) {
                        m_queuedMouseWheelMovement += MathUtils.Sign(e.GetHistoricalAxisValue(Axis.Vscroll, num2));
                        m_queuedMouseWheelMovementX += MathUtils.Sign(e.GetHistoricalAxisValue(Axis.Hscroll, num2));
                    }
                    m_queuedMouseWheelMovement += MathUtils.Sign(e.GetAxisValue(Axis.Vscroll));
                    m_queuedMouseWheelMovementX += MathUtils.Sign(e.GetAxisValue(Axis.Hscroll));
                    break;
                }
            }
        }

        public static MouseButton TranslateMouseButton(MotionEventButtonState state) => state switch {
            MotionEventButtonState.Primary => MouseButton.Left,
            MotionEventButtonState.Secondary => MouseButton.Right,
            MotionEventButtonState.Tertiary => MouseButton.Middle,
            MotionEventButtonState.Back => MouseButton.Ext1,
            MotionEventButtonState.Forward => MouseButton.Ext2,
            _ => MouseButton.Left
        };

        public static PointerIconType TranslateCursorType(CursorType cursorType) => cursorType switch {
            CursorType.Arrow => PointerIconType.Arrow,
            CursorType.IBeam => PointerIconType.Text,
            CursorType.Crosshair => PointerIconType.Crosshair,
            CursorType.Hand => PointerIconType.Hand,
            CursorType.HResize => PointerIconType.HorizontalDoubleArrow,
            CursorType.VResize => PointerIconType.VerticalDoubleArrow,
            CursorType.NwseResize => PointerIconType.TopLeftDiagonalDoubleArrow,
            CursorType.NeswResize => PointerIconType.TopRightDiagonalDoubleArrow,
            CursorType.ResizeAll => PointerIconType.AllScroll,
            CursorType.NotAllowed => PointerIconType.NoDrop,
            CursorType.Grab => PointerIconType.Grab,
            CursorType.Grabbing => PointerIconType.Grabbing,
            _ => PointerIconType.Default
        };

        public class OnCapturedPointerListener : Java.Lang.Object, View.IOnCapturedPointerListener {
            public bool OnCapturedPointer(View view, MotionEvent e) {
                if (e == null) {
                    return true;
                }
                if ((e.Source & InputSourceType.MouseRelative) == InputSourceType.MouseRelative) {
                    HandleMotionEvent(e);
                }
                return true;
            }
        }
#elif !IOS && !BROWSER
        static void MouseDownHandler(IMouse mouse, Silk.NET.Input.MouseButton button) {
            MouseButton mouseButton = TranslateMouseButton(button);
            if (mouseButton != (MouseButton)(-1)) {
                System.Numerics.Vector2 position = mouse.Position;
                ProcessMouseDown(mouseButton, new Point2((int)position.X, (int)position.Y));
            }
        }

        static void MouseUpHandler(IMouse mouse, Silk.NET.Input.MouseButton button) {
            MouseButton mouseButton = TranslateMouseButton(button);
            if (mouseButton != (MouseButton)(-1)) {
                System.Numerics.Vector2 position = mouse.Position;
                ProcessMouseUp(mouseButton, new Point2((int)position.X, (int)position.Y));
            }
        }

        // ReSharper disable UnusedParameter.Local
        // ReSharper disable UnusedMember.Local
        static void MouseMoveHandler(IMouse mouse, System.Numerics.Vector2 position)
            // ReSharper restore UnusedMember.Local
            // ReSharper restore UnusedParameter.Local
        {
            ProcessMouseMove(new Point2((int)position.X, (int)position.Y));
        }

        static void MouseWheelHandler(IMouse mouse, ScrollWheel scrollWheel) => ProcessMouseWheelXY(new Vector2(scrollWheel.X, scrollWheel.Y));

        public static MouseButton TranslateMouseButton(Silk.NET.Input.MouseButton mouseButton) => mouseButton switch {
            Silk.NET.Input.MouseButton.Left => MouseButton.Left,
            Silk.NET.Input.MouseButton.Right => MouseButton.Right,
            Silk.NET.Input.MouseButton.Middle => MouseButton.Middle,
            Silk.NET.Input.MouseButton.Button4 => MouseButton.Ext1,
            Silk.NET.Input.MouseButton.Button5 => MouseButton.Ext2,
            _ => (MouseButton)(-1)
        };

        public static StandardCursor TranslateCursorType(CursorType cursorType) => cursorType switch {
            CursorType.Arrow => StandardCursor.Arrow,
            CursorType.IBeam => StandardCursor.IBeam,
            CursorType.Crosshair => StandardCursor.Crosshair,
            CursorType.Hand or CursorType.Grab or CursorType.Grabbing => StandardCursor.Hand,
            CursorType.HResize => StandardCursor.HResize,
            CursorType.VResize => StandardCursor.VResize,
            CursorType.NwseResize => StandardCursor.NwseResize,
            CursorType.NeswResize => StandardCursor.NeswResize,
            CursorType.ResizeAll => StandardCursor.ResizeAll,
            CursorType.NotAllowed => StandardCursor.NotAllowed,
            CursorType.Wait => StandardCursor.Wait,
            CursorType.WaitArrow => StandardCursor.WaitArrow,
            _ => StandardCursor.Default
        };
#endif

        static Mouse() {
            m_mouseButtonsDownArray = new bool[Enum.GetValues<MouseButton>().Length];
            m_mouseButtonsDownFrameArray = new int[Enum.GetValues<MouseButton>().Length];
            m_mouseButtonsDelayedUpArray = new bool[Enum.GetValues<MouseButton>().Length];
            m_mouseButtonsDownOnceArray = new bool[Enum.GetValues<MouseButton>().Length];
            m_mouseButtonsUpOnceArray = new bool[Enum.GetValues<MouseButton>().Length];
            IsMouseVisible = true;
        }

        public static bool IsMouseButtonDown(MouseButton mouseButton) => m_mouseButtonsDownArray[(int)mouseButton];

        public static bool IsMouseButtonDownOnce(MouseButton mouseButton) => m_mouseButtonsDownOnceArray[(int)mouseButton];

        public static bool IsMouseButtonUpOnce(MouseButton mouseButton) => m_mouseButtonsUpOnceArray[(int)mouseButton];

        public static void Clear() {
            for (int i = 0; i < m_mouseButtonsDownArray.Length; i++) {
                m_mouseButtonsDownArray[i] = false;
                m_mouseButtonsDownFrameArray[i] = 0;
                m_mouseButtonsDelayedUpArray[i] = false;
                m_mouseButtonsDownOnceArray[i] = false;
                m_mouseButtonsUpOnceArray[i] = false;
            }
        }

        internal static void AfterFrame() {
            for (int i = 0; i < m_mouseButtonsDownArray.Length; i++) {
                m_mouseButtonsDownOnceArray[i] = false;
                if (m_mouseButtonsDelayedUpArray[i]) {
                    m_mouseButtonsDelayedUpArray[i] = false;
                    m_mouseButtonsDownArray[i] = false;
                    m_mouseButtonsUpOnceArray[i] = true;
                }
                else {
                    m_mouseButtonsUpOnceArray[i] = false;
                }
            }
            if (!IsMouseVisible) {
                MousePosition = null;
#if !MOBILE && !BROWSER
                m_mouse.Cursor.CursorMode = Window.IsActive ? CursorMode.Raw : CursorMode.Normal;
            }
            else {
                m_mouse.Cursor.CursorMode = CursorMode.Normal;
#endif
            }
            MouseWheelMovement = 0;
        }

        public static void ProcessMouseDown(MouseButton mouseButton, Point2 position) {
            if (Window.IsActive
                && !Keyboard.IsKeyboardVisible) {
                if (!MousePosition.HasValue) {
                    ProcessMouseMove(position);
                }
                m_mouseButtonsDownArray[(int)mouseButton] = true;
                m_mouseButtonsDownFrameArray[(int)mouseButton] = Time.FrameIndex;
                m_mouseButtonsDownOnceArray[(int)mouseButton] = true;
                Point2 scaledPosition = Point2.Round(position.X * Window.Scale, position.Y * Window.Scale);
                MousePosition = scaledPosition;
                if (IsMouseVisible && MouseDown != null) {
                    MouseDown(new MouseButtonEvent { Button = mouseButton, Position = scaledPosition });
                }
            }
        }

        public static void ProcessMouseUp(MouseButton mouseButton, Point2 position) {
            if (Window.IsActive
                && !Keyboard.IsKeyboardVisible) {
                if (!MousePosition.HasValue) {
                    ProcessMouseMove(position);
                }
                if (m_mouseButtonsDownArray[(int)mouseButton]
                    && Time.FrameIndex == m_mouseButtonsDownFrameArray[(int)mouseButton]) {
                    m_mouseButtonsDelayedUpArray[(int)mouseButton] = true;
                }
                else {
                    m_mouseButtonsDownArray[(int)mouseButton] = false;
                    m_mouseButtonsUpOnceArray[(int)mouseButton] = true;
                }
                Point2 scaledPosition = Point2.Round(position.X * Window.Scale, position.Y * Window.Scale);
                MousePosition = scaledPosition;
                if (IsMouseVisible && MouseUp != null) {
                    MouseUp(new MouseButtonEvent { Button = mouseButton, Position = scaledPosition });
                }
            }
        }

        public static void ProcessMouseMove(Point2 position) {
            if (Window.IsActive
                && !Keyboard.IsKeyboardVisible
                && IsMouseVisible) {
                Point2 scaledPosition = Point2.Round(position.X * Window.Scale, position.Y * Window.Scale);
                MousePosition = scaledPosition;
                MouseMove?.Invoke(new MouseEvent { Position = scaledPosition });
            }
        }

        [Obsolete("Use ProcessMouseWheelXY instead.")]
        public static void ProcessMouseWheel(float value) {
            if (Window.IsActive
                && !Keyboard.IsKeyboardVisible) {
                MouseWheelMovement += (int)(120 * value);
            }
        }

        public static void ProcessMouseWheelXY(Vector2 value) {
            if (Window.IsActive
                && !Keyboard.IsKeyboardVisible) {
                MouseWheelMovement += (int)(120 * value.Y);
                MouseWheelMovementX += (int)(120 * value.X);
            }
        }

        public static void SetCursorType(CursorType cursorType) {
#if ANDROID
            //不知道为什么安卓上无效
            if (Build.VERSION.SdkInt >= (BuildVersionCodes)24) {
                Window.m_surface?.PointerIcon = PointerIcon.GetSystemIcon(Application.Context, TranslateCursorType(cursorType));
            }
#elif !IOS && !BROWSER
            m_mouse.Cursor.StandardCursor = TranslateCursorType(cursorType);
#endif
        }
    }
}