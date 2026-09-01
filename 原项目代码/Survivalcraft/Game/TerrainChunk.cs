using Engine;

namespace Game {
    public class TerrainChunk : IDisposable {
        public struct BrushPaint {
            public Point3 Position;

            public TerrainBrush Brush;
        }

        public const int SizeBits = 4;

        public const int Size = 16;

        public const int HeightBits = 8;

        public const int Height = 256;

        public const int SizeMinusOne = 15;

        public const int HeightMinusOne = 255;

        public const int SliceHeight = 16;

        public const int SlicesCount = 16;

        public Terrain Terrain;

        public Point2 Coords;

        public Point2 Origin;

        public BoundingBox BoundingBox;

        public Vector2 Center;

        public TerrainChunkState State;

        public TerrainChunkState ThreadState;

        public bool WasDowngraded;

        public TerrainChunkState? DowngradedState;

        public bool WasUpgraded;

        public TerrainChunkState? UpgradedState;

        public int ModificationCounter;

        public float[] HazeEnds = new float[4];

        public bool AreBehaviorsNotified;

        public bool IsLoaded;

        public volatile bool NewGeometryData;

        public TerrainChunkGeometry Geometry = new();

        public int[] Cells;

        public int[] Shafts;

        public static ArrayCache<int> m_cellsCache = new([Size * Size * Height], 0.66f, 60f, 0.33f, 5f);

        public static ArrayCache<int> m_shaftsCache = new([Size * Size], 0.66f, 60f, 0.33f, 5f);

        public DynamicArray<BrushPaint> m_brushPaints = [];

        public TerrainGeometry[] ChunkSliceGeometries = new TerrainGeometry[SlicesCount];

        public DynamicArray<TerrainChunkGeometry.Buffer> Buffers = [];

        public int[] SliceContentsHashes = new int[SlicesCount];

        public int[] GeneratedSliceContentsHashes = new int[SlicesCount];

        public TerrainChunk(Terrain terrain, int x, int z) {
            Terrain = terrain;
            Coords = new Point2(x, z);
            Origin = new Point2(x * Size, z * Size);
            BoundingBox = new BoundingBox(new Vector3(Origin.X, 0f, Origin.Y), new Vector3(Origin.X + Size, Height, Origin.Y + Size));
            Center = new Vector2((float)Origin.X + Size / 2, (float)Origin.Y + Size / 2);
            Cells = m_cellsCache.Rent(Size * Size * Height, true);
            Shafts = m_shaftsCache.Rent(Size * Size, true);
        }

        public virtual void DisposeVertexIndexBuffers() {
            foreach (TerrainChunkGeometry.Buffer b in Buffers) {
                b.Dispose();
            }
            Buffers.Clear();
        }

        public virtual void InvalidateSliceContentsHashes() {
            for (int i = 0; i < GeneratedSliceContentsHashes.Length; i++) {
                GeneratedSliceContentsHashes[i] = 0;
            }
        }

        public virtual void CopySliceContentsHashes() {
            for (int i = 0; i < GeneratedSliceContentsHashes.Length; i++) {
                GeneratedSliceContentsHashes[i] = SliceContentsHashes[i];
            }
        }

        public virtual void Dispose() {
            DisposeVertexIndexBuffers();
            if (Geometry == null) {
                throw new InvalidOperationException();
            }
            Geometry = null;
            m_cellsCache.Return(Cells);
            m_shaftsCache.Return(Shafts);
        }

        public static bool IsCellValid(int x, int y, int z) {
            if (x >= 0
                && x < Size
                && y >= 0
                && y < Height
                && z >= 0) {
                return z < Size;
            }
            return false;
        }

        public static bool IsShaftValid(int x, int z) {
            if (x >= 0
                && x < Size
                && z >= 0) {
                return z < Size;
            }
            return false;
        }

        public static int CalculateCellIndex(int x, int y, int z) {
            if (y is >= 0 and < Height) {
                return y | (x << HeightBits) | (z << 12);
            }
            int absY = Math.Abs(y);
            int yUpperBits = absY >> HeightBits;
            if (yUpperBits > 0x7FFF) {
                throw new ArgumentOutOfRangeException(nameof(y), "Height is too large.");
            }
            int yLower8Bits = absY & 0xFF;
            yUpperBits = yUpperBits & 0x7FFF;
            return ((y < 0 ? 1 : 0) << 31) | (yUpperBits << 16) | (z << 12) | (x << HeightBits) | yLower8Bits;
        }

        public virtual int CalculateTopmostCellHeight(int x, int z) {
            int num = CalculateCellIndex(x, HeightMinusOne, z);
            int num2 = HeightMinusOne;
            while (num2 >= 0) {
                if (Terrain.ExtractContents(GetCellValueFast(num)) != 0) {
                    return num2;
                }
                num2--;
                num--;
            }
            return 0;
        }

        public virtual int GetCellValueFast(int index) => Cells[index];

        public virtual int GetCellValueFast(int x, int y, int z) => Cells[y + x * Height + z * Height * Size];

        public virtual int GetCellValueFast(Point3 p) => Cells[p.Y + p.X * Height + p.Z * Height * Size];

        public virtual void SetCellValueFast(int x, int y, int z, int value) {
            Cells[y + x * Height + z * Height * Size] = value;
        }

        public virtual void SetCellValueFast(Point3 p, int value) {
            Cells[p.Y + p.X * Height + p.Z * Height * Size] = value;
        }

        public virtual void SetCellValueFast(int index, int value) {
            Cells[index] = value;
        }

        public virtual int GetCellContentsFast(int x, int y, int z) => Terrain.ExtractContents(GetCellValueFast(x, y, z));

        public virtual int GetCellContentsFast(Point3 p) => Terrain.ExtractContents(GetCellValueFast(p));

        public virtual int GetCellLightFast(int x, int y, int z) => Terrain.ExtractLight(GetCellValueFast(x, y, z));

        public virtual int GetCellLightFast(Point3 p) => Terrain.ExtractLight(GetCellValueFast(p));

        public virtual int GetShaftValueFast(int x, int z) => Shafts[x + z * Size];

        public virtual int GetShaftValueFast(Point2 p) => Shafts[p.X + p.Y * Size];

        public virtual void SetShaftValueFast(int x, int z, int value) => Shafts[x + z * Size] = value;

        public virtual void SetShaftValueFast(Point2 p, int value) => Shafts[p.X + p.Y * Size] = value;

        public virtual int GetTemperatureFast(int x, int z) => Terrain.ExtractTemperature(GetShaftValueFast(x, z));

        public virtual int GetTemperatureFast(Point2 p) => Terrain.ExtractTemperature(GetShaftValueFast(p));

        public virtual void SetTemperatureFast(int x, int z, int temperature) => SetShaftValueFast(x, z, Terrain.ReplaceTemperature(GetShaftValueFast(x, z), temperature));

        public virtual void SetTemperatureFast(Point2 p, int temperature) => SetShaftValueFast(p, Terrain.ReplaceTemperature(GetShaftValueFast(p), temperature));

        public virtual int GetHumidityFast(int x, int z) => Terrain.ExtractHumidity(GetShaftValueFast(x, z));

        public virtual int GetHumidityFast(Point2 p) => Terrain.ExtractHumidity(GetShaftValueFast(p));

        public virtual void SetHumidityFast(int x, int z, int humidity) => SetShaftValueFast(x, z, Terrain.ReplaceHumidity(GetShaftValueFast(x, z), humidity));

        public virtual void SetHumidityFast(Point2 p, int humidity) => SetShaftValueFast(p, Terrain.ReplaceHumidity(GetShaftValueFast(p), humidity));

        public virtual int GetTopHeightFast(int x, int z) => Terrain.ExtractTopHeight(GetShaftValueFast(x, z));

        public virtual int GetTopHeightFast(Point2 p) => Terrain.ExtractTopHeight(GetShaftValueFast(p));

        public virtual void SetTopHeightFast(int x, int z, int topHeight) => SetShaftValueFast(x, z, Terrain.ReplaceTopHeight(GetShaftValueFast(x, z), topHeight));

        public virtual void SetTopHeightFast(Point2 p, int topHeight) => SetShaftValueFast(p, Terrain.ReplaceTopHeight(GetShaftValueFast(p), topHeight));

        public virtual int GetBottomHeightFast(int x, int z) => Terrain.ExtractBottomHeight(GetShaftValueFast(x, z));

        public virtual int GetBottomHeightFast(Point2 p) => Terrain.ExtractBottomHeight(GetShaftValueFast(p));

        public virtual void SetBottomHeightFast(int x, int z, int bottomHeight) => SetShaftValueFast(x, z, Terrain.ReplaceBottomHeight(GetShaftValueFast(x, z), bottomHeight));

        public virtual void SetBottomHeightFast(Point2 p, int bottomHeight) => SetShaftValueFast(p, Terrain.ReplaceBottomHeight(GetShaftValueFast(p), bottomHeight));

        public virtual int GetSunlightHeightFast(int x, int z) => Terrain.ExtractSunlightHeight(GetShaftValueFast(x, z));

        public virtual int GetSunlightHeightFast(Point2 p) => Terrain.ExtractSunlightHeight(GetShaftValueFast(p));

        public virtual void SetSunlightHeightFast(int x, int z, int sunlightHeight) => SetShaftValueFast(x, z, Terrain.ReplaceSunlightHeight(GetShaftValueFast(x, z), sunlightHeight));

        public virtual void SetSunlightHeightFast(Point2 p, int sunlightHeight) => SetShaftValueFast(p, Terrain.ReplaceSunlightHeight(GetShaftValueFast(p), sunlightHeight));

        public virtual void AddBrushPaint(int x, int y, int z, TerrainBrush brush) => m_brushPaints.Add(new BrushPaint { Position = new Point3(x, y, z), Brush = brush });

        public virtual void AddBrushPaint(Point3 p, TerrainBrush brush) => m_brushPaints.Add(new BrushPaint { Position = p, Brush = brush });

        public virtual void ApplyBrushPaints(TerrainChunk chunk) {
            foreach (BrushPaint brushPaint in m_brushPaints) {
                brushPaint.Brush.PaintFast(chunk, brushPaint.Position.X, brushPaint.Position.Y, brushPaint.Position.Z);
            }
        }
    }
}