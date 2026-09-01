using System.Xml.Linq;
using Engine;
using Game;

public class ModsManageContentScreen : Screen {
    public static string fName = "ModsManageContentScreen";

    public ListPanelWidget m_modsContentList;
    public ButtonWidget m_viewDetailButton;
    public ButtonWidget m_triggerEnableButton;
    public ButtonWidget m_moveUpButton;
    public ButtonWidget m_moveDownButton;
    public ButtonWidget m_openHomepageButton;

    public int m_fixedModsCount;
    public bool m_needRestart;

    public static bool IsOldApiVersionMod(ModEntity modEntity, out string description) {
        ModInfo modInfo = modEntity.modInfo;
        if (modInfo == null || string.IsNullOrEmpty(modInfo.ApiVersion)) {
            description = string.Format(LanguageControl.Get(fName, "68"), LanguageControl.Unknown);
            return true;
        }
        if (modInfo.ApiVersion.StartsWith("1.4")
            || modInfo.ApiVersion.StartsWith("1.5")
            || modInfo.ApiVersion.StartsWith("1.6")
            || modInfo.ApiVersion.StartsWith("1.7")) {
            description = string.Format(LanguageControl.Get(fName, "68"), modInfo.ApiVersion);
            return true;
        }
        if (modInfo.ApiVersionRange != null) {
            if (!modInfo.ApiVersionRange.Satisfies(ModsManager.APINuGetVersion)) {
                description = string.Format(LanguageControl.Get(fName, "76"), modInfo.ApiVersion);
                return true;
            }
            if (!modInfo.ApiVersionRange.HasUpperBound
                && modInfo.ApiVersionRange.MinVersion != null
                && modInfo.ApiVersionRange.MinVersion.Major == 1
                && modInfo.ApiVersionRange.MinVersion.Minor <= 7) {
                description = string.Format(LanguageControl.Get(fName, "68"), modInfo.ApiVersion);
                return true;
            }
        }
        description = modEntity.IsDisabled ? $"{LanguageControl.Get("ModDetailsDialog", "8")}{Storage.GetFileName(modEntity.ModFilePath)}" : GetFirstLine(modInfo.Description);
        return false;
    }

    public static string GetFirstLine(string input) {
        if (string.IsNullOrEmpty(input)) {
            return input;
        }
        ReadOnlySpan<char> span = input.AsSpan();
        int index = span.IndexOf('\n');
        return index == -1 ? input : span.Slice(0, index).TrimEnd('\r').ToString();
    }

    public ModsManageContentScreen() {
        m_fixedModsCount = ModsManager.ModListAll.IndexOf(ModsManager.FastDebugModEntity) == 1 ? 2 : 1;
        XElement node = ContentManager.Get<XElement>("Screens/ModsManageContentScreen");
        LoadContents(this, node);
        m_modsContentList = Children.Find<ListPanelWidget>("ModsContentList");
        Children.Find<LabelWidget>("TopBar.Label").Text = LanguageControl.Get(fName, "1");
        m_viewDetailButton = Children.Find<ButtonWidget>("ViewDetailButton");
        m_triggerEnableButton = Children.Find<BevelledButtonWidget>("TriggerEnableButton");
        m_moveUpButton = Children.Find<BevelledButtonWidget>("MoveUpButton");
        m_moveDownButton = Children.Find<BevelledButtonWidget>("MoveDownButton");
        m_openHomepageButton = Children.Find<BevelledButtonWidget>("OpenHomepageButton");
        m_viewDetailButton.Text = LanguageControl.Get(fName, "80");
        m_triggerEnableButton.Text = LanguageControl.Get(fName, "18");
        m_openHomepageButton.Text = LanguageControl.Get(fName, "77");
        m_modsContentList.ItemWidgetFactory = item => {
            if (item is not ModEntity entity) {
                return null;
            }
            GetTriggerAndTitle(entity, out string title, out Color titleColor);
            ModsManageContentItemWidget result = new() { Title = title, IsDisabled = entity.IsDisabled, TitleColor = titleColor };
            if (IsOldApiVersionMod(entity, out string description)) {
                result.TitleColor = Color.Red;
                if (string.IsNullOrEmpty(entity.ModFilePath)) {
                    result.IsInformationVisible = false;
                }
                else {
                    result.Information = Storage.GetFileName(entity.ModFilePath);
                }
            }
            else {
                result.Information = string.Format(
                    LanguageControl.Get(fName, "79"),
                    entity.modInfo!.Version,
                    entity.modInfo.ApiVersion,
                    entity.modInfo.Author,
                    DataSizeFormatter.Format(entity.Size)
                );
            }
            if (entity.Icon != null) {
                result.Icon = entity.Icon;
            }
            result.Description = description;
            return result;
        };
        m_modsContentList.ItemClicked += item => {
            if (item is not ModEntity entity) {
                return;
            }
            if (ReferenceEquals(entity, m_modsContentList.SelectedItem)) {
                DialogsManager.ShowDialog(null, new ModDetailsDialog(this, entity));
            }
            else {
                m_viewDetailButton.IsEnabled = true;
                m_triggerEnableButton.IsEnabled = entity.IsDisabled
                    ? entity.DisableReason == ModDisableReason.Manually
                    : entity.modInfo.PackageName is not "survivalcraft" and not "fastdebug";
                m_triggerEnableButton.Text = LanguageControl.Get(fName, GetTrigger(entity) ? "19" : "18");
                m_openHomepageButton.IsEnabled = true;
                UpdateMoveUpAndDownEnable(ModsManager.ModListAll.FindIndex(x => ReferenceEquals(x, entity)));
            }
        };
        m_modsContentList.SelectionChanged += () => {
            if (m_modsContentList.SelectedItem is not ModEntity) {
                m_viewDetailButton.IsEnabled = false;
                m_triggerEnableButton.IsEnabled = false;
                m_triggerEnableButton.Text = LanguageControl.Get(fName, "18");
                m_moveUpButton.IsEnabled = false;
                m_moveDownButton.IsEnabled = false;
                m_openHomepageButton.IsEnabled = false;
            }
        };
    }

    public override void Enter(object[] parameters) {
        m_modsContentList.AddItems(ModsManager.ModListAll);
    }

    public override void Leave() {
        m_modsContentList.ClearItems();
        m_viewDetailButton.IsEnabled = false;
        m_triggerEnableButton.IsEnabled = false;
        m_openHomepageButton.IsEnabled = false;
    }

    public override void Update() {
        if (m_modsContentList.SelectedItem is ModEntity entity) {
            if (m_viewDetailButton.IsClicked) {
                DialogsManager.ShowDialog(null, new ModDetailsDialog(this, entity));
            }
            if (m_triggerEnableButton.IsClicked) {
                TriggerEnable(entity);
            }
            if (m_moveUpButton.IsClicked) {
                MoveUp(entity);
            }
            if (m_moveDownButton.IsClicked) {
                MoveDown(entity);
            }
            if (m_openHomepageButton.IsClicked) {
                if (string.IsNullOrEmpty(entity.modInfo?.Link)) {
                    DialogsManager.ShowDialog(
                        null,
                        new MessageDialog(LanguageControl.Error, LanguageControl.Get(fName, 78), LanguageControl.Ok, null, null)
                    );
                }
                else {
                    WebBrowserManager.LaunchBrowser(entity.modInfo.Link);
                }
            }
        }
        if (Input.Back
            || Input.Cancel
            || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
            if (m_needRestart) {
                List<string> array = [];
                foreach (ModEntity entity1 in ModsManager.ModListAll) {
                    if (!string.IsNullOrEmpty(entity1.LoadAfter)) {
                        array.Add(entity1.modInfo.PackageName);
                        array.Add(entity1.LoadAfter);
                    }
                }
                SettingsManager.ModLoadAfters = string.Join(';', array);
                DialogsManager.ShowDialog(
                    null,
                    new MessageDialog(
                        LanguageControl.Get(fName, 4),
                        LanguageControl.Get(fName, 38),
                        LanguageControl.Get(fName, 39),
                        LanguageControl.Get(fName, 31),
                        delegate(MessageDialogButton result) {
                            SettingsManager.SaveSettings();
                            if (result == MessageDialogButton.Button1) {
                                Window.Restart();
                            }
                            if (result == MessageDialogButton.Button2) {
                                ScreensManager.SwitchScreen("Content");
                            }
                        }
                    )
                );
            }
            else {
                ScreensManager.SwitchScreen("Content");
            }
        }
    }

    public void TriggerEnable(ModEntity entity) {
        if (entity.IsDisabled) {
            if (entity.DisableReason == ModDisableReason.Manually) {
                if (ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions)) {
                    if (!versions.Remove(entity.modInfo.Version)) {
                        versions.Add(entity.modInfo.Version);
                    }
                }
                else {
                    ModsManager.DisabledMods.Add(entity.modInfo.PackageName, [entity.modInfo.Version]);
                }
                m_needRestart = true;
            }
        }
        else if (entity.modInfo!.PackageName is not "survivalcraft" and not "fastdebug") {
            if (ModsManager.DisabledMods.TryGetValue(entity.modInfo.PackageName, out HashSet<string> versions)) {
                if (!versions.Remove(entity.modInfo.Version)) {
                    versions.Add(entity.modInfo.Version);
                }
            }
            else {
                ModsManager.DisabledMods.Add(entity.modInfo.PackageName, [entity.modInfo.Version]);
            }
            m_needRestart = true;
        }
        if (m_modsContentList.m_widgetsByIndex[m_modsContentList.SelectedIndex!.Value] is ModsManageContentItemWidget itemWidget) {
            m_triggerEnableButton.Text = LanguageControl.Get(fName, GetTriggerAndTitle(entity, out string title, out Color titleColor) ? "19" : "18");
            itemWidget.Title = title;
            itemWidget.TitleColor = titleColor;
        }
    }

    /// <returns>true: mod能被启用，false：mod能被禁用</returns>
    public static bool GetTriggerAndTitle(ModEntity entity, out string title, out Color titleColor) {
        title = entity.modInfo?.Name ?? Storage.GetFileName(entity.ModFilePath);
        titleColor = Color.White;
        if (entity.IsDisabled) {
            if (entity.DisableReason == ModDisableReason.Manually) {
                if (ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions1)
                    && versions1.Contains(entity.modInfo.Version)) {
                    title = $"[{LanguageControl.Get(fName, "22")}] {title}";
                    titleColor = Color.Red;
                    return true;
                }
                title = $"[{LanguageControl.Get(fName, "23")} {LanguageControl.Get(fName, "81")}] {title}";
                titleColor = Color.Yellow;
                return false;
            }
            title = $"[{LanguageControl.Get(fName, "22")}] {title}";
            titleColor = Color.Red;
            return true;
        }
        if (entity.modInfo != null
            && ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions2)
            && versions2.Contains(entity.modInfo.Version)) {
            title = $"[{LanguageControl.Get(fName, "22")} {LanguageControl.Get(fName, "81")}] {title}";
            titleColor = Color.Yellow;
            return true;
        }
        return false;
    }

    /// <returns>true: mod能被启用，false：mod能被禁用</returns>
    public static bool GetTrigger(ModEntity entity) {
        if (entity.IsDisabled) {
            if (entity.DisableReason == ModDisableReason.Manually) {
                return ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions1)
                    && versions1.Contains(entity.modInfo.Version);
            }
            return true;
        }
        return entity.modInfo != null
            && ModsManager.DisabledMods.TryGetValue(entity.modInfo!.PackageName, out HashSet<string> versions2)
            && versions2.Contains(entity.modInfo.Version);
    }

    public void MoveUp(ModEntity entity) {
        // ModEntity 重写了 Equals（基于包名+版本），同包名同版本的重复模组会撞键，因此必须按引用定位
        int index = ModsManager.ModListAll.FindIndex(x => ReferenceEquals(x, entity));
        if (index <= m_fixedModsCount) {
            return;
        }
        ModEntity upEntity = ModsManager.ModListAll[index - 1];
        if (entity.modInfo.DependencyRanges.ContainsKey(upEntity.modInfo.PackageName)) {
            return;
        }
        m_needRestart = true;
        ModsManager.ModListAll[index - 1] = entity;
        ModsManager.ModListAll[index] = upEntity;
        entity.LoadAfter = ModsManager.ModListAll[index - 2].modInfo.PackageName;
        upEntity.LoadAfter = entity.modInfo.PackageName;
        if (index + 1 < ModsManager.ModListAll.Count) {
            ModsManager.ModListAll[index + 1].LoadAfter = upEntity.modInfo.PackageName;
        }
        m_modsContentList.SwapItemsAt(index, index - 1);
        UpdateMoveUpAndDownEnable(index - 1);
    }

    public void MoveDown(ModEntity entity) {
        // ModEntity 重写了 Equals（基于包名+版本），同包名同版本的重复模组会撞键，因此必须按引用定位
        int index = ModsManager.ModListAll.FindIndex(x => ReferenceEquals(x, entity));
        if (index < m_fixedModsCount || index >= ModsManager.ModListAll.Count - 1) {
            return;
        }
        ModEntity downEntity = ModsManager.ModListAll[index + 1];
        if (entity.modInfo.DependencyRanges.ContainsKey(downEntity.modInfo.PackageName)) {
            return;
        }
        m_needRestart = true;
        ModsManager.ModListAll[index + 1] = entity;
        ModsManager.ModListAll[index] = downEntity;
        entity.LoadAfter = downEntity.modInfo.PackageName;
        downEntity.LoadAfter = ModsManager.ModListAll[index - 1].modInfo.PackageName;
        if (index + 2 < ModsManager.ModListAll.Count) {
            ModsManager.ModListAll[index + 2].LoadAfter = entity.modInfo.PackageName;
        }
        m_modsContentList.SwapItemsAt(index, index + 1);
        UpdateMoveUpAndDownEnable(index + 1);
    }

    public void UpdateMoveUpAndDownEnable(int index) {
        if (index < m_fixedModsCount) {
            m_moveUpButton.IsEnabled = false;
            m_moveDownButton.IsEnabled = false;
        }
        else {
            ModEntity entity = ModsManager.ModListAll[index];
            m_moveUpButton.IsEnabled = index > m_fixedModsCount && !entity.modInfo.DependencyRanges.ContainsKey(ModsManager.ModListAll[index - 1].modInfo.PackageName);
            m_moveDownButton.IsEnabled = index < ModsManager.ModListAll.Count - 1 && !ModsManager.ModListAll[index + 1].modInfo.DependencyRanges.ContainsKey(entity.modInfo.PackageName);
        }
    }
}