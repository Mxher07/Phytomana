using System.Xml.Linq;

namespace Engine.Serialization {
    public class XElementSerializer : ISerializer<XElement> {
        public void Serialize(InputArchive archive, ref XElement value) {
            if (archive is XmlInputArchive xmlInputArchive) {
                value = xmlInputArchive.Node.Elements().First();
                return;
            }
            string value2 = null;
            archive.Serialize(null, ref value2);
            value = XElement.Parse(value2);
        }

        public void Serialize(OutputArchive archive, XElement value) {
            if (archive is XmlOutputArchive xmlOutputArchive) {
                xmlOutputArchive.Node.Add(value);
            }
            else {
                archive.Serialize(null, value.ToString());
            }
        }
    }
}