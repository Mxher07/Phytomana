using System.Xml.Linq;
using Engine;

namespace Game {
    public class SettingsPerformanceScreen : Screen {
        public static List<int> m_presentationIntervals = [2, 1, 0];

        public static List<int> m_visibilityRanges = [
            32,
            48,
            64,
            80,
            96,
            112,
            128,
            160,
            192,
            224,
            256,
            320,
            384,
            448,
            512,
            576,
            640,
            704,
            768,
            832,
            896,
            960,
            1024,
            1280,
            1536,
            2048
        ];

        public ButtonWidget m_resolutionButton;

        public SliderWidget m_visibilityRangeSlider;

        public LabelWidget m_visibilityRangeWarningLabel;

        public ButtonWidget m_terrainMipmapsButton;

        public ButtonWidget m_skyRenderingModeButton;

        public ButtonWidget m_objectShadowsButton;

        public SliderWidget m_framerateLimitSlider;

        public ButtonWidget m_displayFpsCounterButton;

        public ButtonWidget m_displayFpsRibbonButton;

        public SliderWidget m_lowFPSToTimeDecelerationSlider;

        public ButtonWidget m_useAPISleepTimeAccelerationButton;

        public SliderWidget m_animatedTextureRefreshLimitSlider;

        public int m_enterVisibilityRange;
        public static string fName = "SettingsPerformanceScreen";

        public SettingsPerformanceScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/SettingsPerformanceScreen");
            LoadContents(this, node);
            m_resolutionButton = Children.Find<ButtonWidget>("ResolutionButton");
            m_visibilityRangeSlider = Children.Find<SliderWidget>("VisibilityRangeSlider");
            m_visibilityRangeWarningLabel = Children.Find<LabelWidget>("VisibilityRangeWarningLabel");
            m_terrainMipmapsButton = Children.Find<ButtonWidget>("TerrainMipmapsButton");
            m_skyRenderingModeButton = Children.Find<ButtonWidget>("SkyRenderingModeButton");
            m_objectShadowsButton = Children.Find<ButtonWidget>("ObjectShadowsButton");
            m_framerateLimitSlider = Children.Find<SliderWidget>("FramerateLimitSlider");
            m_displayFpsCounterButton = Children.Find<ButtonWidget>("DisplayFpsCounterButton");
            m_displayFpsRibbonButton = Children.Find<ButtonWidget>("DisplayFpsRibbonButton");
            m_lowFPSToTimeDecelerationSlider = Children.Find<SliderWidget>("LowFPSToTimeDeceleration");
            m_useAPISleepTimeAccelerationButton = Children.Find<ButtonWidget>("UseAPISleepTimeAccelerationButton");
            m_visibilityRangeSlider.MinValue = 0f;
            m_visibilityRangeSlider.MaxValue = m_visibilityRanges.Count - 1;
            m_lowFPSToTimeDecelerationSlider.MinValue = 0f;
            m_lowFPSToTimeDecelerationSlider.MaxValue = 20f;
            m_lowFPSToTimeDecelerationSlider.Value = SettingsManager.LowFPSToTimeDeceleration;
            m_animatedTextureRefreshLimitSlider = Children.Find<SliderWidget>("AnimatedTextureRefreshLimitSlider");
#if ANDROID
            m_framerateLimitSlider.MinValue = 1;
#elif BROWSER
            m_framerateLimitSlider.MinValue = 1;
            m_framerateLimitSlider.MaxValue = 1;
#else
            if (Engine.Graphics.GLWrapper.UsingAngle) {
                m_framerateLimitSlider.MinValue = 1;
            }
#endif
        }

        public override void Enter(object[] parameters) {
            m_enterVisibilityRange = SettingsManager.VisibilityRange;
        }

        public override void Update() {
            if (m_resolutionButton.IsClicked) {
                IList<int> enumValues = EnumUtils.GetEnumValues<ResolutionMode>();
                SettingsManager.ResolutionMode = (ResolutionMode)((enumValues.IndexOf((int)SettingsManager.ResolutionMode) + 1) % enumValues.Count);
            }
            if (m_visibilityRangeSlider.IsSliding) {
                SettingsManager.VisibilityRange = m_visibilityRanges[Math.Clamp((int)m_visibilityRangeSlider.Value, 0, m_visibilityRanges.Count - 1)];
            }
            if (m_terrainMipmapsButton.IsClicked) {
                SettingsManager.TerrainMipmapsEnabled = !SettingsManager.TerrainMipmapsEnabled;
            }
            if (m_skyRenderingModeButton.IsClicked) {
                IList<int> enumValues3 = EnumUtils.GetEnumValues<SkyRenderingMode>();
                SettingsManager.SkyRenderingMode = (SkyRenderingMode)((enumValues3.IndexOf((int)SettingsManager.SkyRenderingMode) + 1)
                    % enumValues3.Count);
            }
            if (m_objectShadowsButton.IsClicked) {
                SettingsManager.ObjectsShadowsEnabled = !SettingsManager.ObjectsShadowsEnabled;
            }
            if (m_framerateLimitSlider.IsSliding) {
                SettingsManager.PresentationInterval = m_presentationIntervals[Math.Clamp(
                    (int)m_framerateLimitSlider.Value,
                    0,
                    m_presentationIntervals.Count - 1
                )];
                Window.PresentationInterval = SettingsManager.PresentationInterval;
            }
            if (m_displayFpsCounterButton.IsClicked) {
                SettingsManager.DisplayFpsCounter = !SettingsManager.DisplayFpsCounter;
            }
            if (m_displayFpsRibbonButton.IsClicked) {
                SettingsManager.DisplayFpsRibbon = !SettingsManager.DisplayFpsRibbon;
            }
            if (m_lowFPSToTimeDecelerationSlider.IsSliding) {
                SettingsManager.LowFPSToTimeDeceleration = m_lowFPSToTimeDecelerationSlider.Value;
            }
            if (m_useAPISleepTimeAccelerationButton.IsClicked) {
                SettingsManager.UseAPISleepTimeAcceleration = !SettingsManager.UseAPISleepTimeAcceleration;
            }
            if (m_animatedTextureRefreshLimitSlider.IsSliding) {
                SettingsManager.AnimatedTextureRefreshLimit = (int)m_animatedTextureRefreshLimitSlider.Value;
            }
            m_resolutionButton.Text = LanguageControl.Get("ResolutionMode", SettingsManager.ResolutionMode.ToString());
            m_visibilityRangeSlider.Value = m_visibilityRanges.IndexOf(SettingsManager.VisibilityRange) >= 0
                ? m_visibilityRanges.IndexOf(SettingsManager.VisibilityRange)
                : 64;
            m_visibilityRangeSlider.Text = string.Format(LanguageControl.Get(fName, 1), SettingsManager.VisibilityRange);
            if (SettingsManager.VisibilityRange <= 48) {
                m_visibilityRangeWarningLabel.IsVisible = true;
                m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 2);
            }
            else if (SettingsManager.VisibilityRange <= 64) {
                m_visibilityRangeWarningLabel.IsVisible = false;
            }
            else if (SettingsManager.VisibilityRange <= 112) {
                m_visibilityRangeWarningLabel.IsVisible = true;
                m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 3);
            }
            else if (SettingsManager.VisibilityRange <= 224) {
                m_visibilityRangeWarningLabel.IsVisible = true;
                m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 4);
            }
            else if (SettingsManager.VisibilityRange <= 384) {
                m_visibilityRangeWarningLabel.IsVisible = true;
                m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 5);
            }
            else if (SettingsManager.VisibilityRange <= 512) {
                m_visibilityRangeWarningLabel.IsVisible = true;
                m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 6);
            }
            else if (SettingsManager.VisibilityRange <= 1024) {
                m_visibilityRangeWarningLabel.IsVisible = true;
                m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 7);
            }
            else {
                m_visibilityRangeWarningLabel.IsVisible = true;
                m_visibilityRangeWarningLabel.Text = LanguageControl.Get(fName, 18);
            }
            m_terrainMipmapsButton.Text = SettingsManager.TerrainMipmapsEnabled ? LanguageControl.Enable : LanguageControl.Disable;
            m_skyRenderingModeButton.Text = LanguageControl.Get("SkyRenderingMode", SettingsManager.SkyRenderingMode.ToString());
            m_objectShadowsButton.Text = SettingsManager.ObjectsShadowsEnabled ? LanguageControl.Enable : LanguageControl.Disable;
            m_framerateLimitSlider.Value = m_presentationIntervals.IndexOf(SettingsManager.PresentationInterval) >= 0
                ? m_presentationIntervals.IndexOf(SettingsManager.PresentationInterval)
                : m_presentationIntervals.Count - 1;
            string str = SettingsManager.PresentationInterval switch {
                1 => "14",
                2 => "15",
                _ => "9"
            };
            m_framerateLimitSlider.Text = LanguageControl.Get(fName, str);
            m_displayFpsCounterButton.Text = SettingsManager.DisplayFpsCounter ? LanguageControl.Yes : LanguageControl.No;
            m_displayFpsRibbonButton.Text = SettingsManager.DisplayFpsRibbon ? LanguageControl.Yes : LanguageControl.No;
            m_lowFPSToTimeDecelerationSlider.Text = SettingsManager.LowFPSToTimeDeceleration > 0
                ? string.Format(LanguageControl.Get(fName, 8), SettingsManager.LowFPSToTimeDeceleration)
                : LanguageControl.Get(fName, 9);
            m_useAPISleepTimeAccelerationButton.Text = SettingsManager.UseAPISleepTimeAcceleration
                ? LanguageControl.Get(fName, 12)
                : LanguageControl.Get(fName, 13);
            switch (SettingsManager.AnimatedTextureRefreshLimit) {
                case 0: m_animatedTextureRefreshLimitSlider.Text = LanguageControl.Get(fName, 16); break;
                case >= 7: m_animatedTextureRefreshLimitSlider.Text = LanguageControl.Get(fName, 17); break;
                default: m_animatedTextureRefreshLimitSlider.Text = (SettingsManager.AnimatedTextureRefreshLimit * 10).ToString(); break;
            }
            m_animatedTextureRefreshLimitSlider.Value = SettingsManager.AnimatedTextureRefreshLimit;
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                bool flag = SettingsManager.VisibilityRange > 128;
                if (SettingsManager.VisibilityRange > m_enterVisibilityRange && flag) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            LanguageControl.Get(fName, 10),
                            LanguageControl.Get(fName, 11),
                            LanguageControl.Ok,
                            LanguageControl.Back,
                            delegate(MessageDialogButton button) {
                                if (button == MessageDialogButton.Button1) {
                                    ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
                                }
                            }
                        )
                    );
                }
                else {
                    ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
                }
            }
        }
    }
}