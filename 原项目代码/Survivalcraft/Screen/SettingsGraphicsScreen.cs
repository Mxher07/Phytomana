using System.Globalization;
using System.Xml.Linq;

namespace Game {
    public class SettingsGraphicsScreen : Screen {
        public BevelledButtonWidget m_virtualRealityButton;

        public SliderWidget m_brightnessSlider;

        SliderWidget m_viewAngleSlider;

        public ContainerWidget m_vrPanel;
        public SliderWidget m_vrEyeHeightOffsetSlider;
        public ContainerWidget m_vrEyeHeightOffsetPanel;

        public SettingsGraphicsScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/SettingsGraphicsScreen");
            LoadContents(this, node);
            m_virtualRealityButton = Children.Find<BevelledButtonWidget>("VirtualRealityButton");
            m_brightnessSlider = Children.Find<SliderWidget>("BrightnessSlider");
            m_viewAngleSlider = Children.Find<SliderWidget>("ViewAngleSlider");
            m_vrPanel = Children.Find<ContainerWidget>("VrPanel");
            m_vrEyeHeightOffsetSlider = Children.Find<SliderWidget>("VrEyeHeightOffsetSlider");
            m_vrEyeHeightOffsetPanel = Children.Find<ContainerWidget>("VrEyeHeightOffsetPanel");
#if !WINDOWS
            m_vrPanel.IsVisible = false;
#endif
        }

        public override void Update() {
            if (m_brightnessSlider.IsSliding) {
                SettingsManager.Brightness = m_brightnessSlider.Value;
            }
            if (m_viewAngleSlider.IsSliding) {
                SettingsManager.ViewAngle = m_viewAngleSlider.Value;
            }
            if (m_vrEyeHeightOffsetSlider.IsSliding) {
                SettingsManager.VrEyeHeightOffset = m_vrEyeHeightOffsetSlider.Value;
            }
            if (m_virtualRealityButton.IsClicked) {
                if (SettingsManager.UseVr) {
                    SettingsManager.UseVr = false;
                    if (VrManager.IsVrAvailable) {
                        VrManager.StopVr();
                    }
                }
                else {
                    // StartVr waits for background init and returns false if VR is
                    // unavailable, so the IsVrAvailable precheck is redundant (and
                    // stale during the early init window).
                    SettingsManager.UseVr = VrManager.StartVr();
                }
            }
            m_virtualRealityButton.Text = SettingsManager.UseVr ? LanguageControl.Enable : LanguageControl.Disable;
            m_brightnessSlider.Value = SettingsManager.Brightness;
            m_brightnessSlider.Text = MathF.Round(SettingsManager.Brightness * 10f).ToString(CultureInfo.InvariantCulture);
            m_viewAngleSlider.Value = SettingsManager.ViewAngle;
            m_viewAngleSlider.Text = $"{MathF.Round(SettingsManager.ViewAngle * 100f)}% ({MathF.Round(SettingsManager.ViewAngle * 80f)}°)";
            m_vrEyeHeightOffsetSlider.Value = SettingsManager.VrEyeHeightOffset;
            m_vrEyeHeightOffsetSlider.Text = $"{SettingsManager.VrEyeHeightOffset:+0.0;-0.0;0.0}m";
            m_vrEyeHeightOffsetPanel.IsVisible = VrManager.IsVrAvailable;
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
            }
        }
    }
}