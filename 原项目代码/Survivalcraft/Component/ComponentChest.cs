using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentChest : ComponentInventoryBase {
        ComponentBlockEntity m_componentBlockEntity;

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_componentBlockEntity = Entity.FindComponent<ComponentBlockEntity>();
            if (m_componentBlockEntity != null) {
                m_componentBlockEntity.m_inventoryToGatherPickable = this;
            }
        }
    }
}