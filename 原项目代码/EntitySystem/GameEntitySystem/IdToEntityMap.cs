using System;
using System.Collections.Generic;

namespace GameEntitySystem {
    public class IdToEntityMap {
        public Dictionary<int, Entity> m_map;

        public IdToEntityMap(Dictionary<int, Entity> map) => m_map = map;

        [Obsolete("Use Project.FindEntity instead.", true)]
        public Entity FindEntity(int id) {
            if (m_map.TryGetValue(id, out Entity value)) {
                return value;
            }
            return null;
        }

        [Obsolete("Use Project.FindEntity()?.FindComponent() instead.", true)]
        public T FindComponent<T>(int id, string name) where T : Component {
            Entity entity = FindEntity(id);
            if (entity != null) {
                return entity.FindComponent<T>(name, false);
            }
            return null;
        }
    }
}