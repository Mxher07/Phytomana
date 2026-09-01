namespace Game {
    [Flags]
    public enum CreatureCategory {
        LandPredator = 1,
        LandOther = 1 << 1,
        WaterPredator = 1 << 2,
        WaterOther = 1 << 3,
        Bird = 1 << 4
    }
}