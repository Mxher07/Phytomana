using System.Globalization;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentCreativeInventory : Component, IInventory {
        internal class Order {
            public Block block;
            public int order;
            public int value;

            public Order(Block b, int o, int v) {
                block = b;
                order = o;
                value = v;
            }
        }

        public List<int> m_slots = [];

        public int m_activeSlotIndex;

        public int m_visibleSlotsCount = 10;

        public const int m_largeNumber = 0x1fffffff;
        public int OpenSlotsCount { get; set; }

        public int CategoryIndex { get; set; }

        public int PageIndex { get; set; }

        Project IInventory.Project => Project;

        public int ActiveSlotIndex {
            get => m_activeSlotIndex;
            set => m_activeSlotIndex = Math.Clamp(value, 0, VisibleSlotsCount - 1);
        }

        public int SlotsCount => m_slots.Count;

        public int VisibleSlotsCount {
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
                        ComponentInventoryBase.DropSlotItems(this, i, position, velocity);
                    }
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            m_activeSlotIndex = valuesDictionary.GetValue<int>("ActiveSlotIndex");
            OpenSlotsCount = valuesDictionary.GetValue<int>("OpenSlotsCount");
            CategoryIndex = valuesDictionary.GetValue<int>("CategoryIndex");
            PageIndex = valuesDictionary.GetValue<int>("PageIndex");
            for (int i = 0; i < OpenSlotsCount; i++) {
                m_slots.Add(0);
            }
            List<Order> orders = [];
            foreach (Block item in BlocksManager.Blocks) {
                foreach (int creativeValue in item.GetCreativeValues()) {
                    orders.Add(new Order(item, item.GetDisplayOrder(creativeValue), creativeValue));
                }
            }
            IOrderedEnumerable<Order> orderList = orders.OrderBy(o => o.order);
            foreach (Order c in orderList) {
                m_slots.Add(c.value);
            }
            ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("Slots", null);
            if (value == null) {
                return;
            }
            for (int j = 0; j < OpenSlotsCount; j++) {
                ValuesDictionary value2 = value.GetValue<ValuesDictionary>($"Slot{j.ToString(CultureInfo.InvariantCulture)}", null);
                if (value2 != null) {
                    m_slots[j] = value2.GetValue<int>("Contents");
                }
            }
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap) {
            valuesDictionary.SetValue("ActiveSlotIndex", m_activeSlotIndex);
            valuesDictionary.SetValue("CategoryIndex", CategoryIndex);
            valuesDictionary.SetValue("PageIndex", PageIndex);
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Slots", valuesDictionary2);
            for (int i = 0; i < OpenSlotsCount; i++) {
                if (m_slots[i] != 0) {
                    ValuesDictionary valuesDictionary3 = new();
                    valuesDictionary2.SetValue($"Slot{i.ToString(CultureInfo.InvariantCulture)}", valuesDictionary3);
                    valuesDictionary3.SetValue("Contents", m_slots[i]);
                }
            }
        }

        public virtual int GetSlotValue(int slotIndex) {
            if (slotIndex >= 0
                && slotIndex < m_slots.Count) {
                return m_slots[slotIndex];
            }
            return 0;
        }

        public virtual int GetSlotCount(int slotIndex) {
            if (slotIndex >= 0
                && slotIndex < m_slots.Count) {
                if (m_slots[slotIndex] == 0) {
                    return 0;
                }
                return m_largeNumber;
            }
            return 0;
        }

        public virtual int GetSlotCapacity(int slotIndex, int value) {
            if (slotIndex >= VisibleSlotsCount
                && slotIndex < 10) {
                return 0;
            }
            if (slotIndex >= 0
                && slotIndex < OpenSlotsCount) {
                return m_largeNumber << 1;
            }
            int num = Terrain.ExtractContents(value);
            if (BlocksManager.Blocks[num].IsNonDuplicable_(value)) {
                return m_largeNumber;
            }
            return m_largeNumber << 1;
        }

        public virtual int GetSlotProcessCapacity(int slotIndex, int value) {
            int slotCount = GetSlotCount(slotIndex);
            int slotValue = GetSlotValue(slotIndex);
            if (slotCount > 0
                && slotValue != 0) {
                SubsystemBlockBehavior[] blockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true)
                    .GetBlockBehaviors(Terrain.ExtractContents(slotValue));
                for (int i = 0; i < blockBehaviors.Length; i++) {
                    int processInventoryItemCapacity = blockBehaviors[i].GetProcessInventoryItemCapacity(this, slotIndex, value);
                    if (processInventoryItemCapacity > 0) {
                        return processInventoryItemCapacity;
                    }
                }
            }
            if (slotIndex < OpenSlotsCount) {
                return 0;
            }
            return m_largeNumber;
        }

        public virtual void AddSlotItems(int slotIndex, int value, int count) {
            if (slotIndex >= 0
                && slotIndex < OpenSlotsCount) {
                m_slots[slotIndex] = value;
            }
        }

        public virtual void ProcessSlotItems(int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount) {
            int slotCount = GetSlotCount(slotIndex);
            int slotValue = GetSlotValue(slotIndex);
            if (slotCount > 0
                && slotValue != 0) {
                SubsystemBlockBehavior[] blockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true)
                    .GetBlockBehaviors(Terrain.ExtractContents(slotValue));
                foreach (SubsystemBlockBehavior subsystemBlockBehavior in blockBehaviors) {
                    int processInventoryItemCapacity = subsystemBlockBehavior.GetProcessInventoryItemCapacity(this, slotIndex, value);
                    if (processInventoryItemCapacity > 0) {
                        subsystemBlockBehavior.ProcessInventoryItem(
                            this,
                            slotIndex,
                            value,
                            count,
                            MathUtils.Min(processInventoryItemCapacity, processCount),
                            out processedValue,
                            out processedCount
                        );
                        return;
                    }
                }
            }
            if (slotIndex >= OpenSlotsCount) {
                processedValue = 0;
                processedCount = 0;
            }
            else {
                processedValue = value;
                processedCount = count;
            }
        }

        public virtual int RemoveSlotItems(int slotIndex, int count) {
            int num = Terrain.ExtractContents(m_slots[slotIndex]);
            int maxStacking = BlocksManager.Blocks[num].GetMaxStacking(m_slots[slotIndex]);
            if (slotIndex >= 0
                && slotIndex < OpenSlotsCount) {
                if (BlocksManager.Blocks[num].IsNonDuplicable_(m_slots[slotIndex])) {
                    m_slots[slotIndex] = 0;
                    return 1;
                }
                if (count >= m_largeNumber) {
                    m_slots[slotIndex] = 0;
                    return 1;
                }
            }
            if (SettingsManager.CreativeDragMaxStacking) {
                return MathUtils.Min(maxStacking, count);
            }
            return 1;
        }

        public virtual void DropAllItems(Vector3 position) { }
    }
}