using Engine;
using Engine.Graphics;
using Engine.Input;
using TemplatesDatabase;

namespace Game {
    public class WidgetInput {
        public bool m_isCleared;

        public Widget m_widget;

        public Vector2 m_softMouseCursorPosition;

        public Vector2? m_mouseDownPoint;

        public MouseButton m_mouseDownButton;

        public double m_mouseDragTime;

        public bool m_mouseDragInProgress;

        public bool m_mouseHoldInProgress;

        public bool m_isMouseCursorVisible = true;

        public bool m_useSoftMouseCursor;

        public int? m_touchId;

        public bool m_touchCleared;

        public Vector2 m_touchStartPoint;

        public double m_touchStartTime;

        public bool m_touchDragInProgress;

        public bool m_touchHoldInProgress;

        public Vector2 m_padCursorPosition;

        public Vector2? m_padDownPoint;

        public double m_padDragTime;

        public bool m_padDragInProgress;

        public bool m_isPadCursorVisible = true;

        public Vector2? m_vrDownPoint;

        public double m_vrDragTime;

        public bool m_vrDragInProgress;

        public bool m_isVrCursorVisible = true;

        public bool Any { get; set; }

        public bool Ok { get; set; }

        public bool Cancel { get; set; }

        public bool Back { get; set; }

        public bool Left { get; set; }

        public bool Right { get; set; }

        public bool Up { get; set; }

        public bool Down { get; set; }

        public Vector2? Press { get; set; }

        public Vector2? Tap { get; set; }

        public Segment2? Click { get; set; }

        public Segment2? SpecialClick { get; set; }

        public Vector2? Drag { get; set; }

        public DragMode DragMode { get; set; }

        public Vector2? Hold { get; set; }

        public float HoldTime { get; set; }

        public Vector3? Scroll { get; set; }

        public float? ScrollX { get; set; }

        public Key? LastKey {
            get {
                if (m_isCleared) {
                    return null;
                }
                if ((Devices & WidgetInputDevice.Keyboard) != WidgetInputDevice.None) {
                    return Keyboard.LastKey;
                }
                for (int i = 0; i < 4; i++) {
                    if (((uint)Devices & (uint)(2 << i)) != 0u) {
                        return MultiKeyboard.LastKey(i);
                    }
                }
                return null;
            }
        }

        public char? LastChar {
            get {
                if (m_isCleared) {
                    return null;
                }
                if ((Devices & WidgetInputDevice.Keyboard) != WidgetInputDevice.None) {
                    return Keyboard.LastChar;
                }
                for (int i = 0; i < 4; i++) {
                    if (((uint)Devices & (uint)(2 << i)) != 0u) {
                        return MultiKeyboard.LastChar(i);
                    }
                }
                return null;
            }
        }

        public bool UseSoftMouseCursor {
            get => m_useSoftMouseCursor;
            set => m_useSoftMouseCursor = value;
        }

        public bool IsMouseCursorVisible {
            get {
                if ((Devices & (WidgetInputDevice.MultiMice | WidgetInputDevice.Mouse)) == WidgetInputDevice.None) {
                    return false;
                }
                return m_isMouseCursorVisible;
            }
            set => m_isMouseCursorVisible = value;
        }

        public Vector2? MousePosition {
            get {
                if (m_isCleared) {
                    return null;
                }
                if ((Devices & WidgetInputDevice.Mouse) != WidgetInputDevice.None) {
                    if (m_useSoftMouseCursor) {
                        return m_softMouseCursorPosition;
                    }
                    if (!Mouse.MousePosition.HasValue) {
                        return null;
                    }
                    return new Vector2(Mouse.MousePosition.Value);
                }
                for (int i = 0; i < 4; i++) {
                    if (((uint)Devices & (uint)(64 << i)) != 0u) {
                        if (m_useSoftMouseCursor) {
                            return m_softMouseCursorPosition;
                        }
                        return new Vector2(MultiMouse.MousePosition(i));
                    }
                }
                return null;
            }
            set {
                Vector2 vector;
                Vector2 vector2;
                if (Widget != null) {
                    vector = Widget.GlobalBounds.Min;
                    vector2 = Widget.GlobalBounds.Max;
                }
                else {
                    vector = Vector2.Zero;
                    vector2 = new Vector2(Window.Size);
                }
                if ((Devices & WidgetInputDevice.Mouse) != WidgetInputDevice.None
                    && value.HasValue) {
                    if (m_useSoftMouseCursor) {
                        m_softMouseCursorPosition = new Vector2(
                            Math.Clamp(value.Value.X, vector.X, vector2.X - 1f),
                            Math.Clamp(value.Value.Y, vector.Y, vector2.Y - 1f)
                        );
                    }
                    else {
                        Mouse.SetMousePosition((int)value.Value.X, (int)value.Value.Y);
                    }
                    return;
                }
                for (int i = 0; i < 4; i++) {
                    if (((uint)Devices & (uint)(64 << i)) != 0u && m_useSoftMouseCursor) {
                        m_softMouseCursorPosition = new Vector2(
                            Math.Clamp(value.Value.X, vector.X, vector2.X - 1f),
                            Math.Clamp(value.Value.Y, vector.Y, vector2.Y - 1f)
                        );
                    }
                }
            }
        }

        public Point2 MouseMovement {
            get {
                if (!m_isCleared
                    && (Devices & WidgetInputDevice.Mouse) != WidgetInputDevice.None) {
                    return Mouse.MouseMovement;
                }
                for (int i = 0; i < 4; i++) {
                    if (((uint)Devices & (uint)(64 << i)) != 0u) {
                        return MultiMouse.MouseMovement(i);
                    }
                }
                return Point2.Zero;
            }
        }

        public int MouseWheelMovement {
            get {
                if (!m_isCleared
                    && (Devices & WidgetInputDevice.Mouse) != WidgetInputDevice.None) {
                    return Mouse.MouseWheelMovement;
                }
                for (int i = 0; i < 4; i++) {
                    if (((uint)Devices & (uint)(64 << i)) != 0u) {
                        return MultiMouse.MouseWheelMovement(i);
                    }
                }
                return 0;
            }
        }

        public int MouseWheelMovementX {
            get {
                if (!m_isCleared
                    && (Devices & WidgetInputDevice.Mouse) != WidgetInputDevice.None) {
                    return Mouse.MouseWheelMovementX;
                }
                return 0;
            }
        }

        public bool IsPadCursorVisible {
            get {
                if (m_isPadCursorVisible) {
                    if (((Devices & WidgetInputDevice.GamePad1) == WidgetInputDevice.None || !GamePad.IsConnected(0))
                        && ((Devices & WidgetInputDevice.GamePad2) == WidgetInputDevice.None || !GamePad.IsConnected(1))
                        && ((Devices & WidgetInputDevice.GamePad3) == WidgetInputDevice.None || !GamePad.IsConnected(2))) {
                        if ((Devices & WidgetInputDevice.GamePad4) != WidgetInputDevice.None) {
                            return GamePad.IsConnected(3);
                        }
                        return false;
                    }
                    return true;
                }
                return false;
            }
            set => m_isPadCursorVisible = value;
        }

        public Vector2 PadCursorPosition {
            get => m_padCursorPosition;
            set {
                Vector2 corner1;
                Vector2 corner2;
                if (Widget != null) {
                    corner1 = Widget.GlobalBounds.Min;
                    corner2 = Widget.GlobalBounds.Max;
                }
                else {
                    corner1 = Vector2.Zero;
                    corner2 = new Vector2(Window.Size);
                }
                value.X = Math.Clamp(value.X, corner1.X, corner2.X);
                value.Y = Math.Clamp(value.Y, corner1.Y, corner2.Y);
                m_padCursorPosition = value;
            }
        }

        public ReadOnlyList<TouchLocation> TouchLocations {
            get {
                if (!m_isCleared
                    && (Devices & WidgetInputDevice.Touch) != WidgetInputDevice.None) {
                    return Touch.TouchLocations;
                }
                return ReadOnlyList<TouchLocation>.Empty;
            }
        }

        public Matrix? VrQuadMatrix { get; set; }

        public Vector2? VrCursorLocalPosition { get; set; }

        public bool IsVrCursorVisible {
            get {
                if (m_isVrCursorVisible) {
                    if ((Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None) {
                        return VrManager.IsFrameActive;
                    }
                }
                return false;
            }
            set => m_isVrCursorVisible = value;
        }

        public Vector2? VrCursorPosition { get; set; }

        public static WidgetInput EmptyInput { get; } = new(WidgetInputDevice.None);

        public Widget Widget => m_widget;

        public WidgetInputDevice Devices { get; set; }

        public bool IsKeyDown(Key key) {
            if (m_isCleared) {
                return false;
            }
            if ((Devices & WidgetInputDevice.Keyboard) != WidgetInputDevice.None) {
                return Keyboard.IsKeyDown(key);
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2 << i)) != 0u) {
                    return MultiKeyboard.IsKeyDown(i, key);
                }
            }
            return false;
        }

        public bool IsKeyDownOnce(Key key) {
            if (m_isCleared) {
                return false;
            }
            if ((Devices & WidgetInputDevice.Keyboard) != WidgetInputDevice.None) {
                return Keyboard.IsKeyDownOnce(key);
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2 << i)) != 0u) {
                    return MultiKeyboard.IsKeyDownOnce(i, key);
                }
            }
            return false;
        }

        public bool IsKeyDownRepeat(Key key) {
            if (m_isCleared) {
                return false;
            }
            if ((Devices & WidgetInputDevice.Keyboard) != WidgetInputDevice.None) {
                return Keyboard.IsKeyDownRepeat(key);
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2 << i)) != 0u) {
                    return MultiKeyboard.IsKeyDownRepeat(i, key);
                }
            }
            return false;
        }

        /// <summary>
        ///     根据按键映射名称判断键盘鼠标按键是否按住
        /// </summary>
        /// <param name="mappingName"></param>
        /// <returns></returns>
        public bool IsKeyOrMouseDown(string mappingName) {
            object key = SettingsManager.GetKeyboardMapping(mappingName);
            if (key is MouseButton mouseButton) {
                return IsMouseButtonDown(mouseButton);
            }
            if (key is Key key1
                && key1 != Key.Null) {
                return IsKeyDown(key1);
            }
            return false;
        }

        /// <summary>
        ///     根据按键映射名称判断键盘鼠标按键是否按下
        /// </summary>
        /// <param name="mappingName"></param>
        /// <returns></returns>
        public bool IsKeyOrMouseDownOnce(string mappingName) {
            object key = SettingsManager.GetKeyboardMapping(mappingName);
            if (key is MouseButton mouseButton) {
                return IsMouseButtonDownOnce(mouseButton);
            }
            if (key is Key key1
                && key1 != Key.Null) {
                return IsKeyDownOnce(key1);
            }
            return false;
        }

        /// <summary>
        ///     根据按键映射名称判断手柄按键是否按住
        /// </summary>
        /// <param name="mappingName"></param>
        /// <returns></returns>
        public bool IsGamepadDown(string mappingName) {
            object key = SettingsManager.GetGamepadMapping(mappingName);
            if (key is GamePadTrigger trigger) {
                return IsPadTriggerDown(trigger, 0f, SettingsManager.GamepadTriggerThreshold);
            }
            if (key is GamePadButton button
                && button != GamePadButton.Null) {
                return IsPadButtonDown(button);
            }
            return false;
        }

        /// <summary>
        ///     根据按键映射名称判断手柄按键是否按下
        /// </summary>
        /// <param name="mappingName"></param>
        /// <returns></returns>
        public bool IsGamepadDownOnce(string mappingName) {
            //注意：组合键中行动键只能是GamePadButton，并且不能是肩键
            float threshold = SettingsManager.GamepadTriggerThreshold;
            object key = SettingsManager.GetGamepadMapping(mappingName);
            if (key is GamePadTrigger trigger) {
                return IsTriggerDownOnce(trigger, threshold);
            }
            if (key is GamePadButton button
                && button != GamePadButton.Null
                && (!IsAnyModifierKeyHolding() || GamePad.IsModifierKey(button))) { //当前没有按住修饰键，或者当前按下的就是修饰键（对于GamePadButton来说是左右肩键）
                return IsPadButtonDownOnce(button);
            }
            if (key is ValuesDictionary combo) {
                object modifier = combo.GetValue<object>("ModifierKey", null);
                bool modifierHolding = false;
                if (modifier is GamePadTrigger trigger1) {
                    modifierHolding = IsPadTriggerDown(trigger1, 0f, threshold);
                }
                else if (modifier is GamePadButton button1
                    && button1 != GamePadButton.Null) {
                    modifierHolding = IsPadButtonDown(button1);
                }
                if (modifierHolding) {
                    object action = combo.GetValue<object>("ActionKey", null);
                    if (action is GamePadButton button2
                        && button2 != GamePadButton.Null) {
                        bool flag = IsPadButtonDownOnce(button2);
                        if (flag) {
                            SetModifierKeyOfCurrentCombo(modifier);
                        }
                        return flag;
                    }
                }
            }
            return false;
        }

        public void EnterText(ContainerWidget parentWidget, string title, string text, int maxLength, Action<string> handler) {
            Keyboard.ShowKeyboard(
                title,
                string.Empty,
                text,
                false,
                delegate(string s) {
                    if (s.Length > maxLength) {
                        s = s.Substring(0, maxLength);
                    }
                    handler(s);
                },
                delegate { handler(null); }
            );
        }

        public bool IsMouseButtonDown(MouseButton button) {
            if (!m_isCleared
                && (Devices & WidgetInputDevice.Mouse) != WidgetInputDevice.None) {
                return Mouse.IsMouseButtonDown(button);
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(64 << i)) != 0u) {
                    return MultiMouse.IsMouseButtonDown(i, button);
                }
            }
            return false;
        }

        public bool IsMouseButtonDownOnce(MouseButton button) {
            if (!m_isCleared
                && (Devices & WidgetInputDevice.Mouse) != WidgetInputDevice.None) {
                return Mouse.IsMouseButtonDownOnce(button);
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(64 << i)) != 0u) {
                    return MultiMouse.IsMouseButtonDownOnce(i, button);
                }
            }
            return false;
        }

        public Vector2 GetPadStickPosition(GamePadStick stick, float deadZone = 0f) {
            if (m_isCleared) {
                return Vector2.Zero;
            }
            Vector2 zero = Vector2.Zero;
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u) {
                    zero += GamePad.GetStickPosition(i, stick, deadZone);
                }
            }
            if (!(zero.LengthSquared() > 1f)) {
                return zero;
            }
            return Vector2.Normalize(zero);
        }

        public float GetPadTriggerPosition(GamePadTrigger trigger, float deadZone = 0f) {
            if (m_isCleared) {
                return 0f;
            }
            float num = 0f;
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u) {
                    num += GamePad.GetTriggerPosition(i, trigger, deadZone);
                }
            }
            return MathUtils.Min(num, 1f);
        }

        public bool IsPadTriggerDown(GamePadTrigger trigger, float deadZone = 0f, float threshold = 0.5f) {
            if (m_isCleared) {
                return false;
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u
                    && GamePad.IsTriggerDown(i, trigger, deadZone, threshold)) {
                    return true;
                }
            }
            return false;
        }

        public bool IsTriggerDownOnce(GamePadTrigger trigger, float deadZone = 0f, float threshold = 0.5f) {
            if (m_isCleared) {
                return false;
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u
                    && GamePad.IsTriggerDownOnce(i, trigger, deadZone, threshold)) {
                    return true;
                }
            }
            return false;
        }

        public void SetModifierKeyOfCurrentCombo(object modifierKey) {
            if (m_isCleared) {
                return;
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u) {
                    GamePad.SetModifierKeyOfCurrentCombo(i, modifierKey);
                }
            }
        }

        public bool IsPadButtonDown(GamePadButton button) {
            if (m_isCleared) {
                return false;
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u
                    && GamePad.IsButtonDown(i, button)) {
                    return true;
                }
            }
            return false;
        }

        public bool IsPadButtonDownOnce(GamePadButton button) {
            if (m_isCleared) {
                return false;
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u
                    && GamePad.IsButtonDownOnce(i, button)) {
                    return true;
                }
            }
            return false;
        }

        public bool IsPadButtonDownRepeat(GamePadButton button) {
            if (m_isCleared) {
                return false;
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u
                    && GamePad.IsButtonDownRepeat(i, button)) {
                    return true;
                }
            }
            return false;
        }

        public bool IsAnyModifierKeyHolding() {
            if (m_isCleared) {
                return false;
            }
            for (int i = 0; i < 4; i++) {
                if (((uint)Devices & (uint)(2048 << i)) != 0u
                    && GamePad.IsAnyModifierKeyHolding(i)) {
                    //此处若使用SettingsManager.GamepadTriggerThreshold，恐与GamePad类中使用到IsAnyModifierKeyHolding的地方不一致。因为GamePad无法访问SettingsManager
                    return true;
                }
            }
            return false;
        }

        ///// <summary>
        ///// 使手柄的马达震动
        ///// </summary>
        ///// <param name="vibration">震动幅度(马达速度)，在0到1之间</param>
        ///// <param name="durationMs">震动持续时间(毫秒)</param>
        //public void MakeGamePadVibration(float vibration,float durationMs)
        //{
        //	for(int i = 0; i < 4; i++)
        //	{
        //		if(((uint)Devices & (uint)(2048 << i)) != 0u)
        //		{
        //			GamePad.MakeVibration(i,vibration,durationMs);
        //			break;
        //		}
        //	}
        //}

        public Vector2 GetVrStickPosition(VrController controller, float deadZone = 0f) {
            if (!m_isCleared
                && (Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None) {
                return VrManager.GetStickPosition(controller, deadZone);
            }
            return Vector2.Zero;
        }

        public Vector2? GetVrTouchpadPosition(VrController controller, float deadZone = 0f) {
            if (!m_isCleared
                && (Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None) {
                return VrManager.GetTouchpadPosition(controller, deadZone);
            }
            return null;
        }

        public float GetVrTriggerPosition(VrController controller, float deadZone = 0f) {
            if (!m_isCleared
                && (Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None) {
                return VrManager.GetTriggerPosition(controller, deadZone);
            }
            return 0f;
        }

        public bool IsVrButtonDown(VrController controller, VrControllerButton button) {
            if (!m_isCleared
                && (Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None) {
                return VrManager.IsButtonDown(controller, button);
            }
            return false;
        }

        public bool IsVrButtonDownOnce(VrController controller, VrControllerButton button) {
            if (!m_isCleared
                && (Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None) {
                return VrManager.IsButtonDownOnce(controller, button);
            }
            return false;
        }

        public WidgetInput(WidgetInputDevice devices = WidgetInputDevice.All) => Devices = devices;

        public void Clear() {
            m_isCleared = true;
            m_mouseDownPoint = null;
            m_mouseDragInProgress = false;
            m_touchCleared = true;
            m_padDownPoint = null;
            m_padDragInProgress = false;
            m_vrDownPoint = null;
            m_vrDragInProgress = false;
            ClearInput();
        }

        public virtual void Update() {
            m_isCleared = false;
            ClearInput();
            if (Window.IsActive) {
                if ((Devices & (WidgetInputDevice.MultiKeyboards | WidgetInputDevice.Keyboard)) != WidgetInputDevice.None) {
                    UpdateInputFromKeyboard();
                }
                if ((Devices & (WidgetInputDevice.MultiMice | WidgetInputDevice.Mouse)) != WidgetInputDevice.None) {
                    UpdateInputFromMouse();
                }
                if ((Devices & WidgetInputDevice.Gamepads) != WidgetInputDevice.None) {
                    UpdateInputFromGamepads();
                }
                if ((Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None && VrManager.IsFrameActive) {
                    UpdateInputFromVrControllers();
                }
                if ((Devices & WidgetInputDevice.Touch) != WidgetInputDevice.None) {
                    UpdateInputFromTouch();
                }
            }
        }

        public virtual void Draw(Widget.DrawContext dc) {
            if (IsMouseCursorVisible
                && UseSoftMouseCursor
                && MousePosition.HasValue) {
                Texture2D texture2D = m_mouseDragInProgress ? ContentManager.Get<Texture2D>("Textures/Gui/PadCursorDrag") :
                    !m_mouseDownPoint.HasValue ? ContentManager.Get<Texture2D>("Textures/Gui/PadCursor") :
                    ContentManager.Get<Texture2D>("Textures/Gui/PadCursorDown");
                TexturedBatch2D texturedBatch2D = dc.CursorPrimitivesRenderer2D.TexturedBatch(texture2D);
                Vector2 corner;
                Vector2 corner2 = (corner = Vector2.Transform(MousePosition.Value, Widget.InvertedGlobalTransform))
                    + new Vector2(texture2D.Width, texture2D.Height) * 0.8f;
                int count = texturedBatch2D.TriangleVertices.Count;
                texturedBatch2D.QueueQuad(corner, corner2, 0f, Vector2.Zero, Vector2.One, Color.White);
                texturedBatch2D.TransformTriangles(Widget.GlobalTransform, count);
            }
            if (IsMouseCursorVisible && !UseSoftMouseCursor) {
                Mouse.SetCursorType(m_mouseDragInProgress ? CursorType.Grabbing : CursorType.Default);
            }
            if (IsPadCursorVisible) {
                Texture2D texture2D2 = m_padDragInProgress ? ContentManager.Get<Texture2D>("Textures/Gui/PadCursorDrag") :
                    !m_padDownPoint.HasValue ? ContentManager.Get<Texture2D>("Textures/Gui/PadCursor") :
                    ContentManager.Get<Texture2D>("Textures/Gui/PadCursorDown");
                TexturedBatch2D texturedBatch2D2 = dc.CursorPrimitivesRenderer2D.TexturedBatch(texture2D2);
                Vector2 corner3;
                Vector2 corner4 = (corner3 = Vector2.Transform(PadCursorPosition, Widget.InvertedGlobalTransform))
                    + new Vector2(texture2D2.Width, texture2D2.Height) * 0.8f;
                int count2 = texturedBatch2D2.TriangleVertices.Count;
                texturedBatch2D2.QueueQuad(corner3, corner4, 0f, Vector2.Zero, Vector2.One, Color.White);
                texturedBatch2D2.TransformTriangles(Widget.GlobalTransform, count2);
            }
            if (IsVrCursorVisible && VrCursorLocalPosition.HasValue) {
                Vector2 screenPos = Vector2.Transform(VrCursorLocalPosition.Value, Widget.GlobalTransform);
                dc.CursorPrimitivesRenderer2D.FlatBatch(0, null, null, null).QueueDisc(
                    screenPos, new Vector2(10f, 10f), 0f, Color.White);
            }
        }

        public void ClearInput() {
            Any = false;
            Ok = false;
            Cancel = false;
            Back = false;
            Left = false;
            Right = false;
            Up = false;
            Down = false;
            Press = null;
            Tap = null;
            Click = null;
            SpecialClick = null;
            Drag = null;
            DragMode = DragMode.AllItems;
            Hold = null;
            HoldTime = 0f;
            Scroll = null;
            ScrollX = null;
        }

        public virtual void UpdateInputFromKeyboard() {
            if (LastKey.HasValue
                && LastKey != Key.Escape) {
                Any = true;
            }
            if (IsKeyDownOnce(Key.Escape)) {
                Back = true;
                Cancel = true;
            }
            if (IsKeyDownRepeat(Key.LeftArrow)) {
                Left = true;
            }
            if (IsKeyDownRepeat(Key.RightArrow)) {
                Right = true;
            }
            if (IsKeyDownRepeat(Key.UpArrow)) {
                Up = true;
            }
            if (IsKeyDownRepeat(Key.DownArrow)) {
                Down = true;
            }
            if (IsKeyDownOnce(Key.Back)) {
                Back = true;
            }
        }

        public virtual void UpdateInputFromMouse() {
            if (IsMouseButtonDownOnce(MouseButton.Left)) {
                Any = true;
            }
            if (IsMouseCursorVisible && MousePosition.HasValue) {
                Vector2 value = MousePosition.Value;
                if (IsMouseButtonDown(MouseButton.Left)
                    || IsMouseButtonDown(MouseButton.Right)) {
                    Press = value;
                }
                if (IsMouseButtonDownOnce(MouseButton.Left)
                    || IsMouseButtonDownOnce(MouseButton.Right)) {
                    Tap = value;
                    m_mouseDownPoint = value;
                    m_mouseDownButton = !IsMouseButtonDownOnce(MouseButton.Left) ? MouseButton.Right : MouseButton.Left;
                    m_mouseDragTime = Time.FrameStartTime;
                }
                if (!IsMouseButtonDown(MouseButton.Left)
                    && m_mouseDownPoint.HasValue
                    && m_mouseDownButton == MouseButton.Left) {
                    if (IsKeyDown(Key.Shift)) {
                        SpecialClick = new Segment2(m_mouseDownPoint.Value, value);
                    }
                    else {
                        Click = new Segment2(m_mouseDownPoint.Value, value);
                    }
                }
                if (!IsMouseButtonDown(MouseButton.Right)
                    && m_mouseDownPoint.HasValue
                    && m_mouseDownButton == MouseButton.Right) {
                    SpecialClick = new Segment2(m_mouseDownPoint.Value, value);
                }
                if (MouseWheelMovement != 0) {
                    Scroll = new Vector3(value, MouseWheelMovement / 120f);
                }
                if (MouseWheelMovementX != 0) {
                    ScrollX = MouseWheelMovementX / 120f;
                }
                if (m_mouseHoldInProgress && m_mouseDownPoint.HasValue) {
                    Hold = m_mouseDownPoint.Value;
                    HoldTime = (float)(Time.FrameStartTime - m_mouseDragTime);
                }
                if (m_mouseDragInProgress) {
                    Drag = value;
                }
                else if ((IsMouseButtonDown(MouseButton.Left) || IsMouseButtonDown(MouseButton.Right))
                    && m_mouseDownPoint.HasValue) {
                    if (Vector2.Distance(m_mouseDownPoint.Value, value) > SettingsManager.MinimumDragDistance * Widget.GlobalScale) {
                        m_mouseDragInProgress = true;
                        DragMode = !IsMouseButtonDown(MouseButton.Left) ? DragMode.SingleItem : DragMode.AllItems;
                        Drag = m_mouseDownPoint.Value;
                    }
                    else if (Time.FrameStartTime - m_mouseDragTime > SettingsManager.MinimumHoldDuration) {
                        m_mouseHoldInProgress = true;
                    }
                }
            }
            if (!IsMouseButtonDown(MouseButton.Left)
                && !IsMouseButtonDown(MouseButton.Right)) {
                m_mouseDragInProgress = false;
                m_mouseHoldInProgress = false;
                m_mouseDownPoint = null;
            }
            if (m_useSoftMouseCursor && IsMouseCursorVisible) {
                float x = 0.75f * MathUtils.PowSign(MouseMovement.X, 1.25f);
                float y = 0.75f * MathUtils.PowSign(MouseMovement.Y, 1.25f);
                MousePosition = (MousePosition ?? Vector2.Zero) + new Vector2(x, y);
            }
        }

        public virtual void UpdateInputFromGamepads() {
            if (IsPadButtonDownRepeat(GamePadButton.DPadLeft)) {
                Left = true;
            }
            if (IsPadButtonDownRepeat(GamePadButton.DPadRight)) {
                Right = true;
            }
            if (IsPadButtonDownRepeat(GamePadButton.DPadUp)) {
                Up = true;
            }
            if (IsPadButtonDownRepeat(GamePadButton.DPadDown)) {
                Down = true;
            }
            if (IsPadCursorVisible) {
                if (IsPadButtonDownRepeat(GamePadButton.DPadUp)) {
                    Scroll = new Vector3(PadCursorPosition, 1f);
                }
                if (IsPadButtonDownRepeat(GamePadButton.DPadDown)) {
                    Scroll = new Vector3(PadCursorPosition, -1f);
                }
                if (IsPadButtonDown(GamePadButton.A)) {
                    Press = PadCursorPosition;
                }
                if (IsPadButtonDownOnce(GamePadButton.A)) {
                    Ok = true;
                    Tap = PadCursorPosition;
                    m_padDownPoint = PadCursorPosition;
                    m_padDragTime = Time.FrameStartTime;
                }
                if (!IsPadButtonDown(GamePadButton.A)
                    && m_padDownPoint.HasValue) {
                    if (IsPadTriggerDown(GamePadTrigger.Left, 0f, SettingsManager.GamepadTriggerThreshold)) {
                        SpecialClick = new Segment2(m_padDownPoint.Value, PadCursorPosition);
                    }
                    else {
                        Click = new Segment2(m_padDownPoint.Value, PadCursorPosition);
                    }
                }
            }
            if (IsPadButtonDownOnce(GamePadButton.A)
                || IsPadButtonDownOnce(GamePadButton.B)
                || IsPadButtonDownOnce(GamePadButton.X)
                || IsPadButtonDownOnce(GamePadButton.Y)) {
                Any = true;
            }
            if (!IsPadButtonDown(GamePadButton.A)) {
                m_padDragInProgress = false;
                m_padDownPoint = null;
            }
            if (IsPadButtonDownOnce(GamePadButton.B)) {
                Cancel = true;
            }
            if (IsGamepadDownOnce("Back")) {
                Back = true;
            }
            if (m_padDragInProgress) {
                Drag = PadCursorPosition;
            }
            else if (IsPadButtonDown(GamePadButton.A)
                && m_padDownPoint.HasValue) {
                if (Vector2.Distance(m_padDownPoint.Value, PadCursorPosition) > SettingsManager.MinimumDragDistance * Widget.GlobalScale) {
                    m_padDragInProgress = true;
                    Drag = m_padDownPoint.Value;
                    DragMode = DragMode.AllItems;
                }
                else if (Time.FrameStartTime - m_padDragTime > SettingsManager.MinimumHoldDuration) {
                    Hold = m_padDownPoint.Value;
                    HoldTime = (float)(Time.FrameStartTime - m_padDragTime);
                }
            }
            if (IsPadCursorVisible) {
                Vector2 v = Vector2.Transform(PadCursorPosition, Widget.InvertedGlobalTransform);
                Vector2 padStickPosition = GetPadStickPosition(GamePadStick.Left, SettingsManager.GamepadDeadZone);
                Vector2 v2 = new(padStickPosition.X, 0f - padStickPosition.Y);
                v2 = 1200f * SettingsManager.GamepadCursorSpeed * v2.LengthSquared() * Vector2.Normalize(v2) * Time.FrameDuration;
                v += v2;
                PadCursorPosition = Vector2.Transform(v, Widget.GlobalTransform);
            }
        }

        public virtual void UpdateInputFromTouch() {
            foreach (TouchLocation touchLocation in TouchLocations) {
                if (touchLocation.State == TouchLocationState.Pressed) {
                    if (Widget.HitTest(touchLocation.Position)) {
                        Any = true;
                        Tap = touchLocation.Position;
                        Press = touchLocation.Position;
                        m_touchStartPoint = touchLocation.Position;
                        m_touchId = touchLocation.Id;
                        m_touchCleared = false;
                        m_touchStartTime = Time.FrameStartTime;
                        m_touchDragInProgress = false;
                        m_touchHoldInProgress = false;
                    }
                }
                else if (touchLocation.State == TouchLocationState.Moved) {
                    if (m_touchId != touchLocation.Id) {
                        continue;
                    }
                    Press = touchLocation.Position;
                    if (m_touchCleared) {
                        continue;
                    }
                    if (m_touchDragInProgress) {
                        Drag = touchLocation.Position;
                    }
                    else if (Vector2.Distance(touchLocation.Position, m_touchStartPoint) > SettingsManager.MinimumDragDistance * Widget.GlobalScale) {
                        m_touchDragInProgress = true;
                        Drag = m_touchStartPoint;
                    }
                    if (!m_touchDragInProgress) {
                        if (m_touchHoldInProgress) {
                            Hold = m_touchStartPoint;
                            HoldTime = (float)(Time.FrameStartTime - m_touchStartTime);
                        }
                        else if (Time.FrameStartTime - m_touchStartTime > SettingsManager.MinimumHoldDuration) {
                            m_touchHoldInProgress = true;
                        }
                    }
                }
                else if (touchLocation.State == TouchLocationState.Released
                    && m_touchId == touchLocation.Id) {
                    if (!m_touchCleared) {
                        Click = new Segment2(m_touchStartPoint, touchLocation.Position);
                    }
                    m_touchId = null;
                    m_touchCleared = false;
                    m_touchDragInProgress = false;
                    m_touchHoldInProgress = false;
                }
            }
        }

        public virtual void UpdateInputFromVrControllers() {
            VrCursorPosition = null;
            VrCursorLocalPosition = null;
            if (VrQuadMatrix.HasValue) {
                Matrix quadMatrix = VrQuadMatrix.Value;
                Matrix controllerMatrix = VrManager.GetControllerMatrix(VrController.Right);
                Plane plane = new(quadMatrix.Translation, quadMatrix.Translation + quadMatrix.Right, quadMatrix.Translation + quadMatrix.Up);
                Ray3 ray = new(controllerMatrix.Translation, controllerMatrix.Forward);
                float? intersection = ray.Intersection(plane);
                if (intersection.HasValue) {
                    Vector3 vector = ray.Position + intersection.Value * ray.Direction - quadMatrix.Translation;
                    float x = Vector3.Dot(vector, Vector3.Normalize(quadMatrix.Right)) / quadMatrix.Right.Length() * Widget.ActualSize.X;
                    float y = (1f - Vector3.Dot(vector, Vector3.Normalize(quadMatrix.Up)) / quadMatrix.Up.Length()) * Widget.ActualSize.Y;
                    VrCursorLocalPosition = new Vector2(x, y);
                    VrCursorPosition = Vector2.Transform(new Vector2(x, y), Widget.GlobalTransform);
                }
            }
            if (IsVrButtonDownOnce(VrController.Left, VrControllerButton.TrackpadLeft)) {
                Left = true;
            }
            if (IsVrButtonDownOnce(VrController.Left, VrControllerButton.TrackpadRight)) {
                Right = true;
            }
            if (IsVrButtonDownOnce(VrController.Left, VrControllerButton.TrackpadUp)) {
                Up = true;
            }
            if (IsVrButtonDownOnce(VrController.Left, VrControllerButton.TrackpadDown)) {
                Down = true;
            }
            if (IsVrButtonDownOnce(VrController.Right, VrControllerButton.TrackpadLeft)) {
                Left = true;
            }
            if (IsVrButtonDownOnce(VrController.Right, VrControllerButton.TrackpadRight)) {
                Right = true;
            }
            if (IsVrButtonDownOnce(VrController.Right, VrControllerButton.TrackpadUp)) {
                Up = true;
            }
            if (IsVrButtonDownOnce(VrController.Right, VrControllerButton.TrackpadDown)) {
                Down = true;
            }
            if (IsVrCursorVisible
                && (IsVrButtonDownOnce(VrController.Right, VrControllerButton.Grip)
                || IsVrButtonDownOnce(VrController.Right, VrControllerButton.Menu)
                || IsVrButtonDownOnce(VrController.Left, VrControllerButton.Menu))) {
                Back = true;
                Cancel = true;
            }
            if (IsVrButtonDownOnce(VrController.Left, VrControllerButton.Trackpad)
                || IsVrButtonDownOnce(VrController.Left, VrControllerButton.Trigger)
                || IsVrButtonDownOnce(VrController.Right, VrControllerButton.Trackpad)
                || IsVrButtonDownOnce(VrController.Right, VrControllerButton.Trigger)) {
                Any = true;
            }
            if (IsVrCursorVisible && VrCursorPosition.HasValue) {
                if (IsVrButtonDown(VrController.Right, VrControllerButton.TrackpadUp)
                    || GetVrStickPosition(VrController.Right, 0f).Y > 0.5f) {
                    Scroll = new Vector3(VrCursorPosition.Value, 0.5f);
                }
                if (IsVrButtonDown(VrController.Right, VrControllerButton.TrackpadDown)
                    || GetVrStickPosition(VrController.Right, 0f).Y < -0.5f) {
                    Scroll = new Vector3(VrCursorPosition.Value, -0.5f);
                }
                if (IsVrButtonDown(VrController.Right, VrControllerButton.Trigger)) {
                    Press = VrCursorPosition.Value;
                }
                if (IsVrButtonDownOnce(VrController.Right, VrControllerButton.Trigger)) {
                    Ok = true;
                    Tap = VrCursorPosition.Value;
                    m_vrDownPoint = VrCursorPosition.Value;
                    m_vrDragTime = Time.FrameStartTime;
                }
                if (!IsVrButtonDown(VrController.Right, VrControllerButton.Trigger)
                    && m_vrDownPoint.HasValue) {
                    if (GetVrTriggerPosition(VrController.Left, 0f) > 0.5f) {
                        SpecialClick = new Segment2(m_vrDownPoint.Value, VrCursorPosition.Value);
                    }
                    else {
                        Click = new Segment2(m_vrDownPoint.Value, VrCursorPosition.Value);
                    }
                }
            }
            if (!IsVrButtonDown(VrController.Right, VrControllerButton.Trigger)) {
                m_vrDragInProgress = false;
                m_vrDownPoint = null;
            }
            if (m_vrDragInProgress && VrCursorPosition.HasValue) {
                Drag = VrCursorPosition;
                return;
            }
            if (IsVrButtonDown(VrController.Right, VrControllerButton.Trigger) && m_vrDownPoint.HasValue && VrCursorPosition.HasValue) {
                if (Vector2.Distance(m_vrDownPoint.Value, VrCursorPosition.Value) > SettingsManager.MinimumDragDistance * Widget.GlobalScale) {
                    m_vrDragInProgress = true;
                    Drag = m_vrDownPoint.Value;
                    DragMode = DragMode.AllItems;
                    return;
                }
                if (Time.FrameStartTime - m_vrDragTime > SettingsManager.MinimumHoldDuration) {
                    Hold = m_vrDownPoint.Value;
                    HoldTime = (float)(Time.FrameStartTime - m_vrDragTime);
                }
            }
        }
    }
}