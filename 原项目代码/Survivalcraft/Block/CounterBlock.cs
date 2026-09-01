namespace Game {
    public class CounterBlock : RotateableMountedElectricElementBlock {
        public static int Index = 184;

        public CounterBlock() : base("Models/Gates", "Counter", 0.5f) { }

        public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new CounterElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));

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
                    || connectorDirection == ElectricConnectorDirection.In) {
                    return ElectricConnectorType.Input;
                }
                if (connectorDirection == ElectricConnectorDirection.Top
                    || connectorDirection == ElectricConnectorDirection.Bottom) {
                    return ElectricConnectorType.Output;
                }
            }
            return null;
        }
    }
}