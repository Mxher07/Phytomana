namespace Game {
    public class SubsystemAdjustableDelayGateBlockBehavior : SubsystemBlockBehavior {
        public override int[] HandledBlocks => [224];

        public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer) {
            int value = inventory.GetSlotValue(slotIndex);
            int count = inventory.GetSlotCount(slotIndex);
            int data = Terrain.ExtractData(value);
            int delay = AdjustableDelayGateBlock.GetDelay(data);
            DialogsManager.ShowDialog(
                componentPlayer.GuiWidget,
                new EditAdjustableDelayGateDialog(
                    delay,
                    delegate(int newDelay) {
                        int data2 = AdjustableDelayGateBlock.SetDelay(data, newDelay);
                        int num = Terrain.ReplaceData(value, data2);
                        if (num != value) {
                            inventory.RemoveSlotItems(slotIndex, count);
                            inventory.AddSlotItems(slotIndex, num, count);
                        }
                    }
                )
            );
            return true;
        }

        public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer) {
            int data = Terrain.ExtractData(value);
            int delay = AdjustableDelayGateBlock.GetDelay(data);
            DialogsManager.ShowDialog(
                componentPlayer.GuiWidget,
                new EditAdjustableDelayGateDialog(
                    delay,
                    delegate(int newDelay) {
                        int num = AdjustableDelayGateBlock.SetDelay(data, newDelay);
                        if (num != data) {
                            int value2 = Terrain.ReplaceData(value, num);
                            SubsystemTerrain.ChangeCell(x, y, z, value2);
                            int face = ((AdjustableDelayGateBlock)BlocksManager.Blocks[224]).GetFace(value);
                            SubsystemElectricity subsystemElectricity = Project.FindSubsystem<SubsystemElectricity>(true);
                            ElectricElement electricElement = subsystemElectricity.GetElectricElement(x, y, z, face);
                            if (electricElement != null) {
                                subsystemElectricity.QueueElectricElementForSimulation(electricElement, subsystemElectricity.CircuitStep + 1);
                            }
                        }
                    }
                )
            );
            return true;
        }
    }
}