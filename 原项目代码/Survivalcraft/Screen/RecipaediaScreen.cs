using System.Xml.Linq;
using Engine;

namespace Game {
    public class RecipaediaScreen : Screen {
        public class Order {
            public Block block;
            public int order;
            public int value;

            public Order(Block b, int o, int v) {
                block = b;
                order = o;
                value = v;
            }
        }

        public ListPanelWidget m_blocksList;
        public LabelWidget m_categoryLabel;
        public ButtonWidget m_prevCategoryButton;
        public ButtonWidget m_nextCategoryButton;
        public ButtonWidget m_detailsButton;
        public ButtonWidget m_recipesButton;
        public Screen m_previousScreen;

        public List<string> m_categories = [];
        public int m_categoryIndex;
        public int m_listCategoryIndex = -1;
        public const string fName = "RecipaediaScreen";

        /// <summary>
        ///     在方块项目被点击时执行
        ///     改用实名方法，便于模组删除或禁用
        /// </summary>
        /// <param name="item"></param>
        public virtual void OnBlocksListItemClicked(object item) {
            if (m_blocksList.SelectedItem == item
                && item is int) {
                int value = (int)item;
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
                ScreensManager.m_screens["RecipaediaDescription"] = block.GetBlockDescriptionScreen(value);
                ScreensManager.SwitchScreen("RecipaediaDescription", item, m_blocksList.Items.Cast<int>().ToList());
            }
        }

        public RecipaediaScreen() {
            XElement node = ContentManager.Get<XElement>("Screens/RecipaediaScreen");
            LoadContents(this, node);
            m_blocksList = Children.Find<ListPanelWidget>("BlocksList");
            m_categoryLabel = Children.Find<LabelWidget>("Category");
            m_prevCategoryButton = Children.Find<ButtonWidget>("PreviousCategory");
            m_nextCategoryButton = Children.Find<ButtonWidget>("NextCategory");
            m_detailsButton = Children.Find<ButtonWidget>("DetailsButton");
            m_recipesButton = Children.Find<ButtonWidget>("RecipesButton");
            m_categories.Add(null);
            m_categories.AddRange(BlocksManager.Categories);
            m_blocksList.ItemWidgetFactory = delegate(object item) {
                int value = (int)item;
                int num = Terrain.ExtractContents(value);
                Block block = BlocksManager.Blocks[num];
                XElement node2 = ContentManager.Get<XElement>("Widgets/RecipaediaItem");
                ContainerWidget obj = (ContainerWidget)LoadWidget(this, node2, null);
                obj.Children.Find<BlockIconWidget>("RecipaediaItem.Icon").Value = value;
                obj.Children.Find<LabelWidget>("RecipaediaItem.Text").Text = block.GetDisplayName(null, value);
                string description = block.GetDescription(value);
                description = description.Replace("\n", "  ");
                obj.Children.Find<LabelWidget>("RecipaediaItem.Details").Text = description;
                return obj;
            };
            m_blocksList.ItemClicked += OnBlocksListItemClicked;
        }

        public override void Enter(object[] parameters) {
            if (ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("RecipaediaRecipes")
                && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("RecipaediaDescription")) {
                m_previousScreen = ScreensManager.PreviousScreen;
            }
        }

        public override void Update() {
            if (m_listCategoryIndex != m_categoryIndex) {
                PopulateBlocksList();
            }
            string arg = m_categories[m_categoryIndex] == null
                ? LanguageControl.Get("BlocksManager", "All Blocks")
                : LanguageControl.Get("BlocksManager", m_categories[m_categoryIndex]);
            m_categoryLabel.Text = $"{arg} ({m_blocksList.Items.Count})";
            m_prevCategoryButton.IsEnabled = m_categoryIndex > 0;
            m_nextCategoryButton.IsEnabled = m_categoryIndex < m_categories.Count - 1;
            int? value = null;
            int num = 0;
            if (m_blocksList.SelectedItem is int) {
                value = (int)m_blocksList.SelectedItem;
                num = CraftingRecipesManager.Recipes.Count(r => r.ResultValue == value);
            }
            if (num > 0) {
                m_recipesButton.Text = $"{num} {(num == 1 ? LanguageControl.Get(fName, 1) : LanguageControl.Get(fName, 2))}";
                m_recipesButton.IsEnabled = true;
            }
            else {
                m_recipesButton.Text = LanguageControl.Get(fName, 3);
                m_recipesButton.IsEnabled = false;
            }
            m_detailsButton.IsEnabled = value.HasValue;
            if (m_prevCategoryButton.IsClicked
                || Input.Left) {
                m_categoryIndex = MathUtils.Max(m_categoryIndex - 1, 0);
            }
            if (m_nextCategoryButton.IsClicked
                || Input.Right) {
                m_categoryIndex = MathUtils.Min(m_categoryIndex + 1, m_categories.Count - 1);
            }
            if (value.HasValue
                && m_detailsButton.IsClicked) {
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(value.Value)];
                ScreensManager.m_screens["RecipaediaDescription"] = block.GetBlockDescriptionScreen(value.Value);
                ScreensManager.SwitchScreen("RecipaediaDescription", value.Value, m_blocksList.Items.Cast<int>().ToList());
            }
            if (value.HasValue
                && m_recipesButton.IsClicked) {
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(value.Value)];
                ScreensManager.m_screens["RecipaediaRecipes"] = block.GetBlockRecipeScreen(value.Value);
                ScreensManager.SwitchScreen("RecipaediaRecipes", value.Value);
            }
            if (Input.Back
                || Input.Cancel
                || Children.Find<ButtonWidget>("TopBar.Back").IsClicked) {
                ScreensManager.SwitchScreen(m_previousScreen);
            }
        }

        public void PopulateBlocksList() {
            m_listCategoryIndex = m_categoryIndex;
            string text = m_categories[m_categoryIndex];
            m_blocksList.ScrollPosition = 0f;
            m_blocksList.ClearItems();
            List<Order> orders = [];
            foreach (Block item in BlocksManager.Blocks) {
                foreach (int creativeValue in item.GetCreativeValues()) {
                    if (string.IsNullOrEmpty(text)
                        || item.GetCategory(creativeValue) == text) {
                        orders.Add(new Order(item, item.GetDisplayOrder(creativeValue), creativeValue));
                    }
                }
            }
            IOrderedEnumerable<Order> orderList = orders.OrderBy(o => o.order);
            foreach (Order c in orderList) {
                m_blocksList.AddItem(c.value);
            }
        }
    }
}