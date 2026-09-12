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
    /// 符文台配方的一种原料：方块 + 数量。
    /// Name 支持「类名:data」后缀精确到特殊值（如 MediumRunesBlock:5 = 夏之符文）；
    /// 不带后缀表示不限特殊值（如任意颜色的须弥花、任意树苗）。
    /// Remain 指定合成后返还的方块（如水桶返还空桶）。
    /// 符文类的原料在生存模式下不消耗，合成完成时随成品一起返还。
    /// </summary>
    public class RunesRecipeIngredient {
        public string BlockName;
        public int Contents = -1;
        /// <summary>要求的 data 值；-1 表示不限。</summary>
        public int ExpectedData = -1;
        public int Count = 1;
        /// <summary>合成后返还的方块内容索引；-1 表示无返还。</summary>
        public int RemainContents = -1;
        /// <summary>是否为符文类原料（生存模式不消耗，随成品返还）。</summary>
        public bool IsRune;
    }

    /// <summary>
    /// 一条符文台配方：若干原料 → 产物符文（可带魔力消耗）。由外部 .rr 文件声明。
    /// </summary>
    public class RunesRecipe {
        public string ResultBlockName;
        public int ResultContents = -1;
        public int ResultData;
        public int ResultCount = 1;
        public float ManaCost;
        public List<RunesRecipeIngredient> Ingredients = [];
        /// <summary>来源文件名（便于排查配置错误）。</summary>
        public string SourceFile;
    }

    /// <summary>
    /// 符文台配方注册表。扫描本模组全部 <c>.rr</c> 文件（外部配方文件），解析为
    /// <see cref="RunesRecipe"/> 并提供无序匹配查询。匹配基于完整方块值：
    /// 原料可指定特殊值，提供的原料须与配方在种类、特殊值、数量上完全一致。
    /// </summary>
    public static class RunesRecipeRegistry {
        public const string Extension = ".rr";

        static readonly List<RunesRecipe> m_recipes = [];

        public static bool IsInitialized { get; private set; }

        public static IReadOnlyList<RunesRecipe> Recipes => m_recipes;

        public static int Count => m_recipes.Count;

        /// <summary>由 PhytomanaMod 在 BlocksInitalized 时机调用。</summary>
        internal static void Initialize(ModEntity entity) {
            m_recipes.Clear();
            if (entity != null) {
                try {
                    entity.GetFiles(Extension, (name, stream) => LoadFromStream(stream, name));
                }
                catch (Exception e) {
                    Log.Error($"[PhytoMana]RunesRecipes: failed to scan {Extension} files: {e}");
                }
            }
            IsInitialized = true;
            Log.Information($"[PhytoMana]RunesRecipeRegistry: {m_recipes.Count} runes table recipes loaded.");
        }

        /// <summary>解析单个 .rr 文件流，将其中所有 &lt;Recipe&gt; 追加进注册表。</summary>
        public static void LoadFromStream(Stream stream, string sourceName) {
            XElement root;
            try {
                root = XmlUtils.LoadXmlFromStream(stream, null, true);
            }
            catch (Exception e) {
                Log.Error($"[PhytoMana]RunesRecipes: failed to parse \"{sourceName}\": {e}");
                return;
            }
            if (root == null) {
                return;
            }
            foreach (XElement element in root.Elements("Recipe")) {
                RunesRecipe recipe = DecodeRecipe(element, sourceName);
                if (recipe != null) {
                    m_recipes.Add(recipe);
                }
            }
        }

        /// <summary>
        /// 无序精确匹配：提供的原料（完整方块值列表）在种类、特殊值、数量上恰好满足某配方时返回该配方。
        /// </summary>
        public static bool TryMatch(IList<int> providedValues, out RunesRecipe matched) {
            matched = null;
            if (providedValues == null || providedValues.Count == 0) {
                return false;
            }
            foreach (RunesRecipe recipe in m_recipes) {
                if (Matches(recipe, providedValues)) {
                    matched = recipe;
                    return true;
                }
            }
            return false;
        }

        static bool Matches(RunesRecipe recipe, IList<int> providedValues) {
            int requiredTotal = 0;
            foreach (RunesRecipeIngredient ingredient in recipe.Ingredients) {
                requiredTotal += ingredient.Count;
            }
            if (providedValues.Count != requiredTotal) {
                return false;
            }
            Dictionary<KeyValuePair<int, int>, int> available = [];
            foreach (int value in providedValues) {
                KeyValuePair<int, int> key = new(Terrain.ExtractContents(value), Terrain.ExtractData(value));
                available[key] = available.GetValueOrDefault(key) + 1;
            }
            foreach (RunesRecipeIngredient ingredient in recipe.Ingredients) {
                int remain = ingredient.Count;
                if (ingredient.ExpectedData >= 0) {
                    KeyValuePair<int, int> key = new(ingredient.Contents, ingredient.ExpectedData);
                    int take = Math.Min(remain, available.GetValueOrDefault(key));
                    available[key] = available.GetValueOrDefault(key) - take;
                    remain -= take;
                }
                else {
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
        /// 用于符文台状态提示（还差哪些材料）。missing 列出每种原料槽位的缺口数量。
        /// </summary>
        public static bool TryMatchClosest(
            IList<int> providedValues,
            out RunesRecipe matched,
            out List<KeyValuePair<RunesRecipeIngredient, int>> missing
        ) {
            matched = null;
            missing = [];
            if (providedValues == null || providedValues.Count == 0) {
                return false;
            }
            int bestDeficit = int.MaxValue;
            foreach (RunesRecipe recipe in m_recipes) {
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
                        RunesRecipeIngredient ingredient = recipe.Ingredients[i];
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
                List<KeyValuePair<RunesRecipeIngredient, int>> recipeMissing = [];
                for (int i = 0; i < recipe.Ingredients.Count; i++) {
                    if (remain[i] > 0) {
                        deficit += remain[i];
                        recipeMissing.Add(new KeyValuePair<RunesRecipeIngredient, int>(recipe.Ingredients[i], remain[i]));
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

        /// <summary>原料是否出现在任意 .rr 配方中（只收配方认识的材料）。</summary>
        public static bool IsKnownIngredient(int contents) {
            return MaxRequiredCount(contents) > 0;
        }

        /// <summary>该原料在所有配方中的最大需求数，作为放置上限防止过量投入。</summary>
        public static int MaxRequiredCount(int contents) {
            int max = 0;
            foreach (RunesRecipe recipe in m_recipes) {
                foreach (RunesRecipeIngredient ingredient in recipe.Ingredients) {
                    if (ingredient.Contents == contents) {
                        max = Math.Max(max, ingredient.Count);
                    }
                }
            }
            return max;
        }

        static RunesRecipe DecodeRecipe(XElement element, string sourceName) {
            RunesRecipeIngredient resultIngredient = DecodeIngredient((string)element.Attribute("Result"), sourceName);
            if (resultIngredient == null) {
                return null;
            }
            RunesRecipe recipe = new() {
                ResultBlockName = resultIngredient.BlockName,
                ResultContents = resultIngredient.Contents,
                ResultData = resultIngredient.ExpectedData < 0 ? 0 : resultIngredient.ExpectedData,
                ResultCount = Math.Max(1, ParseInt(element.Attribute("ResultCount"), 1)),
                ManaCost = Math.Max(0f, ParseFloat(element.Attribute("ManaCost"), 0f)),
                SourceFile = sourceName
            };
            foreach (XElement ingredientElement in element.Elements("Ingredient")) {
                RunesRecipeIngredient ingredient = DecodeIngredient((string)ingredientElement.Attribute("Name"), sourceName);
                if (ingredient == null) {
                    continue;
                }
                ingredient.Count = Math.Max(1, ParseInt(ingredientElement.Attribute("Count"), 1));
                string remain = (string)ingredientElement.Attribute("Remain");
                if (!string.IsNullOrWhiteSpace(remain)) {
                    ingredient.RemainContents = BlocksManager.GetBlockIndex(remain, false);
                    if (ingredient.RemainContents < 0) {
                        Log.Warning($"[PhytoMana]RunesRecipes: unknown Remain \"{remain}\" in \"{sourceName}\".");
                    }
                }
                recipe.Ingredients.Add(ingredient);
            }
            if (recipe.Ingredients.Count == 0) {
                Log.Warning($"[PhytoMana]RunesRecipes: recipe for \"{resultIngredient.BlockName}\" in \"{sourceName}\" has no valid ingredients, skipped.");
                return null;
            }
            return recipe;
        }

        /// <summary>
        /// 解析「类名」或「类名:data」（data 为原始特殊值，如 MediumRunesBlock:5 = 夏之符文）；
        /// 符文类方块自动标记 IsRune；解析失败返回 null 并记警告。
        /// </summary>
        static RunesRecipeIngredient DecodeIngredient(string text, string sourceName) {
            string name = text;
            int expectedData = -1;
            if (!string.IsNullOrEmpty(text)) {
                int colon = text.IndexOf(':');
                if (colon >= 0) {
                    name = text[..colon];
                    if (!int.TryParse(text[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int suffix)) {
                        Log.Warning($"[PhytoMana]RunesRecipes: invalid data suffix in \"{text}\" ({sourceName}), treating as no suffix.");
                    }
                    else {
                        expectedData = suffix;
                    }
                }
            }
            int contents = string.IsNullOrWhiteSpace(name) ? -1 : BlocksManager.GetBlockIndex(name, false);
            if (contents < 0) {
                Log.Warning($"[PhytoMana]RunesRecipes: unknown block \"{text}\" in \"{sourceName}\".");
                return null;
            }
            Block block = BlocksManager.Blocks[contents];
            return new RunesRecipeIngredient {
                BlockName = name,
                Contents = contents,
                ExpectedData = expectedData,
                IsRune = block is BasicRunesBlock or MediumRunesBlock or ExpertRunesBlock
            };
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