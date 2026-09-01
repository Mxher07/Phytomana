using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentPickableGathererPlayer : ComponentPickableGatherer, IUpdateable {
        public ComponentPlayer m_componentPlayer;

        public IInventory m_inventory;

        public int m_experienceBlockIndex;

        public ComponentHealth m_componentHealth;

        public override bool CanGatherPickable(Pickable pickable) {
            if (m_componentHealth.Health <= 0) {
                return false;
            }
            if (pickable.FlyToPosition.HasValue) {
                return false;
            }
            if (pickable.ToRemove) {
                return false;
            }
            double pickableTimeExisted = m_subsystemGameInfo.TotalElapsedGameTime - pickable.CreationTime;
            if (pickableTimeExisted < pickable.TimeWaitToAutoPick) {
                return false;
            }
            if (pickable.Value == m_experienceBlockIndex) {
                return true;
            }
            return ComponentInventoryBase.FindAcquireSlotForItem(m_inventory, pickable.Value) >= 0;
        }

        public override void GatherPickable(Pickable pickable) {
            SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(pickable.Value));
            for (int i = 0; i < blockBehaviors.Length; i++) {
                if (pickable.ToRemove) {
                    break;
                }
                blockBehaviors[i].OnPickableGathered(pickable, this, pickable.Position - Position);
            }
            if (!pickable.ToRemove) {
                pickable.Count = ComponentInventoryBase.AcquireItems(m_inventory, pickable.Value, pickable.Count);
                if (pickable.Count == 0) {
                    pickable.ToRemove = true;
                    m_subsystemAudio.PlaySound("Audio/PickableCollected", 0.7f, -0.4f, Position, 2f, false);
                }
            }
        }

        public override void Update(float dt) {
            if (m_inventory == null) {
                m_inventory = m_componentPlayer.ComponentMiner.Inventory;
            }
            base.Update(dt);
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_experienceBlockIndex = BlocksManager.GetBlockIndex<ExperienceBlock>(true);
            m_componentHealth = Entity.FindComponent<ComponentHealth>(true);
        }
    }
}