using System.Xml.Linq;
using Engine;

namespace Game {
    /// <summary>
    /// 通用递归设置页面。root 聚合所有模组顶层 Page；非 root 渲染指定页面 Items。
    /// 单实例复用：每次 Enter(params) 重建内容；内部页面栈导航规避单实例 SwitchScreen 同实例重入限制。
    /// </summary>
    public class ModSettingsScreen : Screen {
        LabelWidget m_titleLabel;
        StackPanelWidget m_contentStack;
        LabelWidget m_descriptionLabel;

        string m_packageName;
        string[] m_pageIds;
        ModSettingPage m_currentPage;
        string m_descriptionText;

        // 栈空 = root；非空 peek 为当前页
        readonly Stack<(string PackageName, string[] PageIds, string Title)> m_pageStack = new();
        readonly Dictionary<BevelledButtonWidget, (string PackageName, string[] PageIds, string Title)> m_navButtons = new();
        readonly List<IModSettingItemWidget> m_itemWidgets = new();

        public const string fName = "ModSettingsScreen";

        public ModSettingsScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/ModSettingsScreen");
            LoadContents(this, node);
            m_titleLabel = Children.Find<LabelWidget>("TopBar.Label");
            m_contentStack = Children.Find<StackPanelWidget>("ContentStack");
            m_descriptionLabel = Children.Find<LabelWidget>("Description");
        }

        public override void Enter(object[] parameters) {
            ModSettingsManager.SettingChanged += OnSettingChanged;
            m_pageStack.Clear();
            NavigateCurrent();
        }

        public override void Leave() {
            ModSettingsManager.SettingChanged -= OnSettingChanged;
            base.Leave();
        }

        /// <summary>Manager 值变更：按 CachedPath 命中当前页 Widget 并刷新控件显示。逐项 try/catch 隔离，单项失败不影响其它。</summary>
        void OnSettingChanged(string path, object value) {
            foreach (IModSettingItemWidget w in m_itemWidgets) {
                try {
                    if (w.Descriptor?.CachedPath != path) continue;
                    w.ApplyExternalValue(value);
                }
                catch (Exception e) {
                    if (!LanguageControl.TryGet(out string msg, fName, "11")) msg = "ApplyExternalValue error, item={0}: {1}";
                    Log.Error("[ModSettings] " + string.Format(msg, w.Descriptor?.Id, e.Message));
                }
            }
        }

        void NavigateCurrent() {
            m_descriptionText = null;
            m_contentStack.Children.Clear();
            m_navButtons.Clear();
            m_itemWidgets.Clear();
            if (m_pageStack.Count == 0)
                BuildRoot();
            else {
                (string PackageName, string[] PageIds, string Title) top = m_pageStack.Peek();
                m_packageName = top.PackageName;
                m_pageIds = top.PageIds;
                m_titleLabel.Text = top.Title;
                BuildPage();
            }
        }

        void BuildRoot() {
            m_titleLabel.Text = LanguageControl.Get(fName, "1");
            // 每个模组一组：左图标+名称，右该模组各顶层 Page 的入口按钮
            foreach ((string packageName, List<ModSettingPage> pages) in ModSettingsManager.ModSettingPages) {
                if (pages == null || pages.Count == 0 || !ModsManager.PackageNameToModEntity.TryGetValue(packageName, out ModEntity modEntity)) continue;

                UniformSpacingPanelWidget group = new() {
                    Direction = LayoutDirection.Horizontal,
                    Margin = new Vector2(20f, 8f)
                };
                StackPanelWidget right = new() {
                    Direction = LayoutDirection.Vertical,
                    VerticalAlignment = WidgetAlignment.Center
                };
                foreach (ModSettingPage page in pages) {
                    if (page.Items.Count == 0) continue;
                    string[] pageIds = { page.Id };
                    string entryName = ModSettingLocalizer.ResolveText(packageName, pageIds, "Name", page.Name, true);
                    string title = ModSettingLocalizer.ResolveText(packageName, pageIds, "Title", page.Title, true);
                    AddNavButton(right, entryName, packageName, pageIds, title);
                }
                if (right.Children.Count == 0) continue;

                StackPanelWidget left = new() {
                    Direction = LayoutDirection.Horizontal
                };
                left.Children.Add(new RectangleWidget {
                    Size = new Vector2(60f),
                    FillColor = Color.White,
                    OutlineColor = Color.Transparent,
                    HorizontalAlignment = WidgetAlignment.Center,
                    VerticalAlignment = WidgetAlignment.Center,
                    Subtexture = modEntity.Icon != null
                        ? new Subtexture(modEntity.Icon, Vector2.Zero, Vector2.One)
                        : ContentManager.Get<Subtexture>("Textures/Gui/DefaultModIcon")
                });
                left.Children.Add(new LabelWidget {
                    Text = modEntity.modInfo.Name,
                    Color = Color.White,
                    VerticalAlignment = WidgetAlignment.Center,
                    Margin = new Vector2(10f, 0f),
                    WordWrap = true
                });

                group.Children.Add(left);
                group.Children.Add(right);
                m_contentStack.Children.Add(group);
                m_contentStack.Children.Add(CreateElementWidget(new ModSettingSeparator()));
            }
            m_descriptionLabel.Text = m_contentStack.Children.Count > 0 ? LanguageControl.Get(fName, "2") : LanguageControl.Get(fName, "3");
            m_currentPage = null;
        }

        void BuildPage() {
            m_currentPage = FindPage(ModSettingsManager.GetPages(m_packageName), m_pageIds);
            if (m_currentPage == null) return;
            m_descriptionLabel.Text = ModSettingLocalizer.ResolveText(m_packageName, m_pageIds, "Description", m_currentPage.Description, false);
            foreach (ModSettingElement el in m_currentPage.Items) {
                if (CreateElementWidget(el) is Widget w)
                    m_contentStack.Children.Add(w);
            }
        }

        void AddNavButton(ContainerWidget parent, string text, string packageName, string[] pageIds, string title) {
            BevelledButtonWidget btn = new() {
                Style = ContentManager.Get<XElement>("Styles/ButtonStyle_310x60"),
                Text = text,
                HorizontalAlignment = WidgetAlignment.Center,
                VerticalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(0f, 6f)
            };
            m_navButtons[btn] = (packageName, pageIds, title);
            parent.Children.Add(btn);
        }

        Widget CreateElementWidget(ModSettingElement el) {
            switch (el) {
                case ModSettingLabel label: {
                    bool hasId = label.Id != null;
                    string[] idChain = hasId ? AppendId(m_pageIds, label.Id) : m_pageIds;
                    string fieldName = hasId ? "Name" : "Text";
                    return new LabelWidget {
                        Text = ModSettingLocalizer.ResolveText(m_packageName, idChain, fieldName, label.Text, true),
                        HorizontalAlignment = WidgetAlignment.Near,
                        Color = new Color(200, 200, 200),
                        Margin = new Vector2(20f, 6f)
                    };
                }
                case ModSettingSeparator:
                    return new RectangleWidget {
                        Size = new Vector2(float.PositiveInfinity, 2f),
                        FillColor = new Color(80, 80, 80),
                        OutlineColor = Color.Transparent,
                        HorizontalAlignment = WidgetAlignment.Center,
                        Margin = new Vector2(80f, 6f)
                    };
                case ModSettingPage subPage: {
                    string[] subIds = AppendId(m_pageIds, subPage.Id);
                    string name = ModSettingLocalizer.ResolveText(m_packageName, subIds, "Name", subPage.Name, true);
                    string title = ModSettingLocalizer.ResolveText(m_packageName, subIds, "Title", subPage.Title, true);
                    BevelledButtonWidget btn = new() {
                        Style = ContentManager.Get<XElement>("Styles/ButtonStyle_310x60"),
                        Text = name,
                        HorizontalAlignment = WidgetAlignment.Center,
                        VerticalAlignment = WidgetAlignment.Center,
                        Margin = new Vector2(0f, 6f)
                    };
                    m_navButtons[btn] = (m_packageName, subIds, title);
                    return btn;
                }
                case ModSettingItem item: {
                    string[] itemChain = AppendId(m_pageIds, item.Id);
                    string name = ModSettingLocalizer.ResolveText(m_packageName, itemChain, "Name", item.Name, true);
                    string desc = ModSettingLocalizer.ResolveText(m_packageName, itemChain, "Description", item.Description, false);
                    object current = ModSettingsManager.GetValue(BuildPath(m_packageName, itemChain));
                    IModSettingItemWidget w = ModSettingItemWidgetFactory.Create(item, current, name, desc);
                    if (w == null) return null;
                    if (w is not Widget widget) {
                        if (!LanguageControl.TryGet(out string msg, fName, "4")) msg = "Setting '{0}' widget '{1}' does not derive from Widget, cannot render, skipped.";
                        Log.Error("[ModSettings] " + string.Format(msg, item.Id, w.GetType().Name));
                        return null;
                    }
                    string[] fullPath = BuildPath(m_packageName, itemChain);
                    w.ValueChanged = v => ModSettingsManager.Set(fullPath, v);
                    m_itemWidgets.Add(w);
                    return widget;
                }
            }
            return null;
        }

        static string[] AppendId(string[] ids, string id) {
            string[] result = new string[ids.Length + 1];
            ids.CopyTo(result, 0);
            result[ids.Length] = id;
            return result;
        }

        static string[] BuildPath(string packageName, string[] idChain) {
            string[] path = new string[idChain.Length + 1];
            path[0] = packageName;
            idChain.CopyTo(path, 1);
            return path;
        }

        static ModSettingPage FindPage(List<ModSettingPage> pages, string[] pageIds) {
            List<ModSettingPage> level = pages;
            ModSettingPage current = null;
            for (int i = 0; i < pageIds.Length; i++) {
                current = level?.Find(p => p.Id == pageIds[i]);
                if (current == null) return null;
                level = new List<ModSettingPage>();
                foreach (ModSettingElement el in current.Items)
                    if (el is ModSettingPage sub) level.Add(sub);
            }
            return current;
        }

        public override void Update() {
            base.Update();
            foreach (KeyValuePair<BevelledButtonWidget, (string PackageName, string[] PageIds, string Title)> nav in m_navButtons) {
                if (nav.Key.IsClicked) {
                    m_pageStack.Push(nav.Value);
                    NavigateCurrent();
                    return;
                }
            }
            // 共享 Description：激活项（如滑块滑动）显示其说明；一旦显示即保持，不再回归页面默认
            foreach (IModSettingItemWidget w in m_itemWidgets) {
                if (w.IsOperating) {
                    if (m_descriptionText != w.DescriptionText) {
                        m_descriptionText = w.DescriptionText;
                        m_descriptionLabel.Text = m_descriptionText;
                    }
                    break;
                }
            }

            if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                if (m_pageStack.Count > 0) {
                    m_pageStack.Pop();
                    NavigateCurrent();
                }
                else {
                    ScreensManager.GoBack();
                }
            }
        }
    }
}
