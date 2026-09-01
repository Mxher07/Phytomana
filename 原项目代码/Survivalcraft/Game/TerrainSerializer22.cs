using Engine;

namespace Game {
    public class TerrainSerializer22 : IDisposable {
        public const int MaxChunks = 65536;

        public const int TocEntryBytesCount = 12;

        public const int TocBytesCount = 786444;

        public const int ChunkSizeX = 16;

        public const int ChunkSizeY = 256;

        public const int ChunkSizeZ = 16;

        public const int ChunkBitsX = 4;

        public const int ChunkBitsZ = 4;

        public const int ChunkBytesCount = 263184;

        public const string ChunksFileName = "Chunks32h.dat";

        public Terrain m_terrain;

        public byte[] m_buffer = new byte[262144];

        public Dictionary<Point2, long> m_chunkOffsets = [];

        public IEnumerable<Point2> Chunks => m_chunkOffsets.Keys;

        public Stream m_stream;

        public TerrainSerializer22(Terrain terrain, string directoryName) {
            m_terrain = terrain;
            string path = Storage.CombinePaths(directoryName, "Chunks32h.dat");
            if (!Storage.FileExists(path)) {
                using (Stream stream = Storage.OpenFile(path, OpenFileMode.Create)) {
                    for (int i = 0; i < 65537; i++) {
                        WriteTOCEntry(stream, 0, 0, -1);
                    }
                }
            }
            m_stream = Storage.OpenFile(path, OpenFileMode.ReadWrite);
            while (true) {
                ReadTOCEntry(m_stream, out int cx, out int cz, out int index);
                if (index >= 0) {
                    m_chunkOffsets[new Point2(cx, cz)] = 786444 + 263184L * index;
                    continue;
                }
                break;
            }
        }

        public bool LoadChunk(TerrainChunk chunk) => LoadChunkBlocks(chunk);

        public void SaveChunk(TerrainChunk chunk) {
            if (chunk.State > TerrainChunkState.InvalidContents4
                && chunk.ModificationCounter > 0) {
                SaveChunkBlocks(chunk);
                chunk.ModificationCounter = 0;
            }
        }

        public void Dispose() {
            Utilities.Dispose(ref m_stream);
        }

        public static void ReadChunkHeader(Stream stream) {
            int num = ReadInt(stream);
            int num2 = ReadInt(stream);
            ReadInt(stream);
            ReadInt(stream);
            if (num != -559038737
                || num2 != -2) {
                throw new InvalidOperationException("Invalid chunk header.");
            }
        }

        public static void WriteChunkHeader(Stream stream, int cx, int cz) {
            WriteInt(stream, -559038737);
            WriteInt(stream, -2);
            WriteInt(stream, cx);
            WriteInt(stream, cz);
        }

        public static void ReadTOCEntry(Stream stream, out int cx, out int cz, out int index) {
            cx = ReadInt(stream);
            cz = ReadInt(stream);
            index = ReadInt(stream);
        }

        public static void WriteTOCEntry(Stream stream, int cx, int cz, int index) {
            WriteInt(stream, cx);
            WriteInt(stream, cz);
            WriteInt(stream, index);
        }

        unsafe bool LoadChunkBlocks(TerrainChunk chunk) {
            bool result = false;
            int num = chunk.Origin.X >> 4;
            int num2 = chunk.Origin.Y >> 4;
            try {
                if (m_chunkOffsets.TryGetValue(new Point2(num, num2), out long value)) {
                    _ = Time.RealTime;
                    m_stream.Seek(value, SeekOrigin.Begin);
                    ReadChunkHeader(m_stream);
                    m_stream.ReadExactly(m_buffer, 0, 262144);
                    fixed (byte* ptr = &m_buffer[0]) {
                        int* ptr2 = (int*)ptr;
                        for (int i = 0; i < 16; i++) {
                            for (int j = 0; j < 16; j++) {
                                int num3 = TerrainChunk.CalculateCellIndex(i, 0, j);
                                int num4 = 0;
                                while (num4 < 256) {
                                    chunk.SetCellValueFast(num3, *ptr2);
                                    num4++;
                                    num3++;
                                    ptr2++;
                                }
                            }
                        }
                    }
                    m_stream.ReadExactly(m_buffer, 0, 1024);
                    fixed (byte* ptr3 = &m_buffer[0]) {
                        int* ptr4 = (int*)ptr3;
                        for (int k = 0; k < 16; k++) {
                            for (int l = 0; l < 16; l++) {
                                chunk.SetShaftValueFast(k, l, *ptr4);
                                ptr4++;
                            }
                        }
                    }
                    result = true;
                    _ = Time.RealTime;
                }
                return result;
            }
            catch (Exception e) {
                Log.Error(ExceptionManager.MakeFullErrorMessage($"Error loading data for chunk ({num},{num2}).", e));
                return result;
            }
        }

        unsafe void SaveChunkBlocks(TerrainChunk chunk) {
            _ = Time.RealTime;
            int num = chunk.Origin.X >> 4;
            int num2 = chunk.Origin.Y >> 4;
            try {
                bool flag = false;
                if (m_chunkOffsets.TryGetValue(new Point2(num, num2), out long value)) {
                    m_stream.Seek(value, SeekOrigin.Begin);
                }
                else {
                    flag = true;
                    value = m_stream.Length;
                    m_stream.Seek(value, SeekOrigin.Begin);
                }
                WriteChunkHeader(m_stream, num, num2);
                fixed (byte* ptr = &m_buffer[0]) {
                    int* ptr2 = (int*)ptr;
                    for (int i = 0; i < 16; i++) {
                        for (int j = 0; j < 16; j++) {
                            int num3 = TerrainChunk.CalculateCellIndex(i, 0, j);
                            int num4 = 0;
                            while (num4 < 256) {
                                *ptr2 = chunk.GetCellValueFast(num3);
                                num4++;
                                num3++;
                                ptr2++;
                            }
                        }
                    }
                }
                m_stream.Write(m_buffer, 0, 262144);
                fixed (byte* ptr3 = &m_buffer[0]) {
                    int* ptr4 = (int*)ptr3;
                    for (int k = 0; k < 16; k++) {
                        for (int l = 0; l < 16; l++) {
                            *ptr4 = chunk.GetShaftValueFast(k, l);
                            ptr4++;
                        }
                    }
                }
                m_stream.Write(m_buffer, 0, 1024);
                if (flag) {
                    m_stream.Flush();
                    int num5 = m_chunkOffsets.Count % 65536 * 3 * 4;
                    m_stream.Seek(num5, SeekOrigin.Begin);
                    WriteInt(m_stream, num);
                    WriteInt(m_stream, num2);
                    WriteInt(m_stream, m_chunkOffsets.Count);
                    m_chunkOffsets[new Point2(num, num2)] = value;
                }
                m_stream.Flush();
            }
            catch (Exception e) {
                Log.Error(ExceptionManager.MakeFullErrorMessage($"Error writing data for chunk ({num},{num2}).", e));
            }
            _ = Time.RealTime;
        }

        public static int ReadInt(Stream stream) =>
            stream.ReadByte() + (stream.ReadByte() << 8) + (stream.ReadByte() << 16) + (stream.ReadByte() << 24);

        public static void WriteInt(Stream stream, int value) {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }
    }
}