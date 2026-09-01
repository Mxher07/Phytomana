using Engine.Media;

namespace Game {
    public class LabelWidget : FontTextWidget {
        public static BitmapFont m_bitmapFont;

        public static BitmapFont BitmapFont {
            get => m_bitmapFont ??= ContentManager.Get<BitmapFont>("Fonts/Pericles");
            set => m_bitmapFont = value;
        }

        public override string Text {
            get => m_text;
            set {
                if (m_text != value
                    && value != null) {
                    if (value.StartsWith('[')
                        && value.EndsWith(']')) {
                        string[] xp = value.Substring(1, value.Length - 2).Split(':');
                        m_text = xp.Length == 2 ? LanguageControl.GetContentWidgets(xp[0], xp[1]) : LanguageControl.Get("Usual", value);
                    }
                    else {
                        m_text = value;
                    }
                    m_linesSize = null;
                }
            }
        }

        public LabelWidget() => Font = BitmapFont;
    }
}