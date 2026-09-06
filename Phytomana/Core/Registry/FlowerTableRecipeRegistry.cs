using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Engine;
using Game;
using XmlUtilities;

namespace Phytomana {
    /// <summary>
    /// 花药台配方的一种原料：方块 + 数量。
    /// Name 支持「类名:data」后缀（如 SumeruPetalBlock:28 = 红色须弥花瓣，data = 色号×2）；
    /// 不带后缀表示不限特殊值。
    /// </summary>
    public class FlowerRecipeIngredient {
        public string BlockName;
        public int Contents = -1;
        /// <summary>要求的 data 值；-1 表示不限。</summary>
        public int ExpectedData = -1;
        public int Count = 1;
    }

    /// <summary>
    /// 一条花药台配方：若干原料 → 产物（可带魔力消耗）。由外部 .fr 文件声明。
    /// </summary>
    public class FlowerRecipe {
        public string ResultBlockName;
        public int ResultContents = -1;
        public int ResultData;
        public int ResultCount = 1;
        public float ManaCost;
        /// <summary>
        /// 为 true 时产物的 data 继承第一份原料的 data（用于颜色变体保色，
        /// 如须弥花 → 同色须弥花瓣）。默认 false（产物 data 为 0 或声明的固定值）。
        /// </summary>
        public bool CopyData;
        public List<FlowerRecipeIngredient> Ingredients = [];
        /// <summary>来源文件名（便于排查配置错误）。</summary>
        public string SourceFile;
    }

    /// <summary>
    /// 花药台配方注册表。扫描本模组全部 <c>.fr</c> 文件（外部配方文件，规定「什么 + 什么」
    /// 在花药台合成），解析为 <see cref="FlowerRecipe"/> 并提供无序匹配查询。
    /// 匹配基于完整方块值：原料可指定色号，提供的原料须与配方在种类、颜色、数量上完全一致。
    /// </summary>
    public static class FlowerTableRecipeRegistry {
        public const string Extension = ".fr";

        static readonly List<FlowerRecipe> m_recipes = [];

        public static bool IsInitialized { get; private set; }

        public static IReadOnlyList<FlowerRecipe> Recipes => m_recipes;

        public static int Count => m_recipes.Count;

        /// <summary>由 PhytomanaMod 在 BlocksInitalized 时机调用：扫描本模组全部 .fr 文件并解析。</summary>
        internal static void Initialize(ModEntity entity) {
            m_recipes.Clear();
            if (entity != null) {
                try {
                    entity.GetFiles(Extension, (name, stream) => LoadFromStream(stream, name));
                }
                catch (Exception e) {
                    Log.Error($"[PhytoMana]FlowerTableRecipes: failed to scan {Extension} files: {e}");
                }
            }
            IsInitialized = true;
            Log.Information($"[PhytoMana]FlowerTableRecipeRegistry: {m_recipes.Count} flower table recipes loaded.");
        }

        /// <summary>解析单个 .fr 文件流，将其中所有 &lt;Recipe&gt; 追加进注册表。</summary>
        public static void LoadFromStream(Stream stream, string sourceName) {
            XElement root;
            try {
                root = XmlUtils.LoadXmlFromStream(stream, null, true);
            }
            catch (Exception e) {
                Log.Error($"[PhytoMana]FlowerTableRecipes: failed to parse \"{sourceName}\": {e}");
                return;
            }
            if (root == null) {
                return;
            }
            foreach (XElement element in root.Elements("Recipe")) {
                FlowerRecipe recipe = DecodeRecipe(element, sourceName);
                if (recipe != null) {
                    m_recipes.Add(recipe);
                }
            }
        }

        /// <summary>
        /// 无序精确匹配：提供的原料（完整方块值列表）在种类、颜色、数量上恰好满足某配方时返回该配方。
        /// </summary>
        public static bool TryMatch(IList<int> providedValues, out FlowerRecipe matched) {
            matched = null;
            if (providedValues == null || providedValues.Count == 0) {
                return false;
            }
            foreach (FlowerRecipe recipe in m_recipes) {
                if (Matches(recipe, providedValues)) {
                    matched = recipe;
                    return true;
                }
            }
            return false;
        }

        static bool Matches(FlowerRecipe recipe, IList<int> providedValues) {
            int requiredTotal = 0;
            foreach (FlowerRecipeIngredient ingredient in recipe.Ingredients) {
                requiredTotal += ingredient.Count;
            }
            if (providedValues.Count != requiredTotal) {
                return false;
            }
            // 提供的按（内容, data）计数
            Dictionary<KeyValuePair<int, int>, int> available = [];
            foreach (int value in providedValues) {
                KeyValuePair<int, int> key = new(Terrain.ExtractContents(value), Terrain.ExtractData(value));
                available[key] = available.GetValueOrDefault(key) + 1;
            }
            foreach (FlowerRecipeIngredient ingredient in recipe.Ingredients) {
                int remain = ingredient.Count;
                if (ingredient.ExpectedData >= 0) {
                    // 指定色号：只消耗匹配 data 的原料
                    KeyValuePair<int, int> key = new(ingredient.Contents, ingredient.ExpectedData);
                    int take = Math.Min(remain, available.GetValueOrDefault(key));
                    available[key] = available.GetValueOrDefault(key) - take;
                    remain -= take;
                }
                else {
                    // 不限特殊值：消耗该方块下任意剩余
                    List<KeyValuePair<int, int>> keys = [];
                    foreach (KeyValuePair<KeyValuePair<int, int>, int> pair in available) {
                        if (pair.Key.Key == ingredient.Contents && pair.Value > 0) {
                            keys.Add(pair.Key);
                        }
                    }
                    foreach (KeyValuePair<int, int> key in keys) {
                        int take = Math.Min(remain, available[key]);
                        available[key] = available[key] - take;
                        remain -= take;
                        if (remain <= 0) {
                            break;
                        }
                    }
                }
                if (remain > 0) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 模糊匹配：在「所提供原料能被其原料槽位接纳」的配方中找出缺口最小的一条，
        /// 用于花药台状态提示（还差哪些材料）。missing 列出每种原料槽位的缺口数量。
        /// </summary>
        public static bool TryMatchClosest(
            IList<int> providedValues,
            out FlowerRecipe matched,
            out List<KeyValuePair<FlowerRecipeIngredient, int>> missing
        ) {
            matched = null;
            missing = [];
            if (providedValues == null || providedValues.Count == 0) {
                return false;
            }
            int bestDeficit = int.MaxValue;
            foreach (FlowerRecipe recipe in m_recipes) {
                // 剩余槽位：原料 → 缺口
                int[] remain = new int[recipe.Ingredients.Count];
                for (int i = 0; i < remain.Length; i++) {
                    remain[i] = recipe.Ingredients[i].Count;
                }
                bool subset = true;
                foreach (int value in providedValues) {
                    int contents = Terrain.ExtractContents(value);
                    int data = Terrain.ExtractData(value);
                    int slot = -1;
                    for (int i = 0; i < recipe.Ingredients.Count; i++) {
                        FlowerRecipeIngredient ingredient = recipe.Ingredients[i];
                        if (remain[i] > 0
                            && ingredient.Contents == contents
                            && (ingredient.ExpectedData < 0 || ingredient.ExpectedData == data)) {
                            slot = i;
                            break;
                        }
                    }
                    if (slot < 0) {
                        subset = false;
                        break;
                    }
                    remain[slot]--;
                }
                if (!subset) {
                    continue;
                }
                int deficit = 0;
                List<KeyValuePair<FlowerRecipeIngredient, int>> recipeMissing = [];
                for (int i = 0; i < recipe.Ingredients.Count; i++) {
                    if (remain[i] > 0) {
                        deficit += remain[i];
                        recipeMissing.Add(new KeyValuePair<FlowerRecipeIngredient, int>(recipe.Ingredients[i], remain[i]));
                    }
                }
                if (deficit < bestDeficit) {
                    bestDeficit = deficit;
                    matched = recipe;
                    missing = recipeMissing;
                }
            }
            return matched != null;
        }

        static FlowerRecipe DecodeRecipe(XElement element, string sourceName) {
            FlowerRecipeIngredient resultIngredient = DecodeIngredient((string)element.Attribute("Result"), sourceName);
            if (resultIngredient == null) {
                return null;
            }
            FlowerRecipe recipe = new() {
                ResultBlockName = resultIngredient.BlockName,
                ResultContents = resultIngredient.Contents,
                ResultData = resultIngredient.ExpectedData < 0 ? 0 : resultIngredient.ExpectedData,
                ResultCount = Math.Max(1, ParseInt(element.Attribute("ResultCount"), 1)),
                ManaCost = Math.Max(0f, ParseFloat(element.Attribute("ManaCost"), 0f)),
                CopyData = ParseBool(element.Attribute("CopyData")),
                SourceFile = sourceName
            };
            foreach (XElement ingredientElement in element.Elements("Ingredient")) {
                FlowerRecipeIngredient ingredient = DecodeIngredient((string)ingredientElement.Attribute("Name"), sourceName);
                if (ingredient == null) {
                    continue;
                }
                ingredient.Count = Math.Max(1, ParseInt(ingredientElement.Attribute("Count"), 1));
                recipe.Ingredients.Add(ingredient);
            }
            if (recipe.Ingredients.Count == 0) {
                Log.Warning($"[PhytoMana]FlowerTableRecipes: recipe for \"{resultIngredient.BlockName}\" in \"{sourceName}\" has no valid ingredients, skipped.");
                return null;
            }
            return recipe;
        }

        /// <summary>
        /// 解析「类名」或「类名:data」：后缀为原始 data 值（须弥花/花瓣的 16 色 data = 色号×2），
        /// 与 .cr 配方及语言键的约定一致；解析失败返回 null 并记警告。
        /// </summary>
        static FlowerRecipeIngredient DecodeIngredient(string text, string sourceName) {
            string name = text;
            int expectedData = -1;
            if (!string.IsNullOrEmpty(text)) {
                int colon = text.IndexOf(':');
                if (colon >= 0) {
                    name = text[..colon];
                    if (!int.TryParse(text[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int suffix)) {
                        Log.Warning($"[PhytoMana]FlowerTableRecipes: invalid data suffix in \"{text}\" ({sourceName}), treating as no suffix.");
                    }
                    else {
                        expectedData = suffix;
                    }
                }
            }
            int contents = string.IsNullOrWhiteSpace(name) ? -1 : BlocksManager.GetBlockIndex(name, false);
            if (contents < 0) {
                Log.Warning($"[PhytoMana]FlowerTableRecipes: unknown block \"{text}\" in \"{sourceName}\".");
                return null;
            }
            return new FlowerRecipeIngredient {
                BlockName = name,
                Contents = contents,
                ExpectedData = expectedData
            };
        }

        static bool ParseBool(XAttribute attribute) {
            string text = (string)attribute;
            return text == "true" || text == "True" || text == "1";
        }

        static int ParseInt(XAttribute attribute, int fallback) {
            string text = (string)attribute;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        }

        static float ParseFloat(XAttribute attribute, float fallback) {
            string text = (string)attribute;
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
        }
    }
}