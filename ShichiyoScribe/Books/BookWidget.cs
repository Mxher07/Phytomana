using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game {
    /// <summary>
    ///     双页书阅读器。整本书始终以"左右双页"呈现：
    ///     起始页跨页 → 目录页(左文字/右分类) → 分类页(左介绍/右子目录) → 子目录页(右条目) → 条目内容页(左+右)。
    ///     条目/起始页的内容来自 Assets/ShichiyoScribe/ 下由 .vlb 引索的 .xml 控件树。
    /// </summary>
    public class BookWidget : CanvasWidget {
        public const float BookWidth = 1000f;
        public const float BookHeight = 552f;

        public const float PaperWidth = 468f;
        public const float PaperHeight = 442f;
        public const float LeftPaperX = 22f;
        public const float RightPaperX = 510f;
        public const float PaperY = 56f;

        public const float ContentWidth = 444f;
        public const float ContentHeight = 408f;
        public const float LeftContentX = 34f;
        public const float RightContentX = 522f;
        public const float ContentY = 74f;

        public Color TitleColor = new(100, 66, 34);
        public Color TextColor = new(54, 46, 36);
        public Color HintColor = new(130, 108, 78);

        public enum ReadState {
            Start,
            Contents,
            Section,
            SubSection,
            Entry
        }

        public BookDefinition m_definition;
        public ComponentPlayer m_player;

        public CanvasWidget m_leftContent;
        public CanvasWidget m_rightContent;
        public LabelWidget m_bookTitleLabel;
        public LabelWidget m_pageLabel;
        public BevelledButtonWidget m_backButton;
        public BevelledButtonWidget m_nextButton;
        public BevelledButtonWidget m_contentsButton;
        public BevelledButtonWidget m_closeButton;

        public ReadState m_state;
        public int m_startIndex;
        public int m_sectionIndex;
        public int m_subSectionIndex;
        public int m_entryIndex;

        public readonly List<(ButtonWidget button, Action action)> m_links = [];

        float m_scale = 1f;

        public BookWidget(BookDefinition definition, ComponentPlayer player) {
            m_definition = definition ?? throw new ArgumentNullException(nameof(definition));
            m_player = player ?? throw new ArgumentNullException(nameof(player));
            Size = new Vector2(BookWidth, BookHeight);
            IsHitTestVisible = true;
            HorizontalAlignment = WidgetAlignment.Center;
            VerticalAlignment = WidgetAlignment.Center;
            BuildChrome();
            m_state = definition.StartPages.Count > 0 ? ReadState.Start : ReadState.Contents;
            Navigate();
        }

        void BuildChrome() {
            BevelledRectangleWidget cover = new() {
                Size = new Vector2(BookWidth, BookHeight),
                CenterColor = new Color(181, 172, 154),
                BevelColor = new Color(181, 172, 154),
                BevelSize = 3f,
                RoundingRadius = 6f,
                IsHitTestVisible = true
            };
            Children.Add(cover);

            BevelledRectangleWidget spine = new() {
                Size = new Vector2(20f, PaperHeight + 16f),
                BevelColor = new Color(136, 127, 113),
                CenterColor = new Color(158, 149, 135),
                BevelSize = 3f,
                IsHitTestVisible = false
            };
            Children.Add(spine);
            SetWidgetPosition(spine, new Vector2(490f, PaperY - 8f));

            BevelledRectangleWidget leftPaper = CreatePaper();
            Children.Add(leftPaper);
            SetWidgetPosition(leftPaper, new Vector2(LeftPaperX, PaperY));

            BevelledRectangleWidget rightPaper = CreatePaper();
            Children.Add(rightPaper);
            SetWidgetPosition(rightPaper, new Vector2(RightPaperX, PaperY));

            m_leftContent = CreateContentCanvas();
            Children.Add(m_leftContent);
            SetWidgetPosition(m_leftContent, new Vector2(LeftContentX, ContentY));

            m_rightContent = CreateContentCanvas();
            Children.Add(m_rightContent);
            SetWidgetPosition(m_rightContent, new Vector2(RightContentX, ContentY));

            m_bookTitleLabel = new LabelWidget {
                Text = m_definition.Title ?? string.Empty,
                Size = new Vector2(BookWidth, 28f),
                FontScale = 1f,
                Color = new Color(255, 255, 255, 192),
                DropShadow = true,
                TextAnchor = TextAnchor.HorizontalCenter,
                HorizontalAlignment = WidgetAlignment.Center
            };
            Children.Add(m_bookTitleLabel);
            SetWidgetPosition(m_bookTitleLabel, new Vector2(0f, 18f));

            m_pageLabel = new LabelWidget {
                Text = string.Empty,
                Size = new Vector2(280f, 28f),
                FontScale = 0.62f,
                Color = HintColor
            };
            Children.Add(m_pageLabel);
            SetWidgetPosition(m_pageLabel, new Vector2(40f, 510f));

            m_backButton = BuildNavButton("上一页", 668f);
            m_nextButton = BuildNavButton("下一页", 752f);
            m_contentsButton = BuildNavButton("目录", 836f);
            m_closeButton = BuildNavButton("关闭", 920f);
        }

        BevelledRectangleWidget CreatePaper() {
            return new BevelledRectangleWidget {
                Size = new Vector2(PaperWidth, PaperHeight),
                CenterColor = new Color(222, 214, 200),
                BevelColor = new Color(160, 151, 137),
                BevelSize = 3f,
                RoundingRadius = 4f,
                IsHitTestVisible = false
            };
        }

        CanvasWidget CreateContentCanvas() {
            return new CanvasWidget {
                Size = new Vector2(ContentWidth, ContentHeight),
                IsHitTestVisible = true
            };
        }

        BevelledButtonWidget BuildNavButton(string text, float x) {
            BevelledButtonWidget button = new() {
                Text = text,
                Size = new Vector2(80f, 40f),
                FontScale = 0.7f,
                Color = new Color(255, 255, 255, 235)
            };
            Children.Add(button);
            SetWidgetPosition(button, new Vector2(x, 502f));
            return button;
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            float scale = ComputeAdaptiveScale(parentAvailableSize);
            if (Math.Abs(scale - m_scale) > 0.001f) {
                m_scale = scale;
                LayoutTransform = Matrix.CreateScale(scale);
            }
            base.MeasureOverride(parentAvailableSize);
        }

        /// <summary>
        ///     根据当前逻辑屏幕可用空间自动缩放整本书，使其永远完整显示在屏幕内。
        ///     逻辑像素 = 物理像素 × FinalUiScale（见 ScreensManager.LayoutAndDrawWidgets）。
        /// </summary>
        float ComputeAdaptiveScale(Vector2 parentAvailableSize) {
            Vector2 available = parentAvailableSize;
            if (!float.IsFinite(available.X)
                || available.X <= 0f
                || !float.IsFinite(available.Y)
                || available.Y <= 0f) {
                float uiScale = ScreensManager.FinalUiScale;
                if (uiScale > 0f) {
                    available = new Vector2(Display.Viewport.Width * uiScale, Display.Viewport.Height * uiScale);
                }
                else {
                    return 1f;
                }
            }
            const float margin = 32f;
            float scale = 1f;
            if (available.X > 0f) {
                scale = MathUtils.Min(scale, MathUtils.Max(0f, (available.X - margin) / BookWidth));
            }
            if (available.Y > 0f) {
                scale = MathUtils.Min(scale, MathUtils.Max(0f, (available.Y - margin) / BookHeight));
            }
            return Math.Clamp(scale, 0.4f, 1f);
        }

        public override void Update() {
            if (m_backButton.IsClicked) {
                GoBack();
            }
            if (m_nextButton.IsClicked) {
                GoNext();
            }
            if (m_contentsButton.IsClicked) {
                m_state = ReadState.Contents;
                Navigate();
            }
            if (m_closeButton.IsClicked && m_player != null) {
                m_player.ComponentGui.ModalPanelWidget = null;
                return;
            }
            foreach ((ButtonWidget button, Action action) in m_links) {
                if (button.IsClicked) {
                    action();
                    break;
                }
            }
        }

        void GoBack() {
            switch (m_state) {
                case ReadState.Start:
                    if (m_startIndex > 0) {
                        m_startIndex--;
                        Navigate();
                    }
                    break;
                case ReadState.Contents:
                    if (m_definition.StartPages.Count > 0) {
                        m_startIndex = m_definition.StartPages.Count - 1;
                        m_state = ReadState.Start;
                        Navigate();
                    }
                    break;
                case ReadState.Section:
                    m_state = ReadState.Contents;
                    Navigate();
                    break;
                case ReadState.SubSection:
                    m_state = ReadState.Section;
                    Navigate();
                    break;
                case ReadState.Entry:
                    m_state = ReadState.SubSection;
                    Navigate();
                    break;
            }
        }

        void GoNext() {
            if (m_state == ReadState.Start) {
                if (m_startIndex + 1 < m_definition.StartPages.Count) {
                    m_startIndex++;
                    Navigate();
                }
                else {
                    m_state = ReadState.Contents;
                    Navigate();
                }
            }
        }

        void Navigate() {
            m_links.Clear();
            m_leftContent.Children.Clear();
            m_rightContent.Children.Clear();
            switch (m_state) {
                case ReadState.Start:
                    BuildStartPage();
                    break;
                case ReadState.Contents:
                    BuildContentsPage();
                    break;
                case ReadState.Section:
                    BuildSectionPage();
                    break;
                case ReadState.SubSection:
                    BuildSubSectionPage();
                    break;
                case ReadState.Entry:
                    BuildEntryPage();
                    break;
            }
            m_pageLabel.Text = string.Format(LanguageControl.Get("ShichiyoScribeWidget", 0), ComputePage(), ComputeTotalPages());
        }

        int ComputeTotalPages() {
            int total = m_definition.StartPages.Count * 2 + 2;
            foreach (BookSection section in m_definition.Sections) {
                total += 2;
                foreach (BookSubSection sub in section.SubSections) {
                    total += 2;
                    if (sub.Entries.Count > 0) {
                        total += sub.Entries.Count * 2;
                    }
                }
            }
            return total;
        }

        int ComputePage() {
            int page = 0;
            List<BookSection> sections = m_definition.Sections;
            switch (m_state) {
                case ReadState.Start:
                    return m_startIndex * 2 + 2;
                case ReadState.Contents:
                    return m_definition.StartPages.Count * 2 + 2;
                case ReadState.Section:
                    page = m_definition.StartPages.Count * 2 + 2;
                    for (int i = 0; i < m_sectionIndex; i++) {
                        page += SectionPageCount(sections[i]);
                    }
                    return page + 2;
                case ReadState.SubSection:
                    page = m_definition.StartPages.Count * 2 + 2;
                    for (int i = 0; i < m_sectionIndex; i++) {
                        page += SectionPageCount(sections[i]);
                    }
                    page += 2;
                    for (int j = 0; j < m_subSectionIndex; j++) {
                        page += SubSectionPageCount(sections[m_sectionIndex].SubSections[j]);
                    }
                    return page + 2;
                case ReadState.Entry:
                    page = m_definition.StartPages.Count * 2 + 2;
                    for (int i = 0; i < m_sectionIndex; i++) {
                        page += SectionPageCount(sections[i]);
                    }
                    page += 2;
                    for (int j = 0; j < m_subSectionIndex; j++) {
                        page += SubSectionPageCount(sections[m_sectionIndex].SubSections[j]);
                    }
                    page += 2;
                    page += m_entryIndex * 2;
                    return page + 2;
            }
            return 1;
        }

        static int SectionPageCount(BookSection section) {
            int count = 2;
            foreach (BookSubSection sub in section.SubSections) {
                count += SubSectionPageCount(sub);
            }
            return count;
        }

        static int SubSectionPageCount(BookSubSection sub) {
            int count = 2;
            if (sub.Entries.Count > 0) {
                count += sub.Entries.Count * 2;
            }
            return count;
        }

        void BuildStartPage() {
            BookSpread spread = m_definition.StartPages[m_startIndex];
            AddPageTitle(m_leftContent, spread.Title ?? m_definition.Title);
            AddPageTitle(m_rightContent, spread.Title ?? m_definition.Title);
            LoadPage(m_leftContent, spread.Left, spread.LeftText);
            LoadPage(m_rightContent, spread.Right, spread.RightText);
        }

        void BuildContentsPage() {
            AddPageTitle(m_leftContent, LanguageControl.Get("ShichiyoScribeWidget", 1));
            AddPageTitle(m_rightContent, LanguageControl.Get("ShichiyoScribeWidget", 2));
            if (!string.IsNullOrEmpty(m_definition.TocLeft)) {
                LoadPage(m_leftContent, m_definition.TocLeft, null);
            }
            else {
                AddText(m_leftContent, m_definition.TocText ?? string.Empty, 30f);
            }
            float y = 36f;
            for (int i = 0; i < m_definition.Sections.Count && i < 6; i++) {
                BookSection section = m_definition.Sections[i];
                int index = i;
                AddLinkButton(m_rightContent, $"◆  {index + 1}. {section.Title}", new Vector2(0f, y), () => {
                    m_sectionIndex = index;
                    m_state = ReadState.Section;
                    Navigate();
                });
                y += 66f;
            }
        }

        void BuildSectionPage() {
            BookSection section = m_definition.Sections[m_sectionIndex];
            AddPageTitle(m_leftContent, section.Title ?? string.Empty);
            AddPageTitle(m_rightContent, LanguageControl.Get("ShichiyoScribeWidget", 3));
            if (!string.IsNullOrEmpty(section.Left)) {
                LoadPage(m_leftContent, section.Left, null);
            }
            else if (!string.IsNullOrEmpty(section.Text)) {
                AddText(m_leftContent, section.Text, 30f);
            }
            else {
                AddText(m_leftContent, LanguageControl.Get("ShichiyoScribeWidget", 10), 30f);
            }
            float y = 36f;
            int j = 0;
            for (; j < section.SubSections.Count && j < 5; j++) {
                BookSubSection sub = section.SubSections[j];
                int subIndex = j;
                AddLinkButton(m_rightContent, $"▸  {subIndex + 1}. {sub.Title}", new Vector2(0f, y), () => {
                    m_subSectionIndex = subIndex;
                    m_state = ReadState.SubSection;
                    Navigate();
                });
                y += 66f;
            }
            if (j == 0) {
                AddText(m_rightContent, LanguageControl.Get("ShichiyoScribeWidget", 5), 40f);
            }
        }

        void BuildSubSectionPage() {
            BookSubSection sub = m_definition.Sections[m_sectionIndex].SubSections[m_subSectionIndex];
            AddPageTitle(m_leftContent, sub.Title ?? string.Empty);
            AddPageTitle(m_rightContent, LanguageControl.Get("ShichiyoScribeWidget", 6));
            if (!string.IsNullOrEmpty(sub.Left)) {
                LoadPage(m_leftContent, sub.Left, null);
            }
            else if (!string.IsNullOrEmpty(sub.Text)) {
                AddText(m_leftContent, sub.Text, 30f);
            }
            else {
                AddText(m_leftContent, LanguageControl.Get("ShichiyoScribeWidget", 10), 30f);
            }
            float y = 36f;
            int k = 0;
            for (; k < sub.Entries.Count && k < 5; k++) {
                BookEntry entry = sub.Entries[k];
                int entryIndex = k;
                AddLinkButton(m_rightContent, $"•  {entryIndex + 1}. {entry.Title}", new Vector2(0f, y), () => {
                    m_entryIndex = entryIndex;
                    m_state = ReadState.Entry;
                    Navigate();
                });
                y += 66f;
            }
            if (k == 0) {
                AddText(m_rightContent, LanguageControl.Get("ShichiyoScribeWidget", 5), 40f);
            }
        }

        void BuildEntryPage() {
            BookEntry entry = m_definition.Sections[m_sectionIndex].SubSections[m_subSectionIndex].Entries[m_entryIndex];
            AddPageTitle(m_leftContent, entry.Title ?? string.Empty);
            AddPageTitle(m_rightContent, LanguageControl.Get("ShichiyoScribeWidget", 7));
            if (!string.IsNullOrEmpty(entry.Left)) {
                LoadPage(m_leftContent, entry.Left, null);
            }
            else if (!string.IsNullOrEmpty(entry.Text)) {
                AddText(m_leftContent, entry.Text, 30f);
            }
            else {
                AddText(m_leftContent, LanguageControl.Get("ShichiyoScribeWidget", 10), 30f);
            }
            if (!string.IsNullOrEmpty(entry.Right)) {
                LoadPage(m_rightContent, entry.Right, null);
            }
            else {
                AddText(m_rightContent, LanguageControl.Get("ShichiyoScribeWidget", 8), 100f);
                int blockIndex = OlderBookManager.GetBlockIndex(m_definition.BlockName);
                if (blockIndex >= 0) {
                    BlockIconWidget icon = new() {
                        Size = new Vector2(96f, 96f),
                        Scale = 1.6f,
                        Contents = blockIndex
                    };
                    m_rightContent.Children.Add(icon);
                    m_rightContent.SetWidgetPosition(icon, new Vector2((ContentWidth - 96f) / 2f, 220f));
                }
            }
        }

        void AddPageTitle(CanvasWidget canvas, string text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            LabelWidget title = new() {
                Text = text,
                Size = new Vector2(ContentWidth, 26f),
                FontScale = 1f,
                Color = TitleColor,
                WordWrap = true
            };
            canvas.Children.Add(title);
            canvas.SetWidgetPosition(title, Vector2.Zero);
        }

        void AddText(CanvasWidget canvas, string text, float y) {
            LabelWidget label = new() {
                Text = text ?? string.Empty,
                Size = new Vector2(ContentWidth, -1f),
                FontScale = 0.8f,
                Color = TextColor,
                WordWrap = true,
                TextAnchor = TextAnchor.HorizontalCenter
            };
            canvas.Children.Add(label);
            canvas.SetWidgetPosition(label, new Vector2(0f, y));
        }

        void LoadPage(CanvasWidget canvas, string path, string fallbackText) {
            if (string.IsNullOrEmpty(path)) {
                if (!string.IsNullOrEmpty(fallbackText)) {
                    AddText(canvas, fallbackText, 30f);
                }
                return;
            }
            string contentPath = BookDefinition.NormalizeContentPath(path);
            try {
                XElement node = ContentManager.Get<XElement>(contentPath);
                if (node == null) {
                    throw new InvalidOperationException($"page content '{contentPath}' not found.");
                }
                Widget.LoadWidget(null, node, canvas);
            }
            catch (Exception e) {
                Log.Error($"[ShichiyoScribe] Load page '{contentPath}' failed: {e}");
                AddText(canvas, string.Format(LanguageControl.Get("ShichiyoScribeWidget", 9), contentPath), 60f);
            }
        }

        void AddLinkButton(CanvasWidget canvas, string text, Vector2 position, Action action) {
            BevelledButtonWidget button = new() {
                Text = text,
                Size = new Vector2(ContentWidth, 56f),
                FontScale = 0.78f,
                Color = new Color(255, 255, 255, 235)
            };
            canvas.Children.Add(button);
            canvas.SetWidgetPosition(button, position);
            m_links.Add((button, action));
        }
    }
}