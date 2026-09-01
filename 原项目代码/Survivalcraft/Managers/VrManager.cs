using Engine;
using Engine.Graphics;
using Silk.NET.OpenGLES;
#if ANDROID
using Engine.Input;
#endif

namespace Game {
    public class VrManager {
        static IVrBackend _backend;
        static bool m_frameActive;
        static bool m_eyesRendered;
#if WINDOWS
        static int m_savedPresentationInterval;
        static bool m_hasSavedPresentationInterval;
#endif
        static bool m_vrMappingDefaultsEnsured;

        struct VrTouchTracker {
            public bool ClickActive;
            public Vector2 ClickStartStick;
            public float ClickStartTime;
            public int ClickStartFrame;
            public bool MoveActive;
            public Vector2 MoveStartStick;
            public float MoveStartTime;
            public int MoveStartFrame;
            public Vector2 LastStick;
        }

        static VrTouchTracker[] m_touchTrackers = [default, default];

        public static void SetBackend(IVrBackend backend) => _backend = backend;

        public static bool IsVrAvailable => _backend?.IsAvailable ?? false;

        public static bool IsVrStarted => _backend?.IsStarted ?? false;

        public static VrControllerType ControllerType => _backend?.ControllerType ?? VrControllerType.Unknown;

        public static bool IsFrameActive => m_frameActive;

        [Obsolete]
        public static RenderTarget2D VrRenderTarget => _backend?.VrRenderTarget;

        public static Matrix HmdMatrix => _backend?.HmdMatrix ?? default;

        public static Matrix HmdMatrixInverted => _backend?.HmdMatrixInverted ?? default;

        public static Vector3 HmdMatrixYpr => _backend?.HmdMatrixYpr ?? default;

        public static Matrix HmdLastMatrix => _backend?.HmdLastMatrix ?? default;

        public static Matrix HmdLastMatrixInverted => _backend?.HmdLastMatrixInverted ?? default;

        public static Vector3 HmdLastMatrixYpr => _backend?.HmdLastMatrixYpr ?? default;

        public static Vector2 HeadMove => _backend?.HeadMove ?? default;

        public static Vector2 WalkingVelocity => _backend?.WalkingVelocity ?? default;

        public static void Initialize() {
            Window.Closed += Shutdown;
            _backend?.Initialize();
        }

        public static bool StartVr() {
            m_vrMappingDefaultsEnsured = false;
            _backend?.StartVr();
            if (IsVrStarted) {
#if ANDROID
                Keyboard.IsAndroidDialogAvailable = false;
#endif
                // Disable VSync so the frame loop isn't throttled by the
                // desktop monitor's refresh rate. VR frame pacing is handled
                // by xrWaitFrame instead.
                // Only needed on Windows where the desktop monitor may be 60Hz.
                // On Android VR headsets the display IS the VR display, so
                // VSync at the panel refresh rate is already correct.
#if WINDOWS
                if (!m_hasSavedPresentationInterval) {
                    m_savedPresentationInterval = Window.PresentationInterval;
                    m_hasSavedPresentationInterval = true;
                }
                Window.PresentationInterval = 0;
#endif
            }
            return IsVrStarted;
        }

        public static void StopVr() {
            _backend?.StopVr();
            m_frameActive = false;
            m_vrMappingDefaultsEnsured = false;
#if ANDROID
            Keyboard.IsAndroidDialogAvailable = true;
#endif
            // Restore the user's VSync preference.
#if WINDOWS
            if (m_hasSavedPresentationInterval) {
                Window.PresentationInterval = m_savedPresentationInterval;
                m_hasSavedPresentationInterval = false;
            }
#endif
        }

        [Obsolete]
        public static void WaitGetPoses() { }

        [Obsolete]
        public static void SubmitEyeTexture(VrEye eye, Texture2D texture) { }

        public static Matrix GetEyeToHeadTransform(VrEye eye) => _backend?.GetEyeToHeadTransform((Engine.VrEye)eye) ?? default;

        public static Matrix GetProjectionMatrix(VrEye eye, float near, float far) => _backend?.GetProjectionMatrix((Engine.VrEye)eye, near, far) ?? default;

        public static bool IsControllerPresent(VrController controller) => _backend?.IsControllerPresent(controller) ?? false;

        public static Matrix GetControllerMatrix(VrController controller) => _backend?.GetControllerMatrix(controller) ?? default;

        public static Vector2 GetStickPosition(VrController controller, float deadZone = 0f) => _backend?.GetStickPosition(controller, deadZone) ?? default;

        public static Vector2? GetTouchpadPosition(VrController controller, float deadZone = 0f) => _backend?.GetTouchpadPosition(controller, deadZone);

        public static float GetTriggerPosition(VrController controller, float deadZone = 0f) => _backend?.GetTriggerPosition(controller, deadZone) ?? 0f;

        public static bool IsButtonDown(VrController controller, VrControllerButton button) => _backend?.IsButtonDown(controller, button) ?? false;

        public static bool IsButtonDownOnce(VrController controller, VrControllerButton button) => _backend?.IsButtonDownOnce(controller, button) ?? false;

        public static TouchInput? GetTouchInput(VrController controller) {
            if (!IsVrStarted) return null;
            int idx = (int)controller;
            ref VrTouchTracker tracker = ref m_touchTrackers[idx];

            Vector2 stick = GetStickPosition(controller, 0f);
            bool clicked = IsButtonDown(controller, VrControllerButton.Trackpad);
            float now = (float)Time.FrameStartTime;
            int frame = Time.FrameIndex;

            const float MOVE_THRESHOLD = 0.3f;
            const float TAP_MAX_DURATION = 0.3f;
            const float HOLD_MIN_DURATION = 0.5f;

            bool stickDeflected = stick.LengthSquared() > MOVE_THRESHOLD * MOVE_THRESHOLD;

            TouchInput? result = null;

            if (clicked) {
                if (!tracker.ClickActive) {
                    tracker.ClickActive = true;
                    tracker.MoveActive = false;
                    tracker.ClickStartStick = stick;
                    tracker.ClickStartTime = now;
                    tracker.ClickStartFrame = frame;
                }
                float duration = now - tracker.ClickStartTime;
                if (duration >= HOLD_MIN_DURATION) {
                    Vector2 totalMove = stick - tracker.ClickStartStick;
                    result = new TouchInput {
                        InputType = TouchInputType.Hold,
                        Position = tracker.ClickStartStick,
                        Move = stick - tracker.LastStick,
                        TotalMove = totalMove,
                        TotalMoveLimited = ClampMove(totalMove),
                        Duration = duration,
                        DurationFrames = frame - tracker.ClickStartFrame
                    };
                }
            }
            else {
                if (tracker.ClickActive) {
                    tracker.ClickActive = false;
                    float duration = now - tracker.ClickStartTime;
                    if (duration < TAP_MAX_DURATION) {
                        result = new TouchInput {
                            InputType = TouchInputType.Tap,
                            Position = tracker.ClickStartStick,
                            Move = Vector2.Zero,
                            TotalMove = stick - tracker.ClickStartStick,
                            TotalMoveLimited = ClampMove(stick - tracker.ClickStartStick),
                            Duration = duration,
                            DurationFrames = frame - tracker.ClickStartFrame
                        };
                    }
                }
                if (result == null && stickDeflected) {
                    if (!tracker.MoveActive) {
                        tracker.MoveActive = true;
                        tracker.MoveStartStick = stick;
                        tracker.MoveStartTime = now;
                        tracker.MoveStartFrame = frame;
                    }
                    Vector2 totalMove = stick - tracker.MoveStartStick;
                    result = new TouchInput {
                        InputType = TouchInputType.Move,
                        Position = stick,
                        Move = stick - tracker.LastStick,
                        TotalMove = totalMove,
                        TotalMoveLimited = ClampMove(totalMove),
                        Duration = now - tracker.MoveStartTime,
                        DurationFrames = frame - tracker.MoveStartFrame
                    };
                }
                else if (result == null) {
                    tracker.MoveActive = false;
                }
            }

            tracker.LastStick = stick;
            return result;
        }

        static Vector2 ClampMove(Vector2 v) {
            float len = v.Length();
            return len > 1f ? v * (1f / len) : v;
        }

        public static bool BeginFrame() {
            m_eyesRendered = false;
            m_frameActive = _backend?.BeginFrame() ?? false;
            if (m_frameActive && !m_vrMappingDefaultsEnsured && ControllerType != VrControllerType.Unknown) {
                SettingsManager.EnsureVrMappingDefaults(ControllerType);
                m_vrMappingDefaultsEnsured = true;
            }
            return m_frameActive;
        }

        public static EyeFrame GetEyeFrame(VrEye eye) => _backend?.GetEyeFrame((Engine.VrEye)eye) ?? default;

        public static void ReleaseEye(VrEye eye) => _backend?.ReleaseEye((Engine.VrEye)eye);

        public static void EndFrame() {
            if (m_eyesRendered) {
                _backend?.EndFrame();
            }
            else {
                // RenderToEyes was not called this frame — submit 0 layers
                // to prevent the compositor from showing stale swapchain images
                // (e.g. VR background from a previous menu frame).
                _backend?.EndFrameEmpty();
            }
            m_frameActive = false;
        }

        public static void Update() => _backend?.Update();

        public static void Shutdown() {
            if (_backend == null) return;
            try {
                Program.DisableVrCameras();
                _backend.Dispose();
            }
            catch (Exception ex) {
                Log.Error($"VR shutdown error: {ex}");
            }
            _backend = null;
        }

        public static int SwapchainWidth => _backend?.SwapchainWidth ?? 0;

        public static int SwapchainHeight => _backend?.SwapchainHeight ?? 0;

        public static void RenderToEyes(Action<VrEye, EyeFrame> renderAction) {
            if (!m_frameActive) return;
            m_eyesRendered = true;
            int vrW = SwapchainWidth;
            int vrH = SwapchainHeight;
            int origFbo = GLWrapper.m_mainFramebuffer;
            Point2? origOverride = Display.BackbufferSizeOverride;
            Viewport origViewport = Display.Viewport;
            Rectangle origScissor = Display.ScissorRectangle;
            RenderTarget2D origRenderTarget = Display.RenderTarget;

            try {
                for (int eye = 0; eye < 2; eye++) {
                    VrEye vrEye = (VrEye)eye;
                    EyeFrame eyeFrame = GetEyeFrame(vrEye);

                    Display.BackbufferSizeOverride = new Point2(vrW, vrH);
                    GLWrapper.m_mainFramebuffer = eyeFrame.Fbo;
                    GLWrapper.BindFramebuffer(eyeFrame.Fbo);
                    GLWrapper.Disable(EnableCap.FramebufferSrgb);
                    Display.RenderTarget = null;
                    Viewport vp = new(0, 0, vrW, vrH);
                    Rectangle sc = new(0, 0, vrW, vrH);
                    Display.Viewport = vp;
                    Display.ScissorRectangle = sc;
                    GLWrapper.ApplyViewportScissor(vp, sc, true);
                    // Use GLWrapper.Clear to reset ColorMask(0xF) and DepthMask(true)
                    // before clearing. A manual GL.Clear would skip these resets,
                    // leaving stale content if a previous render pass disabled depth
                    // or color writes (e.g. transparent-object passes).
                    GLWrapper.Clear(null, new Vector4(0, 0, 0, 1), 1f, null);

                    renderAction(vrEye, eyeFrame);
                }
            }
            finally {
                GLWrapper.m_mainFramebuffer = origFbo;
                GLWrapper.BindFramebuffer(origFbo);
                Display.BackbufferSizeOverride = origOverride;
                Display.Viewport = origViewport;
                Display.ScissorRectangle = origScissor;
                Display.RenderTarget = origRenderTarget;

                for (int eye = 0; eye < 2; eye++) {
                    ReleaseEye((VrEye)eye);
                }
            }
        }
    }
}
