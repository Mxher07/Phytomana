using System.Xml.Linq;
using Engine;

namespace Game {
    public class SelectExternalContentProviderDialog : ListSelectionDialog {
        public SelectExternalContentProviderDialog(string title, bool listingSupportRequired, Action<IExternalContentProvider> selectionHandler) :
            base(
                title,
                ExternalContentManager.Providers.Where(p => !listingSupportRequired || p.SupportsListing),
                100f,
                delegate(object item) {
                    IExternalContentProvider externalContentProvider = (IExternalContentProvider)item;
                    XElement node = ContentManager.Get<XElement>("Widgets/SelectExternalContentProviderItem");
                    ContainerWidget obj = (ContainerWidget)LoadWidget(null, node, null);
                    obj.Children.Find<LabelWidget>("SelectExternalContentProvider.Text").Text = externalContentProvider.DisplayName;
                    obj.Children.Find<LabelWidget>("SelectExternalContentProvider.Details").Text = externalContentProvider.Description;
                    return obj;
                },
                delegate(object item) { selectionHandler((IExternalContentProvider)item); }
            ) => ContentSize = new Vector2(700f, ContentSize.Y);
    }
}