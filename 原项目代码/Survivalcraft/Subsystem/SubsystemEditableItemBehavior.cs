using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game {
    public abstract class SubsystemEditableItemBehavior<T> : SubsystemBlockBehavior where T : IEditableItemData, new() {
        public SubsystemItemsScanner m_subsystemItemsScanner;

        public int m_contents;

        public Dictionary<int, T> m_itemsData = [];

        public Dictionary<Point3, T> m_blocksData = [];
        public Dictionary<MovingBlock, T> m_movingBlocksData = new();

        public SubsystemEditableItemBehavior(int contents) => m_contents = contents;

        public T GetBlockData(Point3 point) {
            m_blocksData.TryGetValue(point, out T value);
            return value;
        }

        public T GetBlockData(MovingBlock movingBlock) {
            m_movingBlocksData.TryGetValue(movingBlock, out T value);
            return value;
        }

        public void SetBlockData(Point3 point, T t) {
            if (t != null) {
                m_blocksData[point] = t;
            }
            else {
                m_blocksData.Remove(point);
            }
        }

        public void SetBlockData(MovingBlock movingBlock, T t) {
            if (t != null) {
                m_movingBlocksData[movingBlock] = t;
            }
            else {
                m_movingBlocksData.Remove(movingBlock);
            }
        }

        public T GetItemData(int id) {
            m_itemsData.TryGetValue(id, out T value);
            return value;
        }

        public int StoreItemDataAtUniqueId(T t) {
            int num = FindFreeItemId();
            m_itemsData[num] = t;
            return num;
        }

        public override void OnItemPlaced(int x, int y, int z, ref BlockPlacementData placementData, int itemValue) {
            int id = Terrain.ExtractData(itemValue);
            T itemData = GetItemData(id);
            if (itemData != null) {
                m_blocksData[new Point3(x, y, z)] = (T)itemData.Copy();
            }
        }

        public override void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue) {
            T blockData = GetBlockData(new Point3(x, y, z));
            if (blockData != null) {
                int num = FindFreeItemId();
                m_itemsData.Add(num, (T)blockData.Copy());
                dropValue.Value = Terrain.ReplaceData(dropValue.Value, num);
            }
        }

        public override void OnBlockRemoved(int value, int newValue, int x, int y, int z) {
            m_blocksData.Remove(new Point3(x, y, z));
        }

        public override void OnBlockStartMoving(int value, int newValue, int x, int y, int z, MovingBlock movingBlock) {
            Point3 point = new(x, y, z);
            T blockData = m_blocksData[point];
            m_blocksData.Remove(point);
            m_movingBlocksData[movingBlock] = blockData;
        }

        public override void OnBlockStopMoving(int value, int oldValue, int x, int y, int z, MovingBlock movingBlock) {
            Point3 point = new(x, y, z);
            T blockData = m_movingBlocksData[movingBlock];
            m_movingBlocksData.Remove(movingBlock);
            m_blocksData[point] = blockData;
        }

        public override void Load(ValuesDictionary valuesDictionary) {
            base.Load(valuesDictionary);
            m_subsystemItemsScanner = Project.FindSubsystem<SubsystemItemsScanner>(true);
            foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Blocks")) {
                T value = new();
                value.LoadString((string)item.Value);
                MovingBlock movingBlock = MovingBlock.LoadFromString(Project, item.Key, out Exception exception);
                if (exception == null) {
                    m_movingBlocksData[movingBlock] = value;
                }
                else {
                    Point3 key = HumanReadableConverter.ConvertFromString<Point3>(item.Key);
                    m_blocksData[key] = value;
                }
            }
            foreach (KeyValuePair<string, object> item2 in valuesDictionary.GetValue<ValuesDictionary>("Items")) {
                int key2 = HumanReadableConverter.ConvertFromString<int>(item2.Key);
                T value2 = new();
                value2.LoadString((string)item2.Value);
                m_itemsData[key2] = value2;
            }
            m_subsystemItemsScanner.ItemsScanned += GarbageCollectItems;
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            base.Save(valuesDictionary);
            ValuesDictionary valuesDictionary2 = new();
            valuesDictionary.SetValue("Blocks", valuesDictionary2);
            foreach (KeyValuePair<Point3, T> blocksDatum in m_blocksData) {
                valuesDictionary2.SetValue(HumanReadableConverter.ConvertToString(blocksDatum.Key), blocksDatum.Value.SaveString());
            }
            foreach (KeyValuePair<MovingBlock, T> movingBlocksDatum in m_movingBlocksData) {
                valuesDictionary2.SetValue(movingBlocksDatum.Key.ToString(), movingBlocksDatum.Value.SaveString());
            }
            ValuesDictionary valuesDictionary3 = new();
            valuesDictionary.SetValue("Items", valuesDictionary3);
            foreach (KeyValuePair<int, T> itemsDatum in m_itemsData) {
                valuesDictionary3.SetValue(HumanReadableConverter.ConvertToString(itemsDatum.Key), itemsDatum.Value.SaveString());
            }
        }

        public int FindFreeItemId() {
            for (int i = 1; i < 1000; i++) {
                if (!m_itemsData.ContainsKey(i)) {
                    return i;
                }
            }
            return 0;
        }

        public void GarbageCollectItems(ReadOnlyList<ScannedItemData> allExistingItems) {
            HashSet<int> hashSet = new();
            foreach (ScannedItemData item in allExistingItems) {
                if (Terrain.ExtractContents(item.Value) == m_contents) {
                    hashSet.Add(Terrain.ExtractData(item.Value));
                }
            }
            List<int> list = new();
            foreach (KeyValuePair<int, T> itemsDatum in m_itemsData) {
                if (!hashSet.Contains(itemsDatum.Key)) {
                    list.Add(itemsDatum.Key);
                }
            }
            foreach (int item2 in list) {
                m_itemsData.Remove(item2);
            }
        }
    }
}