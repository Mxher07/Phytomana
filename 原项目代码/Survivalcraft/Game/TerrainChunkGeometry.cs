using Engine;
using Engine.Graphics;

namespace Game {
    public class TerrainChunkGeometry {
        public TerrainChunk TerrainChunk;

        public class Buffer : IDisposable {
            public VertexBuffer VertexBuffer;

            public IndexBuffer IndexBuffer;

            public Texture2D Texture;

            public int[] SubsetIndexBufferStarts = new int[7];

            public int[] SubsetIndexBufferEnds = new int[7];

            public int[] SubsetVertexBufferStarts = new int[7];

            public int[] SubsetVertexBufferEnds = new int[7];

            public virtual void Dispose() {
                Utilities.Dispose(ref VertexBuffer);
                Utilities.Dispose(ref IndexBuffer);
            }
        }
    }
}