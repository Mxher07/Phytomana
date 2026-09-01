using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Engine.Serialization {
    public class XmlInputArchive : InputArchive {
        public XElement Node { get; private set; }

        public XmlInputArchive(XElement node, int version = 0, object context = null) : base(version, context) {
            ArgumentNullException.ThrowIfNull(node);
            Node = node;
        }

        public void Reset(XElement node, int version = 0, object context = null) {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Reset(version, context);
        }

        public override void Serialize(string name, ref sbyte value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = sbyte.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref byte value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = byte.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref short value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = short.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref ushort value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = ushort.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref int value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = int.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref uint value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = uint.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref long value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = long.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref ulong value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = ulong.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref float value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = float.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref double value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = double.Parse(value2, CultureInfo.InvariantCulture);
        }

        public override void Serialize(string name, ref bool value) {
            string value2 = null;
            Serialize(name, ref value2);
            if (string.Equals(value2, "False", StringComparison.OrdinalIgnoreCase)) {
                value = false;
                return;
            }
            if (string.Equals(value2, "True", StringComparison.OrdinalIgnoreCase)) {
                value = true;
                return;
            }
            throw new InvalidOperationException($"Cannot convert string \"{value2}\" to a Boolean.");
        }

        public override void Serialize(string name, ref char value) {
            string value2 = null;
            Serialize(name, ref value2);
            if (value2.Length == 1) {
                value = value2[0];
                return;
            }
            throw new InvalidOperationException($"Cannot convert string \"{value2}\" to a Char.");
        }

        public override void Serialize(string name, ref string value) {
            if (name != null) {
                XAttribute xAttribute = Node.Attribute(name);
                if (xAttribute == null) {
                    throw new InvalidOperationException($"Required XML node \"{name}\" not found.");
                }
                value = xAttribute.Value;
            }
            else {
                value = Node.Value;
            }
        }

        public override void Serialize(string name, ref byte[] value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = Convert.FromBase64String(value2);
        }

        public override void Serialize(string name, int length, ref byte[] value) {
            string value2 = null;
            Serialize(name, ref value2);
            value = Convert.FromBase64String(value2);
            if (value.Length != length) {
                throw new InvalidOperationException("Invalid fixed array length.");
            }
        }

        public override void SerializeCollection<T>(string name, ICollection<T> collection) {
            EnterNode(name);
            SerializeData serializeData = GetSerializeData(typeof(T), true);
            using (IEnumerator<T> enumerator = collection.Count > 0 ? collection.GetEnumerator() : null) {
                foreach (XElement item in Node.Elements()) {
                    Node = item;
                    if (enumerator != null
                        && enumerator.MoveNext()) {
                        T value = enumerator.Current;
                        ReadObject(null, serializeData, ref value, false);
                    }
                    else {
                        T value2 = default;
                        ReadObject(null, serializeData, ref value2, true);
                        collection.Add(value2);
                    }
                    Node = Node.Parent;
                }
            }
            LeaveNode(name);
        }

        public override void SerializeDictionary<K, V>(string name, IDictionary<K, V> dictionary) {
            EnterNode(name);
            SerializeData serializeData = GetSerializeData(typeof(K), true);
            SerializeData serializeData2 = GetSerializeData(typeof(V), true);
            if (typeof(K) == typeof(string)) {
                using (IEnumerator<XElement> enumerator = Node.Elements().GetEnumerator()) {
                    while (enumerator.MoveNext()) {
                        XElement xElement = Node = enumerator.Current;
                        if (xElement != null) {
                            string localName = XmlConvert.DecodeName(xElement.Name.LocalName);
                            K key = (K)HumanReadableConverter.ConvertFromString(typeof(K), localName);
                            V value = default;
                            if (dictionary.TryGetValue(key, out V value2)) {
                                value = value2;
                                ReadObject(null, serializeData2, ref value, false);
                            }
                            else {
                                ReadObject(null, serializeData2, ref value, true);
                                dictionary.Add(key, value);
                            }
                            Node = Node.Parent;
                        }
                    }
                }
            }
            else {
                using (IEnumerator<XElement> enumerator = Node.Elements().GetEnumerator()) {
                    while (enumerator.MoveNext()) {
                        //XElement xElement2 = Node = enumerator.Current;
                        K value3 = default;
                        V value4 = default;
                        ReadObject("k", serializeData, ref value3, true);
                        if (dictionary.TryGetValue(value3, out V value5)) {
                            value4 = value5;
                            ReadObject("v", serializeData2, ref value4, false);
                        }
                        ReadObject("v", serializeData2, ref value4, true);
                        dictionary.Add(value3, value4);
                        Node = Node?.Parent;
                    }
                }
            }
            LeaveNode(name);
        }

        public override void ReadObjectInfo(out int? objectId, out bool isReference, out Type runtimeType) {
            XAttribute xAttribute = Node.Attribute("_ref");
            if (xAttribute != null) {
                runtimeType = null;
                isReference = true;
                objectId = int.Parse(xAttribute.Value);
                return;
            }
            XAttribute xAttribute2 = Node.Attribute("_def");
            objectId = xAttribute2 != null ? new int?(int.Parse(xAttribute2.Value)) : null;
            XAttribute xAttribute3 = Node.Attribute("_type");
            if (xAttribute2 != null
                && xAttribute3 != null) {
                runtimeType = TypeCache.FindType(xAttribute3.Value, false, true);
            }
            else {
                runtimeType = null;
            }
            isReference = false;
        }

        protected override void ReadObject(string name, SerializeData staticSerializeData, ref object value, bool allowOverwriteOfExistingObject) {
            if (staticSerializeData.IsHumanReadableSupported) {
                string value2 = null;
                Serialize(name, ref value2);
                value = HumanReadableConverter.ConvertFromString(staticSerializeData.Type, value2);
            }
            else {
                EnterNode(name);
                base.ReadObject(name, staticSerializeData, ref value, allowOverwriteOfExistingObject);
                LeaveNode(name);
            }
        }

        protected override void ReadObject<T>(string name, SerializeData staticSerializeData, ref T value, bool allowOverwriteOfExistingObject) {
            object value2 = value;
            ReadObject(name, staticSerializeData, ref value2, allowOverwriteOfExistingObject);
            value = (T)value2;
        }

        void EnterNode(string name) {
            if (name != null) {
                XElement xElement = Node.Element(name);
                Node = xElement ?? throw new InvalidOperationException($"XML element \"{name}\" not found.");
            }
        }

        void LeaveNode(string name) {
            if (name != null) {
                Node = Node.Parent;
            }
        }
    }
}