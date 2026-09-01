using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentInventory : ComponentInventoryBase {
        public int m_activeSlotIndex;

        public int m_visibleSlotsCount = 10;

        public const int ShortInventorySlotsCount = 10;

        public override int ActiveSlotIndex {
            get => m_activeSlotIndex;
            set => m_activeSlotIndex = Math.Clamp(value, 0, VisibleSlotsCount - 1);
        }

        public override int VisibleSlotsCount {
            get => m_visibleSlotsCount;
            set {
                value = Math.Clamp(value, 0, 10);
                if (value == m_visibleSlotsCount) {
                    return;
                }
                m_visibleSlotsCount = value;
                ActiveSlotIndex = ActiveSlotIndex;
                ComponentFrame componentFrame = Entity.FindComponent<ComponentFrame>();
                if (componentFrame != null) {
                    Vector3 position = componentFrame.Position + new Vector3(0f, 0.5f, 0f);
                    Vector3 velocity = 1f * componentFrame.Rotation.GetForwardVector();
                    for (int i = m_visibleSlotsCount; i < 10; i++) {
                        DropSlotItems(this, i, position, velocity);
                    }
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            ActiveSlotIndex = valuesDictionary.GetValue<int>("ActiveSlotIndex");
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            base.Save(valuesDictionary, entityToIdMap);
            valuesDictionary.SetValue("ActiveSlotIndex", ActiveSlotIndex);
        }

        public override int GetSlotCapacity(int slotIndex, int value) {
            if (slotIndex >= VisibleSlotsCount
                && slotIndex < 10) {
                return 0;
            }
            return BlocksManager.Blocks[Terrain.ExtractContents(value)].GetMaxStacking(value);
        }
    }
}