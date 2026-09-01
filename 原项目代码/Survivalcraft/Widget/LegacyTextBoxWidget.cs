using Engine;
using Engine.Graphics;
using Engine.Input;
using Engine.Media;

namespace Game {
    [Obsolete($"LegacyTextBoxWidget is obsolete, please use {nameof(LegacyTextBoxWidget)} instead.")]
    public class LegacyTextBoxWidget : Widget {
        public BitmapFont m_font;

        public string m_text = string.Empty;

        public int m_maximumLength = 512;

        public bool m_hasFocus;

        public int m_caretPosition;

        public double m_focusStartTime;

        public float m_scroll;

        public Vector2? m_size;

        public Vector2 Size {
            get {
                if (!m_size.HasValue) {
                    return Vector2.Zero;
                }
                return m_size.Value;
            }
            set => m_size = value;
        }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Text {
            get => m_text;
            set {
                string text = value.Length > MaximumLength ? value.Substring(0, MaximumLength) : value;
                if (text != m_text) {
                    m_text = text;
                    CaretPosition = CaretPosition;
                    TextChanged?.Invoke(this);
                }
            }
        }

        public int MaximumLength {
            get => m_maximumLength;
            set {
                m_maximumLength = MathUtils.Max(value, 0);
                if (Text.Length > m_maximumLength) {
                    Text = Text.Substring(0, m_maximumLength);
                }
            }
        }

        public bool OverwriteMode { get; set; }

        public bool HasFocus {
            get { return m_hasFocus; }
            set {
                if (value != m_hasFocus) {
                    m_hasFocus = value;
                    if (value) {
#if !ANDROID
                        if (m_hasFocus && Text == string.Empty) {
                            //清空之前的输入
                            KeyboardInput.GetInput();
                        }
#endif
                        CaretPosition = m_text.Length;
                        Keyboard.ShowKeyboard(
                            Title,
                            Description,
                            Text,
                            false,
                            delegate(string text) {
                                Text = text;
                                HasFocus = false;
                            },
                            null
                        );
                    }
                    else {
                        FocusLost?.Invoke(this);
                    }
                }
            }
        }

        public BitmapFont Font {
            get => m_font;
            set => m_font = value;
        }

        public float FontScale { get; set; }

        public Vector2 FontSpacing { get; set; }

        public Color Color { get; set; }

        public bool TextureLinearFilter { get; set; }

        public int CaretPosition {
            get => m_caretPosition;
            set {
                m_caretPosition = Math.Clamp(value, 0, Text.Length);
                m_focusStartTime = Time.RealTime;
            }
        }

        public event Action<LegacyTextBoxWidget> TextChanged;

        public event Action<LegacyTextBoxWidget> Enter;

        public event Action<LegacyTextBoxWidget> Escape;

        public event Action<LegacyTextBoxWidget> FocusLost;

        public bool MoveNextFlag;

        public bool JustOpened;

        public LegacyTextBoxWidget() {
            ClampToBounds = true;
            Color = Color.White;
            TextureLinearFilter = true;
            Font = ContentManager.Get<BitmapFont>("Fonts/Pericles");
            FontScale = 1f;
            Title = string.Empty;
            Description = string.Empty;
            JustOpened = true;
        }

        public override void Update() {
            if (HasFocus) {
#if ANDROID
                if (Input.LastChar.HasValue
                    && !Input.IsKeyDown(Key.Control)
                    && !char.IsControl(Input.LastChar.Value)) {
                    EnterText(new string(Input.LastChar.Value, 1));
                    Input.Clear();
                }
                if (Input.LastKey.HasValue) {
                    bool flag = false;
                    Key value = Input.LastKey.Value;
                    if (value == Key.V
                        && Input.IsKeyDown(Key.Control)) {
                        EnterText(ClipboardManager.ClipboardString);
                        flag = true;
                    }
                    else if (value == Key.BackSpace
                        && CaretPosition > 0) {
                        CaretPosition--;
                        Text = Text.Remove(CaretPosition, 1);
                        flag = true;
                    }
                    else {
                        switch (value) {
                            case Key.Delete:
                                if (CaretPosition < m_text.Length) {
                                    Text = Text.Remove(CaretPosition, 1);
                                    flag = true;
                                }
                                break;
                            case Key.LeftArrow:
                                CaretPosition--;
                                flag = true;
                                break;
                            case Key.RightArrow:
                                CaretPosition++;
                                flag = true;
                                break;
                            case Key.Home:
                                CaretPosition = 0;
                                flag = true;
                                break;
                            case Key.End:
                                CaretPosition = m_text.Length;
                                flag = true;
                                break;
                            case Key.Enter:
                                flag = true;
                                HasFocus = false;
                                Enter?.Invoke(this);
                                break;
                            case Key.Escape:
                                flag = true;
                                HasFocus = false;
                                Escape?.Invoke(this);
                                break;
                        }
                    }
                    if (flag) {
                        Input.Clear();
                    }
                }
#else
                //处理文字删除
                if (KeyboardInput.DeletePressed) {
                    if (CaretPosition != 0) {
                        CaretPosition--;
                        CaretPosition = Math.Max(0, CaretPosition);
                        if (Text.Length > 0) {
                            Text = Text.Remove(CaretPosition, 1);
                        }
                        float num = Font.CalculateCharacterPosition(Text, 0, new Vector2(FontScale), FontSpacing);
                        m_scroll = num - ActualSize.X;
                        m_scroll = MathUtils.Max(0, m_scroll);
                    }
                }
                //处理文字输入
                string inputString = KeyboardInput.GetInput();
                if (JustOpened) {
                    inputString = string.Empty;
                    JustOpened = false;
                }
                if (!string.IsNullOrEmpty(inputString)) {
                    EnterText(inputString);
                }
#endif
            }
            if (Input.Click.HasValue) {
                //处理电脑键盘输入时会处理成游戏输入
                HasFocus = HitTestGlobal(Input.Click.Value.Start) == this && HitTestGlobal(Input.Click.Value.End) == this;
            }
            if (HasFocus) { //处理复制粘贴事件
                if (Input.IsKeyDown(Key.Control)) {
                    if (Input.IsKeyDownOnce(Key.V)) {
                        Text += ClipboardManager.ClipboardString;
                    }
                    else if (Input.IsKeyDownOnce(Key.C)) {
                        ClipboardManager.ClipboardString = Text;
                    }
                    else if (Input.IsKeyDownOnce(Key.X)) {
                        ClipboardManager.ClipboardString = Text;
                        Text = string.Empty;
                    }
                }
                if (Input.IsKeyDownOnce(Key.Tab)) {
                    MoveNext(ScreensManager.CurrentScreen.Children);
                }
                if (Input.IsKeyDownRepeat(Key.LeftArrow)) {
                    CaretPosition = MathUtils.Max(0, --CaretPosition);
                }
                if (Input.IsKeyDownRepeat(Key.RightArrow)) {
                    CaretPosition = MathUtils.Min(Text.Length, ++CaretPosition);
                }
                if (Input.IsKeyDownRepeat(Key.UpArrow)) {
                    CaretPosition = 0;
                }
                if (Input.IsKeyDownRepeat(Key.DownArrow)) {
                    CaretPosition = Text.Length;
                }
                if (Input.IsKeyDownRepeat(Key.Enter)) {
                    Enter?.Invoke(this);
                }
                if (Input.IsKeyDownRepeat(Key.Escape)) {
                    Escape?.Invoke(this);
                }
            }
        }

        public void MoveNext(WidgetsList widgets) {
            foreach (Widget widget in widgets) {
                if (widget is TextBoxWidget textBox) {
                    if (!MoveNextFlag
                        && widget == this) {
                        MoveNextFlag = true;
                    }
                    else if (MoveNextFlag) {
                        textBox.HasFocus = true;
                        HasFocus = false;
                        MoveNextFlag = false;
                    }
                }
                if (widget is ContainerWidget container) {
                    MoveNext(container.Children);
                }
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = true;
            if (m_size.HasValue) {
                DesiredSize = m_size.Value;
                return;
            }
            DesiredSize = Font.MeasureText(Text.Length == 0 ? " " : Text, new Vector2(FontScale), FontSpacing);
            DesiredSize += new Vector2(1f * FontScale * Font.Scale, 0f);
        }

        public override void Draw(DrawContext dc) {
            Color color = Color * GlobalColorTransform;
            if (!string.IsNullOrEmpty(m_text)) {
                Vector2 position = new(0f - m_scroll, ActualSize.Y / 2f);
                SamplerState samplerState = TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp;
                FontBatch2D fontBatch2D = dc.PrimitivesRenderer2D.FontBatch(Font, 1, DepthStencilState.None, null, null, samplerState);
                int count = fontBatch2D.TriangleVertices.Count;
                fontBatch2D.QueueText(
                    Text,
                    position,
                    0f,
                    color,
                    TextAnchor.VerticalCenter,
                    new Vector2(FontScale),
                    FontSpacing
                );
                fontBatch2D.TransformTriangles(GlobalTransform, count);
            }
            if (!m_hasFocus
                || !(MathUtils.Remainder(Time.RealTime - m_focusStartTime, 0.5) < 0.25)) {
                return;
            }
            float caretPosition = Font.CalculateCharacterPosition(Text, CaretPosition, new Vector2(FontScale), FontSpacing);
            Vector2 caretCenter = new Vector2(0f, ActualSize.Y / 2f) + new Vector2(caretPosition - m_scroll, 0f);
            if (m_hasFocus) {
                if (caretCenter.X < 0f) {
                    m_scroll = MathUtils.Max(m_scroll + caretCenter.X, 0f);
                }
                if (caretCenter.X > ActualSize.X) {
                    m_scroll += caretCenter.X - ActualSize.X + 1f;
                }
            }
            FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.None);
            int count2 = flatBatch2D.TriangleVertices.Count;
            flatBatch2D.QueueQuad(
                caretCenter - new Vector2(0f, Font.GlyphHeight / 2f * FontScale * Font.Scale),
                caretCenter + new Vector2(1f, Font.GlyphHeight / 2f * FontScale * Font.Scale),
                0f,
                color
            );
            flatBatch2D.TransformTriangles(GlobalTransform, count2);
        }

        public void EnterText(string s) {
            if (OverwriteMode) {
                if (CaretPosition + s.Length <= MaximumLength) {
                    if (CaretPosition < m_text.Length) {
                        Text = Text.Remove(CaretPosition, s.Length).Insert(CaretPosition, s);
                    }
                    else {
                        Text = m_text + s;
                    }
                    CaretPosition += s.Length;
                }
            }
            else if (m_text.Length + s.Length <= MaximumLength) {
                if (CaretPosition < m_text.Length) {
                    Text = Text.Insert(CaretPosition, s);
                }
                else {
                    Text = m_text + s;
                }
                CaretPosition += s.Length;
            }
        }
    }
}