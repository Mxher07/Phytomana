using System.Text.Json;

namespace Game {
    public abstract class ModSettingElement { }

    public class ModSettingPage : ModSettingElement {
        public string Id;
        public string Name;
        public string Title;
        public string Description;
        public List<ModSettingElement> Items = new();
    }

    public class ModSettingItem : ModSettingElement {
        public string Id;
        public string Name;
        public string Description;
        public Type Type;
        public object Default;
        public Type WidgetType;
        public JsonElement? WidgetProperties;
        public string CachedPath;
    }

    public class ModSettingSeparator : ModSettingElement { }

    public class ModSettingLabel : ModSettingElement {
        public string Id;
        public string Text;
    }
}
