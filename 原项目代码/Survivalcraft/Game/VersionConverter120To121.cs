using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game {
    public class VersionConverter120To121 : VersionConverter {
        public override string SourceVersion => "1.20";

        public override string TargetVersion => "1.21";

        public override void ConvertProjectXml(XElement projectNode) {
            XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
            foreach (XElement item in projectNode.Element("Entities").Elements()) {
                foreach (XElement item2 in from e in item.Elements("Values")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Body"
                        || XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Frame"
                    select e) {
                    using (IEnumerator<XElement> enumerator3 = (from e in item2.Elements("Value")
                            where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "LocalMatrix"
                            select e).GetEnumerator()) {
                        if (enumerator3.MoveNext()) {
                            XElement current2 = enumerator3.Current;
                            XmlUtils.GetAttributeValue<Matrix>(current2, "Value")
                                .Decompose(out Vector3 _, out Quaternion rotation, out Vector3 translation);
                            XElement xElement = new("Value");
                            XElement xElement2 = new("Value");
                            XmlUtils.SetAttributeValue(xElement, "Name", "Position");
                            XmlUtils.SetAttributeValue(xElement, "Type", "Microsoft.Xna.Framework.Vector3");
                            XmlUtils.SetAttributeValue(xElement, "Value", translation);
                            XmlUtils.SetAttributeValue(xElement2, "Name", "Rotation");
                            XmlUtils.SetAttributeValue(xElement2, "Type", "Microsoft.Xna.Framework.Quaternion");
                            XmlUtils.SetAttributeValue(xElement2, "Value", rotation);
                            item2.Add(xElement);
                            item2.Add(xElement2);
                            current2.Remove();
                        }
                    }
                }
            }
        }

        public override void ConvertWorld(string directoryName) {
            string path = Storage.CombinePaths(directoryName, "Project.xml");
            XElement xElement;
            using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read)) {
                xElement = XmlUtils.LoadXmlFromStream(stream, null, true);
            }
            ConvertProjectXml(xElement);
            using (Stream stream2 = Storage.OpenFile(path, OpenFileMode.Create)) {
                XmlUtils.SaveXmlToStream(xElement, stream2, null, true);
            }
        }
    }
}