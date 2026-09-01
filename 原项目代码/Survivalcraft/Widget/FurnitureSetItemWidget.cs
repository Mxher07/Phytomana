using System.Xml.Linq;
using Engine;
using Engine.Graphics;

namespace Game {
    public class FurnitureSetItemWidget : CanvasWidget, IDragTargetWidget {
        public FurnitureInventoryPanel m_furnitureInventoryPanel;

        public FurnitureSet m_furnitureSet;
        public bool m_highlighted;
        public const string fName = "FurnitureSetItemWidget";

        public FurnitureSetItemWidget(FurnitureInventoryPanel furnitureInventoryWidget, FurnitureSet furnitureSet) {
            m_furnitureInventoryPanel = furnitureInventoryWidget;
            m_furnitureSet = furnitureSet;
            XElement node = ContentManager.Get<XElement>("Widgets/FurnitureSetItemWidget");
            LoadContents(this, node);
            LabelWidget labelWidget = Children.Find<LabelWidget>("FurnitureSetItem.Name");
            LabelWidget labelWidget2 = Children.Find<LabelWidget>("FurnitureSetItem.DesignsCount");
            labelWidget.Text = furnitureSet == null ? LanguageControl.Get(fName, 0) : furnitureSet.Name;
            labelWidget2.Text = string.Format(LanguageControl.Get(fName, 1), CountFurnitureDesigns());
        }

        public void DragDrop(Widget dragWidget, object data) {
            FurnitureDesign furnitureDesign = GetFurnitureDesign(data);
            if (furnitureDesign != null) {
                m_furnitureInventoryPanel.SubsystemFurnitureBlockBehavior.AddToFurnitureSet(furnitureDesign, m_furnitureSet);
                m_furnitureInventoryPanel.Invalidate();
            }
        }

        public void DragOver(Widget dragWidget, object data) {
            m_highlighted = GetFurnitureDesign(data) != null;
        }

        public override void MeasureOverride(Vector2 parentAvailableSize) {
            IsDrawRequired = m_highlighted;
            base.MeasureOverride(parentAvailableSize);
        }

        public override void Draw(DrawContext dc) {
            if (m_highlighted) {
                FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(100, DepthStencilState.None);
                int count = flatBatch2D.TriangleVertices.Count;
                flatBatch2D.QueueQuad(Vector2.Zero, ActualSize, 0f, new Color(128, 128, 128, 128));
                flatBatch2D.TransformTriangles(GlobalTransform, count);
                m_highlighted = false;
            }
        }

        public FurnitureDesign GetFurnitureDesign(object dragData) {
            if (dragData is InventoryDragData inventoryDragData) {
                int slotValue = inventoryDragData.Inventory.GetSlotValue(inventoryDragData.SlotIndex);
                if (Terrain.ExtractContents(slotValue) == 227) {
                    int designIndex = FurnitureBlock.GetDesignIndex(Terrain.ExtractData(slotValue));
                    return m_furnitureInventoryPanel.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                }
            }
            return null;
        }

        public int CountFurnitureDesigns() {
            int num = 0;
            for (int i = 0; i < m_furnitureInventoryPanel.ComponentFurnitureInventory.SlotsCount; i++) {
                int slotValue = m_furnitureInventoryPanel.ComponentFurnitureInventory.GetSlotValue(i);
                if (Terrain.ExtractContents(slotValue) == 227) {
                    int designIndex = FurnitureBlock.GetDesignIndex(Terrain.ExtractData(slotValue));
                    FurnitureDesign design = m_furnitureInventoryPanel.SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
                    if (design != null
                        && design.FurnitureSet == m_furnitureSet) {
                        num++;
                    }
                }
            }
            return num;
        }
    }
}