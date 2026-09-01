using Engine;
using Engine.Graphics;

namespace Game {
    public static class ScreensManager {
        public class AnimationData {
            public Screen OldScreen;
            public Screen NewScreen;
            public float Factor;
            public float Speed;
            public object[] Parameters;
        }

        public static Dictionary<string, Screen> m_screens = [];
        public static AnimationData m_animationData;
        public static PrimitivesRenderer2D m_pr2 = new();
        public static PrimitivesRenderer3D m_pr3 = new();
        public static Random Random = new(0);
        public static RenderTarget2D m_uiRenderTarget;
        public static Vector3 m_vrQuadPosition;
        public static Matrix m_vrQuadMatrix;
        static bool m_vrQuadInitialized;
        public static float DebugUiScale = 1f;

        public static ContainerWidget RootWidget { get; set; }

        public static bool IsAnimating => m_animationData != null;

        public static Screen CurrentScreen { get; set; }

        /// <summary>
        ///     上一个Screen
        /// </summary>
        public static Screen PreviousScreen { get; set; }

        public static Stack<Screen> HistoryStack { get; } = [];

        public static Screen TopOfHistoryScreen => HistoryStack.TryPeek(out Screen screen) ? screen : null;

        public static float FinalUiScale { get; set; }

        public static T FindScreen<T>(string name) where T : Screen {
            m_screens.TryGetValue(name, out Screen value);
            return (T)value;
        }

        public static void AddScreen(string name, Screen screen) => m_screens.Add(name, screen);

        public static void SwitchScreen(string name, params object[] parameters) => SwitchScreen(string.IsNullOrEmpty(name) ? null : FindScreen<Screen>(name), parameters);

        public static void SwitchScreen(Screen screen, params object[] parameters) {
            ModsManager.HookAction(
                "OnSwitchScreen",
                loader => {
                    loader.OnSwitchScreen(ref screen, parameters);
                    return false;
                }
            );
            if (screen == CurrentScreen) {
                return;
            }
            if (screen == null) {
                throw new ArgumentNullException(nameof(screen));
            }
            if (m_animationData != null) {
                EndAnimation();
            }
            m_animationData = new AnimationData {
                NewScreen = screen, OldScreen = CurrentScreen, Parameters = parameters, Speed = CurrentScreen == null ? float.MaxValue : 4f
            };
            if (CurrentScreen != null) {
                RootWidget.IsUpdateEnabled = false;
                CurrentScreen.Input.Clear();
            }
            PreviousScreen = CurrentScreen;
            if (screen == TopOfHistoryScreen) {
                HistoryStack.Pop();
            }
            else if (CurrentScreen != null) {
                HistoryStack.Push(CurrentScreen);
            }
            CurrentScreen = screen;
            UpdateAnimation();
            if (CurrentScreen != null) {
                Log.Verbose($"Entered screen \"{GetScreenName(CurrentScreen)}\"");
                UpdateTopBarMarginLeft();
            }
        }

        public static void GoBack(params object[] parameters) => SwitchScreen(TopOfHistoryScreen, parameters);

        public static void Initialize() {
            RootWidget = new CanvasWidget();
            RootWidget.WidgetsHierarchyInput = new WidgetInput();
            InitScreens();
            SwitchScreen("Loading");
            Window.DisplayCutoutInsetsChanged += (_, _) => UpdateTopBarMarginLeft();
        }

        public static void InitScreens() {
            LoadingScreen loadingScreen = new();
            AddScreen("Loading", loadingScreen);
        }

        public static void Update() {
            if (m_animationData != null) {
                UpdateAnimation();
            }
            Widget.UpdateWidgetsHierarchy(RootWidget);
        }

        public static void Draw() {
            Utilities.Dispose(ref m_uiRenderTarget);

            if (VrManager.IsFrameActive && CurrentScreen is not GameScreen) {
                RenderVrMenu();
#if ANDROID
                return;
#endif
            }
            LayoutAndDrawWidgets();
        }

        static void RenderVrMenu() {
            int vrW = VrManager.SwapchainWidth;
            int vrH = VrManager.SwapchainHeight;
#if WINDOWS
            Point2 desktopSize = Display.BackbufferSize;
#else
            // Android VR: use fixed 1280x720 UI resolution
            Point2 desktopSize = new(1280, 720);
#endif
            float desktopAspect = (float)desktopSize.X / desktopSize.Y;
            float vrAspect = (float)vrW / vrH;
            int rtW, rtH;
            if (desktopAspect > vrAspect) {
                rtW = vrW;
                rtH = (int)MathF.Ceiling(vrW / desktopAspect);
            }
            else {
                rtH = vrH;
                rtW = (int)MathF.Ceiling(vrH * desktopAspect);
            }

            if (m_uiRenderTarget == null
                || m_uiRenderTarget.Width != rtW
                || m_uiRenderTarget.Height != rtH) {
                Utilities.Dispose(ref m_uiRenderTarget);
                m_uiRenderTarget = new RenderTarget2D(rtW, rtH, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
            }

            Display.RenderTarget = m_uiRenderTarget;
            Display.Clear(Color.Transparent, 1f, 0);
            LayoutAndDrawWidgets();
            Display.RenderTarget = null;

            AnimateVrQuad();

            if (!m_vrQuadInitialized) return;

            VrManager.RenderToEyes((vrEye, eyeFrame) => {
                DrawVrBackground();
                DrawVrQuad();
                m_pr3.Flush(eyeFrame.ViewMatrix * eyeFrame.ProjectionMatrix);
            });
        }

        public static void UpdateAnimation() {
            float num = MathUtils.Min(Time.FrameDuration, 0.1f);
            float factor = m_animationData.Factor;
            m_animationData.Factor = MathUtils.Min(m_animationData.Factor + m_animationData.Speed * num, 1f);
            if (m_animationData.Factor < 0.5f) {
                if (m_animationData.OldScreen != null) {
                    float num2 = 2f * (0.5f - m_animationData.Factor);
                    float scale = 1f;
                    m_animationData.OldScreen.ColorTransform = new Color(num2, num2, num2, num2);
                    m_animationData.OldScreen.RenderTransform =
                        Matrix.CreateTranslation(
                            (0f - m_animationData.OldScreen.ActualSize.X) / 2f,
                            (0f - m_animationData.OldScreen.ActualSize.Y) / 2f,
                            0f
                        )
                        * Matrix.CreateScale(scale)
                        * Matrix.CreateTranslation(m_animationData.OldScreen.ActualSize.X / 2f, m_animationData.OldScreen.ActualSize.Y / 2f, 0f);
                }
            }
            else if (factor < 0.5f) {
                if (m_animationData.OldScreen != null) {
                    m_animationData.OldScreen.Leave();
                    ModsManager.HookAction(
                        "OnScreenLeaved",
                        loader => {
                            loader.OnScreenLeaved(m_animationData.OldScreen);
                            return false;
                        }
                    );
                    RootWidget.Children.Remove(m_animationData.OldScreen);
                }
                if (m_animationData.NewScreen != null) {
                    RootWidget.Children.Insert(0, m_animationData.NewScreen);
                    m_animationData.NewScreen.Enter(m_animationData.Parameters);
                    m_animationData.NewScreen.ColorTransform = Color.Transparent;
                    ModsManager.HookAction(
                        "OnScreenEntered",
                        loader => {
                            loader.OnScreenEntered(m_animationData.NewScreen, m_animationData.Parameters);
                            return false;
                        }
                    );
                    RootWidget.IsUpdateEnabled = true;
                }
            }
            else if (m_animationData.NewScreen != null) {
                float num3 = 2f * (m_animationData.Factor - 0.5f);
                float scale2 = 1f;
                m_animationData.NewScreen.ColorTransform = new Color(num3, num3, num3, num3);
                m_animationData.NewScreen.RenderTransform =
                    Matrix.CreateTranslation(
                        (0f - m_animationData.NewScreen.ActualSize.X) / 2f,
                        (0f - m_animationData.NewScreen.ActualSize.Y) / 2f,
                        0f
                    )
                    * Matrix.CreateScale(scale2)
                    * Matrix.CreateTranslation(m_animationData.NewScreen.ActualSize.X / 2f, m_animationData.NewScreen.ActualSize.Y / 2f, 0f);
            }
            if (m_animationData.Factor >= 1f) {
                EndAnimation();
            }
        }

        public static void EndAnimation() {
            if (m_animationData.NewScreen != null) {
                m_animationData.NewScreen.ColorTransform = Color.White;
                m_animationData.NewScreen.RenderTransform = Matrix.CreateScale(1f);
            }
            m_animationData = null;
        }

        public static string GetScreenName(Screen screen) {
            string key = m_screens.FirstOrDefault(kvp => kvp.Value == screen).Key;
            if (key == null) {
                return string.Empty;
            }
            return key;
        }

        public static void AnimateVrQuad() {
            if (Time.FrameIndex >= 5) {
                float num = 6f;
                Matrix hmdMatrix = VrManager.HmdMatrix;
                Vector3 hmdForward = hmdMatrix.Forward * new Vector3(1f, 0f, 1f);
                if (hmdForward.LengthSquared() < 0.001f) return;
                Vector3 vector = hmdMatrix.Translation
                    + num * (Vector3.Normalize(hmdForward) + new Vector3(0f, 0.1f, 0f));
                if (!m_vrQuadInitialized) {
                    m_vrQuadPosition = vector;
                    m_vrQuadInitialized = true;
                }
                if (Vector3.Distance(m_vrQuadPosition, vector) > 0f) {
                    Vector3 v = vector * new Vector3(1f, 0f, 1f) - m_vrQuadPosition * new Vector3(1f, 0f, 1f);
                    Vector3 v2 = vector * new Vector3(0f, 1f, 0f) - m_vrQuadPosition * new Vector3(0f, 1f, 0f);
                    float num2 = v.Length();
                    float num3 = v2.Length();
                    m_vrQuadPosition += v * MathUtils.Min(0.75f * MathF.Pow(MathUtils.Max(num2 - 0.15f * num, 0f), 0.33f) * Time.FrameDuration, 1f);
                    m_vrQuadPosition += v2 * MathUtils.Min(1.5f * MathF.Pow(MathUtils.Max(num3 - 0.05f * num, 0f), 0.33f) * Time.FrameDuration, 1f);
                }
                Vector2 vector2 = new(m_uiRenderTarget.Width / (float)m_uiRenderTarget.Height, 1f);
                vector2 /= MathUtils.Max(vector2.X, vector2.Y);
                vector2 *= 7.5f;
                m_vrQuadMatrix.Forward = Vector3.Normalize(hmdMatrix.Translation - m_vrQuadPosition);
                m_vrQuadMatrix.Right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, m_vrQuadMatrix.Forward)) * vector2.X;
                m_vrQuadMatrix.Up = Vector3.Normalize(Vector3.Cross(m_vrQuadMatrix.Forward, m_vrQuadMatrix.Right)) * vector2.Y;
                m_vrQuadMatrix.Translation = m_vrQuadPosition - 0.5f * (m_vrQuadMatrix.Right + m_vrQuadMatrix.Up);
                RootWidget.WidgetsHierarchyInput.VrQuadMatrix = m_vrQuadMatrix;
            }
        }

        public static void DrawVrQuad() {
            QueueQuad(
                m_pr3.TexturedBatch(
                    m_uiRenderTarget,
                    false,
                    0,
                    DepthStencilState.Default,
                    RasterizerState.CullNoneScissor,
                    BlendState.Opaque,
                    SamplerState.LinearClamp
                ),
                m_vrQuadMatrix.Translation,
                m_vrQuadMatrix.Right,
                m_vrQuadMatrix.Up,
                Color.White
            );
        }

        public static void DrawVrBackground() {
            Matrix hmdMatrix = VrManager.HmdMatrix;
            TexturedBatch3D batch = m_pr3.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Star"));
            Random.Seed(0);
            for (int i = 0; i < 1500; i++) {
                float f = MathF.Pow(Random.Float(0f, 1f), 6f);
                Color rGB = (MathUtils.Lerp(0.05f, 0.4f, f) * Color.White).RGB;
                int num = 6;
                Vector3 vector = Random.Vector3(500f);
                Vector3 vector2 = Vector3.Normalize(Vector3.Cross(vector, Vector3.UnitY)) * num;
                Vector3 up = Vector3.Normalize(Vector3.Cross(vector2, vector)) * num;
                QueueQuad(batch, vector + hmdMatrix.Translation, vector2, up, rGB);
            }
            TexturedBatch3D batch2 = m_pr3.TexturedBatch(
                ContentManager.Get<Texture2D>("Textures/Blocks"),
                true,
                1,
                null,
                null,
                null,
                SamplerState.PointClamp
            );
            for (int j = -8; j <= 8; j++) {
                for (int k = -8; k <= 8; k++) {
                    float num2 = 1f;
                    float num3 = 1f;
                    Vector3 vector3 = new Vector3((j - 0.5f) * num2, 0f, (k - 0.5f) * num2)
                        + new Vector3(MathF.Round(hmdMatrix.Translation.X), 0f, MathF.Round(hmdMatrix.Translation.Z));
                    float num4 = Vector3.Distance(vector3, hmdMatrix.Translation);
                    float num5 = MathUtils.Lerp(1f, 0f, MathUtils.Saturate(num4 / 7f));
                    if (num5 > 0f) {
                        QueueQuad(
                            batch2,
                            vector3,
                            new Vector3(num3, 0f, 0f),
                            new Vector3(0f, 0f, num3),
                            Color.Gray * num5,
                            new Vector2(0.1875f, 0.25f),
                            new Vector2(0.25f, 0.3125f)
                        );
                    }
                }
            }
        }

        public static void LayoutAndDrawWidgets() {
            if (m_animationData != null) {
                Display.Clear(Color.Black, 1f, 0);
            }
            float num = 850f / Math.Clamp(SettingsManager.UIScale, 0.5f, 1.2f) * DebugUiScale;
            Vector2 vector = new(Display.Viewport.Width, Display.Viewport.Height);
            float num2 = vector.X / num;
            Vector2 availableSize = new(num, num / vector.X * vector.Y);
            float num3 = num * 9f / 16f;
            if (vector.Y / num2 < num3) {
                num2 = vector.Y / num3;
                availableSize = new Vector2(num3 / vector.Y * vector.X, num3);
            }
            FinalUiScale = 1f / num2;
            RootWidget.LayoutTransform = Matrix.CreateScale(num2, num2, 1f);
            if (SettingsManager.UpsideDownLayout) {
                RootWidget.LayoutTransform *= new Matrix(
                    -1f,
                    0f,
                    0f,
                    0f,
                    0f,
                    -1f,
                    0f,
                    0f,
                    0f,
                    0f,
                    1f,
                    0f,
                    0f,
                    0f,
                    0f,
                    1f
                );
            }
            Widget.LayoutWidgetsHierarchy(RootWidget, availableSize);
            Widget.DrawWidgetsHierarchy(RootWidget);
        }

        public static void QueueQuad(FlatBatch3D batch, Vector3 corner, Vector3 right, Vector3 up, Color color) {
            Vector3 p = corner + right;
            Vector3 p2 = corner + right + up;
            Vector3 p3 = corner + up;
            batch.QueueQuad(corner, p, p2, p3, color);
        }

        public static void QueueQuad(TexturedBatch3D batch, Vector3 center, Vector3 right, Vector3 up, Color color) {
            QueueQuad(
                batch,
                center,
                right,
                up,
                color,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f)
            );
        }

        public static void QueueQuad(TexturedBatch3D batch,
            Vector3 corner,
            Vector3 right,
            Vector3 up,
            Color color,
            Vector2 tc1,
            Vector2 tc2) {
            Vector3 p = corner + right;
            Vector3 p2 = corner + right + up;
            Vector3 p3 = corner + up;
            batch.QueueQuad(
                corner,
                p,
                p2,
                p3,
                new Vector2(tc1.X, tc2.Y),
                new Vector2(tc2.X, tc2.Y),
                new Vector2(tc2.X, tc1.Y),
                new Vector2(tc1.X, tc1.Y),
                color
            );
        }

        public static void UpdateTopBarMarginLeft() {
            if (SettingsManager.AdaptEdgeToEdgeDisplay
                && CurrentScreen?.Children.Find<BevelledButtonWidget>("TopBar.Back", false)?.ParentWidget?.ParentWidget is CanvasWidget topBar
                && topBar.Size.X == 64f) {
                topBar.MarginLeft = Window.DisplayCutoutInsets.X * FinalUiScale;
            }
        }

        public static void ResetAllTopBarMarginLeft() {
            foreach (Screen screen in m_screens.Values) {
                if (screen.Children.Find<BevelledButtonWidget>("TopBar.Back", false)?.ParentWidget?.ParentWidget is CanvasWidget topBar
                    && topBar.Size.X == 64f) {
                    topBar.MarginLeft = 0f;
                }
            }
        }
    }
}