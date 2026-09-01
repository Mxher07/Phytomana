using System.Xml.Linq;
using Engine;

namespace Game {
    public class SmeltingRecipeWidget : CanvasWidget {
        public LabelWidget m_nameWidget;

        public LabelWidget m_descriptionWidget;

        public GridPanelWidget m_gridWidget;

        public FireWidget m_fireWidget;

        public CraftingRecipeSlotWidget m_resultWidget;

        public CraftingRecipe m_recipe;

        public string m_nameSuffix;

        public bool m_dirty = true;

        public string NameSuffix {
            get => m_nameSuffix;
            set {
                if (value != m_nameSuffix) {
                    m_nameSuffix = value;
                    m_dirty = true;
                }
            }
        }

        public CraftingRecipe Recipe {
            get => m_recipe;
            set {
                if (value != m_recipe) {
                    m_recipe = value;
                    m_dirty = true;
                }
            }
        }

        public SmeltingRecipeWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/SmeltingRecipe");
            LoadContents(this, node);
            m_nameWidget = Children.Find<LabelWidget>("SmeltingRecipeWidget.Name");
            m_descriptionWidget = Children.Find<LabelWidget>("SmeltingRecipeWidget.Description");
            m_gridWidget = Children.Find<GridPanelWidget>("SmeltingRecipeWidget.Ingredients");
            m_fireWidget = Children.Find<FireWidget>("SmeltingRecipeWidget.Fire");
            m_resultWidget = Children.Find<CraftingRecipeSlotWidget>("SmeltingRecipeWidget.Result");
            for (int i = 0; i < m_gridWidget.RowsCount; i++) {
                for (int j = 0; j < m_gridWidget.ColumnsCount; j++) {
                    CraftingRecipeSlotWidget widget = new();
                    m_gridWidget.Children.Add(widget);
                    m_gridWidget.SetWidgetCell(widget, new Point2(j, i));
                }
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            if (m_dirty) {
                UpdateWidgets();
            }
            base.MeasureOverride(parentAvailableSize);
        }

        public virtual void UpdateWidgets() {
            m_dirty = false;
            if (m_recipe != null) {
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(m_recipe.ResultValue)];
                m_nameWidget.Text = block.GetDisplayName(null, m_recipe.ResultValue)
                    + (!string.IsNullOrEmpty(NameSuffix) ? NameSuffix : string.Empty);
                m_descriptionWidget.Text = m_recipe.Description;
                m_nameWidget.IsVisible = true;
                m_descriptionWidget.IsVisible = true;
                foreach (Widget widget in m_gridWidget.Children) {
                    if (widget is CraftingRecipeSlotWidget child) {
                        Point2 widgetCell = m_gridWidget.GetWidgetCell(child);
                        child.SetIngredient(m_recipe.Ingredients[widgetCell.X + widgetCell.Y * 3]);
                    }
                }
                m_resultWidget.SetResult(m_recipe.ResultValue, m_recipe.ResultCount);
                m_fireWidget.ParticlesPerSecond = 40f;
            }
            else {
                m_nameWidget.IsVisible = false;
                m_descriptionWidget.IsVisible = false;
                foreach (Widget widget in m_gridWidget.Children) {
                    (widget as CraftingRecipeSlotWidget)?.SetIngredient(null);
                }
                m_resultWidget.SetResult(0, 0);
                m_fireWidget.ParticlesPerSecond = 0f;
            }
        }
    }
}