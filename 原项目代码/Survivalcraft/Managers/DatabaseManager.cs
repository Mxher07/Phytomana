using System.Reflection;
using System.Xml.Linq;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public static class DatabaseManager {
        public static GameDatabase m_gameDatabase;

        public static Dictionary<string, ValuesDictionary> m_valueDictionaries = [];

        public static GameDatabase GameDatabase {
            get {
                if (m_gameDatabase != null) {
                    return m_gameDatabase;
                }
                throw new InvalidOperationException("Database not loaded.");
            }
        }

        public static ICollection<ValuesDictionary> EntitiesValuesDictionaries => m_valueDictionaries.Values;

        public static XElement DatabaseNode;

        public static void Initialize() {
            DatabaseNode = null;
            m_valueDictionaries.Clear();
        }

        public static void LoadDataBaseFromXml(XElement node) {
            m_valueDictionaries.Clear();
            m_gameDatabase = new GameDatabase(XmlDatabaseSerializer.LoadDatabase(node));
            foreach (DatabaseObject explicitNestingChild in GameDatabase.Database.Root.GetExplicitNestingChildren(
                    GameDatabase.EntityTemplateType,
                    false
                )) {
                ValuesDictionary valuesDictionary = new();
                valuesDictionary.PopulateFromDatabaseObject(explicitNestingChild);
                m_valueDictionaries.Add(explicitNestingChild.Name, valuesDictionary);
            }
        }

        public static ValuesDictionary FindEntityValuesDictionary(string entityTemplateName, bool throwIfNotFound) {
            if (!m_valueDictionaries.TryGetValue(entityTemplateName, out ValuesDictionary value) && throwIfNotFound) {
                throw new InvalidOperationException($"EntityTemplate \"{entityTemplateName}\" not found.");
            }
            return value;
        }

        public static ValuesDictionary FindValuesDictionaryForComponent(ValuesDictionary entityVd, Type componentType) {
            foreach (ValuesDictionary item in entityVd.Values.OfType<ValuesDictionary>()) {
                if (item.DatabaseObject.Type == GameDatabase.MemberComponentTemplateType) {
                    Type type = TypeCache.FindType(item.GetValue<string>("Class"), true, true);
                    if (componentType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo())) {
                        return item;
                    }
                }
            }
            return null;
        }

        [Obsolete]
        public static Entity CreateEntity(Project project, SpawnEntityData spawnEntityData, bool throwIfNotFound) {
            Entity entity = CreateEntity(project, spawnEntityData.TemplateName, throwIfNotFound);
            if (entity != null) {
                if (spawnEntityData.EntityId != 0) {
                    entity.Id = spawnEntityData.EntityId;
                }
                else {
                    entity.Id = 0;
                }
            }
            return entity;
        }

        public static Entity CreateEntity(Project project, string entityTemplateName, bool throwIfNotFound) {
            ValuesDictionary valuesDictionary = FindEntityValuesDictionary(entityTemplateName, throwIfNotFound);
            if (valuesDictionary == null) {
                return null;
            }
            return project.CreateEntity(valuesDictionary);
        }

        public static Entity CreateEntity(Project project, string entityTemplateName, ValuesDictionary overrides, bool throwIfNotFound) {
            ValuesDictionary valuesDictionary = FindEntityValuesDictionary(entityTemplateName, throwIfNotFound);
            if (valuesDictionary != null) {
                valuesDictionary.ApplyOverrides(overrides);
                return project.CreateEntity(valuesDictionary);
            }
            return null;
        }
    }
}