using Engine;
using Engine.Input;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentInput : Component, IUpdateable {
        public static volatile float VrFadeAlpha;
        public static float VrSnapBodyRotation;
        public static float VrSnapCameraRotation;

        public SubsystemTime m_subsystemTime;

        public ComponentGui m_componentGui;

        public ComponentPlayer m_componentPlayer;

        public PlayerInput m_playerInput;

        public bool m_isViewHoldStarted;

        public double m_lastJumpTime;

        public Vector2 m_vrSmoothLook;
        public bool m_vrSnapTurning;
        public float m_vrSnapFadeTimer;

        public SubsystemTerrain m_subsystemTerrain;
        public Vector3? m_vrTeleportTarget;
        public List<Vector3> m_vrTeleportArcPoints = new();
        public bool m_vrTeleportValid;
        public bool m_vrTeleportForceCrouch;
        public bool m_vrTeleportStickActive;
        public bool m_vrTeleportSuppressAfterBackward;
        public int m_vrTeleportHitFace;

        public bool ToggleFlyInDoubleJump { get; set; } = true;
        public PlayerInput PlayerInput => m_playerInput;

        public bool IsControlledByTouch { get; set; } = Touch.IsTouched;

        public bool IsControlledByVr => VrManager.IsFrameActive
            && (m_componentPlayer.GameWidget.Input.Devices & WidgetInputDevice.VrControllers) != WidgetInputDevice.None;

        public bool AllowHandleInput { get; set; } = true;

        public IInventory SplitSourceInventory { get; set; }

        public int SplitSourceSlotIndex { get; set; }

        public Vector2? SetMousePositionInNextFrame { get; set; }

        public UpdateOrder UpdateOrder => UpdateOrder.Input;

        public virtual void SetSplitSourceInventoryAndSlot(IInventory inventory, int slotIndex) {
            SplitSourceInventory = inventory;
            SplitSourceSlotIndex = slotIndex;
        }

        public virtual Ray3? CalculateVrHandRay() {
            if (VrManager.IsControllerPresent(VrController.Right)) {
                Camera activeCamera = m_componentPlayer.GameWidget.ActiveCamera;
                Matrix matrix = VrManager.HmdMatrixInverted
                    * Matrix.CreateWorld(activeCamera.ViewPosition, activeCamera.ViewDirection, activeCamera.ViewUp);
                Matrix matrix2 = VrManager.GetControllerMatrix(VrController.Right) * matrix;
                return new Ray3(matrix2.Translation + matrix2.Forward * 0.078125f, matrix2.Forward);
            }
            return null;
        }

        public virtual void Update(float dt) {
            m_playerInput = default;
            UpdateInputFromMouseAndKeyboard(m_componentPlayer.GameWidget.Input);
            UpdateInputFromGamepad(m_componentPlayer.GameWidget.Input);
            UpdateInputFromVrControllers(m_componentPlayer.GameWidget.Input);
            UpdateInputFromWidgets(m_componentPlayer.GameWidget.Input);
            if (m_playerInput.Jump) {
                if (Time.RealTime - m_lastJumpTime < 0.3 && ToggleFlyInDoubleJump) {
                    m_playerInput.ToggleCreativeFly = true;
                    m_lastJumpTime = 0.0;
                }
                else {
                    m_lastJumpTime = Time.RealTime;
                }
            }
            m_playerInput.CameraMove = m_playerInput.Move;
            m_playerInput.CameraCrouchMove = m_playerInput.CrouchMove;
            m_playerInput.CameraLook = m_playerInput.Look;
            if (!Window.IsActive
                || !m_componentPlayer.PlayerData.IsReadyForPlaying) {
                m_playerInput = default;
            }
            else if (m_componentPlayer.ComponentHealth.Health <= 0f
                || m_componentPlayer.ComponentSleep.SleepFactor > 0f
                || !m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled) {
                m_playerInput = new PlayerInput {
                    CameraMove = m_playerInput.CameraMove,
                    CameraCrouchMove = m_playerInput.CameraCrouchMove,
                    CameraLook = m_playerInput.CameraLook,
                    TimeOfDay = m_playerInput.TimeOfDay,
                    Precipitation = m_playerInput.Precipitation,
                    Fog = m_playerInput.Fog,
                    TakeScreenshot = m_playerInput.TakeScreenshot,
                    KeyboardHelp = m_playerInput.KeyboardHelp
                };
            }
            else if (m_componentPlayer.GameWidget.ActiveCamera.UsesMovementControls) {
                m_playerInput.Move = Vector3.Zero;
                m_playerInput.CrouchMove = Vector3.Zero;
                m_playerInput.Look = Vector2.Zero;
                m_playerInput.Jump = false;
                m_playerInput.ToggleCrouch = false;
                m_playerInput.ToggleCreativeFly = false;
            }
            if (m_playerInput.Move.LengthSquared() > 1f) {
                m_playerInput.Move = Vector3.Normalize(m_playerInput.Move);
            }
            if (m_playerInput.CrouchMove.LengthSquared() > 1f) {
                m_playerInput.CrouchMove = Vector3.Normalize(m_playerInput.CrouchMove);
            }
            if (SplitSourceInventory != null
                && SplitSourceInventory.GetSlotCount(SplitSourceSlotIndex) == 0) {
                SetSplitSourceInventoryAndSlot(null, -1);
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_componentGui = Entity.FindComponent<ComponentGui>(true);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
        }

        public virtual void UpdateInputFromMouseAndKeyboard(WidgetInput input) {
            Vector3 viewPosition = m_componentPlayer.GameWidget.ActiveCamera.ViewPosition;
            Vector3 viewDirection = m_componentPlayer.GameWidget.ActiveCamera.ViewDirection;
            if (m_componentGui.ModalPanelWidget != null
                || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget)) {
                if (!input.IsMouseCursorVisible) {
                    input.IsMouseCursorVisible = true;
                    ViewWidget viewWidget = m_componentPlayer.ViewWidget;
                    SetMousePositionInNextFrame = viewWidget.WidgetToScreen(viewWidget.ActualSize / 2f);
                }
                else if (SetMousePositionInNextFrame.HasValue
                    && input.MousePosition.HasValue) {
                    if (!input.IsPadCursorVisible) {
                        input.MousePosition = SetMousePositionInNextFrame.Value;
                    }
                    SetMousePositionInNextFrame = null;
                }
            }
            else {
                input.IsMouseCursorVisible = false;
                Vector2 zero = Vector2.Zero;
                int num = 0;
                if (Window.IsActive
                    && Time.FrameDuration > 0f) {
                    Point2 mouseMovement = input.MouseMovement;
                    int mouseWheelMovement = input.MouseWheelMovement;
                    float num2 = MathF.Pow(1.4f, 10f * (SettingsManager.LookSensitivity - 0.5f));
                    zero.X = 0.02f * num2 * mouseMovement.X / Time.FrameDuration / 60f;
                    zero.Y = -0.02f * num2 * mouseMovement.Y / Time.FrameDuration / 60f;
                    num = mouseWheelMovement / 120;
                    if (mouseMovement != Point2.Zero) {
                        IsControlledByTouch = false;
                    }
                }
                Vector3 vector = default;
                vector += -Vector3.UnitX * (input.IsKeyOrMouseDown("MoveLeft") ? 1 : 0);
                vector += Vector3.UnitX * (input.IsKeyOrMouseDown("MoveRight") ? 1 : 0);
                vector += Vector3.UnitY * (input.IsKeyOrMouseDown("MoveUp") ? 1 : 0);
                vector += -Vector3.UnitY * (input.IsKeyOrMouseDown("MoveDown") ? 1 : 0);
                vector += -Vector3.UnitZ * (input.IsKeyOrMouseDown("MoveBack") ? 1 : 0);
                vector += Vector3.UnitZ * (input.IsKeyOrMouseDown("MoveFront") ? 1 : 0);
                m_playerInput.Look += new Vector2(Math.Clamp(zero.X, -15f, 15f), Math.Clamp(zero.Y, -15f, 15f));
                m_playerInput.Move += vector;
                m_playerInput.CrouchMove += vector;
                m_playerInput.Jump |= input.IsKeyOrMouseDownOnce("Jump");
                m_playerInput.ScrollInventory -= num;
                m_playerInput.Dig = input.IsKeyOrMouseDown("Dig") ? new Ray3(viewPosition, viewDirection) : m_playerInput.Dig;
                m_playerInput.Hit = input.IsKeyOrMouseDownOnce("Hit") ? new Ray3(viewPosition, viewDirection) : m_playerInput.Hit;
                m_playerInput.Aim = input.IsKeyOrMouseDown("Aim") ? new Ray3(viewPosition, viewDirection) : m_playerInput.Aim;
                m_playerInput.Interact = input.IsKeyOrMouseDownOnce("Interact") ? new Ray3(viewPosition, viewDirection) : m_playerInput.Interact;
                m_playerInput.ToggleCrouch |= input.IsKeyOrMouseDownOnce("ToggleCrouch");
                m_playerInput.ToggleMount |= input.IsKeyOrMouseDownOnce("ToggleMount");
                m_playerInput.ToggleCreativeFly |= input.IsKeyOrMouseDownOnce("ToggleFly");
                m_playerInput.PickBlockType = input.IsKeyOrMouseDownOnce("PickBlockType")
                    ? new Ray3(viewPosition, viewDirection)
                    : m_playerInput.PickBlockType;
            }
            if (!DialogsManager.HasDialogs(m_componentPlayer.GuiWidget) && AllowHandleInput) {
                m_playerInput.ToggleInventory |= input.IsKeyOrMouseDownOnce("ToggleInventory");
                m_playerInput.ToggleClothing |= input.IsKeyOrMouseDownOnce("ToggleClothing");
                m_playerInput.TakeScreenshot |= input.IsKeyOrMouseDownOnce("TakeScreenshot");
                m_playerInput.SwitchCameraMode |= input.IsKeyOrMouseDownOnce("SwitchCameraMode");
                m_playerInput.TimeOfDay |= input.IsKeyOrMouseDownOnce("TimeOfDay");
                m_playerInput.Lighting |= input.IsKeyOrMouseDownOnce("Lightning");
                m_playerInput.Precipitation |= input.IsKeyOrMouseDownOnce("Precipitation");
                m_playerInput.Fog |= input.IsKeyOrMouseDownOnce("Fog");
                m_playerInput.Drop |= input.IsKeyOrMouseDownOnce("Drop");
                m_playerInput.EditItem |= input.IsKeyOrMouseDownOnce("EditItem");
                m_playerInput.KeyboardHelp |= input.IsKeyOrMouseDownOnce("KeyboardHelp");
                if (input.IsKeyDownOnce(Key.Number1)) {
                    m_playerInput.SelectInventorySlot = 0;
                }
                if (input.IsKeyDownOnce(Key.Number2)) {
                    m_playerInput.SelectInventorySlot = 1;
                }
                if (input.IsKeyDownOnce(Key.Number3)) {
                    m_playerInput.SelectInventorySlot = 2;
                }
                if (input.IsKeyDownOnce(Key.Number4)) {
                    m_playerInput.SelectInventorySlot = 3;
                }
                if (input.IsKeyDownOnce(Key.Number5)) {
                    m_playerInput.SelectInventorySlot = 4;
                }
                if (input.IsKeyDownOnce(Key.Number6)) {
                    m_playerInput.SelectInventorySlot = 5;
                }
                if (input.IsKeyDownOnce(Key.Number7)) {
                    m_playerInput.SelectInventorySlot = 6;
                }
                if (input.IsKeyDownOnce(Key.Number8)) {
                    m_playerInput.SelectInventorySlot = 7;
                }
                if (input.IsKeyDownOnce(Key.Number9)) {
                    m_playerInput.SelectInventorySlot = 8;
                }
                if (input.IsKeyDownOnce(Key.Number0)) {
                    m_playerInput.SelectInventorySlot = 9;
                }
            }
            ModsManager.HookAction(
                "UpdateInput",
                loader => {
                    loader.UpdateInput(this, input);
                    return false;
                }
            );
        }

        public virtual void UpdateInputFromGamepad(WidgetInput input) {
            Vector3 viewPosition = m_componentPlayer.GameWidget.ActiveCamera.ViewPosition;
            Vector3 viewDirection = m_componentPlayer.GameWidget.ActiveCamera.ViewDirection;
            if (m_componentGui.ModalPanelWidget != null
                || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget)) {
                if (!input.IsPadCursorVisible) {
                    ViewWidget viewWidget = m_componentPlayer.ViewWidget;
                    Vector2 padCursorPosition = viewWidget.WidgetToScreen(viewWidget.ActualSize / 2f);
                    input.IsPadCursorVisible = true;
                    input.PadCursorPosition = padCursorPosition;
                }
            }
            else {
                input.IsPadCursorVisible = false;
                Vector3 zero = Vector3.Zero;
                Vector2 padStickPosition = input.GetPadStickPosition(GamePadStick.Left, SettingsManager.GamepadDeadZone);
                Vector2 padStickPosition2 = input.GetPadStickPosition(GamePadStick.Right, SettingsManager.GamepadDeadZone);
                float num = MathF.Pow(1.4f, 10f * (SettingsManager.LookSensitivity - 0.5f));
                zero += new Vector3(2f * padStickPosition.X, 0f, 2f * padStickPosition.Y);
                zero += Vector3.UnitY * (input.IsGamepadDown("MoveUp") ? 1 : 0);
                zero += -Vector3.UnitY * (input.IsGamepadDown("MoveDown") ? 1 : 0);
                m_playerInput.Move += zero;
                m_playerInput.CrouchMove += zero;
                m_playerInput.Look += 0.75f * num * padStickPosition2 * MathF.Pow(padStickPosition2.LengthSquared(), 0.25f);
                m_playerInput.Jump |= input.IsGamepadDownOnce("Jump");
                m_playerInput.Dig = input.IsGamepadDown("Dig") ? new Ray3(viewPosition, viewDirection) : m_playerInput.Dig;
                m_playerInput.Hit = input.IsGamepadDownOnce("Hit") ? new Ray3(viewPosition, viewDirection) : m_playerInput.Hit;
                m_playerInput.Aim = input.IsGamepadDown("Aim") ? new Ray3(viewPosition, viewDirection) : m_playerInput.Aim;
                m_playerInput.Interact = input.IsGamepadDownOnce("Interact") ? new Ray3(viewPosition, viewDirection) : m_playerInput.Interact;
                m_playerInput.ToggleMount |= input.IsGamepadDownOnce("ToggleMount");
                m_playerInput.ToggleCrouch |= input.IsGamepadDownOnce("ToggleCrouch");
                m_playerInput.PickBlockType = input.IsGamepadDownOnce("PickBlockType")
                    ? new Ray3(viewPosition, viewDirection)
                    : m_playerInput.PickBlockType;
                if (input.IsPadButtonDownRepeat(GamePadButton.DPadLeft)) {
                    m_playerInput.ScrollInventory--;
                }
                if (input.IsPadButtonDownRepeat(GamePadButton.DPadRight)) {
                    m_playerInput.ScrollInventory++;
                }
                if (padStickPosition != Vector2.Zero
                    || padStickPosition2 != Vector2.Zero) {
                    IsControlledByTouch = false;
                }
            }
            if (!DialogsManager.HasDialogs(m_componentPlayer.GuiWidget) && AllowHandleInput) {
                m_playerInput.ToggleInventory |= input.IsGamepadDownOnce("ToggleInventory");
                m_playerInput.ToggleClothing |= input.IsGamepadDownOnce("ToggleClothing");
                m_playerInput.TakeScreenshot |= input.IsGamepadDownOnce("TakeScreenshot");
                m_playerInput.Drop |= input.IsGamepadDownOnce("Drop");
                m_playerInput.GamepadHelp |= input.IsGamepadDownOnce("GamepadHelp");
                if (m_componentGui.ModalPanelWidget == null) {//避免查看背包时使用十字键翻页时触发一些不该触发的功能
                    m_playerInput.SwitchCameraMode |= input.IsGamepadDownOnce("SwitchCameraMode");
                    m_playerInput.TimeOfDay |= input.IsGamepadDownOnce("TimeOfDay");
                    m_playerInput.Lighting |= input.IsGamepadDownOnce("Lightning");
                    m_playerInput.Precipitation |= input.IsGamepadDownOnce("Precipitation");
                    m_playerInput.Fog |= input.IsGamepadDownOnce("Fog");
                    m_playerInput.EditItem |= input.IsGamepadDownOnce("EditItem");
                }
            }
        }

        public virtual void UpdateInputFromVrControllers(WidgetInput input) {
            if (!IsControlledByVr) {
                return;
            }
            IsControlledByTouch = false;
            // HMD head tracking always runs regardless of modal/dialog state
            Vector3 hmdMatrixYpr = VrManager.HmdMatrixYpr;
            Vector3 hmdLastMatrixYpr = VrManager.HmdLastMatrixYpr;
            Vector3 vector2 = hmdMatrixYpr - hmdLastMatrixYpr;
            if (m_componentPlayer.GameWidget.ActiveCamera is FppCamera) {
                m_playerInput.VrLook = new Vector2(vector2.X, hmdMatrixYpr.Y);
            }
            if (m_componentGui.ModalPanelWidget != null
                || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget)) {
                if (!input.IsVrCursorVisible) {
                    input.IsVrCursorVisible = true;
                }
            }
            else {
                input.IsVrCursorVisible = false;
                float num = MathF.Pow(1.4f, 10f * (SettingsManager.MoveSensitivity - 0.5f));
                float num2 = MathF.Pow(1.4f, 10f * (SettingsManager.LookSensitivity - 0.5f));
                float num3 = Math.Clamp(m_subsystemTime.GameTimeDelta, 0f, 0.1f);
                Vector2 v = Vector2.Normalize(m_componentPlayer.ComponentBody.Matrix.Right.XZ);
                Vector2 v2 = Vector2.Normalize(m_componentPlayer.ComponentBody.Matrix.Forward.XZ);
                Vector2 vrStickPosition = input.GetVrStickPosition(VrController.Left, 0.2f);
                Vector2 vrStickPosition2 = input.GetVrStickPosition(VrController.Right, 0.2f);
                Matrix m = VrManager.HmdMatrixInverted.OrientationMatrix * m_componentPlayer.ComponentCreatureModel.EyeRotation.ToMatrix();
                Vector2 xZ = Vector3.TransformNormal(new Vector3(VrManager.WalkingVelocity.X, 0f, VrManager.WalkingVelocity.Y), m).XZ;
                Vector3 value = Vector3.TransformNormal(new Vector3(VrManager.HeadMove.X, 0f, VrManager.HeadMove.Y), m);
                Vector3 zero = Vector3.Zero;
                if (m_componentPlayer.GameWidget.ActiveCamera is FppCamera) {
                    zero += 0.5f * new Vector3(Vector2.Dot(xZ, v), 0f, Vector2.Dot(xZ, v2));
                }
                zero += new Vector3(2f * vrStickPosition.X, 2f * vrStickPosition2.Y, 2f * vrStickPosition.Y);
                // Teleport mode: suppress left stick forward/strafe
                // (but force Smooth when on ladder or mounted — teleport makes no sense there)
                bool isTeleportMode = SettingsManager.VrMoveControlMode == VrMoveControlMode.Teleport
                    && !m_componentPlayer.ComponentLocomotion.LadderValue.HasValue
                    && m_componentPlayer.ComponentRider.Mount == null;
                if (isTeleportMode) {
                    zero.X -= 2f * vrStickPosition.X;
                    // Only suppress forward, allow backward smooth movement
                    if (vrStickPosition.Y > 0f)
                        zero.Z -= 2f * vrStickPosition.Y;
                }
                m_playerInput.Move += zero;
                m_playerInput.CrouchMove += zero;
                m_playerInput.VrMove = value;
                TouchInput? touchInput = VrManager.GetTouchInput(VrController.Left);
                // Jump: special handling for Trackpad (tap gesture) vs other buttons
                var (jumpCtrl, jumpBtn) = SettingsManager.GetVrMapping("VrJump");
                if (jumpBtn == VrControllerButton.Trackpad) {
                    // Only check the controller specified by the user's mapping
                    TouchInput? jumpTouch = jumpCtrl == VrController.Right
                        ? VrManager.GetTouchInput(VrController.Right)
                        : touchInput;
                    if (jumpTouch.HasValue && num3 > 0f && jumpTouch.Value.InputType == TouchInputType.Tap)
                        m_playerInput.Jump = true;
                }
                else if (jumpBtn != VrControllerButton.Null) {
                    m_playerInput.Jump |= VrManager.IsButtonDownOnce(jumpCtrl, jumpBtn);
                }
                // Touchpad move gestures remain for movement (analog, not remappable)
                // Ignore touchpad when stick is active (dual-input controllers)
                bool leftStickActive = vrStickPosition.LengthSquared() > 0.04f;
                if (touchInput.HasValue
                    && num3 > 0f
                    && !leftStickActive) {
                    if (touchInput.Value.InputType == TouchInputType.Move) {
                        if (isTeleportMode) {
                            // Teleport mode: allow backward touchpad movement, suppress forward/sideways
                            if (touchInput.Value.Move.Y < 0f) {
                                Vector2 move = touchInput.Value.Move;
                                Vector2 vector = 10f * num / num3 * new Vector2(0.5f) * move * MathF.Pow(move.LengthSquared(), 0.175f);
                                m_playerInput.CrouchMove.X += vector.X;
                                m_playerInput.CrouchMove.Z += vector.Y;
                                m_playerInput.Move.Z += ProcessInputValue(touchInput.Value.TotalMoveLimited.Y, 0.1f, 1f);
                            }
                        }
                        else {
                            Vector2 move = touchInput.Value.Move;
                            Vector2 vector = 10f * num / num3 * new Vector2(0.5f) * move * MathF.Pow(move.LengthSquared(), 0.175f);
                            m_playerInput.CrouchMove.X += vector.X;
                            m_playerInput.CrouchMove.Z += vector.Y;
                            m_playerInput.Move.X += ProcessInputValue(touchInput.Value.TotalMoveLimited.X, 0.1f, 1f);
                            m_playerInput.Move.Z += ProcessInputValue(touchInput.Value.TotalMoveLimited.Y, 0.1f, 1f);
                        }
                    }
                }
                if (SettingsManager.VrLookControlMode == VrLookControlMode.Smooth) {
                    m_playerInput.Look += 0.5f * vrStickPosition2 * MathF.Pow(vrStickPosition2.LengthSquared(), 0.25f);
                }
                TouchInput? touchInput2 = VrManager.GetTouchInput(VrController.Right);
                Vector2 zero2 = Vector2.Zero;
                if (touchInput2.HasValue) {
                    if (touchInput2.Value.InputType == TouchInputType.Move) {
                        zero2.X = touchInput2.Value.Move.X;
                        m_playerInput.Move.Y += ProcessInputValue(touchInput2.Value.TotalMoveLimited.Y, 0.1f, 1f);
                    }
                }
                // Snap turn or smooth turn for VR look
                if (SettingsManager.VrLookControlMode == VrLookControlMode.Snap) {
                    // Combine right stick X and touchpad X for snap input
                    float snapInput = vrStickPosition2.X;
                    if (touchInput2.HasValue && touchInput2.Value.InputType == TouchInputType.Move)
                        snapInput += zero2.X;
                    if (!m_vrSnapTurning) {
                        float snapAngle = 0f;
                        if (snapInput > 0.5f) snapAngle = -MathF.PI / 2f;
                        else if (snapInput < -0.5f) snapAngle = MathF.PI / 2f;
                        if (snapAngle != 0f) {
                            if (m_playerInput.VrLook.HasValue) {
                                // FppCamera: use VrLook path (body + pitch)
                                m_playerInput.VrLook = new Vector2(
                                    m_playerInput.VrLook.Value.X + snapAngle,
                                    m_playerInput.VrLook.Value.Y);
                            }
                            else if (m_componentPlayer.GameWidget.ActiveCamera.UsesMovementControls) {
                                // OrbitCamera/DebugCamera: route to camera rotation
                                VrSnapCameraRotation += snapAngle;
                            }
                            else {
                                // TppCamera/FixedCamera: route to body rotation
                                VrSnapBodyRotation += snapAngle;
                            }
                            m_vrSnapTurning = true;
                            m_vrSnapFadeTimer = 0.2f;
                        }
                    }
                    else if (MathF.Abs(snapInput) < 0.2f) {
                        m_vrSnapTurning = false;
                    }
                }
                else {
                    if (num3 > 0f) {
                        m_vrSmoothLook = Vector2.Lerp(m_vrSmoothLook, zero2, 14f * num3);
                        m_playerInput.Look += num2 / num3 * new Vector2(0.25f) * m_vrSmoothLook * MathF.Pow(m_vrSmoothLook.LengthSquared(), 0.3f);
                    }
                }
                // Update snap turn fade
                if (m_vrSnapFadeTimer > 0f) {
                    m_vrSnapFadeTimer = MathF.Max(0f, m_vrSnapFadeTimer - num3);
                    // Fade in/out: first half black, second half fade out
                    float halfFade = 0.1f;
                    if (m_vrSnapFadeTimer > halfFade)
                        VrFadeAlpha = 1f;
                    else
                        VrFadeAlpha = m_vrSnapFadeTimer / halfFade;
                }
                else {
                    VrFadeAlpha = 0f;
                }
                // Teleport mode: track left stick/touchpad, compute arc, execute on release
                if (isTeleportMode) {
                    bool touchpadMove = touchInput.HasValue && touchInput.Value.InputType == TouchInputType.Move
                        && !leftStickActive; // Ignore touchpad when stick is active
                    // Activation threshold (high) vs hold threshold (low) — hysteresis
                    bool stickForward = vrStickPosition.Y >= 0f && vrStickPosition.LengthSquared() > 0.25f;
                    bool touchpadForward = touchpadMove && touchInput.Value.Move.Y >= 0f;
                    bool stickHeld = vrStickPosition.Y >= 0f && vrStickPosition.LengthSquared() > 0.04f;
                    // Suppress activation after backward until fully centered (prevents touchpad dual-report from triggering)
                    if (vrStickPosition.Y < 0f && vrStickPosition.LengthSquared() > 0.04f) {
                        m_vrTeleportSuppressAfterBackward = true;
                    }
                    if (vrStickPosition.LengthSquared() < 0.01f) {
                        m_vrTeleportSuppressAfterBackward = false;
                    }

                    if ((stickForward || touchpadForward) && !m_vrTeleportStickActive && !m_vrTeleportSuppressAfterBackward) {
                        m_vrTeleportStickActive = true;
                    }
                    if (m_vrTeleportStickActive) {
                        if (stickHeld || touchpadForward) {
                            ComputeTeleportArc();
                        }
                        else if (vrStickPosition.LengthSquared() < 0.04f && !touchpadMove) {
                            // Stick returned to center — execute teleport if valid
                            m_vrTeleportStickActive = false;
                            if (m_vrTeleportValid && m_vrTeleportTarget.HasValue) {
                                ExecuteTeleport(m_vrTeleportTarget.Value, m_vrTeleportForceCrouch);
                            }
                            m_vrTeleportArcPoints.Clear();
                            m_vrTeleportTarget = null;
                            m_vrTeleportValid = false;
                        }
                        else {
                            // Stick pulled backward — cancel teleport without executing
                            m_vrTeleportStickActive = false;
                            m_vrTeleportArcPoints.Clear();
                            m_vrTeleportTarget = null;
                            m_vrTeleportValid = false;
                        }
                    }
                }
                else {
                    m_vrTeleportArcPoints.Clear();
                    m_vrTeleportTarget = null;
                    m_vrTeleportValid = false;
                    m_vrTeleportStickActive = false;
                    m_vrTeleportSuppressAfterBackward = false;
                }
                if (VrManager.IsControllerPresent(VrController.Right)) {
                    var (hitCtrl, hitBtn) = SettingsManager.GetVrMapping("VrHit");
                    var (digCtrl, digBtn) = SettingsManager.GetVrMapping("VrDig");
                    var (aimCtrl, aimBtn) = SettingsManager.GetVrMapping("VrAim");
                    var (interactCtrl, interactBtn) = SettingsManager.GetVrMapping("VrInteract");
                    if (digBtn != VrControllerButton.Null)
                        m_playerInput.Dig = VrManager.IsButtonDown(digCtrl, digBtn)
                            ? CalculateVrHandRay()
                            : m_playerInput.Dig;
                    if (hitBtn != VrControllerButton.Null)
                        m_playerInput.Hit = VrManager.IsButtonDownOnce(hitCtrl, hitBtn)
                            ? CalculateVrHandRay()
                            : m_playerInput.Hit;
                    if (aimBtn != VrControllerButton.Null)
                        m_playerInput.Aim = VrManager.IsButtonDown(aimCtrl, aimBtn)
                            ? CalculateVrHandRay()
                            : m_playerInput.Aim;
                    if (interactBtn != VrControllerButton.Null)
                        m_playerInput.Interact = VrManager.IsButtonDownOnce(interactCtrl, interactBtn)
                            ? CalculateVrHandRay()
                            : m_playerInput.Interact;
                }
                m_playerInput.ToggleMount |= IsVrActionDownOnce("VrToggleMount");
                m_playerInput.ToggleCrouch |= IsVrActionDownOnce("VrToggleCrouch");
                m_playerInput.EditItem |= IsVrActionDownOnce("VrEditItem");
                m_playerInput.ToggleCreativeFly |= IsVrActionDownOnce("VrToggleFly");
                if (IsVrActionDownOnce("VrScrollLeft")) {
                    m_playerInput.ScrollInventory--;
                }
                if (IsVrActionDownOnce("VrScrollRight")) {
                    m_playerInput.ScrollInventory++;
                }
                m_playerInput.Drop |= IsVrActionDownOnce("VrDrop");
                m_playerInput.SwitchCameraMode |= IsVrActionDownOnce("VrSwitchCameraMode");
            }
            if (!DialogsManager.HasDialogsOrAnimations(m_componentPlayer.GuiWidget)) {
                if (input.IsVrButtonDownOnce(VrController.Right, VrControllerButton.Menu)
                    || input.IsVrButtonDownOnce(VrController.Left, VrControllerButton.Menu)) {
                    input.Back = true;
                }
                m_playerInput.ToggleInventory |= IsVrActionDownOnce("VrToggleInventory");
                m_playerInput.ToggleClothing |= IsVrActionDownOnce("VrToggleClothing");
            }
        }

        public virtual void UpdateInputFromWidgets(WidgetInput input) {
            float num = MathF.Pow(1.25f, 10f * (SettingsManager.MoveSensitivity - 0.5f));
            float num2 = MathF.Pow(1.25f, 10f * (SettingsManager.LookSensitivity - 0.5f));
            float num3 = Math.Clamp(m_subsystemTime.GameTimeDelta, 0f, 0.1f);
            ViewWidget viewWidget = m_componentPlayer.ViewWidget;
            m_componentGui.MoveWidget.Radius = 30f / num * m_componentGui.MoveWidget.GlobalScale;
            if (m_componentGui.ModalPanelWidget != null
                || !(m_subsystemTime.GameTimeFactor > 0f)
                || !(num3 > 0f)) {
                return;
            }
            Vector2 v = new(SettingsManager.LeftHandedLayout ? 96 : -96, -96f);
            v = Vector2.TransformNormal(v, input.Widget.GlobalTransform);
            if (m_componentGui.ViewWidget != null
                && m_componentGui.ViewWidget.TouchInput.HasValue) {
                IsControlledByTouch = true;
                TouchInput value = m_componentGui.ViewWidget.TouchInput.Value;
                Camera activeCamera = m_componentPlayer.GameWidget.ActiveCamera;
                Vector3 viewPosition = activeCamera.ViewPosition;
                Vector3 viewDirection = activeCamera.ViewDirection;
                Vector3 direction = Vector3.Normalize(activeCamera.ScreenToWorld(new Vector3(value.Position, 1f), Matrix.Identity) - viewPosition);
                Vector3 direction2 = Vector3.Normalize(
                    activeCamera.ScreenToWorld(new Vector3(value.Position + v, 1f), Matrix.Identity) - viewPosition
                );
                if (value.InputType == TouchInputType.Tap) {
                    if (SettingsManager.LookControlMode == LookControlMode.SplitTouch) {
                        m_playerInput.Interact = new Ray3(viewPosition, viewDirection);
                        m_playerInput.Hit = new Ray3(viewPosition, viewDirection);
                    }
                    else {
                        m_playerInput.Interact = new Ray3(viewPosition, direction);
                        m_playerInput.Hit = new Ray3(viewPosition, direction);
                    }
                }
                else if (value.InputType == TouchInputType.Hold
                    && value.DurationFrames > 1
                    && value.Duration > 0.2f) {
                    if (SettingsManager.LookControlMode == LookControlMode.SplitTouch) {
                        m_playerInput.Dig = new Ray3(viewPosition, viewDirection);
                        m_playerInput.Aim = new Ray3(viewPosition, direction2);
                    }
                    else {
                        m_playerInput.Dig = new Ray3(viewPosition, direction);
                        m_playerInput.Aim = new Ray3(viewPosition, direction2);
                    }
                    m_isViewHoldStarted = true;
                }
                else if (value.InputType == TouchInputType.Move) {
                    if (SettingsManager.LookControlMode == LookControlMode.EntireScreen
                        || SettingsManager.LookControlMode == LookControlMode.SplitTouch) {
                        Vector2 v2 = Vector2.TransformNormal(value.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
                        Vector2 vector = num2 / num3 * new Vector2(0.0006f, -0.0006f) * v2 * MathF.Pow(v2.LengthSquared(), 0.125f);
                        m_playerInput.Look += vector;
                    }
                    if (m_isViewHoldStarted) {
                        if (SettingsManager.LookControlMode == LookControlMode.SplitTouch) {
                            m_playerInput.Dig = new Ray3(viewPosition, viewDirection);
                            m_playerInput.Aim = new Ray3(viewPosition, direction2);
                        }
                        else {
                            m_playerInput.Dig = new Ray3(viewPosition, direction);
                            m_playerInput.Aim = new Ray3(viewPosition, direction2);
                        }
                    }
                }
            }
            else {
                m_isViewHoldStarted = false;
            }
            if (m_componentGui.MoveWidget != null
                && m_componentGui.MoveWidget.TouchInput.HasValue) {
                IsControlledByTouch = true;
                float radius = m_componentGui.MoveWidget.Radius;
                TouchInput value2 = m_componentGui.MoveWidget.TouchInput.Value;
                if (value2.InputType == TouchInputType.Tap) {
                    m_playerInput.Jump = true;
                }
                else if (value2.InputType == TouchInputType.Move
                    || value2.InputType == TouchInputType.Hold) {
                    Vector2 v3 = Vector2.TransformNormal(value2.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
                    Vector2 vector2 = num / num3 * new Vector2(0.003f, -0.003f) * v3 * MathF.Pow(v3.LengthSquared(), 0.175f);
                    m_playerInput.CrouchMove.X += vector2.X;
                    m_playerInput.CrouchMove.Z += vector2.Y;
                    Vector2 vector3 = Vector2.TransformNormal(value2.TotalMoveLimited, m_componentGui.ViewWidget.InvertedGlobalTransform);
                    m_playerInput.Move.X += ProcessInputValue(vector3.X * viewWidget.GlobalScale, 0.2f * radius, radius);
                    m_playerInput.Move.Z += ProcessInputValue((0f - vector3.Y) * viewWidget.GlobalScale, 0.2f * radius, radius);
                }
            }
            if (m_componentGui.MoveRoseWidget != null) {
                if (m_componentGui.MoveRoseWidget.Direction != Vector3.Zero
                    || m_componentGui.MoveRoseWidget.Jump) {
                    IsControlledByTouch = true;
                }
                m_playerInput.Move += m_componentGui.MoveRoseWidget.Direction;
                m_playerInput.CrouchMove += m_componentGui.MoveRoseWidget.Direction;
                m_playerInput.Jump |= m_componentGui.MoveRoseWidget.Jump;
            }
            if (m_componentGui.LookWidget != null
                && m_componentGui.LookWidget.TouchInput.HasValue) {
                IsControlledByTouch = true;
                TouchInput value3 = m_componentGui.LookWidget.TouchInput.Value;
                if (value3.InputType == TouchInputType.Tap) {
                    m_playerInput.Jump = true;
                }
                else if (value3.InputType == TouchInputType.Move) {
                    Vector2 v4 = Vector2.TransformNormal(value3.Move, m_componentGui.ViewWidget.InvertedGlobalTransform);
                    Vector2 vector4 = num2 / num3 * new Vector2(0.0006f, -0.0006f) * v4 * MathF.Pow(v4.LengthSquared(), 0.125f);
                    m_playerInput.Look += vector4;
                }
            }
        }

        public static float ProcessInputValue(float value, float deadZone, float saturationZone) =>
            MathF.Sign(value) * Math.Clamp((MathF.Abs(value) - deadZone) / (saturationZone - deadZone), 0f, 1f);

        static bool IsVrActionDown(string action) {
            var (ctrl, btn) = SettingsManager.GetVrMapping(action);
            if (btn == VrControllerButton.Null) return false;
            if (VrManager.IsButtonDown(ctrl, btn)) return true;
            if (SettingsManager.GetVrActionAlternative(action) is VrControllerButton alt)
                return VrManager.IsButtonDown(ctrl, alt);
            return false;
        }

        static bool IsVrActionDownOnce(string action) {
            var (ctrl, btn) = SettingsManager.GetVrMapping(action);
            if (btn == VrControllerButton.Null) return false;
            if (VrManager.IsButtonDownOnce(ctrl, btn)) return true;
            if (SettingsManager.GetVrActionAlternative(action) is VrControllerButton alt)
                return VrManager.IsButtonDownOnce(ctrl, alt);
            return false;
        }

        void ComputeTeleportArc() {
            m_vrTeleportTarget = null;
            m_vrTeleportValid = false;
            m_vrTeleportForceCrouch = false;

            // Get left controller world position and direction
            Camera activeCamera = m_componentPlayer.GameWidget.ActiveCamera;
            Matrix worldMatrix = VrManager.HmdMatrixInverted
                * Matrix.CreateWorld(activeCamera.ViewPosition, activeCamera.ViewDirection, activeCamera.ViewUp);
            Matrix controllerMatrix = VrManager.GetControllerMatrix(VrController.Left) * worldMatrix;

            Vector3 origin = controllerMatrix.Translation;
            Vector3 direction = controllerMatrix.Forward;

            bool isCreativeFlyEnabled = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
            float initialSpeed = isCreativeFlyEnabled ? 16f : 8f;
            int numPoints = isCreativeFlyEnabled ? 60 : 30;
            float maxRange = isCreativeFlyEnabled ? float.PositiveInfinity : 20f;
            const float gravity = 10f;
            const float timeStep = 0.05f;

            m_vrTeleportArcPoints.Clear();
            m_vrTeleportArcPoints.Add(origin);
            m_vrTeleportHitFace = 4;

            Vector3 velocity = direction * initialSpeed;
            Vector3 pos = origin;

            for (int i = 1; i <= numPoints; i++) {
                Vector3 newPos = pos + velocity * timeStep;
                velocity.Y -= gravity * timeStep;

                // Raycast between consecutive points — only accept collidable blocks
                TerrainRaycastResult? hit = m_subsystemTerrain.Raycast(pos, newPos, false, true,
                    (value, _) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable_(value));
                if (hit.HasValue) {
                    m_vrTeleportArcPoints.Add(hit.Value.HitPoint());
                    ValidateTeleportLanding(hit.Value);
                    return;
                }

                if (Vector3.Distance(origin, newPos) > maxRange) {
                    m_vrTeleportArcPoints.Add(newPos);
                    return;
                }

                m_vrTeleportArcPoints.Add(newPos);
                pos = newPos;
            }
        }

        int CountClearance(int cellX, int cellY, int cellZ) {
            int clearance = 0;
            for (int y = Math.Max(cellY, 0); y <= cellY + 1 && y < 255; y++) {
                int value = m_subsystemTerrain.Terrain.GetCellValue(cellX, y, cellZ);
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                if (block is LadderBlock || !block.IsCollidable_(value))
                    clearance++;
                else break;
            }
            return clearance;
        }

        void ApplyTeleportResult(Vector3 target) {
            int cellX = Terrain.ToCell(target.X);
            int cellY = Terrain.ToCell(target.Y);
            int cellZ = Terrain.ToCell(target.Z);
            int clearance = CountClearance(cellX, cellY, cellZ);
            if (clearance >= 2) {
                m_vrTeleportTarget = target;
                m_vrTeleportValid = true;
            }
            else if (clearance == 1) {
                m_vrTeleportTarget = target;
                m_vrTeleportValid = true;
                m_vrTeleportForceCrouch = true;
            }
        }

        void ValidateTeleportLanding(TerrainRaycastResult hit) {
            int face = hit.CellFace.Face;
            bool isCreativeFly = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
            if (face == 5
                && !isCreativeFly) {
                return;
            }
            m_vrTeleportHitFace = face;
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(hit.Value)];

            // Wall hit (face 0-3): only allow if creative fly or LadderBlock
            if (face >= 0 && face <= 3) {
                if (isCreativeFly || block is LadderBlock) {
                    Vector3 hitPoint = hit.HitPoint();
                    Vector3 normal = CellFace.FaceToVector3(face);
                    // Offset slightly into the space in front of the wall
                    Vector3 playerPos = hitPoint + normal * 0.5f;
                    ApplyTeleportResult(playerPos);
                }
                return;
            }

            // Floor hit (face 4 = top): land above the block
            // Ceiling hit (face 5 = bottom): land below the block (fly mode)
            Vector3 landingPoint = hit.HitPoint();
            int standY = face == 5 ? hit.CellFace.Y - 1 : hit.CellFace.Y + 1;
            Vector3 target = new Vector3(landingPoint.X, standY, landingPoint.Z);
            ApplyTeleportResult(target);
        }

        void ExecuteTeleport(Vector3 target, bool forceCrouch) {
            if (m_componentPlayer.ComponentRider.Mount != null) return;
            if (m_componentPlayer.ComponentHealth.Health <= 0f) return;
            if (m_componentGui.ModalPanelWidget != null
                || DialogsManager.HasDialogs(m_componentPlayer.GuiWidget)) return;

            // Re-validate clearance at target (terrain may have changed)
            int cellX = Terrain.ToCell(target.X);
            int cellY = Terrain.ToCell(target.Y);
            int cellZ = Terrain.ToCell(target.Z);
            if (CountClearance(cellX, cellY, cellZ) == 0) return;

            ComponentBody body = m_componentPlayer.ComponentBody;
            body.Position = target;
            body.Velocity = Vector3.Zero;

            if (forceCrouch) {
                body.IsCrouching = true;
                body.CrouchFactor = 1f;
            }
            else {
                body.TargetCrouchFactor = 0f;
            }

            m_vrSnapFadeTimer = 0.2f;
        }
    }
}