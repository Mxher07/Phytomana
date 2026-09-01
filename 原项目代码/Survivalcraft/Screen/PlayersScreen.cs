using System.Xml.Linq;
using Engine;

namespace Game {
    public class PlayersScreen : Screen {
        public StackPanelWidget m_playersPanel;
        public ButtonWidget m_addPlayerButton;
        public ButtonWidget m_screenLayoutButton;

        public SubsystemPlayers m_subsystemPlayers;
        public CharacterSkinsCache m_characterSkinsCache = new();
        public const string fName = "PlayersScreen";

        public PlayersScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/PlayersScreen");
            LoadContents(this, node);
            m_playersPanel = Children.Find<StackPanelWidget>("PlayersPanel");
            m_addPlayerButton = Children.Find<ButtonWidget>("AddPlayerButton");
            m_screenLayoutButton = Children.Find<ButtonWidget>("ScreenLayoutButton");
        }

        public override void Enter(object[] parameters) {
            m_subsystemPlayers = (SubsystemPlayers)parameters[0];
            m_subsystemPlayers.PlayerAdded += PlayersChanged;
            m_subsystemPlayers.PlayerRemoved += PlayersChanged;
            UpdatePlayersPanel();
        }

        public override void Leave() {
            m_subsystemPlayers.PlayerAdded -= PlayersChanged;
            m_subsystemPlayers.PlayerRemoved -= PlayersChanged;
            m_subsystemPlayers = null;
            m_characterSkinsCache.Clear();
            m_playersPanel.Children.Clear();
        }

        public override void Update() {
            if (m_addPlayerButton.IsClicked) {
                SubsystemGameInfo subsystemGameInfo = m_subsystemPlayers.Project.FindSubsystem<SubsystemGameInfo>(true);
                if (subsystemGameInfo.WorldSettings.GameMode == GameMode.Cruel) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            LanguageControl.Unavailable,
                            LanguageControl.GetContentWidgets(fName, 3),
                            LanguageControl.Ok,
                            null,
                            null
                        )
                    );
                }
                else if (subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            LanguageControl.Unavailable,
                            LanguageControl.GetContentWidgets(fName, 4),
                            LanguageControl.Ok,
                            null,
                            null
                        )
                    );
                }
                else if (m_subsystemPlayers.PlayersData.Count >= 4) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            LanguageControl.Unavailable,
                            string.Format(LanguageControl.GetContentWidgets(fName, 5), SubsystemPlayers.MaxPlayers),
                            LanguageControl.Ok,
                            null,
                            null
                        )
                    );
                }
                else {
                    ScreensManager.SwitchScreen("Player", PlayerScreen.Mode.Add, m_subsystemPlayers.Project);
                }
            }
            if (m_screenLayoutButton.IsClicked) {
                ScreenLayout[] array = null;
                if (m_subsystemPlayers.PlayersData.Count == 1) {
                    array = new ScreenLayout[1];
                }
                else if (m_subsystemPlayers.PlayersData.Count == 2) {
                    array = [ScreenLayout.DoubleVertical, ScreenLayout.DoubleHorizontal, ScreenLayout.DoubleOpposite];
                }
                else if (m_subsystemPlayers.PlayersData.Count == 3) {
                    array = [ScreenLayout.TripleVertical, ScreenLayout.TripleHorizontal, ScreenLayout.TripleEven, ScreenLayout.TripleOpposite];
                }
                else if (m_subsystemPlayers.PlayersData.Count == 4) {
                    array = [ScreenLayout.Quadruple, ScreenLayout.QuadrupleOpposite];
                }
                if (array != null) {
                    DialogsManager.ShowDialog(
                        null,
                        new ListSelectionDialog(
                            LanguageControl.GetContentWidgets(fName, 6),
                            array,
                            80f,
                            delegate(object o) {
                                string str = o.ToString();
                                string name = $"Textures/Atlas/ScreenLayout{str}";
                                return new StackPanelWidget {
                                    Direction = LayoutDirection.Horizontal,
                                    VerticalAlignment = WidgetAlignment.Center,
                                    Children = {
                                        new RectangleWidget {
                                            Size = new Vector2(98f, 56f),
                                            Subtexture = ContentManager.Get<Subtexture>(name),
                                            FillColor = Color.White,
                                            OutlineColor = Color.Transparent,
                                            Margin = new Vector2(10f, 0f)
                                        },
                                        new StackPanelWidget {
                                            Direction = LayoutDirection.Vertical,
                                            VerticalAlignment = WidgetAlignment.Center,
                                            Margin = new Vector2(10f, 0f),
                                            Children = {
                                                new LabelWidget { Text = StringsManager.GetString($"ScreenLayout.{str}.Name") },
                                                new LabelWidget {
                                                    Text = StringsManager.GetString($"ScreenLayout.{str}.Description"), Color = Color.Gray
                                                }
                                            }
                                        }
                                    }
                                };
                            },
                            delegate(object o) {
                                if (o != null) {
                                    if (m_subsystemPlayers.PlayersData.Count == 1) {
                                        SettingsManager.ScreenLayout1 = (ScreenLayout)o;
                                    }
                                    if (m_subsystemPlayers.PlayersData.Count == 2) {
                                        SettingsManager.ScreenLayout2 = (ScreenLayout)o;
                                    }
                                    if (m_subsystemPlayers.PlayersData.Count == 3) {
                                        SettingsManager.ScreenLayout3 = (ScreenLayout)o;
                                    }
                                    if (m_subsystemPlayers.PlayersData.Count == 4) {
                                        SettingsManager.ScreenLayout4 = (ScreenLayout)o;
                                    }
                                }
                            }
                        )
                    );
                }
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen("Game");
            }
        }

        public virtual void UpdatePlayersPanel() {
            m_playersPanel.Children.Clear();
            foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData) {
                m_playersPanel.Children.Add(new PlayerWidget(playersDatum, m_characterSkinsCache));
            }
        }

        public void PlayersChanged(PlayerData playerData) {
            UpdatePlayersPanel();
        }
    }
}