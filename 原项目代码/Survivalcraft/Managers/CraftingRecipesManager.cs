using System.Globalization;
using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game {
    public static class CraftingRecipesManager {
        public static List<CraftingRecipe> m_recipes = [];
        public static List<CraftingRecipe> Recipes => m_recipes;
        public static string fName = "CraftingRecipesManager";

        /// <summary>
        ///     启用等级限制
        ///     Mod在初始化时，设置为false可以让物品合成不受玩家等级限制
        /// </summary>
        public static bool EnableLevelRestrictions = true;

        public static void Initialize() {
            m_recipes.Clear();
            XElement source = null;
            foreach (ModEntity modEntity in ModsManager.ModList) {
                modEntity.LoadCr(ref source);
            }
            LoadData(source);
            Block[] blocks = BlocksManager.Blocks;
            foreach (Block block in blocks) {
                m_recipes.AddRange(block.GetProceduralCraftingRecipes());
            }
            bool sort = true;
            ModsManager.HookAction(
                "CraftingRecipesManagerInitialize",
                loader => {
                    loader.CraftingRecipesManagerInitialize(m_recipes, ref sort);
                    return false;
                }
            );
            if (sort) {
                m_recipes.Sort(
                    delegate(CraftingRecipe r1, CraftingRecipe r2) {
                        int result = Comparer<int>.Default.Compare(r1.DisplayOrder, r2.DisplayOrder);
                        if (result == 0) {
                            int y = r1.Ingredients.Count(s => !string.IsNullOrEmpty(s));
                            int x = r2.Ingredients.Count(s => !string.IsNullOrEmpty(s));
                            return Comparer<int>.Default.Compare(x, y);
                        }
                        return result;
                    }
                );
            }
            ModsManager.HookAction(
                "CraftingRecipesManagerInitialized",
                loader => {
                    loader.CraftingRecipesManagerInitialized();
                    return false;
                }
            );
        }

        public static void LoadData(XElement item) {
            try {
                if (item.Attribute("Result") == null) {
                    foreach (XElement xElement in item.Elements()) {
                        LoadData(xElement);
                    }
                    return;
                }
                bool flag = false;
                ModsManager.HookAction(
                    "OnCraftingRecipeDecode",
                    modLoader => {
                        modLoader.OnCraftingRecipeDecode(m_recipes, item, out flag);
                        return flag;
                    }
                );
                if (!flag) {
                    CraftingRecipe craftingRecipe = DecodeElementToCraftingRecipe(item);
                    m_recipes.Add(craftingRecipe);
                }
            }
            catch (Exception e) {
                Log.Error(e);
            }
        }

        public static CraftingRecipe DecodeElementToCraftingRecipe(XElement item, int horizontalLen = 3) {
            CraftingRecipe craftingRecipe = new();
            string attributeValue = XmlUtils.GetAttributeValue<string>(item, "Result");
            string desc = XmlUtils.GetAttributeValue<string>(item, "Description");
            if (desc.StartsWith('[')
                && desc.EndsWith(']')
                && LanguageControl.TryGetBlock(attributeValue, $"CRDescription:{desc.Substring(1, desc.Length - 2)}", out string r)) {
                desc = r;
            }
            craftingRecipe.ResultValue = DecodeResult(attributeValue);
            craftingRecipe.ResultCount = XmlUtils.GetAttributeValue<int>(item, "ResultCount");
            string attributeValue2 = XmlUtils.GetAttributeValue(item, "Remains", string.Empty);
            if (!string.IsNullOrEmpty(attributeValue2)) {
                craftingRecipe.RemainsValue = DecodeResult(attributeValue2);
                craftingRecipe.RemainsCount = XmlUtils.GetAttributeValue<int>(item, "RemainsCount");
            }
            craftingRecipe.RequiredHeatLevel = XmlUtils.GetAttributeValue<float>(item, "RequiredHeatLevel");
            craftingRecipe.RequiredPlayerLevel = XmlUtils.GetAttributeValue(item, "RequiredPlayerLevel", 1f);
            craftingRecipe.Description = desc;
            craftingRecipe.Message = XmlUtils.GetAttributeValue<string>(item, "Message", null);
            craftingRecipe.DisplayOrder = XmlUtils.GetAttributeValue(item, "DisplayOrder", 0);
            Dictionary<char, string> dictionary = new();
            foreach (XAttribute item2 in from a in item.Attributes()
                where a.Name.LocalName.Length == 1 && char.IsLower(a.Name.LocalName[0])
                select a) {
                DecodeIngredient(item2.Value, out string craftingId, out int? data);
                if (BlocksManager.FindBlocksByCraftingId(craftingId).Length == 0) {
                    throw new InvalidOperationException($"Block with craftingId \"{item2.Value}\" not found.");
                }
                if (data.HasValue
                    && (data.Value < 0 || data.Value > 262143)) {
                    throw new InvalidOperationException($"Data in recipe ingredient \"{item2.Value}\" must be between 0 and 0x3FFFF.");
                }
                dictionary.Add(item2.Name.LocalName[0], item2.Value);
            }
            string[] array = item.Value.Trim().Split(["\n"], StringSplitOptions.None);
            for (int i = 0; i < array.Length; i++) {
                int num = array[i].IndexOf('"');
                int num2 = array[i].LastIndexOf('"');
                if (num < 0
                    || num2 < 0
                    || num2 <= num) {
                    throw new InvalidOperationException("Invalid recipe line.");
                }
                string text = array[i].Substring(num + 1, num2 - num - 1);
                for (int j = 0; j < text.Length; j++) {
                    char c = text[j];
                    if (char.IsLower(c)) {
                        string text2 = dictionary[c];
                        craftingRecipe.Ingredients[j + i * horizontalLen] = text2;
                    }
                }
            }
            return craftingRecipe;
        }

        public static CraftingRecipe FindMatchingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, float playerLevel) {
            if (ingredients.All(string.IsNullOrEmpty)) {
                return null;
            }
            CraftingRecipe craftingRecipe = null;
            Block[] blocks = BlocksManager.Blocks;
            for (int i = 0; i < blocks.Length; i++) {
                CraftingRecipe adHocCraftingRecipe = blocks[i].GetAdHocCraftingRecipe(terrain, ingredients, heatLevel, playerLevel);
                if (adHocCraftingRecipe != null
                    && MatchRecipe(adHocCraftingRecipe.Ingredients, ingredients)) {
                    craftingRecipe = adHocCraftingRecipe;
                    break;
                }
            }
            if (craftingRecipe == null) {
                foreach (CraftingRecipe recipe in Recipes) {
                    if (MatchRecipe(recipe.Ingredients, ingredients)) {
                        craftingRecipe = recipe;
                        break;
                    }
                }
            }
            if (craftingRecipe != null) {
                if (heatLevel < craftingRecipe.RequiredHeatLevel) {
                    craftingRecipe = !(heatLevel > 0f)
                        ? new CraftingRecipe { Message = LanguageControl.Get(fName, 0) }
                        : new CraftingRecipe { Message = LanguageControl.Get(fName, 1) };
                }
                else if (playerLevel < craftingRecipe.RequiredPlayerLevel && EnableLevelRestrictions) {
                    craftingRecipe = !(craftingRecipe.RequiredHeatLevel > 0f)
                        ? new CraftingRecipe { Message = string.Format(LanguageControl.Get(fName, 2), craftingRecipe.RequiredPlayerLevel) }
                        : new CraftingRecipe { Message = string.Format(LanguageControl.Get(fName, 3), craftingRecipe.RequiredPlayerLevel) };
                }
            }
            return craftingRecipe;
        }

        public static int DecodeResult(string result) {
            bool flag2 = false;
            int result2 = 0;
            ModsManager.HookAction(
                "DecodeResult",
                modLoader => {
                    result2 = modLoader.DecodeResult(result, out flag2);
                    return flag2;
                }
            );
            if (flag2) {
                return result2;
            }
            if (!string.IsNullOrEmpty(result)) {
                string[] array = result.Split([':'], StringSplitOptions.None);
                int blockIndex = BlocksManager.GetBlockIndex(array[0], true);
                return Terrain.MakeBlockValue(blockIndex, 0, array.Length == 2 ? int.Parse(array[1], CultureInfo.InvariantCulture) : 0);
            }
            return 0;
        }

        public static void DecodeIngredient(string ingredient, out string craftingId, out int? data) {
            bool flag2 = false;
            string craftingId_R = string.Empty;
            int? data_R = null;
            ModsManager.HookAction(
                "DecodeIngredient",
                modLoader => {
                    modLoader.DecodeIngredient(ingredient, out craftingId_R, out data_R, out flag2);
                    return flag2;
                }
            );
            if (flag2) {
                craftingId = craftingId_R;
                data = data_R;
                return;
            }
            string[] array = ingredient.Split([':'], StringSplitOptions.None);
            craftingId = array[0];
            data = array.Length >= 2 ? new int?(int.Parse(array[1], CultureInfo.InvariantCulture)) : null;
        }

        public static bool MatchRecipe(string[] requiredIngredients, string[] actualIngredients) {
            bool flag2 = false;
            bool result = false;
            ModsManager.HookAction(
                "MatchRecipe",
                modLoader => {
                    result = modLoader.MatchRecipe(requiredIngredients, actualIngredients, out flag2);
                    return flag2;
                }
            );
            if (flag2) {
                return result;
            }
            if (actualIngredients.Length > 9) {
                return false;
            }
            string[] array = new string[9];
            for (int i = 0; i < 2; i++) {
                bool flip = i != 0;
                for (int j = -4; j <= 2; j++) {
                    for (int k = -4; k <= 2; k++) {
                        if (!TransformRecipe(array, requiredIngredients, k, j, flip)) {
                            continue;
                        }
                        bool flag = true;
                        for (int l = 0; l < 9; l++) {
                            if (l == actualIngredients.Length
                                || !CompareIngredients(array[l], actualIngredients[l])) {
                                flag = false;
                                break;
                            }
                        }
                        if (flag) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool TransformRecipe(string[] transformedIngredients, string[] ingredients, int shiftX, int shiftY, bool flip) {
            for (int i = 0; i < 9; i++) {
                transformedIngredients[i] = null;
            }
            for (int j = 0; j < 3; j++) {
                for (int k = 0; k < 3; k++) {
                    int num = (flip ? 3 - k - 1 : k) + shiftX;
                    int num2 = j + shiftY;
                    string text = ingredients[k + j * 3];
                    if (num >= 0
                        && num2 >= 0
                        && num < 3
                        && num2 < 3) {
                        transformedIngredients[num + num2 * 3] = text;
                    }
                    else if (!string.IsNullOrEmpty(text)) {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool CompareIngredients(string requiredIngredient, string actualIngredient) {
            if (requiredIngredient == null) {
                return actualIngredient == null;
            }
            if (actualIngredient == null) {
                //return requiredIngredient == null;
                return false;
            }
            DecodeIngredient(requiredIngredient, out string craftingId, out int? data);
            DecodeIngredient(actualIngredient, out string craftingId2, out int? data2);
            if (!data2.HasValue) {
                throw new InvalidOperationException("Actual ingredient data not specified.");
            }
            if (craftingId == craftingId2) {
                if (!data.HasValue) {
                    return true;
                }
                return data.Value == data2.Value;
            }
            return false;
        }
    }
}