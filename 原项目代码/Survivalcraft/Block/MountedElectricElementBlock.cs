namespace Game {
    public abstract class MountedElectricElementBlock : Block, IElectricElementBlock {
        public abstract int GetFace(int value);

        public abstract ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z);

        public abstract ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain,
            int value,
            int face,
            int connectorFace,
            int x,
            int y,
            int z);

        public virtual int GetConnectionMask(int value) => int.MaxValue;

        public override bool IsMovableByPiston(int value, int pistonFace, int y, out bool isEnd) {
            isEnd = true;
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            return ((MountedElectricElementBlock)block).GetFace(value) == pistonFace;
        }
    }
}