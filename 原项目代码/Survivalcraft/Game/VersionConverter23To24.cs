using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game {
    public class VersionConverter23To24 : VersionConverter {
        public override string SourceVersion => "2.3";

        public override string TargetVersion => "2.4";

        public override void ConvertProjectXml(XElement projectNode) {
            XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
            foreach (XElement item in from e in projectNode.Element("Subsystems").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "GameInfo"
                select e) {
                XElement xElement = XmlUtils.AddElement(item, "Value");
                xElement.SetAttributeValue("Name", "AreSeasonsChanging");
                xElement.SetAttributeValue("Type", "bool");
                xElement.SetAttributeValue("Value", "false");
                XElement xElement2 = XmlUtils.AddElement(item, "Value");
                xElement2.SetAttributeValue("Name", "TimeOfYear");
                xElement2.SetAttributeValue("Type", "float");
                xElement2.SetAttributeValue("Value", IntervalUtils.Midpoint(SubsystemSeasons.SummerStart, SubsystemSeasons.AutumnStart));
            }
        }

        public override void ConvertWorld(string directoryName) {
            string path = Storage.CombinePaths(directoryName, "Project.xml");
            XElement xElement;
            using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read)) {
                xElement = XmlUtils.LoadXmlFromStream(stream, null, true);
            }
            ConvertProjectXml(xElement);
            using Stream stream2 = Storage.OpenFile(path, OpenFileMode.Create);
            XmlUtils.SaveXmlToStream(xElement, stream2, null, true);
        }
    }
}