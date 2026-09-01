using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game {
    public class FontTextWidget : Widget {
        public string m_text = string.Empty;

        public TextOrientation m_textOrientation;

        public BitmapFont m_font;

        public Vector2 m_fontSpacing;

        public float m_fontScale;

        public int m_maxLines = int.MaxValue;

        public bool m_wordWrap;

        public bool m_ellipsis;

        public List<string> m_lines = [];

        public Vector2? m_linesSize;

        public float? m_linesAvailableWidth;

        public float? m_linesAvailableHeight;

        public Vector2 Size { get; set; }

        public virtual string Text {
            get => m_text;
            set {
                if (m_text != value
                    && value != null) {
                    m_text = value;
                    m_linesSize = null;
                }
            }
        }

        public TextAnchor TextAnchor { get; set; }

        public TextOrientation TextOrientation {
            get => m_textOrientation;
            set {
                if (value != m_textOrientation) {
                    m_textOrientation = value;
                    m_linesSize = null;
                }
            }
        }

        public BitmapFont Font {
            get => m_font ?? (LabelWidget.BitmapFont ?? BitmapFont.DebugFont);
            set {
                if (value != m_font) {
                    m_font = value;
                    m_linesSize = null;
                }
            }
        }

        public float FontScale {
            get => m_fontScale;
            set {
                if (value != m_fontScale) {
                    m_fontScale = value;
                    m_linesSize = null;
                }
            }
        }

        public Vector2 FontSpacing {
            get => m_fontSpacing;
            set {
                if (value != m_fontSpacing) {
                    m_fontSpacing = value;
                    m_linesSize = null;
                }
            }
        }

        public bool WordWrap {
            get => m_wordWrap;
            set {
                if (value != m_wordWrap) {
                    m_wordWrap = value;
                    m_linesSize = null;
                }
            }
        }

        public bool Ellipsis {
            get => m_ellipsis;
            set {
                if (value != m_ellipsis) {
                    m_ellipsis = value;
                    m_linesSize = null;
                }
            }
        }

        public int MaxLines {
            get => m_maxLines;
            set {
                if (value != m_maxLines) {
                    m_maxLines = value;
                    m_linesSize = null;
                }
            }
        }

        public Color Color { get; set; }

        public bool DropShadow { get; set; }

        public bool TextureLinearFilter { get; set; }
        public bool m_usedebug;

        public FontTextWidget() {
            IsHitTestVisible = false;
            FontScale = 1f;
            Color = Color.White;
            TextureLinearFilter = true;
            Size = new Vector2(-1f);
        }

        public override void Draw(DrawContext dc) {
            if (!string.IsNullOrEmpty(Text)
                && Color.A != 0) {
                SamplerState samplerState = TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp;
                FontBatch2D fontBatch2D = dc.PrimitivesRenderer2D.FontBatch(Font, 1, DepthStencilState.None, null, null, samplerState);
                int count = fontBatch2D.TriangleVertices.Count;
                float num = 0f;
                if ((TextAnchor & TextAnchor.VerticalCenter) != 0) {
                    float num2 = Font.GlyphHeight * FontScale * Font.Scale
                        + (m_lines.Count - 1) * ((Font.GlyphHeight + Font.Spacing.Y) * FontScale * Font.Scale + FontSpacing.Y);
                    num = (ActualSize.Y - num2) / 2f;
                }
                else if ((TextAnchor & TextAnchor.Bottom) != 0) {
                    float num3 = Font.GlyphHeight * FontScale * Font.Scale
                        + (m_lines.Count - 1) * ((Font.GlyphHeight + Font.Spacing.Y) * FontScale * Font.Scale + FontSpacing.Y);
                    num = ActualSize.Y - num3;
                }
                TextAnchor anchor = TextAnchor & ~(TextAnchor.VerticalCenter | TextAnchor.Bottom);
                Color color = Color * GlobalColorTransform;
                float num4 = CalculateLineHeight();
                foreach (string line in m_lines) {
                    float x = 0f;
                    if ((TextAnchor & TextAnchor.HorizontalCenter) != 0) {
                        x = ActualSize.X / 2f;
                    }
                    else if ((TextAnchor & TextAnchor.Right) != 0) {
                        x = ActualSize.X;
                    }
                    //bool flag = true;
                    Vector2 vector = Vector2.Zero;
                    float angle = 0f;
                    if (TextOrientation == TextOrientation.Horizontal) {
                        vector = new Vector2(x, num);
                        angle = 0f;
                        _ = Display.ScissorRectangle;
                        //flag = true;
                    }
                    else if (TextOrientation == TextOrientation.VerticalLeft) {
                        vector = new Vector2(x, ActualSize.Y + num);
                        angle = MathUtils.DegToRad(-90f);
                        //flag = true;
                    }
                    //if (flag)
                    //{
                    if (DropShadow) {
                        fontBatch2D.QueueText(
                            line,
                            vector + 1f * new Vector2(FontScale),
                            0f,
                            new Color((byte)0, (byte)0, (byte)0, color.A),
                            anchor,
                            new Vector2(FontScale),
                            FontSpacing,
                            angle
                        );
                    }
                    fontBatch2D.QueueText(
                        line,
                        vector,
                        0f,
                        color,
                        anchor,
                        new Vector2(FontScale),
                        FontSpacing,
                        angle
                    );
                    //}
                    num += num4;
                }
                fontBatch2D.TransformTriangles(GlobalTransform, count);
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = !string.IsNullOrEmpty(Text) && Color.A != 0;
            if (TextOrientation == TextOrientation.Horizontal) {
                UpdateLines(parentAvailableSize.X, parentAvailableSize.Y);
                if (!m_linesSize.HasValue) {
                    return;
                }
                DesiredSize = new Vector2(Size.X < 0f ? m_linesSize.Value.X : Size.X, Size.Y < 0f ? m_linesSize.Value.Y : Size.Y);
            }
            else if (TextOrientation == TextOrientation.VerticalLeft) {
                UpdateLines(parentAvailableSize.Y, parentAvailableSize.X);
                if (!m_linesSize.HasValue) {
                    return;
                }
                DesiredSize = new Vector2(Size.X < 0f ? m_linesSize.Value.Y : Size.X, Size.Y < 0f ? m_linesSize.Value.X : Size.Y);
            }
        }

        public float CalculateLineHeight() => (Font.GlyphHeight + Font.Spacing.Y + FontSpacing.Y) * FontScale * Font.Scale;

        // 在 text 的前 fitCount 个字符超出可用宽度时，寻找合适的换行位置（返回值为换行点，即下一行首字符下标）。
        // 拉丁文本按原有规则回退到最近的空白或标点处；CJK 文本字间即可断行，并遵守行首/行尾禁则
        public static int FindLineBreakPosition(string text, int fitCount) {
            if (CanBreakBeforeCjk(text, fitCount)) {
                return fitCount;
            }
            int index = fitCount - 2;
            while (index >= 0
                && !char.IsWhiteSpace(text[index])
                && !char.IsPunctuation(text[index])
                && !CanBreakBeforeCjk(text, index + 1)) {
                index--;
            }
            return index < 0 ? fitCount - 1 : index + 1;
        }

        // 判断是否可以在 text 的 position 处（下一行首字符下标）断行：下一行首字符须为 CJK 字符，
        // 且不是行首禁则标点（如 ，。」…），其前一字符也不是行尾禁则标点（如 （「“）
        public static bool CanBreakBeforeCjk(string text, int position) {
            if (position <= 0 || position >= text.Length) {
                return false;
            }
            char next = text[position];
            char previous = text[position - 1];
            return IsCjkChar(next)
                && !IsCjkForbiddenLineStart(next)
                && !IsCjkForbiddenLineEnd(previous);
        }

        public static bool IsCjkChar(char c) => c is >= '\u2e80' and <= '\u9fff' // 部首、假名、注音、CJK 统一表意文字
            or >= '\uac00' and <= '\ud7af' // 谚文
            or >= '\uf900' and <= '\ufaff'; // CJK 兼容表意文字

        public static bool IsCjkForbiddenLineStart(char c) => "、。，．：；？！〉》」』】〕）｝ー・»”’‥…,:;!?)]}".Contains(c);

        public static bool IsCjkForbiddenLineEnd(char c) => "（《「『〈【〔｛“‘([{".Contains(c);

        public virtual void UpdateLines(float availableWidth, float availableHeight) {
            if (m_linesAvailableHeight.HasValue
                && m_linesAvailableHeight == availableHeight
                && m_linesAvailableWidth.HasValue
                && m_linesSize.HasValue) {
                float num = MathUtils.Min(m_linesSize.Value.X, m_linesAvailableWidth.Value) - 0.1f;
                float num2 = MathUtils.Max(m_linesSize.Value.X, m_linesAvailableWidth.Value) + 0.1f;
                if (availableWidth >= num
                    && availableWidth <= num2) {
                    return;
                }
            }
            availableWidth += 0.1f;
            m_lines.Clear();
            string[] array = (Text ?? string.Empty).Split(["\n"], StringSplitOptions.None);
            string text = "...";
            float x = Font.MeasureText(text, new Vector2(FontScale), FontSpacing).X;
            if (WordWrap) {
                int num3 = (int)MathUtils.Min(MathF.Floor(availableHeight / CalculateLineHeight()), MaxLines);
                for (int i = 0; i < array.Length; i++) {
                    string text2 = array[i].TrimEnd();
                    if (text2.Length == 0) {
                        m_lines.Add(string.Empty);
                        continue;
                    }
                    while (text2.Length > 0) {
                        bool flag;
                        int num4;
                        if (Ellipsis && m_lines.Count + 1 >= num3) {
                            num4 = Font.FitText(MathUtils.Max(availableWidth - x, 0f), text2, 0, text2.Length, FontScale, FontSpacing.X);
                            flag = true;
                        }
                        else {
                            num4 = Font.FitText(availableWidth, text2, 0, text2.Length, FontScale, FontSpacing.X);
                            num4 = MathUtils.Max(num4, 1);
                            flag = false;
                            if (num4 < text2.Length) {
                                num4 = FindLineBreakPosition(text2, num4);
                            }
                        }
                        string text3;
                        if (num4 == text2.Length) {
                            text3 = text2;
                            text2 = string.Empty;
                        }
                        else {
                            text3 = text2.Substring(0, num4).TrimEnd();
                            if (flag) {
                                text3 += text;
                            }
                            text2 = text2.Substring(num4, text2.Length - num4).TrimStart();
                        }
                        m_lines.Add(text3);
                        if (flag) {
                            break;
                        }
                    }
                }
            }
            else if (Ellipsis) {
                for (int j = 0; j < array.Length; j++) {
                    string text4 = array[j].TrimEnd();
                    int num7 = Font.FitText(MathUtils.Max(availableWidth - x, 0f), text4, 0, text4.Length, FontScale, FontSpacing.X);
                    if (num7 < text4.Length) {
                        m_lines.Add(text4.Substring(0, num7).TrimEnd() + text);
                    }
                    else {
                        m_lines.Add(text4);
                    }
                }
            }
            else {
                m_lines.AddRange(array);
            }
            if (m_lines.Count > MaxLines) {
                m_lines = m_lines.Take(MaxLines).ToList();
            }
            Vector2 zero = Vector2.Zero;
            for (int k = 0; k < m_lines.Count; k++) {
                Vector2 vector = Font.MeasureText(m_lines[k], new Vector2(FontScale), FontSpacing);
                zero.X = MathUtils.Max(zero.X, vector.X);
                if (k < m_lines.Count - 1) {
                    zero.Y += (Font.GlyphHeight + Font.Spacing.Y + FontSpacing.Y) * FontScale * Font.Scale;
                }
                else {
                    zero.Y += Font.GlyphHeight * FontScale * Font.Scale;
                }
            }
            m_linesSize = zero;
            m_linesAvailableWidth = availableWidth;
            m_linesAvailableHeight = availableHeight;
        }
    }
}