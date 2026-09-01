using System.Xml.Linq;
using Engine;

namespace Game {
    public class SelectGameModeDialog : ListSelectionDialog {
        public SelectGameModeDialog(string title, bool allowAdventure, bool allowCruel, Action<GameMode> selectionHandler) : base(
            title,
            GetAllowedGameModes(allowAdventure, allowCruel),
            140f,
            delegate(object item) {
                GameMode gameMode = (GameMode)item;
                XElement node = ContentManager.Get<XElement>("Widgets/SelectGameModeItem");
                ContainerWidget obj = (ContainerWidget)LoadWidget(null, node, null);
                obj.Children.Find<LabelWidget>("SelectGameModeItem.Name").Text = LanguageControl.Get("GameMode", gameMode.ToString());
                obj.Children.Find<LabelWidget>("SelectGameModeItem.Description").Text =
                    StringsManager.GetString($"GameMode.{gameMode}.Description");
                return obj;
            },
            delegate(object item) { selectionHandler((GameMode)item); }
        ) => ContentSize = new Vector2(750f, 420f);

        public static IEnumerable<GameMode> GetAllowedGameModes(bool allowAdventure, bool allowCruel) {
            yield return GameMode.Creative;
            yield return GameMode.Survival;
            yield return GameMode.Challenging;
            yield return GameMode.Harmless;
            if (allowAdventure) {
                yield return GameMode.Adventure;
            }
            if (allowCruel) {
                yield return GameMode.Cruel;
            }
        }
    }
}