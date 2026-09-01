using System.Xml.Linq;

namespace Game {
    public class SpawnDialog : Dialog {
        public LabelWidget m_seasonLabelWidget;

        public LabelWidget m_largeLabelWidget;

        public LabelWidget m_smallLabelWidget;

        public ValueBarWidget m_progressWidget;

        public float TimeOfYear {
            set {
                m_seasonLabelWidget.Text = SubsystemSeasons.GetTimeOfYearName(value);
                m_seasonLabelWidget.Color = SubsystemSeasons.GetTimeOfYearColor(value);
                m_progressWidget.LitBarColor = SubsystemSeasons.GetTimeOfYearColor(value);
            }
        }

        public string LargeMessage {
            get => m_largeLabelWidget.Text;
            set => m_largeLabelWidget.Text = value;
        }

        public string SmallMessage {
            get => m_smallLabelWidget.Text;
            set => m_smallLabelWidget.Text = value;
        }

        public float Progress {
            get => m_progressWidget.Value;
            set => m_progressWidget.Value = value;
        }

        public SpawnDialog() {
            XElement node = ContentManager.Get<XElement>("Dialogs/SpawnDialog");
            LoadContents(this, node);
            m_seasonLabelWidget = Children.Find<LabelWidget>("SpawnDialog.SeasonLabel");
            m_largeLabelWidget = Children.Find<LabelWidget>("SpawnDialog.LargeLabel");
            m_smallLabelWidget = Children.Find<LabelWidget>("SpawnDialog.SmallLabel");
            m_progressWidget = Children.Find<ValueBarWidget>("SpawnDialog.Progress");
        }
    }
}