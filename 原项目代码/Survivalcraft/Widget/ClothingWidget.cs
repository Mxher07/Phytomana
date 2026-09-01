using System.Xml.Linq;
using Engine;

namespace Game {
    public class ClothingWidget : CanvasWidget {
        public GridPanelWidget m_inventoryGrid;

        public StackPanelWidget m_clothingStack;

        public ButtonWidget m_vitalStatsButton;

        public ButtonWidget m_sleepButton;

        public PlayerModelWidget m_innerClothingModelWidget;

        [Obsolete("m_innerClothingModelWidget contains all clothes, including outer clothes.")]
        public PlayerModelWidget m_outerClothingModelWidget;

        public ComponentPlayer m_componentPlayer;

        public ClothingWidget(ComponentPlayer componentPlayer) {
            m_componentPlayer = componentPlayer;
            XElement node = ContentManager.Get<XElement>("Widgets/ClothingWidget");
            LoadContents(this, node);
            m_clothingStack = Children.Find<StackPanelWidget>("ClothingStack");
            m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
            m_vitalStatsButton = Children.Find<ButtonWidget>("VitalStatsButton");
            m_sleepButton = Children.Find<ButtonWidget>("SleepButton");
            m_innerClothingModelWidget = Children.Find<PlayerModelWidget>("InnerClothingModel");
#pragma warning disable CS0618
            m_outerClothingModelWidget = Children.Find<PlayerModelWidget>("OuterClothingModel");
#pragma warning restore CS0618
            for (int i = 0; i < 4; i++) {
                InventorySlotWidget inventorySlotWidget = new();
                float y = float.PositiveInfinity;
                if (i == 0) {
                    y = 68f;
                }
                if (i == 3) {
                    y = 54f;
                }
                inventorySlotWidget.Size = new Vector2(float.PositiveInfinity, y);
                inventorySlotWidget.BevelColor = Color.Transparent;
                inventorySlotWidget.CenterColor = Color.Transparent;
                inventorySlotWidget.AssignInventorySlot(m_componentPlayer.ComponentClothing, i);
                inventorySlotWidget.HideEditOverlay = true;
                inventorySlotWidget.HideInteractiveOverlay = true;
                inventorySlotWidget.HideFoodOverlay = true;
                inventorySlotWidget.HideHighlightRectangle = true;
                inventorySlotWidget.HideBlockIcon = true;
                inventorySlotWidget.HideHealthBar = m_componentPlayer.Project.FindSubsystem<SubsystemGameInfo>(true).WorldSettings.GameMode
                    == GameMode.Creative;
                m_clothingStack.Children.Add(inventorySlotWidget);
            }
            int num = 10;
            for (int j = 0; j < m_inventoryGrid.RowsCount; j++) {
                for (int k = 0; k < m_inventoryGrid.ColumnsCount; k++) {
                    InventorySlotWidget inventorySlotWidget2 = new();
                    inventorySlotWidget2.AssignInventorySlot(componentPlayer.ComponentMiner.Inventory, num++);
                    m_inventoryGrid.Children.Add(inventorySlotWidget2);
                    m_inventoryGrid.SetWidgetCell(inventorySlotWidget2, new Point2(k, j));
                }
            }
            m_innerClothingModelWidget.PlayerClass = componentPlayer.PlayerData.PlayerClass;
            m_innerClothingModelWidget.CharacterSkinTexture = m_componentPlayer.ComponentClothing.InnerClothedTexture;
            m_innerClothingModelWidget.OuterClothingTexture = m_componentPlayer.ComponentClothing.OuterClothedTexture;
            m_innerClothingModelWidget.PlayerData = componentPlayer.PlayerData;
        }

        public override void Update() {
            if (m_vitalStatsButton.IsClicked
                && m_componentPlayer != null) {
                m_componentPlayer.ComponentGui.ModalPanelWidget = new VitalStatsWidget(m_componentPlayer);
            }
            if (m_sleepButton.IsClicked
                && m_componentPlayer != null) {
                if (!m_componentPlayer.ComponentSleep.CanSleep(out string reason)) {
                    m_componentPlayer.ComponentGui.DisplaySmallMessage(reason, Color.White, false, false);
                }
                else {
                    m_componentPlayer.ComponentSleep.Sleep(true);
                }
            }
        }
    }
}