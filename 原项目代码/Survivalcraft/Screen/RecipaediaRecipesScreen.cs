using System.Xml.Linq;
using Engine;

namespace Game {
    public class RecipaediaRecipesScreen : Screen {
        public CraftingRecipeWidget m_craftingRecipeWidget;
        public SmeltingRecipeWidget m_smeltingRecipeWidget;
        public ButtonWidget m_prevRecipeButton;
        public ButtonWidget m_nextRecipeButton;

        public int m_recipeIndex;
        public List<CraftingRecipe> m_craftingRecipes = [];
        public int RecipesCount;
        public const string fName = "RecipaediaRecipesScreen";

        public static RecipaediaRecipesScreen Default => new();

        public RecipaediaRecipesScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/RecipaediaRecipesScreen");
            LoadContents(this, node);
            m_craftingRecipeWidget = Children.Find<CraftingRecipeWidget>("CraftingRecipe");
            m_smeltingRecipeWidget = Children.Find<SmeltingRecipeWidget>("SmeltingRecipe");
            m_prevRecipeButton = Children.Find<ButtonWidget>("PreviousRecipe");
            m_nextRecipeButton = Children.Find<ButtonWidget>("NextRecipe");
        }

        public override void Enter(object[] parameters) {
            int value = (int)parameters[0];
            m_craftingRecipes.Clear();
            m_craftingRecipes.AddRange(CraftingRecipesManager.Recipes.Where(r => r.ResultValue == value && r.ResultValue != 0));
            RecipesCount = m_craftingRecipes.Count;
            m_recipeIndex = 0;
        }

        public override void Update() {
            if (m_recipeIndex < RecipesCount) {
                if (m_recipeIndex < m_craftingRecipes.Count) {
                    CraftingRecipe craftingRecipe = m_craftingRecipes[m_recipeIndex];
                    if (craftingRecipe.RequiredHeatLevel == 0f) {
                        m_craftingRecipeWidget.Recipe = craftingRecipe;
                        m_craftingRecipeWidget.NameSuffix = string.Format(LanguageControl.GetContentWidgets(fName, 1), m_recipeIndex + 1);
                        m_craftingRecipeWidget.IsVisible = true;
                        m_smeltingRecipeWidget.IsVisible = false;
                    }
                    else {
                        m_smeltingRecipeWidget.Recipe = craftingRecipe;
                        m_smeltingRecipeWidget.NameSuffix = string.Format(LanguageControl.GetContentWidgets(fName, 1), m_recipeIndex + 1);
                        m_smeltingRecipeWidget.IsVisible = true;
                        m_craftingRecipeWidget.IsVisible = false;
                    }
                }
                else {
                    m_craftingRecipeWidget.IsVisible = false;
                    m_smeltingRecipeWidget.IsVisible = false;
                }
                ModsManager.HookAction(
                    "EditRecipeScreenWidget",
                    loader => {
                        loader.EditRecipeScreenWidget(this);
                        return false;
                    }
                );
            }
            m_prevRecipeButton.IsEnabled = m_recipeIndex > 0;
            m_nextRecipeButton.IsEnabled = m_recipeIndex < RecipesCount - 1;
            if (m_prevRecipeButton.IsClicked) {
                m_recipeIndex = MathUtils.Max(m_recipeIndex - 1, 0);
            }
            if (m_nextRecipeButton.IsClicked) {
                m_recipeIndex = MathUtils.Min(m_recipeIndex + 1, RecipesCount - 1);
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
            }
        }
    }
}