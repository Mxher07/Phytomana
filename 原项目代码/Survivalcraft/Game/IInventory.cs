using Engine;
using GameEntitySystem;

namespace Game {
    public interface IInventory {
        Project Project { get; }

        int SlotsCount { get; }

        int VisibleSlotsCount { get; set; }

        int ActiveSlotIndex { get; set; }

        int GetSlotValue(int slotIndex);

        int GetSlotCount(int slotIndex);

        int GetSlotCapacity(int slotIndex, int value);

        int GetSlotProcessCapacity(int slotIndex, int value);

        void AddSlotItems(int slotIndex, int value, int count);

        void ProcessSlotItems(int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount);

        /// <returns>实际移除的数量</returns>
        int RemoveSlotItems(int slotIndex, int count);

        void DropAllItems(Vector3 position);
    }
}