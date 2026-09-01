using Engine;

namespace Game {
    public class SubsystemTruthTableCircuitBlockBehavior : SubsystemEditableItemBehavior<TruthTableData> {
        public override int[] HandledBlocks => [188];

        public SubsystemTruthTableCircuitBlockBehavior() : base(188) { }

        public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer) {
            if (componentPlayer.DragHostWidget.IsDragInProgress) {
                return false;
            }
            int value = inventory.GetSlotValue(slotIndex);
            int count = inventory.GetSlotCount(slotIndex);
            int id = Terrain.ExtractData(value);
            TruthTableData truthTableData = GetItemData(id);
            truthTableData = truthTableData != null ? (TruthTableData)truthTableData.Copy() : new TruthTableData();
            DialogsManager.ShowDialog(
                componentPlayer.GuiWidget,
                new EditTruthTableDialog(
                    truthTableData,
                    delegate {
                        int data = StoreItemDataAtUniqueId(truthTableData);
                        int value2 = Terrain.ReplaceData(value, data);
                        inventory.RemoveSlotItems(slotIndex, count);
                        inventory.AddSlotItems(slotIndex, value2, count);
                    }
                )
            );
            return true;
        }

        public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer) {
            TruthTableData truthTableData = GetBlockData(new Point3(x, y, z)) ?? new TruthTableData();
            DialogsManager.ShowDialog(
                componentPlayer.GuiWidget,
                new EditTruthTableDialog(
                    truthTableData,
                    delegate {
                        SetBlockData(new Point3(x, y, z), truthTableData);
                        int face = ((TruthTableCircuitBlock)BlocksManager.Blocks[188]).GetFace(value);
                        SubsystemElectricity subsystemElectricity = SubsystemTerrain.Project.FindSubsystem<SubsystemElectricity>(true);
                        ElectricElement electricElement = subsystemElectricity.GetElectricElement(x, y, z, face);
                        if (electricElement != null) {
                            subsystemElectricity.QueueElectricElementForSimulation(electricElement, subsystemElectricity.CircuitStep + 1);
                        }
                    }
                )
            );
            return true;
        }
    }
}