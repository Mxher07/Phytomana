using System.Globalization;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game {
    public class SettingsUiScreen : Screen {
        public ContainerWidget m_windowModeContainer;

        public ButtonWidget m_windowModeButton;

        public ButtonWidget m_languageButton;

        public ButtonWidget m_displayLogButton;

        public SliderWidget m_uiScaleSlider;

        public SliderWidget m_vrGuiSizeSlider;
        public ContainerWidget m_vrGuiSizePanel;

        public ButtonWidget m_vrGuiDockLeftHandButton;
        public ContainerWidget m_vrGuiDockLeftHandPanel;

        public ButtonWidget m_upsideDownButton;

        public UniformSpacingPanelWidget m_adaptEdgeToEdgeDisplayContainer;
        public ButtonWidget m_adaptEdgeToEdgeDisplay;

        public ButtonWidget m_hideMoveLookPadsButton;

        public ButtonWidget m_hideCrosshairButton;

        public ButtonWidget m_showGuiInScreenshotsButton;

        public ButtonWidget m_showLogoInScreenshotsButton;

        public ButtonWidget m_screenshotSizeButton;
        public ContainerWidget m_screenshotSizeCustomWidthSliderContainer;
        public SliderWidget m_screenshotSizeCustomWidthSlider;
        public ContainerWidget m_screenshotSizeCustomAspectRatioSliderContainer;
        public SliderWidget m_screenshotSizeCustomAspectRatioSlider;

        public ButtonWidget m_communityContentModeButton;

        public ButtonWidget m_originalCommunityContentModeButton;

        public ButtonWidget m_deleteWorldNeedToTextButton;

        public static string fName = "SettingsUiScreen";

        public SettingsUiScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/SettingsUiScreen");
            LoadContents(this, node);
            m_windowModeContainer = Children.Find<ContainerWidget>("WindowModeContainer");
            m_languageButton = Children.Find<ButtonWidget>("LanguageButton");
            m_displayLogButton = Children.Find<ButtonWidget>("DisplayLogButton");
            m_windowModeButton = Children.Find<ButtonWidget>("WindowModeButton");
            m_uiScaleSlider = Children.Find<SliderWidget>("UIScaleSlider");
            m_vrGuiSizeSlider = Children.Find<SliderWidget>("VrGuiSizeSlider");
            m_vrGuiSizePanel = Children.Find<ContainerWidget>("VrGuiSizePanel");
            m_vrGuiDockLeftHandButton = Children.Find<ButtonWidget>("VrGuiDockLeftHand");
            m_vrGuiDockLeftHandPanel = Children.Find<ContainerWidget>("VrGuiDockLeftHandPanel");
            m_upsideDownButton = Children.Find<ButtonWidget>("UpsideDownButton");
            m_adaptEdgeToEdgeDisplayContainer = Children.Find<UniformSpacingPanelWidget>("AdaptEdgeToEdgeDisplayContainer");
            m_adaptEdgeToEdgeDisplay = Children.Find<ButtonWidget>("AdaptEdgeToEdgeDisplay");
            m_hideMoveLookPadsButton = Children.Find<ButtonWidget>("HideMoveLookPads");
            m_hideCrosshairButton = Children.Find<ButtonWidget>("HideCrosshair");
            m_showGuiInScreenshotsButton = Children.Find<ButtonWidget>("ShowGuiInScreenshotsButton");
            m_showLogoInScreenshotsButton = Children.Find<ButtonWidget>("ShowLogoInScreenshotsButton");
            m_screenshotSizeButton = Children.Find<ButtonWidget>("ScreenshotSizeButton");
            m_screenshotSizeCustomWidthSliderContainer = Children.Find<ContainerWidget>("ScreenshotSizeCustomWidthSliderContainer");
            m_screenshotSizeCustomWidthSlider = Children.Find<SliderWidget>("ScreenshotSizeCustomWidthSlider");
            m_screenshotSizeCustomAspectRatioSliderContainer = Children.Find<ContainerWidget>("ScreenshotSizeCustomAspectRatioSliderContainer");
            m_screenshotSizeCustomAspectRatioSlider = Children.Find<SliderWidget>("ScreenshotSizeCustomAspectRatioSlider");
            m_communityContentModeButton = Children.Find<ButtonWidget>("CommunityContentModeButton");
            m_originalCommunityContentModeButton = Children.Find<ButtonWidget>("OriginalCommunityContentModeButton");
            m_deleteWorldNeedToTextButton = Children.Find<ButtonWidget>("DeleteWorldNeedToTextButton");
            int maxWidth = Math.Min(Display.MaxTextureSize, 16320);
            m_screenshotSizeCustomWidthSlider.MaxValue = SettingsManager.ScreenshotSizeCustomWidths.Length - 1;
            m_screenshotSizeCustomAspectRatioSlider.MaxValue = SettingsManager.ScreenshotSizeCustomAspectRatios.Length - 1;
            m_screenshotSizeCustomWidthSlider.Value = SettingsManager.ScreenshotSizeCustomWidthIndex;
            m_screenshotSizeCustomAspectRatioSlider.Value = SettingsManager.ScreenshotSizeCustomAspectRatioIndex;
            if (SettingsManager.ScreenshotSize != ScreenshotSize.Custom) {
                m_screenshotSizeCustomWidthSliderContainer.IsVisible = false;
                m_screenshotSizeCustomAspectRatioSliderContainer.IsVisible = false;
            }
        }

        public override void Enter(object[] parameters) {
            m_windowModeContainer.IsVisible = VersionsManager.CurrentPlatform != VersionsManager.Platform.Android;
            m_adaptEdgeToEdgeDisplayContainer.IsVisible = VersionsManager.CurrentPlatform == VersionsManager.Platform.Android;
        }

        public override void Update() {
            if (m_windowModeButton.IsClicked) {
#if BROWSER
                Window.WindowMode = Window.WindowMode == WindowMode.Fullscreen ? WindowMode.Fixed : WindowMode.Fullscreen;
#else
                SettingsManager.WindowMode = (WindowMode)((int)(SettingsManager.WindowMode + 1) % EnumUtils.GetEnumValues<WindowMode>().Count);
#endif
            }
            if (m_uiScaleSlider.SlidingCompleted) {
                SettingsManager.UIScale = m_uiScaleSlider.Value;
            }
            if (m_vrGuiSizeSlider.SlidingCompleted) {
                SettingsManager.VrGuiSize = m_vrGuiSizeSlider.Value;
            }
            if (m_languageButton.IsClicked) {
                LanguageControl.CreateLanguageSelectionDialog(null);
            }
            if (m_displayLogButton.IsClicked) {
                SettingsManager.DisplayLog = !SettingsManager.DisplayLog;
            }
            if (!m_uiScaleSlider.IsSliding) {
                m_uiScaleSlider.Value = SettingsManager.UIScale;
            }
            m_uiScaleSlider.Text = $"{m_uiScaleSlider.Value * 100f:0}%";
            if (!m_vrGuiSizeSlider.IsSliding) {
                m_vrGuiSizeSlider.Value = SettingsManager.VrGuiSize;
            }
            m_vrGuiSizeSlider.Text = $"{m_vrGuiSizeSlider.Value * 100f:0}%";
            m_vrGuiSizePanel.IsVisible = VrManager.IsVrAvailable;
            if (m_vrGuiDockLeftHandButton.IsClicked) {
                SettingsManager.VrGuiDockLeftHand = !SettingsManager.VrGuiDockLeftHand;
            }
            m_vrGuiDockLeftHandButton.Text = SettingsManager.VrGuiDockLeftHand ? LanguageControl.Yes : LanguageControl.No;
            m_vrGuiDockLeftHandPanel.IsVisible = VrManager.IsVrAvailable;
            if (m_upsideDownButton.IsClicked) {
                SettingsManager.UpsideDownLayout = !SettingsManager.UpsideDownLayout;
            }
            if (m_adaptEdgeToEdgeDisplay.IsClicked) {
                SettingsManager.AdaptEdgeToEdgeDisplay = !SettingsManager.AdaptEdgeToEdgeDisplay;
                if (SettingsManager.AdaptEdgeToEdgeDisplay) {
                    ScreensManager.UpdateTopBarMarginLeft();
                }
                else {
                    ScreensManager.ResetAllTopBarMarginLeft();
                }
            }
            if (m_hideMoveLookPadsButton.IsClicked) {
                SettingsManager.HideMoveLookPads = !SettingsManager.HideMoveLookPads;
            }
            if (m_hideCrosshairButton.IsClicked) {
                SettingsManager.HideCrosshair = !SettingsManager.HideCrosshair;
            }
            if (m_showGuiInScreenshotsButton.IsClicked) {
                SettingsManager.ShowGuiInScreenshots = !SettingsManager.ShowGuiInScreenshots;
            }
            if (m_showLogoInScreenshotsButton.IsClicked) {
                SettingsManager.ShowLogoInScreenshots = !SettingsManager.ShowLogoInScreenshots;
            }
            if (m_screenshotSizeButton.IsClicked) {
                SettingsManager.ScreenshotSize = (ScreenshotSize)((int)(SettingsManager.ScreenshotSize + 1)
                    % EnumUtils.GetEnumValues<ScreenshotSize>().Count);
                if (SettingsManager.ScreenshotSize == ScreenshotSize.Custom) {
                    m_screenshotSizeCustomWidthSliderContainer.IsVisible = true;
                    m_screenshotSizeCustomAspectRatioSliderContainer.IsVisible = true;
                }
                else {
                    m_screenshotSizeCustomWidthSliderContainer.IsVisible = false;
                    m_screenshotSizeCustomAspectRatioSliderContainer.IsVisible = false;
                }
            }
            if (m_screenshotSizeCustomWidthSlider.IsSliding) {
                SettingsManager.ScreenshotSizeCustomWidthIndex = (int)m_screenshotSizeCustomWidthSlider.Value;
            }
            if (m_screenshotSizeCustomAspectRatioSlider.IsSliding) {
                SettingsManager.ScreenshotSizeCustomAspectRatioIndex = (int)m_screenshotSizeCustomAspectRatioSlider.Value;
            }
            if (m_deleteWorldNeedToTextButton.IsClicked) {
                SettingsManager.DeleteWorldNeedToText = !SettingsManager.DeleteWorldNeedToText;
            }
            if (m_communityContentModeButton.IsClicked) {
                SettingsManager.CommunityContentMode = (CommunityContentMode)((int)(SettingsManager.CommunityContentMode + 1)
                    % EnumUtils.GetEnumValues<CommunityContentMode>().Count);
            }
            if (m_originalCommunityContentModeButton.IsClicked) {
                SettingsManager.OriginalCommunityContentMode = (CommunityContentMode)((int)(SettingsManager.OriginalCommunityContentMode + 1)
                    % EnumUtils.GetEnumValues<CommunityContentMode>().Count);
            }
#if BROWSER
            m_windowModeButton.Text = LanguageControl.Get("WindowMode", Window.WindowMode.ToString());
#else
            m_windowModeButton.Text = LanguageControl.Get("WindowMode", SettingsManager.WindowMode.ToString());
#endif
            m_languageButton.Text = LanguageControl.Get("Language", "Name");
            m_displayLogButton.Text = SettingsManager.DisplayLog ? LanguageControl.Yes : LanguageControl.No;
            m_upsideDownButton.Text = SettingsManager.UpsideDownLayout ? LanguageControl.Yes : LanguageControl.No;
            m_adaptEdgeToEdgeDisplay.Text = SettingsManager.AdaptEdgeToEdgeDisplay ? LanguageControl.Yes : LanguageControl.No;
            m_hideMoveLookPadsButton.Text = SettingsManager.HideMoveLookPads ? LanguageControl.Yes : LanguageControl.No;
            m_hideCrosshairButton.Text = SettingsManager.HideCrosshair ? LanguageControl.Yes : LanguageControl.No;
            m_showGuiInScreenshotsButton.Text = SettingsManager.ShowGuiInScreenshots ? LanguageControl.Yes : LanguageControl.No;
            m_showLogoInScreenshotsButton.Text = SettingsManager.ShowLogoInScreenshots ? LanguageControl.Yes : LanguageControl.No;
            m_screenshotSizeButton.Text = LanguageControl.Get("ScreenshotSize", SettingsManager.ScreenshotSize.ToString());
            m_screenshotSizeCustomWidthSlider.Text = SettingsManager.ScreenshotSizeCustomWidths[SettingsManager.ScreenshotSizeCustomWidthIndex].ToString();
            m_screenshotSizeCustomAspectRatioSlider.Text = SettingsManager.ScreenshotSizeCustomAspectRatiosNames[SettingsManager.ScreenshotSizeCustomAspectRatioIndex];
            m_deleteWorldNeedToTextButton.Text = SettingsManager.DeleteWorldNeedToText ? LanguageControl.Yes : LanguageControl.No;
            m_communityContentModeButton.Text = LanguageControl.Get("CommunityContentMode", SettingsManager.CommunityContentMode.ToString());
            m_originalCommunityContentModeButton.Text = LanguageControl.Get(
                "CommunityContentMode",
                SettingsManager.OriginalCommunityContentMode.ToString()
            );
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
            }
        }
    }
}