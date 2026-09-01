using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Engine;
using Engine.Serialization;
using XmlUtilities;

namespace TemplatesDatabase {
    public static class XmlDatabaseSerializer {
        public static Database LoadDatabase(XElement node) {
            Dictionary<string, DatabaseObjectType> dictionary = [];
            XElement xElement = XmlUtils.FindChildElement(node, "DatabaseObjectTypes", true);
            foreach (XElement item in xElement.Elements()) {
                string attributeValue = XmlUtils.GetAttributeValue<string>(item, "Name");
                string attributeValue2 = XmlUtils.GetAttributeValue<string>(item, "DefaultInstanceName");
                string attributeValue3 = XmlUtils.GetAttributeValue<string>(item, "IconName");
                int attributeValue4 = XmlUtils.GetAttributeValue<int>(item, "Order");
                bool attributeValue5 = XmlUtils.GetAttributeValue<bool>(item, "SupportsValue");
                bool attributeValue6 = XmlUtils.GetAttributeValue<bool>(item, "MustInherit");
                int attributeValue7 = XmlUtils.GetAttributeValue<int>(item, "NameLengthLimit");
                bool attributeValue8 = XmlUtils.GetAttributeValue<bool>(item, "SaveStandalone");
                DatabaseObjectType value = new(
                    attributeValue,
                    attributeValue2,
                    attributeValue3,
                    attributeValue4,
                    attributeValue5,
                    attributeValue6,
                    attributeValue7,
                    attributeValue8
                );
                dictionary.Add(attributeValue, value);
            }
            foreach (XElement item2 in xElement.Elements()) {
                string attributeValue9 = XmlUtils.GetAttributeValue<string>(item2, "Name");
                string attributeValue10 = XmlUtils.GetAttributeValue<string>(item2, "AllowedNestingParents");
                string attributeValue11 = XmlUtils.GetAttributeValue<string>(item2, "AllowedInheritanceParents");
                string attributeValue12 = XmlUtils.GetAttributeValue<string>(item2, "NestedValueType");
                List<DatabaseObjectType> list = [];
                string[] array = attributeValue10.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
                foreach (string text in array) {
                    if (!dictionary.TryGetValue(text, out DatabaseObjectType value2)) {
                        throw new InvalidOperationException($"Database object type \"{text}\" not found.");
                    }
                    list.Add(value2);
                }
                List<DatabaseObjectType> list2 = [];
                array = attributeValue11.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
                foreach (string text2 in array) {
                    if (!dictionary.TryGetValue(text2, out DatabaseObjectType value3)) {
                        throw new InvalidOperationException($"Database object type \"{text2}\" not found.");
                    }
                    list2.Add(value3);
                }
                DatabaseObjectType value4 = null;
                if (!string.IsNullOrEmpty(attributeValue12)
                    && !dictionary.TryGetValue(attributeValue12, out value4)) {
                    throw new InvalidOperationException($"Database object type \"{attributeValue12}\" not found.");
                }
                dictionary[attributeValue9].InitializeRelations(list, list2, value4);
            }
            foreach (XElement item3 in XmlUtils.FindChildElement(node, "Assemblies", true).Elements()) {
                string attributeValue13 = XmlUtils.GetAttributeValue<string>(item3, "Name");
                try {
                    Assembly.Load(new AssemblyName(attributeValue13));
                }
                catch (Exception ex) {
                    Log.Warning($"Error loading assembly {attributeValue13}. {ex}");
                }
            }
            XElement node2 = XmlUtils.FindChildElement(node, "DatabaseObjects", true);
            Database database = new(
                new DatabaseObject(
                    guid: XmlUtils.GetAttributeValue<Guid>(node2, "RootGuid"),
                    databaseObjectType: dictionary["Root"],
                    name: "Root",
                    value: null
                ),
                dictionary.Values
            );
            foreach (DatabaseObject item4 in LoadDatabaseObjectsList(node2, database)) {
                item4.NestingParent = database.Root;
            }
            return database;
        }

        public static List<DatabaseObject> LoadDatabaseObjectsList(XElement node, Database database) =>
            LoadDatabaseObjectsList(node, database, false);

        public static List<DatabaseObject> LoadDatabaseObjectsList(XElement node, Database database, bool generateNewGuids) {
            Dictionary<DatabaseObject, Guid> dictionary = [];
            Dictionary<DatabaseObject, Guid> dictionary2 = [];
            Dictionary<Guid, Guid> dictionary3 = generateNewGuids ? [] : null;
            List<DatabaseObject> list = InternalLoadDatabaseObjectsList(node, database, dictionary, dictionary2, dictionary3);
            Dictionary<Guid, DatabaseObject> dictionary4 = [];
            foreach (DatabaseObject item in list) {
                dictionary4.Add(item.Guid, item);
                foreach (DatabaseObject explicitNestingChild in item.GetExplicitNestingChildren(null, false)) {
                    dictionary4.Add(explicitNestingChild.Guid, explicitNestingChild);
                }
            }
            foreach (KeyValuePair<DatabaseObject, Guid> item2 in dictionary) {
                Guid key = item2.Value;
                if (dictionary3 != null
                    && dictionary3.TryGetValue(key, out Guid value)) {
                    key = value;
                }
                if (!dictionary4.TryGetValue(key, out DatabaseObject value2)) {
                    throw new InvalidOperationException($"Required nesting parent {item2.Value} not found in database objects list.");
                }
                item2.Key.NestingParent = value2;
            }
            foreach (KeyValuePair<DatabaseObject, Guid> item3 in dictionary2) {
                Guid guid = item3.Value;
                if (dictionary3 != null
                    && dictionary3.TryGetValue(guid, out Guid value3)) {
                    guid = value3;
                }
                item3.Key.ExplicitInheritanceParent = dictionary4.TryGetValue(guid, out DatabaseObject value4)
                    ? value4
                    : database.FindDatabaseObject(guid, null, true);
            }
            return list.Where(x => x.NestingParent == null).ToList();
        }

        public static DatabaseObject LoadDatabaseObject(XElement node, Database database) {
            Dictionary<DatabaseObject, Guid> dictionary = [];
            DatabaseObject result = InternalLoadDatabaseObject(node, database, null, dictionary, null);
            foreach (KeyValuePair<DatabaseObject, Guid> item in dictionary) {
                item.Key.ExplicitInheritanceParent = database.FindDatabaseObject(item.Value, null, true);
            }
            return result;
        }

        public static void SaveDatabase(XElement node, Database database) {
            XElement parentNode = XmlUtils.AddElement(node, "DatabaseObjectTypes");
            foreach (DatabaseObjectType databaseObjectType in database.DatabaseObjectTypes) {
                XElement node2 = XmlUtils.AddElement(parentNode, "DatabaseObjectType");
                XmlUtils.SetAttributeValue(node2, "Name", databaseObjectType.Name);
                XmlUtils.SetAttributeValue(node2, "DefaultInstanceName", databaseObjectType.DefaultInstanceName);
                XmlUtils.SetAttributeValue(
                    node2,
                    "IconName",
                    !string.IsNullOrEmpty(databaseObjectType.IconName) ? databaseObjectType.IconName : string.Empty
                );
                XmlUtils.SetAttributeValue(node2, "Order", databaseObjectType.Order);
                XmlUtils.SetAttributeValue(node2, "SupportsValue", databaseObjectType.SupportsValue);
                XmlUtils.SetAttributeValue(node2, "MustInherit", databaseObjectType.MustInherit);
                XmlUtils.SetAttributeValue(node2, "NameLengthLimit", databaseObjectType.NameLengthLimit);
                XmlUtils.SetAttributeValue(node2, "SaveStandalone", databaseObjectType.SaveStandalone);
                XmlUtils.SetAttributeValue(
                    node2,
                    "AllowedNestingParents",
                    databaseObjectType.AllowedNestingParents.Aggregate(string.Empty, (r, d) => r.Length != 0 ? $"{r},{d.Name}" : d.Name)
                );
                XmlUtils.SetAttributeValue(
                    node2,
                    "AllowedInheritanceParents",
                    databaseObjectType.AllowedInheritanceParents.Aggregate(string.Empty, (r, d) => r.Length != 0 ? $"{r},{d.Name}" : d.Name)
                );
                XmlUtils.SetAttributeValue(
                    node2,
                    "NestedValueType",
                    databaseObjectType.NestedValueType != null ? databaseObjectType.NestedValueType.Name : string.Empty
                );
            }
            List<Type> list = [];
            database.FindUsedValueTypes(list);
            List<Assembly> list2 = [];
            foreach (Type item in list) {
                if (!list2.Contains(item.GetTypeInfo().Assembly)) {
                    list2.Add(item.GetTypeInfo().Assembly);
                }
            }
            list2.Sort((a1, a2) => string.CompareOrdinal(a1.FullName, a2.FullName));
            XElement parentNode2 = XmlUtils.AddElement(node, "Assemblies");
            foreach (Assembly item2 in list2) {
                XmlUtils.SetAttributeValue(XmlUtils.AddElement(parentNode2, "Assembly"), "Name", item2.GetName().Name);
            }
            XElement node3 = XmlUtils.AddElement(node, "DatabaseObjects");
            XmlUtils.SetAttributeValue(node3, "RootGuid", database.Root.Guid);
            SaveDatabaseObjectsList(node3, database.Root.GetExplicitNestingChildren(null, true));
        }

        public static void SaveDatabaseObjectsList(XElement node, IEnumerable<DatabaseObject> databaseObjects) {
            List<DatabaseObject> list = [];
            IEnumerable<DatabaseObject> enumerable = databaseObjects as DatabaseObject[] ?? databaseObjects.ToArray();
            foreach (DatabaseObject databaseObject in enumerable) {
                list.AddRange(from x in databaseObject.GetExplicitNestingChildren(null, false) where x.Type.SaveStandalone select x);
            }
            InternalSaveDatabaseObjectsList(node, list, true);
            InternalSaveDatabaseObjectsList(node, enumerable, false);
        }

        public static void SaveDatabaseObject(XElement node, DatabaseObject databaseObject) {
            InternalSaveDatabaseObject(node, databaseObject, false);
        }

        public static List<DatabaseObject> InternalLoadDatabaseObjectsList(XElement node,
            Database database,
            Dictionary<DatabaseObject, Guid> nestingParents,
            Dictionary<DatabaseObject, Guid> inheritanceParents,
            Dictionary<Guid, Guid> guidTranslation) {
            List<DatabaseObject> list = [];
            foreach (XElement item2 in node.Elements()) {
                DatabaseObject item = InternalLoadDatabaseObject(item2, database, nestingParents, inheritanceParents, guidTranslation);
                list.Add(item);
            }
            return list;
        }

        public static DatabaseObject InternalLoadDatabaseObject(XElement node,
            Database database,
            Dictionary<DatabaseObject, Guid> nestingParents,
            Dictionary<DatabaseObject, Guid> inheritanceParents,
            Dictionary<Guid, Guid> guidTranslation) {
            Guid guid = XmlUtils.GetAttributeValue(node, "Guid", Guid.Empty);
            string attributeValue = XmlUtils.GetAttributeValue(node, "Name", string.Empty);
            string attributeValue2 = XmlUtils.GetAttributeValue(node, "Description", string.Empty);
            Guid attributeValue3 = XmlUtils.GetAttributeValue(node, "NestingParent", Guid.Empty);
            Guid attributeValue4 = XmlUtils.GetAttributeValue(node, "InheritanceParent", Guid.Empty);
            string attributeValue5 = XmlUtils.GetAttributeValue(node, "Type", string.Empty);
            if (guid == Guid.Empty) {
                guid = Guid.NewGuid();
            }
            if (guidTranslation != null) {
                Guid guid2 = Guid.NewGuid();
                guidTranslation.Add(guid, guid2);
                guid = guid2;
            }
            DatabaseObjectType databaseObjectType = database.FindDatabaseObjectType(node.Name.ToString(), true);
            object value = null;
            if (!string.IsNullOrEmpty(attributeValue5)) {
                Type type = TypeCache.FindType(attributeValue5, false, true);
                value = XmlUtils.GetAttributeValue(node, "Value", type);
            }
            DatabaseObject databaseObject = new(databaseObjectType, guid, attributeValue, value) { Description = attributeValue2 };
            if (nestingParents != null
                && attributeValue3 != Guid.Empty) {
                nestingParents.Add(databaseObject, attributeValue3);
            }
            if (inheritanceParents != null
                && attributeValue4 != Guid.Empty) {
                inheritanceParents.Add(databaseObject, attributeValue4);
            }
            foreach (DatabaseObject item in InternalLoadDatabaseObjectsList(node, database, nestingParents, inheritanceParents, guidTranslation)) {
                item.NestingParent = databaseObject;
            }
            return databaseObject;
        }

        public static void InternalSaveDatabaseObjectsList(XElement node, IEnumerable<DatabaseObject> databaseObjects, bool saveNestingParents) {
            List<DatabaseObject> list = new(databaseObjects);
            list.Sort((o1, o2) => o1.Type.Order != o2.Type.Order ? o1.Type.Order - o2.Type.Order : o1.Guid.CompareTo(o2.Guid));
            foreach (DatabaseObject item in list) {
                InternalSaveDatabaseObject(XmlUtils.AddElement(node, item.Type.Name), item, saveNestingParents);
            }
        }

        public static void InternalSaveDatabaseObject(XElement node, DatabaseObject databaseObject, bool saveNestingParent) {
            XmlUtils.SetAttributeValue(node, "Name", databaseObject.Name);
            if (!string.IsNullOrEmpty(databaseObject.Description)) {
                XmlUtils.SetAttributeValue(node, "Description", databaseObject.Description);
            }
            XmlUtils.SetAttributeValue(node, "Guid", databaseObject.Guid);
            if (databaseObject.Value != null) {
                XmlUtils.SetAttributeValue(node, "Value", databaseObject.Value);
                XmlUtils.SetAttributeValue(node, "Type", TypeCache.GetShortTypeName(databaseObject.Value.GetType().FullName));
            }
            if (databaseObject.ExplicitInheritanceParent != null) {
                XmlUtils.SetAttributeValue(node, "InheritanceParent", databaseObject.ExplicitInheritanceParent.Guid);
            }
            if (saveNestingParent && databaseObject.NestingParent != null) {
                XmlUtils.SetAttributeValue(node, "NestingParent", databaseObject.NestingParent.Guid);
            }
            InternalSaveDatabaseObjectsList(
                node,
                from x in databaseObject.GetExplicitNestingChildren(null, true) where !x.Type.SaveStandalone select x,
                false
            );
        }
    }
}