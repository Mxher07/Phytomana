using System.Runtime.CompilerServices;
using Engine;

namespace Game {
    public class Terrain : IDisposable {
        public class ChunksStorage {
            public const int Shift = 8;

            public const int Capacity = 65536;

            public const int CapacityMinusOne = 65535;

            public TerrainChunk[] m_array = new TerrainChunk[Capacity];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual TerrainChunk Get(int x, int y) {
                int num = (x + (y << Shift)) & CapacityMinusOne;
                TerrainChunk terrainChunk;
                while (true) {
                    terrainChunk = m_array[num];
                    if (terrainChunk == null) {
                        return null;
                    }
                    if (terrainChunk.Coords.X == x
                        && terrainChunk.Coords.Y == y) {
                        break;
                    }
                    num = (num + 1) & CapacityMinusOne;
                }
                return terrainChunk;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual TerrainChunk Get(Point2 p) => Get(p.X, p.Y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual TerrainChunk Get(Point3 p) => Get(p.X, p.Z);

            public virtual void Add(int x, int y, TerrainChunk chunk) {
                int num = (x + (y << Shift)) & CapacityMinusOne;
                while (m_array[num] != null) {
                    num = (num + 1) & CapacityMinusOne;
                }
                m_array[num] = chunk;
            }

            public virtual void Remove(int x, int y) {
                int num = (x + (y << Shift)) & CapacityMinusOne;
                while (true) {
                    TerrainChunk terrainChunk = m_array[num];
                    if (terrainChunk == null) {
                        return;
                    }
                    if (terrainChunk.Coords.X == x
                        && terrainChunk.Coords.Y == y) {
                        break;
                    }
                    num = (num + 1) & CapacityMinusOne;
                }
                m_array[num] = null;
            }
        }

        public const int ContentsMask = 1023;

        public const int LightMask = 15360;

        public const int LightShift = 10;

        public const int DataMask = -16384;

        public const int DataShift = 14;

        public const int TopHeightMask = 255;

        public const int TopHeightShift = 0;

        public const int TemperatureMask = 3840;

        public const int TemperatureShift = 8;

        public const int HumidityMask = 61440;

        public const int HumidityShift = 12;

        public const int BottomHeightMask = 16711680;

        public const int BottomHeightShift = 16;

        public const int SunlightHeightMask = -16777216;

        public const int SunlightHeightShift = 24;

        public ChunksStorage m_allChunks;

        public HashSet<TerrainChunk> m_allocatedChunks;

        public TerrainChunk[] m_allocatedChunksArray;

        public int SeasonTemperature;

        public int SeasonHumidity;

        public virtual TerrainChunk[] AllocatedChunks {
            get {
                if (m_allocatedChunksArray == null) {
                    m_allocatedChunksArray = m_allocatedChunks.ToArray();
                }
                return m_allocatedChunksArray;
            }
        }

        public Terrain() {
            m_allChunks = new ChunksStorage();
            m_allocatedChunks = [];
        }

        public virtual void Dispose() {
            foreach (TerrainChunk allocatedChunk in m_allocatedChunks) {
                allocatedChunk.Dispose();
            }
        }

        public virtual TerrainChunk LoopChunks(int startChunkX, int startChunkZ, bool skipStartChunk, out bool hasLooped) {
            hasLooped = false;
            TerrainChunk terrainChunk = null;
            if (!skipStartChunk) {
                terrainChunk = GetChunkAtCoords(startChunkX, startChunkZ);
                if (terrainChunk != null) {
                    return terrainChunk;
                }
            }
            TerrainChunk[] allocatedChunks = AllocatedChunks;
            for (int i = 0; i < allocatedChunks.Length; i++) {
                if (ComparePoints(allocatedChunks[i].Coords, new Point2(startChunkX, startChunkZ)) > 0
                    && (terrainChunk == null || ComparePoints(allocatedChunks[i].Coords, terrainChunk.Coords) < 0)) {
                    terrainChunk = allocatedChunks[i];
                }
            }
            if (terrainChunk == null) {
                for (int j = 0; j < allocatedChunks.Length; j++) {
                    if (terrainChunk == null
                        || ComparePoints(allocatedChunks[j].Coords, terrainChunk.Coords) < 0) {
                        terrainChunk = allocatedChunks[j];
                        hasLooped = true;
                    }
                }
            }
            return terrainChunk;
        }

        public virtual TerrainChunk LoopChunks(int startChunkX, int startChunkZ, bool skipStartChunk) =>
            LoopChunks(startChunkX, startChunkZ, skipStartChunk, out bool _);

        public virtual TerrainChunk GetChunkAtCoords(int chunkX, int chunkZ) => m_allChunks.Get(chunkX, chunkZ);

        public virtual TerrainChunk GetChunkAtCoords(Point2 p) => m_allChunks.Get(p.X, p.Y);

        public virtual TerrainChunk GetChunkAtCoords(int chunkX, int chunkY, int chunkZ) =>
            chunkY is >= 0 and < TerrainChunk.Height / TerrainChunk.Size ? m_allChunks.Get(chunkX, chunkZ) : null;

        public virtual TerrainChunk GetChunkAtCoords(Point3 chunkP) => chunkP.Y is >= 0 and < TerrainChunk.Height / TerrainChunk.Size ? m_allChunks.Get(chunkP.X, chunkP.Z) : null;

        public virtual TerrainChunk GetChunkAtCell(int x, int z) => GetChunkAtCoords(x >> TerrainChunk.SizeBits, z >> TerrainChunk.SizeBits);

        public virtual TerrainChunk GetChunkAtCell(Point2 p) => GetChunkAtCoords(p.X >> TerrainChunk.SizeBits, p.Y >> TerrainChunk.SizeBits);

        public virtual TerrainChunk GetChunkAtCell(int x, int y, int z) => y is >= 0 and < TerrainChunk.Height
            ? m_allChunks.Get(x >> TerrainChunk.SizeBits, z >> TerrainChunk.SizeBits)
            : null;

        public virtual TerrainChunk GetChunkAtCell(Point3 p) => p.Y is >= 0 and < TerrainChunk.Height
            ? m_allChunks.Get(p.X >> TerrainChunk.SizeBits, p.Z >> TerrainChunk.SizeBits)
            : null;

        public virtual TerrainChunk AllocateChunk(int chunkX, int chunkZ) {
            if (GetChunkAtCoords(chunkX, chunkZ) != null) {
                throw new InvalidOperationException("Chunk already allocated.");
            }
            TerrainChunk terrainChunk = new(this, chunkX, chunkZ);
            m_allocatedChunks.Add(terrainChunk);
            m_allChunks.Add(chunkX, chunkZ, terrainChunk);
            m_allocatedChunksArray = null;
            return terrainChunk;
        }

        public virtual void FreeChunk(TerrainChunk chunk) {
            if (!m_allocatedChunks.Remove(chunk)) {
                throw new InvalidOperationException("Chunk not allocated.");
            }
            m_allChunks.Remove(chunk.Coords.X, chunk.Coords.Y);
            m_allocatedChunksArray = null;
            chunk.Dispose();
        }

        public static int ComparePoints(Point2 c1, Point2 c2) {
            if (c1.Y != c2.Y) {
                return c1.Y <= c2.Y ? -1 : 1;
            }
            if (c1.X != c2.X) {
                return c1.X <= c2.X ? -1 : 1;
            }
            return 0;
        }

        public static Point2 ToChunk(Vector2 p) => ToChunk(ToCell(p.X), ToCell(p.Y));

        public static Point2 ToChunk(int x, int z) => new(x >> TerrainChunk.SizeBits, z >> TerrainChunk.SizeBits);

        public static int ToCell(float x) => (int)MathF.Floor(x);

        public static Point2 ToCell(float x, float y) => new((int)MathF.Floor(x), (int)MathF.Floor(y));

        public static Point2 ToCell(Vector2 p) => new((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y));

        public static Point3 ToCell(float x, float y, float z) => new((int)MathF.Floor(x), (int)MathF.Floor(y), (int)MathF.Floor(z));

        public static Point3 ToCell(Vector3 p) => new((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y), (int)MathF.Floor(p.Z));

        public virtual bool IsCellValid(int x, int y, int z) => y is >= 0 and < TerrainChunk.Height;

        public virtual bool IsCellValid(Point3 p) => p.Y is >= 0 and < TerrainChunk.Height;

        public virtual int GetCellValue(int x, int y, int z) => !IsCellValid(x, y, z) ? 0 : GetCellValueFast(x, y, z);

        public virtual int GetCellValue(Point3 p) => !IsCellValid(p) ? 0 : GetCellValueFast(p);

        public virtual int GetCellContents(int x, int y, int z) => !IsCellValid(x, y, z) ? 0 : GetCellContentsFast(x, y, z);

        public virtual int GetCellContents(Point3 p) => !IsCellValid(p) ? 0 : GetCellContentsFast(p);

        public virtual int GetCellLight(int x, int y, int z) => !IsCellValid(x, y, z) ? 0 : GetCellLightFast(x, y, z);

        public virtual int GetCellLight(Point3 p) => !IsCellValid(p) ? 0 : GetCellLightFast(p);

        public virtual int GetCellValueFast(int x, int y, int z) => GetChunkAtCell(x, z)?.GetCellValueFast(x & 0xF, y, z & 0xF) ?? 0;

        public virtual int GetCellValueFast(Point3 p) => GetChunkAtCell(p)?.GetCellValueFast(p.X & 0xF, p.Y, p.Z & 0xF) ?? 0;

        public virtual int GetCellValueFastChunkExists(int x, int y, int z) => GetChunkAtCell(x, z).GetCellValueFast(x & 0xF, y, z & 0xF);

        public virtual int GetCellValueFastChunkExists(Point3 p) => GetChunkAtCell(p).GetCellValueFast(p.X & 0xF, p.Y, p.Z & 0xF);

        public virtual int GetCellContentsFast(int x, int y, int z) => ExtractContents(GetCellValueFast(x, y, z));

        public virtual int GetCellContentsFast(Point3 p) => ExtractContents(GetCellValueFast(p));

        public virtual int GetCellLightFast(int x, int y, int z) => ExtractLight(GetCellValueFast(x, y, z));

        public virtual int GetCellLightFast(Point3 p) => ExtractLight(GetCellValueFast(p));

        public virtual void SetCellValueFast(int x, int y, int z, int value) => GetChunkAtCell(x, z)?.SetCellValueFast(x & 0xF, y, z & 0xF, value);

        public virtual void SetCellValueFast(Point3 p, int value) => GetChunkAtCell(p.X, p.Z)?.SetCellValueFast(p.X & 0xF, p.Y, p.Z & 0xF, value);

        public virtual int CalculateTopmostCellHeight(int x, int z) => GetChunkAtCell(x, z)?.CalculateTopmostCellHeight(x & 0xF, z & 0xF) ?? 0;

        public virtual int CalculateTopmostCellHeight(Point2 p) => GetChunkAtCell(p.X, p.Y)?.CalculateTopmostCellHeight(p.X & 0xF, p.Y & 0xF) ?? 0;

        public virtual int GetShaftValue(int x, int z) => GetChunkAtCell(x, z)?.GetShaftValueFast(x & 0xF, z & 0xF) ?? 0;

        public virtual int GetShaftValue(Point2 p) => GetChunkAtCell(p.X, p.Y)?.GetShaftValueFast(p.X & 0xF, p.Y & 0xF) ?? 0;

        public virtual void SetShaftValue(int x, int z, int value) => GetChunkAtCell(x, z)?.SetShaftValueFast(x & 0xF, z & 0xF, value);

        public virtual void SetShaftValue(Point2 p, int value) => GetChunkAtCell(p.X, p.Y)?.SetShaftValueFast(p.X & 0xF, p.Y & 0xF, value);

        public virtual int GetTemperature(int x, int z) => ExtractTemperature(GetShaftValue(x, z));

        public virtual int GetTemperature(Point2 p) => ExtractTemperature(GetShaftValue(p));

        public virtual void SetTemperature(int x, int z, int temperature) => SetShaftValue(x, z, ReplaceTemperature(GetShaftValue(x, z), temperature));

        public virtual void SetTemperature(Point2 p, int temperature) => SetShaftValue(p, ReplaceTemperature(GetShaftValue(p), temperature));

        public virtual int GetHumidity(int x, int z) => ExtractHumidity(GetShaftValue(x, z));

        public virtual int GetHumidity(Point2 p) => ExtractHumidity(GetShaftValue(p));

        public virtual void SetHumidity(int x, int z, int humidity) => SetShaftValue(x, z, ReplaceHumidity(GetShaftValue(x, z), humidity));

        public virtual int GetTopHeight(int x, int z) => ExtractTopHeight(GetShaftValue(x, z));

        public virtual void SetTopHeight(int x, int z, int topHeight) => SetShaftValue(x, z, ReplaceTopHeight(GetShaftValue(x, z), topHeight));

        public virtual int GetBottomHeight(int x, int z) => ExtractBottomHeight(GetShaftValue(x, z));

        public virtual void SetBottomHeight(int x, int z, int bottomHeight) => SetShaftValue(x, z, ReplaceBottomHeight(GetShaftValue(x, z), bottomHeight));

        public virtual int GetSunlightHeight(int x, int z) => ExtractSunlightHeight(GetShaftValue(x, z));

        public virtual int GetSunlightHeight(Point2 p) => ExtractSunlightHeight(GetShaftValue(p));

        public virtual void SetSunlightHeight(int x, int z, int sunlightHeight) => SetShaftValue(x, z, ReplaceSunlightHeight(GetShaftValue(x, z), sunlightHeight));

        public virtual void SetSunlightHeight(Point2 p, int sunlightHeight) => SetShaftValue(p, ReplaceSunlightHeight(GetShaftValue(p), sunlightHeight));

        public static int MakeBlockValue(int contents) => contents & ContentsMask;

        public static int MakeBlockValue(int contents, int light, int data) =>
            (contents & ContentsMask) | ((light << LightShift) & LightMask) | ((data << DataShift) & DataMask);

        public static int ExtractContents(int value) => value & ContentsMask;

        public static int ExtractLight(int value) => (value & LightMask) >> LightShift;

        public static int ExtractData(int value) => (value & DataMask) >> DataShift;

        public static int ExtractTopHeight(int value) => value & TopHeightMask;

        public static int ExtractBottomHeight(int value) => (value & BottomHeightMask) >> BottomHeightShift;

        public static int ExtractSunlightHeight(int value) => value >>> SunlightHeightShift;

        public static int ExtractHumidity(int value) => (value & HumidityMask) >> HumidityShift;

        public static int ExtractTemperature(int value) => (value & TemperatureMask) >> TemperatureShift;

        /// <summary>
        ///     方块值的最低10位，替换为目标Content
        /// </summary>
        public static int ReplaceContents(int value, int contents) => value ^ ((value ^ contents) & ContentsMask);

        /// <summary>
        ///     方块值的最低10位，替换为目标Content(value始终为0时)
        /// </summary>
        public static int ReplaceContents(int contents) => contents & ContentsMask;

        public static int ReplaceLight(int value, int light) => value ^ ((value ^ (light << LightShift)) & LightMask);

        public static int ReplaceData(int value, int data) => value ^ ((value ^ (data << DataShift)) & DataMask);

        public static int ReplaceTopHeight(int value, int topHeight) => value ^ ((value ^ topHeight) & TopHeightMask);

        public static int ReplaceBottomHeight(int value, int bottomHeight) =>
            value ^ ((value ^ (bottomHeight << BottomHeightShift)) & BottomHeightMask);

        public static int ReplaceSunlightHeight(int value, int sunlightHeight) => (value & 16777215) | (sunlightHeight << SunlightHeightShift);

        public static int ReplaceHumidity(int value, int humidity) => value ^ ((value ^ (humidity << HumidityShift)) & HumidityMask);

        public static int ReplaceTemperature(int value, int temperature) => value ^ ((value ^ (temperature << TemperatureShift)) & TemperatureMask);

        public virtual int GetSeasonalTemperature(int x, int z) => Math.Max(GetTemperature(x, z) + SeasonTemperature, 0);

        public virtual int GetSeasonalTemperature(int shaftValue) => Math.Max(ExtractTemperature(shaftValue) + SeasonTemperature, 0);

        public virtual int GetSeasonalHumidity(int x, int z) => Math.Max(GetHumidity(x, z) + SeasonHumidity, 0);

        public virtual int GetSeasonalHumidity(int shaftValue) => Math.Max(ExtractHumidity(shaftValue) + SeasonHumidity, 0);
    }
}