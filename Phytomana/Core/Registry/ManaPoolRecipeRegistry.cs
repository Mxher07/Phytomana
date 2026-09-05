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
    /// 一条魔力池配方：投入 1 个原料（可叠加投放）→ 产出若干产物，消耗固定魔力。
    /// 由外部 .mp 文件声明。
    /// </summary>
    public class ManaPoolRecipe {
        public string IngredientBlockName;
        public int IngredientContents = -1;
        public string ResultBlockName;
        public int ResultContents = -1;
        public int ResultCount = 1;
        public float ManaCost;
        /// <summary>来源文件名（便于排查配置错误）。</summary>
        public string SourceFile;
    }

    /// <summary>
    /// 魔力池配方注册表。扫描本模组内全部 <c>.mp</c> 文件（魔力池外部配方文件，规定
    /// 「什么物品 + 多少魔力 → 什么产物」），解析为 <see cref="ManaPoolRecipe"/> 并提供匹配查询，
    /// 供魔法池的物品转化逻辑使用。加载方式与 .fr 花药台配方一致。
    /// </summary>
    public static class ManaPoolRecipeRegistry {
        public const string Extension = ".mp";

        static readonly List<ManaPoolRecipe> m_recipes = [];

        public static bool IsInitialized { get; private set; }

        public static IReadOnlyList<ManaPoolRecipe> Recipes => m_recipes;

        public static int Count => m_recipes.Count;

        /// <summary>由 PhytomanaMod 在 BlocksInitalized 时机调用：扫描本模组全部 .mp 文件并解析。</summary>
        internal static void Initialize(ModEntity entity) {
            m_recipes.Clear();
            if (entity != null) {
                try {
                    entity.GetFiles(Extension, (name, stream) => LoadFromStream(stream, name));
                }
                catch (Exception e) {
                    Log.Error($"[PhytoMana]ManaPoolRecipes: failed to scan {Extension} files: {e}");
                }
            }
            IsInitialized = true;
            Log.Information($"[PhytoMana]ManaPoolRecipeRegistry: {m_recipes.Count} mana pool recipes loaded.");
        }

        /// <summary>解析单个 .mp 文件流，将其中所有 &lt;Recipe&gt; 追加进注册表。</summary>
        public static void LoadFromStream(Stream stream, string sourceName) {
            XElement root;
            try {
                root = XmlUtils.LoadXmlFromStream(stream, null, true);
            }
            catch (Exception e) {
                Log.Error($"[PhytoMana]ManaPoolRecipes: failed to parse \"{sourceName}\": {e}");
                return;
            }
            if (root == null) {
                return;
            }
            foreach (XElement element in root.Elements("Recipe")) {
                ManaPoolRecipe recipe = DecodeRecipe(element, sourceName);
                if (recipe != null) {
                    m_recipes.Add(recipe);
                }
            }
        }

        /// <summary>按原料方块索引查找配方（魔力池转化按物品类型逐一注入）。</summary>
        public static ManaPoolRecipe FindByIngredient(int contents) {
            foreach (ManaPoolRecipe recipe in m_recipes) {
                if (recipe.IngredientContents == contents) {
                    return recipe;
                }
            }
            return null;
        }

        /// <summary>全部配方的最低魔力消耗，供 Update 做廉价的提前判断。</summary>
        public static float MinManaCost() {
            float min = float.MaxValue;
            foreach (ManaPoolRecipe recipe in m_recipes) {
                if (recipe.ManaCost < min) {
                    min = recipe.ManaCost;
                }
            }
            return m_recipes.Count > 0 ? min : float.MaxValue;
        }

        static ManaPoolRecipe DecodeRecipe(XElement element, string sourceName) {
            string ingredientName = (string)element.Attribute("Ingredient");
            string resultName = (string)element.Attribute("Result");
            int ingredientContents = string.IsNullOrWhiteSpace(ingredientName) ? -1 : BlocksManager.GetBlockIndex(ingredientName, false);
            int resultContents = string.IsNullOrWhiteSpace(resultName) ? -1 : BlocksManager.GetBlockIndex(resultName, false);
            if (ingredientContents < 0) {
                Log.Warning($"[PhytoMana]ManaPoolRecipes: unknown Ingredient \"{ingredientName}\" in \"{sourceName}\", recipe skipped.");
                return null;
            }
            if (resultContents < 0) {
                Log.Warning($"[PhytoMana]ManaPoolRecipes: unknown Result \"{resultName}\" in \"{sourceName}\", recipe skipped.");
                return null;
            }
            if (FindByIngredientLoaded(ingredientContents) != null) {
                Log.Warning($"[PhytoMana]ManaPoolRecipes: duplicate recipe for \"{ingredientName}\" in \"{sourceName}\", recipe skipped.");
                return null;
            }
            return new ManaPoolRecipe {
                IngredientBlockName = ingredientName,
                IngredientContents = ingredientContents,
                ResultBlockName = resultName,
                ResultContents = resultContents,
                ResultCount = Math.Max(1, ParseInt(element.Attribute("ResultCount"), 1)),
                ManaCost = Math.Max(0f, ParseFloat(element.Attribute("ManaCost"), 0f)),
                SourceFile = sourceName
            };
        }

        static ManaPoolRecipe FindByIngredientLoaded(int contents) {
            foreach (ManaPoolRecipe recipe in m_recipes) {
                if (recipe.IngredientContents == contents) {
                    return recipe;
                }
            }
            return null;
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