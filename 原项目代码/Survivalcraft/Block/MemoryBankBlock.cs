namespace Game {
    public class MemoryBankBlock : RotateableMountedElectricElementBlock {
        public static int Index = 186;

        public MemoryBankBlock() : base("Models/Gates", "MemoryBank", 0.875f) { }

        public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new MemoryBankElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));

        public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain,
            int value,
            int face,
            int connectorFace,
            int x,
            int y,
            int z) {
            int data = Terrain.ExtractData(value);
            if (GetFace(value) == face) {
                ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(
                    GetFace(value),
                    GetRotation(data),
                    connectorFace
                );
                if (connectorDirection == ElectricConnectorDirection.Right
                    || connectorDirection == ElectricConnectorDirection.Left
                    || connectorDirection == ElectricConnectorDirection.Bottom
                    || connectorDirection == ElectricConnectorDirection.In) {
                    return ElectricConnectorType.Input;
                }
                if (connectorDirection == ElectricConnectorDirection.Top) {
                    return ElectricConnectorType.Output;
                }
            }
            return null;
        }
    }
}