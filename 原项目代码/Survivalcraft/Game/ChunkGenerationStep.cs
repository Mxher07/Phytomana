namespace Game {
    public class ChunkGenerationStep {
        //TODO:可以根据名字来查找
        public bool ShouldGenerate = true;
        public int GenerateOrder = 1600;
        public string Name = string.Empty;

        public bool ErrorLogged = false;

        public Action<TerrainChunk> GenerateAction;

        public ChunkGenerationStep(int generateOrder, Action<TerrainChunk> action) : this(
            generateOrder,
            action,
            action.Method?.Name ?? string.Empty
        ) {
            //为了保证1.8的模组能够用带有2个参数的方法，这玩意别删
        }

        public ChunkGenerationStep(int generateOrder, Action<TerrainChunk> action, string name) {
            GenerateOrder = generateOrder;
            GenerateAction = action;
            Name = name;
        }
    }
}