using System.Runtime.InteropServices;
using Engine.Input;

namespace Engine.Browser {
    public static unsafe class InputBridge {
        static SharedInputMemory* _sharedPtr;
        static InputBuffer* _currentReadBuffer;

        public static Point2 CanvasSize = Point2.One;
        public static bool IsFullscreen = false;

        public static IntPtr Initialize() {
            // 分配对齐的非托管内存
            void* ptr = NativeMemory.AlignedAlloc((nuint)sizeof(SharedInputMemory), 16);
            NativeMemory.Clear(ptr, (nuint)sizeof(SharedInputMemory));
            _sharedPtr = (SharedInputMemory*)ptr;
            return (IntPtr)ptr;
        }

        public static void BeforeFrame() {
            if (_sharedPtr == null) {
                return;
            }
            // 1. 原子翻转 Buffer
            int currentIndex = _sharedPtr->ActiveIndex;
            int nextIndex = (currentIndex + 1) % 2;
            Interlocked.Exchange(ref _sharedPtr->ActiveIndex, nextIndex);
            // 2. 获取刚才 JS 写入的 Buffer
            _currentReadBuffer = currentIndex == 0 ? &_sharedPtr->Buffer0 : &_sharedPtr->Buffer1;
            // 3. 处理状态数据
            ProcessStates();
            // 4. 处理事件流
            ProcessEvents();
            // 5. 重置事件计数 (状态数据不需要清零，会覆盖)
            _currentReadBuffer->UsedBytes = 0;
        }

        static void ProcessStates() {
            float newCanvasWidth = _currentReadBuffer->CanvasWidth;
            float newCanvasHeight = _currentReadBuffer->CanvasHeight;
            if (CanvasSize.X != newCanvasWidth || CanvasSize.Y != newCanvasHeight) {
                CanvasSize = new Point2((int)newCanvasWidth, (int)newCanvasHeight);
                Window.ResizeHandler(default);
            }
            float mousePositionX = _currentReadBuffer->MousePositionX;
            float mousePositionY = _currentReadBuffer->MousePositionY;
            Mouse.m_currentMousePosition = new Point2((int)mousePositionX, (int)mousePositionY);
            for (int gamepadIndex = 0; gamepadIndex < 4; gamepadIndex++) {
                GamePad.State state = GamePad.m_states[gamepadIndex];
                if (!state.IsConnected) {
                    continue;
                }
                state.Sticks[0] = new Vector2(_currentReadBuffer->GamepadAxes[gamepadIndex * 4], -_currentReadBuffer->GamepadAxes[gamepadIndex * 4 + 1]);
                state.Sticks[1] = new Vector2(_currentReadBuffer->GamepadAxes[gamepadIndex * 4 + 2], -_currentReadBuffer->GamepadAxes[gamepadIndex * 4 + 3]);
                state.Triggers[0] = _currentReadBuffer->GamepadTriggers[gamepadIndex * 2];
                state.Triggers[1] = _currentReadBuffer->GamepadTriggers[gamepadIndex * 2 + 1];
            }
        }

        static void ProcessEvents() {
            byte* ptr = _currentReadBuffer->EventData;
            byte* end = ptr + _currentReadBuffer->UsedBytes;
            while (ptr < end) {
                InputEventType type = (InputEventType)(*ptr);
                if (type == InputEventType.None) {
                    return;
                }
                if (((byte)type & 128) == 128) {
                    // 读取 12 字节
                    LargeEvent* e = (LargeEvent*)ptr;
                    HandleLargeEvent(e);
                    ptr += 12;
                }
                else {
                    // 读取 4 字节
                    SmallEvent* e = (SmallEvent*)ptr;
                    HandleSmallEvent(e);
                    ptr += 4;
                }
            }
        }

        static void HandleSmallEvent(SmallEvent* e) {
            switch (e->Type) {
                case InputEventType.KeyDown:
                    Keyboard.ProcessKeyDown((Key)e->Param);
                    char c = (char)e->Payload;
                    if (c != 0) {
                        Keyboard.ProcessCharacterEntered(c);
                    }
                    break;
                case InputEventType.KeyUp:
                    Keyboard.ProcessKeyUp((Key)e->Param);
                    break;
                case InputEventType.GamepadButtonDown:
                    GamePad.m_states[e->Payload].Buttons[e->Param] = true;
                    break;
                case InputEventType.GamepadButtonUp:
                    GamePad.m_states[e->Payload].Buttons[e->Param] = false;
                    break;
                case InputEventType.GamepadDisconnected:
                    GamePad.GamepadDisconnectedHandler(e->Payload);
                    break;
                case InputEventType.FocusChange:
                    Window.FocusedChangedHandler(e->Param == 1);
                    break;
                case InputEventType.FullscreenChange:
                    IsFullscreen = e->Param == 1;
                    break;
            }
        }

        static void HandleLargeEvent(LargeEvent* e) {
            switch (e->Header.Type) {
                case InputEventType.MouseDown:
                    Mouse.ProcessMouseDown((MouseButton)e->Header.Param, new Point2((int)e->X, (int)e->Y));
                    break;
                case InputEventType.MouseUp:
                    Mouse.ProcessMouseUp((MouseButton)e->Header.Param, new Point2((int)e->X, (int)e->Y));
                    break;
                case InputEventType.MouseMove:
                    Mouse.m_queuedMouseMovement += new Vector2(e->X, e->Y);
                    break;
                case InputEventType.MouseWheel:
                    Mouse.m_queuedMouseWheelMovement -= e->Y;
                    Mouse.m_queuedMouseWheelMovementX += e->X;
                    break;
                case InputEventType.TouchDown:
                    Touch.ProcessTouchPressed(e->Header.Param, new Point2((int)e->X, (int)e->Y));
                    break;
                case InputEventType.TouchUp:
                    Touch.ProcessTouchReleased(e->Header.Param, new Point2((int)e->X, (int)e->Y));
                    break;
                case InputEventType.TouchMove:
                    Touch.ProcessTouchMoved(e->Header.Param, new Point2((int)e->X, (int)e->Y));
                    break;
            }
        }
    }
}