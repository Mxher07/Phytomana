using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Engine.Serialization;
using TemplatesDatabase;

namespace GameEntitySystem {
    public class Entity : IDisposable {
        public struct FilteredComponentsEnumerable<T> : IEnumerable<T> where T : class {
            public Entity m_entity;

            public FilteredComponentsEnumerable(Entity entity) => m_entity = entity;

            public FilteredComponentsEnumerator<T> GetEnumerator() => new(m_entity);

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => new FilteredComponentsEnumerator<T>(m_entity);

            IEnumerator IEnumerable.GetEnumerator() => new FilteredComponentsEnumerator<T>(m_entity);
        }

        public struct FilteredComponentsEnumerator<T> : IEnumerator<T> where T : class {
            public Entity m_entity;

            public int m_index;

            public T m_current;

            public T Current => m_current;

            object IEnumerator.Current => m_current;

            public FilteredComponentsEnumerator(Entity entity) {
                m_entity = entity;
                m_index = 0;
                m_current = null;
            }

            public void Dispose() { }

            public bool MoveNext() {
                while (m_index < m_entity.m_components.Count) {
                    if (m_entity.m_components[m_index++] is T val) {
                        m_current = val;
                        return true;
                    }
                }
                m_current = null;
                return false;
            }

            public void Reset() {
                m_index = 0;
                m_current = null;
            }
        }

        public Project m_project;

        public ValuesDictionary m_valuesDictionary;

        public List<Component> m_components;

        public bool m_isAddedToProject;

        public Project Project => m_project;

        public ValuesDictionary ValuesDictionary => m_valuesDictionary;

        public bool IsAddedToProject => m_isAddedToProject;

        public List<Component> Components => m_components;

        public event EventHandler EntityAdded;

        public event EventHandler EntityRemoved;

        public int Id;

        public Entity() { }

        public Entity(Project project, ValuesDictionary valuesDictionary, int id) : this(project, valuesDictionary) => Id = id;

        public static event Action<Entity, List<KeyValuePair<int, Component>>> EntityComponentsInitialized;

        public Entity(Project project, ValuesDictionary valuesDictionary) {
            if (valuesDictionary.DatabaseObject.Type != project.GameDatabase.EntityTemplateType) {
                throw new InvalidOperationException("ValuesDictionary was not created from EntityTemplate.");
            }
            m_project = project;
            m_valuesDictionary = valuesDictionary;
            List<KeyValuePair<int, Component>> list = [];
            foreach (ValuesDictionary item in from x in valuesDictionary.Values
                select x as ValuesDictionary
                into x
                where x != null && x.DatabaseObject != null && x.DatabaseObject.Type == project.GameDatabase.MemberComponentTemplateType
                select x) {
                bool isOptional = item.GetValue<bool>("IsOptional");
                string className = item.GetValue<string>("Class");
                int loadOrder = item.GetValue<int>("LoadOrder");
                Type type = TypeCache.FindType(className, false, !isOptional);
                if (type != null) {
                    object obj;
                    try {
#pragma warning disable IL2072
                        obj = Activator.CreateInstance(type);
#pragma warning restore IL2072
                    }
                    catch (TargetInvocationException ex) {
                        if (ex.InnerException is not null) {
                            throw ex.InnerException;
                        }
                        throw;
                    }
                    if (obj is not Component component) {
                        throw new InvalidOperationException(
                            $"Type \"{className}\" cannot be used as a component because it does not inherit from Component class."
                        );
                    }
                    component.Initialize(this, item);
                    bool isModComponent = type.Namespace != "Game";
                    //如果是原版的组件，则按原来顺序，否则往后
                    int adjustedLoadOrder = isModComponent ? loadOrder + 10000 : loadOrder;
                    list.Add(new KeyValuePair<int, Component>(adjustedLoadOrder, component));
                }
            }
            EntityComponentsInitialized?.Invoke(this, list);
            // 按调整后的 LoadOrder 排序
            list.Sort((x, y) => x.Key - y.Key);
            m_components = new List<Component>(list.Select(x => x.Value));
        }

        public Component FindComponent(Type type, string name, bool throwOnError) {
            foreach (Component component in m_components) {
                if (type.GetTypeInfo().IsAssignableFrom(component.GetType().GetTypeInfo())
                    && (string.IsNullOrEmpty(name) || component.ValuesDictionary.DatabaseObject.Name == name)) {
                    return component;
                }
            }
            if (throwOnError) {
                if (string.IsNullOrEmpty(name)) {
                    throw new Exception($"Required component {type.FullName} does not exist in entity.");
                }
                throw new Exception($"Required component {type.FullName} with name \"{name}\" does not exist in entity.");
            }
            return null;
        }

        public Component FindComponent(string name, bool throwOnError) {
            if (throwOnError) {
                if (string.IsNullOrEmpty(name)) {
                    throw new ArgumentNullException(nameof(name));
                }
            }
            foreach (Component component in m_components) {
                if (component.ValuesDictionary.DatabaseObject.Name == name) {
                    return component;
                }
            }
            if (throwOnError) {
                throw new Exception($"Required component {name} does not exist in entity.");
            }
            return null;
        }

        public T FindComponent<T>() where T : class => FindComponent(typeof(T), null, false) as T;

        public T FindComponent<T>(bool throwOnError) where T : class => FindComponent(typeof(T), null, throwOnError) as T;

        public T FindComponent<T>(string name, bool throwOnError) where T : class => FindComponent(typeof(T), name, throwOnError) as T;

        public void RemoveComponent(Component component) {
            m_components.Remove(component);
        }

        public void ReplaceComponent(Component oldComponent, Component newComponent) {
            if (newComponent.GetType().GetTypeInfo().IsAssignableFrom(oldComponent.GetType().GetTypeInfo())) {
                newComponent.InheritFromComponent(oldComponent);
            }
        }

        public FilteredComponentsEnumerable<T> FindComponents<T>() where T : class => new(this);

        public void Dispose() {
            foreach (Component component in m_components) {
                component.DisposeInternal();
            }
        }

        internal void DisposeInternal() {
            GC.SuppressFinalize(this);
            Dispose();
        }

        internal List<Entity> InternalGetOwnedEntities() {
            List<Entity> list = null;
            foreach (Component component in m_components) {
                IEnumerable<Entity> ownedEntities = component.GetOwnedEntities();
                list = list ?? [];
                list.AddRange(ownedEntities);
            }
            return list;
        }

        public void InternalLoadEntity(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            foreach (Component component in m_components) {
                try {
                    component.Load(component.ValuesDictionary, idToEntityMap);
                }
                catch (Exception innerException) {
                    throw new InvalidOperationException($"Error loading component {component.GetType().FullName}.", innerException);
                }
            }
        }

        public void InternalSaveEntity(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            foreach (Component component in Components) {
                ValuesDictionary valuesDictionary2 = [];
                component.Save(valuesDictionary2, entityToIdMap);
                if (valuesDictionary2.Count > 0) {
                    valuesDictionary.SetValue(component.ValuesDictionary.DatabaseObject.Name, valuesDictionary2);
                }
            }
        }

        public void FireEntityAddedEvent() {
            EntityAdded?.Invoke(this, EventArgs.Empty);
        }

        public void FireEntityRemovedEvent() {
            EntityRemoved?.Invoke(this, EventArgs.Empty);
        }
    }
}