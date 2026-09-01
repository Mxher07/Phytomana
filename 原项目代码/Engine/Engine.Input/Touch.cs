#if ANDROID
using System.Collections.Concurrent;
using Android.Views;
#elif BROWSER
using System.Collections.Concurrent;
#endif

namespace Engine.Input {
    public static class Touch {
#if ANDROID
        public struct TouchInfo {
            public int PointerId;
            public Vector2 Position;
            public int ActionMasked; //1: down, 2: move, 3: up

            public TouchInfo(int pointerId, Vector2 position, int actionMasked) {
                PointerId = pointerId;
                Position = position;
                ActionMasked = actionMasked;
            }
        }
#endif

        static List<TouchLocation> m_touchLocations = [];

        public static ReadOnlyList<TouchLocation> TouchLocations => new(m_touchLocations);

        public static event Action<TouchLocation> TouchPressed;

        public static event Action<TouchLocation> TouchReleased;

        public static event Action<TouchLocation> TouchMoved;

        public static bool IsTouched;

        internal static void Initialize() { }

        internal static void Dispose() { }

#if ANDROID
        public static ConcurrentQueue<TouchInfo> m_cachedTouchEvents = [];
        public static void EnqueueTouchEvent(int pointerId, Vector2 position, int actionMasked) => m_cachedTouchEvents.Enqueue(new TouchInfo(pointerId, position, actionMasked));
        internal static void HandleTouchEvent(MotionEvent e) {
#pragma warning disable CA1416
            switch (e.ActionMasked) {
                case MotionEventActions.Down:
                case MotionEventActions.Pointer1Down:
                    m_cachedTouchEvents.Enqueue(
                        new TouchInfo(e.GetPointerId(e.ActionIndex), new Vector2(e.GetX(e.ActionIndex), e.GetY(e.ActionIndex)), 1)
                    ); break;
                case MotionEventActions.Move:
                    for (int i = 0; i < e.PointerCount; i++) {
                        m_cachedTouchEvents.Enqueue(new TouchInfo(e.GetPointerId(i), new Vector2(e.GetX(i), e.GetY(i)), 2));
                    }
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Pointer1Up:
                case MotionEventActions.Cancel:
                case MotionEventActions.Outside:
                    m_cachedTouchEvents.Enqueue(
                        new TouchInfo(e.GetPointerId(e.ActionIndex), new Vector2(e.GetX(e.ActionIndex), e.GetY(e.ActionIndex)), 3)
                    ); break;
            }
#pragma warning restore CA1416
        }
#endif

        public static void Clear() => m_touchLocations.Clear();

        internal static void BeforeFrame() {
#if ANDROID
            while (!m_cachedTouchEvents.IsEmpty) {
                if (m_cachedTouchEvents.TryDequeue(out TouchInfo touchInfo)) {
                    switch (touchInfo.ActionMasked) {
                        case 1: ProcessTouchPressed(touchInfo.PointerId, touchInfo.Position); break;
                        case 2: ProcessTouchMoved(touchInfo.PointerId, touchInfo.Position); break;
                        case 3: ProcessTouchReleased(touchInfo.PointerId, touchInfo.Position); break;
                    }
                }
                else {
                    Thread.Yield();
                }
            }
#endif
        }

        internal static void AfterFrame() {
            for (int i = 0; i < m_touchLocations.Count; i++) {
                if (m_touchLocations[i].State == TouchLocationState.Released) {
                    m_touchLocations.RemoveAt(i);
                    continue;
                }
                if (m_touchLocations[i].ReleaseQueued) {
                    m_touchLocations[i] = new TouchLocation {
                        Id = m_touchLocations[i].Id, Position = m_touchLocations[i].Position, State = TouchLocationState.Released
                    };
                }
                else if (m_touchLocations[i].State == TouchLocationState.Pressed) {
                    m_touchLocations[i] = new TouchLocation {
                        Id = m_touchLocations[i].Id, Position = m_touchLocations[i].Position, State = TouchLocationState.Moved
                    };
                }
            }
        }

        static int FindTouchLocationIndex(int id) {
            for (int i = 0; i < m_touchLocations.Count; i++) {
                if (m_touchLocations[i].Id == id) {
                    return i;
                }
            }
            return -1;
        }

        public static void ProcessTouchPressed(int id, Vector2 position) => ProcessTouchMoved(id, position);

        public static void ProcessTouchMoved(int id, Vector2 position) {
            if (!Window.IsActive
                || Keyboard.IsKeyboardVisible) {
                return;
            }
            IsTouched = true;
            int num = FindTouchLocationIndex(id);
            if (num >= 0) {
                if (m_touchLocations[num].State == TouchLocationState.Moved) {
                    m_touchLocations[num] = new TouchLocation { Id = id, Position = position, State = TouchLocationState.Moved };
                }
                TouchMoved?.Invoke(m_touchLocations[num]);
            }
            else {
                m_touchLocations.Add(new TouchLocation { Id = id, Position = position, State = TouchLocationState.Pressed });
                TouchPressed?.Invoke(m_touchLocations[^1]);
            }
        }

        public static void ProcessTouchReleased(int id, Vector2 position) {
            if (!Window.IsActive
                || Keyboard.IsKeyboardVisible) {
                return;
            }
            int num = FindTouchLocationIndex(id);
            if (num >= 0) {
                if (m_touchLocations[num].State == TouchLocationState.Pressed) {
                    m_touchLocations[num] = new TouchLocation {
                        Id = id, Position = position, State = TouchLocationState.Pressed, ReleaseQueued = true
                    };
                }
                else {
                    m_touchLocations[num] = new TouchLocation { Id = id, Position = position, State = TouchLocationState.Released };
                }
                TouchReleased?.Invoke(m_touchLocations[num]);
            }
        }
    }
}