using System.Xml.Linq;
using Engine;

namespace Game {
    public class ShortInventoryWidget : CanvasWidget {
        public GridPanelWidget m_inventoryGrid;

        public IInventory m_inventory;

        public int MinVisibleSlotsCount = 7;

        public int MaxVisibleSlotsCount = 7;

        public int MaxVisibleSlotsCountInCreative = 10;

        public ShortInventoryWidget() {
            XElement node = ContentManager.Get<XElement>("Widgets/ShortInventoryWidget");
            LoadContents(this, node);
            m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
        }

        public void AssignComponents(IInventory inventory) {
            if (inventory != m_inventory) {
                m_inventory = inventory;
                m_inventoryGrid.Children.Clear();
            }
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            if (m_inventory == null) return;
            int max = m_inventory is ComponentCreativeInventory ? MaxVisibleSlotsCountInCreative : MaxVisibleSlotsCount;
            m_inventory.VisibleSlotsCount = Math.Clamp((int)((parentAvailableSize.X - 320f - 25f) / 72f), MinVisibleSlotsCount, max);
            if (m_inventory.VisibleSlotsCount != m_inventoryGrid.Children.Count) {
                m_inventoryGrid.Children.Clear();
                m_inventoryGrid.RowsCount = 1;
                m_inventoryGrid.ColumnsCount = m_inventory.VisibleSlotsCount;
                for (int i = 0; i < m_inventoryGrid.ColumnsCount; i++) {
                    InventorySlotWidget inventorySlotWidget = new();
                    inventorySlotWidget.AssignInventorySlot(m_inventory, i);
                    inventorySlotWidget.BevelColor = new Color(181, 172, 154) * 0.6f;
                    inventorySlotWidget.CenterColor = new Color(181, 172, 154) * 0.33f;
                    m_inventoryGrid.Children.Add(inventorySlotWidget);
                    m_inventoryGrid.SetWidgetCell(inventorySlotWidget, new Point2(i, 0));
                }
            }
            base.MeasureOverride(parentAvailableSize);
        }
    }
}