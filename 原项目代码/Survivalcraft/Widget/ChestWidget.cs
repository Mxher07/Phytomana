using System.Xml.Linq;
using Engine;

namespace Game {
    public class ChestWidget : CanvasWidget {
        public ComponentChest m_componentChest;

        public GridPanelWidget m_inventoryGrid;

        public GridPanelWidget m_chestGrid;

        public ChestWidget(IInventory inventory, ComponentChest componentChest) {
            m_componentChest = componentChest;
            XElement node = ContentManager.Get<XElement>("Widgets/ChestWidget");
            LoadContents(this, node);
            m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
            m_chestGrid = Children.Find<GridPanelWidget>("ChestGrid");
            int num = 0;
            for (int i = 0; i < m_chestGrid.RowsCount; i++) {
                for (int j = 0; j < m_chestGrid.ColumnsCount; j++) {
                    InventorySlotWidget inventorySlotWidget = new();
                    inventorySlotWidget.AssignInventorySlot(componentChest, num++);
                    m_chestGrid.Children.Add(inventorySlotWidget);
                    m_chestGrid.SetWidgetCell(inventorySlotWidget, new Point2(j, i));
                }
            }
            num = 10;
            for (int k = 0; k < m_inventoryGrid.RowsCount; k++) {
                for (int l = 0; l < m_inventoryGrid.ColumnsCount; l++) {
                    InventorySlotWidget inventorySlotWidget2 = new();
                    inventorySlotWidget2.AssignInventorySlot(inventory, num++);
                    m_inventoryGrid.Children.Add(inventorySlotWidget2);
                    m_inventoryGrid.SetWidgetCell(inventorySlotWidget2, new Point2(l, k));
                }
            }
        }

        public override void Update() {
            if (!m_componentChest.IsAddedToProject) {
                ParentWidget.Children.Remove(this);
            }
        }
    }
}