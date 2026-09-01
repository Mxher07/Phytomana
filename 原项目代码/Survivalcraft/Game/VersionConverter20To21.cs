using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game {
    public class VersionConverter20To21 : VersionConverter {
        public override string SourceVersion => "2.0";

        public override string TargetVersion => "2.1";

        public override void ConvertProjectXml(XElement projectNode) {
            XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
            string value = string.Empty;
            foreach (XElement item in from e in projectNode.Element("Subsystems").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "GameInfo"
                select e) {
                foreach (XElement item2 in from e in item.Elements("Value")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "TerrainGenerationMode"
                    select e) {
                    if (XmlUtils.GetAttributeValue(item2, "Value", "") == "Normal") {
                        XmlUtils.SetAttributeValue(item2, "Value", "Continent");
                    }
                }
                XElement xElement = (from e in item.Elements("Value")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "CharacterSkinName"
                    select e).FirstOrDefault();
                if (xElement != null) {
                    value = XmlUtils.GetAttributeValue(xElement, "Value", string.Empty);
                }
            }
            if (string.IsNullOrEmpty(value)) {
                value = "$Male1";
            }
            foreach (XElement item3 in from e in projectNode.Element("Subsystems").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "Player"
                select e) {
                XmlUtils.SetAttributeValue(item3, "Name", "Players");
                XElement xElement2 = new("Values");
                xElement2.SetAttributeValue("Name", "Players");
                XElement xElement3 = new("Values");
                xElement3.SetAttributeValue("Name", "1");
                xElement2.Add(xElement3);
                XElement[] array = item3.Elements().ToArray();
                foreach (XElement xElement4 in array) {
                    xElement4.Remove();
                    xElement3.Add(xElement4);
                }
                xElement3.Add(
                    new XElement(
                        "Value",
                        new XAttribute("Name", "CharacterSkinName"),
                        new XAttribute("Type", "string"),
                        new XAttribute("Value", value)
                    )
                );
                item3.Add(xElement2);
                item3.Add(
                    new XElement("Value", new XAttribute("Name", "NextPlayerIndex"), new XAttribute("Type", "int"), new XAttribute("Value", "2"))
                );
            }
            foreach (XElement item4 in from e in projectNode.Element("Subsystems").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "PlayerStats"
                select e) {
                XElement xElement5 = (from e in item4.Elements("Values")
                    where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "PlayerStats"
                    select e).FirstOrDefault();
                if (xElement5 != null) {
                    XElement xElement6 = new("Values");
                    XmlUtils.SetAttributeValue(xElement6, "Name", "Stats");
                    item4.Add(xElement6);
                    XmlUtils.SetAttributeValue(xElement5, "Name", "1");
                    xElement5.Remove();
                    xElement6.Add(xElement5);
                }
            }
            foreach (XElement item5 in from e in projectNode.Element("Entities").Elements()
                where XmlUtils.GetAttributeValue(e, "Name", string.Empty).StartsWith("Player")
                select e) {
                XmlUtils.SetAttributeValue(item5, "Guid", "bef1b918-6418-41c9-a598-95e8ffd39ab3");
                XmlUtils.SetAttributeValue(item5, "Name", "MalePlayer");
            }
            foreach (XElement item6 in projectNode.Element("Entities").Elements()) {
                foreach (XElement item7 in (from e in item6.Descendants("Value")
                        where XmlUtils.GetAttributeValue(e, "Name", string.Empty) == "SpawnPool"
                        select e).ToList()) {
                    item7.Remove();
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