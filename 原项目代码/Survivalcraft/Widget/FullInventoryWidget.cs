using System.Xml.Linq;
using Engine;

namespace Game {
    public class FullInventoryWidget : CanvasWidget {
        public GridPanelWidget m_inventoryGrid;

        public GridPanelWidget m_craftingGrid;

        public InventorySlotWidget m_craftingResultSlot;

        public InventorySlotWidget m_craftingRemainsSlot;

        public FullInventoryWidget(IInventory inventory, ComponentCraftingTable componentCraftingTable) {
            XElement node = ContentManager.Get<XElement>("Widgets/FullInventoryWidget");
            LoadContents(this, node);
            m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
            m_craftingGrid = Children.Find<GridPanelWidget>("CraftingGrid");
            m_craftingResultSlot = Children.Find<InventorySlotWidget>("CraftingResultSlot");
            m_craftingRemainsSlot = Children.Find<InventorySlotWidget>("CraftingRemainsSlot");
            int num = 10;
            for (int i = 0; i < m_inventoryGrid.RowsCount; i++) {
                for (int j = 0; j < m_inventoryGrid.ColumnsCount; j++) {
                    InventorySlotWidget inventorySlotWidget = new();
                    inventorySlotWidget.AssignInventorySlot(inventory, num++);
                    m_inventoryGrid.Children.Add(inventorySlotWidget);
                    m_inventoryGrid.SetWidgetCell(inventorySlotWidget, new Point2(j, i));
                }
            }
            num = 0;
            for (int k = 0; k < m_craftingGrid.RowsCount; k++) {
                for (int l = 0; l < m_craftingGrid.ColumnsCount; l++) {
                    InventorySlotWidget inventorySlotWidget2 = new();
                    inventorySlotWidget2.AssignInventorySlot(componentCraftingTable, num++);
                    m_craftingGrid.Children.Add(inventorySlotWidget2);
                    m_craftingGrid.SetWidgetCell(inventorySlotWidget2, new Point2(l, k));
                }
            }
            m_craftingResultSlot.AssignInventorySlot(componentCraftingTable, componentCraftingTable.ResultSlotIndex);
            m_craftingRemainsSlot.AssignInventorySlot(componentCraftingTable, componentCraftingTable.RemainsSlotIndex);
        }
    }
}