namespace Game {
    public interface IPaintableBlock {
        int? GetPaintColor(int value);

        int Paint(SubsystemTerrain subsystemTerrain, int value, int? color);
    }
}