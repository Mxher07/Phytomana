using System.Xml.Linq;
using Engine;

namespace Game {
    public class ModsManageContentItemWidget : StackPanelWidget {
        public readonly RectangleWidget m_iconWidget;
        public readonly RectangleWidget m_disabledOverlayWidget;
        public readonly LabelWidget m_titleWidget;
        public readonly LabelWidget m_informationWidget;
        public readonly LabelWidget m_descriptionWidget;

        public Subtexture Icon {
            get => m_iconWidget.Subtexture;
            set => m_iconWidget.Subtexture = value;
        }

        public bool IsDisabled {
            get => m_disabledOverlayWidget.IsVisible;
            set {
                m_disabledOverlayWidget.IsVisible = value;
                m_iconWidget.Size = value ? new Vector2(48f) : new Vector2(64f);
            }
        }

        public string Title {
            get => m_titleWidget.Text;
            set => m_titleWidget.Text = value;
        }

        public Color TitleColor {
            get => m_titleWidget.Color;
            set => m_titleWidget.Color = value;
        }

        public string Information {
            get => m_informationWidget.Text;
            set => m_informationWidget.Text = value;
        }

        public bool IsInformationVisible {
            get => m_informationWidget.IsVisible;
            set => m_informationWidget.IsVisible = value;
        }

        public string Description {
            get => m_descriptionWidget.Text;
            set => m_descriptionWidget.Text = value;
        }

        public ModsManageContentItemWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/ModsManageContentItem");
            LoadContents(this, node);
            m_iconWidget = Children.Find<RectangleWidget>("ModsManageContentItem.Icon");
            m_disabledOverlayWidget = Children.Find<RectangleWidget>("ModsManageContentItem.DisabledOverlay");
            m_titleWidget = Children.Find<LabelWidget>("ModsManageContentItem.Title");
            m_informationWidget = Children.Find<LabelWidget>("ModsManageContentItem.Information");
            m_descriptionWidget = Children.Find<LabelWidget>("ModsManageContentItem.Description");
        }
    }
}