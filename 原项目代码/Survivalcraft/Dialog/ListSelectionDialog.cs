using System.Xml.Linq;
using Engine;

namespace Game {
    public class ListSelectionDialog : Dialog {
        public Action<object> m_selectionHandler;

        public LabelWidget m_titleLabelWidget;

        public ListPanelWidget m_listWidget;

        public CanvasWidget m_contentWidget;

        public double? m_dismissTime;

        public bool m_isDismissed;

        public Vector2 ContentSize {
            get => m_contentWidget.Size;
            set => m_contentWidget.Size = value;
        }

        public ListSelectionDialog(string title,
            IEnumerable items,
            float itemSize,
            Func<object, Widget> itemWidgetFactory,
            Action<object> selectionHandler) {
            m_selectionHandler = selectionHandler;
            XElement node = ContentManager.Get<XElement>("Dialogs/ListSelectionDialog");
            LoadContents(this, node);
            m_titleLabelWidget = Children.Find<LabelWidget>("ListSelectionDialog.Title");
            m_listWidget = Children.Find<ListPanelWidget>("ListSelectionDialog.List");
            m_contentWidget = Children.Find<CanvasWidget>("ListSelectionDialog.Content");
            m_titleLabelWidget.Text = title;
            m_titleLabelWidget.IsVisible = !string.IsNullOrEmpty(title);
            m_listWidget.ItemSize = itemSize;
            if (itemWidgetFactory != null) {
                m_listWidget.ItemWidgetFactory = itemWidgetFactory;
            }
            foreach (object item in items) {
                m_listWidget.AddItem(item);
            }
            int num = m_listWidget.Items.Count;
            float num2;
            while (true) {
                if (num >= 0) {
                    num2 = MathUtils.Min(num + 0.5f, m_listWidget.Items.Count);
                    if (num2 * itemSize <= m_contentWidget.Size.Y) {
                        break;
                    }
                    num--;
                    continue;
                }
                return;
            }
            m_contentWidget.Size = new Vector2(m_contentWidget.Size.X, num2 * itemSize);
        }

        public ListSelectionDialog(string title,
            IEnumerable items,
            float itemSize,
            Func<object, string> itemToStringConverter,
            Action<object> selectionHandler) : this(
            title,
            items,
            itemSize,
            item => new LabelWidget {
                Text = itemToStringConverter(item), HorizontalAlignment = WidgetAlignment.Center, VerticalAlignment = WidgetAlignment.Center
            },
            selectionHandler
        ) { }

        public override void Update() {
            if (Input.Back
                || Input.Cancel) {
                m_dismissTime = 0.0;
            }
            else if (Input.Tap.HasValue
                && !m_listWidget.HitTest(Input.Tap.Value)) {
                m_dismissTime = 0.0;
            }
            else if (!m_dismissTime.HasValue
                && m_listWidget.SelectedItem != null) {
                m_dismissTime = Time.FrameStartTime + 0.05000000074505806;
            }
            if (m_dismissTime.HasValue
                && Time.FrameStartTime >= m_dismissTime.Value) {
                Dismiss(m_listWidget.SelectedItem);
            }
        }

        public void Dismiss(object result) {
            if (!m_isDismissed) {
                m_isDismissed = true;
                DialogsManager.HideDialog(this);
                if (m_selectionHandler != null
                    && result != null) {
                    m_selectionHandler(result);
                }
            }
        }
    }
}