using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game {
    public class GameScreen : Screen {
        public double m_lastAutosaveTime;

        public GameScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/GameScreen");
            LoadContents(this, node);
            IsDrawRequired = true;
            Window.Deactivated += delegate { GameManager.SaveProject(true, false); };
            Window.Closed += delegate { GameManager.DisposeProject(); };
        }

        public override void Enter(object[] parameters) {
            if (GameManager.Project != null) {
                GameManager.Project.FindSubsystem<SubsystemAudio>(true).Unmute();
            }
            MusicManager.StopMusic();
            MusicManager.CurrentMix = MusicManager.Mix.InGame;
        }

        public override void Leave() {
            if (GameManager.Project != null) {
                GameManager.Project.FindSubsystem<SubsystemAudio>(true).Mute();
                GameManager.SaveProject(true, true);
            }
            ShowHideCursors(true);
            MusicManager.CurrentMix = MusicManager.Mix.Menu;
        }

        public override void Update() {
            if (GameManager.Project != null) {
                double realTime = Time.RealTime;
                if (realTime - m_lastAutosaveTime > 300.0) {
                    m_lastAutosaveTime = realTime;
                    GameManager.SaveProject(false, true);
                    SettingsManager.SaveSettings();
                }
                if (MarketplaceManager.IsTrialMode
                    && GameManager.Project.FindSubsystem<SubsystemGameInfo>(true).TotalElapsedGameTime > 1140.0) {
                    GameManager.SaveProject(true, false);
                    GameManager.DisposeProject();
                    ScreensManager.SwitchScreen("TrialEnded");
                }
                GameManager.UpdateProject();
            }
            ShowHideCursors(
                GameManager.Project == null
                || DialogsManager.HasDialogs(this)
                || DialogsManager.HasDialogs(RootWidget)
                || ScreensManager.CurrentScreen != this
            );
        }

        public override void Draw(DrawContext dc) {
            if (!ScreensManager.IsAnimating
                && SettingsManager.ResolutionMode == ResolutionMode.High) {
                Display.Clear(Color.Black, 1f, 0);
            }
        }

        public void ShowHideCursors(bool show) {
            Input.IsMouseCursorVisible = show;
            Input.IsPadCursorVisible = show;
        }
    }
}