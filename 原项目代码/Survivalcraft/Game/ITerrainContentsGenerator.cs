using Engine;

namespace Game {
    public interface ITerrainContentsGenerator {
        int OceanLevel { get; }

        Vector3 FindCoarseSpawnPosition();

        float CalculateOceanShoreDistance(float x, float z);

        float CalculateHeight(float x, float z);

        int CalculateTemperature(float x, float z);

        int CalculateHumidity(float x, float z);

        float CalculateMountainRangeFactor(float x, float z);

        void GenerateChunkContentsPass1(TerrainChunk chunk);

        void GenerateChunkContentsPass2(TerrainChunk chunk);

        void GenerateChunkContentsPass3(TerrainChunk chunk);

        void GenerateChunkContentsPass4(TerrainChunk chunk);
    }
}