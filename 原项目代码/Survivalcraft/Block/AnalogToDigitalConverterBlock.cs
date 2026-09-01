namespace Game {
    public class AnalogToDigitalConverterBlock : RotateableMountedElectricElementBlock {
        public static int Index = 181;

        public AnalogToDigitalConverterBlock() : base("Models/Gates", "AnalogToDigitalConverter", 0.375f) { }

        public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new AnalogToDigitalConverterElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));

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
                    return ElectricConnectorType.Input;
                }
                if (connectorDirection == ElectricConnectorDirection.Bottom
                    || connectorDirection == ElectricConnectorDirection.Top
                    || connectorDirection == ElectricConnectorDirection.Right
                    || connectorDirection == ElectricConnectorDirection.Left) {
                    return ElectricConnectorType.Output;
                }
            }
            return null;
        }
    }
}