namespace Game {
    public class TruthTableCircuitBlock : RotateableMountedElectricElementBlock {
        public static int Index = 188;

        public TruthTableCircuitBlock() : base("Models/Gates", "TruthTableCircuit", 0.5f) { }

        public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z) =>
            new TruthTableCircuitElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)));

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
                    || connectorDirection == ElectricConnectorDirection.Top) {
                    return ElectricConnectorType.Input;
                }
                if (connectorDirection == ElectricConnectorDirection.In) {
                    return ElectricConnectorType.Output;
                }
            }
            return null;
        }
    }
}