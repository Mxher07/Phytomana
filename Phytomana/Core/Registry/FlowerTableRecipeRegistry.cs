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
    /// </summary>
    public class FlowerRecipeIngredient {
        public string BlockName;
        public int Contents = -1;
        public int Count = 1;
    }

    /// <summary>
    /// 一条花药台配方：若干原料 → 产物（可带魔力消耗）。由外部 .fr 文件声明。
    /// </summary>
    public class FlowerRecipe {
        public string ResultBlockName;
        public int ResultContents = -1;
        public int ResultCount = 1;
        public float ManaCost;
        public List<FlowerRecipeIngredient> Ingredients = [];
        /// <summary>来源文件名（便于排查配置错误）。</summary>
        public string SourceFile;
    }

    /// <summary>
    /// 花药台配方注册表。扫描本模组内全部 <c>.fr</c> 文件（外部配方文件，规定「什么 + 什么」在花药台合成），
    /// 解析为 <see cref="FlowerRecipe"/> 并提供无序匹配查询，供花药台合成逻辑使用。
    /// 加载方式与游戏 .cr 合成表一致：按扩展名收集文件流并解码。
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
        /// 无序匹配：提供的原料（方块值列表）恰好满足某配方时返回该配方（数量须完全吻合，不允许多余）。
        /// </summary>
        public static bool TryMatch(IList<int> providedContents, out FlowerRecipe matched) {
            matched = null;
            if (providedContents == null || providedContents.Count == 0) {
                return false;
            }
            foreach (FlowerRecipe recipe in m_recipes) {
                if (Matches(recipe, providedContents)) {
                    matched = recipe;
                    return true;
                }
            }
            return false;
        }

        static bool Matches(FlowerRecipe recipe, IList<int> provided) {
            int requiredTotal = 0;
            foreach (FlowerRecipeIngredient ingredient in recipe.Ingredients) {
                requiredTotal += ingredient.Count;
            }
            if (provided.Count != requiredTotal) {
                return false;
            }
            Dictionary<int, int> providedCounts = [];
            foreach (int contents in provided) {
                providedCounts[contents] = providedCounts.GetValueOrDefault(contents) + 1;
            }
            foreach (FlowerRecipeIngredient ingredient in recipe.Ingredients) {
                if (providedCounts.GetValueOrDefault(ingredient.Contents) < ingredient.Count) {
                    return false;
                }
            }
            return true;
        }

        static FlowerRecipe DecodeRecipe(XElement element, string sourceName) {
            string resultName = (string)element.Attribute("Result");
            int resultContents = string.IsNullOrWhiteSpace(resultName) ? -1 : BlocksManager.GetBlockIndex(resultName, false);
            if (resultContents < 0) {
                Log.Warning($"[PhytoMana]FlowerTableRecipes: unknown Result \"{resultName}\" in \"{sourceName}\", recipe skipped.");
                return null;
            }
            FlowerRecipe recipe = new() {
                ResultBlockName = resultName,
                ResultContents = resultContents,
                ResultCount = Math.Max(1, ParseInt(element.Attribute("ResultCount"), 1)),
                ManaCost = Math.Max(0f, ParseFloat(element.Attribute("ManaCost"), 0f)),
                SourceFile = sourceName
            };
            foreach (XElement ingredientElement in element.Elements("Ingredient")) {
                string ingredientName = (string)ingredientElement.Attribute("Name");
                int ingredientContents = string.IsNullOrWhiteSpace(ingredientName) ? -1 : BlocksManager.GetBlockIndex(ingredientName, false);
                if (ingredientContents < 0) {
                    Log.Warning($"[PhytoMana]FlowerTableRecipes: unknown Ingredient \"{ingredientName}\" in \"{sourceName}\", ingredient skipped.");
                    continue;
                }
                recipe.Ingredients.Add(new FlowerRecipeIngredient {
                    BlockName = ingredientName,
                    Contents = ingredientContents,
                    Count = Math.Max(1, ParseInt(ingredientElement.Attribute("Count"), 1))
                });
            }
            if (recipe.Ingredients.Count == 0) {
                Log.Warning($"[PhytoMana]FlowerTableRecipes: recipe for \"{resultName}\" in \"{sourceName}\" has no valid ingredients, skipped.");
                return null;
            }
            return recipe;
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
