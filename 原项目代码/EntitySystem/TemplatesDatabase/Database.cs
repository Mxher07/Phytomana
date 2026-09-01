using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace TemplatesDatabase {
    public class Database {
        public DatabaseObject m_root;

        public ReadOnlyList<DatabaseObjectType> m_databaseObjectTypes;

        public Dictionary<Guid, DatabaseObject> m_databaseObjectsByGuid = [];

        public IList<DatabaseObjectType> DatabaseObjectTypes => m_databaseObjectTypes;

        public DatabaseObject Root => m_root;

        public Database(DatabaseObject root, IEnumerable<DatabaseObjectType> databaseObjectTypes) {
            List<DatabaseObjectType> objectTypes = databaseObjectTypes.ToList();
            if (!objectTypes.Contains(root.Type)) {
                throw new Exception("Database root has invalid database object type.");
            }
            if (root.NestingParent != null) {
                throw new Exception("Database root cannot be nested.");
            }
            m_databaseObjectTypes = new ReadOnlyList<DatabaseObjectType>(objectTypes);
            m_root = root;
            m_root.m_database = this;
        }

        public DatabaseObjectType FindDatabaseObjectType(string name, bool throwIfNotFound) {
            foreach (DatabaseObjectType databaseObjectType in m_databaseObjectTypes) {
                if (databaseObjectType.Name == name) {
                    return databaseObjectType;
                }
            }
            if (throwIfNotFound) {
                throw new Exception($"Required database object type \"{name}\" not found.");
            }
            return null;
        }

        public DatabaseObject FindDatabaseObject(Guid guid, DatabaseObjectType type, bool throwIfNotFound) {
            m_databaseObjectsByGuid.TryGetValue(guid, out DatabaseObject value);
            if (value != null) {
                if (type != null
                    && value.Type != type) {
                    throw new InvalidOperationException($"Database object {guid} has invalid type. Expected {type.Name}, found {value.Type.Name}.");
                }
            }
            else if (throwIfNotFound) {
                throw new InvalidOperationException($"Required database object {guid} not found.");
            }
            return value;
        }

        public DatabaseObject FindDatabaseObject(string name, DatabaseObjectType type, bool throwIfNotFound) =>
            Root.FindExplicitNestedChild(name, type, false, throwIfNotFound);

        public void FindUsedValueTypes(List<Type> typesList) {
            foreach (DatabaseObject explicitNestingChild in Root.GetExplicitNestingChildren(null, false)) {
                if (explicitNestingChild.Value != null
                    && !typesList.Contains(explicitNestingChild.Value.GetType())) {
                    typesList.Add(explicitNestingChild.Value.GetType());
                }
            }
        }

        internal void AddDatabaseObject(DatabaseObject databaseObject, bool checkThatGuidsAreUnique) {
            if (databaseObject.m_database != null) {
                throw new InvalidOperationException("Internal error: database object is already in a database.");
            }
            if (!m_databaseObjectTypes.Contains(databaseObject.Type)) {
                throw new InvalidOperationException($"Database object type \"{databaseObject.Type.Name}\" is not supported by the database.");
            }
            if (checkThatGuidsAreUnique) {
                if (databaseObject.Guid != Guid.Empty
                    && m_databaseObjectsByGuid.ContainsKey(databaseObject.Guid)) {
                    throw new InvalidOperationException($"Database object {databaseObject.Guid} is already present in the database.");
                }
                foreach (DatabaseObject explicitNestingChild in databaseObject.GetExplicitNestingChildren(null, false)) {
                    if (explicitNestingChild.Guid != Guid.Empty
                        && m_databaseObjectsByGuid.ContainsKey(explicitNestingChild.Guid)) {
                        throw new InvalidOperationException($"Database object {explicitNestingChild.Guid} is already present in the database.");
                    }
                }
            }
            databaseObject.m_database = this;
            if (databaseObject.Guid != Guid.Empty) {
                m_databaseObjectsByGuid.Add(databaseObject.Guid, databaseObject);
            }
            foreach (DatabaseObject explicitNestingChild2 in databaseObject.GetExplicitNestingChildren(null, true)) {
                AddDatabaseObject(explicitNestingChild2, false);
            }
        }

        internal void RemoveDatabaseObject(DatabaseObject databaseObject) {
            if (databaseObject.m_database != this) {
                throw new InvalidOperationException("Internal error: database object is not in the database.");
            }
            databaseObject.m_database = null;
            if (databaseObject.Guid != Guid.Empty
                && !m_databaseObjectsByGuid.Remove(databaseObject.Guid)) {
                throw new InvalidOperationException("Internal error: database object not in dictionary.");
            }
            foreach (DatabaseObject explicitNestingChild in databaseObject.GetExplicitNestingChildren(null, true)) {
                RemoveDatabaseObject(explicitNestingChild);
            }
        }
    }
}