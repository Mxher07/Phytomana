using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace GameEntitySystem {
    public class Project : IDisposable {
        public GameDatabase m_gameDatabase;

        public DatabaseObject m_projectTemplate;

        public List<Subsystem> m_subsystems = [];

        public Dictionary<Entity, bool> m_entities = [];

        public virtual GameDatabase GameDatabase => m_gameDatabase;

        public virtual DatabaseObject ProjectTemplate => m_projectTemplate;

        public virtual List<Subsystem> Subsystems => m_subsystems;

        public virtual Dictionary<Entity, bool>.KeyCollection Entities => m_entities.Keys;

        public static event EventHandler<EntityAddRemoveEventArgs> EntityAdded;

        public static event EventHandler<EntityAddRemoveEventArgs> EntityRemoved;

        public static event Action<Project> OnProjectLoad;

        public static event Action<Project> BeforeSubsystemsAndEntitiesLoad;

        public ProjectData m_projectData;

        public int NextEntityID = 1;

        public bool PostponeFireEntityAddedEvents = true;

        public Project() { }
        public Project(GameDatabase gameDatabase, ProjectData projectData) {
            try {
                m_gameDatabase = gameDatabase;
                m_projectData = projectData;
                m_projectTemplate = m_projectData.ValuesDictionary.DatabaseObject;
                Dictionary<string, Subsystem> dictionary = [];
                foreach (ValuesDictionary item in from x in projectData.ValuesDictionary.Values
                                                  select x as ValuesDictionary
                    into x
                                                  where x != null && x.DatabaseObject != null && x.DatabaseObject.Type == gameDatabase.MemberSubsystemTemplateType
                                                  select x) {
                    bool value = item.GetValue<bool>("IsOptional");
                    string value2 = item.GetValue<string>("Class");
                    Type type = TypeCache.FindType(value2, false, !value);
                    if (type != null) {
                        object obj;
                        try {
#pragma warning disable IL2072
                            obj = Activator.CreateInstance(type);
#pragma warning restore IL2072
                        }
                        catch (TargetInvocationException ex) {
                            if (ex.InnerException != null) {
                                throw ex.InnerException;
                            }
                            throw;
                        }
                        if (obj is not Subsystem subsystem) {
                            throw new InvalidOperationException(
                                $"Type \"{value2}\" cannot be used as a subsystem because it does not inherit from Subsystem class."
                            );
                        }
                        subsystem.Initialize(this, item);
                        dictionary.Add(item.DatabaseObject.Name, subsystem);
                        m_subsystems.Add(subsystem);
                    }
                }
                Dictionary<Subsystem, bool> loadedSubsystems = [];
                List<Entity> entities = new();
                if (projectData.EntityDataList != null) {
                    entities = InitializeEntities(projectData.EntityDataList);
                    NextEntityID = projectData.NextEntityID;
                    AddEntities(entities);
                }
                BeforeSubsystemsAndEntitiesLoad?.Invoke(this);
                foreach (Subsystem value3 in dictionary.Values) {
                    LoadSubsystem(value3, dictionary, loadedSubsystems, 0);
                }
                LoadEntities(projectData.EntityDataList, entities);
                OnProjectLoad?.Invoke(this);
            }
            catch (Exception e) {
                try {
                    Log.Error($"Loading World Failed!\n{e}");
                }
                catch {
                    // ignored
                }
                try {
                    Dispose();
                }
                catch (Exception e2) {
                    Log.Error(e2);
                }
                throw;
            }
        }

        public virtual Subsystem FindSubsystem(Type type, string name, bool throwOnError) {
            foreach (Subsystem subsystem in Subsystems) {
                if (type.GetTypeInfo().IsAssignableFrom(subsystem.GetType().GetTypeInfo())
                    && (name == null || subsystem.ValuesDictionary.DatabaseObject.Name == name)) {
                    return subsystem;
                }
            }
            if (throwOnError) {
                if (name != null) {
                    throw new Exception($"Required subsystem {type.FullName} with name \"{name}\" does not exist in project.");
                }
                throw new Exception($"Required subsystem {type.FullName} does not exist in project.");
            }
            return null;
        }

        public virtual Subsystem FindSubsystem(string name, bool throwOnError) {
            if (throwOnError) {
                if (string.IsNullOrEmpty(name)) {
                    throw new ArgumentNullException(nameof(name));
                }
            }
            foreach (Subsystem subsystem in Subsystems) {
                if (subsystem.ValuesDictionary.DatabaseObject.Name == name) {
                    return subsystem;
                }
            }
            if (throwOnError) {
                throw new Exception($"Required subsystem with name \"{name}\" does not exist in project.");
            }
            return null;
        }

        public virtual T FindSubsystem<T>() where T : class => FindSubsystem(typeof(T), null, false) as T;

        public virtual T FindSubsystem<T>(bool throwOnError) where T : class => FindSubsystem(typeof(T), null, throwOnError) as T;

        public virtual T FindSubsystem<T>(string name, bool throwOnError) where T : class => FindSubsystem(typeof(T), name, throwOnError) as T;

        public virtual IEnumerable<Subsystem> FindSubsystems(Type type) {
            foreach (Subsystem subsystem in Subsystems) {
                if (type.GetTypeInfo().IsAssignableFrom(subsystem.GetType().GetTypeInfo())) {
                    yield return subsystem;
                }
            }
        }

        public virtual IEnumerable<T> FindSubsystems<T>() where T : class {
            foreach (Subsystem subsystem in Subsystems) {
                if (subsystem is T val) {
                    yield return val;
                }
            }
        }

        public virtual Entity FindEntity(int EntityID) {
            return Entities.FirstOrDefault(entity => entity.Id == EntityID, null);
        }

        public virtual Entity CreateEntity(ValuesDictionary valuesDictionary, int entityId = 0) {
            try {
                Entity entity = new(this, valuesDictionary, entityId);
                IdToEntityMap idToEntityMap = new([]);
                entity.InternalLoadEntity(valuesDictionary, idToEntityMap);
                return entity;
            }
            catch (Exception innerException) {
                throw new Exception($"Error creating entity from template \"{valuesDictionary.DatabaseObject.Name}\".", innerException);
            }
        }

        public virtual void AddEntity(Entity entity) {
            if (entity.Project != this) {
                throw new Exception("Entity does not belong to this project.");
            }
            if (!entity.IsAddedToProject) {
                m_entities.Add(entity, true);
                if (entity.Id == 0) {
                    entity.Id = NextEntityID;
                    NextEntityID++;
                }
                entity.m_isAddedToProject = true;
                if (!PostponeFireEntityAddedEvents) {
                    FireEntityAddedEvents(entity);
                }
            }
        }

        public virtual void RemoveEntity(Entity entity, bool disposeEntity) {
            if (entity.Project != this) {
                throw new Exception("Entity does not belong to this project.");
            }
            if (entity.IsAddedToProject) {
                m_entities.Remove(entity);
                entity.m_isAddedToProject = false;
                FireEntityRemovedEvents(entity);
                if (disposeEntity) {
                    entity.Dispose();
                }
            }
        }

        public virtual void AddEntities(IEnumerable<Entity> entities) {
            foreach (Entity entity in entities) {
                AddEntity(entity);
            }
        }

        public virtual void RemoveEntities(IEnumerable<Entity> entities, bool disposeEntities) {
            foreach (Entity entity in entities) {
                RemoveEntity(entity, disposeEntities);
            }
        }

        public virtual List<Entity> InitializeEntities(EntityDataList entityDataList) {
            List<Entity> list = new(entityDataList.EntitiesData.Count);
            Dictionary<int, Entity> dictionary = [];
            foreach (EntityData entitiesDatum in entityDataList.EntitiesData) {
                try {
                    Entity entity = new(this, entitiesDatum.ValuesDictionary, entitiesDatum.Id);
                    list.Add(entity);
                    if (entitiesDatum.Id != 0) {
                        if (dictionary.ContainsKey(entitiesDatum.Id)) {
                            Log.Warning($"Multiple Entities use the same ID{entitiesDatum.Id}");
                        }
                        dictionary[entitiesDatum.Id] = entity;
                    }
                }
                catch (Exception innerException) {
                    throw new Exception(
                        $"Error creating entity from template \"{entitiesDatum.ValuesDictionary.DatabaseObject.Name}\".",
                        innerException
                    );
                }
            }
            return list;
        }

        public virtual void LoadEntities(EntityDataList entityDataList, List<Entity> entityList) {
            int num = 0;
            if (entityDataList?.EntitiesData != null) {
                foreach (EntityData entitiesDatum2 in entityDataList.EntitiesData) {
                    entityList[num].InternalLoadEntity(entitiesDatum2.ValuesDictionary, null);
                    num++;
                }
            }
            if (Entities != null) {
                foreach (Entity entity in Entities) {
                    FireEntityAddedEvents(entity);
                }
            }
            PostponeFireEntityAddedEvents = false;
        }

        public virtual EntityDataList SaveEntities(IEnumerable<Entity> entities) {
            IEnumerable<Entity> enumerable = entities as Entity[] ?? entities.ToArray();
            Dictionary<Entity, bool> dictionary = DetermineNotOwnedEntities(enumerable);
            int num = 1;
            Dictionary<Entity, int> dictionary2 = [];
            EntityToIdMap entityToIdMap = new(dictionary2);
            foreach (Entity key in dictionary.Keys) {
                dictionary2.Add(key, num);
                num++;
            }
            EntityDataList entityDataList = new() { EntitiesData = new List<EntityData>(dictionary.Keys.Count) };
            foreach (Entity key2 in enumerable) {
                EntityData entityData = new() { Id = key2.Id, ValuesDictionary = [] };
                entityData.ValuesDictionary.DatabaseObject = key2.ValuesDictionary.DatabaseObject;
                key2.InternalSaveEntity(entityData.ValuesDictionary, entityToIdMap);
                entityDataList.EntitiesData.Add(entityData);
            }
            return entityDataList;
        }

        public virtual ProjectData Save() {
            ProjectData projectData = new() { ValuesDictionary = [] };
            projectData.ValuesDictionary.DatabaseObject = ProjectTemplate;
            foreach (Subsystem subsystem in Subsystems) {
                ValuesDictionary valuesDictionary = [];
                subsystem.Save(valuesDictionary);
                if (valuesDictionary.Count > 0) {
                    projectData.ValuesDictionary.SetValue(subsystem.ValuesDictionary.DatabaseObject.Name, valuesDictionary);
                }
            }
            projectData.NextEntityID = NextEntityID;
            projectData.EntityDataList = SaveEntities(Entities);
            return projectData;
        }

        public virtual void Dispose() {
            if (m_entities != null) {
                foreach (Entity entity in m_entities.Keys) {
                    entity.DisposeInternal();
                }
            }
            if (Subsystems != null) {
                foreach (Subsystem subsystem in Subsystems) {
                    subsystem.DisposeInternal();
                }
            }
            OnProjectLoad = null;
            EntityRemoved = null;
            EntityAdded = null;
            GC.SuppressFinalize(this);
        }

        public virtual void FireEntityAddedEvents(Entity entity) {
            foreach (Component component in entity.Components) {
                component.OnEntityAdded();
            }
            foreach (Subsystem subsystem in Subsystems) {
                subsystem.OnEntityAdded(entity);
            }
            EntityAdded?.Invoke(this, new EntityAddRemoveEventArgs(entity));
            entity.FireEntityAddedEvent();
        }

        public virtual void FireEntityRemovedEvents(Entity entity) {
            foreach (Component component in entity.Components) {
                component.OnEntityRemoved();
            }
            foreach (Subsystem subsystem in Subsystems) {
                subsystem.OnEntityRemoved(entity);
            }
            EntityRemoved?.Invoke(this, new EntityAddRemoveEventArgs(entity));
            entity.FireEntityRemovedEvent();
        }

        public static Dictionary<Entity, bool> DetermineNotOwnedEntities(IEnumerable<Entity> entities) {
            Dictionary<Entity, bool> dictionary = [];
            List<Entity> list = [];
            foreach (Entity entity in entities) {
                dictionary.Add(entity, true);
                List<Entity> list2 = entity.InternalGetOwnedEntities();
                if (list2 != null) {
                    list.AddRange(list2);
                }
            }
            for (int i = 0; i < list.Count; i++) {
                List<Entity> list3 = list[i].InternalGetOwnedEntities();
                if (list3 != null) {
                    list.AddRange(list3);
                }
                dictionary.Remove(list[i]);
            }
            return dictionary;
        }

        public virtual void LoadSubsystem(Subsystem subsystem,
            Dictionary<string, Subsystem> subsystemsByName,
            Dictionary<Subsystem, bool> loadedSubsystems,
            int depth) {
            if (depth > 100) {
                throw new InvalidOperationException(
                    $"Too deep dependencies recursion while loading subsystem \"{subsystem.ValuesDictionary.DatabaseObject.Name}\"."
                );
            }
            if (loadedSubsystems.ContainsKey(subsystem)) {
                return;
            }
            string value = subsystem.ValuesDictionary.GetValue("Dependencies", string.Empty);
            if (!string.IsNullOrEmpty(value)) {
                string[] array = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < array.Length; i++) {
                    string text = array[i].Trim();
                    if (subsystemsByName.TryGetValue(text, out Subsystem value2)) {
                        LoadSubsystem(value2, subsystemsByName, loadedSubsystems, depth + 1);
                        continue;
                    }
                    throw new InvalidOperationException(
                        $"Dependency subsystem \"{text}\" not found when loading subsystem \"{subsystem.ValuesDictionary.DatabaseObject.Name}\"."
                    );
                }
            }
            subsystem.Load(subsystem.ValuesDictionary);
            loadedSubsystems.Add(subsystem, true);
        }
    }
}