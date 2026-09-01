using System.Globalization;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class ComponentCraftingTable : ComponentInventoryBase, IUpdateable {
        public int m_craftingGridSize;

        public string[] m_matchedIngredients = new string[9];

        public CraftingRecipe m_matchedRecipe;
        public int RemainsSlotIndex => SlotsCount - 1;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public bool m_recipeUpdateNeeded;

        public bool m_recipeRefindNeeded;
        public int ResultSlotIndex => SlotsCount - 2;

        public bool m_resetWhenSlotItemsRemoved;

        public virtual void Update(float dt) {
            if (m_recipeUpdateNeeded) {
                UpdateCraftingResult(m_recipeRefindNeeded);
            }
            m_recipeUpdateNeeded = false;
            m_recipeRefindNeeded = false;
        }

        public override int GetSlotCapacity(int slotIndex, int value) {
            if (slotIndex < SlotsCount - 2) {
                return base.GetSlotCapacity(slotIndex, value);
            }
            return 0;
        }

        public override void AddSlotItems(int slotIndex, int value, int count) {
            int oldCount = GetSlotCount(slotIndex);
            base.AddSlotItems(slotIndex, value, count);
            if (oldCount == 0) {
                m_recipeRefindNeeded = true;
            }
            m_recipeUpdateNeeded = true;
            m_slots[ResultSlotIndex].Count = 0;
        }

        public override int RemoveSlotItems(int slotIndex, int count) {
            int num = 0;
            int[] originalCount = new int[SlotsCount - 2];
            for (int i = 0; i < originalCount.Length; i++) {
                originalCount[i] = GetSlotCount(i);
            }
            if (slotIndex == ResultSlotIndex) {
                if (m_matchedRecipe != null) {
                    if (m_matchedRecipe.RemainsValue != 0
                        && m_matchedRecipe.RemainsCount > 0) {
                        if (m_slots[RemainsSlotIndex].Count == 0
                            || m_slots[RemainsSlotIndex].Value == m_matchedRecipe.RemainsValue) {
                            int num2 = BlocksManager.Blocks[Terrain.ExtractContents(m_matchedRecipe.RemainsValue)]
                                    .GetMaxStacking(m_matchedRecipe.RemainsValue)
                                - m_slots[RemainsSlotIndex].Count;
                            count = MathUtils.Min(count, num2 / m_matchedRecipe.RemainsCount * m_matchedRecipe.ResultCount);
                        }
                        else {
                            count = 0;
                        }
                    }
                    count = count / m_matchedRecipe.ResultCount * m_matchedRecipe.ResultCount;
                    num = base.RemoveSlotItems(slotIndex, count);
                    if (num > 0) {
                        for (int i = 0; i < 9; i++) {
                            if (!string.IsNullOrEmpty(m_matchedIngredients[i])) {
                                int index = i % 3 + m_craftingGridSize * (i / 3);
                                m_slots[index].Count = MathUtils.Max(m_slots[index].Count - num / m_matchedRecipe.ResultCount, 0);
                            }
                        }
                        if (m_matchedRecipe.RemainsValue != 0
                            && m_matchedRecipe.RemainsCount > 0) {
                            m_slots[RemainsSlotIndex].Value = m_matchedRecipe.RemainsValue;
                            m_slots[RemainsSlotIndex].Count += num / m_matchedRecipe.ResultCount * m_matchedRecipe.RemainsCount;
                        }
                        ComponentPlayer componentPlayer = FindInteractingPlayer();
                        if (componentPlayer != null
                            && componentPlayer.PlayerStats != null) {
                            componentPlayer.PlayerStats.ItemsCrafted += num;
                        }
                    }
                }
            }
            else {
                num = base.RemoveSlotItems(slotIndex, count);
            }
            m_recipeUpdateNeeded = true;
            if (m_resetWhenSlotItemsRemoved) {
                m_slots[ResultSlotIndex].Count = 0;
            }
            for (int i = 0; i < originalCount.Length; i++) {
                if (originalCount[i] > 0
                    && GetSlotCount(i) == 0) {
                    m_recipeRefindNeeded = true;
                }
            }
            return num;
        }

        public override void DropAllItems(Vector3 position) {
            for (int i = 0; i < SlotsCount; i++) {
                if (i != ResultSlotIndex) {
                    DropSlotItems(
                        this,
                        i,
                        position,
                        m_random.Float(5f, 10f)
                        * Vector3.Normalize(new Vector3(m_random.Float(-1f, 1f), m_random.Float(1f, 2f), m_random.Float(-1f, 1f)))
                    );
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap) {
            base.Load(valuesDictionary, idToEntityMap);
            m_craftingGridSize = (int)MathF.Sqrt(SlotsCount - 2);
            UpdateCraftingResult(true);
        }

        public virtual void UpdateCraftingResult(bool recipeRefindNeeded) {
            int num = int.MaxValue;
            for (int i = 0; i < m_craftingGridSize; i++) {
                for (int j = 0; j < m_craftingGridSize; j++) {
                    int num2 = i + j * 3;
                    int slotIndex = i + j * m_craftingGridSize;
                    int slotValue = GetSlotValue(slotIndex);
                    int num3 = Terrain.ExtractContents(slotValue);
                    int num4 = Terrain.ExtractData(slotValue);
                    int slotCount = GetSlotCount(slotIndex);
                    if (slotCount > 0) {
                        Block block = BlocksManager.Blocks[num3];
                        m_matchedIngredients[num2] = $"{block.GetCraftingId(slotValue)}:{num4.ToString(CultureInfo.InvariantCulture)}";
                        num = MathUtils.Min(num, slotCount);
                    }
                    else {
                        m_matchedIngredients[num2] = null;
                    }
                }
            }
            ComponentPlayer componentPlayer = FindInteractingPlayer();
            float playerLevel = componentPlayer?.PlayerData.Level ?? 1f;
            CraftingRecipe craftingRecipe;
            if (recipeRefindNeeded) {
                craftingRecipe = CraftingRecipesManager.FindMatchingRecipe(
                    Project.FindSubsystem<SubsystemTerrain>(true),
                    m_matchedIngredients,
                    0f,
                    playerLevel
                );
            }
            else {
                craftingRecipe = m_matchedRecipe;
            }
            if (craftingRecipe != null
                && craftingRecipe.ResultValue != 0) {
                m_matchedRecipe = craftingRecipe;
                m_slots[ResultSlotIndex].Value = craftingRecipe.ResultValue;
                m_slots[ResultSlotIndex].Count = craftingRecipe.ResultCount * num;
            }
            else {
                m_matchedRecipe = null;
                m_slots[ResultSlotIndex].Value = 0;
                m_slots[ResultSlotIndex].Count = 0;
            }
            if (craftingRecipe != null
                && !string.IsNullOrEmpty(craftingRecipe.Message)) {
                string message = craftingRecipe.Message;
                if (message.StartsWith('[')
                    && message.EndsWith(']')) {
                    message = LanguageControl.Get("CRMessage", message.Substring(1, message.Length - 2));
                }
                componentPlayer?.ComponentGui.DisplaySmallMessage(message, Color.White, true, true);
            }
        }
    }
}