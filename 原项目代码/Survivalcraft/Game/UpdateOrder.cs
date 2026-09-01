namespace Game {
    public enum UpdateOrder {
        Reset = -100,
        SubsystemPlayers = -20,
        Input = -10,
        Default = 0,
        Locomotion = 1,
        Body = 2,
        CreatureModels = 10,
        FirstPersonModels = 20,
        BlocksScanner = 99,
        Terrain = 100,
        Views = 200,
        BlockHighlight = 201
    }
}