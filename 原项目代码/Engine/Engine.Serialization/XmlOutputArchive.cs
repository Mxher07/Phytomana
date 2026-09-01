using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Engine.Serialization {
    public class XmlOutputArchive : OutputArchive {
        public XElement Node { get; private set; }

        public XmlOutputArchive(string rootNodeName, int version = 0, object context = null) : this(new XElement(rootNodeName), version, context) { }

        public XmlOutputArchive(XElement node, int version = 0, object context = null) : base(version, context) {
            ArgumentNullException.ThrowIfNull(node);
            Node = node;
        }

        public void Reset(XElement node, int version = 0, object context = null) {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Reset(version, context);
        }

        public override void Serialize(string name, sbyte value) {
            Serialize(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, byte value) {
            Serialize(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, short value) {
            Serialize(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, ushort value) {
            Serialize(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, int value) {
            Serialize(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, uint value) {
            Serialize(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, long value) {
            Serialize(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, ulong value) {
            Serialize(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, float value) {
            Serialize(name, value.ToString("R", CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, double value) {
            Serialize(name, value.ToString("R", CultureInfo.InvariantCulture));
        }

        public override void Serialize(string name, bool value) {
            Serialize(name, value ? "True" : "False");
        }

        public override void Serialize(string name, char value) {
            Serialize(name, value.ToString());
        }

        public override void Serialize(string name, string value) {
            if (name == null) {
                Node.SetValue(value ?? string.Empty);
            }
            else {
                Node.SetAttributeValue(name, value ?? string.Empty);
            }
        }

        public override void Serialize(string name, byte[] value) {
            Serialize(name, Convert.ToBase64String(value));
        }

        public override void Serialize(string name, int length, byte[] value) {
            if (value.Length != length) {
                throw new InvalidOperationException("Invalid fixed array length.");
            }
            Serialize(name, Convert.ToBase64String(value));
        }

        public override void SerializeCollection<T>(string name, Func<T, string> itemNameFunc, IEnumerable<T> collection) {
            EnterNode(name, false);
            SerializeData serializeData = GetSerializeData(typeof(T), true);
            bool flag = true;
            using IEnumerator<XElement> enumerator = Node.Elements().GetEnumerator();
            foreach (T item in collection) {
                string name2 = itemNameFunc != null ? itemNameFunc(item) : "Item";
                if (flag && enumerator.MoveNext()) {
                    EnterNode(enumerator.Current);
                    WriteObject(null, serializeData, item);
                    LeaveNode();
                }
                else {
                    EnterNode(name2, true);
                    WriteObject(null, serializeData, item);
                    LeaveNode(name2);
                    flag = false;
                }
            }
            LeaveNode(name);
        }

        public override void SerializeDictionary<K, V>(string name, IDictionary<K, V> dictionary) {
            EnterNode(name, false);
            SerializeData serializeData = GetSerializeData(typeof(K), true);
            SerializeData serializeData2 = GetSerializeData(typeof(V), true);
            if (serializeData.IsHumanReadableSupported) {
                foreach (KeyValuePair<K, V> item in dictionary) {
                    string name2 = XmlConvert.EncodeLocalName(HumanReadableConverter.ConvertToString(item.Key));
                    EnterNode(name2, true);
                    WriteObject(null, serializeData2, item.Value);
                    LeaveNode(name2);
                }
            }
            else {
                foreach (KeyValuePair<K, V> item2 in dictionary) {
                    EnterNode("e", true);
                    WriteObject("k", serializeData, item2.Key);
                    WriteObject("v", serializeData2, item2.Value);
                    LeaveNode("e");
                }
            }
            LeaveNode(name);
        }

        public override void WriteObjectInfo(int? objectId, bool isReference, Type runtimeType) {
            if (isReference) {
                Node.SetAttributeValue("_ref", objectId.HasValue ? objectId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
                return;
            }
            Node.SetAttributeValue("_def", objectId.HasValue ? objectId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
            if (runtimeType != null) {
                Node.SetAttributeValue("_type", TypeCache.GetShortTypeName(runtimeType.FullName));
            }
        }

        protected override void WriteObject(string name, SerializeData staticSerializeData, object value) {
            if (staticSerializeData.IsHumanReadableSupported) {
                Serialize(name, value != null ? HumanReadableConverter.ConvertToString(value) : string.Empty);
                return;
            }
            EnterNode(name, false);
            base.WriteObject(name, staticSerializeData, value);
            LeaveNode(name);
        }

        void EnterNode(XElement node) {
            Node = node;
        }

        void EnterNode(string name, bool createNewNode) {
            if (name != null) {
                if (createNewNode) {
                    XElement xElement = new(name);
                    Node.Add(xElement);
                    Node = xElement;
                    return;
                }
                XElement xElement2 = Node.Element(name);
                if (xElement2 == null) {
                    xElement2 = new XElement(name);
                    Node.Add(xElement2);
                }
                Node = xElement2;
            }
        }

        void LeaveNode() {
            Node = Node.Parent;
        }

        void LeaveNode(string name) {
            if (name != null) {
                Node = Node.Parent;
            }
        }

        public static void RemoveUnusedDefs(XElement node) {
            HashSet<int> set = new();
            FindUsedDefs(node, set);
            RemoveUnusedDefs(node, set);
        }

        static void RemoveUnusedDefs(XElement node, HashSet<int> set) {
            foreach (XElement item in node.Elements()) {
                RemoveUnusedDefs(item, set);
            }
            XAttribute xAttribute = node.Attribute("_def");
            if (xAttribute != null
                && !set.Contains(int.Parse(xAttribute.Value))) {
                xAttribute.Remove();
            }
        }

        static void FindUsedDefs(XElement node, HashSet<int> set) {
            foreach (XElement item in node.Elements()) {
                FindUsedDefs(item, set);
            }
            XAttribute xAttribute = node.Attribute("_ref");
            if (xAttribute != null) {
                set.Add(int.Parse(xAttribute.Value));
            }
        }
    }
}