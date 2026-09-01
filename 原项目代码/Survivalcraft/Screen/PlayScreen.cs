using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Engine;
using TemplatesDatabase;

namespace Game {
    public class PlayScreen : Screen {
        public ListPanelWidget m_worldsListWidget;
        public ButtonWidget m_playButton;
        public ButtonWidget m_newWorldButton;
        public ButtonWidget m_propertiesButton;

        public static int MaxWorlds = 300;
        public double m_modTipsTime;
        public long m_totalWorldsSize;
        public CultureInfo m_cultureInfo;
        public static string fName = "PlayScreen";

        public virtual void OnWorldsListWidgetItemClicked(object item) {
            if (item != null
                && m_worldsListWidget.SelectedItem == item) {
                Play(item);
            }
        }

        public Widget WorldInfoWidget(object item) {
            WorldInfo worldInfo = (WorldInfo)item;
            XElement node2 = ContentManager.Get<XElement>("Widgets/SavedWorldItem");
            ContainerWidget containerWidget = (ContainerWidget)LoadWidget(this, node2, null);
            LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("WorldItem.Name");
            LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("WorldItem.Details");
            containerWidget.Tag = worldInfo;
            labelWidget.Text = worldInfo.WorldSettings.Name;
            labelWidget2.Text = string.Format(
                "{0} | {1} | {2} | {3} | {4}",
                DataSizeFormatter.Format(worldInfo.Size),
                worldInfo.LastSaveTime.ToLocalTime().ToString(m_cultureInfo),
                worldInfo.PlayerInfos.Count > 1
                    ? string.Format(LanguageControl.GetContentWidgets(fName, 9), worldInfo.PlayerInfos.Count)
                    : string.Format(LanguageControl.GetContentWidgets(fName, 10), 1),
                LanguageControl.Get("GameMode", worldInfo.WorldSettings.GameMode.ToString()),
                LanguageControl.Get("EnvironmentBehaviorMode", worldInfo.WorldSettings.EnvironmentBehaviorMode.ToString())
            );
            if (worldInfo.SerializationVersion != VersionsManager.SerializationVersion) {
                labelWidget2.Text = $"{labelWidget2.Text} | {(string.IsNullOrEmpty(worldInfo.SerializationVersion)
                    ? LanguageControl.GetContentWidgets("Usual", "Unknown")
                    : $"({worldInfo.SerializationVersion})")}";
            }
            ModsManager.HookAction(
                "LoadWorldInfoWidget",
                loader => {
                    loader.LoadWorldInfoWidget(worldInfo, node2, ref containerWidget);
                    return false;
                }
            );
            return containerWidget;
        }

        public PlayScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/PlayScreen");
            LoadContents(this, node);
            m_worldsListWidget = Children.Find<ListPanelWidget>("WorldsList");
            m_playButton = Children.Find<ButtonWidget>("Play");
            m_newWorldButton = Children.Find<ButtonWidget>("NewWorld");
            m_propertiesButton = Children.Find<ButtonWidget>("Properties");
            ListPanelWidget worldsListWidget = m_worldsListWidget;
            worldsListWidget.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(worldsListWidget.ItemWidgetFactory, WorldInfoWidget);
            m_worldsListWidget.ScrollPosition = 0f;
            m_worldsListWidget.ScrollSpeed = 0f;
            m_worldsListWidget.ItemClicked += OnWorldsListWidgetItemClicked;
            m_modTipsTime = -10000000f;
#if BROWSER
            m_cultureInfo = CultureInfo.CurrentCulture;
#else
            m_cultureInfo = Program.SystemLanguage == null
                ? CultureInfo.CurrentCulture
                : new CultureInfo(Program.SystemLanguage);
#endif
        }

        public override void Enter(object[] parameters) {
            BusyDialog dialog = new(LanguageControl.GetContentWidgets(fName, 5), null);
            DialogsManager.ShowDialog(null, dialog);
            Task.Run(
                delegate {
                    WorldInfo selectedItem = (WorldInfo)m_worldsListWidget.SelectedItem;
                    WorldsManager.UpdateWorldsList();
                    List<WorldInfo> worldInfos = new(WorldsManager.WorldInfos);
                    worldInfos.Sort((w1, w2) => DateTime.Compare(w2.LastSaveTime, w1.LastSaveTime));
                    Dispatcher.Dispatch(
                        delegate {
                            m_worldsListWidget.ClearItems();
                            foreach (WorldInfo item in worldInfos) {
                                m_worldsListWidget.AddItem(item);
                            }
                            m_totalWorldsSize = worldInfos.Sum(wi => wi.Size);
                            if (selectedItem != null) {
                                m_worldsListWidget.SelectedItem = worldInfos.FirstOrDefault(wi => wi.DirectoryName == selectedItem.DirectoryName);
                            }
                            DialogsManager.HideDialog(dialog);
                            if (parameters.Length > 0 && parameters[0] is string str) {
                                WorldInfo info = WorldsManager.WorldInfos.FirstOrDefault(wi => Storage.GetFileName(wi.DirectoryName) == str);
                                if (info != null) {
                                    Play(info);
                                }
                                else {
                                    DialogsManager.ShowDialog(
                                        null,
                                        new MessageDialog(LanguageControl.Error, string.Format(LanguageControl.Get(fName, "12"), str), LanguageControl.Ok, null, null)
                                    );
                                }
                            }
                        }
                    );
                }
            );
        }

        public override void Update() {
            Vector2 size = SettingsManager.UIScale > 1f ? new Vector2(250, 60) : new Vector2(310, 60);
            m_playButton.Size = size;
            m_newWorldButton.Size = size;
            if (m_worldsListWidget.SelectedItem != null
                && WorldsManager.WorldInfos.IndexOf((WorldInfo)m_worldsListWidget.SelectedItem) < 0) {
                m_worldsListWidget.SelectedItem = null;
            }
            if (m_worldsListWidget.Items.Count > 0) {
                Children.Find<LabelWidget>("TopBar.Label").Text = string.Format(
                    LanguageControl.Get(fName, m_worldsListWidget.Items.Count > 1 ? "10" : "9"),
                    m_worldsListWidget.Items.Count,
                    DataSizeFormatter.Format(m_totalWorldsSize, 2)
                );
            }
            else {
                Children.Find<LabelWidget>("TopBar.Label").Text = LanguageControl.Get(fName, "11");
            }
            m_playButton.IsEnabled = m_worldsListWidget.SelectedItem != null;
            m_propertiesButton.IsEnabled = m_worldsListWidget.SelectedItem != null;
            if (m_playButton.IsClicked
                && m_worldsListWidget.SelectedItem != null) {
                Play(m_worldsListWidget.SelectedItem);
            }
            if (m_newWorldButton.IsClicked) {
                if (WorldsManager.WorldInfos.Count >= MaxWorlds) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(
                            LanguageControl.GetContentWidgets(fName, 7),
                            string.Format(LanguageControl.GetContentWidgets(fName, 8), MaxWorlds),
                            LanguageControl.GetContentWidgets("Usual", "ok"),
                            null,
                            null
                        )
                    );
                }
                else {
                    ScreensManager.SwitchScreen("NewWorld");
                    m_worldsListWidget.SelectedItem = null;
                }
            }
            if (m_propertiesButton.IsClicked
                && m_worldsListWidget.SelectedItem != null) {
                WorldInfo worldInfo = (WorldInfo)m_worldsListWidget.SelectedItem;
                ScreensManager.SwitchScreen("ModifyWorld", worldInfo.DirectoryName, worldInfo.WorldSettings);
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen("MainMenu");
                m_worldsListWidget.SelectedItem = null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="item">实际类型为WorldInfo</param>
        public void Play(object item) {
            bool flag = false;
            WorldInfo worldInfo = item as WorldInfo;
            string languageType = !ModsManager.Configs.TryGetValue("Language", out string config) ? "zh-CN" : config;
            if (languageType == "zh-CN"
                && Time.RealTime - m_modTipsTime > 3600f) {
                m_modTipsTime = Time.RealTime;
                flag |= ShowTips(item);
            }
            List<ValuesDictionary> modsNotLoaded = [];
            List<ValuesDictionary> modsVersionNotCapable = [];
            if (worldInfo != null) {
                XElement projectNode = WorldsManager.GetProjectNode(worldInfo);
                if (projectNode != null) {
                    XElement subsystemUsedModsNode = WorldsManager.GetSubsystemNode(projectNode, "UsedMods", false);
                    if (subsystemUsedModsNode != null) {
                        ValuesDictionary subsystemValuesDictionary = new();
                        subsystemValuesDictionary.ApplyOverrides(subsystemUsedModsNode);
                        int modsCount = subsystemValuesDictionary.GetValue("ModsCount", 0);
                        ValuesDictionary valuesDictionary = subsystemValuesDictionary.GetValue<ValuesDictionary>("Mods", null);
                        if (valuesDictionary != null) {
                            for (int i = 0; i < modsCount; i++) {
                                ValuesDictionary modDictionary = valuesDictionary.GetValue<ValuesDictionary>(i.ToString(), null);
                                if (modDictionary == null) {
                                    continue;
                                }
                                bool entityGotten = ModsManager.GetModEntity(
                                    modDictionary.GetValue("PackageName", string.Empty),
                                    out ModEntity modEntity
                                );
                                if (!entityGotten) {
                                    modsNotLoaded.Add(modDictionary);
                                    continue;
                                }
                                bool versionComparePass = modEntity?.Loader?.CompareModVersion(
                                        modEntity.modInfo.Version,
                                        modDictionary.GetValue("Version", "?")
                                    )
                                    ?? true;
                                modDictionary.SetValue("CurrentVersion", modEntity.modInfo.Version);
                                if (!versionComparePass) {
                                    modsVersionNotCapable.Add(modDictionary);
                                }
                            }
                        }
                    }
                }
            }
            if (!flag) {
                if (modsNotLoaded.Count > 0
                    || modsVersionNotCapable.Count > 0) {
                    StringBuilder text = new();
                    if (modsNotLoaded.Count > 0) {
                        text.AppendLine(LanguageControl.Get(fName, 3));
                    }
                    foreach (ValuesDictionary modDictionary in modsNotLoaded) {
                        text.AppendLine(
                            string.Format(
                                LanguageControl.Get(fName, 4),
                                modDictionary.GetValue("Name", "?"),
                                modDictionary.GetValue("Version", "?")
                            )
                        );
                    }
                    if (modsVersionNotCapable.Count > 0) {
                        text.AppendLine(LanguageControl.Get(fName, 5));
                    }
                    foreach (ValuesDictionary modDictionary in modsVersionNotCapable) {
                        text.AppendLine(
                            string.Format(
                                LanguageControl.Get(fName, 6),
                                modDictionary.GetValue("Name", "?"),
                                modDictionary.GetValue("Version", "?"),
                                modDictionary.GetValue("CurrentVersion", "?")
                            )
                        );
                    }
                    text.AppendLine(LanguageControl.Get(fName, 7));
                    DialogsManager.ShowDialog(
                        this,
                        new MessageDialog(
                            LanguageControl.Get(fName, 8),
                            text.ToString(),
                            LanguageControl.Yes,
                            LanguageControl.No,
                            delegate(MessageDialogButton button) {
                                if (button == MessageDialogButton.Button1) {
                                    GameLoad(item);
                                }
                            }
                        )
                    );
                }
                else {
                    GameLoad(item);
                }
            }
        }

        public void GameLoad(object item) {
            ModsManager.HookAction(
                "BeforeGameLoading",
                loader => {
                    item = loader.BeforeGameLoading(this, item);
                    return false;
                }
            );
            if (item != null) {
                ScreensManager.SwitchScreen("GameLoading", item, null);
            }
            m_worldsListWidget.SelectedItem = null;
        }

        public bool ShowTips(object item) {
            string tips = string.Empty;
            int num = 1;
            try {
                foreach (ModEntity modEntity in ModsManager.ModList) {
                    foreach (MotdManager.FilterMod value in MotdManager.FilterModAll) {
                        if (value.FilterAPIVersion == ModsManager.APIVersionString
                            && value.PackageName == modEntity.modInfo.PackageName
                            && CompareVersion(value.Version, modEntity.modInfo.Version)) {
                            tips += $"{num}.{modEntity.modInfo.Name}(v{modEntity.modInfo.Version})  {value.Explanation}\n";
                            num++;
                        }
                    }
                }
            }
            catch {
                return false;
            }
            if (!string.IsNullOrEmpty(tips)) {
                DialogsManager.ShowDialog(
                    null,
                    new MessageDialog(
                        LanguageControl.Get(fName, "1"),
                        tips,
                        LanguageControl.Get(fName, "2"),
                        LanguageControl.Back,
                        delegate(MessageDialogButton button) {
                            if (button == MessageDialogButton.Button1) {
                                GameLoad(item);
                            }
                        }
                    )
                );
                return true;
            }
            return false;
        }

        public bool CompareVersion(string v1, string v2) {
            if (v1 == "all") {
                return true;
            }
            if (v1.Contains("~")) {
                string[] versions = v1.Split(['~'], StringSplitOptions.RemoveEmptyEntries);
                try {
                    double minv = double.Parse(versions[0]);
                    double maxv = double.Parse(versions[1]);
                    double v = double.Parse(v2);
                    return v >= minv && v <= maxv;
                }
                catch {
                    return false;
                }
            }
            if (v1.Contains(";")) {
                string[] versions = v1.Split([';'], StringSplitOptions.RemoveEmptyEntries);
                foreach (string v in versions) {
                    if (v == v2) {
                        return true;
                    }
                }
                return false;
            }
            return v1 == v2;
        }
    }
}