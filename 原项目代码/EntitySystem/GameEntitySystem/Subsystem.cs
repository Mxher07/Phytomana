using System;
using TemplatesDatabase;

namespace GameEntitySystem {
    public abstract class Subsystem : IDisposable {
        public Project m_project;

        public ValuesDictionary m_valuesDictionary;

        public Project Project => m_project;

        public ValuesDictionary ValuesDictionary => m_valuesDictionary;

        public Subsystem() { }

        public virtual void OnEntityAdded(Entity entity) { }

        public virtual void OnEntityRemoved(Entity entity) { }

        public virtual void Load(ValuesDictionary valuesDictionary) { }

        public virtual void Save(ValuesDictionary valuesDictionary) { }

        public virtual void Dispose() { }

        internal void DisposeInternal() {
            GC.SuppressFinalize(this);
            Dispose();
        }

        public virtual void Initialize(Project project, ValuesDictionary valuesDictionary) {
            if (valuesDictionary.DatabaseObject.Type != project.GameDatabase.MemberSubsystemTemplateType) {
                throw new InvalidOperationException("ValuesDictionary has invalid type.");
            }
            m_project = project;
            m_valuesDictionary = valuesDictionary;
        }
    }
}