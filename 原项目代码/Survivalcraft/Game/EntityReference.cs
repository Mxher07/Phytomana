using System.Globalization;
using GameEntitySystem;

namespace Game {
    public struct EntityReference {
        public enum ReferenceType {
            Null = 0,
            Local = 1,
            ByEntityId = 2,
            ByEntityName = 3
        }

        public ReferenceType m_referenceType;

        public string m_entityReference;

        public string m_componentReference;

        public string ReferenceString {
            get {
                if (m_referenceType == ReferenceType.Null) {
                    return "null:";
                }
                if (m_referenceType == ReferenceType.Local) {
                    return $"local:{m_componentReference}";
                }
                if (m_referenceType == ReferenceType.ByEntityId) {
                    return $"id:{m_entityReference}:{m_componentReference}";
                }
                if (m_referenceType == ReferenceType.ByEntityName) {
                    return $"name:{m_entityReference}:{m_componentReference}";
                }
                throw new Exception("Unknown entity reference type.");
            }
        }

        public static EntityReference Null => default;

        public Entity GetEntity(Entity localEntity, IdToEntityMap idToEntityMap, bool throwIfNotFound) {
            Entity entity;
            if (m_referenceType == ReferenceType.Null) {
                entity = null;
            }
            else if (m_referenceType == ReferenceType.Local) {
                entity = localEntity;
            }
            else if (m_referenceType == ReferenceType.ByEntityId) {
                int id = int.Parse(m_entityReference, CultureInfo.InvariantCulture);
                entity = localEntity.Project.FindEntity(id);
            }
            else {
                if (m_referenceType != ReferenceType.ByEntityName) {
                    throw new Exception("Unknown entity reference type.");
                }
                entity = localEntity.Project.FindSubsystem<SubsystemNames>(true).FindEntityByName(m_entityReference);
            }
            if (entity != null) {
                return entity;
            }
            if (throwIfNotFound) {
                throw new Exception($"Required entity \"{ReferenceString}\" not found.");
            }
            return null;
        }

        public T GetComponent<T>(Entity localEntity, IdToEntityMap idToEntityMap, bool throwIfNotFound) where T : class {
            Entity entity = GetEntity(localEntity, idToEntityMap, throwIfNotFound);
            if (entity == null) {
                return null;
            }
            return entity.FindComponent<T>(m_componentReference, throwIfNotFound);
        }

        public bool IsNullOrEmpty() {
            if (m_referenceType != 0
                && (m_referenceType != ReferenceType.Local || !string.IsNullOrEmpty(m_componentReference))
                && (m_referenceType != ReferenceType.ByEntityId || !(m_entityReference == "0"))) {
                if (m_referenceType == ReferenceType.ByEntityName) {
                    return string.IsNullOrEmpty(m_entityReference);
                }
                return false;
            }
            return true;
        }

        public static EntityReference Local(Component component) {
            EntityReference result = default;
            result.m_referenceType = ReferenceType.Local;
            result.m_componentReference = component != null ? component.ValuesDictionary.DatabaseObject.Name : string.Empty;
            return result;
        }

        public static EntityReference FromId(Component component) {
            if (component?.Entity == null) {
                return default;
            }
            EntityReference result = FromId(component.Entity);
            result.m_componentReference = component != null ? component.ValuesDictionary.DatabaseObject.Name : string.Empty;
            return result;
        }

        public static EntityReference FromId(Entity entity) {
            int num = entity.Id;
            EntityReference result = default;
            result.m_referenceType = ReferenceType.ByEntityId;
            result.m_entityReference = num.ToString(CultureInfo.InvariantCulture);
            result.m_componentReference = string.Empty;
            return result;
        }

        public static EntityReference FromId(Entity entity, EntityToIdMap entityToIdMap) => FromId(entity);

        public static EntityReference FromId(Component component, EntityToIdMap entityToIdMap) => FromId(component);

        public static EntityReference FromName(Component component) {
            string entityReference = component != null ? component.Entity.FindComponent<ComponentName>(null, true).Name : string.Empty;
            EntityReference result = default;
            result.m_referenceType = ReferenceType.ByEntityName;
            result.m_entityReference = entityReference;
            result.m_componentReference = component != null ? component.ValuesDictionary.DatabaseObject.Name : string.Empty;
            return result;
        }

        public static EntityReference FromName(Entity entity) {
            string entityReference = entity != null ? entity.FindComponent<ComponentName>(null, true).Name : string.Empty;
            EntityReference result = default;
            result.m_referenceType = ReferenceType.ByEntityName;
            result.m_entityReference = entityReference;
            result.m_componentReference = string.Empty;
            return result;
        }

        public static EntityReference FromReferenceString(string referenceString) {
            EntityReference result = default;
            if (string.IsNullOrEmpty(referenceString)) {
                result.m_referenceType = ReferenceType.Null;
                result.m_entityReference = string.Empty;
                result.m_componentReference = string.Empty;
            }
            else {
                string[] array = referenceString.Split(':');
                if (array.Length == 1) {
                    result.m_referenceType = ReferenceType.Local;
                    result.m_entityReference = string.Empty;
                    result.m_componentReference = array[0];
                }
                else {
                    if (array.Length != 2
                        && array.Length != 3) {
                        throw new Exception("Invalid entity reference. Too many tokens.");
                    }
                    if (array[0] == "null"
                        && array.Length == 2) {
                        result.m_referenceType = ReferenceType.Null;
                        result.m_entityReference = string.Empty;
                        result.m_componentReference = string.Empty;
                    }
                    else if (array[0] == "local"
                        && array.Length == 2) {
                        result.m_referenceType = ReferenceType.Local;
                        result.m_componentReference = array[1];
                    }
                    else if (array[0] == "id") {
                        result.m_referenceType = ReferenceType.ByEntityId;
                        result.m_entityReference = array[1];
                        result.m_componentReference = array.Length == 3 ? array[2] : string.Empty;
                    }
                    else {
                        if (!(array[0] == "name")) {
                            throw new Exception("Unknown entity reference type.");
                        }
                        result.m_referenceType = ReferenceType.ByEntityId;
                        result.m_entityReference = array[1];
                        result.m_componentReference = array.Length == 3 ? array[2] : string.Empty;
                    }
                }
            }
            return result;
        }
    }
}