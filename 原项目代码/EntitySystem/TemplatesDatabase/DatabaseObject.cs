using System;
using System.Collections.Generic;

namespace TemplatesDatabase {
    public class DatabaseObject {
        public class StringBin {
            public int m_mask;

            public List<string> m_list = [];

            public bool Contains(string s) {
                int num = Hash(s) & 0x1F;
                int num2 = 1 << num;
                if ((m_mask & num2) != 0) {
                    return m_list.Contains(s);
                }
                return false;
            }

            public void Add(string s) {
                int num = Hash(s) & 0x1F;
                int num2 = 1 << num;
                m_mask |= num2;
                m_list.Add(s);
            }

            static int Hash(string s) {
                int length = s.Length;
                return s[0] + s[length >> 1] + s[length - 1];
            }
        }

        public Database m_database;

        public DatabaseObjectType m_databaseObjectType;

        public Guid m_guid;

        public string m_name;

        public object m_value;

        public string m_description = string.Empty;

#pragma warning disable CS0649
        bool m_readOnly;
#pragma warning restore CS0649

        public DatabaseObject m_explicitInheritanceParent;

        public DatabaseObject m_nestingParent;

        public List<DatabaseObject> m_nestingChildren;

        public Database Database => m_database;

        public DatabaseObjectType Type => m_databaseObjectType;

        public Guid Guid => m_guid;

        public string Name {
            get => m_name;
            set {
                if (m_readOnly) {
                    throw new InvalidOperationException("Cannot change name of a read-only database object.");
                }
                if (value != m_name) {
                    if (value.Length > Type.NameLengthLimit) {
                        throw new InvalidOperationException(
                            $"Name \"{value}\" is too long, maximum name length for database object of type \"{Type.Name}\" is {Type.NameLengthLimit}."
                        );
                    }
                    if (NestingParent != null) {
                        foreach (DatabaseObject explicitNestingChild in NestingParent.GetExplicitNestingChildren(null, true)) {
                            if (explicitNestingChild.Name == value) {
                                throw new InvalidOperationException(
                                    $"Database object \"{explicitNestingChild.Name}\" is already nested in parent database object \"{NestingParent.Name}\"."
                                );
                            }
                        }
                    }
                    m_name = value;
                }
            }
        }

        public string Description {
            get => m_description;
            set {
                if (m_readOnly) {
                    throw new InvalidOperationException("Cannot change description of a read-only database object.");
                }
                if (value == null) {
                    throw new ArgumentNullException(nameof(value), "Description cannot be null.");
                }
                m_description = value;
            }
        }

        public object Value {
            get => m_value;
            set {
                if (m_readOnly) {
                    throw new InvalidOperationException("Cannot change value of a read-only database object.");
                }
                if (!Type.SupportsValue) {
                    throw new InvalidOperationException($"Database objects of type \"{Type.Name}\" do not support values.");
                }
                if (value == null) {
                    throw new ArgumentNullException(nameof(value), "Value cannot be null.");
                }
                m_value = value;
            }
        }

        public bool ReadOnly => m_readOnly;

        public DatabaseObject NestingParent {
            get => m_nestingParent;
            set {
                if (m_readOnly) {
                    throw new InvalidOperationException("Cannot change nesting parent of a read-only database object.");
                }
                if (value == m_nestingParent) {
                    return;
                }
                if (value != null) {
                    if (m_database != null
                        && m_database.Root == this) {
                        throw new InvalidOperationException("Root database object cannot be nested.");
                    }
                    if (!Type.AllowedNestingParents.Contains(value.Type)) {
                        throw new InvalidOperationException($"Database object of type {Type.Name} cannot be nested in {value.Type.Name}.");
                    }
                    if (value == this
                        || value.EffectivelyInheritsFrom(this)
                        || EffectivelyInheritsFrom(value)
                        || value.IsNestedIn(this)) {
                        throw new InvalidOperationException(
                            $"Cannot set nesting parent of database object \"{Name}\" to database object \"{value.Name}\" because it would create recursive nesting/inheritance."
                        );
                    }
                    if (value.FindExplicitNestedChild(Name, null, true, false) != null) {
                        throw new InvalidOperationException(
                            $"Another database object with name \"{Name}\" is already nested in database object \"{value.Name}\"."
                        );
                    }
                }
                if (m_nestingParent != null) {
                    if (!m_nestingParent.InternalNestingChildren.Remove(this)) {
                        throw new InvalidOperationException("DatabaseObject internal error: nested DatabaseObject not found in container.");
                    }
                    m_nestingParent = null;
                    if (m_database != null) {
                        m_database.RemoveDatabaseObject(this);
                    }
                }
                if (value != null) {
                    if (value.m_database != null) {
                        value.m_database.AddDatabaseObject(this, true);
                    }
                    m_nestingParent = value;
                    m_nestingParent.InternalNestingChildren.Add(this);
                }
            }
        }

        public DatabaseObject NestingRoot {
            get {
                if (m_nestingParent == null) {
                    return this;
                }
                return m_nestingParent.NestingRoot;
            }
        }

        public DatabaseObject ExplicitInheritanceParent {
            get => m_explicitInheritanceParent;
            set {
                if (m_readOnly) {
                    throw new InvalidOperationException("Cannot change inheritance parent of a read-only database object.");
                }
                if (value == m_explicitInheritanceParent) {
                    return;
                }
                if (value != null) {
                    if (value == this
                        || value.EffectivelyInheritsFrom(this)
                        || value.IsNestedIn(this)
                        || IsNestedIn(value)) {
                        throw new InvalidOperationException(
                            $"Cannot set inheritance parent of database object \"{Name}\" to database object \"{value.Name}\" because it would create recursive nesting/inheritance."
                        );
                    }
                    if (!Type.AllowedInheritanceParents.Contains(value.Type)) {
                        throw new InvalidOperationException($"Database object of type {Type.Name} cannot inherit from {value.Type.Name}.");
                    }
                }
                m_explicitInheritanceParent = value;
            }
        }

        public DatabaseObject ExplicitInheritanceRoot {
            get {
                if (m_explicitInheritanceParent == null) {
                    return this;
                }
                return m_explicitInheritanceParent.ExplicitInheritanceRoot;
            }
        }

        public DatabaseObject ImplicitInheritanceParent {
            get {
                if (NestingParent != null) {
                    return NestingParent.EffectiveInheritanceParent?.FindEffectiveNestedChild(Name, null, true, false);
                }
                return null;
            }
        }

        public DatabaseObject ImplicitInheritanceRoot {
            get {
                DatabaseObject implicitInheritanceParent = ImplicitInheritanceParent;
                if (implicitInheritanceParent == null) {
                    return this;
                }
                return implicitInheritanceParent.ImplicitInheritanceRoot;
            }
        }

        public DatabaseObject EffectiveInheritanceParent {
            get {
                if (m_explicitInheritanceParent == null) {
                    return ImplicitInheritanceParent;
                }
                return m_explicitInheritanceParent;
            }
        }

        public DatabaseObject EffectiveInheritanceRoot {
            get {
                DatabaseObject effectiveInheritanceParent = EffectiveInheritanceParent;
                if (effectiveInheritanceParent == null) {
                    return this;
                }
                return effectiveInheritanceParent.EffectiveInheritanceRoot;
            }
        }

        List<DatabaseObject> InternalNestingChildren {
            get {
                if (m_nestingChildren == null) {
                    m_nestingChildren = [];
                }
                return m_nestingChildren;
            }
        }

        public DatabaseObject(DatabaseObjectType databaseObjectType, Guid guid, string name, object value) {
            if (!databaseObjectType.IsInitialized) {
                throw new InvalidOperationException($"InitializeRelations of DatabaseObjectType \"{databaseObjectType.Name}\" not called.");
            }
            m_databaseObjectType = databaseObjectType;
            m_guid = guid;
            Name = name;
            if (value != null) {
                Value = value;
            }
        }

        public DatabaseObject(DatabaseObjectType databaseObjectType, string name, object value) : this(
            databaseObjectType,
            Guid.NewGuid(),
            name,
            value
        ) { }

        public DatabaseObject(DatabaseObjectType databaseObjectType, string name) : this(databaseObjectType, Guid.NewGuid(), name, null) { }

        public bool IsNestedIn(DatabaseObject databaseObject) {
            if (NestingParent == null) {
                return false;
            }
            if (NestingParent != databaseObject) {
                return NestingParent.IsNestedIn(databaseObject);
            }
            return true;
        }

        public IEnumerable<DatabaseObject> GetExplicitNestingChildren(DatabaseObjectType type, bool directChildrenOnly) {
            foreach (DatabaseObject databaseObject in InternalNestingChildren) {
                if (type == null
                    || databaseObject.Type == type) {
                    yield return databaseObject;
                }
                if (!directChildrenOnly) {
                    foreach (DatabaseObject explicitNestingChild in databaseObject.GetExplicitNestingChildren(type, false)) {
                        yield return explicitNestingChild;
                    }
                }
            }
        }

        public DatabaseObject FindExplicitNestedChild(string name, DatabaseObjectType type, bool directChildrenOnly, bool throwIfNotFound) {
            foreach (DatabaseObject explicitNestingChild in GetExplicitNestingChildren(type, directChildrenOnly)) {
                if (explicitNestingChild.Name == name) {
                    return explicitNestingChild;
                }
            }
            if (throwIfNotFound) {
                throw new InvalidOperationException($"Required database object \"{name}\" not found in database object \"{Name}\"");
            }
            return null;
        }

        public bool ExplicitlyInheritsFrom(DatabaseObject databaseObject) {
            DatabaseObject explicitInheritanceParent = ExplicitInheritanceParent;
            if (explicitInheritanceParent != null) {
                if (explicitInheritanceParent != databaseObject) {
                    return explicitInheritanceParent.ExplicitlyInheritsFrom(databaseObject);
                }
                return true;
            }
            return false;
        }

        public bool ImplicitlyInheritsFrom(DatabaseObject databaseObject) {
            DatabaseObject implicitInheritanceParent = ImplicitInheritanceParent;
            if (implicitInheritanceParent != null) {
                if (implicitInheritanceParent != databaseObject) {
                    return implicitInheritanceParent.ImplicitlyInheritsFrom(databaseObject);
                }
                return true;
            }
            return false;
        }

        public bool EffectivelyInheritsFrom(DatabaseObject databaseObject) {
            DatabaseObject effectiveInheritanceParent = EffectiveInheritanceParent;
            if (effectiveInheritanceParent != null) {
                if (effectiveInheritanceParent != databaseObject) {
                    return effectiveInheritanceParent.EffectivelyInheritsFrom(databaseObject);
                }
                return true;
            }
            return false;
        }

        public IEnumerable<DatabaseObject> GetEffectiveNestingChildren(DatabaseObjectType type, bool directChildrenOnly) {
            if (directChildrenOnly) {
                foreach (DatabaseObject item in InternalGetEffectiveNestingChildren(new StringBin(), type)) {
                    if (type == null
                        || item.Type == type) {
                        yield return item;
                    }
                }
            }
            else {
                foreach (DatabaseObject databaseObject in GetEffectiveNestingChildren(null, true)) {
                    if (type == null
                        || databaseObject.Type == type) {
                        yield return databaseObject;
                    }
                    foreach (DatabaseObject effectiveNestingChild in databaseObject.GetEffectiveNestingChildren(type, false)) {
                        yield return effectiveNestingChild;
                    }
                }
            }
        }

        public DatabaseObject FindEffectiveNestedChild(string name, DatabaseObjectType type, bool directChildrenOnly, bool throwIfNotFound) {
            foreach (DatabaseObject effectiveNestingChild in GetEffectiveNestingChildren(type, directChildrenOnly)) {
                if (effectiveNestingChild.Name == name) {
                    return effectiveNestingChild;
                }
            }
            if (throwIfNotFound) {
                throw new InvalidOperationException($"Required database object \"{name}\" not found in database object \"{Name}\"");
            }
            return null;
        }

        public T GetNestedValue<T>(string name) {
            DatabaseObject databaseObject = FindEffectiveNestedChild(name, Type.NestedValueType, true, true);
            return CastValue<T>(databaseObject);
        }

        public T GetNestedValue<T>(string name, T defaultValue) {
            DatabaseObject databaseObject = FindEffectiveNestedChild(name, Type.NestedValueType, true, false);
            if (databaseObject == null) {
                return defaultValue;
            }
            return CastValue<T>(databaseObject);
        }

        public void SetNestedValue<T>(string name, T value) {
            DatabaseObject databaseObject = FindEffectiveNestedChild(name, Type.NestedValueType, true, false);
            if (databaseObject == null
                || databaseObject.NestingParent != this) {
                new DatabaseObject(Type.NestedValueType, Guid.Empty, name, value).NestingParent = this;
            }
            else {
                databaseObject.Value = value;
            }
        }

        public override string ToString() {
            if (NestingParent != null) {
                return $"{Name} in {NestingParent}";
            }
            return $"{Name}";
        }

        T CastValue<T>(DatabaseObject databaseObject) {
            if (databaseObject.Value != null
                && !(databaseObject.Value is T)) {
                throw new Exception(
                    $"Database object \"{databaseObject.Name}\" has invalid type \"{databaseObject.Value.GetType().FullName}\", required type is \"{typeof(T).FullName}\"."
                );
            }
            return (T)databaseObject.Value;
        }

        IEnumerable<DatabaseObject> InternalGetEffectiveNestingChildren(StringBin names, DatabaseObjectType type) {
            if (Type.AllowedNestingChildren.Count != 0) {
                foreach (DatabaseObject explicitNestingChild in GetExplicitNestingChildren(type, true)) {
                    if (!names.Contains(explicitNestingChild.Name)) {
                        names.Add(explicitNestingChild.Name);
                        yield return explicitNestingChild;
                    }
                }
                DatabaseObject effectiveInheritanceParent = EffectiveInheritanceParent;
                if (effectiveInheritanceParent != null) {
                    foreach (DatabaseObject item in effectiveInheritanceParent.InternalGetEffectiveNestingChildren(names, type)) {
                        yield return item;
                    }
                }
            }
        }
    }
}