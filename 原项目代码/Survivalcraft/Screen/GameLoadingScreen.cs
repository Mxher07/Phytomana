using System.Xml.Linq;

namespace Game {
    public class GameLoadingScreen : Screen {
        public WorldInfo m_worldInfo;

        public string m_worldSnapshotName;

        public LabelWidget m_loadingLabel;

        public StateMachine m_stateMachine = new();

        public bool m_upgradeCompleted;

        public Exception m_upgradeError;
        public static string fName = "GameLoadingScreen";

        public GameLoadingScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/GameLoadingScreen");
            LoadContents(this, node);
            m_stateMachine.AddState(
                "WaitingForFadeIn",
                null,
                delegate {
                    if (!ScreensManager.IsAnimating) {
                        if (string.IsNullOrEmpty(m_worldSnapshotName)) {
                            m_stateMachine.TransitionTo("Loading");
                        }
                        else {
                            m_stateMachine.TransitionTo("RestoringSnapshot");
                        }
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Upgrading",
                delegate {
                    GameManager.DisposeProject();
                    m_upgradeCompleted = false;
                    m_upgradeError = null;
                    Task.Run(
                        delegate {
                            try {
                                GameManager.RepairAndUpgradeWorld(m_worldInfo);
                                m_upgradeCompleted = true;
                            }
                            catch (Exception upgradeError) {
                                m_upgradeError = upgradeError;
                            }
                        }
                    );
                },
                delegate {
                    if (m_upgradeCompleted) {
                        m_stateMachine.TransitionTo("Loading");
                    }
                    else if (m_upgradeError != null) {
                        throw m_upgradeError;
                    }
                },
                null
            );
            m_stateMachine.AddState(
                "Loading",
                null,
                delegate {
                    ContainerWidget gamesWidget = ScreensManager.FindScreen<GameScreen>("Game").Children.Find<ContainerWidget>("GamesWidget");
                    GameManager.LoadProject(m_worldInfo, gamesWidget);
                    ScreensManager.SwitchScreen("Game");
                },
                null
            );
            m_stateMachine.AddState(
                "RestoringSnapshot",
                null,
                delegate {
                    GameManager.DisposeProject();
                    WorldsManager.RestoreWorldFromSnapshot(m_worldInfo.DirectoryName, m_worldSnapshotName);
                    m_stateMachine.TransitionTo("Upgrading");
                },
                null
            );
        }

        public override void Update() {
            try {
                m_stateMachine.Update();
            }
            catch (Exception) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
                ViewGameLogDialog dialog = new();
                dialog.SetErrorHead(11, 10);
                DialogsManager.ShowDialog(null, dialog);
            }
        }

        public override void Enter(object[] parameters) {
            m_worldInfo = (WorldInfo)parameters[0];
            m_worldSnapshotName = (string)parameters[1];
            m_stateMachine.TransitionTo("WaitingForFadeIn");
            ProgressManager.UpdateProgress("Loading World", 0f);
            CommunityContentScreen screen = ScreensManager.FindScreen<CommunityContentScreen>("CommunityContent");
            if (screen != null) {
                screen.m_treePanel.Clear();
                screen.m_treePanel.ScrollPosition = 0f;
                screen.m_contentExpiryTime = 0;
            }
            OriginalCommunityContentScreen originalCommunityContentScreen = ScreensManager.FindScreen<OriginalCommunityContentScreen>("OriginalCommunityContent");
            if (originalCommunityContentScreen != null) {
                originalCommunityContentScreen.m_listPanel.ClearItems();
                originalCommunityContentScreen.m_listPanel.ScrollPosition = 0f;
                originalCommunityContentScreen.m_itemsCacheExpiryTime = 0;
            }
        }
    }
}