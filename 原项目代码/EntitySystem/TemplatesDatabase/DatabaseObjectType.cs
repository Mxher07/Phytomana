using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace TemplatesDatabase {
    public class DatabaseObjectType {
        public string m_name;

        public string m_defaultInstanceName;

        public string m_iconName;

        public int m_order;

        public bool m_supportsValue;

        public bool m_mustInherit;

        public int m_nameLengthLimit;

        public bool m_saveStandalone;

        public List<DatabaseObjectType> m_allowedNestingParents;

        public List<DatabaseObjectType> m_allowedInheritanceParents;

        public List<DatabaseObjectType> m_allowedNestingChildren = [];

        public List<DatabaseObjectType> m_allowedInheritanceChildren = [];

        public DatabaseObjectType m_nestedValueType;

        public bool IsInitialized => m_allowedNestingParents != null;

        public string Name => m_name;

        public string DefaultInstanceName => m_defaultInstanceName;

        public string IconName => m_iconName;

        public int Order => m_order;

        public bool SupportsValue => m_supportsValue;

        public bool MustInherit => m_mustInherit;

        public int NameLengthLimit => m_nameLengthLimit;

        public bool SaveStandalone => m_saveStandalone;

        public ReadOnlyList<DatabaseObjectType> AllowedNestingParents => new(m_allowedNestingParents);

        public ReadOnlyList<DatabaseObjectType> AllowedInheritanceParents => new(m_allowedInheritanceParents);

        public ReadOnlyList<DatabaseObjectType> AllowedNestingChildren => new(m_allowedNestingChildren);

        public ReadOnlyList<DatabaseObjectType> AllowedInheritanceChildren => new(m_allowedInheritanceChildren);

        public DatabaseObjectType NestedValueType => m_nestedValueType;

        public DatabaseObjectType(string name,
            string defaultInstanceName,
            string iconName,
            int order,
            bool supportsValue,
            bool mustInherit,
            int nameLengthLimit,
            bool saveStandalone) {
            m_name = name;
            m_defaultInstanceName = defaultInstanceName;
            m_iconName = iconName;
            m_order = order;
            m_supportsValue = supportsValue;
            m_mustInherit = mustInherit;
            m_nameLengthLimit = nameLengthLimit;
            m_saveStandalone = saveStandalone;
        }

        public void InitializeRelations(IEnumerable<DatabaseObjectType> allowedNestingParents,
            IEnumerable<DatabaseObjectType> allowedInheritanceParents,
            DatabaseObjectType nestedValueType) {
            if (IsInitialized) {
                throw new InvalidOperationException("InitializeRelations of this DatabaseObjectType was already called.");
            }
            m_nestedValueType = nestedValueType;
            if (allowedNestingParents != null) {
                m_allowedNestingParents = allowedNestingParents.Distinct().ToList();
                foreach (DatabaseObjectType allowedNestingParent in m_allowedNestingParents) {
                    allowedNestingParent.m_allowedNestingChildren.Add(this);
                }
            }
            else {
                m_allowedNestingParents = [];
            }
            if (allowedInheritanceParents != null) {
                m_allowedInheritanceParents = allowedInheritanceParents.Distinct().ToList();
                foreach (DatabaseObjectType allowedInheritanceParent in m_allowedInheritanceParents) {
                    allowedInheritanceParent.m_allowedInheritanceChildren.Add(this);
                }
            }
            else {
                m_allowedInheritanceParents = [];
            }
        }

        public override string ToString() => Name;
    }
}