using System.Xml.Linq;
using Engine;

namespace Game {
    public class CraftingRecipeSlotWidget : CanvasWidget {
        public BlockIconWidget m_blockIconWidget;

        public LabelWidget m_labelWidget;

        public string m_ingredient;

        public int m_resultValue;

        public int m_resultCount;

        public CraftingRecipeSlotWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/CraftingRecipeSlot");
            LoadContents(this, node);
            m_blockIconWidget = Children.Find<BlockIconWidget>("CraftingRecipeSlotWidget.Icon");
            m_labelWidget = Children.Find<LabelWidget>("CraftingRecipeSlotWidget.Count");
        }

        public void SetIngredient(string ingredient) {
            m_ingredient = ingredient;
            m_resultValue = 0;
            m_resultCount = 0;
        }

        public void SetResult(int value, int count) {
            m_resultValue = value;
            m_resultCount = count;
            m_ingredient = null;
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            m_blockIconWidget.IsVisible = false;
            m_labelWidget.IsVisible = false;
            if (!string.IsNullOrEmpty(m_ingredient)) {
                CraftingRecipesManager.DecodeIngredient(m_ingredient, out string craftingId, out int? data);
                Block[] array = BlocksManager.FindBlocksByCraftingId(craftingId);
                if (array.Length != 0) {
                    Block block = array[(int)(1.0 * Time.RealTime) % array.Length];
                    if (block != null) {
                        m_blockIconWidget.Value = Terrain.MakeBlockValue(block.BlockIndex, 0, data ?? 0);
                        m_blockIconWidget.Light = 15;
                        m_blockIconWidget.IsVisible = true;
                    }
                }
            }
            else if (m_resultValue != 0) {
                m_blockIconWidget.Value = m_resultValue;
                m_blockIconWidget.Light = 15;
                m_labelWidget.Text = m_resultCount.ToString();
                m_blockIconWidget.IsVisible = true;
                m_labelWidget.IsVisible = true;
            }
            base.MeasureOverride(parentAvailableSize);
        }
    }
}