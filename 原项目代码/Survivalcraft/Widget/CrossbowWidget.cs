using System.Xml.Linq;
using Engine;

namespace Game {
    public class CrossbowWidget : CanvasWidget {
        public IInventory m_inventory;

        public int m_slotIndex;

        public float? m_dragStartOffset;

        public GridPanelWidget m_inventoryGrid;

        public InventorySlotWidget m_inventorySlotWidget;

        public LabelWidget m_instructionsLabel;

        public Random m_random = new();
        public static string fName = "CrossbowWidget";

        public CrossbowWidget(IInventory inventory, int slotIndex) {
            m_inventory = inventory;
            m_slotIndex = slotIndex;
            XElement node = ContentManager.Get<XElement>("Widgets/CrossbowWidget");
            LoadContents(this, node);
            m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
            m_inventorySlotWidget = Children.Find<InventorySlotWidget>("InventorySlot");
            m_instructionsLabel = Children.Find<LabelWidget>("InstructionsLabel");
            for (int i = 0; i < m_inventoryGrid.RowsCount; i++) {
                for (int j = 0; j < m_inventoryGrid.ColumnsCount; j++) {
                    InventorySlotWidget widget = new();
                    m_inventoryGrid.Children.Add(widget);
                    m_inventoryGrid.SetWidgetCell(widget, new Point2(j, i));
                }
            }
            int num = 10;
            foreach (Widget child in m_inventoryGrid.Children) {
                (child as InventorySlotWidget)?.AssignInventorySlot(inventory, num++);
            }
            m_inventorySlotWidget.AssignInventorySlot(inventory, slotIndex);
            m_inventorySlotWidget.CustomViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 1f, 0.2f), new Vector3(0f, 0f, 0.2f), -Vector3.UnitZ);
        }

        public override void Update() {
            int slotValue = m_inventory.GetSlotValue(m_slotIndex);
            int slotCount = m_inventory.GetSlotCount(m_slotIndex);
            int num = Terrain.ExtractContents(slotValue);
            int data = Terrain.ExtractData(slotValue);
            int draw = CrossbowBlock.GetDraw(data);
            ArrowBlock.ArrowType? arrowType = CrossbowBlock.GetArrowType(data);
            if (num == 200
                && slotCount > 0) {
                if (draw < 15) {
                    m_instructionsLabel.Text = LanguageControl.Get(fName, 0);
                }
                else {
                    m_instructionsLabel.Text = !arrowType.HasValue ? LanguageControl.Get(fName, 1) : LanguageControl.Get(fName, 2);
                }
                if ((draw < 15 || !arrowType.HasValue)
                    && Input.Tap.HasValue
                    && HitTestGlobal(Input.Tap.Value) == m_inventorySlotWidget) {
                    Vector2 vector = m_inventorySlotWidget.ScreenToWidget(Input.Press.Value);
                    float num2 = vector.Y - DrawToPosition(draw);
                    if (MathF.Abs(vector.X - m_inventorySlotWidget.ActualSize.X / 2f) < 25f
                        && MathF.Abs(num2) < 25f) {
                        m_dragStartOffset = num2;
                    }
                }
                if (!m_dragStartOffset.HasValue) {
                    return;
                }
                if (Input.Press.HasValue) {
                    int num3 = PositionToDraw(m_inventorySlotWidget.ScreenToWidget(Input.Press.Value).Y - m_dragStartOffset.Value);
                    SetDraw(num3);
                    if (draw <= 9
                        && num3 > 9) {
                        AudioManager.PlaySound("Audio/CrossbowDraw", 1f, m_random.Float(-0.2f, 0.2f), 0f);
                    }
                }
                else {
                    m_dragStartOffset = null;
                    if (draw == 15) {
                        AudioManager.PlaySound("Audio/UI/ItemMoved", 1f, 0f, 0f);
                        return;
                    }
                    SetDraw(0);
                    AudioManager.PlaySound("Audio/CrossbowBoing", MathUtils.Saturate((draw - 3) / 10f), m_random.Float(-0.1f, 0.1f), 0f);
                }
            }
            else {
                ParentWidget.Children.Remove(this);
            }
        }

        public void SetDraw(int draw) {
            int data = Terrain.ExtractData(m_inventory.GetSlotValue(m_slotIndex));
            int value = Terrain.MakeBlockValue(200, 0, CrossbowBlock.SetDraw(data, draw));
            m_inventory.RemoveSlotItems(m_slotIndex, 1);
            m_inventory.AddSlotItems(m_slotIndex, value, 1);
        }

        public static float DrawToPosition(int draw) => draw * 5.4f + 85f;

        public static int PositionToDraw(float position) => (int)Math.Clamp(MathF.Round((position - 85f) / 5.4f), 0f, 15f);
    }
}