using System.Globalization;
using System.Xml.Linq;

namespace Game {
    public class SettingsAudioScreen : Screen {
        public SliderWidget m_soundsVolumeSlider;

        public SliderWidget m_musicVolumeSlider;

        public SettingsAudioScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/SettingsAudioScreen");
            LoadContents(this, node);
            m_soundsVolumeSlider = Children.Find<SliderWidget>("SoundsVolumeSlider");
            m_musicVolumeSlider = Children.Find<SliderWidget>("MusicVolumeSlider");
        }

        public override void Update() {
            if (m_soundsVolumeSlider.IsSliding) {
                SettingsManager.SoundsVolume = m_soundsVolumeSlider.Value;
            }
            if (m_musicVolumeSlider.IsSliding) {
                SettingsManager.MusicVolume = m_musicVolumeSlider.Value;
            }
            m_soundsVolumeSlider.Value = SettingsManager.SoundsVolume;
            m_soundsVolumeSlider.Text = MathF.Round(SettingsManager.SoundsVolume * 10f).ToString(CultureInfo.InvariantCulture);
            m_musicVolumeSlider.Value = SettingsManager.MusicVolume;
            m_musicVolumeSlider.Text = MathF.Round(SettingsManager.MusicVolume * 10f).ToString(CultureInfo.InvariantCulture);
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
            }
        }
    }
}