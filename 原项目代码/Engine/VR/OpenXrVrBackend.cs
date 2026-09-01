#if WINDOWS || ANDROID
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Engine.Graphics;
using Silk.NET.OpenGLES;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
#if ANDROID
using Java.Interop;
#endif
using XrAction = Silk.NET.OpenXR.Action;

namespace Engine {
    public abstract unsafe class OpenXrVrBackend : IVrBackend {
        XR m_xr;
        Instance m_instance;
        Task m_initTask;
        volatile bool m_initStarted;
        ulong m_systemId;
        Session m_session;
        Space m_playSpace;
#if WINDOWS
        KhrOpenglEnable m_glExt;
#else
        KhrOpenglEsEnable m_glExt;
#endif
        SessionState m_sessionState;

        // Swapchains
        readonly Swapchain[] m_swapchains = new Swapchain[2];
        uint[][] m_swapchainImages = [[], []];
        uint[] m_swapchainFbos = [0, 0];
        uint[] m_swapchainDepthRbs = [0, 0];
        uint m_acquiredImageIndex;

        // Frame state
        FrameState m_frameState;
        View[] m_views;
        CompositionLayerProjectionView[] m_layerViews;

        // Config
        uint m_swapchainWidth;
        uint m_swapchainHeight;
        InternalFormat m_swapchainFormat;

        // Actions
        ActionSet m_actionSet;
        readonly XrAction[] m_triggerActions = new XrAction[2];
        readonly XrAction[] m_gripActions = new XrAction[2];
        readonly XrAction[] m_gripClickActions = new XrAction[2];
        readonly XrAction[] m_menuActions = new XrAction[2];
        readonly XrAction[] m_stickXActions = new XrAction[2];
        readonly XrAction[] m_stickYActions = new XrAction[2];
        readonly XrAction[] m_poseActions = new XrAction[2];
        readonly XrAction[] m_stickClickActions = new XrAction[2];
        readonly XrAction[] m_primaryActions = new XrAction[2];
        readonly XrAction[] m_secondaryActions = new XrAction[2];
        readonly XrAction[] m_thumbrestActions = new XrAction[2];
        readonly XrAction[] m_trackpadXActions = new XrAction[2];
        readonly XrAction[] m_trackpadYActions = new XrAction[2];
        readonly Space[] m_controllerSpaces = new Space[2];

        // Controller state
        ControllerState[] m_controllers = [default, default];
        ControllerState[] m_lastControllers = [default, default];

        // HMD state
        Matrix m_hmdMatrix;
        Matrix m_hmdMatrixInverted;
        Vector3 m_hmdMatrixYpr;
        Matrix m_hmdLastMatrix;
        Matrix m_hmdLastMatrixInverted;
        Vector3 m_hmdLastMatrixYpr;
        Vector2 m_headMove;

        // Recenter compensation: some runtimes (Quest, SteamVR) shift the reference
        // space origin on recenter, violating the OpenXR spec. Track cumulative Y
        // offset so HmdMatrix.Translation.Y stays accurate regardless.
        float m_recenterYOffset;
        bool m_recenterPending;
        float m_hmdYBeforeRecenterRaw; // raw Y (without offset) at recenter time
        bool m_trackingEstablished;    // true after first valid HMD pose

        // Platform abstract methods
        protected abstract StructureType GraphicsBindingType { get; }
        protected abstract int GetGraphicsBindingSize();
        protected abstract void PopulateGraphicsBinding(void* bindingPtr);

        struct ControllerState {
            public bool IsConnected;
            public Vector2 Stick;
            public Vector2 Trackpad;
            public float Trigger;
            public bool Grip;
            public bool Menu;
            public bool StickClick;
            public bool Primary;
            public bool Secondary;
            public bool Thumbrest;
            public Matrix Matrix;
        }

        // IVrBackend properties
        // Volatile-backed: IsAvailable is written by the background init thread
        // (Windows) and read from the main thread.
        volatile bool m_isAvailable;
        public bool IsAvailable {
            get => m_isAvailable;
            private set => m_isAvailable = value;
        }
        public bool IsStarted { get; private set; }
        public VrControllerType ControllerType { get; private set; } = VrControllerType.Unknown;
        int m_controllerDetectAttempts;
        public Matrix HmdMatrix => m_hmdMatrix;
        public Matrix HmdMatrixInverted => m_hmdMatrixInverted;
        public Vector3 HmdMatrixYpr => m_hmdMatrixYpr;
        public Matrix HmdLastMatrix => m_hmdLastMatrix;
        public Matrix HmdLastMatrixInverted => m_hmdLastMatrixInverted;
        public Vector3 HmdLastMatrixYpr => m_hmdLastMatrixYpr;
        public Vector2 HeadMove => m_headMove;
        public int SwapchainWidth => (int)m_swapchainWidth;
        public int SwapchainHeight => (int)m_swapchainHeight;
        public RenderTarget2D VrRenderTarget => null;

        static void WriteFixedString(byte* dest, string src, int maxLen) {
            int len = Math.Min(src.Length, maxLen - 1);
            for (int i = 0; i < len; i++) {
                dest[i] = (byte)src[i];
            }
            dest[len] = 0;
        }

        static ulong MakeVersion(ushort major, ushort minor, ushort patch) =>
            ((ulong)major << 48) | ((ulong)minor << 32) | ((ulong)patch << 16);

        static float ApplyDeadZone(float value, float deadZone) =>
            MathF.Sign(value) * MathF.Max(MathF.Abs(value) - deadZone, 0f) / MathF.Max(1f - deadZone, 0.001f);

        static Vector2 ApplyDeadZone(Vector2 value, float deadZone) {
            return new Vector2(
                ApplyDeadZone(value.X, deadZone),
                ApplyDeadZone(value.Y, deadZone)
            );
        }

        public void Initialize() {
#if WINDOWS
            // xrCreateInstance stalls for several seconds when an OpenXR runtime
            // (SteamVR / WMR) is registered but no HMD is attached: it spawns the
            // runtime broker synchronously on the calling thread. Kicking
            // DoInitialize off to a background Task overlaps that latency with the
            // rest of startup instead of freezing the loading-screen loop. OpenXR
            // instance / system / view-config queries are thread-safe and do not
            // touch the GL context, so off-thread init is valid; the session and
            // graphics binding are still created on the render thread in StartVr.
            // Idempotent: safe to call more than once.
            if (!m_initStarted) {
                m_initStarted = true;
                m_initTask = Task.Run(() => {
                    try {
                        DoInitialize();
                    }
                    catch (Exception e) {
                        Log.Error($"OpenXR initialization failed: {e}");
                        IsAvailable = false;
                    }
                });
            }
#else
            try {
                DoInitialize();
            }
            catch (Exception e) {
                Log.Error($"OpenXR initialization failed: {e}");
                IsAvailable = false;
            }
#endif
        }

        void DoInitialize() {
            m_xr = XR.GetApi();

#if ANDROID
            // Android: MUST initialize OpenXR loader with JVM info BEFORE any other OpenXR calls.
            // Without this, the loader cannot communicate with the runtime broker and no extensions are visible.
            Silk.NET.Core.PfnVoidFunction initLoaderPtr = default;
            Result r = m_xr.GetInstanceProcAddr(default, "xrInitializeLoaderKHR", ref initLoaderPtr);
            if (r == Result.Success) {
                LoaderInitInfoAndroidKHR loaderInfo = new() {
                    Type = StructureType.LoaderInitInfoAndroidKhr,
                    ApplicationVM = (void*)JniRuntime.CurrentRuntime.InvocationPointer,
#pragma warning disable CA1416
                    ApplicationContext = (void*)Window.Activity.Handle
#pragma warning restore CA1416
                };
                var initLoader = (delegate* unmanaged[Cdecl]<LoaderInitInfoBaseHeaderKHR*, Result>)initLoaderPtr.Handle;
                r = initLoader((LoaderInitInfoBaseHeaderKHR*)&loaderInfo);
                if (r != Result.Success) {
                    Log.Warning($"[VR] xrInitializeLoaderKHR failed: {r}");
                } else {
                    Log.Information("[VR] OpenXR loader initialized for Android");
                }
            } else {
                Log.Warning($"[VR] xrInitializeLoaderKHR not found: {r}");
            }
#endif

            // 1. Check extensions
            string glExtName =
#if WINDOWS
                "XR_KHR_opengl_enable";
#else
                "XR_KHR_opengl_es_enable";
#endif
            if (!m_xr.IsInstanceExtensionPresent(null, glExtName)) {
                Log.Information($"[VR] {glExtName} extension not available, OpenXR will not work");
                return;
            }
            // 2. Collect extensions to enable
            List<string> extensions = [glExtName];
            if (TryAddExtension(extensions, "XR_EXT_local_floor"))
                Log.Information("[VR] Optional extension enabled: XR_EXT_local_floor");
#if ANDROID
            TryAddExtension(extensions, "XR_KHR_android_create_instance");
            if (TryAddExtension(extensions, "XR_BD_controller_interaction"))
                Log.Information("[VR] Optional extension enabled: XR_BD_controller_interaction (PICO controllers)");
#endif

            // 3. Create Instance
            List<nint> extHandles = [];
            try {
                ApplicationInfo appInfo = new() {
                    ApplicationVersion = 1,
                    EngineVersion = 1,
                    ApiVersion = MakeVersion(1, 0, 0)
                };
                WriteFixedString(appInfo.ApplicationName, "Survivalcraft", 128);
                WriteFixedString(appInfo.EngineName, "Survivalcraft", 128);

                foreach (string ext in extensions) {
                    extHandles.Add(Marshal.StringToHGlobalAnsi(ext));
                }
                byte** extNames = stackalloc byte*[extHandles.Count];
                for (int i = 0; i < extHandles.Count; i++) {
                    extNames[i] = (byte*)extHandles[i];
                }

                InstanceCreateInfo createInfo = new() {
                    Type = StructureType.InstanceCreateInfo,
                    ApplicationInfo = appInfo,
                    EnabledExtensionCount = (uint)extHandles.Count,
                    EnabledExtensionNames = extNames
                };

#if ANDROID
                // Android: chain InstanceCreateInfoAndroidKHR with JavaVM and Activity
                InstanceCreateInfoAndroidKHR androidInfo = new() {
                    Type = StructureType.InstanceCreateInfoAndroidKhr,
                    ApplicationVM = (void*)JniRuntime.CurrentRuntime.InvocationPointer,
#pragma warning disable CA1416
                    ApplicationActivity = (void*)Window.Activity.Handle
#pragma warning restore CA1416
                };
                createInfo.Next = &androidInfo;
#endif

                Result result = m_xr.CreateInstance(ref createInfo, ref m_instance);
                if (result != Result.Success) {
                    Log.Information($"xrCreateInstance failed: {result}, OpenXR will not work");
                    return;
                }
            }
            finally {
                foreach (nint handle in extHandles) {
                    Marshal.FreeHGlobal(handle);
                }
            }

            // 3. Get System
            SystemGetInfo systemInfo = new() {
                Type = StructureType.SystemGetInfo,
                FormFactor = FormFactor.HeadMountedDisplay
            };
            {
                Result result = m_xr.GetSystem(m_instance, ref systemInfo, ref m_systemId);
                if (result != Result.Success) {
                    Log.Information($"[VR] xrGetSystem failed: {result}, OpenXR will not work");
                    return;
                }
            }

            // 4. Load OpenGL extension + check requirements
#if WINDOWS
            if (!m_xr.TryGetInstanceExtension<KhrOpenglEnable>(null, m_instance, out m_glExt)) {
                Log.Error("Failed to load XR_KHR_opengl_enable extension");
                return;
            }

            GraphicsRequirementsOpenGLKHR glReqs = new() {
                Type = StructureType.GraphicsRequirementsOpenglKhr
            };
            {
                Result result = m_glExt.GetOpenGlgraphicsRequirements(m_instance, m_systemId, ref glReqs);
                if (result != Result.Success) {
                    Log.Error($"GetOpenGLGraphicsRequirements failed: {result}");
                    return;
                }
            }
#else
            if (!m_xr.TryGetInstanceExtension<KhrOpenglEsEnable>(null, m_instance, out m_glExt)) {
                Log.Error("Failed to load XR_KHR_opengl_es_enable extension");
                return;
            }

            GraphicsRequirementsOpenGLESKHR glReqs = new() {
                Type = StructureType.GraphicsRequirementsOpenglESKhr
            };
            {
                Result result = m_glExt.GetOpenGlesgraphicsRequirements(m_instance, m_systemId, ref glReqs);
                if (result != Result.Success) {
                    Log.Error($"GetOpenGLESgraphicsRequirements failed: {result}");
                    return;
                }
            }
#endif

            // 5. Enumerate view configurations
            ViewConfigurationView[] configViews = new ViewConfigurationView[2];
            for (int i = 0; i < 2; i++) {
                configViews[i] = new() { Type = StructureType.ViewConfigurationView };
            }
            {
                uint viewCount = 2;
                m_xr.EnumerateViewConfigurationView(
                    m_instance, m_systemId,
                    ViewConfigurationType.PrimaryStereo,
                    viewCount, ref viewCount,
                    ref configViews[0]);
                m_swapchainWidth = configViews[0].RecommendedImageRectWidth;
                m_swapchainHeight = configViews[0].RecommendedImageRectHeight;
            }

            Log.Information($"OpenXR swapchain size: {m_swapchainWidth}x{m_swapchainHeight}");
            IsAvailable = true;
        }

        public void StartVr() {
            // Wait for any background initialization (Windows) to finish so the
            // IsAvailable check below sees the final result. When init already
            // completed (the common case — it is kicked off early in
            // Program.Initialize), Wait() returns immediately.
            if (m_initTask != null) {
                try { m_initTask.Wait(); }
                catch (Exception e) { Log.Warning($"[VR] background init wait error: {e.Message}"); }
            }

            if (!IsAvailable || IsStarted) return;

            try {
                if (m_instance.Handle == 0) {
                    DoInitialize();
                }
                DoStartVr();
                ControllerType = VrControllerType.Unknown;
                m_controllerDetectAttempts = 0;
                IsStarted = true;
                Log.Information("OpenXR VR started");
            }
            catch (Exception e) {
                DestroyGraphicsResources();
                DestroySessionResources();
                IsStarted = false;
                Log.Error($"OpenXR StartVr failed: {e.Message}");
            }
        }

        void DoStartVr() {
            GL gl = Graphics.GLWrapper.GL;

            // 1. Create session with platform graphics binding
            CreateSessionWithGraphicsBinding();

#if ANDROID
            // Android: mark this thread as the renderer main thread (non-critical)
            try {
                if (m_xr.TryGetInstanceExtension<KhrAndroidThreadSettings>(null, m_instance, out var threadExt)) {
                    threadExt.SetAndroidApplicationThread(m_session, AndroidThreadTypeKHR.RendererMainKhr, (uint)Environment.CurrentManagedThreadId);
                    threadExt.Dispose();
                }
            } catch (Exception ex) {
                Log.Warning($"[VR] SetAndroidApplicationThread failed (non-critical): {ex.Message}");
            }
#endif

            // 2. Create reference space — LOCAL_FLOOR preferred, fallback to Stage then Local
            CreatePlaySpace();

            // Reset recenter state for fresh session
            m_recenterYOffset = 0f;
            m_recenterPending = false;
            m_hmdYBeforeRecenterRaw = 0f;
            m_trackingEstablished = false;

            SelectSwapchainFormat();
            // 3. Create swapchains
            for (int eye = 0; eye < 2; eye++) {
                CreateSwapchain(eye);
            }

            // 4. Init view arrays
            m_views = new View[2];
            m_layerViews = new CompositionLayerProjectionView[2];
            for (int i = 0; i < 2; i++) {
                m_views[i] = new() { Type = StructureType.View };
                m_layerViews[i] = new() { Type = StructureType.CompositionLayerProjectionView };
            }

            // 5. Create actions and bindings
            CreateActions();

            m_sessionState = SessionState.Idle;
        }

        void CreateSessionWithGraphicsBinding() {
            int bindingSize = GetGraphicsBindingSize();
            byte* bindingMem = stackalloc byte[bindingSize];
            MemClear(bindingMem, bindingSize);
            *(StructureType*)bindingMem = GraphicsBindingType;
            PopulateGraphicsBinding(bindingMem);

            SessionCreateInfo sessionCreateInfo = new() {
                Type = StructureType.SessionCreateInfo,
                Next = bindingMem,
                SystemId = m_systemId
            };

            Result result = m_xr.CreateSession(m_instance, ref sessionCreateInfo, ref m_session);
            if (result != Result.Success) {
                throw new InvalidOperationException($"xrCreateSession failed: {result}");
            }
        }

        void SelectSwapchainFormat() {
            uint formatCount = 0;
            Result result = m_xr.EnumerateSwapchainFormats(m_session, 0, ref formatCount, (long*)null);
            if (result != Result.Success) {
                throw new InvalidOperationException($"xrEnumerateSwapchainFormats failed: {result}");
            }
            if (formatCount == 0) {
                throw new InvalidOperationException("OpenXR runtime did not report any swapchain formats.");
            }
            long[] formats = new long[formatCount];
            fixed (long* pFormats = formats) {
                result = m_xr.EnumerateSwapchainFormats(m_session, formatCount, ref formatCount, pFormats);
                if (result != Result.Success) {
                    throw new InvalidOperationException($"xrEnumerateSwapchainFormats failed: {result}");
                }
            }
            // Prefer sRGB: game outputs sRGB-encoded values with FRAMEBUFFER_SRGB disabled.
            // Declaring swapchain as sRGB tells the compositor data is already gamma-corrected.
            // Linear (Rgba8) causes Meta runtimes to apply a second gamma curve → washed out.
            InternalFormat[] preferredFormats = [InternalFormat.Srgb8Alpha8, InternalFormat.Rgba8];
            foreach (long preferredFormat in preferredFormats) {
                if (formats.Contains(preferredFormat)) {
                    m_swapchainFormat = (InternalFormat)preferredFormat;
                    return;
                }
            }
            m_swapchainFormat = (InternalFormat)formats[0];
        }

        bool TryAddExtension(List<string> extensions, string name) {
            if (m_xr.IsInstanceExtensionPresent(null, name)) {
                extensions.Add(name);
                return true;
            }
            return false;
        }

        static void MemClear(void* ptr, int size) {
            byte* p = (byte*)ptr;
            for (int i = 0; i < size; i++) {
                p[i] = 0;
            }
        }

        void CreateSwapchain(int eye) {
            GL gl = Graphics.GLWrapper.GL;

            SwapchainCreateInfo swapchainInfo = new() {
                Type = StructureType.SwapchainCreateInfo,
                UsageFlags = SwapchainUsageFlags.ColorAttachmentBit | SwapchainUsageFlags.SampledBit,
                Format = (long)m_swapchainFormat,
                SampleCount = 1,
                Width = m_swapchainWidth,
                Height = m_swapchainHeight,
                FaceCount = 1,
                ArraySize = 1,
                MipCount = 1
            };

            Result result = m_xr.CreateSwapchain(m_session, ref swapchainInfo, ref m_swapchains[eye]);
            if (result != Result.Success) {
                throw new InvalidOperationException($"xrCreateSwapchain failed for eye {eye}: {result}");
            }

            // Enumerate swapchain images
            uint imageCount = 0;
            result = m_xr.EnumerateSwapchainImages(m_swapchains[eye], 0, ref imageCount, (SwapchainImageBaseHeader*)null);
            if (result != Result.Success) {
                throw new InvalidOperationException($"xrEnumerateSwapchainImages failed for eye {eye}: {result}");
            }
            if (imageCount == 0) {
                throw new InvalidOperationException($"OpenXR swapchain for eye {eye} has no images.");
            }

#if WINDOWS
            SwapchainImageOpenGLKHR[] images = new SwapchainImageOpenGLKHR[imageCount];
            for (int i = 0; i < imageCount; i++) {
                images[i] = new() { Type = StructureType.SwapchainImageOpenglKhr };
            }
            fixed (SwapchainImageOpenGLKHR* pImages = images) {
                result = m_xr.EnumerateSwapchainImages(m_swapchains[eye], imageCount, ref imageCount, ref *(SwapchainImageBaseHeader*)pImages);
                if (result != Result.Success) {
                    throw new InvalidOperationException($"xrEnumerateSwapchainImages failed for eye {eye}: {result}");
                }
            }
#else
            SwapchainImageOpenGLESKHR[] images = new SwapchainImageOpenGLESKHR[imageCount];
            for (int i = 0; i < imageCount; i++) {
                images[i] = new() { Type = StructureType.SwapchainImageOpenglESKhr };
            }
            fixed (SwapchainImageOpenGLESKHR* pImages = images) {
                result = m_xr.EnumerateSwapchainImages(m_swapchains[eye], imageCount, ref imageCount, ref *(SwapchainImageBaseHeader*)pImages);
                if (result != Result.Success) {
                    throw new InvalidOperationException($"xrEnumerateSwapchainImages failed for eye {eye}: {result}");
                }
            }
#endif

            m_swapchainImages[eye] = new uint[imageCount];
            for (int i = 0; i < imageCount; i++) {
                m_swapchainImages[eye][i] = images[i].Image;
            }

            // Create FBO for this eye
            CreateFBO(eye);
        }

        void CreateFBO(int eye) {
            GL gl = Graphics.GLWrapper.GL;

            gl.GenFramebuffers(1, out uint fbo);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            gl.GenRenderbuffers(1, out uint depthRb);
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRb);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, m_swapchainWidth, m_swapchainHeight);
            gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthRb);

            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, m_swapchainImages[eye][0], 0);

            GLEnum status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            if (status != GLEnum.FramebufferComplete) {
                throw new InvalidOperationException($"OpenXR framebuffer incomplete for eye {eye}: {status}");
            }

            m_swapchainFbos[eye] = fbo;
            m_swapchainDepthRbs[eye] = depthRb;
        }

        void CreateActions() {
            // Create action set
            ActionSetCreateInfo actionSetInfo = new() {
                Type = StructureType.ActionSetCreateInfo,
                Priority = 0
            };
            WriteFixedString(actionSetInfo.ActionSetName, "gameplay", 64);
            WriteFixedString(actionSetInfo.LocalizedActionSetName, "Gameplay", 128);

            Result result = m_xr.CreateActionSet(m_instance, ref actionSetInfo, ref m_actionSet);
            if (result != Result.Success) {
                Log.Error($"xrCreateActionSet failed: {result}");
                return;
            }

            // Per-hand actions: trigger, grip, menu, stick, pose
            string[] handPaths = ["left", "right"];
            for (int hand = 0; hand < 2; hand++) {
                // Trigger (Float)
                CreateActionFloat($"trigger_{handPaths[hand]}", $"Trigger {handPaths[hand]}", out m_triggerActions[hand]);
                // Grip (Float -> bool)
                CreateActionFloat($"grip_{handPaths[hand]}", $"Grip {handPaths[hand]}", out m_gripActions[hand]);
                CreateActionBool($"grip_click_{handPaths[hand]}", $"Grip Click {handPaths[hand]}", out m_gripClickActions[hand]);
                // Menu (Boolean)
                CreateActionBool($"menu_{handPaths[hand]}", $"Menu {handPaths[hand]}", out m_menuActions[hand]);
                // Stick (two separate float actions since GetActionStateVector2f doesn't exist)
                CreateActionFloat($"stick_x_{handPaths[hand]}", $"Stick X {handPaths[hand]}", out m_stickXActions[hand]);
                CreateActionFloat($"stick_y_{handPaths[hand]}", $"Stick Y {handPaths[hand]}", out m_stickYActions[hand]);
                // Pose
                CreateActionPose($"pose_{handPaths[hand]}", $"Pose {handPaths[hand]}", out m_poseActions[hand]);
                // Stick click (Boolean)
                CreateActionBool($"stick_click_{handPaths[hand]}", $"Stick Click {handPaths[hand]}", out m_stickClickActions[hand]);
                // Primary button (X on left, A on right)
                CreateActionBool($"primary_{handPaths[hand]}", $"Primary {handPaths[hand]}", out m_primaryActions[hand]);
                // Secondary button (Y on left, B on right)
                CreateActionBool($"secondary_{handPaths[hand]}", $"Secondary {handPaths[hand]}", out m_secondaryActions[hand]);
                // Thumbrest touch
                CreateActionBool($"thumbrest_{handPaths[hand]}", $"Thumbrest {handPaths[hand]}", out m_thumbrestActions[hand]);
                // Trackpad (separate from thumbstick, for controllers like Index)
                CreateActionFloat($"trackpad_x_{handPaths[hand]}", $"Trackpad X {handPaths[hand]}", out m_trackpadXActions[hand]);
                CreateActionFloat($"trackpad_y_{handPaths[hand]}", $"Trackpad Y {handPaths[hand]}", out m_trackpadYActions[hand]);
            }

            // Suggest bindings for /interaction_profiles/khr_simple_controller
            SuggestBindings();

            // Attach action set
            SessionActionSetsAttachInfo attachInfo = new() {
                Type = StructureType.SessionActionSetsAttachInfo,
                CountActionSets = 1
            };
            fixed (ActionSet* pSet = &m_actionSet) {
                attachInfo.ActionSets = pSet;
                m_xr.AttachSessionActionSets(m_session, ref attachInfo);
            }

            // Controller type detection is deferred to BeginFrame (after SyncActions),
            // because xrGetCurrentInteractionProfile returns XR_NULL_PATH if called
            // before the first xrSyncActions.

            // Create action spaces for controller poses
            for (int hand = 0; hand < 2; hand++) {
                ActionSpaceCreateInfo actionSpaceInfo = new() {
                    Type = StructureType.ActionSpaceCreateInfo,
                    Action = m_poseActions[hand],
                    SubactionPath = 0,
                    PoseInActionSpace = new() {
                        Orientation = new() { X = 0, Y = 0, Z = 0, W = 1 },
                        Position = new() { X = 0, Y = 0, Z = 0 }
                    }
                };
                m_xr.CreateActionSpace(m_session, ref actionSpaceInfo, ref m_controllerSpaces[hand]);
            }
        }

        void CreateActionFloat(string name, string localizedName, out XrAction action) {
            ActionCreateInfo info = new() {
                Type = StructureType.ActionCreateInfo,
                ActionType = ActionType.FloatInput,
                CountSubactionPaths = 0
            };
            WriteFixedString(info.ActionName, name, 64);
            WriteFixedString(info.LocalizedActionName, localizedName, 128);
            action = default;
            m_xr.CreateAction(m_actionSet, ref info, ref action);
        }

        void CreateActionBool(string name, string localizedName, out XrAction action) {
            ActionCreateInfo info = new() {
                Type = StructureType.ActionCreateInfo,
                ActionType = ActionType.BooleanInput,
                CountSubactionPaths = 0
            };
            WriteFixedString(info.ActionName, name, 64);
            WriteFixedString(info.LocalizedActionName, localizedName, 128);
            action = default;
            m_xr.CreateAction(m_actionSet, ref info, ref action);
        }

        void CreateActionPose(string name, string localizedName, out XrAction action) {
            ActionCreateInfo info = new() {
                Type = StructureType.ActionCreateInfo,
                ActionType = ActionType.PoseInput,
                CountSubactionPaths = 0
            };
            WriteFixedString(info.ActionName, name, 64);
            WriteFixedString(info.LocalizedActionName, localizedName, 128);
            action = default;
            m_xr.CreateAction(m_actionSet, ref info, ref action);
        }

        void SuggestBindings() {
            XrAction[] allActions = [
                m_triggerActions[0], m_gripActions[0], m_menuActions[0], m_stickXActions[0], m_stickYActions[0], m_poseActions[0], m_stickClickActions[0],
                m_primaryActions[0], m_secondaryActions[0], m_thumbrestActions[0],
                m_triggerActions[1], m_gripActions[1], m_menuActions[1], m_stickXActions[1], m_stickYActions[1], m_poseActions[1], m_stickClickActions[1],
                m_primaryActions[1], m_secondaryActions[1], m_thumbrestActions[1]
            ];
            // Meta Quest Touch
            SuggestProfileBindings("/interaction_profiles/oculus/touch_controller", allActions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/value",
                "/user/hand/left/input/menu/click",
                "/user/hand/left/input/thumbstick/x",
                "/user/hand/left/input/thumbstick/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/thumbstick/click",
                "/user/hand/left/input/x/click",
                "/user/hand/left/input/y/click",
                "/user/hand/left/input/thumbrest/touch",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/value",
                "/user/hand/right/input/system/click",
                "/user/hand/right/input/thumbstick/x",
                "/user/hand/right/input/thumbstick/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/thumbstick/click",
                "/user/hand/right/input/a/click",
                "/user/hand/right/input/b/click",
                "/user/hand/right/input/thumbrest/touch"
            ]);

            // Windows Mixed Reality / Microsoft Motion Controller.
            // These controllers report squeeze as a click and expose both
            // thumbstick and trackpad paths.
            XrAction[] microsoftActions = [
                m_triggerActions[0], m_gripClickActions[0], m_menuActions[0], m_stickXActions[0], m_stickYActions[0], m_poseActions[0], m_stickClickActions[0],
                m_trackpadXActions[0], m_trackpadYActions[0],
                m_triggerActions[1], m_gripClickActions[1], m_menuActions[1], m_stickXActions[1], m_stickYActions[1], m_poseActions[1], m_stickClickActions[1],
                m_trackpadXActions[1], m_trackpadYActions[1]
            ];
            SuggestProfileBindings("/interaction_profiles/microsoft/motion_controller", microsoftActions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/click",
                "/user/hand/left/input/menu/click",
                "/user/hand/left/input/thumbstick/x",
                "/user/hand/left/input/thumbstick/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/trackpad/click",
                "/user/hand/left/input/trackpad/touch",
                "/user/hand/left/input/trackpad/x",
                "/user/hand/left/input/trackpad/y",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/click",
                "/user/hand/right/input/menu/click",
                "/user/hand/right/input/thumbstick/x",
                "/user/hand/right/input/thumbstick/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/trackpad/click",
                "/user/hand/right/input/trackpad/touch",
                "/user/hand/right/input/trackpad/x",
                "/user/hand/right/input/trackpad/y"
            ]);

            // HTC Vive (trackpad is both Stick and Trackpad)
            XrAction[] viveActions = [
                m_triggerActions[0], m_gripClickActions[0], m_menuActions[0], m_stickXActions[0], m_stickYActions[0], m_poseActions[0], m_stickClickActions[0],
                m_trackpadXActions[0], m_trackpadYActions[0],
                m_triggerActions[1], m_gripClickActions[1], m_menuActions[1], m_stickXActions[1], m_stickYActions[1], m_poseActions[1], m_stickClickActions[1],
                m_trackpadXActions[1], m_trackpadYActions[1]
            ];
            SuggestProfileBindings("/interaction_profiles/htc/vive_controller", viveActions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/click",
                "/user/hand/left/input/menu/click",
                "/user/hand/left/input/trackpad/x",
                "/user/hand/left/input/trackpad/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/trackpad/click",
                "/user/hand/left/input/trackpad/x",
                "/user/hand/left/input/trackpad/y",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/click",
                "/user/hand/right/input/menu/click",
                "/user/hand/right/input/trackpad/x",
                "/user/hand/right/input/trackpad/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/trackpad/click",
                "/user/hand/right/input/trackpad/x",
                "/user/hand/right/input/trackpad/y"
            ]);

            // Valve Index (trackpad + A/B buttons, no X/Y/thumbrest)
            XrAction[] indexActions = [
                m_triggerActions[0], m_gripActions[0], m_menuActions[0], m_stickXActions[0], m_stickYActions[0], m_poseActions[0], m_stickClickActions[0],
                m_primaryActions[0], m_secondaryActions[0],
                m_trackpadXActions[0], m_trackpadYActions[0],
                m_triggerActions[1], m_gripActions[1], m_menuActions[1], m_stickXActions[1], m_stickYActions[1], m_poseActions[1], m_stickClickActions[1],
                m_primaryActions[1], m_secondaryActions[1],
                m_trackpadXActions[1], m_trackpadYActions[1]
            ];
            SuggestProfileBindings("/interaction_profiles/valve/index_controller", indexActions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/value",
                "/user/hand/left/input/system/click",
                "/user/hand/left/input/thumbstick/x",
                "/user/hand/left/input/thumbstick/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/trackpad/force",
                "/user/hand/left/input/a/click",
                "/user/hand/left/input/b/click",
                "/user/hand/left/input/trackpad/x",
                "/user/hand/left/input/trackpad/y",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/value",
                "/user/hand/right/input/system/click",
                "/user/hand/right/input/thumbstick/x",
                "/user/hand/right/input/thumbstick/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/trackpad/force",
                "/user/hand/right/input/a/click",
                "/user/hand/right/input/b/click",
                "/user/hand/right/input/trackpad/x",
                "/user/hand/right/input/trackpad/y"
            ]);

            // Meta Quest Touch Pro (superset of Quest Touch, same button paths)
            SuggestProfileBindings("/interaction_profiles/meta/touch_controller_pro", allActions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/value",
                "/user/hand/left/input/menu/click",
                "/user/hand/left/input/thumbstick/x",
                "/user/hand/left/input/thumbstick/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/thumbstick/click",
                "/user/hand/left/input/x/click",
                "/user/hand/left/input/y/click",
                "/user/hand/left/input/thumbrest/touch",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/value",
                "/user/hand/right/input/system/click",
                "/user/hand/right/input/thumbstick/x",
                "/user/hand/right/input/thumbstick/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/thumbstick/click",
                "/user/hand/right/input/a/click",
                "/user/hand/right/input/b/click",
                "/user/hand/right/input/thumbrest/touch"
            ]);

            // Meta Quest Touch Plus (Quest 3, same button paths)
            SuggestProfileBindings("/interaction_profiles/meta/touch_controller_plus", allActions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/value",
                "/user/hand/left/input/menu/click",
                "/user/hand/left/input/thumbstick/x",
                "/user/hand/left/input/thumbstick/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/thumbstick/click",
                "/user/hand/left/input/x/click",
                "/user/hand/left/input/y/click",
                "/user/hand/left/input/thumbrest/touch",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/value",
                "/user/hand/right/input/system/click",
                "/user/hand/right/input/thumbstick/x",
                "/user/hand/right/input/thumbstick/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/thumbstick/click",
                "/user/hand/right/input/a/click",
                "/user/hand/right/input/b/click",
                "/user/hand/right/input/thumbrest/touch"
            ]);

            // PICO Neo3. Do not use thumbrest paths here; PICO exposes
            // thumbstick touch/click but no thumbrest input.
            XrAction[] picoNeo3Actions = [
                m_triggerActions[0], m_gripActions[0], m_gripClickActions[0], m_menuActions[0], m_stickXActions[0], m_stickYActions[0], m_poseActions[0], m_stickClickActions[0],
                m_primaryActions[0], m_secondaryActions[0],
                m_triggerActions[1], m_gripActions[1], m_gripClickActions[1], m_menuActions[1], m_stickXActions[1], m_stickYActions[1], m_poseActions[1], m_stickClickActions[1],
                m_primaryActions[1], m_secondaryActions[1]
            ];
            SuggestProfileBindings("/interaction_profiles/bytedance/pico_neo3_controller", picoNeo3Actions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/value",
                "/user/hand/left/input/squeeze/click",
                "/user/hand/left/input/menu/click",
                "/user/hand/left/input/thumbstick/x",
                "/user/hand/left/input/thumbstick/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/thumbstick/click",
                "/user/hand/left/input/x/click",
                "/user/hand/left/input/y/click",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/value",
                "/user/hand/right/input/squeeze/click",
                "/user/hand/right/input/menu/click",
                "/user/hand/right/input/thumbstick/x",
                "/user/hand/right/input/thumbstick/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/thumbstick/click",
                "/user/hand/right/input/a/click",
                "/user/hand/right/input/b/click"
            ]);

            // PICO 4. Menu is only exposed on the left hand in this profile;
            // right-hand system/click is not reliable for application input.
            XrAction[] pico4Actions = [
                m_triggerActions[0], m_gripActions[0], m_gripClickActions[0], m_menuActions[0], m_stickXActions[0], m_stickYActions[0], m_poseActions[0], m_stickClickActions[0],
                m_primaryActions[0], m_secondaryActions[0],
                m_triggerActions[1], m_gripActions[1], m_gripClickActions[1], m_stickXActions[1], m_stickYActions[1], m_poseActions[1], m_stickClickActions[1],
                m_primaryActions[1], m_secondaryActions[1]
            ];
            SuggestProfileBindings("/interaction_profiles/bytedance/pico4_controller", pico4Actions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/value",
                "/user/hand/left/input/squeeze/click",
                "/user/hand/left/input/menu/click",
                "/user/hand/left/input/thumbstick/x",
                "/user/hand/left/input/thumbstick/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/thumbstick/click",
                "/user/hand/left/input/x/click",
                "/user/hand/left/input/y/click",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/value",
                "/user/hand/right/input/squeeze/click",
                "/user/hand/right/input/thumbstick/x",
                "/user/hand/right/input/thumbstick/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/thumbstick/click",
                "/user/hand/right/input/a/click",
                "/user/hand/right/input/b/click"
            ]);

            // HTC Vive Focus 3
            SuggestProfileBindings("/interaction_profiles/htc/vive_focus3_controller", allActions, [
                "/user/hand/left/input/trigger/value",
                "/user/hand/left/input/squeeze/value",
                "/user/hand/left/input/menu/click",
                "/user/hand/left/input/thumbstick/x",
                "/user/hand/left/input/thumbstick/y",
                "/user/hand/left/input/aim/pose",
                "/user/hand/left/input/thumbstick/click",
                "/user/hand/left/input/x/click",
                "/user/hand/left/input/y/click",
                "/user/hand/left/input/thumbrest/touch",
                "/user/hand/right/input/trigger/value",
                "/user/hand/right/input/squeeze/value",
                "/user/hand/right/input/system/click",
                "/user/hand/right/input/thumbstick/x",
                "/user/hand/right/input/thumbstick/y",
                "/user/hand/right/input/aim/pose",
                "/user/hand/right/input/thumbstick/click",
                "/user/hand/right/input/a/click",
                "/user/hand/right/input/b/click",
                "/user/hand/right/input/thumbrest/touch"
            ]);
        }

        void SuggestProfileBindings(string profilePath, XrAction[] actions, string[] paths) {
            int count = actions.Length;
            ActionSuggestedBinding[] bindings = new ActionSuggestedBinding[count];
            ulong[] pathHandles = new ulong[count];
            bool valid = true;
            for (int i = 0; i < count; i++) {
                Result r = m_xr.StringToPath(m_instance, paths[i], ref pathHandles[i]);
                if (r != Result.Success) {
                    Log.Warning($"xrStringToPath failed for '{paths[i]}': {r}");
                    valid = false;
                    break;
                }
                bindings[i] = new ActionSuggestedBinding {
                    Action = actions[i],
                    Binding = pathHandles[i]
                };
            }
            if (!valid) return;

            ulong profile = 0;
            m_xr.StringToPath(m_instance, profilePath, ref profile);

            InteractionProfileSuggestedBinding suggestedBindings = new() {
                Type = StructureType.InteractionProfileSuggestedBinding,
                InteractionProfile = profile,
                CountSuggestedBindings = (uint)count
            };
            fixed (ActionSuggestedBinding* pBindings = bindings) {
                suggestedBindings.SuggestedBindings = pBindings;
                Result r = m_xr.SuggestInteractionProfileBinding(m_instance, in suggestedBindings);
                switch (r) {
                    case Result.Success: Log.Information($"SuggestedBindings succeeded for {profilePath}"); break;
                    case Result.ErrorPathUnsupported: Log.Verbose($"OpenXR interaction profile not supported by runtime: {profilePath}"); break;
                    default: Log.Warning($"SuggestBindings failed for {profilePath}: {r}"); break;
                }
            }
        }

        void DetectControllerType() {
            try {
                ulong leftHandPath = 0;
                m_xr.StringToPath(m_instance, "/user/hand/left", ref leftHandPath);
                InteractionProfileState profileState = new() {
                    Type = StructureType.InteractionProfileState
                };
                m_xr.GetCurrentInteractionProfile(m_session, leftHandPath, ref profileState);
                if (profileState.InteractionProfile == 0) {
                    ControllerType = VrControllerType.Unknown;
                    return;
                }
                // Read profile path string
                uint bufLen = 256;
                byte[] buf = new byte[bufLen];
                m_xr.PathToString(m_instance, profileState.InteractionProfile, &bufLen, buf);
                // Find null terminator
                int len = 0;
                while (len < buf.Length && buf[len] != 0) len++;
                string path = System.Text.Encoding.ASCII.GetString(buf, 0, len);
                ControllerType = path switch {
                    "/interaction_profiles/oculus/touch_controller" => VrControllerType.MetaQuestTouch,
                    "/interaction_profiles/meta/touch_controller_pro" => VrControllerType.MetaQuestTouch,
                    "/interaction_profiles/meta/touch_controller_plus" => VrControllerType.MetaQuestTouch,
                    "/interaction_profiles/htc/vive_controller" => VrControllerType.HtcVive,
                    "/interaction_profiles/valve/index_controller" => VrControllerType.ValveIndex,
                    "/interaction_profiles/microsoft/motion_controller" => VrControllerType.MicrosoftMRMotion,
                    "/interaction_profiles/bytedance/pico_neo3_controller" => VrControllerType.PICO,
                    "/interaction_profiles/bytedance/pico4_controller" => VrControllerType.PICO,
                    "/interaction_profiles/htc/vive_focus3_controller" => VrControllerType.MetaQuestTouch,
                    _ => VrControllerType.Unknown
                };
                Log.Information($"OpenXR controller type: {ControllerType} ({path})");
            }
            catch (Exception e) {
                Log.Warning($"Failed to detect controller type: {e.Message}");
                ControllerType = VrControllerType.Unknown;
            }
        }

        // --- Frame loop ---

        public bool BeginFrame() {
            if (!IsStarted) return false;

            PollEvents();

            if (m_sessionState == SessionState.LossPending || m_sessionState == SessionState.Exiting) {
                IsStarted = false;
                return false;
            }

            if (m_sessionState != SessionState.Focused && m_sessionState != SessionState.Synchronized
                && m_sessionState != SessionState.Visible) {
                // Idle/Ready: just poll events, don't call frame functions
                return false;
            }

            FrameWaitInfo waitInfo = new() { Type = StructureType.FrameWaitInfo };
            m_frameState = new() { Type = StructureType.FrameState };
            Result result = m_xr.WaitFrame(m_session, ref waitInfo, ref m_frameState);
            if (result != Result.Success) return false;

            FrameBeginInfo beginInfo = new() { Type = StructureType.FrameBeginInfo };
            m_xr.BeginFrame(m_session, ref beginInfo);

            if (m_frameState.ShouldRender == 0) {
                return true;
            }

            // Locate views
            ViewLocateInfo viewLocateInfo = new() {
                Type = StructureType.ViewLocateInfo,
                ViewConfigurationType = ViewConfigurationType.PrimaryStereo,
                DisplayTime = m_frameState.PredictedDisplayTime,
                Space = m_playSpace
            };

            ViewState viewState = new() { Type = StructureType.ViewState };
            uint viewCount = 2;
            m_xr.LocateView(m_session, ref viewLocateInfo, ref viewState, viewCount, ref viewCount, ref m_views[0]);

            // Update HMD state
            UpdateHmdState();

            // Sync actions
            SyncActions();

            // Detect controller type after first SyncActions (profile may not be available earlier)
            if (ControllerType == VrControllerType.Unknown && m_controllerDetectAttempts < 30) {
                m_controllerDetectAttempts++;
                DetectControllerType();
                if (ControllerType == VrControllerType.Unknown && m_controllerDetectAttempts == 30) {
                    Log.Warning("Could not detect VR controller type after 10 frames, giving up");
                }
            }

            // Update controller states
            UpdateControllers();

            return true;
        }

        public EyeFrame GetEyeFrame(VrEye eye) {
            int eyeIndex = (int)eye;

            SwapchainImageAcquireInfo acquireInfo = new() { Type = StructureType.SwapchainImageAcquireInfo };
            m_xr.AcquireSwapchainImage(m_swapchains[eyeIndex], ref acquireInfo, ref m_acquiredImageIndex);

            SwapchainImageWaitInfo waitInfo = new() {
                Type = StructureType.SwapchainImageWaitInfo,
                Timeout = 1000000000
            };
            m_xr.WaitSwapchainImage(m_swapchains[eyeIndex], ref waitInfo);

            // Re-attach the acquired texture to the FBO
            GL gl = Graphics.GLWrapper.GL;
            uint texture = m_swapchainImages[eyeIndex][m_acquiredImageIndex];
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, m_swapchainFbos[eyeIndex]);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);

            // Build view and projection matrices
            Matrix viewMatrix = CreateViewMatrix(m_views[eyeIndex].Pose);
            Matrix projMatrix = CreateProjectionMatrix(m_views[eyeIndex].Fov, 0.1f, 2048f);

            // Apply recenter offset to keep eye views in the same compensated
            // coordinate space as HmdMatrix.
            if (m_recenterYOffset != 0f) {
                viewMatrix *= Matrix.CreateTranslation(0f, -m_recenterYOffset, 0f);
            }

            Vector3 camPos = new(
                m_views[eyeIndex].Pose.Position.X,
                m_views[eyeIndex].Pose.Position.Y + m_recenterYOffset,
                m_views[eyeIndex].Pose.Position.Z
            );

            return new EyeFrame {
                Fbo = (int)m_swapchainFbos[eyeIndex],
                ViewMatrix = viewMatrix,
                ProjectionMatrix = projMatrix,
                CameraPosition = camPos
            };
        }

        public void ReleaseEye(VrEye eye) {
            SwapchainImageReleaseInfo releaseInfo = new() { Type = StructureType.SwapchainImageReleaseInfo };
            m_xr.ReleaseSwapchainImage(m_swapchains[(int)eye], ref releaseInfo);
        }

        public void EndFrame() {
            if (!IsStarted) return;

            if (m_frameState.ShouldRender != 0) {
                for (int i = 0; i < 2; i++) {
                    // Apply recenter offset to layer pose so compositor reprojection
                    // stays consistent with the compensated view matrices.
                    Posef pose = m_views[i].Pose;
                    if (m_recenterYOffset != 0f) {
                        pose.Position = new Vector3f {
                            X = pose.Position.X,
                            Y = pose.Position.Y + m_recenterYOffset,
                            Z = pose.Position.Z
                        };
                    }
                    m_layerViews[i].Pose = pose;
                    m_layerViews[i].Fov = m_views[i].Fov;
                    m_layerViews[i].SubImage = new() {
                        Swapchain = m_swapchains[i],
                        ImageRect = new() {
                            Offset = new() { X = 0, Y = 0 },
                            Extent = new() { Width = (int)m_swapchainWidth, Height = (int)m_swapchainHeight }
                        },
                        ImageArrayIndex = 0
                    };
                }

                CompositionLayerProjection layer = new() {
                    Type = StructureType.CompositionLayerProjection,
                    Space = m_playSpace,
                    ViewCount = 2
                };

                // fixed block must cover EndFrame call to prevent GC moving managed arrays
                fixed (CompositionLayerProjectionView* pViews = m_layerViews) {
                    layer.Views = pViews;
                    CompositionLayerBaseHeader* pLayer = (CompositionLayerBaseHeader*)&layer;
                    FrameEndInfo endInfo = new() {
                        Type = StructureType.FrameEndInfo,
                        DisplayTime = m_frameState.PredictedDisplayTime,
                        EnvironmentBlendMode = EnvironmentBlendMode.Opaque,
                        LayerCount = 1,
                        Layers = &pLayer
                    };
                    m_xr.EndFrame(m_session, ref endInfo);
                }
            }
            else {
                EndFrameEmpty();
            }
        }

        public void EndFrameEmpty() {
            if (!IsStarted) return;
            FrameEndInfo endInfo = new() {
                Type = StructureType.FrameEndInfo,
                DisplayTime = m_frameState.PredictedDisplayTime,
                EnvironmentBlendMode = EnvironmentBlendMode.Opaque,
                LayerCount = 0,
                Layers = null
            };
            m_xr.EndFrame(m_session, ref endInfo);
        }

        // --- HMD state ---

        void UpdateHmdState() {
            // Save last state
            m_hmdLastMatrix = m_hmdMatrix;
            m_hmdLastMatrixInverted = m_hmdMatrixInverted;
            m_hmdLastMatrixYpr = m_hmdMatrixYpr;

            if (m_views == null) return;

            // Mid-point between two eye positions for HMD position
            Vector3f leftPos = m_views[0].Pose.Position;
            Vector3f rightPos = m_views[1].Pose.Position;
            float midX = (leftPos.X + rightPos.X) * 0.5f;
            float midY = (leftPos.Y + rightPos.Y) * 0.5f;
            float midZ = (leftPos.Z + rightPos.Z) * 0.5f;

            // Use left eye orientation for HMD orientation
            Quaternionf orientation = m_views[0].Pose.Orientation;
            Quaternion q = new(orientation.X, orientation.Y, orientation.Z, orientation.W);
            Matrix rotMatrix = q.ToMatrix();
            Matrix transMatrix = Matrix.CreateTranslation(midX, midY, midZ);
            m_hmdMatrix = rotMatrix * transMatrix;
            m_hmdMatrixInverted = Matrix.Invert(m_hmdMatrix);

            // Extract YPR from quaternion
            Vector3 ypr = q.ToYawPitchRoll();
            m_hmdMatrixYpr = new Vector3(ypr.X, ypr.Y, ypr.Z);

            // Head move from difference
            m_headMove = new Vector2(
                m_hmdMatrix.M41 - m_hmdLastMatrix.M41,
                m_hmdMatrix.M43 - m_hmdLastMatrix.M43
            );

            // Mark tracking as established once we have a valid HMD pose
            if (!m_trackingEstablished) {
                m_trackingEstablished = true;
            }

            // Compensate for recenter: some runtimes shift the reference space origin
            // on recenter. Track how much Y moved and accumulate the offset.
            // Only apply after tracking is established to avoid false compensation
            // from initial ReferenceSpaceChangePending events during startup.
            if (m_recenterPending) {
                float currentRawY = m_hmdMatrix.Translation.Y;
                float delta = m_hmdYBeforeRecenterRaw - currentRawY;
                if (Math.Abs(delta) > 0.01f) { // ignore sub-cm noise
                    m_recenterYOffset += delta;
                    Log.Information($"[VR] Recenter Y offset: {delta:+0.00;-0.00;0.00} (total: {m_recenterYOffset:+0.00;-0.00;0.00})");
                }
                m_recenterPending = false;
            }

            // Apply accumulated recenter offset
            if (m_recenterYOffset != 0f) {
                m_hmdMatrix.Translation += new Vector3(0f, m_recenterYOffset, 0f);
                m_hmdMatrixInverted = Matrix.Invert(m_hmdMatrix);
            }
        }

        // --- Controller input ---

        void SyncActions() {
            ActiveActionSet activeActionSet = new() {
                ActionSet = m_actionSet,
                SubactionPath = 0
            };
            ActionsSyncInfo syncInfo = new() {
                Type = StructureType.ActionsSyncInfo,
                CountActiveActionSets = 1,
                ActiveActionSets = &activeActionSet
            };
            m_xr.SyncAction(m_session, in syncInfo);
        }

        void UpdateControllers() {
            for (int hand = 0; hand < 2; hand++) {
                // Save last state
                m_lastControllers[hand] = m_controllers[hand];

                // Check pose action space location to determine connectivity
                SpaceLocation location = new() { Type = StructureType.SpaceLocation };
                Result locateResult = m_xr.LocateSpace(m_controllerSpaces[hand], m_playSpace, m_frameState.PredictedDisplayTime, ref location);

                bool isConnected = locateResult == Result.Success
                    && (location.LocationFlags & SpaceLocationFlags.PositionValidBit) != 0
                    && (location.LocationFlags & SpaceLocationFlags.OrientationValidBit) != 0;

                m_controllers[hand].IsConnected = isConnected;

                if (!isConnected) continue;

                // Controller matrix from pose (apply recenter offset to match HMD)
                Matrix ctrlMatrix = PoseToMatrix(location.Pose);
                if (m_recenterYOffset != 0f) {
                    ctrlMatrix.Translation += new Vector3(0f, m_recenterYOffset, 0f);
                }
                m_controllers[hand].Matrix = ctrlMatrix;

                // Trigger
                ActionStateFloat triggerState = GetFloatState(m_triggerActions[hand]);
                m_controllers[hand].Trigger = triggerState.CurrentState;

                // Grip (float -> bool >= 0.5)
                ActionStateFloat gripState = GetFloatState(m_gripActions[hand]);
                ActionStateBoolean gripClickState = GetBoolState(m_gripClickActions[hand]);
                m_controllers[hand].Grip = gripState.CurrentState >= 0.5f || gripClickState.CurrentState != 0;

                // Menu
                ActionStateBoolean menuState = GetBoolState(m_menuActions[hand]);
                m_controllers[hand].Menu = menuState.CurrentState != 0;

                // Stick click
                ActionStateBoolean stickClickState = GetBoolState(m_stickClickActions[hand]);
                m_controllers[hand].StickClick = stickClickState.CurrentState != 0;

                // Stick (separate X/Y float actions)
                ActionStateFloat stickXState = GetFloatState(m_stickXActions[hand]);
                ActionStateFloat stickYState = GetFloatState(m_stickYActions[hand]);
                m_controllers[hand].Stick = new Vector2(stickXState.CurrentState, stickYState.CurrentState);

                // Primary (X on left, A on right)
                ActionStateBoolean primaryState = GetBoolState(m_primaryActions[hand]);
                m_controllers[hand].Primary = primaryState.CurrentState != 0;

                // Secondary (Y on left, B on right)
                ActionStateBoolean secondaryState = GetBoolState(m_secondaryActions[hand]);
                m_controllers[hand].Secondary = secondaryState.CurrentState != 0;

                // Thumbrest touch
                ActionStateBoolean thumbrestState = GetBoolState(m_thumbrestActions[hand]);
                m_controllers[hand].Thumbrest = thumbrestState.CurrentState != 0;

                // Trackpad (separate from thumbstick, for controllers like Index)
                ActionStateFloat trackpadXState = GetFloatState(m_trackpadXActions[hand]);
                ActionStateFloat trackpadYState = GetFloatState(m_trackpadYActions[hand]);
                m_controllers[hand].Trackpad = new Vector2(trackpadXState.CurrentState, trackpadYState.CurrentState);
            }
        }

        ActionStateFloat GetFloatState(XrAction action) {
            ActionStateGetInfo getInfo = new() {
                Type = StructureType.ActionStateGetInfo,
                Action = action,
                SubactionPath = 0
            };
            ActionStateFloat state = new() { Type = StructureType.ActionStateFloat };
            m_xr.GetActionStateFloat(m_session, in getInfo, ref state);
            return state;
        }

        ActionStateBoolean GetBoolState(XrAction action) {
            ActionStateGetInfo getInfo = new() {
                Type = StructureType.ActionStateGetInfo,
                Action = action,
                SubactionPath = 0
            };
            ActionStateBoolean state = new() { Type = StructureType.ActionStateBoolean };
            m_xr.GetActionStateBoolean(m_session, in getInfo, ref state);
            return state;
        }

        // --- IVrBackend controller methods ---

        public bool IsControllerPresent(VrController controller) =>
            m_controllers[(int)controller].IsConnected;

        public Matrix GetControllerMatrix(VrController controller) =>
            m_controllers[(int)controller].Matrix;

        public Vector2 GetStickPosition(VrController controller, float deadZone = 0f) {
            Vector2 raw = m_controllers[(int)controller].Stick;
            return deadZone > 0f ? ApplyDeadZone(raw, deadZone) : raw;
        }

        public Vector2? GetTouchpadPosition(VrController controller, float deadZone = 0f) {
            Vector2 raw = m_controllers[(int)controller].Trackpad;
            return deadZone > 0f ? ApplyDeadZone(raw, deadZone) : raw;
        }

        public float GetTriggerPosition(VrController controller, float deadZone = 0f) {
            float raw = m_controllers[(int)controller].Trigger;
            return deadZone > 0f ? ApplyDeadZone(raw, deadZone) : raw;
        }

        public bool IsButtonDown(VrController controller, VrControllerButton button) {
            int idx = (int)controller;
            return button switch {
                VrControllerButton.Trigger => m_controllers[idx].Trigger >= 0.5f,
                VrControllerButton.Grip => m_controllers[idx].Grip,
                VrControllerButton.Menu => m_controllers[idx].Menu,
                VrControllerButton.Trackpad => m_controllers[idx].StickClick,
                VrControllerButton.TrackpadCenter => m_controllers[idx].StickClick
                    && MathF.Abs(m_controllers[idx].Stick.X) < 0.3f
                    && MathF.Abs(m_controllers[idx].Stick.Y) < 0.3f,
                VrControllerButton.TrackpadLeft => m_controllers[idx].Trackpad.X < -0.5f,
                VrControllerButton.TrackpadRight => m_controllers[idx].Trackpad.X > 0.5f,
                VrControllerButton.TrackpadUp => m_controllers[idx].Trackpad.Y > 0.5f,
                VrControllerButton.TrackpadDown => m_controllers[idx].Trackpad.Y < -0.5f,
                VrControllerButton.Primary => m_controllers[idx].Primary,
                VrControllerButton.Secondary => m_controllers[idx].Secondary,
                VrControllerButton.Thumbrest => m_controllers[idx].Thumbrest,
                _ => false
            };
        }

        public bool IsButtonDownOnce(VrController controller, VrControllerButton button) {
            int idx = (int)controller;
            return button switch {
                VrControllerButton.Trigger => m_controllers[idx].Trigger >= 0.5f && m_lastControllers[idx].Trigger < 0.5f,
                VrControllerButton.Grip => m_controllers[idx].Grip && !m_lastControllers[idx].Grip,
                VrControllerButton.Menu => m_controllers[idx].Menu && !m_lastControllers[idx].Menu,
                VrControllerButton.Trackpad => m_controllers[idx].StickClick && !m_lastControllers[idx].StickClick,
                VrControllerButton.TrackpadCenter => m_controllers[idx].StickClick && !m_lastControllers[idx].StickClick
                    && MathF.Abs(m_controllers[idx].Stick.X) < 0.3f
                    && MathF.Abs(m_controllers[idx].Stick.Y) < 0.3f,
                VrControllerButton.TrackpadLeft => m_controllers[idx].Trackpad.X < -0.5f && m_lastControllers[idx].Trackpad.X >= -0.4f,
                VrControllerButton.TrackpadRight => m_controllers[idx].Trackpad.X > 0.5f && m_lastControllers[idx].Trackpad.X <= 0.4f,
                VrControllerButton.TrackpadUp => m_controllers[idx].Trackpad.Y > 0.5f && m_lastControllers[idx].Trackpad.Y <= 0.4f,
                VrControllerButton.TrackpadDown => m_controllers[idx].Trackpad.Y < -0.5f && m_lastControllers[idx].Trackpad.Y >= -0.4f,
                VrControllerButton.Primary => m_controllers[idx].Primary && !m_lastControllers[idx].Primary,
                VrControllerButton.Secondary => m_controllers[idx].Secondary && !m_lastControllers[idx].Secondary,
                VrControllerButton.Thumbrest => m_controllers[idx].Thumbrest && !m_lastControllers[idx].Thumbrest,
                _ => false
            };
        }

        // --- Eye transforms ---

        public Matrix GetEyeToHeadTransform(VrEye eye) {
            if (m_views == null) return Matrix.Identity;
            // Return only the IPD position offset, not the full view pose.
            // The rotation is already applied via HmdMatrix in the camera's CreateLookAt;
            // including it here would cause double rotation (roll inversion).
            Posef eyePose = m_views[(int)eye].Pose;
            Vector3f leftPos = m_views[0].Pose.Position;
            Vector3f rightPos = m_views[1].Pose.Position;
            float midX = (leftPos.X + rightPos.X) * 0.5f;
            float midY = (leftPos.Y + rightPos.Y) * 0.5f;
            float midZ = (leftPos.Z + rightPos.Z) * 0.5f;
            Vector3 playSpaceOffset = new(
                eyePose.Position.X - midX,
                eyePose.Position.Y - midY,
                eyePose.Position.Z - midZ
            );
            Vector3 headLocalOffset = Vector3.TransformNormal(playSpaceOffset, m_hmdMatrixInverted.OrientationMatrix);
            return Matrix.CreateTranslation(headLocalOffset);
        }

        public Matrix GetProjectionMatrix(VrEye eye, float near, float far) {
            if (m_views == null) return Matrix.Identity;
            return CreateProjectionMatrix(m_views[(int)eye].Fov, near, far);
        }

        // --- Update (called once per frame for edge detection) ---

        public void Update() {
            // Controller last-state is already updated in UpdateControllers
            // via m_lastControllers copy before current state overwrite.
        }

        // --- Event handling ---

        void PollEvents() {
            EventDataBuffer eventData = new() { Type = StructureType.EventDataBuffer };
            while (m_xr.PollEvent(m_instance, ref eventData) == Result.Success) {
                if (eventData.Type == StructureType.EventDataSessionStateChanged) {
                    EventDataSessionStateChanged* sessionEvent = (EventDataSessionStateChanged*)&eventData;
                    m_sessionState = sessionEvent->State;
                    HandleSessionStateChange();
                }
                else if (eventData.Type == StructureType.EventDataReferenceSpaceChangePending) {
                    // Reference space changed (e.g. recenter). Recreate the play space.
                    CreatePlaySpace();
                    // Track Y shift for recenter compensation, but only after
                    // tracking is established to avoid false offsets from startup events.
                    if (!m_recenterPending && m_trackingEstablished) {
                        m_hmdYBeforeRecenterRaw = m_hmdMatrix.Translation.Y - m_recenterYOffset;
                        m_recenterPending = true;
                    }
                }
            }
        }

        void HandleSessionStateChange() {
            Log.Information($"[VR] Session state: {m_sessionState}");
            switch (m_sessionState) {
                case SessionState.Ready:
                    BeginSession();
                    break;
                case SessionState.Stopping:
                    EndSession();
                    break;
                case SessionState.LossPending:
                case SessionState.Exiting:
                    IsStarted = false;
                    break;
            }
        }

        void BeginSession() {
            SessionBeginInfo beginInfo = new() {
                Type = StructureType.SessionBeginInfo,
                PrimaryViewConfigurationType = ViewConfigurationType.PrimaryStereo
            };
            Result result = m_xr.BeginSession(m_session, ref beginInfo);
            Log.Information($"[VR] xrBeginSession: {result}");
            if (result == Result.Success) {
                m_sessionState = SessionState.Synchronized;
                Log.Information($"[VR] Session state: Synchronized (implicit)");
            }
        }

        void EndSession() {
            m_xr.EndSession(m_session);
        }

        void CreatePlaySpace() {
            if (m_playSpace.Handle != 0) {
                m_xr.DestroySpace(m_playSpace);
                m_playSpace = default;
            }
            ReferenceSpaceCreateInfo spaceInfo = new() {
                Type = StructureType.ReferenceSpaceCreateInfo,
                PoseInReferenceSpace = new() {
                    Orientation = new() { X = 0, Y = 0, Z = 0, W = 1 },
                    Position = new() { X = 0, Y = 0, Z = 0 }
                }
            };
            // 1. LOCAL_FLOOR: floor-level Y (OpenXR 1.1 / XR_EXT_local_floor).
            //    Spec says no Y shift on recenter, but Quest/SteamVR violate this.
            spaceInfo.ReferenceSpaceType = ReferenceSpaceType.LocalFloor;
            Result result = m_xr.CreateReferenceSpace(m_session, ref spaceInfo, ref m_playSpace);
            if (result == Result.Success) {
                Log.Information("[VR] Play space: LOCAL_FLOOR");
                return;
            }
            Log.Information($"[VR] LOCAL_FLOOR unavailable ({result}), trying Stage");
            // 2. Stage: floor-level Y but may shift on recenter (needs compensation)
            spaceInfo.ReferenceSpaceType = ReferenceSpaceType.Stage;
            result = m_xr.CreateReferenceSpace(m_session, ref spaceInfo, ref m_playSpace);
            if (result == Result.Success) {
                Log.Information("[VR] Play space: STAGE");
                return;
            }
            // 3. Local: eye-level, last resort
            Log.Warning($"LOCAL_FLOOR and Stage unavailable, falling back to Local");
            spaceInfo.ReferenceSpaceType = ReferenceSpaceType.Local;
            result = m_xr.CreateReferenceSpace(m_session, ref spaceInfo, ref m_playSpace);
            if (result != Result.Success) {
                throw new InvalidOperationException($"xrCreateReferenceSpace failed: {result}");
            }
            Log.Information("[VR] Play space: LOCAL");
        }

        // --- Matrix utilities ---

        static Matrix PoseToMatrix(Posef pose) {
            Quaternion q = new(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W);
            Matrix rotMatrix = q.ToMatrix();
            Matrix transMatrix = Matrix.CreateTranslation(pose.Position.X, pose.Position.Y, pose.Position.Z);
            return rotMatrix * transMatrix;
        }

        static Matrix CreateViewMatrix(Posef pose) {
            // View matrix = inverse of camera world transform
            Matrix world = PoseToMatrix(pose);
            return Matrix.Invert(world);
        }

        static Matrix CreateProjectionMatrix(Fovf fov, float nearZ, float farZ) {
            float tanLeft = MathF.Tan(fov.AngleLeft);
            float tanRight = MathF.Tan(fov.AngleRight);
            float tanUp = MathF.Tan(fov.AngleUp);
            float tanDown = MathF.Tan(fov.AngleDown);
            float tanWidth = tanRight - tanLeft;
            float tanHeight = tanUp - tanDown;

            // OpenGL convention: clip Z = [-1, 1]
            float m00 = 2.0f / tanWidth;
            float m11 = 2.0f / tanHeight;
            float m02 = (tanRight + tanLeft) / tanWidth;
            float m12 = (tanUp + tanDown) / tanHeight;
            float m22 = -(farZ + nearZ) / (farZ - nearZ);
            float m23 = -(2.0f * farZ * nearZ) / (farZ - nearZ);

            return new Matrix(
                m00, 0, 0, 0,
                0, m11, 0, 0,
                m02, m12, m22, -1,
                0, 0, m23, 0
            );
        }

        // --- WalkingVelocity (not tracked by OpenXR) ---

        public Vector2 WalkingVelocity => Vector2.Zero;

        // --- Lifecycle ---

        public void StopVr() {
            if (!IsStarted) return;

            DestroyGraphicsResources();

            DestroySessionResources();

            if (m_instance.Handle != 0) {
                m_xr.DestroyInstance(m_instance);
                m_instance = default;
            }
            m_glExt?.Dispose();
            m_xr?.Dispose();

            IsStarted = false;
            // Keep IsAvailable true so StartVr can reinitialize
        }

        void DestroyGraphicsResources() {
            GL gl = Graphics.GLWrapper.GL;
            for (int i = 0; i < 2; i++) {
                if (m_swapchains[i].Handle != 0) {
                    m_xr?.DestroySwapchain(m_swapchains[i]);
                    m_swapchains[i] = default;
                }
                if (m_swapchainFbos[i] != 0) {
                    gl?.DeleteFramebuffer(m_swapchainFbos[i]);
                    m_swapchainFbos[i] = 0;
                }
                if (m_swapchainDepthRbs[i] != 0) {
                    gl?.DeleteRenderbuffer(m_swapchainDepthRbs[i]);
                    m_swapchainDepthRbs[i] = 0;
                }
            }
        }

        public void Dispose() {
            // If background init (Windows) is still in flight, wait for it so we
            // don't tear down while DoInitialize is calling into the OpenXR loader.
            try { m_initTask?.Wait(); }
            catch (Exception e) { Log.Warning($"[VR] background init wait error on dispose: {e.Message}"); }
            StopVr();
            IsAvailable = false;
            GC.SuppressFinalize(this);
        }

        void DestroySessionResources() {
            for (int i = 0; i < 2; i++) {
                if (m_controllerSpaces[i].Handle != 0) {
                    m_xr.DestroySpace(m_controllerSpaces[i]);
                    m_controllerSpaces[i] = default;
                }
            }

            for (int hand = 0; hand < 2; hand++) {
                DestroyAction(ref m_triggerActions[hand]);
                DestroyAction(ref m_gripActions[hand]);
                DestroyAction(ref m_menuActions[hand]);
                DestroyAction(ref m_stickXActions[hand]);
                DestroyAction(ref m_stickYActions[hand]);
                DestroyAction(ref m_stickClickActions[hand]);
                DestroyAction(ref m_poseActions[hand]);
                DestroyAction(ref m_primaryActions[hand]);
                DestroyAction(ref m_secondaryActions[hand]);
                DestroyAction(ref m_thumbrestActions[hand]);
                DestroyAction(ref m_trackpadXActions[hand]);
                DestroyAction(ref m_trackpadYActions[hand]);
            }

            if (m_actionSet.Handle != 0) {
                m_xr.DestroyActionSet(m_actionSet);
                m_actionSet = default;
            }

            if (m_playSpace.Handle != 0) {
                m_xr.DestroySpace(m_playSpace);
                m_playSpace = default;
            }
            if (m_session.Handle != 0) {
                m_xr.DestroySession(m_session);
                m_session = default;
            }
        }
        void DestroyAction(ref XrAction action) {
            if (action.Handle != 0) {
                m_xr.DestroyAction(action);
                action = default;
            }
        }
    }
}
#endif