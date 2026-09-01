namespace Game {
    public class DigitalToAnalogConverterBlock : RotateableMountedElectricElementBlock {
        public static int Index = 180;

        public DigitalToAnalogConverterBlock() : base("Models/Gates", "DigitalToAnalogConverter", 0.375f) { }

        public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new DigitalToAnalogConverterElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));

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
                if (connectorDirection == ElectricConnectorDirection.In) {
                    return ElectricConnectorType.Output;
                }
                if (connectorDirection == ElectricConnectorDirection.Bottom
                    || connectorDirection == ElectricConnectorDirection.Top
                    || connectorDirection == ElectricConnectorDirection.Right
                    || connectorDirection == ElectricConnectorDirection.Left) {
                    return ElectricConnectorType.Input;
                }
            }
            return null;
        }
    }
}