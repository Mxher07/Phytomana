using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Engine;

namespace Game {
    /// <summary>
    /// 设置项 Widget 契约。模组可自行实现（任意 Widget 基类 + 本接口）以自由组装 UI，
    /// 也可继承内置 UniformSpacingPanelSettingWidget 复用 nameLabel/Assemble/CommitValue。
    /// </summary>
    /// <remarks>实现约束（接口无法表达，须遵守）：① 实现类型须为 Engine.Widget 子类（Screen 据此渲染）；
    /// ② 须有无参构造（Factory 用 Activator.CreateInstance 反射实例化）；
    /// ③ Initialize 由 Factory 实例化后调用一次，不应假设可重入——重复调用会重复建控件。</remarks>
    public interface IModSettingItemWidget {
        public ModSettingItem Descriptor { get; }
        public object Value { get; }
        public string DescriptionText { get; set; }
        public Action<object> ValueChanged { get; set; }

        /// <summary>Factory 反射实例化（无参构造）后调用：注入描述符、当前值、名称与描述，子类在此建控件 + Assemble。仅调用一次。</summary>
        public void Initialize(ModSettingItem descriptor, object currentValue, string name, string description);

        /// <summary>子类判定是否支持某值类型（enum Widget 用 typeof(Enum).IsAssignableFrom）。</summary>
        public bool Supports(Type type);

        /// <summary>页面轮询识别激活项（如滑块滑动中），用于更新共享 Description。默认 false。</summary>
        public bool IsOperating { get; set; }

        /// <summary>外部（非用户操作）推送新值时由宿主调用：把新值同步到内部控件显示。默认空实现；内置 widget 经 UniformSpacingPanelSettingWidget 重写，第三方 widget 可按需重写以参与自动刷新。</summary>
        public void ApplyExternalValue(object newValue) { }
    }

    /// <summary>
    /// 一个设置项 = 一行 UI（标题 + 值控件）。描述走页面共享 label。
    /// 纯代码组装，轮询 Update 检测交互。子类负责 Value↔控件互转 + 检测交互 + 触发 ValueChanged。
    /// </summary>
    // 继承 UniformSpacingPanelWidget（fill 容器）：主轴 DesiredSize=Infinity，由父 arrange 给固定 ActualSize 后按子数均分。
    // 选 Horizontal 使主轴=X、Y 轴 desired finite，避免污染上层 ContentStack(StackPanel Vertical) 经 layoutTransform 算 NaN。
    public abstract class UniformSpacingPanelSettingWidget : UniformSpacingPanelWidget, IModSettingItemWidget {
        public ModSettingItem Descriptor { get; private set; }
        public object Value { get; protected set; }
        public string DescriptionText { get; set; }
        public Action<object> ValueChanged { get; set; }

        protected bool m_applyingExternalValue;

        protected LabelWidget m_nameLabel;
        string m_nameLabelText;

        protected UniformSpacingPanelSettingWidget() {
            Direction = LayoutDirection.Horizontal;
            Margin = new Vector2(0, 3);
        }

        /// <summary>设描述符、当前值、名称与描述。子类 override 时先 base.Initialize（name 已存 m_nameLabelText 供 Assemble 取），再建控件 + Assemble。Value 类型已由 Factory 经 Supports 校验，子类可按 descriptor.Type 安全解引用。</summary>
        public virtual void Initialize(ModSettingItem descriptor, object currentValue, string name, string description) {
            Descriptor = descriptor;
            Value = currentValue;
            NameLabelText = name;
            DescriptionText = description;
        }

        /// <summary>Factory 实例化后设值，触发 nameLabel 更新。</summary>
        public string NameLabelText {
            get => m_nameLabelText;
            set {
                m_nameLabelText = value;
                if (m_nameLabel != null) m_nameLabel.Text = value;
            }
        }

        public abstract bool Supports(Type type);

        public bool IsOperating { get; set; }

        /// <summary>子类构造时调用：建 nameLabel + 值控件，水平排进自身（已是 Horizontal StackPanel）。</summary>
        protected void Assemble(Widget valueWidget) {
            m_nameLabel = new LabelWidget {
                Text = m_nameLabelText,
                HorizontalAlignment = WidgetAlignment.Far,
                VerticalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(20, 0)
            };
            Children.Add(m_nameLabel);
            if (valueWidget != null) Children.Add(valueWidget);
        }

        /// <summary>子类检测到值变化时调用：更新 Value 并触发 ValueChanged（→ Manager.Set）。外部推值期间（m_applyingExternalValue）跳过回写，防 widget→Set→event→同 widget 循环。第三方重写本方法时须同样检查 m_applyingExternalValue，否则外部推值会被回写成循环。</summary>
        protected virtual void CommitValue(object newValue) {
            Value = newValue;
            if (!m_applyingExternalValue) ValueChanged?.Invoke(newValue);
        }

        /// <summary>宿主收到 SettingChanged 后调入口：设守卫 → 写 Value（不回写 Manager）→ 同步控件 → 清守卫。子类不重写本方法，重写 ApplyToControl。</summary>
        public virtual void ApplyExternalValue(object newValue) => ApplyExternalValueCore(newValue);

        protected void ApplyExternalValueCore(object newValue) {
            // 入口处类型预检（Supports 契约）：脏值静默忽略，不污染 Value（否则 TextBox 下帧 Update 会误触发回写，BoolButton 下次点击会 InvalidCastException）。
            if (newValue != null && !Supports(newValue.GetType())) return;
            m_applyingExternalValue = true;
            try {
                Value = newValue;
                ApplyToControl(newValue);
            }
            finally {
                m_applyingExternalValue = false;
            }
        }

        /// <summary>把新值同步到内部控件显示。基类默认空；各子类重写以写自己的控件（按 Supports 已校验类型解引用，类型不符静默忽略）。</summary>
        protected virtual void ApplyToControl(object newValue) { }
    }

    // ===== 内置子类 =====

    /// <summary>bool 开关按钮。Text = Value ? TextTrue : TextFalse（默认 Yes/No，可配 Enable/Disable）。</summary>
    public class BoolButtonSettingWidget : UniformSpacingPanelSettingWidget {
        BevelledButtonWidget m_button;
        string m_textTrue, m_textFalse;

        public override void Initialize(ModSettingItem descriptor, object currentValue, string name, string description) {
            base.Initialize(descriptor, currentValue, name, description);
            m_textTrue = LanguageControl.Yes;
            m_textFalse = LanguageControl.No;
            if (descriptor.WidgetProperties is JsonElement props) {
                string packageName = ModSettingLocalizer.ExtractPackageName(descriptor);
                string[] idChain = ModSettingLocalizer.ExtractIdChain(descriptor);
                if (props.TryGetProperty("TextTrue", out JsonElement tt) && tt.ValueKind == JsonValueKind.String)
                    m_textTrue = ModSettingLocalizer.ResolveText(packageName, idChain, "TextTrue", tt.GetString(), useIdFallback: false);
                if (props.TryGetProperty("TextFalse", out JsonElement tf) && tf.ValueKind == JsonValueKind.String)
                    m_textFalse = ModSettingLocalizer.ResolveText(packageName, idChain, "TextFalse", tf.GetString(), useIdFallback: false);
            }
            m_button = new BevelledButtonWidget {
                Style = ContentManager.Get<XElement>("Styles/ButtonStyle_310x60"),
                Text = (bool)Value ? m_textTrue : m_textFalse,
                VerticalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(20, 0)
            };
            Assemble(m_button);
        }

        public override bool Supports(Type type) => type == typeof(bool);

        public override void Update() {
            base.Update();
            IsOperating = m_button.IsClicked;
            if (IsOperating) {
                CommitValue(!(bool)Value);
                m_button.Text = (bool)Value ? m_textTrue : m_textFalse;
            }
        }

        protected override void ApplyToControl(object newValue) {
            if (newValue is bool b) m_button.Text = b ? m_textTrue : m_textFalse;
        }
    }

    /// <summary>enum 多项选择对话框（项数多时默认）。点按钮弹 ListSelectionDialog。</summary>
    public class EnumSelectionDialogSettingWidget : UniformSpacingPanelSettingWidget {
        BevelledButtonWidget m_button;
        Array m_members;
        string m_packageName;

        public override void Initialize(ModSettingItem descriptor, object currentValue, string name, string description) {
            base.Initialize(descriptor, currentValue, name, description);
            m_packageName = ModSettingLocalizer.ExtractPackageName(descriptor);
            m_members = Enum.GetValues(descriptor.Type);
            m_button = new BevelledButtonWidget {
                Style = ContentManager.Get<XElement>("Styles/ButtonStyle_310x60"),
                Text = MemberText(Value),
                VerticalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(20, 0)
            };
            Assemble(m_button);
        }

        public override bool Supports(Type type) => typeof(Enum).IsAssignableFrom(type);

        string MemberText(object value) => ModSettingLocalizer.GetEnumMemberText(m_packageName, Descriptor.Type, value);

        public override void Update() {
            base.Update();
            IsOperating = m_button.IsClicked;
            if (IsOperating) {
                DialogsManager.ShowDialog(null, new ListSelectionDialog(
                    NameLabelText, m_members, 60f,
                    MemberText,
                    item => { CommitValue(item); m_button.Text = MemberText(item); }
                ));
            }
        }

        protected override void ApplyToControl(object newValue) {
            if (newValue is Enum) m_button.Text = MemberText(newValue);
        }
    }

    /// <summary>enum 少项滑块（SliderWidget + 整数下标）。</summary>
    public class EnumSliderSettingWidget : UniformSpacingPanelSettingWidget {
        SliderWidget m_slider;
        Array m_members;
        string m_packageName;

        public override void Initialize(ModSettingItem descriptor, object currentValue, string name, string description) {
            base.Initialize(descriptor, currentValue, name, description);
            m_packageName = ModSettingLocalizer.ExtractPackageName(descriptor);
            m_members = Enum.GetValues(descriptor.Type);
            int lo = 0;
            int hi = Math.Max(1, m_members.Length - 1);
            m_slider = new SliderWidget {
                Size = new Vector2(float.PositiveInfinity, 60),
                VerticalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(20, 0),
                MinValue = lo,
                MaxValue = hi,
                Granularity = 1
            };
            if (descriptor.WidgetProperties is JsonElement props) {
                // 用户配的 Min/Max 钳到下标域 [lo, hi]，防止滑块越界选不中末项
                if (props.TryGetProperty("MinValue", out JsonElement min) && min.ValueKind == JsonValueKind.Number) m_slider.MinValue = Math.Clamp(min.GetSingle(), lo, hi);
                if (props.TryGetProperty("MaxValue", out JsonElement max) && max.ValueKind == JsonValueKind.Number) m_slider.MaxValue = Math.Clamp(max.GetSingle(), lo, hi);
                if (props.TryGetProperty("Granularity", out JsonElement g) && g.ValueKind == JsonValueKind.Number) m_slider.Granularity = g.GetSingle();
            }
            // Value 须在 Min/Max/Granularity 全部就位后再设：否则按默认区间+粒度钳/round，后续改 Granularity 不重 round（与 NumberSlider 同款 bug）。
            m_slider.Value = Math.Clamp(Array.IndexOf(m_members, Value), lo, hi);
            m_slider.Text = MemberText(Value);
            Assemble(m_slider);
        }

        public override bool Supports(Type type) => typeof(Enum).IsAssignableFrom(type);

        string MemberText(object value) => ModSettingLocalizer.GetEnumMemberText(m_packageName, Descriptor.Type, value);

        public override void Update() {
            base.Update();
            IsOperating = m_slider.IsSliding;
            if (m_slider.IsSliding || m_slider.SlidingCompleted) {
                int idx = Math.Clamp((int)Math.Round(m_slider.Value), 0, m_members.Length - 1);
                object current = m_members.GetValue(idx);
                m_slider.Text = MemberText(current);
                if (m_slider.SlidingCompleted) CommitValue(current);
            }
        }

        protected override void ApplyToControl(object newValue) {
            if (newValue is not Enum) return;
            int idx = Array.IndexOf(m_members, newValue);
            if (idx < 0) return;
            m_slider.Value = Math.Clamp(idx, 0, m_members.Length - 1);
            m_slider.Text = MemberText(newValue);
        }
    }

    /// <summary>数值滑块（int/float 等）。需 WidgetProperties.MinValue/MaxValue，缺则降级 0~1。
    /// DecimalPlaces 控制显示小数位数（默认 3，即 0.###）；0=整数显示。</summary>
    public class NumberSliderSettingWidget : UniformSpacingPanelSettingWidget {
        SliderWidget m_slider;
        int m_decimalPlaces = 3;
        string m_textFormat = "0.###";

        /// <summary>显示保留的小数位数。负值视为 0。改动后立即重建格式串。</summary>
        public int DecimalPlaces {
            get => m_decimalPlaces;
            set {
                m_decimalPlaces = Math.Max(0, value);
                m_textFormat = m_decimalPlaces > 0 ? $"0.{new string('#', m_decimalPlaces)}" : "0";
            }
        }

        public override void Initialize(ModSettingItem descriptor, object currentValue, string name, string description) {
            base.Initialize(descriptor, currentValue, name, description);
            // Horizontal + slider Size=(Inf,60)：UniformSpacingPanel 主轴=X 时 desired=(Inf,60)，Y finite。
            // Vertical 会让主轴=Y→desired.Y=Inf，污染 ContentStack(StackPanel Vertical) 的 desired.Y=Inf，
            // arrange 链经 layoutTransform 算 NaN，整个页面 widget 崩。
            m_slider = new SliderWidget {
                Size = new Vector2(float.PositiveInfinity, 60),
                VerticalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(20, 0),
                MinValue = 0,
                MaxValue = 1,
                Granularity = 0.1f
            };
            if (descriptor.WidgetProperties is JsonElement props) {
                if (props.TryGetProperty("MinValue", out JsonElement min) && min.ValueKind == JsonValueKind.Number) m_slider.MinValue = min.GetSingle();
                if (props.TryGetProperty("MaxValue", out JsonElement max) && max.ValueKind == JsonValueKind.Number) m_slider.MaxValue = max.GetSingle();
                if (props.TryGetProperty("Granularity", out JsonElement g) && g.ValueKind == JsonValueKind.Number) m_slider.Granularity = g.GetSingle();
                if (props.TryGetProperty("DecimalPlaces", out JsonElement dp) && dp.ValueKind == JsonValueKind.Number) DecimalPlaces = dp.GetInt32();
            }
            m_slider.Value = ToFloat(Value);
            UpdateText();
            Assemble(m_slider);
        }

        public override bool Supports(Type type) => IsNumeric(type);

        static bool IsNumeric(Type t) =>
            t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
            || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) || t == typeof(sbyte)
            || t == typeof(float) || t == typeof(double) || t == typeof(decimal);

        static float ToFloat(object v) => Convert.ToSingle(v, CultureInfo.InvariantCulture);
        void UpdateText() => m_slider.Text = (Value as IFormattable)?.ToString(m_textFormat, CultureInfo.InvariantCulture) ?? Value?.ToString() ?? "";

        public override void Update() {
            base.Update();
            IsOperating = m_slider.IsSliding;
            if (m_slider.IsSliding) m_slider.Text = m_slider.Value.ToString(m_textFormat, CultureInfo.InvariantCulture);
            if (m_slider.SlidingCompleted) {
                object newVal = Convert.ChangeType(m_slider.Value, Descriptor.Type, CultureInfo.InvariantCulture);
                CommitValue(newVal);
                UpdateText();
            }
        }

        protected override void ApplyToControl(object newValue) {
            if (newValue == null || !IsNumeric(newValue.GetType())) return;
            m_slider.Value = ToFloat(newValue);
            UpdateText();
        }
    }

    /// <summary>文本输入（TextBoxWidget）。</summary>
    public class TextBoxSettingWidget : UniformSpacingPanelSettingWidget {
        TextBoxWidget m_textBox;

        public override void Initialize(ModSettingItem descriptor, object currentValue, string name, string description) {
            base.Initialize(descriptor, currentValue, name, description);
            m_textBox = new TextBoxWidget { Text = Value as string ?? "", Size = new Vector2(float.PositiveInfinity, 50), VerticalAlignment = WidgetAlignment.Center, Margin = new Vector2(10, 0) };
            CanvasWidget canvasWidget = new() {
                VerticalAlignment = WidgetAlignment.Center,
                Size = new Vector2(float.PositiveInfinity, 50),
                Margin = new Vector2(20, 5),
                Children = { new BevelledRectangleWidget { Style = ContentManager.Get<XElement>("Styles/TextBoxArea") }, m_textBox }
            };
            Assemble(canvasWidget);
        }

        public override bool Supports(Type type) => type == typeof(string);

        public override void Update() {
            base.Update();
            IsOperating = m_textBox.HasFocus;
            string current = Value as string;
            if (m_textBox.Text != current) CommitValue(m_textBox.Text);
        }

        protected override void ApplyToControl(object newValue) {
            if (newValue is string s) m_textBox.Text = s;
        }
    }
}
